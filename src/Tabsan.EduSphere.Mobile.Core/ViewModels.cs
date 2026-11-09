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
    private bool _needsNewPassword;
    private string _newPassword = "";
    private string _confirmPassword = "";
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
    /// <summary>The account still has its temporary password (imported accounts): a new one is chosen before the app opens.</summary>
    public bool NeedsNewPassword { get => _needsNewPassword; private set => Set(ref _needsNewPassword, value); }
    public string NewPassword { get => _newPassword; set => Set(ref _newPassword, value); }
    public string ConfirmPassword { get => _confirmPassword; set => Set(ref _confirmPassword, value); }
    public MobileConfig? Config { get => _config; private set => Set(ref _config, value); }
    public VersionPolicy.State VersionState { get => _version; private set => Set(ref _version, value); }
    public ObservableCollection<SsoProvider> Providers { get; } = [];
    /// <summary>The institute's server, as shown under the form (with "Change server" unless the build is locked to it).</summary>
    public string? Server => _client.Server;
    public bool CanChangeServer => _client.CanChangeServer;

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
        if (NeedsNewPassword) { await ChangePasswordAsync(); return; }
        SignInResult? result = null;
        await RunAsync(async () => result = await _client.SignInAsync(Username.Trim(), Password, NeedsMfa ? MfaCode : null));
        if (result is null) return;
        Handle(result);
    }

    /// <summary>Replaces the temporary password, then signs in with the new one (fresh tokens after the change).</summary>
    private async Task ChangePasswordAsync()
    {
        if (string.IsNullOrEmpty(NewPassword) || NewPassword != ConfirmPassword) { Error = AppText.Get("PasswordsDiffer"); return; }
        SignInResult? result = null;
        await RunAsync(async () =>
        {
            await _client.ChangeTemporaryPasswordAsync(Password, NewPassword);
            result = await _client.SignInAsync(Username.Trim(), NewPassword, NeedsMfa ? MfaCode : null);
        });
        if (result is null) return;
        NeedsNewPassword = false;
        Password = NewPassword;
        NewPassword = ConfirmPassword = "";
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
            case SignInStatus.PasswordChangeRequired:
                NeedsNewPassword = true;
                Notice = AppText.Get("ChooseNewPassword");
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

/// <summary>
/// The institute's server, entered once (first start of the standard build, or "Change server"): checked before it is
/// kept, so a typo or an unreachable address is caught here and not on the sign-in page.
/// </summary>
public sealed class ServerViewModel : PageViewModel
{
    private readonly EduSphereClient _client;
    private string _address;

    public ServerViewModel(EduSphereClient client)
    {
        _client = client;
        _address = client.Server ?? "";
        ConnectCommand = new AsyncCommand(ConnectAsync);
    }

    /// <summary>What was typed: "edu.myschool.com", "192.168.1.10:5181", "https://api.myschool.edu" …</summary>
    public string Address { get => _address; set => Set(ref _address, value); }

    public AsyncCommand ConnectCommand { get; }

    /// <summary>Raised once the server is checked and kept, with its app configuration (institution name, sign-in options).</summary>
    public event EventHandler<MobileConfig>? Connected;

    public async Task ConnectAsync()
    {
        IsBusy = true;
        Error = null;
        try
        {
            var check = await _client.CheckServerAsync(Address);
            if (!check.IsEduSphere) { Error = check.Error; return; }
            await _client.UseServerAsync(check.Server!);
            Address = check.Server!;
            Connected?.Invoke(this, check.Config!);
        }
        catch (TaskCanceledException) { Error = AppText.Get("NoConnection"); }
        finally { IsBusy = false; }
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

    /// <summary>"Overdue" for open work past its date, otherwise the due date (a live class shows its start, even once begun).</summary>
    public static string DueLabel(DueItem item, DateTime now)
        => item.Done ? AppText.Get("Done")
           : item.DueAt < now && item.Kind != "LiveClass" ? AppText.Get("Overdue")
           : ShortDate(item.DueAt, now);

    /// <summary>A UTC date in the phone's time ("Sat 7 Nov 17:00"), with the year when it is not this year's.</summary>
    public static string ShortDate(DateTime utc, DateTime now)
    {
        var local = utc.ToLocalTime();
        return local.ToString(local.Year == now.ToLocalTime().Year ? "ddd d MMM HH:mm" : "d MMM yyyy HH:mm");
    }

    /// <summary>
    /// A student's standing: CGPA for University students, otherwise the average of published marks (the API sends CGPA
    /// only for University students; schools and colleges report percentages).
    /// </summary>
    public static (string Label, string Value) Standing(decimal? cgpa, decimal? averagePercent)
        => cgpa is { } g ? (AppText.Get("Cgpa"), g.ToString("0.00"))
           : (AppText.Get("Average"), averagePercent is { } p ? $"{p:0.#}%" : "—");
}

public sealed class TimetableViewModel : PageViewModel
{
    private readonly EduSphereClient _client;
    private readonly Selection? _selection;

    public TimetableViewModel(EduSphereClient client, Selection? selection = null)
    {
        _client = client;
        _selection = selection;
        LoadCommand = new AsyncCommand(LoadAsync);
        if (selection is not null) selection.Changed += (_, _) => _ = LoadAsync();
    }

    /// <summary>Classes grouped by day, starting with today.</summary>
    public ObservableCollection<TimetableDay> Days { get; } = [];
    public AsyncCommand LoadCommand { get; }

    public Task LoadAsync() => RunAsync(async () =>
    {
        var slots = await _client.GetTimetableAsync(_selection?.IsParent == true ? _selection.ChildId : null);
        Days.Clear();
        foreach (var day in Group(slots.Where(s => _selection?.Includes(s.OfferingId) ?? true), (int)DateTime.Now.DayOfWeek)) Days.Add(day);
    });

    public static IReadOnlyList<TimetableDay> Group(IEnumerable<ClassSlot> slots, int today, string? language = null)
        => slots.GroupBy(s => s.DayOfWeek)
            .OrderBy(g => (g.Key - today + 7) % 7)
            .Select(g => new TimetableDay(DayName(g.Key, language) ?? g.First().Day, g.Key == today, g.OrderBy(s => s.Start, StringComparer.Ordinal).ToList()))
            .ToList();

    /// <summary>The day in the app's language (the API names days in English); null for a number that is not a day.</summary>
    public static string? DayName(int dayOfWeek, string? language = null)
        => dayOfWeek is >= 0 and <= 6 ? AppText.Get(((DayOfWeek)dayOfWeek).ToString(), language ?? AppText.Language) : null;
}

public sealed record TimetableDay(string Day, bool IsToday, IReadOnlyList<ClassSlot> Classes);

public sealed class CoursesViewModel : PageViewModel
{
    private readonly EduSphereClient _client;
    private readonly Selection? _selection;

    public CoursesViewModel(EduSphereClient client, Selection? selection = null)
    {
        _client = client;
        _selection = selection;
        LoadCommand = new AsyncCommand(LoadAsync);
        if (selection is not null) selection.Changed += (_, _) => _ = LoadAsync();
    }

    public ObservableCollection<CourseSummary> Courses { get; } = [];
    public AsyncCommand LoadCommand { get; }

    public Task LoadAsync() => RunAsync(async () =>
    {
        var courses = await _client.GetCoursesAsync(_selection?.IsParent == true ? _selection.ChildId : null);
        Courses.Clear();
        foreach (var c in courses.Where(c => _selection?.Includes(c.OfferingId) ?? true)) Courses.Add(c);
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
    /// <summary>Parents: the child whose course this is (they see the child's work and cannot hand in).</summary>
    public Guid? ChildId { get; init; }
    public CourseDetail? Detail { get => _detail; private set => Set(ref _detail, value); }
    public AsyncCommand OpenMaterialCommand { get; }
    public AsyncCommand OpenQuizCommand { get; }

    public Task LoadAsync(Guid offeringId) => RunAsync(async () =>
    {
        OfferingId = offeringId;
        Detail = await _client.GetCourseAsync(offeringId, ChildId);
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

    /// <summary>What an assignment card says: teachers see how much waits to be graded, students their own work's state.</summary>
    /// <summary>The assignment's chip on a course page. A parent reads "Not handed in", as on the Homework tab: "Hand in" is
    /// the student's action, and a parent cannot hand in.</summary>
    public static string AssignmentState(CourseAssignment a, bool isTeacher, bool isParent = false)
        => isTeacher
            ? a.ToGrade switch { null => AppText.Get("ToGrade"), 0 => AppText.Get("NothingToGrade"), var n => string.Format(AppText.Get("ToGradeCount"), n) }
            : a.Marks is { } m ? $"{AppText.Get("Marks")}: {m:0.##}/{a.MaxMarks:0.##}"
            : a.Submitted ? AppText.Get("NotGradedYet")
            : AppText.Get(isParent ? "NotHandedIn" : "HandIn");
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
    private readonly Selection? _selection;

    public ResultsViewModel(EduSphereClient client, Selection? selection = null)
    {
        _client = client;
        _selection = selection;
        LoadCommand = new AsyncCommand(LoadAsync);
        if (selection is not null) selection.Changed += (_, _) => _ = LoadAsync(selection.IsParent ? selection.ChildId : null);
    }

    public ObservableCollection<ResultItem> Results { get; } = [];
    public AsyncCommand LoadCommand { get; }

    /// <summary>The signed-in student's results, or a child's (parents).</summary>
    public Task LoadAsync(Guid? childProfileId = null) => RunAsync(async () =>
    {
        if (_selection?.IsParent == true && childProfileId is null) { Results.Clear(); return; }
        var rows = await _client.GetResultsAsync(childProfileId);
        _selection?.AddClasses(rows.Select(r => r.SemesterName));
        Results.Clear();
        // A School child has the same subject in every class (CS001 in Class 1 … Class 10): newest class first, so the
        // current year leads and each class's subjects stay together; the card shows the class name.
        var ordered = rows.Where(r => _selection?.Includes(r.OfferingId, r.SemesterName) ?? true)
            .OrderBy(r => string.IsNullOrWhiteSpace(r.SemesterName))
            .ThenByDescending(r => r.SemesterName ?? "", Selection.ClassOrder.Instance)
            .ThenBy(r => r.CourseCode, StringComparer.OrdinalIgnoreCase);
        foreach (var r in ordered) Results.Add(r);
    });

    private Task LoadAsync() => LoadAsync(_selection?.IsParent == true ? _selection.ChildId : null);
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

    /// <summary>Ungraded work first, then by name. The submissions list names no one, so names come from the class roster.</summary>
    public Task LoadAsync(CourseAssignment assignment, Guid offeringId) => RunAsync(async () =>
    {
        Assignment = assignment;
        var rows = await _client.GetSubmissionsAsync(assignment.Id);
        var roster = (await _client.GetRosterAsync(offeringId)).GroupBy(s => s.StudentProfileId).ToDictionary(g => g.Key, g => g.First());
        string NameOf(SubmissionItem s) => s.StudentName
            ?? (roster.TryGetValue(s.StudentProfileId, out var r) ? r.StudentName ?? r.RegistrationNumber : null) ?? "";
        Submissions.Clear();
        foreach (var s in rows.OrderBy(s => s.MarksAwarded.HasValue).ThenBy(NameOf, StringComparer.CurrentCulture))
            Submissions.Add(new GradeEntry(s.Key, NameOf(s), s.SubmittedAt, s.MarksAwarded, s.Feedback, assignment.MaxMarks) { Answer = s.TextContent });
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
    /// <summary>The student's typed answer, if any (files open in the portal's grading workspace).</summary>
    public string? Answer { get; init; }
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
    public EduSphereClient Client => _client;
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

/// <summary>Homework of all courses in view (class and campus): students and parents with its state, teachers with what is left to grade.</summary>
public sealed class HomeworkViewModel : PageViewModel
{
    private readonly EduSphereClient _client;
    private readonly Selection _selection;

    public HomeworkViewModel(EduSphereClient client, Selection selection)
    {
        _client = client;
        _selection = selection;
        LoadCommand = new AsyncCommand(LoadAsync);
        selection.Changed += (_, _) => _ = LoadAsync();
    }

    public ObservableCollection<HomeworkItem> Items { get; } = [];
    public AsyncCommand LoadCommand { get; }

    public Task LoadAsync() => RunAsync(async () =>
    {
        var rows = _selection.IsParent && _selection.ChildId is null ? [] : await _client.GetHomeworkAsync(_selection.IsParent ? _selection.ChildId : null);
        Items.Clear();
        foreach (var h in Order(rows.Where(h => _selection.Includes(h.OfferingId, h.SemesterName)), DateTime.UtcNow)) Items.Add(h);
    });

    /// <summary>Still to do (soonest first), then the rest (most recent first).</summary>
    public static IEnumerable<HomeworkItem> Order(IEnumerable<HomeworkItem> items, DateTime now)
    {
        var list = items.ToList();
        bool Open(HomeworkItem h) => h.ToGrade > 0 || (!h.Submitted && h.DueDate >= now);
        return list.Where(Open).OrderBy(h => h.DueDate).Concat(list.Where(h => !Open(h)).OrderByDescending(h => h.DueDate));
    }

    /// <summary>The homework's state as the student, parent or teacher reads it (parents are not told to hand in).</summary>
    public static string State(HomeworkItem h, bool isTeacher, DateTime now, bool isParent = false)
        => isTeacher ? (h.ToGrade > 0 ? string.Format(AppText.Get("ToGradeCount"), h.ToGrade) : AppText.Get("NothingToGrade"))
           : h.Marks is { } m ? $"{AppText.Get("Marks")}: {m:0.##}/{h.MaxMarks:0.##}"
           : h.Submitted ? AppText.Get("NotGradedYet")
           : h.DueDate < now ? AppText.Get("Overdue") : AppText.Get(isParent ? "NotHandedIn" : "HandIn");
}

/// <summary>Attendance in view (class and campus): per course, overall and day by day — a student's own or a parent's child's.</summary>
public sealed class AttendanceScreenViewModel : PageViewModel
{
    private readonly EduSphereClient _client;
    private readonly Selection _selection;
    private decimal? _overall;

    public AttendanceScreenViewModel(EduSphereClient client, Selection selection)
    {
        _client = client;
        _selection = selection;
        LoadCommand = new AsyncCommand(LoadAsync);
        selection.Changed += (_, _) => _ = LoadAsync();
    }

    public ObservableCollection<AttendanceCourse> Courses { get; } = [];
    public ObservableCollection<AttendanceDay> Days { get; } = [];
    /// <summary>Attended (present or late) out of all marked classes in view.</summary>
    public decimal? Overall { get => _overall; private set => Set(ref _overall, value); }
    public AsyncCommand LoadCommand { get; }

    public Task LoadAsync() => RunAsync(async () =>
    {
        if (_selection.IsTeacher || (_selection.IsParent && _selection.ChildId is null))
        {
            Courses.Clear();
            Days.Clear();
            Overall = null;
            return;
        }
        var child = _selection.IsParent ? _selection.ChildId : null;
        var courses = await _client.GetAttendanceAsync(child);
        var days = await _client.GetAttendanceDaysAsync(child);
        _selection.AddClasses(days.Select(d => d.SemesterName));
        var shown = courses.Where(c => _selection.Includes(c.OfferingId, days.FirstOrDefault(d => d.OfferingId == c.OfferingId)?.SemesterName)).ToList();
        Courses.Clear();
        foreach (var c in shown) Courses.Add(c);
        Days.Clear();
        foreach (var d in days.Where(d => _selection.Includes(d.OfferingId, d.SemesterName)).Take(60)) Days.Add(d);
        Overall = OverallPercent(shown);
    });

    public static decimal? OverallPercent(IEnumerable<AttendanceCourse> courses)
    {
        var list = courses.ToList();
        var total = list.Sum(c => c.Total);
        return total == 0 ? null : Math.Round(100m * list.Sum(c => c.Attended) / total, 1);
    }
}

/// <summary>Admins: the chosen campus (or all of theirs) at a glance.</summary>
public sealed class CampusViewModel : PageViewModel
{
    private readonly EduSphereClient _client;
    private readonly Selection _selection;
    private CampusSummary? _summary;

    public CampusViewModel(EduSphereClient client, Selection selection)
    {
        _client = client;
        _selection = selection;
        selection.Changed += (_, _) => _ = LoadAsync();
    }

    public CampusSummary? Summary { get => _summary; private set => Set(ref _summary, value); }

    public Task LoadAsync() => RunAsync(async () => Summary = await _client.GetCampusSummaryAsync(_selection.Scope?.CampusId, DateTime.Now.Date));
}

/// <summary>Changing the password: the current one, then the new one twice (the API checks the password rules).</summary>
public sealed class ChangePasswordViewModel : PageViewModel
{
    private readonly EduSphereClient _client;
    private string _current = "", _new = "", _confirm = "";

    public ChangePasswordViewModel(EduSphereClient client)
    {
        _client = client;
        SaveCommand = new AsyncCommand(SaveAsync);
    }

    public string CurrentPassword { get => _current; set => Set(ref _current, value ?? ""); }
    public string NewPassword { get => _new; set => Set(ref _new, value ?? ""); }
    public string ConfirmPassword { get => _confirm; set => Set(ref _confirm, value ?? ""); }
    public AsyncCommand SaveCommand { get; }

    public async Task SaveAsync()
    {
        Notice = null;
        Error = string.IsNullOrEmpty(CurrentPassword) || string.IsNullOrEmpty(NewPassword) || string.IsNullOrEmpty(ConfirmPassword) ? AppText.Get("FillAllPasswords")
            : NewPassword != ConfirmPassword ? AppText.Get("PasswordsDiffer")
            : NewPassword == CurrentPassword ? AppText.Get("SamePassword")
            : null;
        if (Error is not null) return;
        if (await RunAsync(() => _client.ChangePasswordAsync(CurrentPassword, NewPassword)))
        {
            CurrentPassword = NewPassword = ConfirmPassword = "";
            Notice = AppText.Get("PasswordChanged");
        }
    }
}
