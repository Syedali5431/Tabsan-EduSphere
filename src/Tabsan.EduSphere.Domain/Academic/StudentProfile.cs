using Tabsan.EduSphere.Domain.Common;
using Tabsan.EduSphere.Domain.Enums;

namespace Tabsan.EduSphere.Domain.Academic;

/// <summary>
/// Extended academic profile for a Student user.
/// Holds registration number, programme, department, CGPA, and enrolment history.
/// There is exactly one StudentProfile per User account with the Student role.
/// </summary>
public class StudentProfile : AuditableEntity
{
    /// <summary>FK to the User identity record.</summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Unique registration / roll number issued at admission.
    /// Format is institution-defined (e.g. "FA22-BSCS-001").
    /// </summary>
    public string RegistrationNumber { get; private set; } = default!;

    /// <summary>FK to the degree programme the student is enrolled in.</summary>
    public Guid ProgramId { get; private set; }

    /// <summary>Navigation to the academic programme.</summary>
    public AcademicProgram Program { get; private set; } = default!;

    /// <summary>FK to the student's home department (denormalized from programme for query performance).</summary>
    public Guid DepartmentId { get; private set; }

    /// <summary>Navigation to the department.</summary>
    public Department Department { get; private set; } = default!;

    /// <summary>UTC date the student was formally admitted.</summary>
    public DateTime AdmissionDate { get; private set; }

    /// <summary>Current cumulative GPA. Updated by the grading service each time results are published.</summary>
    public decimal Cgpa { get; private set; }

    /// <summary>Latest calculated semester GPA for the student's current semester context.</summary>
    public decimal CurrentSemesterGpa { get; private set; }

    /// <summary>Current semester number the student is in (1-based).</summary>
    public int CurrentSemesterNumber { get; private set; } = 1;

    /// <summary>Current lifecycle status: Active, Inactive, or Graduated.</summary>
    public StudentStatus Status { get; private set; } = StudentStatus.Active;

    /// <summary>UTC date when the student was formally graduated. Only set when Status = Graduated.</summary>
    public DateTime? GraduatedDate { get; private set; }

    private StudentProfile() { }

    public StudentProfile(Guid userId, string registrationNumber, Guid programId, Guid departmentId, DateTime admissionDate)
    {
        UserId = userId;
        RegistrationNumber = registrationNumber;
        ProgramId = programId;
        DepartmentId = departmentId;
        AdmissionDate = admissionDate;
    }

    /// <summary>
    /// Updates the student's CGPA after result publication.
    /// CGPA must be between 0.0 and 4.0 on a standard scale.
    /// </summary>
    public void UpdateCgpa(decimal newCgpa)
    {
        if (newCgpa < 0 || newCgpa > 4.0m)
            throw new ArgumentOutOfRangeException(nameof(newCgpa), "CGPA must be between 0.0 and 4.0.");
        Cgpa = newCgpa;
        Touch();
    }

    /// <summary>
    /// Updates both the latest semester GPA and the cumulative GPA after result recalculation.
    /// </summary>
    public void UpdateAcademicStanding(decimal semesterGpa, decimal cgpa)
    {
        if (semesterGpa < 0 || semesterGpa > 4.0m)
            throw new ArgumentOutOfRangeException(nameof(semesterGpa), "Semester GPA must be between 0.0 and 4.0.");
        if (cgpa < 0 || cgpa > 4.0m)
            throw new ArgumentOutOfRangeException(nameof(cgpa), "CGPA must be between 0.0 and 4.0.");

        CurrentSemesterGpa = semesterGpa;
        Cgpa = cgpa;
        Touch();
    }

    /// <summary>Advances the student to the next semester number after semester completion.
    /// Returns true if the student reached a terminal level (graduation/completion boundary).</summary>
    public bool AdvanceSemester()
    {
        CurrentSemesterNumber++;
        Touch();

        // School: Class 10 → Completed (do not advance to Class 11)
        if (Department?.InstitutionType == InstitutionType.School && CurrentSemesterNumber > 10)
        {
            Status = StudentStatus.Graduated; // "Completed" in school context
            GraduatedDate = DateTime.UtcNow;
            return true;
        }

        // College: Class 12 → Completed. CurrentSemesterNumber uses the same absolute
        // class-number convention as School (e.g. Class 11/Class 12, not a relative 1/2
        // program-semester count), consistent with seed data and CourseOffering semester
        // naming - so the completion boundary must be an absolute "> 12", mirroring School's
        // "> 10" above, not the College program's (relative) TotalSemesters.
        if (Department?.InstitutionType == InstitutionType.College && CurrentSemesterNumber > 12)
        {
            Status = StudentStatus.Graduated; // "Completed" in college context
            GraduatedDate = DateTime.UtcNow;
            return true;
        }

        // University: reached total semesters → Graduated
        if (Department?.InstitutionType == InstitutionType.University)
        {
            var maxSemesters = Program?.TotalSemesters ?? 8;
            if (CurrentSemesterNumber > maxSemesters)
            {
                Status = StudentStatus.Graduated;
                GraduatedDate = DateTime.UtcNow;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Moves the student to a different institution/department/programme, resetting the
    /// per-institution academic counters (semester number, semester GPA). Used when a
    /// student who completed School (Class 10) is admitted into College, or a student who
    /// completed College (Class 12) is admitted into University - there was previously no
    /// supported way to do this through the application itself (only direct database access),
    /// even though completion messaging elsewhere in the app ("Student cleared the school.
    /// Create certificate for him.") assumes this transition happens.
    /// Does not touch Cgpa, RegistrationNumber, or AdmissionDate - those carry over.
    /// </summary>
    public void TransferInstitution(Guid departmentId, Guid programId, int semesterNumber)
    {
        if (semesterNumber < 1)
            throw new ArgumentOutOfRangeException(nameof(semesterNumber), "Semester number must be at least 1.");

        DepartmentId = departmentId;
        ProgramId = programId;
        CurrentSemesterNumber = semesterNumber;
        CurrentSemesterGpa = 0;
        Status = StudentStatus.Active;
        GraduatedDate = null;
        Touch();
    }

    /// <summary>Marks the student as Graduated with the current UTC date.</summary>
    public void Graduate()
    {
        Status = StudentStatus.Graduated;
        GraduatedDate = DateTime.UtcNow;
        Touch();
    }

    /// <summary>Marks the student as Inactive (dropout, leave of absence, etc.). Student will be blocked from login.</summary>
    public void Deactivate()
    {
        Status = StudentStatus.Inactive;
        Touch();
    }

    /// <summary>Marks the student as Active (re-activates an inactive account).</summary>
    public void Reactivate()
    {
        Status = StudentStatus.Active;
        Touch();
    }
}
