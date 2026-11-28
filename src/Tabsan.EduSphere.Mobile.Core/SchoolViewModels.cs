using System.Collections.ObjectModel;

namespace Tabsan.EduSphere.Mobile.Core;

// School news, events and forms in the app: what students and parents read, reply to and fill in; and wellbeing:
// students check in and see the notes and plans the school shared with them, parents what was shared about each child.

/// <summary>The School tab: news, events (with replies), forms to fill in and (students and parents) wellbeing, one section at a time.</summary>
public sealed class SchoolViewModel : PageViewModel
{
    public const string NewsSection = "news";
    public const string EventsSection = "events";
    public const string FormsSection = "forms";
    public const string WellbeingSection = "wellbeing";

    private readonly EduSphereClient _client;
    private string _section = NewsSection;
    private int _newNews;
    private int _formsWaiting;
    private bool _checkInsEnabled;
    private CheckInItem? _latestCheckIn;

    public SchoolViewModel(EduSphereClient client)
    {
        _client = client;
        LoadCommand = new AsyncCommand(LoadAsync);
    }

    public ObservableCollection<NewsItem> News { get; } = [];
    public ObservableCollection<SchoolEventItem> Events { get; } = [];
    public ObservableCollection<FormToFillItem> Forms { get; } = [];
    /// <summary>Notes and plans the school shared (a parent's: about each child).</summary>
    public ObservableCollection<WellbeingNoteItem> WellbeingNotes { get; } = [];
    public ObservableCollection<WellbeingPlanItem> WellbeingPlans { get; } = [];
    public AsyncCommand LoadCommand { get; }

    public string Section
    {
        get => _section;
        set => Set(ref _section, value is EventsSection or FormsSection || (value == WellbeingSection && ShowsWellbeing) ? value : NewsSection);
    }

    /// <summary>Students and parents have the Wellbeing section; students also check in there.</summary>
    public bool ShowsWellbeing => _client.Session?.Role is "Student" or "Parent";
    public bool IsStudent => _client.Session?.Role == "Student";
    /// <summary>The student's school has switched check-ins on.</summary>
    public bool CheckInsEnabled { get => _checkInsEnabled; private set => Set(ref _checkInsEnabled, value); }
    public CheckInItem? LatestCheckIn { get => _latestCheckIn; private set { if (Set(ref _latestCheckIn, value)) OnPropertyChanged(nameof(LatestCheckInText)); } }

    /// <summary>"Your last check-in: Good · Today 08:10".</summary>
    public string? LatestCheckInText => LatestCheckIn is { } c
        ? $"{AppText.Get("YourLastCheckIn")} {MoodWord(c.Mood)} · {HomeViewModel.ShortDate(c.CheckedInAt, DateTime.UtcNow)}"
        : null;

    /// <summary>1 "Very low" … 5 "Great".</summary>
    public static string MoodWord(int mood) => mood is >= 1 and <= 5 ? AppText.Get($"Mood{mood}") : "";
    /// <summary>News not opened yet.</summary>
    public int NewNews { get => _newNews; private set => Set(ref _newNews, value); }
    /// <summary>Open forms (or children) still to answer.</summary>
    public int FormsWaiting { get => _formsWaiting; private set => Set(ref _formsWaiting, value); }

    public Task LoadAsync() => RunAsync(async () =>
    {
        var news = await _client.GetNewsAsync();
        var events = await _client.GetSchoolEventsAsync();
        var forms = await _client.GetFormsToFillAsync();
        Replace(News, news);
        Replace(Events, events.OrderBy(e => e.StartsAt));
        Replace(Forms, forms.OrderBy(f => f.Answered).ThenBy(f => f.DueAt ?? DateTime.MaxValue));
        NewNews = news.Count(n => n.IsNew);
        FormsWaiting = forms.Count(f => !f.Answered && f.Open);
        await LoadWellbeingAsync();
    });

    private async Task LoadWellbeingAsync()
    {
        if (!ShowsWellbeing) return;
        try
        {
            if (IsStudent)
            {
                var mine = await _client.GetMyWellbeingAsync();
                CheckInsEnabled = mine?.CheckInsEnabled == true;
                LatestCheckIn = mine?.Latest;
                Replace(WellbeingNotes, mine?.Records ?? []);
                Replace(WellbeingPlans, mine?.Plans ?? []);
            }
            else
            {
                var family = await _client.GetFamilyWellbeingAsync();
                foreach (var child in family) foreach (var note in child.Records) note.ChildName = child.Name;
                Replace(WellbeingNotes, family.SelectMany(c => c.Records).OrderByDescending(n => n.OccurredAt));
                Replace(WellbeingPlans, family.SelectMany(c => c.Plans.Select(p => { p.Title = $"{c.Name}: {p.Title}"; return p; })));
            }
        }
        catch (ApiException) { /* a school without pastoral care: the section stays empty */ }
    }

    /// <summary>A student says how they are; asking to talk tells the wellbeing team at once.</summary>
    public Task<bool> CheckInAsync(int mood, string? note, bool wantsToTalk) => RunAsync(async () =>
    {
        Notice = null;
        LatestCheckIn = await _client.CheckInAsync(mood, string.IsNullOrWhiteSpace(note) ? null : note.Trim(), wantsToTalk);
        Notice = AppText.Get(wantsToTalk ? "CheckInTalkSaved" : "CheckInSaved");
    });

    /// <summary>Going, not going or maybe; the event is shown as it now stands.</summary>
    public Task<bool> ReplyAsync(SchoolEventItem e, string answer, int guests) => RunAsync(async () =>
    {
        Notice = null;
        if (await _client.ReplyToEventAsync(e.Id, answer, answer == "NotGoing" ? 0 : Math.Clamp(guests, 0, e.MaxGuests)) is { } updated)
        {
            var i = Events.IndexOf(e);
            if (i >= 0) Events[i] = updated;
            Notice = AppText.Get("ReplySaved");
        }
    });

    /// <summary>A post was opened: it is no longer new.</summary>
    public void MarkRead(NewsItem item)
    {
        if (!item.IsNew) return;
        item.IsNew = false;
        var i = News.IndexOf(item);
        if (i >= 0) News[i] = item;
        NewNews = News.Count(n => n.IsNew);
    }

    private static void Replace<T>(ObservableCollection<T> target, IEnumerable<T> items)
    {
        target.Clear();
        foreach (var item in items) target.Add(item);
    }
}

public sealed class NewsPostViewModel : PageViewModel
{
    private readonly EduSphereClient _client;
    private NewsPostDetail? _post;

    public NewsPostViewModel(EduSphereClient client) => _client = client;

    public NewsPostDetail? Post { get => _post; private set { if (Set(ref _post, value)) OnPropertyChanged(nameof(Paragraphs)); } }
    /// <summary>The post's paragraphs (an empty line separates them).</summary>
    public IReadOnlyList<string> Paragraphs => Post is null ? [] : Post.Body.Replace("\r\n", "\n").Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    public Task LoadAsync(Guid id) => RunAsync(async () => Post = await _client.GetNewsPostAsync(id));
}

/// <summary>One question being answered: text, a date, a number, or ticks among its choices.</summary>
public sealed class FormAnswerInput : ObservableObject
{
    private string _value = "";

    public FormAnswerInput(FormFieldItem field, string? answer)
    {
        Field = field;
        var picked = (answer ?? "").Split('\n', StringSplitOptions.RemoveEmptyEntries);
        List<(string Value, string Label)> choices = field.Type switch
        {
            "YesNo" => [("Yes", AppText.Get("Yes")), ("No", AppText.Get("No"))],
            "SingleChoice" or "MultipleChoice" => field.Options.Select(o => (o, o)).ToList(),
            _ => []
        };
        foreach (var (value, label) in choices) Choices.Add(new FormChoice(value, label, picked.Contains(value)));
        if (Choices.Count == 0) _value = answer ?? "";
    }

    public FormFieldItem Field { get; }
    public ObservableCollection<FormChoice> Choices { get; } = [];
    public bool IsChoice => Choices.Count > 0;
    public bool IsMultiple => Field.Type == "MultipleChoice";
    public bool IsInformation => Field.Type == "Information";
    public string Label => Field.Required ? Field.Label + " *" : Field.Label;
    public string Value { get => _value; set => Set(ref _value, value ?? ""); }

    /// <summary>Ticks a choice: one of a single choice or yes/no, any of several.</summary>
    public void Pick(FormChoice choice, bool on)
    {
        if (!IsMultiple && on) foreach (var c in Choices) c.IsChecked = ReferenceEquals(c, choice);
        else choice.IsChecked = on;
    }

    /// <summary>What is sent: the text, or the ticked choices one per line.</summary>
    public string Answer => IsChoice ? string.Join("\n", Choices.Where(c => c.IsChecked).Select(c => c.Value)) : Value.Trim();
}

public sealed class FormChoice(string value, string label, bool isChecked) : ObservableObject
{
    private bool _checked = isChecked;
    public string Value { get; } = value;
    public string Label { get; } = label;
    public bool IsChecked { get => _checked; set => Set(ref _checked, value); }
}

/// <summary>Filling in a form (a parent's for one child); the answers already given are shown and can be changed until it closes.</summary>
public sealed class FormViewModel : PageViewModel
{
    private readonly EduSphereClient _client;
    private FormDetail? _form;

    public FormViewModel(EduSphereClient client)
    {
        _client = client;
        SendCommand = new AsyncCommand(SendAsync);
    }

    public FormDetail? Form
    {
        get => _form;
        private set
        {
            if (!Set(ref _form, value)) return;
            OnPropertyChanged(nameof(Subtitle));
            OnPropertyChanged(nameof(CanSend));
        }
    }

    public ObservableCollection<FormAnswerInput> Questions { get; } = [];
    public AsyncCommand SendCommand { get; }
    public bool CanSend => Form?.TakesAnswers == true;

    /// <summary>"For Ali · Due Fri 5 Dec 17:00 · Answered: Mrs Raza".</summary>
    public string Subtitle
    {
        get
        {
            if (Form is null) return "";
            var parts = new List<string>();
            if (Form.StudentName is not null) parts.Add(string.Format(AppText.Get("ForChild"), Form.StudentName));
            if (Form.DueAt is { } due) parts.Add($"{AppText.Get("Due")} {HomeViewModel.ShortDate(due, DateTime.UtcNow)}");
            if (Form.AnsweredBy is not null) parts.Add($"{AppText.Get("Answered")}: {Form.AnsweredBy}");
            if (!Form.TakesAnswers) parts.Add(AppText.Get("FormClosed"));
            return string.Join(" · ", parts);
        }
    }

    public Task LoadAsync(Guid id, Guid? studentProfileId) => RunAsync(async () => Show(await _client.GetFormAsync(id, studentProfileId)));

    public Task SendAsync() => RunAsync(async () =>
    {
        if (Form is null) return;
        Notice = null;
        var answers = Questions.Where(q => !q.IsInformation).ToDictionary(q => q.Field.Id, q => (string?)q.Answer);
        Show(await _client.SubmitFormAsync(Form.Id, Form.StudentProfileId, answers));
        Notice = AppText.Get("AnswersSaved");
    });

    private void Show(FormDetail? form)
    {
        Form = form;
        Questions.Clear();
        if (form is null) return;
        foreach (var f in form.Fields) Questions.Add(new FormAnswerInput(f, form.Answers.GetValueOrDefault(f.Id)));
    }
}
