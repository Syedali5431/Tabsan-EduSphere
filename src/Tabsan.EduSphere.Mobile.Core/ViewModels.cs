using System.Collections.ObjectModel;

namespace Tabsan.EduSphere.Mobile.Core;

// Plan M12 — screen logic shared by every platform. Pages bind to these; nothing here depends on MAUI.

public abstract class PageViewModel : ObservableObject
{
    private bool _busy;
    private string? _error;
    private string? _notice;

    public bool IsBusy { get => _busy; protected set => Set(ref _busy, value); }
    public string? Error { get => _error; protected set => Set(ref _error, value); }
    public string? Notice { get => _notice; protected set => Set(ref _notice, value); }

    /// <summary>Runs a call, showing the API's message (or "no connection") instead of crashing.</summary>
    protected async Task<bool> RunAsync(Func<Task> work)
    {
        IsBusy = true;
        Error = null;
        try
        {
            await work();
            return true;
        }
        catch (ApiException ex) { Error = ex.Message; }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException) { Error = AppText.Get("NoConnection"); }
        finally { IsBusy = false; }
        return false;
    }
}

public sealed class SignInViewModel : PageViewModel
{
    private readonly EduSphereClient _client;
    private readonly IBrowserAuthenticator _browser;
    private readonly AppSettings _settings;
    private string _username = "";
    private string _password = "";
    private string _mfaCode = "";
    private bool _needsMfa;
    private MobileConfig? _config;
    private VersionPolicy.State _version;

    public SignInViewModel(EduSphereClient client, IBrowserAuthenticator browser, AppSettings settings)
    {
        _client = client;
        _browser = browser;
        _settings = settings;
        SignInCommand = new AsyncCommand(SignInAsync);
        SingleSignOnCommand = new AsyncCommand(p => SingleSignOnAsync(p as string ?? ""));
        LoadCommand = new AsyncCommand(LoadAsync);
    }

    public string Username { get => _username; set => Set(ref _username, value); }
    public string Password { get => _password; set => Set(ref _password, value); }
    public string MfaCode { get => _mfaCode; set => Set(ref _mfaCode, value); }
    public bool NeedsMfa { get => _needsMfa; private set => Set(ref _needsMfa, value); }
    public MobileConfig? Config { get => _config; private set => Set(ref _config, value); }
    public VersionPolicy.State VersionState { get => _version; private set => Set(ref _version, value); }
    public ObservableCollection<SsoProvider> Providers { get; } = [];

    public AsyncCommand SignInCommand { get; }
    public AsyncCommand SingleSignOnCommand { get; }
    public AsyncCommand LoadCommand { get; }

    /// <summary>Raised after a successful sign-in.</summary>
    public event EventHandler<Session>? SignedIn;

    public Task LoadAsync() => RunAsync(async () =>
    {
        Config = await _client.GetConfigAsync();
        Providers.Clear();
        foreach (var p in Config?.SsoProviders ?? []) Providers.Add(p);
        VersionState = VersionPolicy.Check(_settings.AppVersion, Config?.MinimumVersion, Config?.LatestVersion);
        if (VersionState == VersionPolicy.State.UpdateRequired) Error = AppText.Get("UpdateRequired");
    });

    public async Task SignInAsync()
    {
        if (VersionState == VersionPolicy.State.UpdateRequired) { Error = AppText.Get("UpdateRequired"); return; }
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrEmpty(Password)) { Error = AppText.Get("WrongCredentials"); return; }
        SignInResult? result = null;
        await RunAsync(async () => result = await _client.SignInAsync(Username.Trim(), Password, NeedsMfa ? MfaCode : null));
        if (result is null) return;
        Handle(result);
    }

    public async Task SingleSignOnAsync(string provider)
    {
        var portal = Config?.PortalBaseUrl;
        if (string.IsNullOrWhiteSpace(portal) || string.IsNullOrWhiteSpace(provider)) { Error = AppText.Get("NoConnection"); return; }
        var pkce = Pkce.Create();
        var callback = new Uri(Config!.AppRedirectUri);
        Uri? returned = null;
        await RunAsync(async () => returned = await _browser.AuthenticateAsync(new Uri(EduSphereClient.SingleSignOnUrl(portal, provider, pkce)), callback));
        if (returned is null) return;
        SignInResult? result = null;
        await RunAsync(async () => result = await _client.CompleteSingleSignOnAsync(returned, pkce, $"EduSphere {_settings.Platform} {_settings.AppVersion}"));
        if (result is not null) Handle(result);
    }

    private void Handle(SignInResult result)
    {
        switch (result.Status)
        {
            case SignInStatus.Success:
                Password = MfaCode = "";
                NeedsMfa = false;
                SignedIn?.Invoke(this, result.Session!);
                break;
            case SignInStatus.MfaRequired:
                NeedsMfa = true;
                Error = AppText.Get("EnterAuthenticatorCode");
                break;
            case SignInStatus.InvalidMfaCode:
                Error = result.Message ?? AppText.Get("EnterAuthenticatorCode");
                break;
            case SignInStatus.SsoRequired:
                Error = AppText.Get("UseSso");
                break;
            case SignInStatus.InvalidCredentials:
                Error = AppText.Get("WrongCredentials");
                break;
            case SignInStatus.UpdateRequired:
                Error = AppText.Get("UpdateRequired");
                break;
            default:
                Error = result.Message ?? AppText.Get("NoConnection");
                break;
        }
    }
}

public sealed class HomeViewModel : PageViewModel
{
    private readonly EduSphereClient _client;
    private HomeData? _home;

    public HomeViewModel(EduSphereClient client)
    {
        _client = client;
        LoadCommand = new AsyncCommand(LoadAsync);
    }

    public HomeData? Home { get => _home; private set { Set(ref _home, value); OnPropertyChanged(nameof(Greeting)); } }
    public string Greeting => string.Format(AppText.Get("Hello"), Home?.DisplayName ?? _client.Session?.Username ?? "");
    public bool IsStudent => string.Equals(_client.Session?.Role, "Student", StringComparison.OrdinalIgnoreCase);
    public bool IsTeacher => string.Equals(_client.Session?.Role, "Faculty", StringComparison.OrdinalIgnoreCase);
    public bool IsParent => string.Equals(_client.Session?.Role, "Parent", StringComparison.OrdinalIgnoreCase);
    public AsyncCommand LoadCommand { get; }

    public Task LoadAsync() => RunAsync(async () => Home = await _client.GetHomeAsync());

    /// <summary>"Overdue" for open work past its date, otherwise the due date.</summary>
    public static string DueLabel(DueItem item, DateTime now)
        => item.Done ? AppText.Get("Done") : item.DueAt < now ? AppText.Get("Overdue") : item.DueAt.ToLocalTime().ToString("ddd d MMM HH:mm");
}

public sealed class TimetableViewModel : PageViewModel
{
    private readonly EduSphereClient _client;

    public TimetableViewModel(EduSphereClient client)
    {
        _client = client;
        LoadCommand = new AsyncCommand(LoadAsync);
    }

    /// <summary>Classes grouped by day, starting with today.</summary>
    public ObservableCollection<TimetableDay> Days { get; } = [];
    public AsyncCommand LoadCommand { get; }

    public Task LoadAsync() => RunAsync(async () =>
    {
        var slots = await _client.GetTimetableAsync();
        Days.Clear();
        foreach (var day in Group(slots, (int)DateTime.Now.DayOfWeek)) Days.Add(day);
    });

    public static IReadOnlyList<TimetableDay> Group(IEnumerable<ClassSlot> slots, int today)
        => slots.GroupBy(s => s.DayOfWeek)
            .OrderBy(g => (g.Key - today + 7) % 7)
            .Select(g => new TimetableDay(g.First().Day, g.Key == today, g.OrderBy(s => s.Start, StringComparer.Ordinal).ToList()))
            .ToList();
}

public sealed record TimetableDay(string Day, bool IsToday, IReadOnlyList<ClassSlot> Classes);

public sealed class CoursesViewModel : PageViewModel
{
    private readonly EduSphereClient _client;

    public CoursesViewModel(EduSphereClient client)
    {
        _client = client;
        LoadCommand = new AsyncCommand(LoadAsync);
    }

    public ObservableCollection<CourseSummary> Courses { get; } = [];
    public AsyncCommand LoadCommand { get; }

    public Task LoadAsync() => RunAsync(async () =>
    {
        var courses = await _client.GetCoursesAsync();
        Courses.Clear();
        foreach (var c in courses) Courses.Add(c);
    });
}

public sealed class CourseViewModel : PageViewModel
{
    private readonly EduSphereClient _client;
    private readonly ILauncherService _launcher;
    private readonly Func<string?> _portalBaseUrl;
    private CourseDetail? _detail;

    public CourseViewModel(EduSphereClient client, ILauncherService launcher, Func<string?> portalBaseUrl)
    {
        _client = client;
        _launcher = launcher;
        _portalBaseUrl = portalBaseUrl;
        OpenMaterialCommand = new AsyncCommand(p => p is CourseMaterial m ? OpenMaterialAsync(m) : Task.CompletedTask);
        OpenQuizCommand = new AsyncCommand(p => p is CourseQuiz q ? OpenQuizAsync(q) : Task.CompletedTask);
    }

    public Guid OfferingId { get; private set; }
    public CourseDetail? Detail { get => _detail; private set => Set(ref _detail, value); }
    public AsyncCommand OpenMaterialCommand { get; }
    public AsyncCommand OpenQuizCommand { get; }

    public Task LoadAsync(Guid offeringId) => RunAsync(async () =>
    {
        OfferingId = offeringId;
        Detail = await _client.GetCourseAsync(offeringId);
    });

    /// <summary>Links open in the browser; uploaded files are served by the API (the portal shows them to signed-in users).</summary>
    public Task OpenMaterialAsync(CourseMaterial material)
    {
        if (!string.IsNullOrWhiteSpace(material.LinkUrl) && Uri.TryCreate(material.LinkUrl, UriKind.Absolute, out var link)
            && (link.Scheme == Uri.UriSchemeHttps || link.Scheme == Uri.UriSchemeHttp))
            return _launcher.OpenAsync(link);
        return _portalBaseUrl() is { } portal ? _launcher.OpenAsync(new Uri($"{portal.TrimEnd('/')}/Portal/CourseMaterialStudent")) : Task.CompletedTask;
    }

    /// <summary>Quizzes are taken in the portal (timer, access codes, lockdown and review rules live there).</summary>
    public Task OpenQuizAsync(CourseQuiz quiz)
        => _portalBaseUrl() is { } portal ? _launcher.OpenAsync(new Uri($"{portal.TrimEnd('/')}/Portal/TakeQuiz?quizId={quiz.Id}")) : Task.CompletedTask;
}

public sealed class SubmitAssignmentViewModel : PageViewModel
{
    private readonly EduSphereClient _client;
    private readonly IPickers _pickers;
    private string _text = "";

    public SubmitAssignmentViewModel(EduSphereClient client, IPickers pickers)
    {
        _client = client;
        _pickers = pickers;
        TakePhotoCommand = new AsyncCommand(TakePhotoAsync);
        AddFilesCommand = new AsyncCommand(AddFilesAsync);
        SubmitCommand = new AsyncCommand(SubmitAsync);
    }

    public CourseAssignment? Assignment { get; set; }
    public string Text { get => _text; set => Set(ref _text, value); }
    public ObservableCollection<UploadFile> Files { get; } = [];
    public AsyncCommand TakePhotoCommand { get; }
    public AsyncCommand AddFilesCommand { get; }
    public AsyncCommand SubmitCommand { get; }

    /// <summary>Raised after the work was handed in.</summary>
    public event EventHandler? Submitted;

    public async Task TakePhotoAsync()
    {
        if (await _pickers.TakePhotoAsync() is { } photo) Files.Add(photo);
    }

    public async Task AddFilesAsync()
    {
        foreach (var f in await _pickers.PickFilesAsync()) Files.Add(f);
    }

    public async Task SubmitAsync()
    {
        if (Assignment is null) return;
        if (string.IsNullOrWhiteSpace(Text) && Files.Count == 0) { Error = AppText.Get("AddSomething"); return; }
        if (await RunAsync(() => _client.SubmitAssignmentAsync(Assignment.Id, Text, Files.ToList())))
        {
            Notice = AppText.Get("Submitted");
            Text = "";
            Files.Clear();
            Submitted?.Invoke(this, EventArgs.Empty);
        }
    }
}

public sealed class ResultsViewModel : PageViewModel
{
    private readonly EduSphereClient _client;

    public ResultsViewModel(EduSphereClient client)
    {
        _client = client;
        LoadCommand = new AsyncCommand(LoadAsync);
    }

    public ObservableCollection<ResultItem> Results { get; } = [];
    public AsyncCommand LoadCommand { get; }

    /// <summary>The signed-in student's results, or a child's (parents).</summary>
    public Task LoadAsync(Guid? childProfileId = null) => RunAsync(async () =>
    {
        var rows = await _client.GetResultsAsync(childProfileId);
        Results.Clear();
        foreach (var r in rows) Results.Add(r);
    });

    private Task LoadAsync() => LoadAsync(null);
}

public sealed class AttendanceViewModel : PageViewModel
{
    private readonly EduSphereClient _client;

    public AttendanceViewModel(EduSphereClient client)
    {
        _client = client;
        LoadCommand = new AsyncCommand(() => LoadAsync(null));
    }

    /// <summary>Per course: attended (present or late) out of marked classes, lowest first.</summary>
    public ObservableCollection<AttendanceCourse> Courses { get; } = [];
    public AsyncCommand LoadCommand { get; }

    public Task LoadAsync(Guid? childProfileId) => RunAsync(async () =>
    {
        var rows = await _client.GetAttendanceAsync(childProfileId);
        Courses.Clear();
        foreach (var c in rows) Courses.Add(c);
    });
}

public sealed class NotificationsViewModel : PageViewModel
{
    private const int PageSize = 20;
    private readonly EduSphereClient _client;
    private int _page;
    private bool _hasMore = true;

    public NotificationsViewModel(EduSphereClient client)
    {
        _client = client;
        LoadCommand = new AsyncCommand(LoadAsync);
        MoreCommand = new AsyncCommand(MoreAsync);
        MarkAllReadCommand = new AsyncCommand(MarkAllReadAsync);
        ReadCommand = new AsyncCommand(p => p is NotificationItem n ? ReadAsync(n) : Task.CompletedTask);
    }

    public ObservableCollection<NotificationItem> Items { get; } = [];
    public bool HasMore { get => _hasMore; private set => Set(ref _hasMore, value); }
    public AsyncCommand LoadCommand { get; }
    public AsyncCommand MoreCommand { get; }
    public AsyncCommand MarkAllReadCommand { get; }
    public AsyncCommand ReadCommand { get; }

    public Task LoadAsync() => RunAsync(async () =>
    {
        _page = 0;
        var items = await _client.GetNotificationsAsync(0, PageSize);
        Items.Clear();
        foreach (var n in items) Items.Add(n);
        HasMore = items.Count == PageSize;
    });

    public Task MoreAsync() => !HasMore ? Task.CompletedTask : RunAsync(async () =>
    {
        var items = await _client.GetNotificationsAsync(_page + 1, PageSize);
        _page++;
        foreach (var n in items) Items.Add(n);
        HasMore = items.Count == PageSize;
    });

    public Task MarkAllReadAsync() => RunAsync(async () =>
    {
        await _client.MarkAllNotificationsReadAsync();
        foreach (var n in Items) n.IsRead = true;
        var copy = Items.ToList();
        Items.Clear();
        foreach (var n in copy) Items.Add(n);
    });

    public Task ReadAsync(NotificationItem item) => item.IsRead ? Task.CompletedTask : RunAsync(async () =>
    {
        await _client.MarkNotificationReadAsync(item.Key);
        item.IsRead = true;
    });
}

/// <summary>A teacher marks a class: everyone starts as present, taps change the mark.</summary>
public sealed class TakeAttendanceViewModel : PageViewModel
{
    private readonly EduSphereClient _client;
    private DateTime _date = DateTime.Today;

    public TakeAttendanceViewModel(EduSphereClient client)
    {
        _client = client;
        SaveCommand = new AsyncCommand(SaveAsync);
    }

    public Guid OfferingId { get; private set; }
    public DateTime Date { get => _date; set => Set(ref _date, value); }
    public ObservableCollection<AttendanceMark> Students { get; } = [];
    public AsyncCommand SaveCommand { get; }

    public Task LoadAsync(Guid offeringId) => RunAsync(async () =>
    {
        OfferingId = offeringId;
        var roster = await _client.GetRosterAsync(offeringId);
        Students.Clear();
        foreach (var s in roster.OrderBy(s => s.StudentName)) Students.Add(new AttendanceMark(s.StudentProfileId, s.StudentName ?? s.RegistrationNumber ?? "", s.RegistrationNumber));
    });

    public async Task SaveAsync()
    {
        if (Students.Count == 0) return;
        if (await RunAsync(() => _client.TakeAttendanceAsync(OfferingId, Date, Students.Select(s => (s.StudentProfileId, s.Status)))))
            Notice = AppText.Get("AttendanceSaved");
    }
}

public sealed class AttendanceMark(Guid studentProfileId, string name, string? registrationNumber) : ObservableObject
{
    private int _status = 1;

    public Guid StudentProfileId { get; } = studentProfileId;
    public string Name { get; } = name;
    public string? RegistrationNumber { get; } = registrationNumber;

    /// <summary>1 present, 2 absent, 3 late, 4 excused.</summary>
    public int Status { get => _status; set { if (Set(ref _status, value)) OnPropertyChanged(nameof(StatusText)); } }

    public string StatusText => AppText.Get(Status switch { 2 => "Absent", 3 => "Late", 4 => "Excused", _ => "Present" });

    /// <summary>Present → absent → late → excused → present.</summary>
    public void Next() => Status = Status % 4 + 1;
}

/// <summary>Simplified grading: marks and feedback per submission (the portal's grading workspace has the full tools).</summary>
public sealed class GradingViewModel : PageViewModel
{
    private readonly EduSphereClient _client;

    public GradingViewModel(EduSphereClient client)
    {
        _client = client;
        SaveCommand = new AsyncCommand(p => p is GradeEntry e ? SaveAsync(e) : Task.CompletedTask);
    }

    public CourseAssignment? Assignment { get; private set; }
    public ObservableCollection<GradeEntry> Submissions { get; } = [];
    public AsyncCommand SaveCommand { get; }

    public Task LoadAsync(CourseAssignment assignment) => RunAsync(async () =>
    {
        Assignment = assignment;
        var rows = await _client.GetSubmissionsAsync(assignment.Id);
        Submissions.Clear();
        foreach (var s in rows.OrderBy(s => s.MarksAwarded.HasValue).ThenBy(s => s.StudentName))
            Submissions.Add(new GradeEntry(s.Key, s.StudentName ?? "", s.SubmittedAt, s.MarksAwarded, s.Feedback, assignment.MaxMarks));
    });

    public async Task SaveAsync(GradeEntry entry)
    {
        if (entry.Marks is { } m && (m < 0 || m > entry.MaxMarks)) { Error = $"0 – {entry.MaxMarks}"; return; }
        if (await RunAsync(() => _client.GradeAsync(entry.SubmissionId, entry.Marks, entry.Feedback)))
        {
            entry.Saved = true;
            Notice = AppText.Get("GradeSaved");
        }
    }
}

public sealed class GradeEntry(Guid submissionId, string student, DateTime? submittedAt, decimal? marks, string? feedback, decimal maxMarks) : ObservableObject
{
    private decimal? _marks = marks;
    private string? _feedback = feedback;
    private bool _saved = marks.HasValue;

    public Guid SubmissionId { get; } = submissionId;
    public string Student { get; } = student;
    public DateTime? SubmittedAt { get; } = submittedAt;
    public decimal MaxMarks { get; } = maxMarks;
    public decimal? Marks { get => _marks; set { if (Set(ref _marks, value)) Saved = false; } }
    public string? Feedback { get => _feedback; set { if (Set(ref _feedback, value)) Saved = false; } }
    public bool Saved { get => _saved; set => Set(ref _saved, value); }
}

public sealed class AnnouncementViewModel : PageViewModel
{
    private readonly EduSphereClient _client;
    private string _title = "";
    private string _body = "";

    public AnnouncementViewModel(EduSphereClient client)
    {
        _client = client;
        PostCommand = new AsyncCommand(PostAsync);
    }

    public Guid OfferingId { get; set; }
    public string Title { get => _title; set => Set(ref _title, value); }
    public string Body { get => _body; set => Set(ref _body, value); }
    public AsyncCommand PostCommand { get; }

    public async Task PostAsync()
    {
        if (string.IsNullOrWhiteSpace(Title) || string.IsNullOrWhiteSpace(Body)) { Error = AppText.Get("TitleAndMessage"); return; }
        if (await RunAsync(() => _client.PostAnnouncementAsync(OfferingId, Title.Trim(), Body.Trim())))
        {
            Notice = AppText.Get("AnnouncementPosted");
            Title = Body = "";
        }
    }
}

public sealed class ChildrenViewModel : PageViewModel
{
    private readonly EduSphereClient _client;

    public ChildrenViewModel(EduSphereClient client)
    {
        _client = client;
        LoadCommand = new AsyncCommand(LoadAsync);
    }

    public ObservableCollection<ChildSummary> Children { get; } = [];
    public AsyncCommand LoadCommand { get; }

    public Task LoadAsync() => RunAsync(async () =>
    {
        var rows = await _client.GetChildrenAsync();
        Children.Clear();
        foreach (var c in rows) Children.Add(c);
    });
}

/// <summary>Report cards (a student's own, or a child's for parents), newest first.</summary>
public sealed class ReportCardsViewModel : PageViewModel
{
    private readonly EduSphereClient _client;

    public ReportCardsViewModel(EduSphereClient client) => _client = client;

    public ObservableCollection<ReportCard> Cards { get; } = [];

    public Task LoadAsync(Guid? childProfileId = null) => RunAsync(async () =>
    {
        var cards = await _client.GetReportCardsAsync(childProfileId);
        Cards.Clear();
        foreach (var c in cards) Cards.Add(c);
    });
}

/// <summary>A course's discussions (pinned first, then newest) and starting a new one.</summary>
public sealed class DiscussionsViewModel : PageViewModel
{
    private readonly EduSphereClient _client;
    private string _title = "";
    private string _message = "";

    public DiscussionsViewModel(EduSphereClient client)
    {
        _client = client;
        StartCommand = new AsyncCommand(StartAsync);
    }

    public Guid OfferingId { get; private set; }
    public ObservableCollection<DiscussionThread> Threads { get; } = [];
    public string Title { get => _title; set => Set(ref _title, value); }
    public string Message { get => _message; set => Set(ref _message, value); }
    public AsyncCommand StartCommand { get; }

    /// <summary>Raised with the new discussion after it was started.</summary>
    public event EventHandler<DiscussionThread>? Started;

    public Task LoadAsync(Guid offeringId) => RunAsync(async () =>
    {
        OfferingId = offeringId;
        var threads = await _client.GetDiscussionsAsync(offeringId);
        Threads.Clear();
        foreach (var t in threads.OrderByDescending(t => t.IsPinned).ThenByDescending(t => t.CreatedAt)) Threads.Add(t);
    });

    public async Task StartAsync()
    {
        if (string.IsNullOrWhiteSpace(Title) || string.IsNullOrWhiteSpace(Message)) { Error = AppText.Get("TitleAndMessage"); return; }
        DiscussionThread? thread = null;
        if (await RunAsync(async () => thread = await _client.StartDiscussionAsync(OfferingId, Title.Trim(), Message.Trim())) && thread is not null)
        {
            Title = Message = "";
            Threads.Insert(0, thread);
            Started?.Invoke(this, thread);
        }
    }
}

/// <summary>One discussion: its messages oldest first, and replying.</summary>
public sealed class DiscussionViewModel : PageViewModel
{
    private readonly EduSphereClient _client;
    private DiscussionThread? _thread;
    private string _reply = "";

    public DiscussionViewModel(EduSphereClient client)
    {
        _client = client;
        ReplyCommand = new AsyncCommand(ReplyAsync);
    }

    public DiscussionThread? Thread { get => _thread; private set { if (Set(ref _thread, value)) OnPropertyChanged(nameof(CanReply)); } }
    public ObservableCollection<DiscussionReply> Messages { get; } = [];
    public string Reply { get => _reply; set => Set(ref _reply, value); }
    /// <summary>Closed discussions take no replies; solved ones only from teachers (the API decides and explains).</summary>
    public bool CanReply => Thread is { IsClosed: false };
    public AsyncCommand ReplyCommand { get; }

    public Task LoadAsync(Guid threadId) => RunAsync(async () =>
    {
        Thread = await _client.GetDiscussionAsync(threadId);
        Messages.Clear();
        foreach (var m in (Thread?.Replies ?? []).OrderBy(r => r.CreatedAt)) Messages.Add(m);
    });

    public async Task ReplyAsync()
    {
        if (Thread is null) return;
        if (string.IsNullOrWhiteSpace(Reply)) { Error = AppText.Get("WriteMessage"); return; }
        if (await RunAsync(() => _client.ReplyAsync(Thread.Id, Reply.Trim())))
        {
            Reply = "";
            await LoadAsync(Thread.Id);
        }
    }
}

/// <summary>One child for a parent: results, attendance, report cards and announcements.</summary>
public sealed class ChildViewModel(EduSphereClient client) : PageViewModel
{
    public ChildSummary? Child { get; private set; }
    public ResultsViewModel Results { get; } = new(client);
    public AttendanceViewModel Attendance { get; } = new(client);
    public ReportCardsViewModel ReportCards { get; } = new(client);
    public ObservableCollection<AnnouncementItem> Announcements { get; } = [];

    public async Task LoadAsync(ChildSummary child)
    {
        Child = child;
        OnPropertyChanged(nameof(Child));
        await Task.WhenAll(Results.LoadAsync(child.StudentProfileId), Attendance.LoadAsync(child.StudentProfileId),
            ReportCards.LoadAsync(child.StudentProfileId),
            RunAsync(async () =>
            {
                var items = await client.GetChildAnnouncementsAsync(child.StudentProfileId);
                Announcements.Clear();
                foreach (var a in items.Take(20)) Announcements.Add(a);
            }));
    }
}

public sealed class SettingsViewModel : PageViewModel
{
    private readonly EduSphereClient _client;
    private readonly IPushTokenProvider _push;
    private readonly AppSettings _settings;

    public SettingsViewModel(EduSphereClient client, IPushTokenProvider push, AppSettings settings)
    {
        _client = client;
        _push = push;
        _settings = settings;
        SignOutCommand = new AsyncCommand(SignOutAsync);
    }

    public string VersionText => string.Format(AppText.Get("Version"), _settings.AppVersion);
    public string Language { get => _client.Language; set { _client.Language = AppText.Language = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsRtl)); } }
    public bool IsRtl => AppText.IsRtl;
    public AsyncCommand SignOutCommand { get; }

    public async Task SignOutAsync() => await _client.SignOutAsync(await _push.GetTokenAsync());

    /// <summary>Registers the phone for push after sign-in (and when the language or version changes).</summary>
    public async Task RegisterForPushAsync(string? deviceModel)
    {
        var token = await _push.GetTokenAsync();
        if (string.IsNullOrEmpty(token) || !_client.IsSignedIn) return;
        await RunAsync(() => _client.RegisterDeviceAsync(token, deviceModel));
    }
}
