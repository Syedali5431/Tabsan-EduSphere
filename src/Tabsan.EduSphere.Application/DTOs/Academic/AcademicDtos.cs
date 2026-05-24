namespace Tabsan.EduSphere.Application.DTOs.Academic;

// ── Programme DTOs ────────────────────────────────────────────────────────────

/// <summary>Request body for creating a new degree programme.</summary>
public sealed record CreateProgramRequest(string Name, string Code, Guid DepartmentId, int TotalSemesters);

/// <summary>Request body for updating a programme's name.</summary>
public sealed record UpdateProgramRequest(string Name);

// ── Semester DTOs ─────────────────────────────────────────────────────────────

/// <summary>Request body for creating a new semester.</summary>
public sealed record CreateSemesterRequest(string Name, DateTime StartDate, DateTime EndDate);

// ── Course DTOs ───────────────────────────────────────────────────────────────

/// <summary>Request body for adding a course to the catalogue.</summary>
// Final-Touches Phase 19 Stage 19.1/19.2 — extended with semester/duration/grading fields
public sealed record CreateCourseRequest(
    string Title,
    string Code,
    int CreditHours,
    Guid DepartmentId,
    bool HasSemesters = true,
    int? TotalSemesters = null,
    int? DurationValue = null,
    string? DurationUnit = null,
    string? GradingType = null);

/// <summary>Request body for updating a course title.</summary>
public sealed record UpdateCourseTitleRequest(string NewTitle);

/// <summary>Request body for creating a course offering within a semester.</summary>
public sealed record CreateOfferingRequest(Guid CourseId, Guid SemesterId, int MaxEnrollment, Guid? FacultyUserId);

/// <summary>Request body for assigning faculty to an existing offering.</summary>
public sealed record AssignFacultyRequest(Guid FacultyUserId);

/// <summary>Request body for updating max enrollment for a course offering.</summary>
public sealed record UpdateMaxEnrollmentRequest(int NewMaxEnrollment);

/// <summary>Response payload for course list and detail endpoints.</summary>
public sealed record CourseResponse(
    Guid Id,
    string Title,
    string Code,
    int CreditHours,
    Guid DepartmentId,
    Guid? TenantId,
    Guid? CampusId,
    int InstitutionType,
    string DepartmentName,
    bool IsActive,
    bool HasSemesters,
    int? TotalSemesters,
    int? DurationValue,
    string? DurationUnit,
    string GradingType,
    int CourseType);

/// <summary>Response payload for course offering list endpoints.</summary>
public sealed record CourseOfferingResponse(
    Guid Id,
    Guid CourseId,
    string CourseCode,
    string CourseTitle,
    Guid DepartmentId,
    Guid? TenantId,
    Guid? CampusId,
    int InstitutionType,
    Guid SemesterId,
    string SemesterName,
    Guid? FacultyUserId,
    int MaxEnrollment,
    bool IsActive);

/// <summary>Response payload for current user's offerings.</summary>
public sealed record MyOfferingResponse(
    Guid Id,
    string CourseTitle,
    string SemesterName,
    int MaxEnrollment,
    bool IsOpen);

// ── Enrollment DTOs ───────────────────────────────────────────────────────────

/// <summary>Request body for enrolling a student into a course offering.</summary>
public sealed record EnrollRequest(Guid CourseOfferingId);

// Final-Touches Phase 8 Stage 8.2 — admin-managed enrollment of any student
/// <summary>Request body for an admin to enroll any student into a course offering.</summary>
/// <param name="OverrideClash">When true the Admin bypasses timetable clash rejection (Phase 15 Stage 15.2).</param>
/// <param name="OverrideReason">Reason for the clash override — required when OverrideClash is true.</param>
public sealed record AdminEnrollRequest(
    Guid StudentProfileId,
    Guid CourseOfferingId,
    bool OverrideClash = false,
    string? OverrideReason = null);

/// <summary>Response returned after a successful enrollment action.</summary>
public sealed record EnrollmentResponse(
    Guid EnrollmentId,
    Guid CourseOfferingId,
    string CourseName,
    string SemesterName,
    string Status,
    DateTime EnrolledAt);

// ── Student DTOs ──────────────────────────────────────────────────────────────

/// <summary>Request body for creating a student profile (used by Admin after account creation).</summary>
public sealed record CreateStudentProfileRequest(
    Guid UserId,
    string RegistrationNumber,
    Guid ProgramId,
    Guid DepartmentId,
    DateTime AdmissionDate);

/// <summary>Request body for student self-registration — checked against the whitelist.</summary>
public sealed record StudentSelfRegisterRequest(
    string Username,
    string Password,
    string RegistrationNumberOrEmail,
    string? Email = null,
    string? PhoneNumber = null);

// ── Faculty Assignment DTOs ───────────────────────────────────────────────────

/// <summary>Request body for assigning a faculty member to a department.</summary>
public sealed record AssignFacultyToDepartmentRequest(Guid FacultyUserId, Guid DepartmentId);

/// <summary>Request body for revoking a faculty user's access to a department.</summary>
public sealed record RemoveFacultyFromDepartmentRequest(Guid FacultyUserId, Guid DepartmentId);

/// <summary>Request body for assigning an admin user to a department.</summary>
public sealed record AssignAdminToDepartmentRequest(Guid AdminUserId, Guid DepartmentId);

/// <summary>Request body for revoking an admin user's access to a department.</summary>
public sealed record RemoveAdminFromDepartmentRequest(Guid AdminUserId, Guid DepartmentId);

// ── Whitelist DTOs ────────────────────────────────────────────────────────────

/// <summary>Single whitelist entry — used for both single add and bulk import.</summary>
public sealed record WhitelistEntryRequest(
    string IdentifierType,   // "Email" or "RegistrationNumber"
    string IdentifierValue,
    Guid DepartmentId,
    Guid ProgramId);
