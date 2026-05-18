using Tabsan.EduSphere.Application.DTOs.Academic;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Academic;
using Tabsan.EduSphere.Domain.Auditing;
using Tabsan.EduSphere.Domain.Identity;
using Tabsan.EduSphere.Domain.Interfaces;

namespace Tabsan.EduSphere.Application.Academic;

/// <summary>
/// Handles both student self-registration (whitelist-gated) and
/// Admin-managed student profile creation.
/// Both flows create a User + StudentProfile atomically.
/// </summary>
public class StudentRegistrationService : IStudentRegistrationService
{
    private readonly IRegistrationWhitelistRepository _whitelistRepo;
    private readonly IStudentProfileRepository _studentProfileRepo;
    private readonly IUserRepository _userRepo;
    private readonly IAcademicProgramRepository _programRepo;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuditService _audit;

    public StudentRegistrationService(
        IRegistrationWhitelistRepository whitelistRepo,
        IStudentProfileRepository studentProfileRepo,
        IUserRepository userRepo,
        IAcademicProgramRepository programRepo,
        IPasswordHasher passwordHasher,
        IAuditService audit)
    {
        _whitelistRepo = whitelistRepo;
        _studentProfileRepo = studentProfileRepo;
        _userRepo = userRepo;
        _programRepo = programRepo;
        _passwordHasher = passwordHasher;
        _audit = audit;
    }

    // ── Self-registration ──────────────────────────────────────────────────────

    /// <summary>
    /// Allows a prospective student to create their own account.
    /// Flow:
    ///   1. Look up the whitelist by the provided identifier (email or reg number).
    ///   2. Verify the entry has not already been used.
    ///   3. Create a User with the Student role.
    ///   4. Create a StudentProfile linked to the whitelist's programme and department.
    ///   5. Mark the whitelist entry as consumed.
    /// Returns the new User ID on success; null when the identifier is not whitelisted or already used.
    /// </summary>
    public async Task<Guid?> SelfRegisterAsync(StudentSelfRegisterRequest request, CancellationToken ct = default)
    {
        // Find the whitelist entry — case-insensitive.
        var entry = await _whitelistRepo.FindUnusedAsync(request.RegistrationNumberOrEmail, ct);
        if (entry is null) return null;

        // Prevent duplicate usernames.
        if (await _userRepo.UsernameExistsAsync(request.Username, ct))
            return null;

        var program = await _programRepo.GetByIdAsync(entry.ProgramId, ct);
        if (program is null) return null;
        if (program.DepartmentId != entry.DepartmentId) return null;

        // Hardcoded Student role ID = 4 (seeded in Phase 1).
        const int studentRoleId = 4;
        var passwordHash = _passwordHasher.Hash(request.Password);

        var user = new User(request.Username, passwordHash, studentRoleId,
                            email: request.Email, departmentId: entry.DepartmentId, phoneNumber: request.PhoneNumber);
        await _userRepo.AddAsync(user, ct);
        await _userRepo.SaveChangesAsync(ct);

        // Generate registration number from the whitelist identifier when it's a reg number type.
        var regNumber = entry.IdentifierType == WhitelistIdentifierType.RegistrationNumber
            ? entry.IdentifierValue
            : $"AUTO-{user.Id.ToString("N")[..8].ToUpperInvariant()}";

        var profile = new StudentProfile(user.Id, regNumber, entry.ProgramId, entry.DepartmentId, DateTime.UtcNow);
        await _studentProfileRepo.AddAsync(profile, ct);

        entry.MarkUsed(user.Id);
        _whitelistRepo.Update(entry);
        await _studentProfileRepo.SaveChangesAsync(ct);

        await _audit.LogAsync(new AuditLog("SelfRegister", "User", user.Id.ToString(),
            actorUserId: user.Id), ct);

        return user.Id;
    }

    // ── Admin-managed profile creation ────────────────────────────────────────

    /// <summary>
    /// Creates a StudentProfile for an existing User that was created by an Admin.
    /// Does not touch the whitelist — Admin bypasses the self-registration gate.
    /// </summary>
    public async Task<Guid> CreateProfileAsync(CreateStudentProfileRequest request, CancellationToken ct = default)
    {
        if (await _studentProfileRepo.RegistrationNumberExistsAsync(request.RegistrationNumber, ct))
            throw new InvalidOperationException($"Registration number '{request.RegistrationNumber}' is already in use.");

        var program = await _programRepo.GetByIdAsync(request.ProgramId, ct)
            ?? throw new InvalidOperationException("Academic program was not found.");

        if (program.DepartmentId != request.DepartmentId)
            throw new InvalidOperationException("Program and department must belong to the same academic scope.");

        var profile = new StudentProfile(
            request.UserId,
            request.RegistrationNumber,
            request.ProgramId,
            request.DepartmentId,
            request.AdmissionDate);

        await _studentProfileRepo.AddAsync(profile, ct);
        await _studentProfileRepo.SaveChangesAsync(ct);

        await _audit.LogAsync(new AuditLog("CreateStudentProfile", "StudentProfile", profile.Id.ToString(),
            actorUserId: request.UserId), ct);

        return profile.Id;
    }
}
