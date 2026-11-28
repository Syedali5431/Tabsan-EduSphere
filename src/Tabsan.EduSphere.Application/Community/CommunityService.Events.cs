using Tabsan.EduSphere.Domain.Community;

namespace Tabsan.EduSphere.Application.Community;

// Events: sports days, excursions, parents' evenings and holidays for an audience, with replies (going, not going,
// maybe; guests; places) and an optional form such as a consent form. The audience is told when an event is published
// and again if it is cancelled.

public sealed partial class CommunityService
{
    /// <summary>Published and cancelled events in the period that the person's audience includes, with their reply.</summary>
    public async Task<IReadOnlyList<EventDto>> EventsAsync(CommunityMember m, DateTime from, DateTime to, DateTime now, CancellationToken ct = default)
    {
        var events = (await _store.EventsAsync(m.TenantId, from, to, ct))
            .Where(e => e.Status != EventStatus.Draft && Sees(e, m)).OrderBy(e => e.StartsAt).ToList();
        return await EventDtosAsync(events, m, now, ct);
    }

    public async Task<CommunityResult<EventDto>> EventAsync(CommunityMember m, Guid id, DateTime now, CancellationToken ct = default)
    {
        var e = await _store.EventAsync(id, ct);
        if (e is null || !(CanManage(e, m) || (e.Status != EventStatus.Draft && Sees(e, m)))) return CommunityResult<EventDto>.NotFound("Event not found.");
        return CommunityResult<EventDto>.Success((await EventDtosAsync([e], m, now, ct))[0]);
    }

    /// <summary>Events the person manages, from two months back, soonest first.</summary>
    public async Task<CommunityResult<IReadOnlyList<EventDto>>> ManageEventsAsync(CommunityMember m, DateTime now, CancellationToken ct = default)
    {
        if (!CanWrite(m)) return CommunityResult<IReadOnlyList<EventDto>>.Forbidden("Only teachers and admins write for the school.");
        var events = (await _store.EventsAsync(m.TenantId, now.AddDays(-60), now.AddYears(2), ct)).Where(e => CanManage(e, m)).OrderBy(e => e.StartsAt).ToList();
        return CommunityResult<IReadOnlyList<EventDto>>.Success(await EventDtosAsync(events, m, now, ct));
    }

    public async Task<CommunityResult<EventDto>> SaveEventAsync(CommunityMember m, Guid? id, SaveEventRequest r, DateTime now, CancellationToken ct = default)
    {
        if (!CanWrite(m)) return CommunityResult<EventDto>.Forbidden("Only teachers and admins write for the school.");
        SchoolEvent? e = null;
        if (id is { } existing)
        {
            e = await _store.EventAsync(existing, ct);
            if (e is null) return CommunityResult<EventDto>.NotFound("Event not found.");
            if (!CanManage(e, m)) return CommunityResult<EventDto>.Forbidden("You cannot change this event.");
            if (e.Status == EventStatus.Cancelled) return CommunityResult<EventDto>.Conflict("The event is cancelled.");
        }
        if (!Enum.TryParse<EventCategory>(r.Category ?? nameof(EventCategory.General), true, out var category) || !Enum.IsDefined(category))
            return CommunityResult<EventDto>.Invalid("Choose a kind of event.");
        if (r.FormId is { } formId && (await _store.FormAsync(formId, ct) is not { } form || !CanManage(form, m)))
            return CommunityResult<EventDto>.Invalid("Choose one of your forms.");
        try
        {
            if (e is null) e = new SchoolEvent(m.UserId, r.Title ?? "", r.StartsAt, r.EndsAt, r.AllDay);
            else e.SetTimes(r.StartsAt, r.EndsAt, r.AllDay);
            e.Edit(r.Title ?? "", r.Description, r.Location, category);
            e.ConfigureReplies(r.RepliesOpen, r.ReplyBy, r.Capacity, r.MaxGuests, r.FormId);
            if (await ApplyAudienceAsync(e, m, r.Audience, ct) is { } error) return CommunityResult<EventDto>.Invalid(error);
        }
        catch (ArgumentException ex) { return CommunityResult<EventDto>.Invalid(ex.Plain()); }
        if (r.Publish) e.Publish();
        if (id is null) _store.Add(e);
        await _store.SaveChangesAsync(ct);
        if (e.Status == EventStatus.Published && e.NotifiedAt is null)
        {
            e.MarkNotified(now);
            await _store.SaveChangesAsync(ct);
            await NotifyAsync(e, e.Title, EventLine(e), null, ct);
        }
        await AuditAsync(id is null ? "SchoolEventCreated" : "SchoolEventUpdated", "SchoolEvent", e.Id, m, ct);
        return CommunityResult<EventDto>.Success((await EventDtosAsync([e], m, now, ct))[0], id is null ? 201 : 200);
    }

    /// <summary>Cancels an event; if it was published, its audience is told.</summary>
    public async Task<CommunityResult<EventDto>> CancelEventAsync(CommunityMember m, Guid id, string? reason, DateTime now, CancellationToken ct = default)
    {
        var e = await _store.EventAsync(id, ct);
        if (e is null) return CommunityResult<EventDto>.NotFound("Event not found.");
        if (!CanManage(e, m)) return CommunityResult<EventDto>.Forbidden("You cannot change this event.");
        if (e.Status == EventStatus.Cancelled) return CommunityResult<EventDto>.Conflict("The event is already cancelled.");
        var wasPublished = e.Status == EventStatus.Published;
        try { e.Cancel(reason); }
        catch (ArgumentException ex) { return CommunityResult<EventDto>.Invalid(ex.Plain()); }
        await _store.SaveChangesAsync(ct);
        if (wasPublished) await NotifyAsync(e, $"Cancelled: {e.Title}", e.CancelReason ?? EventLine(e), null, ct);
        await AuditAsync("SchoolEventCancelled", "SchoolEvent", e.Id, m, ct);
        return CommunityResult<EventDto>.Success((await EventDtosAsync([e], m, now, ct))[0]);
    }

    /// <summary>Going, not going or maybe (with guests and a note), changed as often as needed until replies close.</summary>
    public async Task<CommunityResult<EventDto>> ReplyAsync(CommunityMember m, Guid id, EventReplyRequest r, DateTime now, CancellationToken ct = default)
    {
        var e = await _store.EventAsync(id, ct);
        if (e is null || e.Status == EventStatus.Draft || !InAudience(e, m)) return CommunityResult<EventDto>.NotFound("Event not found.");
        if (!e.TakesReplies(now)) return CommunityResult<EventDto>.Conflict(e.Status == EventStatus.Cancelled ? "The event is cancelled." : "Replies are closed.");
        if (!Enum.TryParse<RsvpAnswer>(r.Answer, true, out var answer) || !Enum.IsDefined(answer)) return CommunityResult<EventDto>.Invalid("Choose going, not going or maybe.");
        if (r.Guests < 0 || r.Guests > e.MaxGuests)
            return CommunityResult<EventDto>.Invalid(e.MaxGuests == 0 ? "This event takes no guests." : $"Bring at most {e.MaxGuests} guests.");

        var replies = await _store.RepliesAsync([id], ct);
        var mine = replies.FirstOrDefault(x => x.UserId == m.UserId);
        var places = answer == RsvpAnswer.Going ? 1 + r.Guests : 0;
        var others = replies.Where(x => x.UserId != m.UserId).Sum(x => x.Places);
        if (e.Capacity is { } capacity && places > 0 && others + places > capacity && places > (mine?.Places ?? 0))
            return CommunityResult<EventDto>.Conflict(others >= capacity ? "The event is full." : capacity - others == 1 ? "Only 1 place is left." : $"Only {capacity - others} places are left.");
        if (mine is null) _store.Add(mine = new EventReply(id, m.UserId));
        mine.Set(answer, r.Guests, r.Note);
        await _store.SaveChangesAsync(ct);
        return CommunityResult<EventDto>.Success((await EventDtosAsync([e], m, now, ct))[0]);
    }

    /// <summary>Published events for the person's calendar (a parent's: those for the chosen child, or for any child).</summary>
    public async Task<IReadOnlyList<SchoolEvent>> CalendarEventsAsync(CommunityMember m, Guid? childId, DateTime from, DateTime to, CancellationToken ct = default)
        => (await _store.EventsAsync(m.TenantId, from, to, ct))
            .Where(e => e.Status == EventStatus.Published && InAudience(e, m)
                        && (childId is null || !m.IsParent || m.Children.Any(c => c.StudentProfileId == childId && Reaches(e, c))))
            .ToList();

    /// <summary>Everyone's replies, for those who manage the event.</summary>
    public async Task<CommunityResult<EventRepliesDto>> EventRepliesAsync(CommunityMember m, Guid id, DateTime now, CancellationToken ct = default)
    {
        var e = await _store.EventAsync(id, ct);
        if (e is null) return CommunityResult<EventRepliesDto>.NotFound("Event not found.");
        if (!CanManage(e, m)) return CommunityResult<EventRepliesDto>.Forbidden("You cannot see this event's replies.");
        var replies = await _store.RepliesAsync([id], ct);
        var people = await _store.PeopleAsync(e, ct);
        var names = await _store.NamesAsync(replies.Select(x => x.UserId).ToList(), ct);
        var roles = people.Students.Select(s => (s.UserId, Role: "Student"))
            .Concat(people.Parents.Select(p => (UserId: p.ParentUserId, Role: "Parent")))
            .Concat(people.Staff.Select(s => (s.UserId, Role: "Staff")))
            .GroupBy(x => x.UserId).ToDictionary(g => g.Key, g => g.First().Role);
        var rows = replies.OrderBy(x => x.Answer).ThenBy(x => names.GetValueOrDefault(x.UserId))
            .Select(x => new EventReplyRowDto(x.UserId, names.GetValueOrDefault(x.UserId) ?? "", roles.GetValueOrDefault(x.UserId) ?? "Staff",
                x.Answer.ToString(), x.Guests, x.Note, x.UpdatedAt ?? x.CreatedAt)).ToList();
        return CommunityResult<EventRepliesDto>.Success(new EventRepliesDto((await EventDtosAsync([e], m, now, ct))[0], rows, people.UserIds(e.Roles).Count));
    }

    private async Task<List<EventDto>> EventDtosAsync(List<SchoolEvent> events, CommunityMember m, DateTime now, CancellationToken ct)
    {
        if (events.Count == 0) return [];
        var replies = await _store.RepliesAsync(events.Select(e => e.Id).ToList(), ct);
        var names = await _store.AudienceNamesAsync(events, ct);
        var authors = await _store.NamesAsync(events.Select(e => e.AuthorUserId).Distinct().ToList(), ct);
        return events.Select(e =>
        {
            var own = replies.Where(x => x.EventId == e.Id).ToList();
            var mine = own.FirstOrDefault(x => x.UserId == m.UserId);
            var manage = CanManage(e, m);
            EventReplyCounts? counts = e.RepliesOpen
                ? new(own.Count(x => x.Answer == RsvpAnswer.Going), own.Count(x => x.Answer == RsvpAnswer.NotGoing), own.Count(x => x.Answer == RsvpAnswer.Maybe), own.Sum(x => x.Places))
                : null;
            return new EventDto(e.Id, e.Title, e.Description, e.Location, e.StartsAt, e.EndsAt, e.AllDay, e.Category.ToString(), e.Status.ToString(),
                e.CancelReason, e.RepliesOpen, e.RepliesOpen ? e.RepliesCloseAt : null, e.Capacity, e.MaxGuests, e.FormId, mine?.Answer.ToString(),
                mine?.Guests ?? 0, mine?.Note, counts, e.TakesReplies(now) && InAudience(e, m), manage, Describe(e, names), authors.GetValueOrDefault(e.AuthorUserId) ?? "");
        }).ToList();
    }

    /// <summary>"Fri 5 Dec 2026, 09:00 UTC · Main hall" for notifications (the app and portal show local times).</summary>
    private static string EventLine(SchoolEvent e)
        => (e.AllDay ? e.StartsAt.ToString("ddd d MMM yyyy") : e.StartsAt.ToString("ddd d MMM yyyy, HH:mm") + " UTC") + (e.Location is null ? "" : " · " + e.Location);
}
