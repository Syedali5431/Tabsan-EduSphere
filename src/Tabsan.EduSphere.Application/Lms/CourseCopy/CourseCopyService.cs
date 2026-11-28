using System.Text.Json;
using Tabsan.EduSphere.Domain.Auditing;
using Tabsan.EduSphere.Domain.Interfaces;
using Tabsan.EduSphere.Domain.Lms;
using P = Tabsan.EduSphere.Application.Lms.CourseCopyParts;

namespace Tabsan.EduSphere.Application.Lms;

/// <summary>
/// Course copy and templates. A teacher or admin copies the content of a course they look after (or of a template shared
/// with them) into another course they look after: last term's course into this term's, or a department template into a
/// new class. They choose what to copy, can move every date by a number of days (suggested from the two terms' start
/// dates), and copies arrive hidden from students unless they keep what was published.
/// <para>Who looks after a course: SuperAdmins every course (of their school when limited to one); admins the courses of
/// their departments; teachers the courses of their departments and those they teach. Templates are shared by admins with
/// the course's department or with the whole school. Copies stay within one school.</para>
/// </summary>
public sealed class CourseCopyService
{
    /// <summary>Dates move by at most ten years either way.</summary>
    public const int MaxShiftDays = 3660;
    public const int SourceLimit = 400;
    public const int HistoryLimit = 50;

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly ICourseCopyStore _store;
    private readonly IAuditService? _audit;

    public CourseCopyService(ICourseCopyStore store, IAuditService? audit = null)
    {
        _store = store;
        _audit = audit;
    }

    public static bool IsSuperAdmin(string? role) => string.Equals(role, "SuperAdmin", StringComparison.OrdinalIgnoreCase);
    public static bool IsAdmin(string? role) => string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);
    public static bool IsFaculty(string? role) => string.Equals(role, "Faculty", StringComparison.OrdinalIgnoreCase);

    // ═════════════════════════════════════════════════════════════════════════
    // Copying
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>The courses and templates the actor may copy into <paramref name="targetId"/>: templates first, then the
    /// same course's other terms, then the rest, newest term first.</summary>
    public async Task<CourseCopyResult<CourseCopySourcesDto>> SourcesAsync(CourseCopyActor actor, Guid targetId, CancellationToken ct = default)
    {
        var access = await AccessAsync(actor, ct);
        var target = await _store.OfferingAsync(targetId, ct);
        if (target is null) return CourseCopyResult<CourseCopySourcesDto>.NotFound();
        if (!access.CanManage(target)) return CourseCopyResult<CourseCopySourcesDto>.Forbidden("You cannot change this course.");

        var templates = (await _store.TemplatesAsync(target.TenantId, ct)).ToDictionary(t => t.Offering.OfferingId);
        var sources = new Dictionary<Guid, CourseCopySourceDto>();
        foreach (var o in await _store.OfferingsAsync(target.TenantId, access.Departments, IsFaculty(actor.Role) ? actor.UserId : null, SourceLimit, ct))
            if (o.OfferingId != targetId && access.CanManage(o))
                sources[o.OfferingId] = ToDto(o, templates.GetValueOrDefault(o.OfferingId).Template, actor.UserId);
        foreach (var (template, offering) in templates.Values)
            if (!sources.ContainsKey(offering.OfferingId) && access.CanCopyFrom(offering, template, target))
                sources[offering.OfferingId] = ToDto(offering, template, actor.UserId);

        var list = sources.Values
            .OrderByDescending(s => s.IsTemplate)
            .ThenByDescending(s => s.TaughtByYou)
            .ThenByDescending(s => s.CourseCode == target.CourseCode)
            .ThenByDescending(s => s.SemesterStart)
            .ThenBy(s => s.CourseCode, StringComparer.OrdinalIgnoreCase)
            .ToList();
        return CourseCopyResult<CourseCopySourcesDto>.Success(new CourseCopySourcesDto(ToDto(target, await _store.TemplateAsync(targetId, ct), actor.UserId), list));
    }

    /// <summary>What <paramref name="sourceId"/> holds, part by part, what cannot be copied, and the suggested date shift.</summary>
    public async Task<CourseCopyResult<CourseCopyPreviewDto>> PreviewAsync(CourseCopyActor actor, Guid targetId, Guid sourceId, CancellationToken ct = default)
    {
        var (source, target, template, error) = await ResolveAsync<CourseCopyPreviewDto>(actor, targetId, sourceId, ct);
        if (error is not null) return error;

        var content = await _store.ReadAsync(source!, ct);
        var runs = await _store.RunsAsync(targetId, HistoryLimit, ct);
        var notCopied = content.ScormPackages.Where(p => !p.IsRemoved).Select(p => new CourseCopyNoteDto(CourseCopyNotes.Scorm, p.Title)).ToList();
        var moduleIds = content.Modules.Select(m => m.Id).ToHashSet();
        notCopied.AddRange(content.Items.Where(i => i.ItemType == ModuleItemType.Discussion && moduleIds.Contains(i.ModuleId))
            .Select(i => new CourseCopyNoteDto(CourseCopyNotes.Discussion, i.Title)));

        var preview = new CourseCopyPreviewDto(
            ToDto(source!, template, actor.UserId),
            ToDto(target!, await _store.TemplateAsync(targetId, ct), actor.UserId),
            SuggestedShift(source!, target!),
            runs.FirstOrDefault(r => r.Run.SourceOfferingId == sourceId).Run?.CreatedAt,
            Describe(content, source!),
            notCopied);
        return CourseCopyResult<CourseCopyPreviewDto>.Success(preview);
    }

    public async Task<CourseCopyResult<CourseCopyResultDto>> CopyAsync(CourseCopyActor actor, Guid targetId, CourseCopyRequest request, DateTime now,
        CancellationToken ct = default)
    {
        if (request is null || request.SourceOfferingId == Guid.Empty) return CourseCopyResult<CourseCopyResultDto>.Invalid("Choose the course to copy from.");
        if (request.SourceOfferingId == targetId) return CourseCopyResult<CourseCopyResultDto>.Invalid("A course cannot be copied into itself.");
        var parts = (request.Parts ?? []).Select(p => (p ?? "").Trim().ToLowerInvariant()).Where(p => p.Length > 0).Distinct().ToList();
        if (parts.FirstOrDefault(p => !P.All.Contains(p)) is { } unknown) return CourseCopyResult<CourseCopyResultDto>.Invalid($"Unknown part \"{unknown}\".");
        if (parts.Count == 0) parts = [.. P.All];
        if (request.ShiftDays is < -MaxShiftDays or > MaxShiftDays)
            return CourseCopyResult<CourseCopyResultDto>.Invalid($"Dates can move by at most {MaxShiftDays} days.");

        var (source, target, _, error) = await ResolveAsync<CourseCopyResultDto>(actor, targetId, request.SourceOfferingId, ct);
        if (error is not null) return error;

        var content = await _store.ReadAsync(source!, ct);
        var itemIds = request.ItemIds is { Count: > 0 } ids ? ids.ToHashSet() : null;
        var plan = new CourseCopyPlan(source!, target!, parts.ToHashSet(), itemIds, request.ShiftDays, request.KeepPublished, actor.UserId);
        var outcome = CourseContentCloner.Clone(content, plan);
        if (outcome.Copied.Count == 0) return CourseCopyResult<CourseCopyResultDto>.Invalid("There is nothing to copy in what you chose.");

        var summary = JsonSerializer.Serialize(new RunSummary(outcome.Copied, outcome.NotCopied), Json);
        var run = new CourseCopyRun(source!.OfferingId, targetId, actor.UserId, request.ShiftDays, request.KeepPublished, summary, now);
        await _store.SaveCopyAsync(outcome.Copy, run, ct);
        if (_audit is not null)
            await _audit.LogAsync(new AuditLog("CourseContentCopied", "CourseOffering", targetId.ToString(), actorUserId: actor.UserId, actorRole: actor.Role,
                newValuesJson: JsonSerializer.Serialize(new { source = source.OfferingId, run = run.Id, request.ShiftDays, request.KeepPublished, copied = outcome.Copied }, Json)), ct);

        return CourseCopyResult<CourseCopyResultDto>.Success(
            new CourseCopyResultDto(run.Id, outcome.Copied, outcome.NotCopied, request.ShiftDays, request.KeepPublished), 201);
    }

    /// <summary>Copies made into the course, newest first.</summary>
    public async Task<CourseCopyResult<IReadOnlyList<CourseCopyRunDto>>> HistoryAsync(CourseCopyActor actor, Guid targetId, CancellationToken ct = default)
    {
        var access = await AccessAsync(actor, ct);
        var target = await _store.OfferingAsync(targetId, ct);
        if (target is null) return CourseCopyResult<IReadOnlyList<CourseCopyRunDto>>.NotFound();
        if (!access.CanManage(target)) return CourseCopyResult<IReadOnlyList<CourseCopyRunDto>>.Forbidden("You cannot change this course.");

        var list = new List<CourseCopyRunDto>();
        var offerings = new Dictionary<Guid, CourseCopyOffering?>();
        foreach (var (run, copiedBy) in await _store.RunsAsync(targetId, HistoryLimit, ct))
        {
            if (!offerings.TryGetValue(run.SourceOfferingId, out var source))
                offerings[run.SourceOfferingId] = source = await _store.OfferingAsync(run.SourceOfferingId, ct);
            var summary = ReadSummary(run.SummaryJson);
            list.Add(new CourseCopyRunDto(run.Id, run.SourceOfferingId,
                source is null ? "" : $"{source.CourseCode} — {source.CourseTitle}", source?.SemesterName ?? "",
                copiedBy, run.CreatedAt, run.DateShiftDays, run.KeptPublishedState, summary.Copied, summary.NotCopied.Sum(n => n.Count)));
        }
        return CourseCopyResult<IReadOnlyList<CourseCopyRunDto>>.Success(list);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Templates
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>The course's template settings, or null when it is not shared as a template.</summary>
    public async Task<CourseCopyResult<CourseTemplateDto?>> TemplateOfAsync(CourseCopyActor actor, Guid offeringId, CancellationToken ct = default)
    {
        var access = await AccessAsync(actor, ct);
        var offering = await _store.OfferingAsync(offeringId, ct);
        if (offering is null) return CourseCopyResult<CourseTemplateDto?>.NotFound();
        if (!access.CanManage(offering)) return CourseCopyResult<CourseTemplateDto?>.Forbidden("You cannot change this course.");
        var template = await _store.TemplateAsync(offeringId, ct);
        return CourseCopyResult<CourseTemplateDto?>.Success(template is null ? null : ToTemplateDto(template, offering));
    }

    /// <summary>Shares the course as a template (or changes its name, description and who may use it). Admins and SuperAdmins.</summary>
    public async Task<CourseCopyResult<CourseTemplateDto>> SaveTemplateAsync(CourseCopyActor actor, Guid offeringId, SaveCourseTemplateRequest request,
        CancellationToken ct = default)
    {
        if (!IsAdmin(actor.Role) && !IsSuperAdmin(actor.Role)) return CourseCopyResult<CourseTemplateDto>.Forbidden("Only admins share courses as templates.");
        if (!Enum.TryParse<CourseTemplateScope>(request?.Scope, true, out var scope) || !Enum.IsDefined(scope))
            return CourseCopyResult<CourseTemplateDto>.Invalid("Choose who may use the template.");
        var access = await AccessAsync(actor, ct);
        var offering = await _store.OfferingAsync(offeringId, ct);
        if (offering is null) return CourseCopyResult<CourseTemplateDto>.NotFound();
        if (!access.CanManage(offering)) return CourseCopyResult<CourseTemplateDto>.Forbidden("You cannot change this course.");

        var template = await _store.TemplateAsync(offeringId, ct);
        var created = template is null;
        try
        {
            if (template is null) _store.AddTemplate(template = new CourseTemplate(offeringId, request!.Name ?? "", request.Description, scope, actor.UserId));
            else template.Update(request!.Name ?? "", request.Description, scope);
        }
        catch (ArgumentException ex) { return CourseCopyResult<CourseTemplateDto>.Invalid(ex.Plain()); }
        await _store.SaveChangesAsync(ct);
        if (_audit is not null)
            await _audit.LogAsync(new AuditLog(created ? "CourseTemplateShared" : "CourseTemplateUpdated", "CourseOffering", offeringId.ToString(),
                actorUserId: actor.UserId, actorRole: actor.Role, newValuesJson: JsonSerializer.Serialize(new { template.Name, scope = template.Scope.ToString() }, Json)), ct);
        return CourseCopyResult<CourseTemplateDto>.Success(ToTemplateDto(template, offering), created ? 201 : 200);
    }

    /// <summary>Stops sharing the course as a template (its content and earlier copies are not touched).</summary>
    public async Task<CourseCopyResult<bool>> RemoveTemplateAsync(CourseCopyActor actor, Guid offeringId, CancellationToken ct = default)
    {
        if (!IsAdmin(actor.Role) && !IsSuperAdmin(actor.Role)) return CourseCopyResult<bool>.Forbidden("Only admins share courses as templates.");
        var access = await AccessAsync(actor, ct);
        var offering = await _store.OfferingAsync(offeringId, ct);
        if (offering is null) return CourseCopyResult<bool>.NotFound();
        if (!access.CanManage(offering)) return CourseCopyResult<bool>.Forbidden("You cannot change this course.");
        if (await _store.TemplateAsync(offeringId, ct) is not { } template) return CourseCopyResult<bool>.NotFound("The course is not shared as a template.");
        _store.RemoveTemplate(template);
        await _store.SaveChangesAsync(ct);
        if (_audit is not null)
            await _audit.LogAsync(new AuditLog("CourseTemplateRemoved", "CourseOffering", offeringId.ToString(), actorUserId: actor.UserId, actorRole: actor.Role), ct);
        return CourseCopyResult<bool>.Success(true);
    }

    // ═════════════════════════════════════════════════════════════════════════
    // Helpers
    // ═════════════════════════════════════════════════════════════════════════

    /// <summary>Days between the two terms' first days, so this term's week 3 lands on the new term's week 3.</summary>
    public static int SuggestedShift(CourseCopyOffering source, CourseCopyOffering target)
        => Math.Clamp((int)Math.Round((target.SemesterStart.Date - source.SemesterStart.Date).TotalDays), -MaxShiftDays, MaxShiftDays);

    /// <summary>What a copy of <paramref name="content"/> can offer, part by part (parts with nothing in them are left out).</summary>
    public static IReadOnlyList<CourseCopyPartDto> Describe(CourseContent content, CourseCopyOffering source)
    {
        var parts = new List<CourseCopyPartDto>();
        void Add(string part, IEnumerable<CourseCopyItemDto> items)
        {
            var list = items.ToList();
            if (list.Count > 0) parts.Add(new CourseCopyPartDto(part, list.Count, list));
        }

        Add(P.Modules, content.Modules.OrderBy(m => m.WeekNumber).ThenBy(m => m.CreatedAt)
            .Select(m => new CourseCopyItemDto(m.Id, m.Title, m.WeekNumber, null, content.Items.Count(i => i.ModuleId == m.Id), m.IsPublished)));
        Add(P.Assignments, content.Assignments.Where(a => !a.IsCompletedElsewhere).OrderBy(a => a.DueDate)
            .Select(a => new CourseCopyItemDto(a.Id, a.Title, null, a.DueDate, null, a.IsPublished)));
        Add(P.Quizzes, content.Quizzes.OrderBy(q => q.AvailableFrom ?? DateTime.MaxValue).ThenBy(q => q.Title)
            .Select(q => new CourseCopyItemDto(q.Id, q.Title, null, q.AvailableUntil, content.Questions.Count(x => x.QuizId == q.Id && !x.IsArchived)
                + content.QuestionGroups.Where(g => g.QuizId == q.Id).Sum(g => g.PickCount), q.IsPublished)));
        Add(P.QuestionBanks, content.Banks.OrderBy(b => b.Title)
            .Select(b => new CourseCopyItemDto(b.Id, b.Title, null, null, content.Questions.Count(x => x.QuestionBankId == b.Id && !x.IsArchived), true)));
        Add(P.Files, content.Materials.Where(m => m.SemesterId == source.SemesterId && m.CourseId == source.CourseId).OrderBy(m => m.Name)
            .Select(m => new CourseCopyItemDto(m.Id, m.Name, null, null, null, m.IsActive)));
        Add(P.ExternalTools, content.ToolLinks.OrderBy(l => l.Title)
            .Select(l => new CourseCopyItemDto(l.Id, l.Title, null, null, null, l.IsVisible)));
        if (content.LatePolicy is { } policy)
            parts.Add(new CourseCopyPartDto(P.Settings, 1, [new CourseCopyItemDto(policy.Id, "Late and missing work policy", null, null, null, true)]));
        return parts;
    }

    private async Task<(CourseCopyOffering? Source, CourseCopyOffering? Target, CourseTemplate? Template, CourseCopyResult<T>? Error)> ResolveAsync<T>(
        CourseCopyActor actor, Guid targetId, Guid sourceId, CancellationToken ct)
    {
        var access = await AccessAsync(actor, ct);
        var target = await _store.OfferingAsync(targetId, ct);
        if (target is null) return (null, null, null, CourseCopyResult<T>.NotFound());
        if (!access.CanManage(target)) return (null, null, null, CourseCopyResult<T>.Forbidden("You cannot change this course."));
        var source = await _store.OfferingAsync(sourceId, ct);
        if (source is null || source.OfferingId == targetId) return (null, null, null, CourseCopyResult<T>.NotFound("The course to copy from was not found."));
        var template = await _store.TemplateAsync(sourceId, ct);
        if (!access.CanCopyFrom(source, template, target))
            return (null, null, null, CourseCopyResult<T>.Forbidden("You cannot copy from that course."));
        return (source, target, template, null);
    }

    private async Task<Access> AccessAsync(CourseCopyActor actor, CancellationToken ct)
        => new(actor, IsSuperAdmin(actor.Role) ? null : (await _store.DepartmentsOfAsync(actor.UserId, actor.Role, ct)).ToHashSet());

    private sealed class Access(CourseCopyActor actor, HashSet<Guid>? departments)
    {
        /// <summary>Null for SuperAdmins (every department).</summary>
        public IReadOnlyCollection<Guid>? Departments => departments;

        private bool InSchool(CourseCopyOffering o) => actor.TenantId is not { } tenant || o.TenantId == tenant;

        public bool CanManage(CourseCopyOffering o)
        {
            if (!InSchool(o)) return false;
            if (departments is null) return IsSuperAdmin(actor.Role);
            if (departments.Contains(o.DepartmentId)) return IsAdmin(actor.Role) || IsFaculty(actor.Role);
            return IsFaculty(actor.Role) && o.TeacherUserId == actor.UserId;
        }

        public bool CanCopyFrom(CourseCopyOffering source, CourseTemplate? template, CourseCopyOffering target)
        {
            if (source.OfferingId == target.OfferingId || source.TenantId != target.TenantId || !InSchool(source)) return false;
            if (CanManage(source)) return true;
            if (template is null) return false;
            return template.Scope == CourseTemplateScope.Institution || departments is null || departments.Contains(source.DepartmentId);
        }
    }

    private static CourseCopySourceDto ToDto(CourseCopyOffering o, CourseTemplate? t, Guid userId)
        => new(o.OfferingId, o.CourseCode, o.CourseTitle, o.SemesterName, o.SemesterStart, o.TeacherName,
            t is not null, t?.Name, t?.Description, t?.Scope.ToString(), o.TeacherUserId == userId);

    private static CourseTemplateDto ToTemplateDto(CourseTemplate t, CourseCopyOffering o)
        => new(o.OfferingId, t.Name, t.Description, t.Scope.ToString(), o.CourseCode, o.CourseTitle, o.SemesterName);

    private sealed record RunSummary(IReadOnlyDictionary<string, int> Copied, IReadOnlyList<CourseCopyNoteDto> NotCopied);

    private static RunSummary ReadSummary(string json)
    {
        try { return JsonSerializer.Deserialize<RunSummary>(json, Json) ?? Empty(); }
        catch (JsonException) { return Empty(); }

        static RunSummary Empty() => new(new Dictionary<string, int>(), []);
    }
}
