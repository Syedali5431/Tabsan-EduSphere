using Tabsan.EduSphere.Application.DTOs.Lms;

namespace Tabsan.EduSphere.Application.Interfaces;

// Final-Touches Phase 20 Stage 20.3 — discussion forum service contract
// Phase 31 Stage 31.3 — Extended with solve/resolution and visibility management

/// <summary>Service for managing course discussion threads and replies.</summary>
public interface IDiscussionService
{
    Task<List<DiscussionThreadDto>> GetThreadsAsync(Guid offeringId, CancellationToken ct = default);
    Task<DiscussionThreadDto?> GetThreadAsync(Guid threadId, CancellationToken ct = default);
    Task<DiscussionThreadDto> CreateThreadAsync(CreateThreadRequest request, CancellationToken ct = default);
    Task SetPinnedAsync(Guid threadId, bool pinned, CancellationToken ct = default);
    Task CloseThreadAsync(Guid threadId, CancellationToken ct = default);
    Task ReopenThreadAsync(Guid threadId, CancellationToken ct = default);
    Task DeleteThreadAsync(Guid threadId, CancellationToken ct = default);

    // Phase 31 Stage 31.3 — New Methods
    Task MarkSolvedAsync(Guid threadId, Guid facultyAdminUserId, CancellationToken ct = default);
    Task MarkUnresolvedAsync(Guid threadId, CancellationToken ct = default);
    Task MarkVisibleToAllAsync(Guid threadId, CancellationToken ct = default);
    Task MarkPrivateAsync(Guid threadId, CancellationToken ct = default);

    Task<DiscussionReplyDto> AddReplyAsync(AddReplyRequest request, bool isFacultyOrAdmin = false, CancellationToken ct = default);
    Task DeleteReplyAsync(Guid replyId, Guid requesterId, bool isFaculty, CancellationToken ct = default);
}
