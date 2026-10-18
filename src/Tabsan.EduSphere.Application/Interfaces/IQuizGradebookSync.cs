namespace Tabsan.EduSphere.Application.Interfaces;

/// <summary>
/// Plan M2: copies finished quiz scores into the gradebook. A student's quiz attempts in an
/// offering are rolled up into one "Quiz" result component (see <c>QuizGradebookCalculator</c>
/// for the rules) and written through <see cref="IResultService.SyncSystemComponentAsync"/>.
/// </summary>
public interface IQuizGradebookSync
{
    /// <summary>
    /// Recalculates the student's quiz component for the offering. Never throws (apart from
    /// cancellation): the quiz action that triggered it has already been saved, so a failure is
    /// logged and reported as null. Null also means there was nothing graded to post yet.
    /// </summary>
    Task<SystemResultSyncOutcome?> RefreshAsync(Guid courseOfferingId, Guid studentProfileId, CancellationToken ct = default);
}
