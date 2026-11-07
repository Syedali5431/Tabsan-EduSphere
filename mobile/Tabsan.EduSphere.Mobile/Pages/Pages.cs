using Microsoft.Maui.Controls.Shapes;
using Tabsan.EduSphere.Mobile.Core;

namespace Tabsan.EduSphere.Mobile.Pages;

// Plan M12 — the app's screens, built in C# and bound to the view models in Tabsan.EduSphere.Mobile.Core.
// The shared look (colours, cards, banner, chips, fields) is in Ui.cs.

// ── sign-in ───────────────────────────────────────────────────────────────────────────────────

public sealed class SignInPage : ContentPage
{
	private readonly SignInViewModel _vm;

	public event EventHandler? SignedIn;

	/// <summary>"Change server" was tapped (the app opens the server page).</summary>
	public event EventHandler? ChangeServer;

	public SignInPage(SignInViewModel vm, ILauncherService launcher, MauiProgram.WhiteLabel label)
	{
		_vm = vm;
		BindingContext = vm;
		Title = label.AppName;
		NavigationPage.SetHasNavigationBar(this, false);
		vm.SignedIn += (_, _) => SignedIn?.Invoke(this, EventArgs.Empty);

		// Top: the Tabsan logo, the institution and what the app is for.
		var institution = Ui.OnBanner(null, 22, bold: true);
		institution.HorizontalTextAlignment = TextAlignment.Center;
		institution.SetBinding(Label.TextProperty, new Binding("Config.InstitutionName") { FallbackValue = label.AppName, TargetNullValue = label.AppName });
		var header = Ui.Header(institution);

		// No suggestions or spell-check: keyboards must not change a username (Samsung added a space after the dot in "tmsgul.c9s1").
		var username = new Entry { Placeholder = Ui.T("Username"), Keyboard = Keyboard.Email, IsTextPredictionEnabled = false, IsSpellCheckEnabled = false, AutomationId = "username" };
		username.SetBinding(Entry.TextProperty, nameof(SignInViewModel.Username));
		var password = new Entry { Placeholder = Ui.T("Password"), IsPassword = true, AutomationId = "password" };
		password.SetBinding(Entry.TextProperty, nameof(SignInViewModel.Password));
		var mfa = new Entry { Placeholder = Ui.T("AuthenticatorCode"), Keyboard = Keyboard.Numeric };
		mfa.SetBinding(Entry.TextProperty, nameof(SignInViewModel.MfaCode));
		var mfaField = Ui.Field(mfa);
		mfaField.SetBinding(IsVisibleProperty, nameof(SignInViewModel.NeedsMfa));
		var newPassword = new Entry { Placeholder = Ui.T("NewPassword"), IsPassword = true, AutomationId = "newpassword" };
		newPassword.SetBinding(Entry.TextProperty, nameof(SignInViewModel.NewPassword));
		var newPasswordField = Ui.Field(newPassword);
		newPasswordField.SetBinding(IsVisibleProperty, nameof(SignInViewModel.NeedsNewPassword));
		var confirmPassword = new Entry { Placeholder = Ui.T("ConfirmPassword"), IsPassword = true, AutomationId = "confirmpassword" };
		confirmPassword.SetBinding(Entry.TextProperty, nameof(SignInViewModel.ConfirmPassword));
		var confirmField = Ui.Field(confirmPassword);
		confirmField.SetBinding(IsVisibleProperty, nameof(SignInViewModel.NeedsNewPassword));
		var signIn = Ui.Primary(Ui.T("SignIn"));
		signIn.AutomationId = "signin";
		signIn.Margin = new Thickness(0, 6, 0, 0);
		signIn.SetBinding(Button.CommandProperty, nameof(SignInViewModel.SignInCommand));

		// The institution's single sign-on buttons, under an "or" line when there are any.
		var or = Ui.Small(Ui.T("Or"));
		or.HorizontalTextAlignment = TextAlignment.Center;
		var providers = new VerticalStackLayout { Spacing = 8 };
		BindableLayout.SetItemsSource(providers, vm.Providers);
		BindableLayout.SetItemTemplate(providers, new DataTemplate(() =>
		{
			var b = Ui.Secondary("");
			b.SetBinding(Button.TextProperty, nameof(SsoProvider.DisplayName), stringFormat: Ui.T("SignInWith"));
			b.SetBinding(Button.CommandParameterProperty, nameof(SsoProvider.Provider));
			b.Command = vm.SingleSignOnCommand;
			return b;
		}));
		or.IsVisible = vm.Providers.Count > 0;
		vm.Providers.CollectionChanged += (_, _) => or.IsVisible = vm.Providers.Count > 0;

		var update = Ui.Secondary(Ui.T("Update"));
		update.IsVisible = false;
		update.Clicked += async (_, _) =>
		{
			var url = DeviceInfo.Platform == DevicePlatform.iOS ? vm.Config?.IosStoreUrl : vm.Config?.AndroidStoreUrl;
			if (Uri.TryCreate(url, UriKind.Absolute, out var store)) await launcher.OpenAsync(store);
		};
		vm.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(SignInViewModel.VersionState)) update.IsVisible = vm.VersionState != VersionPolicy.State.Current; };

		// Which institute's server this is, and (unless the build is locked to one) a way to switch.
		var server = Ui.Server(vm.Server, vm.CanChangeServer ? () => ChangeServer?.Invoke(this, EventArgs.Empty) : null);
		server.Margin = new Thickness(0, 4, 0, 0);

		var form = Ui.Card(new VerticalStackLayout
		{
			Spacing = 12,
			Children =
			{
				Ui.Title(Ui.T("SignInTitle"), 20), Ui.Small(Ui.T("SignInHint")), Ui.Busy(), Ui.Status(vm), Ui.Notice(),
				Ui.Field(username), Ui.Field(password), mfaField, newPasswordField, confirmField, signIn, or, providers, update, server
			}
		}, padding: 20);
		form.Margin = new Thickness(18, -48, 18, 0);

		Content = new ScrollView { Content = new VerticalStackLayout { Children = { header, form, Ui.Footer(label.AppVersion) } } };
		Ui.ApplyDirection(this);
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await _vm.LoadAsync();
	}
}

/// <summary>
/// The institute's server: the first page of the standard build (one app for every institute; each person enters their
/// institute's address once) and the page behind "Change server". The address is checked with the server before it is kept.
/// </summary>
public sealed class ServerPage : ContentPage
{
	public event EventHandler<MobileConfig>? Connected;

	public ServerPage(ServerViewModel vm, MauiProgram.WhiteLabel label, bool canGoBack)
	{
		BindingContext = vm;
		Title = Ui.T("Server");
		NavigationPage.SetHasNavigationBar(this, false);
		vm.Connected += (_, config) => Connected?.Invoke(this, config);

		var title = Ui.OnBanner(Ui.T("ServerTitle"), 22, bold: true);
		title.HorizontalTextAlignment = TextAlignment.Center;

		// A web address: no suggestions, spell-check or capitals (keyboards would change "edu.myschool.com").
		var address = new Entry
		{
			Placeholder = Ui.T("ServerAddress"), Keyboard = Keyboard.Url, IsTextPredictionEnabled = false, IsSpellCheckEnabled = false,
			ReturnType = ReturnType.Go, AutomationId = "server"
		};
		address.SetBinding(Entry.TextProperty, nameof(ServerViewModel.Address));
		address.ReturnCommand = vm.ConnectCommand;
		var connect = Ui.Primary(Ui.T("Connect"));
		connect.AutomationId = "connect";
		connect.Margin = new Thickness(0, 6, 0, 0);
		connect.SetBinding(Button.CommandProperty, nameof(ServerViewModel.ConnectCommand));

		var stack = new VerticalStackLayout
		{
			Spacing = 12,
			Children = { Ui.Title(Ui.T("ServerAddress"), 20), Ui.Small(Ui.T("ServerHint")), Ui.Busy(), Ui.Status(vm), Ui.Field(address), connect, Ui.Small(Ui.T("ServerExamples")) }
		};
		if (canGoBack)
		{
			var cancel = Ui.Secondary(Ui.T("Cancel"));
			cancel.Clicked += async (_, _) => await Navigation.PopAsync();
			stack.Add(cancel);
		}
		var form = Ui.Card(stack, padding: 20);
		form.Margin = new Thickness(18, -48, 18, 0);

		Content = new ScrollView { Content = new VerticalStackLayout { Children = { Ui.Header(title), form, Ui.Footer(label.AppVersion) } } };
		Ui.ApplyDirection(this);
	}
}

// ── home ──────────────────────────────────────────────────────────────────────────────────────

public sealed class HomePage : ContentPage
{
	private readonly HomeViewModel _vm;
	private readonly VerticalStackLayout _body = new() { Padding = new Thickness(16, 12, 16, 24), Spacing = 8 };
	private readonly EduSphereClient _client;
	private readonly ILauncherService _launcher;
	private readonly IPickers _pickers;
	private readonly Func<string?> _portal;
	private readonly Selection _selection;
	private readonly CampusViewModel? _campus;
	private readonly View _scopeBar;

	public HomePage(HomeViewModel vm, Selection selection, EduSphereClient client, ILauncherService launcher, IPickers pickers, Func<string?> portal)
	{
		_vm = vm;
		_selection = selection;
		_client = client;
		_launcher = launcher;
		_pickers = pickers;
		_portal = portal;
		_scopeBar = Ui.ScopeBar(selection);
		_scopeBar.Margin = new Thickness(0, 0, 0, 4);
		BindingContext = vm;
		Title = Ui.T("Home");
		var refresh = new RefreshView { Content = new ScrollView { Content = _body } };
		refresh.SetBinding(RefreshView.IsRefreshingProperty, nameof(PageViewModel.IsBusy));
		refresh.Command = vm.LoadCommand;
		Content = refresh;
		vm.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(HomeViewModel.Home)) Render(); };
		selection.Changed += (_, _) => Render();
		if (selection.IsAdmin)
		{
			_campus = new CampusViewModel(client, selection);
			_campus.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(CampusViewModel.Summary)) Render(); };
		}
		Ui.ApplyDirection(this);
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await Task.WhenAll(_vm.LoadAsync(), _campus?.LoadAsync() ?? Task.CompletedTask);
	}

	private void Render()
	{
		_body.Clear();
		_body.Add(Ui.Status(_vm));
		var home = _vm.Home;
		var greeting = new VerticalStackLayout
		{
			Spacing = 4,
			Children = { Ui.OnBanner(_vm.Greeting, 22, bold: true), Ui.OnBanner($"{Ui.RoleName(_client.Session?.Role)} · {Ui.Today()}", 14, opacity: 0.85) }
		};
		if (home is { UnreadNotifications: > 0 } h)
		{
			var unread = Ui.Chip(string.Format(Ui.T("Unread"), h.UnreadNotifications), Colors.White, onBanner: true);
			unread.Margin = new Thickness(0, 8, 0, 0);
			greeting.Add(unread);
		}
		_body.Add(Ui.Banner(greeting));
		if (_selection.IsTeacher || _selection.IsAdmin) _body.Add(_scopeBar);
		if (_campus?.Summary is { } campus)
		{
			// Admins: the chosen campus (or all of theirs) at a glance.
			_body.Add(Ui.Heading(campus.CampusName is { } name ? $"{campus.TenantName} — {name}" : Ui.T("AllCampuses")));
			_body.Add(Ui.Stats(
				Ui.Stat(Ui.T("Students"), campus.Students.ToString("N0"), Ui.Sky),
				Ui.Stat(Ui.T("Teachers"), campus.Teachers.ToString("N0"), Ui.Sky),
				Ui.Stat(Ui.T("Courses"), campus.Courses.ToString("N0"), Ui.Sky)));
			_body.Add(Ui.Stats(Ui.Stat($"{Ui.T("AttendanceToday")} · {string.Format(Ui.T("MarksToday"), campus.AttendanceTodayMarked)}",
				campus.AttendanceTodayPercent is { } p ? $"{p:0.#}%" : "—", Ui.PercentColor(campus.AttendanceTodayPercent))));
		}
		if (home is null) return;

		if (home.Student is { } s)
		{
			var (standing, standingValue) = HomeViewModel.Standing(s.Cgpa, s.AveragePercent);
			var fees = s.OutstandingFees is > 0 ? s.OutstandingFees.Value : 0;
			_body.Add(Ui.Stats(
				Ui.Stat(Ui.T("AttendanceRate"), s.AttendancePercent is { } a ? $"{a:0.#}%" : "—", Ui.PercentColor(s.AttendancePercent)),
				Ui.Stat(standing, standingValue, Ui.Sky),
				Ui.Stat(Ui.T("Fees"), fees.ToString("N0"), fees > 0 ? Ui.Bad : Ui.Good)));
		}
		if (home.Teacher is { } t)
			_body.Add(Ui.Stats(
				Ui.Stat(Ui.T("Courses"), t.Courses.ToString(), Ui.Sky),
				Ui.Stat(Ui.T("ToGrade"), t.SubmissionsToGrade.ToString(), t.SubmissionsToGrade > 0 ? Ui.Warn : Ui.Good)));

		if (home.Children is { Count: > 0 } children)
		{
			_body.Add(Ui.Heading(Ui.T("Children")));
			foreach (var c in children)
			{
				var card = ChildCard(c);
				var tap = new TapGestureRecognizer();
				tap.Tapped += async (_, _) => await Navigation.PushAsync(new ChildPage(new ChildViewModel(_client), c));
				card.GestureRecognizers.Add(tap);
				_body.Add(card);
			}
		}

		if (!_vm.IsParent && !_selection.IsAdmin)
		{
			// Teachers with several classes or campuses see the ones chosen above.
			var today = home.Today.Where(c => _selection.Includes(c.OfferingId)).ToList();
			var due = home.Due.Where(d => _selection.Includes(d.OfferingId)).ToList();
			_body.Add(Ui.Heading(Ui.T("Today")));
			if (today.Count == 0) _body.Add(Ui.Empty(Ui.T("NoClassesToday")));
			foreach (var c in today) _body.Add(Ui.ClassCard(c));

			_body.Add(Ui.Heading(Ui.T("Due")));
			if (due.Count == 0) _body.Add(Ui.Empty(Ui.T("NothingDue")));
			foreach (var d in due)
			{
				var label = HomeViewModel.DueLabel(d, DateTime.UtcNow);
				var color = d.Done ? Ui.Good : label == Ui.T("Overdue") ? Ui.Bad : Ui.Accent;
				var card = Ui.Card(Ui.Row(null, Ui.Lines(Ui.Title(d.Title), Ui.Small(d.CourseTitle)), Ui.Chip(label, color)));
				var tap = new TapGestureRecognizer();
				tap.Tapped += async (_, _) => await OpenCourseAsync(d.OfferingId);
				card.GestureRecognizers.Add(tap);
				_body.Add(card);
			}
		}

		if (home.Announcements.Count > 0)
		{
			_body.Add(Ui.Heading(Ui.T("Announcements")));
			foreach (var a in home.Announcements) _body.Add(Ui.AnnouncementCard(a, withCourse: true));
		}
	}

	/// <summary>A child at a glance: name, class, and attendance, marks and fees as chips.</summary>
	internal static Border ChildCard(ChildSummary c)
	{
		var (standing, standingValue) = HomeViewModel.Standing(c.Cgpa, c.AveragePercent);
		var fees = c.OutstandingFees ?? 0;
		var chips = new FlexLayout { Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap, Margin = new Thickness(0, 6, 0, 0) };
		foreach (var chip in new[]
		{
			Ui.Chip($"{Ui.T("AttendanceRate")} {(c.AttendancePercent is { } p ? $"{p:0.#}%" : "—")}", Ui.PercentColor(c.AttendancePercent)),
			Ui.Chip($"{standing} {standingValue}", Ui.Sky),
			Ui.Chip($"{Ui.T("Fees")} {fees:N0}", fees > 0 ? Ui.Bad : Ui.Good)
		})
		{
			chip.Margin = new Thickness(0, 0, 6, 6);
			chips.Add(chip);
		}
		var details = Ui.Lines(Ui.Title(c.Name), Ui.Small(string.Join(" · ", new[] { c.RegistrationNumber, c.ProgramName }.Where(x => !string.IsNullOrWhiteSpace(x)))), chips);
		return Ui.Card(Ui.Row(Ui.Avatar(c.Name, Ui.Accent), details, Ui.Chevron()));
	}

	private Task OpenCourseAsync(Guid offeringId)
		=> Navigation.PushAsync(new CoursePage(new CourseViewModel(_client, _launcher, _portal), offeringId, _client, _pickers));
}

// ── timetable ─────────────────────────────────────────────────────────────────────────────────

public sealed class TimetablePage : ContentPage
{
	private readonly TimetableViewModel _vm;

	public TimetablePage(TimetableViewModel vm, Selection selection)
	{
		_vm = vm;
		BindingContext = vm;
		Title = Ui.T("Timetable");
		var list = new CollectionView
		{
			ItemsSource = vm.Days,
			ItemTemplate = new DataTemplate(() =>
			{
				var day = Ui.Heading("");
				day.Margin = new Thickness(0);
				day.SetBinding(Label.TextProperty, nameof(TimetableDay.Day));
				var today = Ui.Chip(Ui.T("Today"), Ui.Accent);
				today.SetBinding(IsVisibleProperty, nameof(TimetableDay.IsToday));
				var header = new HorizontalStackLayout { Spacing = 10, Margin = new Thickness(2, 18, 2, 4), Children = { day, today } };
				var classes = new VerticalStackLayout { Spacing = 0 };
				classes.SetBinding(BindableLayout.ItemsSourceProperty, nameof(TimetableDay.Classes));
				BindableLayout.SetItemTemplate(classes, new DataTemplate(() =>
				{
					var start = new Label();
					start.SetBinding(Label.TextProperty, nameof(ClassSlot.Start));
					var end = Ui.Small();
					end.SetBinding(Label.TextProperty, nameof(ClassSlot.End));
					var subject = Ui.Title();
					subject.SetBinding(Label.TextProperty, nameof(ClassSlot.Subject));
					var details = Ui.Small();
					details.SetBinding(Label.TextProperty, nameof(ClassSlot.Details));
					return Ui.ClassCard(start, end, subject, details);
				}));
				return new VerticalStackLayout { Padding = new Thickness(16, 0), Children = { header, classes } };
			}),
			EmptyView = new ContentView { Padding = 16, Content = Ui.Empty(Ui.T("NoClasses")) },
			Footer = new ContentView { HeightRequest = 24 }
		};
		Content = Ui.ListLayout(vm, Ui.ScopeBar(selection), list);
		Ui.ApplyDirection(this);
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await _vm.LoadAsync();
	}
}

// ── courses ───────────────────────────────────────────────────────────────────────────────────

public sealed class CoursesPage : ContentPage
{
	private readonly CoursesViewModel _vm;

	public CoursesPage(CoursesViewModel vm, Selection selection, EduSphereClient client, ILauncherService launcher, IPickers pickers, Func<string?> portal)
	{
		_vm = vm;
		BindingContext = vm;
		Title = Ui.T("Courses");
		var list = new CollectionView
		{
			ItemsSource = vm.Courses,
			SelectionMode = SelectionMode.Single,
			Footer = new ContentView { HeightRequest = 16 },
			EmptyView = new ContentView { Padding = 16, Content = Ui.Empty(Ui.T("NoCourses")) },
			ItemTemplate = new DataTemplate(() =>
			{
				var initials = new Label();
				initials.SetBinding(Label.TextProperty, nameof(CourseSummary.CourseTitle), converter: new InitialsOf());
				var title = Ui.Title();
				title.SetBinding(Label.TextProperty, nameof(CourseSummary.CourseTitle));
				var code = Ui.Small();
				code.SetBinding(Label.TextProperty, new MultiBinding { Bindings = { new Binding(nameof(CourseSummary.CourseCode)), new Binding(nameof(CourseSummary.SemesterName)) }, StringFormat = "{0} · {1}" });
				var teacher = Ui.Small();
				teacher.SetBinding(Label.TextProperty, nameof(CourseSummary.Teacher));
				return new ContentView { Padding = new Thickness(16, 0), Content = Ui.Card(Ui.Row(Ui.Avatar(initials, Ui.Sky), Ui.Lines(title, code, teacher), Ui.Chevron())) };
			})
		};
		list.SelectionChanged += async (_, e) =>
		{
			if (e.CurrentSelection.FirstOrDefault() is CourseSummary course)
			{
				list.SelectedItem = null;
				var childId = selection.IsParent ? selection.ChildId : null;
				await Navigation.PushAsync(new CoursePage(new CourseViewModel(client, launcher, portal) { ChildId = childId }, course.OfferingId, client, pickers));
			}
		};
		Content = Ui.ListLayout(vm, Ui.ScopeBar(selection), list);
		Ui.ApplyDirection(this);
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await _vm.LoadAsync();
	}
}

/// <summary>A course: materials, assignments (hand in / grade), quizzes (in the portal), announcements; teachers also take attendance and post.</summary>
public sealed class CoursePage : ContentPage
{
	private readonly CourseViewModel _vm;
	private readonly Guid _offeringId;
	private readonly EduSphereClient _client;
	private readonly IPickers _pickers;
	private readonly VerticalStackLayout _body = new() { Padding = new Thickness(16, 12, 16, 24), Spacing = 8 };

	public CoursePage(CourseViewModel vm, Guid offeringId, EduSphereClient client, IPickers pickers)
	{
		_vm = vm;
		_offeringId = offeringId;
		_client = client;
		_pickers = pickers;
		BindingContext = vm;
		Content = new ScrollView { Content = _body };
		vm.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(CourseViewModel.Detail)) Render(); };
		Ui.ApplyDirection(this);
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await _vm.LoadAsync(_offeringId);
	}

	private void Render()
	{
		_body.Clear();
		_body.Add(Ui.Status(_vm));
		var d = _vm.Detail;
		if (d is null) return;
		Title = d.Course.Title;
		var sub = string.Join(" · ", new[] { d.Course.CourseCode, d.Course.SemesterName, d.Course.Teacher }.Where(x => !string.IsNullOrWhiteSpace(x)));
		_body.Add(Ui.Banner(new VerticalStackLayout { Spacing = 4, Children = { Ui.OnBanner(d.Course.CourseTitle, 20, bold: true), Ui.OnBanner(sub, 13, opacity: 0.85) } }));

		var actions = new FlexLayout { Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap };
		void Action(Button b, Func<Task> open)
		{
			b.Margin = new Thickness(0, 0, 8, 8);
			b.Clicked += async (_, _) => await open();
			actions.Add(b);
		}
		if (d.IsTeacher)
		{
			Action(Ui.Primary(Ui.T("TakeAttendance")), () => Navigation.PushAsync(new TakeAttendancePage(new TakeAttendanceViewModel(_client), _offeringId)));
			Action(Ui.Secondary(Ui.T("NewAnnouncement")), () => Navigation.PushAsync(new AnnouncementPage(new AnnouncementViewModel(_client) { OfferingId = _offeringId })));
		}
		var parent = _vm.ChildId is not null;
		if (!parent) Action(Ui.Secondary(Ui.T("Discussions")), () => Navigation.PushAsync(new DiscussionsPage(new DiscussionsViewModel(_client), _offeringId, _client)));
		if (actions.Children.Count > 0) _body.Add(actions);

		if (d.Assignments.Count > 0) _body.Add(Ui.Heading(Ui.T("Assignments")));
		foreach (var a in d.Assignments)
		{
			var state = CourseViewModel.AssignmentState(a, d.IsTeacher);
			var color = d.IsTeacher ? (a.ToGrade is > 0 ? Ui.Warn : Ui.Good)
				: a.Marks is not null ? Ui.Good : a.Submitted ? Ui.Sky : a.DueDate < DateTime.UtcNow ? Ui.Bad : Ui.Accent;
			var card = Ui.Card(Ui.Row(null, Ui.Lines(Ui.Title(a.Title), Ui.Small($"{Ui.T("Due")}: {Ui.Date(a.DueDate)}")), Ui.Chip(state, color)));
			var tap = new TapGestureRecognizer();
			tap.Tapped += async (_, _) =>
			{
				if (d.IsTeacher) await Navigation.PushAsync(new GradingPage(new GradingViewModel(_client), a, _offeringId));
				else if (!parent) await Navigation.PushAsync(new SubmitPage(new SubmitAssignmentViewModel(_client, _pickers) { Assignment = a }));
			};
			card.GestureRecognizers.Add(tap);
			_body.Add(card);
		}

		if (d.Materials.Count > 0) _body.Add(Ui.Heading(Ui.T("Materials")));
		foreach (var m in d.Materials)
		{
			var open = Ui.Small(Ui.T("Open"));
			open.TextColor = Ui.Accent;
			open.FontFamily = "OpenSansSemibold";
			var card = Ui.Card(Ui.Row(Ui.Avatar(new Label { Text = string.IsNullOrWhiteSpace(m.Type) ? "•" : m.Type[..1].ToUpperInvariant() }, Ui.Accent, 36), Ui.Lines(Ui.Title(m.Title), Ui.Small(m.Description)), open), padding: 12);
			card.GestureRecognizers.Add(new TapGestureRecognizer { Command = _vm.OpenMaterialCommand, CommandParameter = m });
			_body.Add(card);
		}

		if (d.Quizzes.Count > 0)
		{
			_body.Add(Ui.Heading(Ui.T("Quizzes")));
			_body.Add(Ui.Small(Ui.T("QuizInPortal")));
			foreach (var q in d.Quizzes)
			{
				var card = Ui.Card(Ui.Row(null, Ui.Title(q.Title), Ui.Chip($"{q.AttemptsUsed}/{q.MaxAttempts}", Ui.Sky)));
				card.GestureRecognizers.Add(new TapGestureRecognizer { Command = _vm.OpenQuizCommand, CommandParameter = q });
				_body.Add(card);
			}
		}

		if (d.Announcements.Count > 0)
		{
			_body.Add(Ui.Heading(Ui.T("Announcements")));
			foreach (var a in d.Announcements) _body.Add(Ui.AnnouncementCard(a, withCourse: false));
		}
	}
}

public sealed class SubmitPage : ContentPage
{
	public SubmitPage(SubmitAssignmentViewModel vm)
	{
		BindingContext = vm;
		Title = vm.Assignment?.Title ?? Ui.T("HandIn");
		var text = new Editor { Placeholder = Ui.T("YourAnswer"), AutoSize = EditorAutoSizeOption.TextChanges, MinimumHeightRequest = 140 };
		text.SetBinding(Editor.TextProperty, nameof(SubmitAssignmentViewModel.Text));
		var files = new FlexLayout { Wrap = Microsoft.Maui.Layouts.FlexWrap.Wrap };
		BindableLayout.SetItemsSource(files, vm.Files);
		BindableLayout.SetItemTemplate(files, new DataTemplate(() =>
		{
			var chip = Ui.Chip("", Ui.Sky);
			chip.Margin = new Thickness(0, 0, 6, 6);
			((Label)chip.Content!).SetBinding(Label.TextProperty, nameof(UploadFile.FileName));
			return chip;
		}));
		vm.Submitted += async (_, _) => await Navigation.PopAsync();
		var photo = Ui.Secondary(Ui.T("TakePhoto"));
		photo.Command = vm.TakePhotoCommand;
		var file = Ui.Secondary(Ui.T("AddFile"));
		file.Command = vm.AddFilesCommand;
		var handIn = Ui.Primary(Ui.T("HandIn"));
		handIn.Command = vm.SubmitCommand;
		var details = Ui.Lines(Ui.Title(vm.Assignment?.Title), Ui.Body(vm.Assignment?.Description));
		if (vm.Assignment is { } a) details.Add(Ui.Chip($"{Ui.T("Due")}: {Ui.Date(a.DueDate)}", Ui.Accent));
		var about = Ui.Card(details);
		var page = Ui.Page(Title, vm,
			about,
			Ui.Field(text),
			Ui.Stats(photo, file),
			files,
			handIn);
		Content = page.Content;
		Ui.ApplyDirection(this);
	}
}

// ── discussions (messaging between teachers and students about a course) ──────────────────────

public sealed class DiscussionsPage : ContentPage
{
	private readonly DiscussionsViewModel _vm;
	private readonly Guid _offeringId;

	public DiscussionsPage(DiscussionsViewModel vm, Guid offeringId, EduSphereClient client)
	{
		_vm = vm;
		_offeringId = offeringId;
		BindingContext = vm;
		Title = Ui.T("Discussions");
		var list = new VerticalStackLayout { Spacing = 0 };
		BindableLayout.SetItemsSource(list, vm.Threads);
		BindableLayout.SetEmptyView(list, Ui.Empty(Ui.T("NoDiscussions")));
		BindableLayout.SetItemTemplate(list, new DataTemplate(() =>
		{
			var initials = new Label();
			initials.SetBinding(Label.TextProperty, nameof(DiscussionThread.AuthorName), converter: new InitialsOf());
			var title = Ui.Title();
			title.SetBinding(Label.TextProperty, nameof(DiscussionThread.Title));
			var meta = Ui.Small();
			meta.SetBinding(Label.TextProperty, new MultiBinding
			{
				Bindings = { new Binding(nameof(DiscussionThread.AuthorName)), new Binding(nameof(DiscussionThread.ReplyCount)) },
				StringFormat = "{0} · " + Ui.T("Replies").Replace("{0}", "{1}")
			});
			var pinned = Ui.Chip(Ui.T("Pinned"), Ui.Warn);
			pinned.SetBinding(IsVisibleProperty, nameof(DiscussionThread.IsPinned));
			var card = Ui.Card(Ui.Row(Ui.Avatar(initials, Ui.Sky, 38), Ui.Lines(title, meta), pinned));
			var tap = new TapGestureRecognizer();
			tap.Tapped += async (s, _) =>
			{
				if ((s as BindableObject)?.BindingContext is DiscussionThread t)
					await Navigation.PushAsync(new DiscussionPage(new DiscussionViewModel(client), t.Id));
			};
			card.GestureRecognizers.Add(tap);
			return card;
		}));
		var newTitle = new Entry { Placeholder = Ui.T("Title") };
		newTitle.SetBinding(Entry.TextProperty, nameof(DiscussionsViewModel.Title));
		var newMessage = new Editor { Placeholder = Ui.T("Message"), AutoSize = EditorAutoSizeOption.TextChanges, MinimumHeightRequest = 100 };
		newMessage.SetBinding(Editor.TextProperty, nameof(DiscussionsViewModel.Message));
		var start = Ui.Primary(Ui.T("Start"));
		start.Command = vm.StartCommand;
		vm.Started += async (_, thread) => await Navigation.PushAsync(new DiscussionPage(new DiscussionViewModel(client), thread.Id));
		var compose = Ui.Card(new VerticalStackLayout { Spacing = 10, Children = { Ui.Title(Ui.T("NewDiscussion")), Ui.Field(newTitle), Ui.Field(newMessage), start } }, padding: 16);
		Content = Ui.Page(Title, vm, list, compose).Content;
		Ui.ApplyDirection(this);
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await _vm.LoadAsync(_offeringId);
	}
}

public sealed class DiscussionPage : ContentPage
{
	private readonly DiscussionViewModel _vm;
	private readonly Guid _threadId;

	public DiscussionPage(DiscussionViewModel vm, Guid threadId)
	{
		_vm = vm;
		_threadId = threadId;
		BindingContext = vm;
		Title = Ui.T("Discussions");
		var messages = new VerticalStackLayout { Spacing = 0 };
		BindableLayout.SetItemsSource(messages, vm.Messages);
		BindableLayout.SetItemTemplate(messages, new DataTemplate(() =>
		{
			var initials = new Label();
			initials.SetBinding(Label.TextProperty, nameof(DiscussionReply.AuthorName), converter: new InitialsOf());
			var who = Ui.Title(size: 14);
			who.SetBinding(Label.TextProperty, nameof(DiscussionReply.AuthorName));
			var when = Ui.Small();
			when.SetBinding(Label.TextProperty, nameof(DiscussionReply.CreatedAt), converter: new ShortLocalDate());
			var body = Ui.Body();
			body.SetBinding(Label.TextProperty, nameof(DiscussionReply.Body));
			var avatar = Ui.Avatar(initials, Ui.Accent, 34);
			avatar.VerticalOptions = LayoutOptions.Start;
			return Ui.Card(Ui.Row(avatar, Ui.Lines(who, when, body)), padding: 12);
		}));
		var heading = Ui.Title(size: 19);
		heading.SetBinding(Label.TextProperty, $"{nameof(DiscussionViewModel.Thread)}.{nameof(DiscussionThread.Title)}");
		var reply = new Editor { Placeholder = Ui.T("WriteReply"), AutoSize = EditorAutoSizeOption.TextChanges, MinimumHeightRequest = 80 };
		reply.SetBinding(Editor.TextProperty, nameof(DiscussionViewModel.Reply));
		var send = Ui.Primary(Ui.T("Send"));
		send.Command = vm.ReplyCommand;
		var replyBox = Ui.Card(new VerticalStackLayout { Spacing = 10, Children = { Ui.Field(reply), send } }, padding: 14);
		replyBox.SetBinding(VisualElement.IsVisibleProperty, nameof(DiscussionViewModel.CanReply));
		var closed = Ui.Empty(Ui.T("DiscussionClosed"));
		closed.SetBinding(VisualElement.IsVisibleProperty, nameof(DiscussionViewModel.CanReply), converter: new Not());
		Content = Ui.Page(Title, vm, heading, messages, replyBox, closed).Content;
		Ui.ApplyDirection(this);
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await _vm.LoadAsync(_threadId);
	}
}

// ── teachers ──────────────────────────────────────────────────────────────────────────────────

public sealed class TakeAttendancePage : ContentPage
{
	private readonly TakeAttendanceViewModel _vm;
	private readonly Guid _offeringId;

	public TakeAttendancePage(TakeAttendanceViewModel vm, Guid offeringId)
	{
		_vm = vm;
		_offeringId = offeringId;
		BindingContext = vm;
		Title = Ui.T("TakeAttendance");
		var date = new DatePicker();
		date.SetBinding(DatePicker.DateProperty, nameof(TakeAttendanceViewModel.Date));
		var students = new VerticalStackLayout { Spacing = 0 };
		BindableLayout.SetItemsSource(students, vm.Students);
		BindableLayout.SetItemTemplate(students, new DataTemplate(() =>
		{
			var initials = new Label();
			initials.SetBinding(Label.TextProperty, nameof(AttendanceMark.Name), converter: new InitialsOf());
			var name = Ui.Title(size: 14);
			name.SetBinding(Label.TextProperty, nameof(AttendanceMark.Name));
			var number = Ui.Small();
			number.SetBinding(Label.TextProperty, nameof(AttendanceMark.RegistrationNumber));
			var status = new Button { WidthRequest = 112, Padding = new Thickness(8, 6), MinimumHeightRequest = 40, FontSize = 13 };
			status.SetBinding(Button.TextProperty, nameof(AttendanceMark.StatusText));
			status.SetBinding(VisualElement.BackgroundColorProperty, nameof(AttendanceMark.Status), converter: new AttendanceColor());
			status.Clicked += (s, _) => ((s as Button)?.BindingContext as AttendanceMark)?.Next();
			return Ui.Card(Ui.Row(Ui.Avatar(initials, Ui.Sky, 36), Ui.Lines(name, number), status), padding: 10);
		}));
		var save = Ui.Primary(Ui.T("Save"));
		save.Command = vm.SaveCommand;
		Content = Ui.Page(Title, vm, Ui.Field(date), students, save).Content;
		Ui.ApplyDirection(this);
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		if (_vm.Students.Count == 0) await _vm.LoadAsync(_offeringId);
	}

	/// <summary>Present green, absent red, late amber, excused blue.</summary>
	private sealed class AttendanceColor : IValueConverter
	{
		public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
			=> value switch { 2 => Ui.Bad, 3 => Ui.Warn, 4 => Ui.Sky, _ => Ui.Good };
		public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture) => throw new NotSupportedException();
	}
}

public sealed class GradingPage : ContentPage
{
	private readonly GradingViewModel _vm;
	private readonly CourseAssignment _assignment;
	private readonly Guid _offeringId;

	public GradingPage(GradingViewModel vm, CourseAssignment assignment, Guid offeringId)
	{
		_vm = vm;
		_assignment = assignment;
		_offeringId = offeringId;
		BindingContext = vm;
		Title = assignment.Title;
		var list = new VerticalStackLayout { Spacing = 0 };
		BindableLayout.SetItemsSource(list, vm.Submissions);
		BindableLayout.SetItemTemplate(list, new DataTemplate(() =>
		{
			var initials = new Label();
			initials.SetBinding(Label.TextProperty, nameof(GradeEntry.Student), converter: new InitialsOf());
			var who = Ui.Title();
			who.SetBinding(Label.TextProperty, nameof(GradeEntry.Student));
			var answer = Ui.Body();
			answer.MaxLines = 6;
			answer.LineBreakMode = LineBreakMode.TailTruncation;
			answer.SetBinding(Label.TextProperty, nameof(GradeEntry.Answer));
			var answerBox = new Border { Content = answer, Padding = 10, StrokeThickness = 0, StrokeShape = new RoundRectangle { CornerRadius = 10 }, BackgroundColor = Ui.Sky.WithAlpha(0.08f) };
			answerBox.SetBinding(IsVisibleProperty, nameof(GradeEntry.Answer), converter: new NotEmpty());
			var marks = new Entry { Placeholder = $"{Ui.T("Marks")} / {assignment.MaxMarks:0.##}", Keyboard = Keyboard.Numeric };
			marks.SetBinding(Entry.TextProperty, nameof(GradeEntry.Marks));
			var feedback = new Editor { Placeholder = Ui.T("Feedback"), AutoSize = EditorAutoSizeOption.TextChanges, MinimumHeightRequest = 70 };
			feedback.SetBinding(Editor.TextProperty, nameof(GradeEntry.Feedback));
			var save = Ui.Primary(Ui.T("SaveGrade"));
			save.Command = vm.SaveCommand;
			save.SetBinding(Button.CommandParameterProperty, ".");
			return Ui.Card(new VerticalStackLayout { Spacing = 10, Children = { Ui.Row(Ui.Avatar(initials, Ui.Sky, 36), who), answerBox, Ui.Field(marks), Ui.Field(feedback), save } }, padding: 14);
		}));
		Content = Ui.Page(Title, vm, list).Content;
		Ui.ApplyDirection(this);
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		if (_vm.Submissions.Count == 0) await _vm.LoadAsync(_assignment, _offeringId);
	}
}

public sealed class AnnouncementPage : ContentPage
{
	public AnnouncementPage(AnnouncementViewModel vm)
	{
		BindingContext = vm;
		Title = Ui.T("NewAnnouncement");
		var title = new Entry { Placeholder = Ui.T("Title") };
		title.SetBinding(Entry.TextProperty, nameof(AnnouncementViewModel.Title));
		var body = new Editor { Placeholder = Ui.T("Message"), AutoSize = EditorAutoSizeOption.TextChanges, MinimumHeightRequest = 160 };
		body.SetBinding(Editor.TextProperty, nameof(AnnouncementViewModel.Body));
		var post = Ui.Primary(Ui.T("Post"));
		post.Command = vm.PostCommand;
		Content = Ui.Page(Title, vm, Ui.Field(title), Ui.Field(body), post).Content;
		Ui.ApplyDirection(this);
	}
}

// ── results, attendance ───────────────────────────────────────────────────────────────────────

public sealed class ResultsPage : ContentPage
{
	private readonly ResultsViewModel _results;
	private readonly ReportCardsViewModel _reportCards;
	private readonly Selection _selection;

	/// <summary>The Results tab: results of the class in view, then report cards (attendance has its own tab).</summary>
	public ResultsPage(ResultsViewModel results, ReportCardsViewModel reportCards, Selection selection)
	{
		_results = results;
		_reportCards = reportCards;
		_selection = selection;
		BindingContext = results;
		Title = Ui.T("Results");
		Content = new ScrollView { Content = new VerticalStackLayout { Children = { Ui.ScopeBar(selection), Build(results, null, reportCards) } } };
		selection.Changed += (_, _) => _ = reportCards.LoadAsync(selection.IsParent ? selection.ChildId : null);
		Ui.ApplyDirection(this);
	}

	/// <summary>Report cards: one card per period with its lines (headings in bold).</summary>
	internal static View ReportCards(ReportCardsViewModel vm)
	{
		var cards = new VerticalStackLayout { Spacing = 0, BindingContext = vm };
		BindableLayout.SetItemsSource(cards, vm.Cards);
		BindableLayout.SetEmptyView(cards, Ui.Empty(Ui.T("NoReportCards")));
		BindableLayout.SetItemTemplate(cards, new DataTemplate(() =>
		{
			var period = Ui.Title();
			period.SetBinding(Label.TextProperty, nameof(ReportCard.PeriodLabel));
			var generated = Ui.Small();
			generated.SetBinding(Label.TextProperty, nameof(ReportCard.GeneratedAt), stringFormat: "{0:d}");
			var lines = new VerticalStackLayout { Spacing = 4 };
			lines.SetBinding(BindableLayout.ItemsSourceProperty, nameof(ReportCard.Lines));
			BindableLayout.SetItemTemplate(lines, new DataTemplate(() =>
			{
				var label = Ui.Body();
				label.SetBinding(Label.TextProperty, nameof(ReportCardLine.Label));
				label.SetBinding(Label.FontAttributesProperty, nameof(ReportCardLine.IsHeading), converter: new BoldWhenTrue());
				var value = Ui.Body();
				value.HorizontalTextAlignment = TextAlignment.End;
				value.SetBinding(Label.TextProperty, nameof(ReportCardLine.Value));
				var row = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) } };
				row.Add(label, 0);
				row.Add(value, 1);
				return row;
			}));
			return Ui.Card(new VerticalStackLayout { Spacing = 6, Children = { Ui.Row(null, period, generated), lines } });
		}));
		return new VerticalStackLayout { Spacing = 4, Children = { Ui.Status(vm), Ui.Heading(Ui.T("ReportCards")), cards } };
	}

	/// <summary>A course with a figure and a progress bar coloured by the percentage (results and attendance).</summary>
	internal static View PercentCard(Label course, Label detail, string percentPath)
	{
		var percent = new Label { FontSize = 18, FontFamily = "OpenSansSemibold", VerticalOptions = LayoutOptions.Center };
		percent.SetBinding(Label.TextProperty, percentPath, stringFormat: "{0:0.#}%");
		percent.SetBinding(Label.TextColorProperty, percentPath, converter: new PercentToColor());
		var bar = new ProgressBar { HeightRequest = 6, Margin = new Thickness(0, 6, 0, 0) };
		bar.SetBinding(ProgressBar.ProgressProperty, percentPath, converter: new PercentToProgress());
		bar.SetBinding(ProgressBar.ProgressColorProperty, percentPath, converter: new PercentToColor());
		return Ui.Card(new VerticalStackLayout { Children = { Ui.Row(null, Ui.Lines(course, detail), percent), bar } });
	}

	/// <summary>Results, attendance per course (when given; the child page shows it, the Results tab has its own Attendance tab) and report cards.</summary>
	internal static View Build(ResultsViewModel results, AttendanceViewModel? attendance, ReportCardsViewModel reportCards)
	{
		var layout = new VerticalStackLayout { Padding = new Thickness(16, 4, 16, 24), Spacing = 4 };
		var resultsEmpty = Ui.Empty(Ui.T("NoResults"));
		layout.Add(Ui.Status(results));
		layout.Add(Ui.Heading(Ui.T("Results")));
		var resultList = new VerticalStackLayout { Spacing = 0, BindingContext = results };
		BindableLayout.SetItemsSource(resultList, results.Results);
		BindableLayout.SetItemTemplate(resultList, new DataTemplate(() =>
		{
			var course = Ui.Title();
			course.SetBinding(Label.TextProperty, new MultiBinding { Bindings = { new Binding(nameof(ResultItem.CourseCode)), new Binding(nameof(ResultItem.CourseTitle)) }, StringFormat = "{0} — {1}" });
			// "Class 8 · Final: 91/100": the class tells apart the same subject in different years (School children).
			var marks = Ui.Small();
			marks.BindingContextChanged += (_, _) =>
			{
				if (marks.BindingContext is not ResultItem r) return;
				marks.Text = string.Join(" · ", new[] { r.SemesterName, $"{r.ResultType}: {r.MarksObtained:0.##}/{r.MaxMarks:0.##}" }.Where(x => !string.IsNullOrWhiteSpace(x)));
			};
			return PercentCard(course, marks, nameof(ResultItem.Percentage));
		}));
		BindableLayout.SetEmptyView(resultList, resultsEmpty);
		layout.Add(resultList);
		if (attendance is not null)
		{
			layout.Add(Ui.Heading(Ui.T("Attendance")));
			layout.Add(AttendanceCourses(attendance.Courses, attendance));
		}
		layout.Add(ReportCards(reportCards));
		return layout;
	}

	/// <summary>Attendance per course: attended out of marked classes, with a coloured bar.</summary>
	internal static View AttendanceCourses(System.Collections.IEnumerable courses, object bindingContext)
	{
		var attendanceList = new VerticalStackLayout { Spacing = 0, BindingContext = bindingContext };
		BindableLayout.SetItemsSource(attendanceList, courses);
		BindableLayout.SetEmptyView(attendanceList, Ui.Empty(Ui.T("NoAttendance")));
		BindableLayout.SetItemTemplate(attendanceList, new DataTemplate(() =>
		{
			var course = Ui.Title();
			course.SetBinding(Label.TextProperty, nameof(AttendanceCourse.CourseTitle));
			var rate = Ui.Small();
			rate.SetBinding(Label.TextProperty, new MultiBinding
			{
				Bindings = { new Binding(nameof(AttendanceCourse.Attended)), new Binding(nameof(AttendanceCourse.Total)) },
				StringFormat = "{0}/{1}"
			});
			return PercentCard(course, rate, nameof(AttendanceCourse.Percent));
		}));
		return attendanceList;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		var child = _selection.IsParent ? _selection.ChildId : null;
		if (_selection.IsParent && child is null) return;
		await Task.WhenAll(_results.LoadAsync(child), _reportCards.LoadAsync(child));
	}
}

// ── attendance and homework tabs ──────────────────────────────────────────────────────────────

/// <summary>
/// Attendance for the class in view. Students and parents: overall figure, each course, and day by day (date, course, status).
/// Teachers: their courses in that class, each opening Take attendance.
/// </summary>
public sealed class AttendancePage : ContentPage
{
	private readonly AttendanceScreenViewModel _vm;
	private readonly Selection _selection;

	public AttendancePage(AttendanceScreenViewModel vm, Selection selection, EduSphereClient client)
	{
		_vm = vm;
		_selection = selection;
		BindingContext = vm;
		Title = Ui.T("Attendance");
		var body = new VerticalStackLayout { Padding = new Thickness(16, 4, 16, 24), Spacing = 4 };
		if (selection.IsTeacher)
		{
			// Teachers: the courses they teach in the chosen class and campus.
			var courses = new VerticalStackLayout { Spacing = 0 };
			void ShowCourses()
			{
				courses.Clear();
				var shown = selection.Courses.Where(c => selection.Includes(c.OfferingId)).OrderBy(c => c.SemesterName, Selection.ClassOrder.Instance).ToList();
				if (shown.Count == 0) courses.Add(Ui.Empty(Ui.T("NoCourses")));
				foreach (var c in shown)
				{
					var take = Ui.Primary(Ui.T("TakeAttendance"));
					take.Padding = new Thickness(12, 8);
					take.MinimumHeightRequest = 40;
					take.FontSize = 13;
					take.Clicked += async (_, _) => await Navigation.PushAsync(new TakeAttendancePage(new TakeAttendanceViewModel(client), c.OfferingId));
					courses.Add(Ui.Card(Ui.Row(null, Ui.Lines(Ui.Title(c.CourseTitle), Ui.Small(string.Join(" · ", new[] { c.CourseCode, c.CampusName }.Where(x => !string.IsNullOrWhiteSpace(x))))), take), padding: 12));
				}
			}
			selection.Changed += (_, _) => ShowCourses();
			ShowCourses();
			body.Add(Ui.Small(Ui.T("TakeAttendanceHint")));
			body.Add(courses);
		}
		else
		{
			var overall = Ui.Stat(Ui.T("Overall"), "—", Ui.Sky);
			var overallValue = (Label)((VerticalStackLayout)overall.Content!).Children[0];
			vm.PropertyChanged += (_, e) =>
			{
				if (e.PropertyName != nameof(AttendanceScreenViewModel.Overall)) return;
				overallValue.Text = vm.Overall is { } p ? $"{p:0.#}%" : "—";
				overallValue.TextColor = Ui.PercentColor(vm.Overall);
			};
			var days = new VerticalStackLayout { Spacing = 0 };
			BindableLayout.SetItemsSource(days, vm.Days);
			BindableLayout.SetEmptyView(days, Ui.Empty(Ui.T("NoAttendance")));
			BindableLayout.SetItemTemplate(days, new DataTemplate(() =>
			{
				var date = Ui.Title(size: 14);
				date.SetBinding(Label.TextProperty, nameof(AttendanceDay.Date), stringFormat: "{0:ddd d MMM yyyy}");
				var course = Ui.Small();
				course.SetBinding(Label.TextProperty, new MultiBinding { Bindings = { new Binding(nameof(AttendanceDay.CourseCode)), new Binding(nameof(AttendanceDay.CourseTitle)) }, StringFormat = "{0} · {1}" });
				var chip = Ui.Chip("", Ui.Good);
				var text = (Label)chip.Content!;
				chip.BindingContextChanged += (s, _) =>
				{
					if ((s as BindableObject)?.BindingContext is not AttendanceDay d) return;
					var color = Ui.StatusColor(d.Status);
					text.Text = Ui.StatusText(d.Status);
					text.TextColor = color;
					chip.BackgroundColor = color.WithAlpha(0.13f);
				};
				return Ui.Card(Ui.Row(null, Ui.Lines(date, course), chip), padding: 12);
			}));
			body.Add(Ui.Status(vm));
			body.Add(Ui.Stats(overall));
			body.Add(Ui.Heading(Ui.T("Courses")));
			body.Add(ResultsPage.AttendanceCourses(vm.Courses, vm));
			body.Add(Ui.Heading(Ui.T("DayByDay")));
			body.Add(days);
		}
		var refresh = new RefreshView { Content = new ScrollView { Content = new VerticalStackLayout { Children = { Ui.ScopeBar(selection), body } } }, Command = vm.LoadCommand };
		refresh.SetBinding(RefreshView.IsRefreshingProperty, nameof(PageViewModel.IsBusy));
		Content = refresh;
		Ui.ApplyDirection(this);
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await _vm.LoadAsync();
	}
}

/// <summary>Homework of the class in view: students hand in, teachers grade, parents follow their child's.</summary>
public sealed class HomeworkPage : ContentPage
{
	private readonly HomeworkViewModel _vm;

	public HomeworkPage(HomeworkViewModel vm, Selection selection, EduSphereClient client, IPickers pickers)
	{
		_vm = vm;
		BindingContext = vm;
		Title = Ui.T("Homework");
		var list = new CollectionView
		{
			ItemsSource = vm.Items,
			SelectionMode = SelectionMode.Single,
			Footer = new ContentView { HeightRequest = 16 },
			EmptyView = new ContentView { Padding = 16, Content = Ui.Empty(Ui.T("NoHomework")) },
			ItemTemplate = new DataTemplate(() =>
			{
				var title = Ui.Title();
				var course = Ui.Small();
				var due = Ui.Small();
				var chip = Ui.Chip("", Ui.Accent);
				var text = (Label)chip.Content!;
				var card = Ui.Card(Ui.Row(null, Ui.Lines(title, course, due), chip));
				card.BindingContextChanged += (_, _) =>
				{
					if (card.BindingContext is not HomeworkItem h) return;
					var now = DateTime.UtcNow;
					title.Text = h.Title;
					course.Text = string.Join(" · ", new[] { h.CourseCode, h.SemesterName }.Where(x => !string.IsNullOrWhiteSpace(x)));
					due.Text = $"{Ui.T("Due")}: {Ui.Date(h.DueDate)}";
					var color = selection.IsTeacher ? (h.ToGrade > 0 ? Ui.Warn : Ui.Good)
						: h.Marks is not null ? Ui.Good : h.Submitted ? Ui.Sky : h.DueDate < now ? Ui.Bad : Ui.Accent;
					text.Text = HomeworkViewModel.State(h, selection.IsTeacher, now, selection.IsParent);
					text.TextColor = color;
					chip.BackgroundColor = color.WithAlpha(0.13f);
				};
				return new ContentView { Padding = new Thickness(16, 0), Content = card };
			})
		};
		list.SelectionChanged += async (_, e) =>
		{
			if (e.CurrentSelection.FirstOrDefault() is not HomeworkItem h) return;
			list.SelectedItem = null;
			if (selection.IsTeacher) await Navigation.PushAsync(new GradingPage(new GradingViewModel(client), h.ToAssignment(), h.OfferingId));
			else if (!selection.IsParent) await Navigation.PushAsync(new SubmitPage(new SubmitAssignmentViewModel(client, pickers) { Assignment = h.ToAssignment() }));
		};
		var refresh = new RefreshView { Content = list, Command = vm.LoadCommand };
		refresh.SetBinding(RefreshView.IsRefreshingProperty, nameof(PageViewModel.IsBusy));
		Content = Ui.ListLayout(vm, Ui.ScopeBar(selection), refresh);
		Ui.ApplyDirection(this);
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await _vm.LoadAsync();
	}
}

// ── parents ───────────────────────────────────────────────────────────────────────────────────

public sealed class ChildrenPage : ContentPage
{
	private readonly ChildrenViewModel _vm;

	public ChildrenPage(ChildrenViewModel vm, EduSphereClient client)
	{
		_vm = vm;
		BindingContext = vm;
		Title = Ui.T("Children");
		var list = new CollectionView
		{
			ItemsSource = vm.Children,
			SelectionMode = SelectionMode.Single,
			Header = new ContentView { HeightRequest = 8 },
			ItemTemplate = new DataTemplate(() =>
			{
				var initials = new Label();
				initials.SetBinding(Label.TextProperty, nameof(ChildSummary.Name), converter: new InitialsOf());
				var name = Ui.Title();
				name.SetBinding(Label.TextProperty, nameof(ChildSummary.Name));
				var sub = Ui.Small();
				sub.SetBinding(Label.TextProperty, new MultiBinding { Bindings = { new Binding(nameof(ChildSummary.RegistrationNumber)), new Binding(nameof(ChildSummary.ProgramName)) }, StringFormat = "{0} · {1}" });
				return new ContentView { Padding = new Thickness(16, 0), Content = Ui.Card(Ui.Row(Ui.Avatar(initials, Ui.Accent), Ui.Lines(name, sub), Ui.Chevron())) };
			})
		};
		list.SelectionChanged += async (_, e) =>
		{
			if (e.CurrentSelection.FirstOrDefault() is ChildSummary child)
			{
				list.SelectedItem = null;
				await Navigation.PushAsync(new ChildPage(new ChildViewModel(client), child));
			}
		};
		Content = new Grid { RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star) }, Children = { Ui.Status(vm), list } };
		Grid.SetRow(list, 1);
		Ui.ApplyDirection(this);
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await _vm.LoadAsync();
	}
}

public sealed class ChildPage : ContentPage
{
	private readonly ChildViewModel _vm;
	private readonly ChildSummary _child;

	public ChildPage(ChildViewModel vm, ChildSummary child)
	{
		_vm = vm;
		_child = child;
		BindingContext = vm;
		Title = child.Name;
		var announcements = new VerticalStackLayout { Spacing = 0 };
		BindableLayout.SetItemsSource(announcements, vm.Announcements);
		BindableLayout.SetItemTemplate(announcements, new DataTemplate(() =>
		{
			var title = Ui.Title();
			title.SetBinding(Label.TextProperty, nameof(AnnouncementItem.Title));
			var when = Ui.Small();
			when.SetBinding(Label.TextProperty, nameof(AnnouncementItem.PostedAt), converter: new ShortLocalDate());
			var body = Ui.Body();
			body.SetBinding(Label.TextProperty, nameof(AnnouncementItem.Body));
			var bar = new BoxView { WidthRequest = 4, CornerRadius = 2, Color = Ui.Sky, BackgroundColor = Colors.Transparent, VerticalOptions = LayoutOptions.Fill };
			return Ui.Card(Ui.Row(bar, Ui.Lines(title, when, body)));
		}));
		var (standing, standingValue) = HomeViewModel.Standing(child.Cgpa, child.AveragePercent);
		var fees = child.OutstandingFees ?? 0;
		var banner = Ui.Banner(new VerticalStackLayout
		{
			Spacing = 4,
			Children = { Ui.OnBanner(child.Name, 20, bold: true), Ui.OnBanner(string.Join(" · ", new[] { child.RegistrationNumber, child.ProgramName }.Where(x => !string.IsNullOrWhiteSpace(x))), 13, opacity: 0.85) }
		});
		var stats = Ui.Stats(
			Ui.Stat(Ui.T("AttendanceRate"), child.AttendancePercent is { } a ? $"{a:0.#}%" : "—", Ui.PercentColor(child.AttendancePercent)),
			Ui.Stat(standing, standingValue, Ui.Sky),
			Ui.Stat(Ui.T("Fees"), fees.ToString("N0"), fees > 0 ? Ui.Bad : Ui.Good));
		Content = new ScrollView
		{
			Content = new VerticalStackLayout
			{
				Children =
				{
					new VerticalStackLayout { Padding = new Thickness(16, 12, 16, 0), Spacing = 8, Children = { banner, stats } },
					ResultsPage.Build(vm.Results, vm.Attendance, vm.ReportCards),
					new VerticalStackLayout { Padding = new Thickness(16, 0, 16, 24), Children = { Ui.Heading(Ui.T("Announcements")), announcements } }
				}
			}
		};
		Ui.ApplyDirection(this);
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await _vm.LoadAsync(_child);
	}
}

// ── notifications, settings ───────────────────────────────────────────────────────────────────

public sealed class NotificationsPage : ContentPage
{
	private readonly NotificationsViewModel _vm;

	public NotificationsPage(NotificationsViewModel vm)
	{
		_vm = vm;
		BindingContext = vm;
		Title = Ui.T("Notifications");
		var list = new CollectionView
		{
			ItemsSource = vm.Items,
			RemainingItemsThreshold = 3,
			RemainingItemsThresholdReachedCommand = vm.MoreCommand,
			EmptyView = new ContentView { Padding = 16, Content = Ui.Empty(Ui.T("NoNotifications")) },
			Header = new ContentView { HeightRequest = 8 },
			ItemTemplate = new DataTemplate(() =>
			{
				var dot = new Ellipse { WidthRequest = 10, HeightRequest = 10, Fill = new SolidColorBrush(Ui.Accent), VerticalOptions = LayoutOptions.Start, Margin = new Thickness(0, 6, 0, 0) };
				dot.SetBinding(IsVisibleProperty, nameof(NotificationItem.IsRead), converter: new Not());
				var title = Ui.Title(size: 14);
				title.SetBinding(Label.TextProperty, nameof(NotificationItem.Title));
				var body = Ui.Small();
				body.SetBinding(Label.TextProperty, nameof(NotificationItem.Body));
				var when = Ui.Small();
				when.FontSize = 12;
				when.SetBinding(Label.TextProperty, nameof(NotificationItem.When), converter: new ShortLocalDate());
				var card = Ui.Card(Ui.Row(new ContentView { WidthRequest = 10, Content = dot }, Ui.Lines(title, body, when)), padding: 12);
				var tap = new TapGestureRecognizer { Command = vm.ReadCommand };
				tap.SetBinding(TapGestureRecognizer.CommandParameterProperty, ".");
				card.GestureRecognizers.Add(tap);
				return new ContentView { Padding = new Thickness(16, 0), Content = card };
			})
		};
		ToolbarItems.Add(new ToolbarItem { Text = Ui.T("MarkAllRead"), Command = vm.MarkAllReadCommand });
		var refresh = new RefreshView { Content = list, Command = vm.LoadCommand };
		refresh.SetBinding(RefreshView.IsRefreshingProperty, nameof(PageViewModel.IsBusy));
		Content = refresh;
		Ui.ApplyDirection(this);
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await _vm.LoadAsync();
	}
}

public sealed class SettingsPage : ContentPage
{
	public SettingsPage(SettingsViewModel vm, string? userName, string? role, string? server)
	{
		BindingContext = vm;
		Title = Ui.T("Settings");

		var account = Ui.Card(Ui.Row(Ui.Avatar(userName, Ui.Accent, 48), Ui.Lines(Ui.Title(userName, 17), Ui.Small(Ui.RoleName(role)))), padding: 16);

		// Light, dark or the phone's own setting; kept on the phone (App.ApplyTheme at start-up).
		var themes = new Picker { Title = Ui.T("Theme"), ItemsSource = new[] { Ui.T("ThemeSystem"), Ui.T("ThemeLight"), Ui.T("ThemeDark") } };
		themes.SelectedIndex = Math.Max(0, Array.IndexOf(App.Themes, Preferences.Default.Get(App.ThemeKey, App.Themes[0])));
		themes.SelectedIndexChanged += (_, _) =>
		{
			if (themes.SelectedIndex < 0) return;
			Preferences.Default.Set(App.ThemeKey, App.Themes[themes.SelectedIndex]);
			App.ApplyTheme();
		};
		var theme = Ui.Card(new VerticalStackLayout { Spacing = 8, Children = { Ui.Title(Ui.T("Theme")), Ui.Field(themes) } }, padding: 16);

		var changePassword = Ui.Secondary(Ui.T("ChangePassword"));
		changePassword.Clicked += async (_, _) => await Navigation.PushAsync(new ChangePasswordPage(new ChangePasswordViewModel(vm.Client)));
		var security = Ui.Card(new VerticalStackLayout { Spacing = 8, Children = { Ui.Title(Ui.T("Password")), changePassword } }, padding: 16);

		var serverLine = Ui.Small($"{Ui.T("Server")}: {ServerAddress.Display(server)}");
		var aboutLines = Ui.Lines(Ui.Title("Tabsan EduSphere", 14), Ui.Small(vm.VersionText), serverLine);
		if (ServerAddress.IsUnencrypted(server))
		{
			var warning = Ui.Small(Ui.T("NotEncrypted"));
			warning.TextColor = Ui.Warn;
			aboutLines.Add(warning);
		}
		var aboutStack = new VerticalStackLayout
		{
			Spacing = 8,
			Children = { Ui.Title(Ui.T("About")), Ui.Row(new Image { Source = "tabsan_mark.png", HeightRequest = 40 }, aboutLines) }
		};
		if (vm.Client.CanChangeServer)
		{
			// Another institute's server: its accounts are different, so this signs out first.
			var changeServer = Ui.Secondary(Ui.T("ChangeServer"));
			changeServer.AutomationId = "changeserver";
			changeServer.Clicked += async (_, _) =>
			{
				if (!await DisplayAlert(Ui.T("ChangeServer"), Ui.T("ChangeServerConfirm"), Ui.T("ChangeServer"), Ui.T("Cancel"))) return;
				if (Application.Current is App app) await app.ChangeServerAsync(vm.SignOutAsync);
			};
			aboutStack.Add(changeServer);
		}
		var about = Ui.Card(aboutStack, padding: 16);

		var signOut = Ui.Danger(Ui.T("SignOut"));
		signOut.Command = vm.SignOutCommand;
		var footer = Ui.Small(Ui.T("PoweredBy"));
		footer.HorizontalTextAlignment = TextAlignment.Center;
		footer.Margin = new Thickness(0, 8, 0, 16);
		Content = Ui.Page(Title, vm, account, theme, security, about, signOut, footer).Content;
		Ui.ApplyDirection(this);
	}
}

/// <summary>Changing the password: current password, then the new one twice.</summary>
public sealed class ChangePasswordPage : ContentPage
{
	public ChangePasswordPage(ChangePasswordViewModel vm)
	{
		BindingContext = vm;
		Title = Ui.T("ChangePassword");
		Entry Password(string placeholder, string property, string id)
		{
			var entry = new Entry { Placeholder = Ui.T(placeholder), IsPassword = true, AutomationId = id, IsTextPredictionEnabled = false, IsSpellCheckEnabled = false };
			entry.SetBinding(Entry.TextProperty, property);
			return entry;
		}
		var save = Ui.Primary(Ui.T("ChangePassword"));
		save.Command = vm.SaveCommand;
		var form = Ui.Card(new VerticalStackLayout
		{
			Spacing = 12,
			Children =
			{
				Ui.Field(Password("CurrentPassword", nameof(ChangePasswordViewModel.CurrentPassword), "currentpassword")),
				Ui.Field(Password("NewPassword", nameof(ChangePasswordViewModel.NewPassword), "newpassword")),
				Ui.Field(Password("ConfirmPassword", nameof(ChangePasswordViewModel.ConfirmPassword), "confirmpassword")),
				save
			}
		}, padding: 16);
		Content = Ui.Page(Title, vm, form).Content;
		Ui.ApplyDirection(this);
	}
}
