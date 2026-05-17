using Tabsan.EduSphere.Domain.Lms;

namespace Tabsan.EduSphere.Domain.Interfaces;

// Final-Touches Phase 20 Stage 20.3 — repository contract for discussion forums
// Phase 31 Stage 31.3 — Extended with ticket generation support

/// <summary>Repository for discussion threads and replies.</summary>
public interface IDiscussionRepository
{
    // ── Threads ─────────────────────────────────────────────────────────────────

    /// <summary>Returns all threads for an offering, pinned first then newest first.</summary>
    Task<IReadOnlyList<DiscussionThread>> GetThreadsByOfferingAsync(Guid offeringId, CancellationToken ct = default);

    /// <summary>Returns a single thread with its replies.</summary>
    Task<DiscussionThread?> GetThreadByIdAsync(Guid threadId, CancellationToken ct = default);

    /// <summary>Counts threads created by a user (by username) for ticket number generation.</summary>
    Task<int> CountThreadsByAuthorUsernameAsync(string authorUsername, CancellationToken ct = default);

    Task AddThreadAsync(DiscussionThread thread, CancellationToken ct = default);
    void UpdateThread(DiscussionThread thread);
    void DeleteThread(DiscussionThread thread);

    // ── Replies ─────────────────────────────────────────────────────────────────

    Task<DiscussionReply?> GetReplyByIdAsync(Guid replyId, CancellationToken ct = default);
    Task AddReplyAsync(DiscussionReply reply, CancellationToken ct = default);
    void UpdateReply(DiscussionReply reply);
    void DeleteReply(DiscussionReply reply);

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
