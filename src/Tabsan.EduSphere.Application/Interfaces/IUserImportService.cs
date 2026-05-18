using Tabsan.EduSphere.Application.Dtos;

namespace Tabsan.EduSphere.Application.Interfaces;

/// <summary>
/// Service for bulk-importing user accounts from a CSV file (P4-S1-01).
/// On import, each user's initial password is set to their username (P4-S2-01)
/// and MustChangePassword is set to true so the user is forced to set a new
/// password on first login (P4-S2-02).
/// </summary>
public interface IUserImportService
{
    /// <summary>
    /// Parses a CSV stream and creates user accounts for each valid row.
    /// CSV must have a header row: Username,Email,FullName,Role
    /// (DepartmentId/InstitutionType/PhoneNumber are optional; omit or leave blank when not needed).
    /// Returns a summary with counts of imported, duplicate, and erroneous rows.
    /// </summary>
    Task<UserImportResult> ImportFromCsvAsync(Stream csvStream, bool strictMode = false, CancellationToken ct = default);
}
