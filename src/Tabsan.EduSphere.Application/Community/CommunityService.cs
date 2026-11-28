using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Tabsan.EduSphere.Application.Interfaces;
using Tabsan.EduSphere.Domain.Auditing;
using Tabsan.EduSphere.Domain.Community;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Domain.Notifications;

namespace Tabsan.EduSphere.Application.Community;

/// <summary>
/// School community: news, events and online forms for an audience (the whole school or a campus, a year level or one class;
/// students, parents and/or staff). Admins write for their school (or their campus), teachers for their own classes.
/// <para>Who sees an item: students in its classes, parents through any of their children, staff of its campus or the
/// teachers of its classes; admins and SuperAdmins see everything of their school. The audience is told when an item goes
/// out (in the app, by push and by e-mail where switched on).</para>
/// </summary>
public sealed partial class CommunityService
{
    private readonly ICommunityStore _store;
    private readonly INotificationService? _notifications;
    private readonly IAuditService? _audit;
    private readonly ILogger<CommunityService> _logger;

    public CommunityService(ICommunityStore store, INotificationService? notifications = null, IAuditService? audit = null,
        ILogger<CommunityService>? logger = null)
    {
        _store = store;
        _notifications = notifications;
        _audit = audit;
        _logger = logger ?? NullLogger<CommunityService>.Instance;
    }

    public Task<CommunityMember?> MemberAsync(Guid userId, string role, CancellationToken ct = default) => _store.MemberAsync(userId, role, ct);

    // ═════════════════════════════════════════════════════════════════════════
    // Who sees and who manages
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>Whether the item's audience includes the person (admins and SuperAdmins: everything of their school).</summary>
    public static bool Sees(CommunityItem item, CommunityMember m)
    {
        if (m.IsSuperAdmin) return m.TenantId is null || item.TenantId is null || item.TenantId == m.TenantId;
        if (item.TenantId is { } tenant && tenant != m.TenantId) return false;
        if (m.IsAdmin) return m.AdminCampuses is null || item.CampusId is null || m.AdminCampuses.Contains(item.CampusId.Value);
        if ((item.Roles & m.Group) == AudienceRoles.None) return false;
        if (m.IsParent) return m.Children.Any(c => Reaches(item, c));
        return Reaches(item, m.Campuses, m.Semesters, m.Offerings);
    }

    /// <summary>Whether the person is someone the item is for (not only someone who may look at it): admins and SuperAdmins
    /// only for staff items of the whole school or a campus. Read marks, replies and form answers come from the audience.</summary>
    public static bool InAudience(CommunityItem item, CommunityMember m)
        => Sees(item, m) && (!m.IsManager || (item.Roles.HasFlag(AudienceRoles.Staff) && item.SemesterId is null && item.CourseOfferingId is null));

    /// <summary>Whether the item's campus, year level and class include a child.</summary>
    public static bool Reaches(CommunityItem item, CommunityChild child) => Reaches(item, child.Campuses, child.Semesters, child.Offerings);

    private static bool Reaches(CommunityItem item, IReadOnlySet<Guid> campuses, IReadOnlySet<Guid> semesters, IReadOnlySet<Guid> offerings)
        => (item.CampusId is null || campuses.Contains(item.CampusId.Value))
           && (item.SemesterId is null || semesters.Contains(item.SemesterId.Value))
           && (item.CourseOfferingId is null || offerings.Contains(item.CourseOfferingId.Value));

    /// <summary>SuperAdmins everything (of their school when limited to one); admins their school's items (their campus's,
    /// for an admin of one campus); teachers what they wrote.</summary>
    public static bool CanManage(CommunityItem item, CommunityMember m)
    {
        if (m.IsSuperAdmin) return m.TenantId is null || item.TenantId == m.TenantId;
        if (m.IsAdmin)
            return item.TenantId == m.TenantId
                   && (m.AdminCampuses is null || item.AuthorUserId == m.UserId || (item.CampusId is { } c && m.AdminCampuses.Contains(c)));
        return m.IsFaculty && item.AuthorUserId == m.UserId;
    }

    public static bool CanWrite(CommunityMember m) => m.IsManager || (m.IsFaculty && m.TeachingOfferings.Count > 0);

    public async Task<CommunityResult<AudienceOptionsDto>> AudienceOptionsAsync(CommunityMember m, CancellationToken ct = default)
        => CanWrite(m)
            ? CommunityResult<AudienceOptionsDto>.Success(await _store.AudienceOptionsAsync(m, ct))
            : CommunityResult<AudienceOptionsDto>.Forbidden("Only teachers and admins write for the school.");

    /// <summary>Checks and sets an item's audience: the campus, year level and class must be the author's school's (and
    /// campus's, for an admin of one campus); teachers write to one of their own classes.</summary>
    private async Task<string?> ApplyAudienceAsync(CommunityItem item, CommunityMember m, AudienceRequest? a, CancellationToken ct)
    {
        a ??= new AudienceRequest(null, null, null, null);
        var roles = AudienceRoles.None;
        foreach (var r in a.Roles ?? [])
        {
            if (!Enum.TryParse<AudienceRoles>(r, true, out var one) || one is AudienceRoles.None or AudienceRoles.Everyone || !Enum.IsDefined(one))
                return $"Unknown audience \"{r}\".";
            roles |= one;
        }
        if (roles == AudienceRoles.None) roles = AudienceRoles.Everyone;

        AudienceTarget? campus = null, semester = null, offering = null;
        if (a.CampusId is { } campusId && (campus = await _store.CampusAsync(campusId, ct)) is null) return "Campus not found.";
        if (a.SemesterId is { } semesterId && (semester = await _store.SemesterAsync(semesterId, ct)) is null) return "Year level not found.";
        if (a.CourseOfferingId is { } offeringId && (offering = await _store.OfferingAsync(offeringId, ct)) is null) return "Class not found.";

        if (m.TenantId is { } tenant && (campus is not null && campus.TenantId != tenant || offering is not null && offering.TenantId != tenant))
            return "That campus or class is not part of your school.";
        if (campus is not null && offering is not null && offering.CampusId != campus.Id) return "That class is not on the chosen campus.";
        if (semester is not null && offering is not null && offering.SemesterId != semester.Id) return "That class is not in the chosen year level.";
        if (m.IsFaculty && !m.IsManager && (offering is null || !m.TeachingOfferings.Contains(offering.Id)))
            return "Teachers write to their own classes: choose one of your classes.";
        if (m.IsAdmin && m.AdminCampuses is { } mine)
        {
            // An admin of some campuses writes for one of them.
            var at = campus?.Id ?? offering?.CampusId;
            if (at is { } chosen && !mine.Contains(chosen)) return "You write for your own campus.";
            if (at is null && mine.Count != 1) return "Choose one of your campuses.";
            campus ??= offering is null ? new AudienceTarget(mine.First(), m.TenantId, mine.First(), null) : null;
        }

        var tenantId = m.TenantId ?? campus?.TenantId ?? offering?.TenantId;
        item.SetAudience(tenantId, campus?.Id, semester?.Id, offering?.Id, roles);
        return null;
    }

    private static AudienceDto Describe(CommunityItem i, AudienceNames names)
        => new(i.CampusId, i.CampusId is { } c ? names.Campuses.GetValueOrDefault(c) : null,
            i.SemesterId, i.SemesterId is { } s ? names.Semesters.GetValueOrDefault(s) : null,
            i.CourseOfferingId, i.CourseOfferingId is { } o ? names.Classes.GetValueOrDefault(o) : null,
            Enum.GetValues<AudienceRoles>().Where(r => r is not (AudienceRoles.None or AudienceRoles.Everyone) && i.Roles.HasFlag(r)).Select(r => r.ToString()).ToList());

    /// <summary>Tells the item's audience (everyone but the author), once.</summary>
    private async Task<int> NotifyAsync(CommunityItem item, string title, string body, IReadOnlyList<Guid>? only, CancellationToken ct)
    {
        if (_notifications is null) return 0;
        var ids = (only ?? (await _store.PeopleAsync(item, ct)).UserIds(item.Roles)).Where(id => id != item.AuthorUserId).Distinct().ToList();
        if (ids.Count == 0) return 0;
        try { await _notifications.SendSystemAsync(title, body, NotificationType.SchoolNews, ids, ct); }
        catch (Exception ex) when (ex is not OperationCanceledException) { _logger.LogWarning(ex, "School community notification failed."); }
        return ids.Count;
    }

    private Task AuditAsync(string action, string entity, Guid id, CommunityMember m, CancellationToken ct)
        => _audit is null ? Task.CompletedTask
            : _audit.LogAsync(new AuditLog(action, entity, id.ToString(), actorUserId: m.UserId, actorRole: m.Role), ct);

    private static string Short(string? text, int max)
    {
        var t = (text ?? "").Replace('\n', ' ').Trim();
        return t.Length <= max ? t : t[..(max - 1)].TrimEnd() + "…";
    }

    /// <summary>Run every minute by the API: tells the audience of scheduled news whose time has come, and reminds people a
    /// day before a form is due. Returns how many items were handled.</summary>
    public async Task<int> NotifyDueAsync(DateTime now, CancellationToken ct = default)
    {
        var handled = 0;
        foreach (var post in await _store.NewsToAnnounceAsync(now, ct))
        {
            await AnnounceAsync(post, now, ct);
            handled++;
        }
        foreach (var form in (await _store.FormsAsync(null, ct)).Where(f => f.Status == FormStatus.Open && f.DueAt is { } due && due > now
                     && due - now <= TimeSpan.FromDays(1) && (f.RemindedAt is null || now - f.RemindedAt > TimeSpan.FromDays(1))))
        {
            var tracking = await TrackAsync(form, ct);
            await RemindAsync(form, tracking, now, ct);
            handled++;
        }
        return handled;
    }
}
