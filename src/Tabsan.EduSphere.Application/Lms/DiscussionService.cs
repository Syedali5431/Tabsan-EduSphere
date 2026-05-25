using Tabsan.EduSphere.Application.DTOs.Lms;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Domain.Lms;

namespace Tabsan.EduSphere.Application.Lms;

// Final-Touches Phase 20 Stage 20.3 — discussion forum service implementation
// Phase 31 Stage 31.3 — Extended with type, ticket generation, solved tracking, and visibility

/// <summary>
/// Application service for Phase 20/31 — discussion threads and replies with advanced features.
/// Author names are resolved via IUserRepository using the AuthorId stored on each entity.
/// Phase 31: Supports thread types (Issue, FAQ), issue sub-classification, resolution tracking, ticket generation, and visibility controls.
/// </summary>
public sealed class DiscussionService : IDiscussionService
{
    private readonly IDiscussionRepository _repo;
    private readonly IUserRepository       _users;
    private readonly ICourseRepository     _courses;

    public DiscussionService(IDiscussionRepository repo, IUserRepository users, ICourseRepository courses)
    {
        _repo    = repo;
        _users   = users;
        _courses = courses;
    }

    public async Task<List<DiscussionThreadDto>> GetThreadsAsync(
        Guid offeringId, CancellationToken ct = default)
    {
        var threads = await _repo.GetThreadsByOfferingAsync(offeringId, ct);
        var dtos    = new List<DiscussionThreadDto>(threads.Count);
        foreach (var t in threads)
        {
            var author = await _users.GetByIdAsync(t.AuthorId, ct);
            var resolvedByName = t.ResolvedBy.HasValue ? (await _users.GetByIdAsync(t.ResolvedBy.Value, ct))?.Username ?? "Unknown" : null;
            dtos.Add(MapThread(t, author?.Username ?? "Unknown", [], resolvedByName));
        }
        return dtos;
    }

    public async Task<DiscussionThreadDto?> GetThreadAsync(Guid threadId, CancellationToken ct = default)
    {
        var thread = await _repo.GetThreadByIdAsync(threadId, ct);
        if (thread is null) return null;

        var author  = await _users.GetByIdAsync(thread.AuthorId, ct);
        var resolvedByName = thread.ResolvedBy.HasValue ? (await _users.GetByIdAsync(thread.ResolvedBy.Value, ct))?.Username ?? "Unknown" : null;
        var replies = new List<DiscussionReplyDto>(thread.Replies.Count);
        foreach (var r in thread.Replies.Where(r => !r.IsDeleted))
        {
            var replyAuthor = await _users.GetByIdAsync(r.AuthorId, ct);
            replies.Add(MapReply(r, replyAuthor?.Username ?? "Unknown"));
        }
        return MapThread(thread, author?.Username ?? "Unknown", replies, resolvedByName);
    }

    public async Task<DiscussionThreadDto> CreateThreadAsync(
        CreateThreadRequest request, CancellationToken ct = default)
    {
        // Validate offering exists
        var offering = await _courses.GetOfferingByIdAsync(request.OfferingId, ct);
        if (offering is null)
            throw new InvalidOperationException($"Course offering {request.OfferingId} not found.");

        // Get author username for ticket generation
        var author = await _users.GetByIdAsync(request.AuthorId, ct);
        var authorUsername = author?.Username ?? "unknown";

        // Generate unique ticket number
        var ticketNumber = await GenerateTicketNumberAsync(authorUsername, ct);

        var thread = new DiscussionThread(
            request.OfferingId, 
            request.AuthorId, 
            request.Title, 
            request.ThreadType,
            request.IssueSubType,
            ticketNumber);
            
        await _repo.AddThreadAsync(thread, ct);
        await _repo.SaveChangesAsync(ct);
        
        return MapThread(thread, authorUsername, [], null);
    }

    public async Task SetPinnedAsync(Guid threadId, bool pinned, CancellationToken ct = default)
    {
        var thread = await _repo.GetThreadByIdAsync(threadId, ct)
            ?? throw new InvalidOperationException($"Thread {threadId} not found.");
        thread.SetPinned(pinned);
        _repo.UpdateThread(thread);
        await _repo.SaveChangesAsync(ct);
    }

    public async Task CloseThreadAsync(Guid threadId, CancellationToken ct = default)
    {
        var thread = await _repo.GetThreadByIdAsync(threadId, ct)
            ?? throw new InvalidOperationException($"Thread {threadId} not found.");
        thread.Close();
        _repo.UpdateThread(thread);
        await _repo.SaveChangesAsync(ct);
    }

    public async Task ReopenThreadAsync(Guid threadId, CancellationToken ct = default)
    {
        var thread = await _repo.GetThreadByIdAsync(threadId, ct)
            ?? throw new InvalidOperationException($"Thread {threadId} not found.");
        thread.Reopen();
        _repo.UpdateThread(thread);
        await _repo.SaveChangesAsync(ct);
    }

    /// <summary>Faculty/Admin marks this discussion as solved, preventing further student replies.</summary>
    public async Task MarkSolvedAsync(Guid threadId, Guid facultyAdminUserId, CancellationToken ct = default)
    {
        var thread = await _repo.GetThreadByIdAsync(threadId, ct)
            ?? throw new InvalidOperationException($"Thread {threadId} not found.");
        thread.MarkSolved(facultyAdminUserId);
        _repo.UpdateThread(thread);
        await _repo.SaveChangesAsync(ct);
    }

    /// <summary>Faculty/Admin marks a solved discussion as unresolved, allowing replies again.</summary>
    public async Task MarkUnresolvedAsync(Guid threadId, CancellationToken ct = default)
    {
        var thread = await _repo.GetThreadByIdAsync(threadId, ct)
            ?? throw new InvalidOperationException($"Thread {threadId} not found.");
        thread.MarkUnresolved();
        _repo.UpdateThread(thread);
        await _repo.SaveChangesAsync(ct);
    }

    /// <summary>Faculty/Admin marks this discussion as visible to all department users (for FAQ or shared solutions).</summary>
    public async Task MarkVisibleToAllAsync(Guid threadId, CancellationToken ct = default)
    {
        var thread = await _repo.GetThreadByIdAsync(threadId, ct)
            ?? throw new InvalidOperationException($"Thread {threadId} not found.");
        thread.MarkVisibleToAll();
        _repo.UpdateThread(thread);
        await _repo.SaveChangesAsync(ct);
    }

    /// <summary>Faculty/Admin marks this discussion as private (visible only to involved parties).</summary>
    public async Task MarkPrivateAsync(Guid threadId, CancellationToken ct = default)
    {
        var thread = await _repo.GetThreadByIdAsync(threadId, ct)
            ?? throw new InvalidOperationException($"Thread {threadId} not found.");
        thread.MarkPrivate();
        _repo.UpdateThread(thread);
        await _repo.SaveChangesAsync(ct);
    }

    public async Task DeleteThreadAsync(Guid threadId, CancellationToken ct = default)
    {
        var thread = await _repo.GetThreadByIdAsync(threadId, ct)
            ?? throw new InvalidOperationException($"Thread {threadId} not found.");
        thread.SoftDelete();
        _repo.UpdateThread(thread);
        await _repo.SaveChangesAsync(ct);
    }

    public async Task<DiscussionReplyDto> AddReplyAsync(AddReplyRequest request, bool isFacultyOrAdmin = false, CancellationToken ct = default)
    {
        var thread = await _repo.GetThreadByIdAsync(request.ThreadId, ct);
        if (thread is null)
            throw new InvalidOperationException($"Discussion thread {request.ThreadId} not found.");
        
        // Solved threads stay open for the thread author and faculty/admin moderators.
        if (thread.IsSolved && request.AuthorId != thread.AuthorId && !isFacultyOrAdmin)
            throw new InvalidOperationException("This discussion has been marked as solved. No further replies are allowed.");

        var reply = new DiscussionReply(request.ThreadId, request.AuthorId, request.Body);
        await _repo.AddReplyAsync(reply, ct);
        await _repo.SaveChangesAsync(ct);
        var author = await _users.GetByIdAsync(reply.AuthorId, ct);
        return MapReply(reply, author?.Username ?? "Unknown");
    }

    public async Task DeleteReplyAsync(
        Guid replyId, Guid requesterId, bool isFaculty, CancellationToken ct = default)
    {
        var reply = await _repo.GetReplyByIdAsync(replyId, ct)
            ?? throw new InvalidOperationException($"Reply {replyId} not found.");
        if (!isFaculty && reply.AuthorId != requesterId)
            throw new UnauthorizedAccessException("Only the author or faculty can delete a reply.");
        reply.SoftDelete();
        _repo.UpdateReply(reply);
        await _repo.SaveChangesAsync(ct);
    }

    // ── Utility Methods ────────────────────────────────────────────────────────

    /// <summary>Generates a unique ticket number with format: DISCUSS-{username}-{sequential}.</summary>
    private async Task<string> GenerateTicketNumberAsync(string username, CancellationToken ct = default)
    {
        // Get count of discussions for this user to create sequential number
        var existingCount = await _repo.CountThreadsByAuthorUsernameAsync(username, ct);
        return $"DISCUSS-{username}-{existingCount + 1:D3}";
    }

    // ── Mappers ────────────────────────────────────────────────────────────────

    private static DiscussionThreadDto MapThread(
        DiscussionThread t, string authorName, List<DiscussionReplyDto> replies, string? resolvedByName = null) => new()
    {
        Id            = t.Id,
        OfferingId    = t.OfferingId,
        Title         = t.Title,
        AuthorId      = t.AuthorId,
        AuthorName    = authorName,
        IsPinned      = t.IsPinned,
        IsClosed      = t.IsClosed,
        CreatedAt     = t.CreatedAt,
        ReplyCount    = t.Replies.Count(r => !r.IsDeleted),
        ThreadType    = t.ThreadType,
        IssueSubType  = t.IssueSubType,
        IsSolved      = t.IsSolved,
        ResolvedBy    = t.ResolvedBy,
        ResolvedByName = resolvedByName,
        ResolvedAt    = t.ResolvedAt,
        TicketNumber  = t.TicketNumber,
        IsVisibleToAll = t.IsVisibleToAll,
        Replies       = replies
    };

    private static DiscussionReplyDto MapReply(DiscussionReply r, string authorName) => new()
    {
        Id         = r.Id,
        ThreadId   = r.ThreadId,
        AuthorId   = r.AuthorId,
        AuthorName = authorName,
        Body       = r.Body,
        CreatedAt  = r.CreatedAt,
        UpdatedAt  = r.UpdatedAt
    };
}
