using Tabsan.EduSphere.Domain.Pastoral;

namespace Tabsan.EduSphere.Application.Pastoral;

// Setting a school up: its wellbeing team (who, for which campus or year level, and whether they see confidential notes),
// its categories and its settings. Admins of the whole school and SuperAdmins; admins of some campuses only add and
// remove team members for their own campuses.
public sealed partial class PastoralService
{
    /// <summary>The school being set up, or why it cannot be.</summary>
    private static string? SchoolOf(PastoralViewer v, Guid? requested, out Guid tenant)
    {
        tenant = Guid.Empty;
        if (!v.IsManager) return "Only admins set up pastoral care.";
        if (v.TenantId is { } own)
        {
            if (requested is { } r && r != own) return "That is not your school.";
            tenant = own;
            return null;
        }
        if (requested is not { } chosen || chosen == Guid.Empty) return "Choose a school.";
        tenant = chosen;
        return null;
    }

    private static bool WholeSchool(PastoralViewer v) => v.IsSuperAdmin || (v.IsAdmin && v.Member.AdminCampuses is null);

    private static PastoralSettingsDto SettingsDto(PastoralSettings? s)
        => new(s?.CheckInsEnabled ?? false, s?.ConcernAlertCount ?? PastoralSettings.DefaultConcernCount, s?.ConcernAlertDays ?? PastoralSettings.DefaultConcernDays);

    public async Task<PastoralResult<PastoralSetupDto>> SetupAsync(PastoralViewer v, Guid? tenantId, CancellationToken ct = default)
    {
        if (!v.IsManager) return PastoralResult<PastoralSetupDto>.Forbidden("Only admins set up pastoral care.");
        var schools = v.TenantId is null ? await _store.SchoolsAsync(ct) : [];
        if (v.TenantId is null && tenantId is null)
            return PastoralResult<PastoralSetupDto>.Success(new PastoralSetupDto(null, schools, [], [], SettingsDto(null), [], new FilterOptionsDto([], [], [])));
        if (SchoolOf(v, tenantId, out var tenant) is { } why) return PastoralResult<PastoralSetupDto>.Forbidden(why);

        var team = await _store.TeamAsync(tenant, ct);
        var places = await _store.PlaceNamesAsync(team.SelectMany(t => new[] { t.Member.CampusId, t.Member.SemesterId }).Where(g => g != null).Select(g => g!.Value)
            .Distinct().ToList(), ct);
        var members = team.OrderBy(t => t.Name, StringComparer.CurrentCultureIgnoreCase)
            .Select(t => new TeamMemberDto(t.Member.Id, t.Member.UserId, t.Name, t.Member.Title, t.Member.CampusId,
                t.Member.CampusId is { } c ? places.GetValueOrDefault(c) : null, t.Member.SemesterId,
                t.Member.SemesterId is { } s ? places.GetValueOrDefault(s) : null, t.Member.SeesConfidential)).ToList();
        return PastoralResult<PastoralSetupDto>.Success(new PastoralSetupDto(tenant, schools, members, await _store.StaffAsync(tenant, ct),
            SettingsDto(await _store.SettingsAsync(tenant, ct)), await CategoriesForAsync(tenant, false, ct),
            await _store.FilterOptionsAsync(tenant, v.IsAdmin ? v.Member.AdminCampuses : null, null, ct)));
    }

    public async Task<PastoralResult<TeamMemberDto>> AddTeamMemberAsync(PastoralViewer v, AddTeamMemberRequest req, CancellationToken ct = default)
    {
        if (SchoolOf(v, req.TenantId, out var tenant) is { } why) return PastoralResult<TeamMemberDto>.Forbidden(why);
        var staff = await _store.StaffAsync(tenant, ct);
        var person = staff.FirstOrDefault(p => p.UserId == req.UserId);
        if (person is null) return PastoralResult<TeamMemberDto>.Invalid("Choose a teacher or admin of the school.");
        var places = await _store.FilterOptionsAsync(tenant, v.IsAdmin ? v.Member.AdminCampuses : null, null, ct);
        if (req.CampusId is { } campus && places.Campuses.All(c => c.Id != campus)) return PastoralResult<TeamMemberDto>.Invalid("Campus not found.");
        if (req.SemesterId is { } semester && places.Semesters.All(s => s.Id != semester)) return PastoralResult<TeamMemberDto>.Invalid("Year level not found.");
        if (v.IsAdmin && v.Member.AdminCampuses is { } mine && (req.CampusId is not { } chosen || !mine.Contains(chosen)))
            return PastoralResult<TeamMemberDto>.Invalid("Choose one of your campuses.");
        var team = await _store.TeamAsync(tenant, ct);
        if (team.Any(t => t.Member.UserId == req.UserId && t.Member.CampusId == req.CampusId && t.Member.SemesterId == req.SemesterId))
            return PastoralResult<TeamMemberDto>.Invalid($"{person.Name} is already on the team for that place.");

        PastoralTeamMember member;
        try { member = new PastoralTeamMember(tenant, req.UserId, req.CampusId, req.SemesterId, req.Title, req.SeesConfidential, v.UserId); }
        catch (ArgumentException ex) { return PastoralResult<TeamMemberDto>.Invalid(ex.Plain()); }
        _store.Add(member);
        await _store.SaveChangesAsync(ct);
        await AuditAsync("PastoralTeamMemberAdded", "PastoralTeamMember", member.Id, v, ct);
        await NotifyAsync("You are on the wellbeing team",
            $"You were added to the school's wellbeing team as {member.Title}. You will be told about serious concerns and students who ask to talk.",
            [member.UserId], v.UserId, ct);
        var names = await _store.PlaceNamesAsync(new[] { member.CampusId, member.SemesterId }.Where(g => g != null).Select(g => g!.Value).ToList(), ct);
        return PastoralResult<TeamMemberDto>.Success(new TeamMemberDto(member.Id, member.UserId, person.Name, member.Title, member.CampusId,
            member.CampusId is { } c ? names.GetValueOrDefault(c) : null, member.SemesterId, member.SemesterId is { } s ? names.GetValueOrDefault(s) : null,
            member.SeesConfidential), 201);
    }

    public async Task<PastoralResult<bool>> RemoveTeamMemberAsync(PastoralViewer v, Guid id, CancellationToken ct = default)
    {
        var member = await _store.TeamMemberAsync(id, ct);
        if (member is null || SchoolOf(v, member.TenantId, out _) is not null) return PastoralResult<bool>.NotFound("Team member not found.");
        if (v.IsAdmin && v.Member.AdminCampuses is { } mine && (member.CampusId is not { } c || !mine.Contains(c)))
            return PastoralResult<bool>.Forbidden("You change the team for your own campuses.");
        _store.Remove(member);
        await _store.SaveChangesAsync(ct);
        await AuditAsync("PastoralTeamMemberRemoved", "PastoralTeamMember", member.Id, v, ct);
        return PastoralResult<bool>.Success(true);
    }

    public async Task<PastoralResult<PastoralSettingsDto>> SaveSettingsAsync(PastoralViewer v, SaveSettingsRequest req, CancellationToken ct = default)
    {
        if (SchoolOf(v, req.TenantId, out var tenant) is { } why) return PastoralResult<PastoralSettingsDto>.Forbidden(why);
        if (!WholeSchool(v)) return PastoralResult<PastoralSettingsDto>.Forbidden("Admins of the whole school change these settings.");
        var settings = await _store.SettingsAsync(tenant, ct);
        var isNew = settings is null;
        settings ??= new PastoralSettings(tenant);
        try { settings.Set(req.CheckInsEnabled, req.ConcernAlertCount, req.ConcernAlertDays); }
        catch (ArgumentException ex) { return PastoralResult<PastoralSettingsDto>.Invalid(ex.Plain()); }
        if (isNew) _store.Add(settings);
        await _store.SaveChangesAsync(ct);
        await AuditAsync("PastoralSettingsChanged", "PastoralSettings", settings.Id, v, ct);
        return PastoralResult<PastoralSettingsDto>.Success(SettingsDto(settings));
    }

    /// <summary>Adds a category (id null) or renames, moves or hides one. The school's first change keeps the starting list.</summary>
    public async Task<PastoralResult<CategoryDto>> SaveCategoryAsync(PastoralViewer v, Guid? id, SaveCategoryRequest req, CancellationToken ct = default)
    {
        if (SchoolOf(v, req.TenantId, out var tenant) is { } why) return PastoralResult<CategoryDto>.Forbidden(why);
        if (!WholeSchool(v)) return PastoralResult<CategoryDto>.Forbidden("Admins of the whole school change the categories.");
        if (!TryParse(req.Kind, PastoralKind.Behaviour, out var kind) || !TryParse(req.Tone, PastoralTone.Neutral, out var tone))
            return PastoralResult<CategoryDto>.Invalid("Choose the kind of note and whether it is praise or a concern.");

        var stored = (await _store.CategoriesAsync(tenant, ct)).ToList();
        if (stored.Count == 0)
        {
            var n = 0;
            foreach (var d in PastoralCategory.Defaults)
            {
                var start = new PastoralCategory(tenant, d.Kind, d.Tone, d.Name, n++);
                _store.Add(start);
                stored.Add(start);
            }
        }
        PastoralCategory category;
        try
        {
            if (id is { } existing && existing != Guid.Empty)
            {
                var found = await _store.CategoryAsync(existing, ct) ?? stored.FirstOrDefault(c => c.Id == existing);
                if (found is null || found.TenantId != tenant) return PastoralResult<CategoryDto>.NotFound("Category not found.");
                found.Edit(kind, tone, req.Name, req.IsActive);
                category = found;
            }
            else
            {
                var name = (req.Name ?? "").Trim();
                // A starting category the page knew without an id: the copy just kept.
                category = stored.FirstOrDefault(c => c.Kind == kind && c.Tone == tone && string.Equals(c.Name, name, StringComparison.CurrentCultureIgnoreCase))
                           ?? new PastoralCategory(tenant, kind, tone, req.Name, stored.Count == 0 ? 0 : stored.Max(c => c.Position) + 1);
                if (!stored.Contains(category)) _store.Add(category);
                else category.Edit(kind, tone, req.Name, req.IsActive);
            }
        }
        catch (ArgumentException ex) { return PastoralResult<CategoryDto>.Invalid(ex.Plain()); }
        await _store.SaveChangesAsync(ct);
        return PastoralResult<CategoryDto>.Success(new CategoryDto(category.Id, category.Kind.ToString(), category.Tone.ToString(), category.Name, category.IsActive));
    }
}
