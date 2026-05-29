// Final-Touches Phase 16 Stage 16.1 — repository interface for gradebook grid data

namespace Tabsan.EduSphere.Domain.Interfaces;

using Tabsan.EduSphere.Domain.Enums;

/// <summary>
/// Provides efficient queries for the gradebook grid, joining enrollments,
/// student profiles, and users in a single database round-trip.
/// </summary>
public interface IGradebookRepository
{
    // Final-Touches Phase 16 Stage 16.1 — retrieve all students enrolled in an offering
    // Returns projection tuples: (StudentProfileId, RegistrationNumber, StudentName)
    Task<IReadOnlyList<GradebookStudentInfo>> GetStudentsForOfferingAsync(
        Guid courseOfferingId,
        CancellationToken ct = default);

    // Final-Touches Phase 16 enhancement — resolve institution type for offering-specific gradebook logic.
    Task<InstitutionType> GetInstitutionTypeForOfferingAsync(
        Guid courseOfferingId,
        CancellationToken ct = default);
}

/// <summary>
/// Lightweight projection of a student's identity for use in the gradebook grid.
/// </summary>
public sealed record GradebookStudentInfo(
    Guid   StudentProfileId,
    string RegistrationNumber,
    string StudentName,
    decimal Cgpa);
