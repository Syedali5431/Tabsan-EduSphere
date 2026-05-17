using Tabsan.EduSphere.Application.DTOs.Academic;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Academic;
using Tabsan.EduSphere.Domain.Auditing;
using Tabsan.EduSphere.Domain.Enums;
using Tabsan.EduSphere.Domain.Interfaces;

namespace Tabsan.EduSphere.Application.Academic;

/// <summary>
/// Orchestrates student enrollment and drop operations.
/// Enforces all business rules:
///   - The course offering must be open.
///   - The parent semester must not be closed.
    ///   - The offering must have available seats or the student is waitlisted.
    ///   - A student cannot hold more than one enrollment record in the same offering.
/// Enrollment rows are never deleted — drops change status to Dropped.
/// </summary>
public class EnrollmentService : IEnrollmentService
{
    private readonly IEnrollmentRepository _enrollmentRepo;
    private readonly ICourseRepository _courseRepo;
    private readonly ISemesterRepository _semesterRepo;
    private readonly IAuditService _audit;
    private readonly IPrerequisiteRepository _prerequisiteRepo;
    private readonly IResultRepository _resultRepo;
    private readonly ITimetableRepository _timetableRepo;
    private readonly IInstitutionGradingProfileRepository _gradingProfileRepo;

    public EnrollmentService(
        IEnrollmentRepository enrollmentRepo,
        ICourseRepository courseRepo,
        ISemesterRepository semesterRepo,
        IAuditService audit,
        IPrerequisiteRepository prerequisiteRepo,
        IResultRepository resultRepo,
        ITimetableRepository timetableRepo,
        IInstitutionGradingProfileRepository gradingProfileRepo)
    {
        _enrollmentRepo = enrollmentRepo;
        _courseRepo = courseRepo;
        _semesterRepo = semesterRepo;
        _audit = audit;
        _prerequisiteRepo = prerequisiteRepo;
        _resultRepo = resultRepo;
        _timetableRepo = timetableRepo;
        _gradingProfileRepo = gradingProfileRepo;
    }

    /// <summary>
    /// Attempts to enroll the student in the requested offering.
    /// Checks (in order): offering exists → semester not closed → offering open → not already enrolled → seat available.
    /// Returns null on any failure.
    /// </summary>
    public async Task<EnrollmentResponse?> EnrollAsync(Guid studentProfileId, EnrollRequest request, CancellationToken ct = default)
    {
        var offering = await _courseRepo.GetOfferingByIdAsync(request.CourseOfferingId, ct);
        if (offering is null) return null;

        // Guard: semester must not be closed.
        if (offering.Semester.IsClosed) return null;

        // Guard: offering must still be accepting enrollments.
        if (!offering.IsOpen) return null;

        // Guard: prevent duplicate open enrollment records (active or waitlisted).
        var existingEnrollment = await _enrollmentRepo.GetAsync(studentProfileId, request.CourseOfferingId, ct);
        if (existingEnrollment is not null && existingEnrollment.Status != EnrollmentStatus.Dropped && existingEnrollment.Status != EnrollmentStatus.Cancelled)
            return null;

        // Guard: check seat availability; if full, enqueue the student instead of rejecting outright.
        var currentCount = await _courseRepo.GetEnrollmentCountAsync(request.CourseOfferingId, ct);
        var isWaitlisted = currentCount >= offering.MaxEnrollment;

        var enrollment = new Enrollment(studentProfileId, request.CourseOfferingId);
        if (isWaitlisted)
            enrollment.Waitlist();

        await _enrollmentRepo.AddAsync(enrollment, ct);
        await _enrollmentRepo.SaveChangesAsync(ct);

        await _audit.LogAsync(new AuditLog(
            "Enroll", "Enrollment", enrollment.Id.ToString(),
            actorUserId: studentProfileId), ct);

        return new EnrollmentResponse(
            EnrollmentId:     enrollment.Id,
            CourseOfferingId: offering.Id,
            CourseName:       offering.Course.Title,
            SemesterName:     offering.Semester.Name,
            Status:           enrollment.Status.ToString(),
            EnrolledAt:       enrollment.EnrolledAt);
    }

    /// <summary>
    /// Drops the student's active enrollment in the given offering.
    /// Changes the status to Dropped; the row is preserved for academic history.
    /// Returns false when no active enrollment is found.
    /// </summary>
    public async Task<bool> DropAsync(Guid studentProfileId, Guid courseOfferingId, CancellationToken ct = default)
    {
        var enrollment = await _enrollmentRepo.GetAsync(studentProfileId, courseOfferingId, ct);
        if (enrollment is null || enrollment.Status != EnrollmentStatus.Active)
            return false;

        enrollment.Drop();
        _enrollmentRepo.Update(enrollment);
        await _enrollmentRepo.SaveChangesAsync(ct);

        await PromoteNextWaitlistedEnrollmentAsync(courseOfferingId, studentProfileId, ct);

        await _audit.LogAsync(new AuditLog(
            "Drop", "Enrollment", enrollment.Id.ToString(),
            actorUserId: studentProfileId), ct);

        return true;
    }

    /// <summary>Returns all enrollment records for the student (full history, not just active).</summary>
    public Task<IReadOnlyList<Enrollment>> GetForStudentAsync(Guid studentProfileId, CancellationToken ct = default)
        => _enrollmentRepo.GetByStudentAsync(studentProfileId, ct);

    /// <summary>Returns all active enrollments in the given course offering.</summary>
    public Task<IReadOnlyList<Enrollment>> GetForOfferingAsync(Guid courseOfferingId, CancellationToken ct = default)
        => _enrollmentRepo.GetByOfferingAsync(courseOfferingId, ct);

    // Final-Touches Phase 8 Stage 8.2 — admin drop any enrollment by its ID
    /// <summary>Drops any active enrollment identified by enrollment ID. Returns false when not found or not active.</summary>
    public async Task<bool> AdminDropByIdAsync(Guid enrollmentId, CancellationToken ct = default)
    {
        var enrollment = await _enrollmentRepo.GetByIdAsync(enrollmentId, ct);
        if (enrollment is null || enrollment.Status != EnrollmentStatus.Active)
            return false;

        enrollment.Drop();
        _enrollmentRepo.Update(enrollment);
        await _enrollmentRepo.SaveChangesAsync(ct);

        await PromoteNextWaitlistedEnrollmentAsync(enrollment.CourseOfferingId, enrollment.StudentProfileId, ct);
        return true;
    }

    // Final-Touches Phase 15 Stages 15.1 & 15.2 — TryEnrollAsync: prerequisite + clash checks
    /// <summary>
    /// Attempts enrollment with full Phase 15 rule checks (prerequisite validation + timetable clash detection).
    /// Returns a rich <see cref="EnrollmentAttemptResult"/> describing success or the specific rejection reason.
    /// </summary>
    public async Task<EnrollmentAttemptResult> TryEnrollAsync(
        Guid studentProfileId,
        Guid courseOfferingId,
        bool overrideClash = false,
        string? overrideReason = null,
        CancellationToken ct = default)
    {
        // 1. Load offering with navigations.
        var offering = await _courseRepo.GetOfferingByIdAsync(courseOfferingId, ct);
        if (offering is null)
            return new EnrollmentAttemptResult(false, RejectionReason: "Offering not found.");

        // 2. Semester closed guard.
        if (offering.Semester.IsClosed)
            return new EnrollmentAttemptResult(false, RejectionReason: "Semester is closed.");

        // 3. Offering open guard.
        if (!offering.IsOpen)
            return new EnrollmentAttemptResult(false, RejectionReason: "Enrollment is closed for this offering.");

        // 4. Duplicate enrollment guard.
        var existingEnrollment = await _enrollmentRepo.GetAsync(studentProfileId, courseOfferingId, ct);
        if (existingEnrollment is not null && existingEnrollment.Status != EnrollmentStatus.Dropped && existingEnrollment.Status != EnrollmentStatus.Cancelled)
            return new EnrollmentAttemptResult(false, RejectionReason: "Already enrolled in this offering.");

        // 5. Seat availability guard — if full, queue the student instead of failing.
        var currentCount = await _courseRepo.GetEnrollmentCountAsync(courseOfferingId, ct);
        var isWaitlisted = currentCount >= offering.MaxEnrollment;

        // 6. Prerequisite check (Stage 15.1).
        var prerequisites = await _prerequisiteRepo.GetByCourseIdAsync(offering.CourseId, ct);
        if (prerequisites.Count > 0)
        {
            var prerequisitePassThreshold = await ResolvePrerequisitePassThresholdPercentageAsync(studentProfileId, ct);
            var unmet = new List<string>();
            foreach (var prereq in prerequisites)
            {
                if (!await _resultRepo.HasPassedCourseAsync(
                        studentProfileId,
                        prereq.PrerequisiteCourseId,
                        prerequisitePassThreshold,
                        ct))
                {
                    var label = prereq.PrerequisiteCourse is not null
                        ? $"{prereq.PrerequisiteCourse.Code} – {prereq.PrerequisiteCourse.Title}"
                        : prereq.PrerequisiteCourseId.ToString();
                    unmet.Add(label);
                }
            }
            if (unmet.Count > 0)
                return new EnrollmentAttemptResult(false,
                    RejectionReason: "Unmet prerequisites.",
                    UnmetPrerequisites: unmet);
        }

        // 7. Timetable clash check (Stage 15.2) — skipped when admin provides override.
        if (!overrideClash)
        {
            var requestedSlots = await _timetableRepo.GetEntriesByCourseOfferingAsync(offering.CourseId, offering.SemesterId, ct);
            if (requestedSlots.Count > 0)
            {
                var studentEnrollments = await _enrollmentRepo.GetByStudentAsync(studentProfileId, ct);
                var activeOfferingIds = studentEnrollments
                    .Where(e => e.Status == EnrollmentStatus.Active && e.CourseOfferingId != courseOfferingId)
                    .Select(e => e.CourseOfferingId)
                    .ToList();

                var clashes = new List<string>();
                foreach (var existingOfferingId in activeOfferingIds)
                {
                    var existingOffering = await _courseRepo.GetOfferingByIdAsync(existingOfferingId, ct);
                    if (existingOffering is null) continue;

                    var existingSlots = await _timetableRepo.GetEntriesByCourseOfferingAsync(
                        existingOffering.CourseId, existingOffering.SemesterId, ct);

                    foreach (var reqSlot in requestedSlots)
                    {
                        foreach (var exSlot in existingSlots)
                        {
                            if (reqSlot.DayOfWeek == exSlot.DayOfWeek
                                && reqSlot.StartTime < exSlot.EndTime
                                && reqSlot.EndTime > exSlot.StartTime)
                            {
                                var day = ((DayOfWeek)reqSlot.DayOfWeek).ToString();
                                clashes.Add(
                                    $"{reqSlot.SubjectName} ({day} {reqSlot.StartTime:hh\\:mm}–{reqSlot.EndTime:hh\\:mm}) " +
                                    $"conflicts with {exSlot.SubjectName} ({day} {exSlot.StartTime:hh\\:mm}–{exSlot.EndTime:hh\\:mm})");
                            }
                        }
                    }
                }

                if (clashes.Count > 0)
                    return new EnrollmentAttemptResult(false,
                        RejectionReason: "Timetable clash detected.",
                        ClashDetails: clashes);
            }
        }

        // 8. All checks passed — create enrollment.
        var enrollment = new Enrollment(studentProfileId, courseOfferingId);
        if (isWaitlisted)
            enrollment.Waitlist();

        await _enrollmentRepo.AddAsync(enrollment, ct);
        await _enrollmentRepo.SaveChangesAsync(ct);

        // Audit clash override when applicable.
        if (overrideClash)
        {
            await _audit.LogAsync(new AuditLog(
                "EnrollClashOverride", "Enrollment", enrollment.Id.ToString(),
                actorUserId: studentProfileId,
                newValuesJson: overrideReason), ct);
        }

        await _audit.LogAsync(new AuditLog(
            "Enroll", "Enrollment", enrollment.Id.ToString(),
            actorUserId: studentProfileId), ct);

        return new EnrollmentAttemptResult(true, Enrollment: new EnrollmentResponse(
            EnrollmentId:     enrollment.Id,
            CourseOfferingId: offering.Id,
            CourseName:       offering.Course.Title,
            SemesterName:     offering.Semester.Name,
            Status:           enrollment.Status.ToString(),
            EnrolledAt:       enrollment.EnrolledAt));
    }

    private async Task PromoteNextWaitlistedEnrollmentAsync(Guid courseOfferingId, Guid actorStudentProfileId, CancellationToken ct)
    {
        var offering = await _courseRepo.GetOfferingByIdAsync(courseOfferingId, ct);
        if (offering is null || offering.Semester.IsClosed || !offering.IsOpen)
            return;

        var waitlisted = await _enrollmentRepo.GetWaitlistedByOfferingAsync(courseOfferingId, ct);
        var next = waitlisted.FirstOrDefault();
        if (next is null)
            return;

        next.PromoteFromWaitlist();
        _enrollmentRepo.Update(next);
        await _enrollmentRepo.SaveChangesAsync(ct);

        await _audit.LogAsync(new AuditLog(
            "PromoteWaitlistedEnrollment", "Enrollment", next.Id.ToString(),
            actorUserId: actorStudentProfileId,
            newValuesJson: $"{{\"waitlistedStudentProfileId\":\"{next.StudentProfileId:D}\",\"courseOfferingId\":\"{courseOfferingId:D}\"}}"), ct);
    }

    private async Task<decimal> ResolvePrerequisitePassThresholdPercentageAsync(Guid studentProfileId, CancellationToken ct)
    {
        var student = await _resultRepo.GetStudentProfileAsync(studentProfileId, ct);
        var institutionType = student?.Department?.InstitutionType ?? InstitutionType.University;

        var profile = await _gradingProfileRepo.GetByTypeAsync(institutionType, ct);
        var threshold = profile?.PassThreshold ?? DefaultThresholdFor(institutionType);

        // University thresholds are configured on GPA scale (0-4); prerequisite checks compare percentages.
        if (institutionType == InstitutionType.University && threshold <= 4.0m)
            return threshold * 25m;

        return threshold;
    }

    private static decimal DefaultThresholdFor(InstitutionType institutionType)
        => institutionType == InstitutionType.University ? 2.0m : 40m;
}
