using System.Text;
using Tabsan.EduSphere.Domain.Community;

namespace Tabsan.EduSphere.Application.Community;

// Online forms: consent forms, permission slips and surveys. Parents answer once for each child the form concerns (either
// parent; the latest answer counts), students and staff once each. The author follows who has answered, reminds the rest
// (and everyone who has not answered is reminded automatically a day before the due date), and downloads the answers.

public sealed partial class CommunityService
{
    /// <summary>Open forms for the person (for a parent, one row per child the form concerns), unanswered first.</summary>
    public async Task<IReadOnlyList<FormToFillDto>> FormsToFillAsync(CommunityMember m, DateTime now, CancellationToken ct = default)
    {
        var forms = (await _store.FormsAsync(m.TenantId, ct)).Where(f => f.Status == FormStatus.Open && Respondents(f, m).Count > 0).ToList();
        if (forms.Count == 0) return [];
        var responses = await _store.ResponsesAsync(forms.Select(f => f.Id).ToList(), ct);
        var names = await _store.NamesAsync(responses.Select(r => r.RespondentUserId).Distinct().ToList(), ct);
        var rows = new List<FormToFillDto>();
        foreach (var f in forms)
            foreach (var child in Respondents(f, m))
            {
                var answer = Find(responses, f.Id, m, child);
                rows.Add(new FormToFillDto(f.Id, f.Title, f.DueAt, child?.StudentProfileId, child?.Name, answer is not null, answer?.SubmittedAt,
                    answer is null ? null : names.GetValueOrDefault(answer.RespondentUserId), f.TakesAnswers(now)));
            }
        return rows.OrderBy(r => r.Answered).ThenBy(r => r.DueAt ?? DateTime.MaxValue).ThenBy(r => r.Title).ToList();
    }

    /// <summary>A form with the person's answers (a parent's for the chosen child). Those who manage it see it as a preview.</summary>
    public async Task<CommunityResult<FormDto>> FormAsync(CommunityMember m, Guid id, Guid? studentProfileId, DateTime now, CancellationToken ct = default)
    {
        var f = await _store.FormAsync(id, ct);
        var manage = f is not null && CanManage(f, m);
        var respondents = f is null ? [] : Respondents(f, m);
        if (f is null || (!manage && (f.Status == FormStatus.Draft || respondents.Count == 0))) return CommunityResult<FormDto>.NotFound("Form not found.");
        CommunityChild? child = null;
        if (m.IsParent && respondents.Count > 0)
        {
            child = studentProfileId is { } sid ? respondents.FirstOrDefault(c => c?.StudentProfileId == sid) : respondents.Count == 1 ? respondents[0] : null;
            if (child is null) return CommunityResult<FormDto>.Invalid("Choose which child you are answering for.");
        }
        var response = respondents.Count == 0 ? null : Find(await _store.ResponsesAsync([id], ct), id, m, child);
        return CommunityResult<FormDto>.Success(await FormDtoAsync(f, m, manage, child, response, now, ct));
    }

    public async Task<CommunityResult<FormDto>> SubmitFormAsync(CommunityMember m, Guid id, SubmitFormRequest r, DateTime now, CancellationToken ct = default)
    {
        var f = await _store.FormAsync(id, ct);
        var respondents = f is null ? [] : Respondents(f, m);
        if (f is null || f.Status == FormStatus.Draft || respondents.Count == 0) return CommunityResult<FormDto>.NotFound("Form not found.");
        if (!f.TakesAnswers(now)) return CommunityResult<FormDto>.Conflict("The form is closed.");
        CommunityChild? child = null;
        if (m.IsParent)
        {
            child = respondents.FirstOrDefault(c => c?.StudentProfileId == r.StudentProfileId);
            if (child is null) return CommunityResult<FormDto>.Invalid("Choose which child you are answering for.");
        }
        var fields = await _store.FieldsAsync(id, ct);
        var (answers, error) = FormAnswers.Check(fields, r.Answers ?? new Dictionary<Guid, string?>());
        if (error is not null) return CommunityResult<FormDto>.Invalid(error);

        var response = Find(await _store.ResponsesAsync([id], ct), id, m, child);
        if (response is null) _store.Add(response = new FormResponse(id, m.UserId, child?.StudentProfileId));
        response.Submit(m.UserId, answers!, now);
        await _store.SaveChangesAsync(ct);
        return CommunityResult<FormDto>.Success(await FormDtoAsync(f, m, CanManage(f, m), child, response, now, ct));
    }

    /// <summary>Forms the person manages with how many answers each has, newest first.</summary>
    public async Task<CommunityResult<IReadOnlyList<FormManageDto>>> ManageFormsAsync(CommunityMember m, CancellationToken ct = default)
    {
        if (!CanWrite(m)) return CommunityResult<IReadOnlyList<FormManageDto>>.Forbidden("Only teachers and admins write for the school.");
        var forms = (await _store.FormsAsync(m.TenantId, ct)).Where(f => CanManage(f, m)).OrderByDescending(f => f.CreatedAt).Take(300).ToList();
        var responses = await _store.ResponsesAsync(forms.Select(f => f.Id).ToList(), ct);
        var names = await _store.AudienceNamesAsync(forms, ct);
        var authors = await _store.NamesAsync(forms.Select(f => f.AuthorUserId).Distinct().ToList(), ct);
        IReadOnlyList<FormManageDto> list = forms.Select(f => new FormManageDto(f.Id, f.Title, f.Status.ToString(), f.DueAt, Describe(f, names),
            responses.Count(r => r.FormId == f.Id), authors.GetValueOrDefault(f.AuthorUserId) ?? "")).ToList();
        return CommunityResult<IReadOnlyList<FormManageDto>>.Success(list);
    }

    /// <summary>Writes a form or changes it. Its questions can change until the first answer comes in.</summary>
    public async Task<CommunityResult<FormDto>> SaveFormAsync(CommunityMember m, Guid? id, SaveFormRequest r, DateTime now, CancellationToken ct = default)
    {
        if (!CanWrite(m)) return CommunityResult<FormDto>.Forbidden("Only teachers and admins write for the school.");
        SchoolForm? f = null;
        IReadOnlyList<FormField> old = [];
        if (id is { } existing)
        {
            f = await _store.FormAsync(existing, ct);
            if (f is null) return CommunityResult<FormDto>.NotFound("Form not found.");
            if (!CanManage(f, m)) return CommunityResult<FormDto>.Forbidden("You cannot change this form.");
            old = await _store.FieldsAsync(existing, ct);
        }
        List<FormField>? fields = null;
        try
        {
            if (f is null) f = new SchoolForm(m.UserId, r.Title ?? "", r.Description, r.DueAt);
            else f.Edit(r.Title ?? "", r.Description, r.DueAt);
            if (r.Fields is not null)
            {
                if (r.Fields.Count > SchoolForm.MaxFields) return CommunityResult<FormDto>.Invalid($"A form has at most {SchoolForm.MaxFields} questions.");
                fields = [];
                foreach (var (q, i) in r.Fields.Select((q, i) => (q, i)))
                {
                    if (!Enum.TryParse<FormFieldType>(q.Type, true, out var type) || !Enum.IsDefined(type)) return CommunityResult<FormDto>.Invalid($"Question {i + 1}: choose a kind of question.");
                    fields.Add(new FormField(f.Id, i + 1, type, q.Label ?? "", q.Help, q.Required, q.Options));
                }
            }
            if (await ApplyAudienceAsync(f, m, r.Audience, ct) is { } error) return CommunityResult<FormDto>.Invalid(error);
        }
        catch (ArgumentException ex) { return CommunityResult<FormDto>.Invalid(ex.Plain()); }

        if (fields is not null && !SameQuestions(old, fields))
        {
            if (id is not null && (await _store.ResponsesAsync([f.Id], ct)).Count > 0)
                return CommunityResult<FormDto>.Conflict("Answers have come in, so the questions can no longer change. Close this form and make a new one.");
            _store.ReplaceFields(old, fields);
        }
        var questions = fields ?? old;
        if (r.Open && !questions.Any(q => q.Type != FormFieldType.Information)) return CommunityResult<FormDto>.Invalid("Add at least one question before opening the form.");
        var opening = r.Open && f.Status != FormStatus.Open;
        if (opening) f.Open(now);
        if (id is null) _store.Add(f);
        await _store.SaveChangesAsync(ct);
        if (opening) await NotifyAsync(f, $"Please fill in: {f.Title}", f.DueAt is { } due ? $"Due {due:d MMM yyyy}." : "A form for you to fill in.", null, ct);
        await AuditAsync(id is null ? "SchoolFormCreated" : "SchoolFormUpdated", "SchoolForm", f.Id, m, ct);
        return CommunityResult<FormDto>.Success(await FormDtoAsync(f, m, true, null, null, now, ct), id is null ? 201 : 200);
    }

    public async Task<CommunityResult<FormDto>> OpenFormAsync(CommunityMember m, Guid id, bool open, DateTime now, CancellationToken ct = default)
    {
        var f = await _store.FormAsync(id, ct);
        if (f is null) return CommunityResult<FormDto>.NotFound("Form not found.");
        if (!CanManage(f, m)) return CommunityResult<FormDto>.Forbidden("You cannot change this form.");
        if (open)
        {
            if (!(await _store.FieldsAsync(id, ct)).Any(q => q.Type != FormFieldType.Information))
                return CommunityResult<FormDto>.Invalid("Add at least one question before opening the form.");
            var first = f.OpenedAt is null;
            f.Open(now);
            await _store.SaveChangesAsync(ct);
            if (first) await NotifyAsync(f, $"Please fill in: {f.Title}", f.DueAt is { } due ? $"Due {due:d MMM yyyy}." : "A form for you to fill in.", null, ct);
        }
        else
        {
            if (f.Status != FormStatus.Open) return CommunityResult<FormDto>.Conflict("The form is not open.");
            f.Close(now);
            await _store.SaveChangesAsync(ct);
        }
        await AuditAsync(open ? "SchoolFormOpened" : "SchoolFormClosed", "SchoolForm", f.Id, m, ct);
        return CommunityResult<FormDto>.Success(await FormDtoAsync(f, m, true, null, null, now, ct));
    }

    /// <summary>Everyone the form waits for, who has answered, and a count of each choice.</summary>
    public async Task<CommunityResult<FormTrackingDto>> FormTrackingAsync(CommunityMember m, Guid id, DateTime now, CancellationToken ct = default)
    {
        var f = await _store.FormAsync(id, ct);
        if (f is null) return CommunityResult<FormTrackingDto>.NotFound("Form not found.");
        if (!CanManage(f, m)) return CommunityResult<FormTrackingDto>.Forbidden("You cannot see this form's answers.");
        var t = await TrackAsync(f, ct);
        var fields = await _store.FieldsAsync(id, ct);
        var summary = fields.Where(q => q.Type is FormFieldType.SingleChoice or FormFieldType.MultipleChoice or FormFieldType.YesNo).Select(q =>
        {
            var choices = q.Type == FormFieldType.YesNo ? [FormAnswers.Yes, FormAnswers.No] : q.Options;
            var counts = choices.ToDictionary(c => c, c => t.Responses.Count(r => r.Answers.TryGetValue(q.Id, out var a) && a.Split('\n').Contains(c)));
            return new FieldSummaryDto(q.Id, q.Label, q.Type.ToString(), counts);
        }).ToList();
        return CommunityResult<FormTrackingDto>.Success(new FormTrackingDto(await FormDtoAsync(f, m, true, null, null, now, ct), t.Rows.Count,
            t.Rows.Count(r => r.Answered), t.Rows, summary, f.RemindedAt));
    }

    /// <summary>Reminds everyone who has not answered (parents of children without an answer, students, staff).</summary>
    public async Task<CommunityResult<int>> RemindFormAsync(CommunityMember m, Guid id, DateTime now, CancellationToken ct = default)
    {
        var f = await _store.FormAsync(id, ct);
        if (f is null) return CommunityResult<int>.NotFound("Form not found.");
        if (!CanManage(f, m)) return CommunityResult<int>.Forbidden("You cannot change this form.");
        if (!f.TakesAnswers(now)) return CommunityResult<int>.Conflict("The form is not taking answers.");
        if (f.RemindedAt is { } last && now - last < TimeSpan.FromHours(1)) return CommunityResult<int>.Conflict("A reminder went out less than an hour ago.");
        var sent = await RemindAsync(f, await TrackAsync(f, ct), now, ct);
        await AuditAsync("SchoolFormReminded", "SchoolForm", f.Id, m, ct);
        return CommunityResult<int>.Success(sent);
    }

    /// <summary>The answers as a CSV file (opens in Excel): one row per answer, one column per question.</summary>
    public async Task<CommunityResult<(string FileName, byte[] Content)>> ExportFormAsync(CommunityMember m, Guid id, CancellationToken ct = default)
    {
        var f = await _store.FormAsync(id, ct);
        if (f is null) return CommunityResult<(string, byte[])>.NotFound("Form not found.");
        if (!CanManage(f, m)) return CommunityResult<(string, byte[])>.Forbidden("You cannot see this form's answers.");
        var fields = (await _store.FieldsAsync(id, ct)).Where(q => q.Type != FormFieldType.Information).OrderBy(q => q.Position).ToList();
        var t = await TrackAsync(f, ct);
        var sb = new StringBuilder();
        void Line(IEnumerable<string?> cells) => sb.Append(string.Join(",", cells.Select(Csv))).Append("\r\n");
        Line(new[] { "For", "Class", "Answered by", "Answered at (UTC)" }.Concat(fields.Select(q => q.Label)));
        foreach (var (row, response) in t.Answered)
            Line(new[] { row.Name, row.ClassName, row.AnsweredBy, response.SubmittedAt.ToString("yyyy-MM-dd HH:mm") }
                .Concat(fields.Select(q => response.Answers.TryGetValue(q.Id, out var a) ? a.Replace("\n", "; ") : "")));
        var name = string.Concat(f.Title.Select(c => char.IsLetterOrDigit(c) ? c : '-')).Trim('-');
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        return CommunityResult<(string, byte[])>.Success(($"{(name.Length == 0 ? "form" : name[..Math.Min(name.Length, 60)])}-answers.csv", bytes));
    }

    // ── helpers ──

    /// <summary>Who the person answers as: a parent once for each child the form concerns (child set), a student or staff
    /// member once for themself (null). Empty when the form is not for them.</summary>
    private static List<CommunityChild?> Respondents(SchoolForm f, CommunityMember m)
    {
        if (!InAudience(f, m)) return [];
        if (m.IsParent) return f.Roles.HasFlag(AudienceRoles.Parents) ? m.Children.Where(c => Reaches(f, c)).Cast<CommunityChild?>().ToList() : [];
        return (f.Roles & m.Group) != AudienceRoles.None ? [null] : [];
    }

    private static FormResponse? Find(IReadOnlyList<FormResponse> responses, Guid formId, CommunityMember m, CommunityChild? child)
        => child is not null
            ? responses.FirstOrDefault(r => r.FormId == formId && r.StudentProfileId == child.StudentProfileId)
            : responses.FirstOrDefault(r => r.FormId == formId && r.StudentProfileId == null && r.RespondentUserId == m.UserId);

    private static bool SameQuestions(IReadOnlyList<FormField> a, IReadOnlyList<FormField> b)
        => a.Count == b.Count && a.OrderBy(q => q.Position).Zip(b.OrderBy(q => q.Position)).All(p =>
            p.First.Type == p.Second.Type && p.First.Label == p.Second.Label && p.First.Help == p.Second.Help && p.First.Required == p.Second.Required
            && p.First.OptionsJson == p.Second.OptionsJson);

    private sealed record Tracking(List<FormTrackingRowDto> Rows, List<FormResponse> Responses, List<(FormTrackingRowDto Row, FormResponse Response)> Answered,
        List<Guid> NotAnswered);

    /// <summary>The form's expected answers: one per child for parent forms, one per student or staff member otherwise.</summary>
    private async Task<Tracking> TrackAsync(SchoolForm f, CancellationToken ct)
    {
        var people = await _store.PeopleAsync(f, ct);
        var responses = (await _store.ResponsesAsync([f.Id], ct)).ToList();
        var names = await _store.NamesAsync(responses.Select(r => r.RespondentUserId).Distinct().ToList(), ct);
        var rows = new List<FormTrackingRowDto>();
        var answered = new List<(FormTrackingRowDto, FormResponse)>();
        var waiting = new List<Guid>();
        void Add(FormTrackingRowDto row, FormResponse? response, IEnumerable<Guid> remind)
        {
            rows.Add(row);
            if (response is not null) answered.Add((row, response));
            else waiting.AddRange(remind);
        }
        if (f.Roles.HasFlag(AudienceRoles.Parents))
            foreach (var s in people.Students.OrderBy(s => s.ClassName).ThenBy(s => s.Name))
            {
                var parents = people.Parents.Where(p => p.StudentProfileId == s.StudentProfileId).ToList();
                var r = responses.FirstOrDefault(x => x.StudentProfileId == s.StudentProfileId);
                Add(new FormTrackingRowDto("Parent", s.Name, s.ClassName, parents.Count == 0 ? null : string.Join(", ", parents.Select(p => p.Name).Distinct()),
                    r is not null, r is null ? null : names.GetValueOrDefault(r.RespondentUserId), r?.SubmittedAt, parents.Count == 0), r, parents.Select(p => p.ParentUserId));
            }
        if (f.Roles.HasFlag(AudienceRoles.Students))
            foreach (var s in people.Students.OrderBy(s => s.ClassName).ThenBy(s => s.Name))
            {
                var r = responses.FirstOrDefault(x => x.StudentProfileId == null && x.RespondentUserId == s.UserId);
                Add(new FormTrackingRowDto("Student", s.Name, s.ClassName, null, r is not null, r is null ? null : s.Name, r?.SubmittedAt, false), r, [s.UserId]);
            }
        if (f.Roles.HasFlag(AudienceRoles.Staff))
            foreach (var s in people.Staff.OrderBy(s => s.Name))
            {
                var r = responses.FirstOrDefault(x => x.StudentProfileId == null && x.RespondentUserId == s.UserId);
                Add(new FormTrackingRowDto("Staff", s.Name, null, null, r is not null, r is null ? null : s.Name, r?.SubmittedAt, false), r, [s.UserId]);
            }
        return new Tracking(rows, responses, answered, waiting.Distinct().ToList());
    }

    private async Task<int> RemindAsync(SchoolForm f, Tracking t, DateTime now, CancellationToken ct)
    {
        f.MarkReminded(now);
        await _store.SaveChangesAsync(ct);
        if (t.NotAnswered.Count == 0) return 0;
        return await NotifyAsync(f, $"Reminder: {f.Title}", f.DueAt is { } due ? $"Please fill in the form by {due:d MMM yyyy}." : "Please fill in the form.",
            t.NotAnswered, ct);
    }

    private async Task<FormDto> FormDtoAsync(SchoolForm f, CommunityMember m, bool manage, CommunityChild? child, FormResponse? response, DateTime now,
        CancellationToken ct)
    {
        var fields = await _store.FieldsAsync(f.Id, ct);
        var names = await _store.AudienceNamesAsync([f], ct);
        var by = response is null ? null : (await _store.NamesAsync([response.RespondentUserId], ct)).GetValueOrDefault(response.RespondentUserId);
        return new FormDto(f.Id, f.Title, f.Description, f.DueAt, f.Status.ToString(), f.TakesAnswers(now), Describe(f, names),
            fields.OrderBy(q => q.Position).Select(q => new FormFieldDto(q.Id, q.Type.ToString(), q.Label, q.Help, q.Required, q.Options)).ToList(),
            child?.StudentProfileId, child?.Name, response?.Answers ?? new Dictionary<Guid, string>(), response?.SubmittedAt, by, manage,
            await _store.EventOfFormAsync(f.Id, ct));
    }

    private static string Csv(string? value)
    {
        var v = value ?? "";
        if (v.Length > 0 && "=+-@".Contains(v[0])) v = "'" + v; // spreadsheet formulas are not run
        return v.IndexOfAny([',', '"', '\n', '\r']) >= 0 ? "\"" + v.Replace("\"", "\"\"") + "\"" : v;
    }
}
