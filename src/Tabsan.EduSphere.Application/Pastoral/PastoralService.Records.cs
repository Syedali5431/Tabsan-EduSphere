using Tabsan.EduSphere.Domain.Pastoral;

namespace Tabsan.EduSphere.Application.Pastoral;

// Notes about students: writing them (for one student or a whole group at once), changing, removing and following them
// up, and a student's pastoral page.
public sealed partial class PastoralService
{
    private static bool TryParse<T>(string? value, T fallback, out T result) where T : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value)) { result = fallback; return true; }
        return Enum.TryParse(value.Trim(), true, out result) && Enum.IsDefined(result) && !int.TryParse(value, out _);
    }

    private static string? ParseRecord(SaveRecordRequest req, out PastoralKind kind, out PastoralTone tone, out PastoralSeverity severity)
    {
        tone = default;
        severity = default;
        if (!TryParse(req.Kind, PastoralKind.General, out kind)) return $"Unknown kind of note \"{req.Kind}\".";
        if (!TryParse(req.Tone, PastoralTone.Neutral, out tone)) return $"Unknown tone \"{req.Tone}\".";
        if (!TryParse(req.Severity, PastoralSeverity.Low, out severity)) return $"Unknown seriousness \"{req.Severity}\".";
        return null;
    }

    private static PastoralVisibility VisibilityOf(bool confidential) => confidential ? PastoralVisibility.Confidential : PastoralVisibility.Staff;

    /// <summary>Who may follow up notes about a student: the wellbeing team for them, their teachers and the person
    /// writing (those who see confidential notes are marked).</summary>
    private async Task<List<PersonOptionDto>> FollowUpOptionsAsync(PastoralStudent s, PastoralViewer v, PastoralAccess a, CancellationToken ct)
    {
        var team = (await _store.TeamAsync(s.TenantId, ct)).Where(t => t.Member.TenantId == s.TenantId && InScope(t.Member.CampusId, t.Member.SemesterId, s)).ToList();
        var teachers = await _store.NamesAsync(s.TeacherUserIds.ToList(), ct);
        var people = new Dictionary<Guid, PersonOptionDto>();
        foreach (var t in team)
        {
            var seen = people.GetValueOrDefault(t.Member.UserId);
            people[t.Member.UserId] = new PersonOptionDto(t.Member.UserId, t.Name, seen?.Role ?? t.Member.Title, (seen?.SeesConfidential ?? false) || t.Member.SeesConfidential);
        }
        foreach (var (id, name) in teachers) people.TryAdd(id, new PersonOptionDto(id, name, "Teacher"));
        var me = await _store.NamesAsync([v.UserId], ct);
        if (!people.ContainsKey(v.UserId))
            people[v.UserId] = new PersonOptionDto(v.UserId, me.GetValueOrDefault(v.UserId) ?? v.Member.Name, "You", a.SeesConfidential);
        return people.Values.OrderBy(p => p.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    /// <summary>Null when the person may follow it up; else why not.</summary>
    private static string? CheckFollowUp(Guid? userId, PastoralVisibility visibility, Guid authorId, IReadOnlyList<PersonOptionDto> options, PastoralStudent s)
    {
        if (userId is not { } id) return null;
        var option = options.FirstOrDefault(o => o.UserId == id);
        if (option is null) return $"Choose someone who works with {s.Name} to follow the note up.";
        if (visibility == PastoralVisibility.Confidential && id != authorId && !option.SeesConfidential)
            return "A confidential note can only be followed up by someone who sees confidential notes.";
        return null;
    }

    public async Task<PastoralResult<SavedRecordsDto>> CreateRecordsAsync(PastoralViewer v, SaveRecordRequest req, DateTime now, CancellationToken ct = default)
    {
        if (!v.IsStaff) return PastoralResult<SavedRecordsDto>.Forbidden("Only staff write pastoral notes.");
        var ids = (req.StudentProfileIds ?? []).Where(id => id != Guid.Empty).Distinct().ToList();
        if (ids.Count == 0) return PastoralResult<SavedRecordsDto>.Invalid("Choose the student the note is about.");
        if (ids.Count > MaxStudentsPerNote) return PastoralResult<SavedRecordsDto>.Invalid($"A note can be written for at most {MaxStudentsPerNote} students at once.");
        if (ParseRecord(req, out var kind, out var tone, out var severity) is { } bad) return PastoralResult<SavedRecordsDto>.Invalid(bad);
        var visibility = VisibilityOf(req.Confidential);

        var found = (await _store.StudentsAsync(ids, ct)).ToDictionary(s => s.StudentProfileId);
        var skipped = new List<string>();
        var allowed = new List<(PastoralStudent Student, PastoralAccess Access)>();
        foreach (var id in ids)
        {
            if (!found.TryGetValue(id, out var s)) { skipped.Add("A student was not found."); continue; }
            var a = AccessTo(v, s);
            if (!a.IsStaff) { skipped.Add($"{s.Name}: you do not teach or look after this student."); continue; }
            allowed.Add((s, a));
        }
        if (allowed.Count == 0)
            return found.Count == 0
                ? PastoralResult<SavedRecordsDto>.NotFound("Student not found.")
                : PastoralResult<SavedRecordsDto>.Forbidden(ids.Count == 1 ? "You do not teach or look after this student." : "You do not teach or look after these students.");

        var records = new List<(PastoralRecord Record, PastoralStudent Student)>();
        try
        {
            foreach (var (s, a) in allowed)
            {
                if (req.FollowUpUserId is not null
                    && CheckFollowUp(req.FollowUpUserId, visibility, v.UserId, await FollowUpOptionsAsync(s, v, a, ct), s) is { } why)
                    return PastoralResult<SavedRecordsDto>.Invalid(why);
                var offering = req.CourseOfferingId is { } o && s.Offerings.Contains(o) ? o : (Guid?)null;
                var record = new PastoralRecord(s.TenantId, s.CampusId, s.StudentProfileId, offering, v.UserId);
                record.Describe(kind, tone, severity, req.Category, req.Summary, req.Details, req.ActionTaken, req.OccurredAt ?? now, now);
                record.SetSharing(visibility, req.ShareWithStudent, req.ShareWithParents);
                if (req.FollowUpUserId is { } person) record.SetFollowUp(person, req.FollowUpBy);
                records.Add((record, s));
            }
        }
        catch (ArgumentException ex) { return PastoralResult<SavedRecordsDto>.Invalid(ex.Plain()); }

        foreach (var (record, _) in records) _store.Add(record);
        await _store.SaveChangesAsync(ct);

        var authorName = (await _store.NamesAsync([v.UserId], ct)).GetValueOrDefault(v.UserId) ?? v.Member.Name;
        foreach (var (record, s) in records)
        {
            await AuditAsync("PastoralNoteWritten", "PastoralRecord", record.Id, v, ct);
            await AfterSaveAsync(record, s, authorName, v, isNew: true, assignedBefore: null, highBefore: false, now, ct);
        }
        await _store.SaveChangesAsync(ct);
        return PastoralResult<SavedRecordsDto>.Success(new SavedRecordsDto(records.Count, records.Select(r => r.Record.Id).ToList(), skipped), 201);
    }

    /// <summary>Tells who needs to know after a note is written or changed: the student and parents it is newly shared
    /// with, the person newly asked to follow it up, and the wellbeing team about a (newly) serious concern or a run of
    /// concerns.</summary>
    private async Task AfterSaveAsync(PastoralRecord r, PastoralStudent s, string authorName, PastoralViewer v, bool isNew, Guid? assignedBefore,
        bool highBefore, DateTime now, CancellationToken ct)
    {
        var label = r.Category ?? Short(r.Summary, 80);
        if (r.IsShared && r.SharedNotifiedAt is null)
        {
            var title = r.Tone == PastoralTone.Positive ? "Good news from school" : "A note from school";
            if (r.SharedWithStudent) await NotifyAsync(title, $"{authorName}: {label}", [s.UserId], v.UserId, ct);
            if (r.SharedWithParents) await NotifyAsync(title, $"About {s.Name}, from {authorName}: {label}", s.ParentUserIds, v.UserId, ct);
            r.MarkSharedNotified(now);
        }
        if (r.FollowUp == FollowUpStatus.Open && r.FollowUpUserId is { } person && person != assignedBefore)
            await NotifyAsync("A note for you to follow up", $"{authorName} asked you to follow up a note about {Who(s)}: {Short(r.Summary, 80)}", [person], v.UserId, ct);

        if (r.Tone != PastoralTone.Concern || r.IsRemoved) return;
        var confidential = r.Visibility == PastoralVisibility.Confidential;
        if (r.Severity == PastoralSeverity.High && !highBefore)
            await NotifyAsync("Serious concern", $"{authorName} wrote about a serious {KindWord(r.Kind)} concern about {Who(s)}. Open their pastoral page to read it.",
                await TeamForAsync(s, confidential, ct), v.UserId, ct);
        if (isNew && r.Kind is PastoralKind.Behaviour or PastoralKind.Wellbeing && s.TenantId is { } tenant)
        {
            // A run of concerns: told once, when the count reaches the school's number.
            var settings = await _store.SettingsAsync(tenant, ct);
            var count = settings?.ConcernAlertCount ?? PastoralSettings.DefaultConcernCount;
            var days = settings?.ConcernAlertDays ?? PastoralSettings.DefaultConcernDays;
            if (await _store.ConcernCountAsync(s.StudentProfileId, now.AddDays(-days), ct) == count)
                await NotifyAsync("A run of concerns", $"{Who(s)} has had {count} behaviour or wellbeing concerns in the last {days} days.",
                    await TeamForAsync(s, false, ct), null, ct);
        }
    }

    private static string KindWord(PastoralKind kind) => kind switch
    {
        PastoralKind.Behaviour => "behaviour",
        PastoralKind.Wellbeing => "wellbeing",
        PastoralKind.LearningSupport => "learning",
        PastoralKind.FamilyContact => "family",
        _ => "general"
    };

    private async Task<(PastoralRecord? Record, PastoralStudent? Student, PastoralAccess Access)> LoadRecordAsync(PastoralViewer v, Guid id, CancellationToken ct)
    {
        var r = await _store.RecordAsync(id, ct);
        if (r is null) return (null, null, PastoralAccess.None);
        var s = (await _store.StudentsAsync([r.StudentProfileId], ct)).FirstOrDefault();
        if (s is null) return (null, null, PastoralAccess.None);
        var a = AccessTo(v, s);
        return CanSee(r, a, v.UserId) && (a.IsStaff || r.AuthorUserId == v.UserId) ? (r, s, a) : (null, null, PastoralAccess.None);
    }

    public async Task<PastoralResult<PastoralRecordDto>> UpdateRecordAsync(PastoralViewer v, Guid id, SaveRecordRequest req, DateTime now, CancellationToken ct = default)
    {
        var (r, s, a) = await LoadRecordAsync(v, id, ct);
        if (r is null || s is null) return PastoralResult<PastoralRecordDto>.NotFound("Note not found.");
        if (!CanManage(r, a, v.UserId)) return PastoralResult<PastoralRecordDto>.Forbidden("Only the author, the wellbeing team and admins change a note.");
        if (r.IsRemoved) return PastoralResult<PastoralRecordDto>.Invalid("The note is removed.");
        if (ParseRecord(req, out var kind, out var tone, out var severity) is { } bad) return PastoralResult<PastoralRecordDto>.Invalid(bad);
        var visibility = VisibilityOf(req.Confidential);
        if (req.FollowUpUserId is { } assignee)
        {
            var options = await FollowUpOptionsAsync(s, v, a, ct);
            if (assignee != r.FollowUpUserId && CheckFollowUp(assignee, visibility, r.AuthorUserId, options, s) is { } why)
                return PastoralResult<PastoralRecordDto>.Invalid(why);
            // The person already following it up must also see it once it is confidential.
            if (assignee == r.FollowUpUserId && visibility == PastoralVisibility.Confidential && assignee != r.AuthorUserId
                && options.FirstOrDefault(o => o.UserId == assignee) is not { SeesConfidential: true })
                return PastoralResult<PastoralRecordDto>.Invalid("A confidential note can only be followed up by someone who sees confidential notes.");
        }

        var assignedBefore = r.FollowUp == FollowUpStatus.Open ? r.FollowUpUserId : null;
        var highBefore = r.Tone == PastoralTone.Concern && r.Severity == PastoralSeverity.High;
        try
        {
            r.Describe(kind, tone, severity, req.Category, req.Summary, req.Details, req.ActionTaken, req.OccurredAt ?? r.OccurredAt, now);
            r.SetSharing(visibility, req.ShareWithStudent, req.ShareWithParents);
            r.SetFollowUp(req.FollowUpUserId, req.FollowUpBy);
        }
        catch (ArgumentException ex) { return PastoralResult<PastoralRecordDto>.Invalid(ex.Plain()); }
        await _store.SaveChangesAsync(ct);
        await AuditAsync("PastoralNoteChanged", "PastoralRecord", r.Id, v, ct);
        var authorName = (await _store.NamesAsync([v.UserId], ct)).GetValueOrDefault(v.UserId) ?? v.Member.Name;
        await AfterSaveAsync(r, s, authorName, v, isNew: false, assignedBefore, highBefore, now, ct);
        await _store.SaveChangesAsync(ct);
        return PastoralResult<PastoralRecordDto>.Success((await RecordDtosAsync([r], new Dictionary<Guid, PastoralStudent> { [s.StudentProfileId] = s }, v, ct)).Single());
    }

    public async Task<PastoralResult<PastoralRecordDto>> AddNoteAsync(PastoralViewer v, Guid id, string? text, DateTime now, CancellationToken ct = default)
    {
        var (r, s, a) = await LoadRecordAsync(v, id, ct);
        if (r is null || s is null) return PastoralResult<PastoralRecordDto>.NotFound("Note not found.");
        if (!CanFollowUp(r, a, v.UserId)) return PastoralResult<PastoralRecordDto>.Forbidden("Only the people following the note up add to it.");
        PastoralNote note;
        try { note = new PastoralNote(r.Id, v.UserId, text, now); }
        catch (ArgumentException ex) { return PastoralResult<PastoralRecordDto>.Invalid(ex.Plain()); }
        _store.Add(note);
        r.Touch();
        await _store.SaveChangesAsync(ct);
        var name = (await _store.NamesAsync([v.UserId], ct)).GetValueOrDefault(v.UserId) ?? v.Member.Name;
        await NotifyAsync("Follow-up note", $"{name} added to a note about {Who(s)}: {Short(note.Text, 80)}",
            new[] { r.AuthorUserId, r.FollowUpUserId ?? Guid.Empty }.Where(g => g != Guid.Empty), v.UserId, ct);
        return PastoralResult<PastoralRecordDto>.Success((await RecordDtosAsync([r], new Dictionary<Guid, PastoralStudent> { [s.StudentProfileId] = s }, v, ct)).Single());
    }

    /// <param name="done">True finishes the follow-up, false opens it again.</param>
    public async Task<PastoralResult<PastoralRecordDto>> SetFollowUpDoneAsync(PastoralViewer v, Guid id, bool done, DateTime now, CancellationToken ct = default)
    {
        var (r, s, a) = await LoadRecordAsync(v, id, ct);
        if (r is null || s is null) return PastoralResult<PastoralRecordDto>.NotFound("Note not found.");
        if (!CanFollowUp(r, a, v.UserId)) return PastoralResult<PastoralRecordDto>.Forbidden("Only the people following the note up change it.");
        try
        {
            if (done) r.CompleteFollowUp(v.UserId, now);
            else r.ReopenFollowUp();
        }
        catch (InvalidOperationException ex) { return PastoralResult<PastoralRecordDto>.Invalid(ex.Message); }
        await _store.SaveChangesAsync(ct);
        return PastoralResult<PastoralRecordDto>.Success((await RecordDtosAsync([r], new Dictionary<Guid, PastoralStudent> { [s.StudentProfileId] = s }, v, ct)).Single());
    }

    public async Task<PastoralResult<PastoralRecordDto>> RemoveRecordAsync(PastoralViewer v, Guid id, string? reason, DateTime now, CancellationToken ct = default)
    {
        var (r, s, a) = await LoadRecordAsync(v, id, ct);
        if (r is null || s is null) return PastoralResult<PastoralRecordDto>.NotFound("Note not found.");
        if (!CanManage(r, a, v.UserId)) return PastoralResult<PastoralRecordDto>.Forbidden("Only the author, the wellbeing team and admins remove a note.");
        try { r.Remove(v.UserId, reason, now); }
        catch (ArgumentException ex) { return PastoralResult<PastoralRecordDto>.Invalid(ex.Plain()); }
        catch (InvalidOperationException ex) { return PastoralResult<PastoralRecordDto>.Invalid(ex.Message); }
        await _store.SaveChangesAsync(ct);
        await AuditAsync("PastoralNoteRemoved", "PastoralRecord", r.Id, v, ct);
        return PastoralResult<PastoralRecordDto>.Success((await RecordDtosAsync([r], new Dictionary<Guid, PastoralStudent> { [s.StudentProfileId] = s }, v, ct)).Single());
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Pages
    // ═════════════════════════════════════════════════════════════════════════

    private async Task<List<PastoralRecordDto>> RecordDtosAsync(IReadOnlyList<PastoralRecord> records, IReadOnlyDictionary<Guid, PastoralStudent> students,
        PastoralViewer v, CancellationToken ct)
    {
        if (records.Count == 0) return [];
        var notes = await _store.NotesAsync(records.Select(r => r.Id).ToList(), ct);
        var people = records.SelectMany(r => new[] { r.AuthorUserId, r.FollowUpUserId ?? Guid.Empty }).Concat(notes.Select(n => n.AuthorUserId))
            .Where(g => g != Guid.Empty).Distinct().ToList();
        var names = await _store.NamesAsync(people, ct);
        var places = await _store.PlaceNamesAsync(records.Where(r => r.CourseOfferingId != null).Select(r => r.CourseOfferingId!.Value).Distinct().ToList(), ct);
        return records.Select(r =>
        {
            var s = students[r.StudentProfileId];
            var a = AccessTo(v, s);
            return new PastoralRecordDto(r.Id, r.StudentProfileId, s.Name, r.Kind.ToString(), r.Tone.ToString(), r.Severity.ToString(), r.Category, r.Summary,
                r.Details, r.ActionTaken, r.OccurredAt, r.CourseOfferingId is { } o ? places.GetValueOrDefault(o) : null, r.Visibility.ToString(),
                r.SharedWithStudent, r.SharedWithParents, names.GetValueOrDefault(r.AuthorUserId) ?? "", r.CreatedAt, r.FollowUp.ToString(), r.FollowUpUserId,
                r.FollowUpUserId is { } f ? names.GetValueOrDefault(f) : null, r.FollowUpBy, r.FollowUpDoneAt, r.IsRemoved, r.RemovedReason,
                CanManage(r, a, v.UserId) && !r.IsRemoved, CanFollowUp(r, a, v.UserId),
                notes.Where(n => n.RecordId == r.Id).OrderBy(n => n.CreatedAt)
                    .Select(n => new PastoralNoteDto(n.Id, names.GetValueOrDefault(n.AuthorUserId) ?? "", n.Text, n.CreatedAt)).ToList());
        }).ToList();
    }

    private static SharedRecordDto Shared(PastoralRecord r, IReadOnlyDictionary<Guid, string> names)
        => new(r.Id, r.Kind.ToString(), r.Tone.ToString(), r.Category, r.Summary, r.Details, r.ActionTaken, r.OccurredAt, names.GetValueOrDefault(r.AuthorUserId) ?? "");

    private static StudentOptionDto Option(PastoralStudent s) => new(s.StudentProfileId, s.Name, s.RegistrationNumber, s.ClassName);

    private async Task<IReadOnlyList<CategoryDto>> CategoriesForAsync(Guid? tenantId, bool activeOnly, CancellationToken ct)
    {
        var stored = tenantId is { } t ? await _store.CategoriesAsync(t, ct) : [];
        if (stored.Count == 0)
            return PastoralCategory.Defaults.Select(d => new CategoryDto(Guid.Empty, d.Kind.ToString(), d.Tone.ToString(), d.Name, true)).ToList();
        return stored.Where(c => !activeOnly || c.IsActive).OrderBy(c => c.Kind).ThenBy(c => c.Position)
            .Select(c => new CategoryDto(c.Id, c.Kind.ToString(), c.Tone.ToString(), c.Name, c.IsActive)).ToList();
    }

    /// <summary>A student's pastoral page for staff who work with them.</summary>
    public async Task<PastoralResult<StudentPastoralDto>> StudentAsync(PastoralViewer v, Guid studentProfileId, DateTime now, CancellationToken ct = default)
    {
        var s = (await _store.StudentsAsync([studentProfileId], ct)).FirstOrDefault();
        if (s is null) return PastoralResult<StudentPastoralDto>.NotFound("Student not found.");
        var a = AccessTo(v, s);
        if (!a.IsStaff) return PastoralResult<StudentPastoralDto>.Forbidden("You do not teach or look after this student.");

        var students = new Dictionary<Guid, PastoralStudent> { [s.StudentProfileId] = s };
        var records = (await _store.RecordsOfAsync(s.StudentProfileId, ct)).Where(r => CanSee(r, a, v.UserId))
            .OrderByDescending(r => r.OccurredAt).ThenByDescending(r => r.CreatedAt).ToList();
        var plans = (await _store.PlansOfAsync(s.StudentProfileId, ct)).Where(p => CanSee(p, a, v.UserId))
            .OrderBy(p => p.Status).ThenByDescending(p => p.StartsOn).ToList();
        var checkIns = a.Manages ? await CheckInDtosAsync(await _store.CheckInsOfAsync(s.StudentProfileId, 30, ct), students, ct) : null;

        var since = now.AddDays(-CountDays);
        var recent = records.Where(r => !r.IsRemoved && r.OccurredAt >= since).ToList();
        var counts = new PastoralCountsDto(recent.Count(r => r.Tone == PastoralTone.Positive), recent.Count(r => r.Tone == PastoralTone.Concern),
            recent.Count(r => r.Kind == PastoralKind.Wellbeing), recent.Count(r => r.Tone == PastoralTone.Neutral && r.Kind != PastoralKind.Wellbeing),
            records.Count(r => !r.IsRemoved && r.FollowUp == FollowUpStatus.Open), plans.Count(p => p.Status == SupportPlanStatus.Active));

        var ownerNames = await _store.NamesAsync(plans.Select(p => p.OwnerUserId).Distinct().ToList(), ct);
        var campus = s.CampusId is { } c ? (await _store.PlaceNamesAsync([c], ct)).GetValueOrDefault(c) : null;
        await AuditAsync("PastoralStudentViewed", "StudentProfile", s.StudentProfileId, v, ct);
        return PastoralResult<StudentPastoralDto>.Success(new StudentPastoralDto(Option(s), campus, counts, await RecordDtosAsync(records, students, v, ct),
            plans.Select(p => PlanDto(p, a, v, ownerNames)).ToList(), checkIns, true, a.Manages, a.SeesConfidential,
            await FollowUpOptionsAsync(s, v, a, ct), await CategoriesForAsync(s.TenantId, true, ct)));
    }

    /// <summary>Students the person may write notes about: by name or number, or the students of one class (a teacher
    /// without either: everyone they teach).</summary>
    public async Task<PastoralResult<IReadOnlyList<StudentOptionDto>>> FindStudentsAsync(PastoralViewer v, string? search, Guid? courseOfferingId,
        CancellationToken ct = default)
    {
        if (!v.IsStaff) return PastoralResult<IReadOnlyList<StudentOptionDto>>.Forbidden("Only staff write pastoral notes.");
        var q = search?.Trim();
        IReadOnlyCollection<Guid>? offerings = courseOfferingId is { } o ? [o] : null;
        var teacherOnly = !v.IsManager && v.Team.Count == 0;
        if (offerings is null && teacherOnly) offerings = v.Member.TeachingOfferings.ToList();
        if (offerings is null && (q is null || q.Length < 2)) return PastoralResult<IReadOnlyList<StudentOptionDto>>.Success([]);
        if (offerings is { Count: 0 }) return PastoralResult<IReadOnlyList<StudentOptionDto>>.Success([]);
        var found = await _store.FindStudentsAsync(v.TenantId, offerings, q is { Length: >= 2 } ? q : null, 400, ct);
        return PastoralResult<IReadOnlyList<StudentOptionDto>>.Success(found.Where(s => AccessTo(v, s).IsStaff)
            .OrderBy(s => s.ClassName, StringComparer.CurrentCultureIgnoreCase).ThenBy(s => s.Name, StringComparer.CurrentCultureIgnoreCase)
            .Take(200).Select(Option).ToList());
    }

    /// <summary>The school's categories for notes (its starting list until it changes them).</summary>
    public async Task<PastoralResult<IReadOnlyList<CategoryDto>>> CategoriesAsync(PastoralViewer v, CancellationToken ct = default)
        => v.IsStaff
            ? PastoralResult<IReadOnlyList<CategoryDto>>.Success(await CategoriesForAsync(v.TenantId, true, ct))
            : PastoralResult<IReadOnlyList<CategoryDto>>.Forbidden("This page is for staff.");

    /// <summary>The classes, year levels and campuses a member of staff can choose from.</summary>
    public async Task<PastoralResult<FilterOptionsDto>> FilterOptionsAsync(PastoralViewer v, CancellationToken ct = default)
        => v.IsStaff ? PastoralResult<FilterOptionsDto>.Success(await FiltersAsync(v, ct)) : PastoralResult<FilterOptionsDto>.Forbidden("This page is for staff.");

    private Task<FilterOptionsDto> FiltersAsync(PastoralViewer v, CancellationToken ct)
    {
        if (v.IsSuperAdmin) return _store.FilterOptionsAsync(v.TenantId, null, null, ct);
        if (v.IsAdmin) return _store.FilterOptionsAsync(v.TenantId, v.Member.AdminCampuses, null, ct);
        if (v.Team.Count > 0)
        {
            IReadOnlySet<Guid>? campuses = v.Team.Any(t => t.CampusId is null) ? null : v.Team.Select(t => t.CampusId!.Value).ToHashSet();
            return _store.FilterOptionsAsync(v.TenantId, campuses, null, ct);
        }
        return _store.FilterOptionsAsync(v.TenantId, null, v.Member.TeachingOfferings, ct);
    }
}
