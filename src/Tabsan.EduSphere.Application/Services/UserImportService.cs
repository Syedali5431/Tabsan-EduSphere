using Tabsan.EduSphere.Application.Dtos;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Enums;
using Tabsan.EduSphere.Domain.Identity;
using Tabsan.EduSphere.Domain.Interfaces;

namespace Tabsan.EduSphere.Application.Services;

/// <summary>
/// Parses a CSV stream and bulk-creates user accounts (P4-S1-01).
/// Rules:
///   - CSV header row required: Username,Email,Role (FullName/DepartmentId/InstitutionType/PhoneNumber optional)
///   - Initial password = Username (P4-S2-01)
///   - MustChangePassword is set to true so the user is forced to change on first login (P4-S2-02)
///   - Rows with duplicate usernames (in batch or existing in DB) are counted as duplicates
///   - Rows with missing/invalid fields are counted as errors and skipped
///   - Valid rows are inserted as a single batch after all rows are parsed
/// </summary>
public class UserImportService : IUserImportService
{
    private readonly IUserRepository _userRepo;
    private readonly IPasswordHasher _hasher;
    private readonly IInstitutionPolicyService _institutionPolicyService;

    /// <summary>
    /// Allowed role names for CSV import. SuperAdmin cannot be created via CSV
    /// (must be provisioned by the super-admin directly).
    /// </summary>
    private static readonly HashSet<string> AllowedRoles =
        new(StringComparer.OrdinalIgnoreCase) { "Admin", "Faculty", "Student", "Finance" };

    public UserImportService(
        IUserRepository userRepo,
        IPasswordHasher hasher,
        IInstitutionPolicyService institutionPolicyService)
    {
        _userRepo = userRepo;
        _hasher = hasher;
        _institutionPolicyService = institutionPolicyService;
    }

    public async Task<UserImportResult> ImportFromCsvAsync(Stream csvStream, bool strictMode = false, CancellationToken ct = default)
    {
        var errors = new List<string>();
        var toImport = new List<User>();
        // Track usernames in the current batch to detect intra-batch duplicates.
        var batchUsernames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        // Cache resolved role IDs to avoid a DB round-trip per row.
        var roleCache = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        int totalRows = 0, duplicates = 0;

        using var reader = new StreamReader(csvStream);

        // Skip header line
        var header = await reader.ReadLineAsync(ct);
        if (header is null)
            return new UserImportResult(0, 0, 0, 0, strictMode, new List<string>());

        var headerParts = header.Split(',').Select(h => h.Trim()).ToArray();
        var headerMap = BuildHeaderMap(headerParts);
        if (!headerMap.TryGetValue("username", out var usernameIndex) ||
            !headerMap.TryGetValue("email", out var emailIndex) ||
            !headerMap.TryGetValue("role", out var roleIndex))
        {
            errors.Add("Invalid CSV format: header must include Username, Email, and Role columns.");
            return new UserImportResult(0, 0, 0, 1, strictMode, errors);
        }

        var departmentIdIndex = headerMap.TryGetValue("departmentid", out var depIdx) ? depIdx : -1;
        var institutionTypeIndex = headerMap.TryGetValue("institutiontype", out var instIdx) ? instIdx : -1;
        var phoneNumberIndex = headerMap.TryGetValue("phonenumber", out var phoneIdx) ? phoneIdx : -1;

        var policy = await _institutionPolicyService.GetPolicyAsync(ct);

        int lineNumber = 1;
        string? line;
        while ((line = await reader.ReadLineAsync(ct)) != null)
        {
            lineNumber++;
            totalRows++;

            if (string.IsNullOrWhiteSpace(line))
            {
                totalRows--; // blank lines don't count
                continue;
            }

            var parts = line.Split(',');
            if (parts.Length < headerParts.Length)
            {
                errors.Add($"Line {lineNumber}: Expected at least {headerParts.Length} columns. Got {parts.Length}.");
                continue;
            }

            var username = GetValue(parts, usernameIndex);
            var email = GetValue(parts, emailIndex);
            var roleName = GetValue(parts, roleIndex);
            var deptIdStr = departmentIdIndex >= 0 ? GetValue(parts, departmentIdIndex) : string.Empty;
            var institutionTypeStr = institutionTypeIndex >= 0 ? GetValue(parts, institutionTypeIndex) : string.Empty;
            var phoneNumberRaw = phoneNumberIndex >= 0 ? GetValue(parts, phoneNumberIndex) : string.Empty;

            // ── Validate username ─────────────────────────────────────────────
            if (string.IsNullOrWhiteSpace(username))
            {
                errors.Add($"Line {lineNumber}: Username is empty.");
                continue;
            }

            // ── Validate role ─────────────────────────────────────────────────
            if (!AllowedRoles.Contains(roleName))
            {
                errors.Add($"Line {lineNumber}: Role '{roleName}' is not allowed. Must be one of: Admin, Faculty, Student, Finance.");
                continue;
            }

            // ── Validate email (optional, but must be valid if provided) ──────
            string? emailValue = null;
            if (!string.IsNullOrWhiteSpace(email))
            {
                if (!email.Contains('@') || email.Length > 256)
                {
                    errors.Add($"Line {lineNumber}: Email '{email}' is invalid.");
                    continue;
                }
                emailValue = email;
            }

            // ── Validate DepartmentId (optional) ──────────────────────────────
            Guid? departmentId = null;
            if (!string.IsNullOrWhiteSpace(deptIdStr))
            {
                if (!Guid.TryParse(deptIdStr, out var parsedDept))
                {
                    errors.Add($"Line {lineNumber}: DepartmentId '{deptIdStr}' is not a valid GUID.");
                    continue;
                }
                departmentId = parsedDept;
            }

            InstitutionType? institutionType = null;
            if (!string.IsNullOrWhiteSpace(institutionTypeStr))
            {
                if (!Enum.TryParse<InstitutionType>(institutionTypeStr, ignoreCase: true, out var parsedInstitutionType))
                {
                    errors.Add($"Line {lineNumber}: InstitutionType '{institutionTypeStr}' is invalid. Use School, College, or University.");
                    continue;
                }

                if (!policy.IsEnabled(parsedInstitutionType))
                {
                    errors.Add($"Line {lineNumber}: InstitutionType '{parsedInstitutionType}' is not enabled by the current license policy.");
                    continue;
                }

                institutionType = parsedInstitutionType;
            }

            string? phoneNumber = null;
            if (!string.IsNullOrWhiteSpace(phoneNumberRaw))
            {
                if (phoneNumberRaw.Length > 32)
                {
                    errors.Add($"Line {lineNumber}: PhoneNumber exceeds max length of 32 characters.");
                    continue;
                }

                phoneNumber = phoneNumberRaw;
            }

            // ── Check intra-batch duplicate ────────────────────────────────────
            if (batchUsernames.Contains(username))
            {
                duplicates++;
                continue;
            }

            // ── Check existing DB duplicate ────────────────────────────────────
            if (await _userRepo.UsernameExistsAsync(username, ct))
            {
                duplicates++;
                continue;
            }

            // ── Resolve role ID (cached) ───────────────────────────────────────
            if (!roleCache.TryGetValue(roleName, out var roleId))
            {
                var role = await _userRepo.GetRoleByNameAsync(roleName, ct);
                if (role is null)
                {
                    errors.Add($"Line {lineNumber}: Role '{roleName}' not found in the database.");
                    continue;
                }
                roleCache[roleName] = role.Id;
                roleId = role.Id;
            }

            // ── Build user — initial password = username (P4-S2-01) ───────────
            var passwordHash = _hasher.Hash(username);
            var user = new User(
                username: username,
                passwordHash: passwordHash,
                roleId: roleId,
                email: emailValue,
                departmentId: departmentId,
                mustChangePassword: true,   // P4-S2-02: force change on first login
                institutionType: institutionType,
                phoneNumber: phoneNumber
            );

            batchUsernames.Add(username);
            toImport.Add(user);
        }

        var shouldRollback = strictMode && (errors.Count > 0 || duplicates > 0);

        if (!shouldRollback && toImport.Count > 0)
        {
            await _userRepo.AddRangeAsync(toImport, ct);
            await _userRepo.SaveChangesAsync(ct);
        }

        if (shouldRollback)
        {
            errors.Insert(0, $"Strict mode rollback: no users were imported because {errors.Count} validation issue(s) and {duplicates} duplicate row(s) were detected.");
            return new UserImportResult(
                TotalRows: totalRows,
                Imported: 0,
                Duplicates: duplicates,
                Errors: errors.Count,
                StrictMode: true,
                ErrorDetails: errors);
        }

        return new UserImportResult(
            TotalRows: totalRows,
            Imported: toImport.Count,
            Duplicates: duplicates,
            Errors: errors.Count,
            StrictMode: strictMode,
            ErrorDetails: errors
        );
    }

    private static Dictionary<string, int> BuildHeaderMap(string[] headerParts)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < headerParts.Length; i++)
        {
            var key = headerParts[i].Trim();
            if (!string.IsNullOrWhiteSpace(key) && !map.ContainsKey(key))
                map[key] = i;
        }

        return map;
    }

    private static string GetValue(string[] parts, int index)
    {
        if (index < 0 || index >= parts.Length)
            return string.Empty;

        return parts[index].Trim();
    }
}
