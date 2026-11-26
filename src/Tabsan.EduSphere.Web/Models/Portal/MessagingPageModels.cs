using Tabsan.EduSphere.Application.Messaging;

namespace Tabsan.EduSphere.Web.Models.Portal;

/// <summary>The inbox (or the archived conversations).</summary>
public sealed class MessagesPageModel
{
    public bool IsConnected { get; set; }
    public string? Message { get; set; }
    public bool Archived { get; set; }
    public List<ConversationSummaryDto> Conversations { get; set; } = [];
}

/// <summary>One conversation with its messages and the reply box.</summary>
public sealed class MessageThreadPageModel
{
    public bool IsConnected { get; set; }
    public string? Message { get; set; }
    public ConversationDto? Conversation { get; set; }
}

/// <summary>Writing a new message: the people the user may write to (narrowed by a search), subject and text.</summary>
public sealed class NewMessagePageModel
{
    public bool IsConnected { get; set; }
    public string? Message { get; set; }
    public string? Search { get; set; }
    public List<MessageContactDto> Contacts { get; set; } = [];
    public HashSet<Guid> Selected { get; set; } = [];
    public string? Subject { get; set; }
    public string? Body { get; set; }
}

/// <summary>How the portal names roles and contact groups in messages.</summary>
public static class MessageFormat
{
    /// <summary>The English text (a translation key) for a role.</summary>
    public static string RoleLabel(string? role) => role switch
    {
        "Faculty" => "Teacher",
        "Student" => "Student",
        "Parent" => "Parent",
        "Finance" => "Finance",
        "Admin" or "SuperAdmin" => "School office",
        _ => role ?? ""
    };

    /// <summary>The groups the API sends as words (others are course titles, shown as they are).</summary>
    public static bool IsKnownGroup(string? group) => group is "Teachers" or "Office" or "Students" or "Parents" or "Staff";
}
