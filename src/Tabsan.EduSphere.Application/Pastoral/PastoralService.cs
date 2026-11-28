using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Auditing;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Domain.Notifications;
using Tabsan.EduSphere.Domain.Pastoral;

namespace Tabsan.EduSphere.Application.Pastoral;

/// <summary>
/// Pastoral care and wellbeing: notes about students (praise, behaviour and wellbeing concerns, learning support, contact
/// with the family) with follow-ups, support plans that tell staff what a student needs, students' own wellbeing
/// check-ins, and the school's wellbeing team.
/// <para>Who sees what: a student's teachers, the wellbeing team (for their campus or year level) and the school's admins
/// see staff notes and plans; confidential ones only their author and the team members allowed to see confidential notes.
/// Students and parents see what is shared with them. Check-ins are seen by the wellbeing team and admins.</para>
/// <para>Alerts: a serious concern, a run of concerns (the school's setting) and a check-in asking to talk tell the
/// wellbeing team for the student at once (the school's admins when the team has nobody for them).</para>
/// </summary>
public sealed partial class PastoralService
{
    /// <summary>How far back a student's page counts notes.</summary>
    public const int CountDays = 90;
    public const int MaxStudentsPerNote = 60;

    private readonly IPastoralStore _store;
    private readonly INotificationService? _notifications;
    private readonly IAuditService? _audit;
    private readonly ILogger<PastoralService> _logger;

    public PastoralService(IPastoralStore store, INotificationService? notifications = null, IAuditService? audit = null, ILogger<PastoralService>? logger = null)
    {
        _store = store;
        _notifications = notifications;
        _audit = audit;
        _logger = logger ?? NullLogger<PastoralService>.Instance;
    }

    public Task<PastoralViewer?> ViewerAsync(Guid userId, string role, CancellationToken ct = default) => _store.ViewerAsync(userId, role, ct);

    // ═════════════════════════════════════════════════════════════════════════
    // Who sees and who changes
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>Whether a place on the team (or a campus and year level) covers the student.</summary>
    public static bool InScope(Guid? campusId, Guid? semesterId, PastoralStudent s)
        => (campusId is null || s.Campuses.Contains(campusId.Value)) && (semesterId is null || s.Semesters.Contains(semesterId.Value));

    public static PastoralAccess AccessTo(PastoralViewer v, PastoralStudent s)
    {
        var self = v.IsStudent && v.StudentProfileId == s.StudentProfileId;
        var parent = v.IsParent && v.Member.Children.Any(c => c.StudentProfileId == s.StudentProfileId);
        var teacher = v.IsFaculty && s.TeacherUserIds.Contains(v.UserId);
        var scopes = v.IsStaff ? v.Team.Where(t => s.TenantId == t.TenantId && InScope(t.CampusId, t.SemesterId, s)).ToList() : [];
        var admin = v.IsAdmin && v.TenantId is not null && s.TenantId == v.TenantId
                    && (v.Member.AdminCampuses is null || s.Campuses.Any(v.Member.AdminCampuses.Contains));
        var superAdmin = v.IsSuperAdmin && (v.TenantId is null || s.TenantId == v.TenantId);
        return new PastoralAccess(self, parent, teacher, scopes.Count > 0, scopes.Any(t => t.SeesConfidential), admin, superAdmin);
    }

    /// <summary>Confidential notes: their author, the person following them up and team members who see confidential notes.
    /// Removed notes: the wellbeing team, admins and the author. Students and parents: what is shared with them.</summary>
    public static bool CanSee(PastoralRecord r, PastoralAccess a, Guid viewerId)
    {
        var involved = r.AuthorUserId == viewerId || r.FollowUpUserId == viewerId;
        if (r.Visibility == PastoralVisibility.Confidential) return involved || a.SeesConfidential;
        if (r.IsRemoved) return a.Manages || r.AuthorUserId == viewerId;
        if (a.IsStaff || involved) return true;
        if (a.IsSelf) return r.SharedWithStudent;
        if (a.IsParent) return r.SharedWithParents;
        return false;
    }

    /// <summary>The author, and the wellbeing team and admins (for confidential notes only team members who see them).</summary>
    public static bool CanManage(PastoralRecord r, PastoralAccess a, Guid viewerId)
        => r.AuthorUserId == viewerId || (a.Manages && (r.Visibility == PastoralVisibility.Staff || a.SeesConfidential));

    public static bool CanFollowUp(PastoralRecord r, PastoralAccess a, Guid viewerId)
        => !r.IsRemoved && (CanManage(r, a, viewerId) || r.FollowUpUserId == viewerId);

    public static bool CanSee(SupportPlan p, PastoralAccess a, Guid viewerId)
    {
        if (p.Visibility == PastoralVisibility.Confidential) return p.OwnerUserId == viewerId || a.SeesConfidential;
        if (a.IsStaff || p.OwnerUserId == viewerId) return true;
        if (p.Status != SupportPlanStatus.Active) return false;
        if (a.IsSelf) return p.SharedWithStudent;
        if (a.IsParent) return p.SharedWithParents;
        return false;
    }

    public static bool CanManage(SupportPlan p, PastoralAccess a, Guid viewerId)
        => p.OwnerUserId == viewerId || (a.Manages && (p.Visibility == PastoralVisibility.Staff || a.SeesConfidential));

    public async Task<PastoralMeDto> MeAsync(PastoralViewer v, CancellationToken ct = default)
    {
        var checkIns = await CheckInsEnabledAsync(v.TenantId, ct);
        var team = v.IsStaff && v.Team.Count > 0;
        return new PastoralMeDto(v.Member.Role, v.IsStaff, team, team && v.Team.Any(t => t.SeesConfidential), v.IsStaff && (team || v.IsManager), v.IsManager,
            checkIns);
    }

    private async Task<bool> CheckInsEnabledAsync(Guid? tenantId, CancellationToken ct)
        => tenantId is { } t && (await _store.SettingsAsync(t, ct))?.CheckInsEnabled == true;

    // ═════════════════════════════════════════════════════════════════════════
    // Alerts
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>The wellbeing team for a student (only those who see confidential notes, when asked); the school's admins
    /// when nobody on the team covers the student.</summary>
    private async Task<List<Guid>> TeamForAsync(PastoralStudent s, bool confidential, CancellationToken ct)
    {
        var team = await _store.TeamAsync(s.TenantId, ct);
        var ids = team.Where(t => t.Member.TenantId == s.TenantId && InScope(t.Member.CampusId, t.Member.SemesterId, s) && (!confidential || t.Member.SeesConfidential))
            .Select(t => t.Member.UserId).Distinct().ToList();
        if (ids.Count == 0 && !confidential) ids = (await _store.AdminsAsync(s.TenantId, s.Campuses, ct)).ToList();
        return ids;
    }

    private async Task<int> NotifyAsync(string title, string body, IEnumerable<Guid> userIds, Guid? except, CancellationToken ct)
    {
        if (_notifications is null) return 0;
        var ids = userIds.Where(id => id != except).Distinct().ToList();
        if (ids.Count == 0) return 0;
        try { await _notifications.SendSystemAsync(title, body, NotificationType.Pastoral, ids, ct); }
        catch (Exception ex) when (ex is not OperationCanceledException) { _logger.LogWarning(ex, "Pastoral notification failed."); }
        return ids.Count;
    }

    private Task AuditAsync(string action, string entity, Guid id, PastoralViewer v, CancellationToken ct)
        => _audit is null ? Task.CompletedTask
            : _audit.LogAsync(new AuditLog(action, entity, id.ToString(), actorUserId: v.UserId, actorRole: v.Member.Role), ct);

    private static string Short(string? text, int max)
    {
        var t = (text ?? "").Replace('\n', ' ').Trim();
        return t.Length <= max ? t : t[..(max - 1)].TrimEnd() + "…";
    }

    private static string Who(PastoralStudent s) => s.ClassName is { } c ? $"{s.Name} ({c})" : s.Name;

    /// <summary>Run every minute by the API: reminds owners of support plans whose review date has come, and the people
    /// following notes up whose date has passed. Returns how many were reminded.</summary>
    public async Task<int> RemindDueAsync(DateTime now, CancellationToken ct = default)
    {
        var plans = await _store.PlansToReviewAsync(now, ct);
        var records = await _store.FollowUpsDueAsync(now, ct);
        if (plans.Count == 0 && records.Count == 0) return 0;
        var students = (await _store.StudentsAsync(plans.Select(p => p.StudentProfileId).Concat(records.Select(r => r.StudentProfileId)).Distinct().ToList(), ct))
            .ToDictionary(s => s.StudentProfileId);
        foreach (var plan in plans)
        {
            var who = students.TryGetValue(plan.StudentProfileId, out var s) ? Who(s) : "a student";
            await NotifyAsync("Support plan to review", $"It is time to review \"{Short(plan.Title, 80)}\" for {who}.", [plan.OwnerUserId], null, ct);
            plan.MarkReviewReminded(now);
        }
        foreach (var record in records)
        {
            var who = students.TryGetValue(record.StudentProfileId, out var s) ? Who(s) : "a student";
            if (record.FollowUpUserId is { } person)
                await NotifyAsync("Follow-up due", $"A note about {who} is waiting for you to follow it up: {Short(record.Summary, 80)}", [person], null, ct);
            record.MarkFollowUpReminded(now);
        }
        await _store.SaveChangesAsync(ct);
        return plans.Count + records.Count;
    }
}
