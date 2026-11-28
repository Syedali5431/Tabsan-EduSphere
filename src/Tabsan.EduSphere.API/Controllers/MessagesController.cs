using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tabsan.EduSphere.Application.Messaging;

namespace Tabsan.EduSphere.API.Controllers;

/// <summary>
/// Messages between teachers, students, parents and the school office (/api/v1/messages). Everyone signed in has an inbox;
/// who they may write to follows their role (contacts). Conversations are visible only to the people in them.
/// </summary>
[ApiController]
[Route("api/v1/messages")]
[Authorize]
public sealed class MessagesController : ControllerBase
{
    private readonly MessagingService _messages;

    public MessagesController(MessagingService messages) => _messages = messages;

    /// <summary>The people the user may write to (?search= narrows by name, username, course or child).</summary>
    [HttpGet("contacts")]
    public async Task<IActionResult> Contacts([FromQuery] string? search, CancellationToken ct)
        => Ok(await _messages.ContactsAsync(UserId(), Role(), search, ct));

    /// <summary>The inbox, newest first (?archived=true for archived conversations; ?page= for older ones, 50 a page).</summary>
    [HttpGet("conversations")]
    public async Task<IActionResult> Inbox([FromQuery] bool archived, [FromQuery] int page, CancellationToken ct)
        => Ok(await _messages.InboxAsync(UserId(), archived, page, ct));

    /// <summary>How many conversations have messages the user has not read.</summary>
    [HttpGet("unread")]
    public async Task<IActionResult> Unread(CancellationToken ct) => Ok(await _messages.UnreadAsync(UserId(), ct));

    /// <summary>A conversation with its latest messages (marks it read).</summary>
    [HttpGet("conversations/{id:guid}")]
    public async Task<IActionResult> Open(Guid id, CancellationToken ct) => Answer(await _messages.OpenAsync(UserId(), id, DateTime.UtcNow, ct));

    /// <summary>A new message: each student or parent gets their own conversation; staff recipients share one.</summary>
    [HttpPost("conversations")]
    public async Task<IActionResult> Start([FromBody] StartConversationRequest request, CancellationToken ct)
        => Answer(await _messages.StartAsync(UserId(), Role(), request, DateTime.UtcNow, ct));

    [HttpPost("conversations/{id:guid}/messages")]
    public async Task<IActionResult> Reply(Guid id, [FromBody] ReplyRequest request, CancellationToken ct)
        => Answer(await _messages.ReplyAsync(UserId(), id, request.Body, DateTime.UtcNow, ct));

    [HttpPost("conversations/{id:guid}/archive")]
    public async Task<IActionResult> Archive(Guid id, CancellationToken ct) => Answer(await _messages.ArchiveAsync(UserId(), id, true, DateTime.UtcNow, ct));

    [HttpPost("conversations/{id:guid}/unarchive")]
    public async Task<IActionResult> Unarchive(Guid id, CancellationToken ct) => Answer(await _messages.ArchiveAsync(UserId(), id, false, DateTime.UtcNow, ct));

    private IActionResult Answer<T>(MessagingResult<T> result)
        => result.Ok ? Ok(result.Value) : StatusCode(result.Status, new { message = result.Error });

    private Guid UserId()
        => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub"), out var id) ? id : Guid.Empty;

    private string Role() => User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role") ?? string.Empty;
}
