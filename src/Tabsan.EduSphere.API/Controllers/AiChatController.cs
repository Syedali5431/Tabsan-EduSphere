using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tabsan.EduSphere.Application.DTOs.AiChat;
using Tabsan.EduSphere.Application.Interfaces;

namespace Tabsan.EduSphere.API.Controllers;

/// <summary>
/// Provides AI chat assistant endpoints for all authenticated users.
/// Access is blocked automatically when the AI Chatbot module is inactive.
/// </summary>
[ApiController]
[Route("api/ai")]
[Route("api/v1/ai")]
[Authorize]
public sealed class AiChatController : ControllerBase
{
    private readonly IAiChatService _chatService;

    /// <summary>Initialises the controller with the AI chat service.</summary>
    public AiChatController(IAiChatService chatService) => _chatService = chatService;

    /// <summary>
    /// Sends a message to the AI assistant; creates a new conversation if none is specified.
    /// Returns 503 when the AI module is inactive or the LLM provider is unavailable.
    /// </summary>
    [HttpPost("message")]
    public async Task<IActionResult> SendMessage(
        [FromBody] SendMessageRequest request,
        CancellationToken ct)
    {
        var userId       = GetCurrentUserId();
        var userRole     = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        var departmentId = GetDepartmentId();

        var result = await _chatService.SendMessageAsync(userId, userRole, departmentId, request, ct);
        if (result is null)
            return StatusCode(503, "The AI Chatbot module is currently inactive.");

        return Ok(result);
    }

    /// <summary>Returns a list of past conversations for the current user.</summary>
    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations(CancellationToken ct)
        => Ok(await _chatService.GetConversationsAsync(GetCurrentUserId(), ct));

    /// <summary>Returns the full message history of a specific conversation.</summary>
    [HttpGet("conversations/{conversationId:guid}")]
    public async Task<IActionResult> GetConversation(Guid conversationId, CancellationToken ct)
    {
        var result = await _chatService.GetConversationAsync(conversationId, ct);
        return result is null ? NotFound() : Ok(result);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Extracts the authenticated user ID from the NameIdentifier JWT claim.</summary>
    private Guid GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(value, out var id) ? id : Guid.Empty;
    }

    /// <summary>Extracts the optional department ID from the "departmentId" JWT claim.</summary>
    private Guid? GetDepartmentId()
    {
        var value = User.FindFirstValue("departmentId");
        return Guid.TryParse(value, out var id) ? id : null;
    }
}
