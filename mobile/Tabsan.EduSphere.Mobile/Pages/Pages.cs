using System.Globalization;
using Tabsan.EduSphere.Mobile.Core;

namespace Tabsan.EduSphere.Mobile.Pages;

// Plan M12 — the app's screens, built in C# and bound to the view models in Tabsan.EduSphere.Mobile.Core.

internal static class Ui
{
	public static string T(string key) => AppText.Get(key);

	public static Label Heading(string text) => new() { Text = text, FontSize = 20, FontAttributes = FontAttributes.Bold, Margin = new Thickness(0, 12, 0, 4) };

	public static Label Small(string? text = null) => new() { Text = text, FontSize = 13, TextColor = Colors.Gray };

	public static Label Status(PageViewModel vm)
	{
		var label = new Label { TextColor = Colors.Firebrick, FontSize = 14 };
		label.SetBinding(Label.TextProperty, nameof(PageViewModel.Error));
		label.SetBinding(VisualElement.IsVisibleProperty, nameof(PageViewModel.Error), converter: new NotEmpty());
		return label;
	}

	public static Label Notice()
	{
		var label = new Label { TextColor = Colors.SeaGreen, FontSize = 14 };
		label.SetBinding(Label.TextProperty, nameof(PageViewModel.Notice));
		label.SetBinding(VisualElement.IsVisibleProperty, nameof(PageViewModel.Notice), converter: new NotEmpty());
		return label;
	}

	public static ActivityIndicator Busy()
	{
		var busy = new ActivityIndicator();
		busy.SetBinding(ActivityIndicator.IsRunningProperty, nameof(PageViewModel.IsBusy));
		busy.SetBinding(VisualElement.IsVisibleProperty, nameof(PageViewModel.IsBusy));
		return busy;
	}

	public static Border Card(View content) => new()
	{
		Content = content, Padding = 12, Margin = new Thickness(0, 4),
		StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 }, Stroke = Colors.LightGray
	};

	public static ContentPage Page(string title, PageViewModel vm, params View[] children)
	{
		var stack = new VerticalStackLayout { Padding = 16, Spacing = 8 };
		stack.Add(Busy());
		stack.Add(Status(vm));
		stack.Add(Notice());
		foreach (var c in children) stack.Add(c);
		return new ContentPage { Title = title, BindingContext = vm, Content = new ScrollView { Content = stack } };
	}

	public static string Date(DateTime? d) => d is { } v ? v.ToLocalTime().ToString("ddd d MMM HH:mm", CultureInfo.CurrentCulture) : "";

	public static void ApplyDirection(Page page) => page.FlowDirection = AppText.IsRtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
}

internal sealed class BoldWhenTrue : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is true ? FontAttributes.Bold : FontAttributes.None;
	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

internal sealed class NotEmpty : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is string s && s.Length > 0;
	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

// ── sign-in ───────────────────────────────────────────────────────────────────────────────────

public sealed class SignInPage : ContentPage
{
	private readonly SignInViewModel _vm;

	public event EventHandler? SignedIn;

	public SignInPage(SignInViewModel vm, ILauncherService launcher, MauiProgram.WhiteLabel label)
	{
		_vm = vm;
		BindingContext = vm;
		Title = label.AppName;
		vm.SignedIn += (_, _) => SignedIn?.Invoke(this, EventArgs.Empty);

		var institution = new Label { FontSize = 24, FontAttributes = FontAttributes.Bold, HorizontalTextAlignment = TextAlignment.Center };
		institution.SetBinding(Label.TextProperty, new Binding("Config.InstitutionName") { FallbackValue = label.AppName, TargetNullValue = label.AppName });
		var username = new Entry { Placeholder = Ui.T("Username"), Keyboard = Keyboard.Email, AutomationId = "username" };
		username.SetBinding(Entry.TextProperty, nameof(SignInViewModel.Username));
		var password = new Entry { Placeholder = Ui.T("Password"), IsPassword = true, AutomationId = "password" };
		password.SetBinding(Entry.TextProperty, nameof(SignInViewModel.Password));
		var mfa = new Entry { Placeholder = Ui.T("AuthenticatorCode"), Keyboard = Keyboard.Numeric };
		mfa.SetBinding(Entry.TextProperty, nameof(SignInViewModel.MfaCode));
		mfa.SetBinding(IsVisibleProperty, nameof(SignInViewModel.NeedsMfa));
		var signIn = new Button { Text = Ui.T("SignIn"), AutomationId = "signin" };
		signIn.SetBinding(Button.CommandProperty, nameof(SignInViewModel.SignInCommand));
		var providers = new VerticalStackLayout { Spacing = 6 };
		BindableLayout.SetItemsSource(providers, vm.Providers);
		BindableLayout.SetItemTemplate(providers, new DataTemplate(() =>
		{
			var b = new Button { BackgroundColor = Colors.White, TextColor = Colors.Black, BorderColor = Colors.LightGray, BorderWidth = 1 };
			b.SetBinding(Button.TextProperty, nameof(SsoProvider.DisplayName), stringFormat: Ui.T("SignInWith"));
			b.SetBinding(Button.CommandParameterProperty, nameof(SsoProvider.Provider));
			b.Command = vm.SingleSignOnCommand;
			return b;
		}));
		var update = new Button { Text = Ui.T("Update"), IsVisible = false };
		update.Clicked += async (_, _) =>
		{
			var url = DeviceInfo.Platform == DevicePlatform.iOS ? vm.Config?.IosStoreUrl : vm.Config?.AndroidStoreUrl;
			if (Uri.TryCreate(url, UriKind.Absolute, out var store)) await launcher.OpenAsync(store);
		};
		vm.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(SignInViewModel.VersionState)) update.IsVisible = vm.VersionState != VersionPolicy.State.Current; };

		Content = new ScrollView
		{
			Content = new VerticalStackLayout
			{
				Padding = new Thickness(24, 48), Spacing = 12,
				Children = { institution, Ui.Busy(), Ui.Status(vm), username, password, mfa, signIn, providers, update }
			}
		};
		Ui.ApplyDirection(this);
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await _vm.LoadAsync();
	}
}

// ── home ──────────────────────────────────────────────────────────────────────────────────────

public sealed class HomePage : ContentPage
{
	private readonly HomeViewModel _vm;
	private readonly VerticalStackLayout _body = new() { Padding = 16, Spacing = 8 };
	private readonly EduSphereClient _client;
	private readonly ILauncherService _launcher;
	private readonly IPickers _pickers;
	private readonly Func<string?> _portal;

	public HomePage(HomeViewModel vm, EduSphereClient client, ILauncherService launcher, IPickers pickers, Func<string?> portal)
	{
		_vm = vm;
		_client = client;
		_launcher = launcher;
		_pickers = pickers;
		_portal = portal;
		BindingContext = vm;
		Title = Ui.T("Home");
		var refresh = new RefreshView { Content = new ScrollView { Content = _body } };
		refresh.SetBinding(RefreshView.IsRefreshingProperty, nameof(PageViewModel.IsBusy));
		refresh.Command = vm.LoadCommand;
		Content = refresh;
		vm.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(HomeViewModel.Home)) Render(); };
		Ui.ApplyDirection(this);
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await _vm.LoadAsync();
	}

	private void Render()
	{
		_body.Clear();
		_body.Add(Ui.Status(_vm));
		_body.Add(Ui.Heading(_vm.Greeting));
		var home = _vm.Home;
		if (home is null) return;
		if (home.Student is { } s)
		{
			_body.Add(Ui.Card(new HorizontalStackLayout
			{
				Spacing = 24,
				Children =
				{
					Stat(Ui.T("AttendanceRate"), s.AttendancePercent is { } a ? $"{a:0.#}%" : "—"),
					Stat(Ui.T("Cgpa"), s.Cgpa?.ToString("0.00") ?? "—"),
					Stat(Ui.T("Fees"), s.OutstandingFees is > 0 ? s.OutstandingFees.Value.ToString("N0") : "0")
				}
			}));
		}
		if (home.Teacher is { } t)
			_body.Add(Ui.Card(new HorizontalStackLayout { Spacing = 24, Children = { Stat(Ui.T("Courses"), t.Courses.ToString()), Stat(Ui.T("ToGrade"), t.SubmissionsToGrade.ToString()) } }));

		if (home.Children is { Count: > 0 } children)
		{
			_body.Add(Ui.Heading(Ui.T("Children")));
			foreach (var c in children)
			{
				var card = Ui.Card(new VerticalStackLayout
				{
					Children =
					{
						new Label { Text = c.Name, FontAttributes = FontAttributes.Bold },
						Ui.Small($"{c.RegistrationNumber} · {c.ProgramName}"),
						Ui.Small($"{Ui.T("AttendanceRate")}: {(c.AttendancePercent is { } p ? $"{p:0.#}%" : "—")} · {Ui.T("Cgpa")}: {c.Cgpa:0.00} · {Ui.T("Fees")}: {c.OutstandingFees:N0}")
					}
				});
				var tap = new TapGestureRecognizer();
				tap.Tapped += async (_, _) => await Navigation.PushAsync(new ChildPage(new ChildViewModel(_client), c));
				card.GestureRecognizers.Add(tap);
				_body.Add(card);
			}
		}

		if (!_vm.IsParent)
		{
			_body.Add(Ui.Heading(Ui.T("Today")));
			if (home.Today.Count == 0) _body.Add(Ui.Small(Ui.T("NoClassesToday")));
			foreach (var c in home.Today)
				_body.Add(Ui.Card(new VerticalStackLayout { Children = { new Label { Text = $"{c.Start}–{c.End}  {c.Subject}", FontAttributes = FontAttributes.Bold }, Ui.Small(c.Where) } }));
			_body.Add(Ui.Heading(Ui.T("Due")));
			if (home.Due.Count == 0) _body.Add(Ui.Small(Ui.T("NothingDue")));
			foreach (var d in home.Due)
			{
				var card = Ui.Card(new VerticalStackLayout
				{
					Children = { new Label { Text = d.Title, FontAttributes = FontAttributes.Bold }, Ui.Small($"{d.CourseTitle} · {HomeViewModel.DueLabel(d, DateTime.UtcNow)}") }
				});
				var tap = new TapGestureRecognizer();
				tap.Tapped += async (_, _) => await OpenCourseAsync(d.OfferingId);
				card.GestureRecognizers.Add(tap);
				_body.Add(card);
			}
		}

		if (home.Announcements.Count > 0)
		{
			_body.Add(Ui.Heading(Ui.T("Announcements")));
			foreach (var a in home.Announcements)
				_body.Add(Ui.Card(new VerticalStackLayout { Children = { new Label { Text = a.Title, FontAttributes = FontAttributes.Bold }, Ui.Small($"{a.CourseTitle} · {Ui.Date(a.PostedAt)}"), new Label { Text = a.Body } } }));
		}
	}

	private Task OpenCourseAsync(Guid offeringId)
		=> Navigation.PushAsync(new CoursePage(new CourseViewModel(_client, _launcher, _portal), offeringId, _client, _pickers));

	private static View Stat(string label, string value)
		=> new VerticalStackLayout { Children = { new Label { Text = value, FontSize = 20, FontAttributes = FontAttributes.Bold }, Ui.Small(label) } };
}

// ── timetable ─────────────────────────────────────────────────────────────────────────────────

public sealed class TimetablePage : ContentPage
{
	private readonly TimetableViewModel _vm;

	public TimetablePage(TimetableViewModel vm)
	{
		_vm = vm;
		BindingContext = vm;
		Title = Ui.T("Timetable");
		var list = new CollectionView
		{
			ItemsSource = vm.Days,
			ItemTemplate = new DataTemplate(() =>
			{
				var day = new Label { FontAttributes = FontAttributes.Bold, FontSize = 18, Margin = new Thickness(0, 12, 0, 4) };
				day.SetBinding(Label.TextProperty, nameof(TimetableDay.Day));
				var classes = new VerticalStackLayout { Spacing = 4 };
				classes.SetBinding(BindableLayout.ItemsSourceProperty, nameof(TimetableDay.Classes));
				BindableLayout.SetItemTemplate(classes, new DataTemplate(() =>
				{
					var title = new Label { FontAttributes = FontAttributes.Bold };
					title.SetBinding(Label.TextProperty, new MultiBinding
					{
						Bindings = { new Binding(nameof(ClassSlot.Start)), new Binding(nameof(ClassSlot.End)), new Binding(nameof(ClassSlot.Subject)) },
						StringFormat = "{0}–{1}  {2}"
					});
					var where = Ui.Small();
					where.SetBinding(Label.TextProperty, nameof(ClassSlot.Where));
					return Ui.Card(new VerticalStackLayout { Children = { title, where } });
				}));
				return new VerticalStackLayout { Padding = new Thickness(16, 0), Children = { day, classes } };
			})
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

// ── courses ───────────────────────────────────────────────────────────────────────────────────

public sealed class CoursesPage : ContentPage
{
	private readonly CoursesViewModel _vm;

	public CoursesPage(CoursesViewModel vm, EduSphereClient client, ILauncherService launcher, IPickers pickers, Func<string?> portal)
	{
		_vm = vm;
		BindingContext = vm;
		Title = Ui.T("Courses");
		var list = new CollectionView
		{
			ItemsSource = vm.Courses,
			SelectionMode = SelectionMode.Single,
			ItemTemplate = new DataTemplate(() =>
			{
				var title = new Label { FontAttributes = FontAttributes.Bold };
				title.SetBinding(Label.TextProperty, nameof(CourseSummary.Title));
				var sub = Ui.Small();
				sub.SetBinding(Label.TextProperty, new MultiBinding { Bindings = { new Binding(nameof(CourseSummary.SemesterName)), new Binding(nameof(CourseSummary.Teacher)) }, StringFormat = "{0} · {1}" });
				return new ContentView { Padding = new Thickness(16, 4), Content = Ui.Card(new VerticalStackLayout { Children = { title, sub } }) };
			})
		};
		list.SelectionChanged += async (_, e) =>
		{
			if (e.CurrentSelection.FirstOrDefault() is CourseSummary course)
			{
				list.SelectedItem = null;
				await Navigation.PushAsync(new CoursePage(new CourseViewModel(client, launcher, portal), course.OfferingId, client, pickers));
			}
		};
		Content = new Grid { RowDefinitions = { new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star) }, Children = { Ui.Status(vm), list } };
		Grid.SetRow(list, 1);
		Ui.ApplyDirection(this);
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		if (_vm.Courses.Count == 0) await _vm.LoadAsync();
	}
}

/// <summary>A course: materials, assignments (hand in / grade), quizzes (in the portal), announcements; teachers also take attendance and post.</summary>
public sealed class CoursePage : ContentPage
{
	private readonly CourseViewModel _vm;
	private readonly Guid _offeringId;
	private readonly EduSphereClient _client;
	private readonly IPickers _pickers;
	private readonly VerticalStackLayout _body = new() { Padding = 16, Spacing = 8 };

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
		_body.Add(Ui.Heading(d.Course.Title));
		if (d.IsTeacher)
		{
			var take = new Button { Text = Ui.T("TakeAttendance") };
			take.Clicked += async (_, _) => await Navigation.PushAsync(new TakeAttendancePage(new TakeAttendanceViewModel(_client), _offeringId));
			var post = new Button { Text = Ui.T("NewAnnouncement") };
			post.Clicked += async (_, _) => await Navigation.PushAsync(new AnnouncementPage(new AnnouncementViewModel(_client) { OfferingId = _offeringId }));
			_body.Add(new HorizontalStackLayout { Spacing = 8, Children = { take, post } });
		}
		var talk = new Button { Text = Ui.T("Discussions") };
		talk.Clicked += async (_, _) => await Navigation.PushAsync(new DiscussionsPage(new DiscussionsViewModel(_client), _offeringId, _client));
		_body.Add(talk);

		_body.Add(Ui.Heading(Ui.T("Assignments")));
		foreach (var a in d.Assignments)
		{
			var state = d.IsTeacher ? Ui.T("ToGrade") : a.Marks is { } m ? $"{Ui.T("Marks")}: {m:0.##}/{a.MaxMarks:0.##}" : a.Submitted ? Ui.T("NotGradedYet") : Ui.T("HandIn");
			var card = Ui.Card(new VerticalStackLayout { Children = { new Label { Text = a.Title, FontAttributes = FontAttributes.Bold }, Ui.Small($"{Ui.T("Due")}: {Ui.Date(a.DueDate)} · {state}") } });
			var tap = new TapGestureRecognizer();
			tap.Tapped += async (_, _) =>
			{
				if (d.IsTeacher) await Navigation.PushAsync(new GradingPage(new GradingViewModel(_client), a));
				else await Navigation.PushAsync(new SubmitPage(new SubmitAssignmentViewModel(_client, _pickers) { Assignment = a }));
			};
			card.GestureRecognizers.Add(tap);
			_body.Add(card);
		}

		_body.Add(Ui.Heading(Ui.T("Materials")));
		foreach (var m in d.Materials)
		{
			var open = new Button { Text = $"{m.Title}", Command = _vm.OpenMaterialCommand, CommandParameter = m, HorizontalOptions = LayoutOptions.Fill };
			_body.Add(open);
		}

		if (d.Quizzes.Count > 0)
		{
			_body.Add(Ui.Heading(Ui.T("Quizzes")));
			_body.Add(Ui.Small(Ui.T("QuizInPortal")));
			foreach (var q in d.Quizzes)
				_body.Add(new Button { Text = $"{q.Title} ({q.AttemptsUsed}/{q.MaxAttempts})", Command = _vm.OpenQuizCommand, CommandParameter = q });
		}

		if (d.Announcements.Count > 0)
		{
			_body.Add(Ui.Heading(Ui.T("Announcements")));
			foreach (var a in d.Announcements)
				_body.Add(Ui.Card(new VerticalStackLayout { Children = { new Label { Text = a.Title, FontAttributes = FontAttributes.Bold }, Ui.Small(Ui.Date(a.PostedAt)), new Label { Text = a.Body } } }));
		}
	}
}

public sealed class SubmitPage : ContentPage
{
	public SubmitPage(SubmitAssignmentViewModel vm)
	{
		BindingContext = vm;
		Title = vm.Assignment?.Title ?? Ui.T("HandIn");
		var text = new Editor { Placeholder = Ui.T("YourAnswer"), AutoSize = EditorAutoSizeOption.TextChanges, MinimumHeightRequest = 120 };
		text.SetBinding(Editor.TextProperty, nameof(SubmitAssignmentViewModel.Text));
		var files = new VerticalStackLayout();
		BindableLayout.SetItemsSource(files, vm.Files);
		BindableLayout.SetItemTemplate(files, new DataTemplate(() =>
		{
			var l = Ui.Small();
			l.SetBinding(Label.TextProperty, nameof(UploadFile.FileName));
			return l;
		}));
		vm.Submitted += async (_, _) => await Navigation.PopAsync();
		var page = Ui.Page(Title, vm,
			new Label { Text = vm.Assignment?.Description },
			text,
			new HorizontalStackLayout { Spacing = 8, Children = { new Button { Text = Ui.T("TakePhoto"), Command = vm.TakePhotoCommand }, new Button { Text = Ui.T("AddFile"), Command = vm.AddFilesCommand } } },
			files,
			new Button { Text = Ui.T("HandIn"), Command = vm.SubmitCommand });
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
		var list = new VerticalStackLayout { Spacing = 4 };
		BindableLayout.SetItemsSource(list, vm.Threads);
		BindableLayout.SetEmptyView(list, Ui.Small(Ui.T("NoDiscussions")));
		BindableLayout.SetItemTemplate(list, new DataTemplate(() =>
		{
			var title = new Label { FontAttributes = FontAttributes.Bold };
			title.SetBinding(Label.TextProperty, nameof(DiscussionThread.Title));
			title.SetBinding(Label.FontAttributesProperty, nameof(DiscussionThread.IsPinned), converter: new BoldWhenTrue());
			var meta = Ui.Small();
			meta.SetBinding(Label.TextProperty, new MultiBinding
			{
				Bindings = { new Binding(nameof(DiscussionThread.AuthorName)), new Binding(nameof(DiscussionThread.ReplyCount)) },
				StringFormat = "{0} · " + Ui.T("Replies").Replace("{0}", "{1}")
			});
			var card = Ui.Card(new VerticalStackLayout { Children = { title, meta } });
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
		vm.Started += async (_, thread) => await Navigation.PushAsync(new DiscussionPage(new DiscussionViewModel(client), thread.Id));
		Content = Ui.Page(Title, vm, list, Ui.Heading(Ui.T("NewDiscussion")), newTitle, newMessage,
			new Button { Text = Ui.T("Start"), Command = vm.StartCommand }).Content;
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
		var messages = new VerticalStackLayout { Spacing = 4 };
		BindableLayout.SetItemsSource(messages, vm.Messages);
		BindableLayout.SetItemTemplate(messages, new DataTemplate(() =>
		{
			var who = new Label { FontAttributes = FontAttributes.Bold };
			who.SetBinding(Label.TextProperty, nameof(DiscussionReply.AuthorName));
			var when = Ui.Small();
			when.SetBinding(Label.TextProperty, nameof(DiscussionReply.CreatedAt), stringFormat: "{0:g}");
			var body = new Label();
			body.SetBinding(Label.TextProperty, nameof(DiscussionReply.Body));
			return Ui.Card(new VerticalStackLayout { Children = { who, when, body } });
		}));
		var heading = Ui.Heading("");
		heading.SetBinding(Label.TextProperty, $"{nameof(DiscussionViewModel.Thread)}.{nameof(DiscussionThread.Title)}");
		var reply = new Editor { Placeholder = Ui.T("WriteReply"), AutoSize = EditorAutoSizeOption.TextChanges, MinimumHeightRequest = 80 };
		reply.SetBinding(Editor.TextProperty, nameof(DiscussionViewModel.Reply));
		var send = new Button { Text = Ui.T("Send"), Command = vm.ReplyCommand };
		var replyBox = new VerticalStackLayout { Spacing = 6, Children = { reply, send } };
		replyBox.SetBinding(VisualElement.IsVisibleProperty, nameof(DiscussionViewModel.CanReply));
		var closed = Ui.Small(Ui.T("DiscussionClosed"));
		closed.SetBinding(VisualElement.IsVisibleProperty, nameof(DiscussionViewModel.CanReply), converter: new Not());
		Content = Ui.Page(Title, vm, heading, messages, replyBox, closed).Content;
		Ui.ApplyDirection(this);
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await _vm.LoadAsync(_threadId);
	}

	private sealed class Not : IValueConverter
	{
		public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is not true;
		public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
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
		var students = new VerticalStackLayout { Spacing = 4 };
		BindableLayout.SetItemsSource(students, vm.Students);
		BindableLayout.SetItemTemplate(students, new DataTemplate(() =>
		{
			var name = new Label { VerticalOptions = LayoutOptions.Center };
			name.SetBinding(Label.TextProperty, nameof(AttendanceMark.Name));
			var status = new Button { WidthRequest = 120 };
			status.SetBinding(Button.TextProperty, nameof(AttendanceMark.StatusText));
			status.Clicked += (s, _) => ((s as Button)?.BindingContext as AttendanceMark)?.Next();
			var grid = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) } };
			grid.Add(name, 0);
			grid.Add(status, 1);
			return Ui.Card(grid);
		}));
		Content = Ui.Page(Title, vm, date, students, new Button { Text = Ui.T("Save"), Command = vm.SaveCommand }).Content;
		Ui.ApplyDirection(this);
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		if (_vm.Students.Count == 0) await _vm.LoadAsync(_offeringId);
	}
}

public sealed class GradingPage : ContentPage
{
	private readonly GradingViewModel _vm;
	private readonly CourseAssignment _assignment;

	public GradingPage(GradingViewModel vm, CourseAssignment assignment)
	{
		_vm = vm;
		_assignment = assignment;
		BindingContext = vm;
		Title = assignment.Title;
		var list = new VerticalStackLayout { Spacing = 6 };
		BindableLayout.SetItemsSource(list, vm.Submissions);
		BindableLayout.SetItemTemplate(list, new DataTemplate(() =>
		{
			var who = new Label { FontAttributes = FontAttributes.Bold };
			who.SetBinding(Label.TextProperty, nameof(GradeEntry.Student));
			var marks = new Entry { Placeholder = Ui.T("Marks"), Keyboard = Keyboard.Numeric };
			marks.SetBinding(Entry.TextProperty, nameof(GradeEntry.Marks));
			var feedback = new Editor { Placeholder = Ui.T("Feedback"), AutoSize = EditorAutoSizeOption.TextChanges };
			feedback.SetBinding(Editor.TextProperty, nameof(GradeEntry.Feedback));
			var save = new Button { Text = Ui.T("SaveGrade"), Command = vm.SaveCommand };
			save.SetBinding(Button.CommandParameterProperty, ".");
			return Ui.Card(new VerticalStackLayout { Spacing = 4, Children = { who, marks, feedback, save } });
		}));
		Content = Ui.Page(Title, vm, list).Content;
		Ui.ApplyDirection(this);
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		if (_vm.Submissions.Count == 0) await _vm.LoadAsync(_assignment);
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
		Content = Ui.Page(Title, vm, title, body, new Button { Text = Ui.T("Post"), Command = vm.PostCommand }).Content;
		Ui.ApplyDirection(this);
	}
}

// ── results, attendance ───────────────────────────────────────────────────────────────────────

public sealed class ResultsPage : ContentPage
{
	private readonly ResultsViewModel _results;
	private readonly AttendanceViewModel _attendance;
	private readonly ReportCardsViewModel _reportCards;

	public ResultsPage(ResultsViewModel results, AttendanceViewModel attendance, ReportCardsViewModel reportCards)
	{
		_results = results;
		_attendance = attendance;
		_reportCards = reportCards;
		BindingContext = results;
		Title = Ui.T("Results");
		Content = new ScrollView { Content = Build(results, attendance, reportCards) };
		Ui.ApplyDirection(this);
	}

	/// <summary>Report cards: one card per period with its lines (headings in bold).</summary>
	internal static View ReportCards(ReportCardsViewModel vm)
	{
		var cards = new VerticalStackLayout { Spacing = 6, BindingContext = vm };
		BindableLayout.SetItemsSource(cards, vm.Cards);
		BindableLayout.SetEmptyView(cards, Ui.Small(Ui.T("NoReportCards")));
		BindableLayout.SetItemTemplate(cards, new DataTemplate(() =>
		{
			var period = new Label { FontAttributes = FontAttributes.Bold };
			period.SetBinding(Label.TextProperty, nameof(ReportCard.PeriodLabel));
			var generated = Ui.Small();
			generated.SetBinding(Label.TextProperty, nameof(ReportCard.GeneratedAt), stringFormat: "{0:d}");
			var lines = new VerticalStackLayout { Spacing = 2 };
			lines.SetBinding(BindableLayout.ItemsSourceProperty, nameof(ReportCard.Lines));
			BindableLayout.SetItemTemplate(lines, new DataTemplate(() =>
			{
				var label = new Label();
				label.SetBinding(Label.TextProperty, nameof(ReportCardLine.Label));
				label.SetBinding(Label.FontAttributesProperty, nameof(ReportCardLine.IsHeading), converter: new BoldWhenTrue());
				var value = new Label { HorizontalTextAlignment = TextAlignment.End };
				value.SetBinding(Label.TextProperty, nameof(ReportCardLine.Value));
				var row = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) } };
				row.Add(label, 0);
				row.Add(value, 1);
				return row;
			}));
			return Ui.Card(new VerticalStackLayout { Spacing = 4, Children = { period, generated, lines } });
		}));
		return new VerticalStackLayout { Spacing = 4, Children = { Ui.Status(vm), Ui.Heading(Ui.T("ReportCards")), cards } };
	}

	internal static View Build(ResultsViewModel results, AttendanceViewModel attendance, ReportCardsViewModel reportCards)
	{
		var resultList = new VerticalStackLayout { Spacing = 4, BindingContext = results };
		BindableLayout.SetItemsSource(resultList, results.Results);
		BindableLayout.SetItemTemplate(resultList, new DataTemplate(() =>
		{
			var course = new Label { FontAttributes = FontAttributes.Bold };
			course.SetBinding(Label.TextProperty, new MultiBinding { Bindings = { new Binding(nameof(ResultItem.CourseCode)), new Binding(nameof(ResultItem.CourseTitle)) }, StringFormat = "{0} — {1}" });
			var marks = Ui.Small();
			marks.SetBinding(Label.TextProperty, new MultiBinding
			{
				Bindings = { new Binding(nameof(ResultItem.ResultType)), new Binding(nameof(ResultItem.MarksObtained)), new Binding(nameof(ResultItem.MaxMarks)), new Binding(nameof(ResultItem.Percentage)) },
				StringFormat = "{0}: {1:0.##}/{2:0.##} ({3:0.#}%)"
			});
			return Ui.Card(new VerticalStackLayout { Children = { course, marks } });
		}));
		var attendanceList = new VerticalStackLayout { Spacing = 4, BindingContext = attendance };
		BindableLayout.SetItemsSource(attendanceList, attendance.Courses);
		BindableLayout.SetItemTemplate(attendanceList, new DataTemplate(() =>
		{
			var course = new Label { FontAttributes = FontAttributes.Bold };
			course.SetBinding(Label.TextProperty, nameof(AttendanceCourse.CourseTitle));
			var rate = Ui.Small();
			rate.SetBinding(Label.TextProperty, new MultiBinding
			{
				Bindings = { new Binding(nameof(AttendanceCourse.Attended)), new Binding(nameof(AttendanceCourse.Total)), new Binding(nameof(AttendanceCourse.Percent)) },
				StringFormat = "{0}/{1} ({2:0.#}%)"
			});
			return Ui.Card(new VerticalStackLayout { Children = { course, rate } });
		}));
		return new VerticalStackLayout
		{
			Padding = 16, Spacing = 8,
			Children = { Ui.Status(results), Ui.Heading(Ui.T("Results")), resultList, Ui.Heading(Ui.T("Attendance")), attendanceList, ReportCards(reportCards) }
		};
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		await Task.WhenAll(_results.LoadAsync(null), _attendance.LoadAsync(null), _reportCards.LoadAsync(null));
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
			ItemTemplate = new DataTemplate(() =>
			{
				var name = new Label { FontAttributes = FontAttributes.Bold };
				name.SetBinding(Label.TextProperty, nameof(ChildSummary.Name));
				var sub = Ui.Small();
				sub.SetBinding(Label.TextProperty, new MultiBinding { Bindings = { new Binding(nameof(ChildSummary.RegistrationNumber)), new Binding(nameof(ChildSummary.ProgramName)) }, StringFormat = "{0} · {1}" });
				return new ContentView { Padding = new Thickness(16, 4), Content = Ui.Card(new VerticalStackLayout { Children = { name, sub } }) };
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
		var announcements = new VerticalStackLayout { Spacing = 4 };
		BindableLayout.SetItemsSource(announcements, vm.Announcements);
		BindableLayout.SetItemTemplate(announcements, new DataTemplate(() =>
		{
			var title = new Label { FontAttributes = FontAttributes.Bold };
			title.SetBinding(Label.TextProperty, nameof(AnnouncementItem.Title));
			var body = new Label();
			body.SetBinding(Label.TextProperty, nameof(AnnouncementItem.Body));
			return Ui.Card(new VerticalStackLayout { Children = { title, body } });
		}));
		var fees = Ui.Card(new Label { Text = $"{Ui.T("Fees")}: {child.OutstandingFees:N0} · {Ui.T("Cgpa")}: {child.Cgpa:0.00}" });
		Content = new ScrollView
		{
			Content = new VerticalStackLayout
			{
				Children = { new ContentView { Padding = new Thickness(16, 8, 16, 0), Content = fees }, ResultsPage.Build(vm.Results, vm.Attendance, vm.ReportCards),
					new VerticalStackLayout { Padding = 16, Children = { Ui.Heading(Ui.T("Announcements")), announcements } } }
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
			EmptyView = Ui.T("NoNotifications"),
			ItemTemplate = new DataTemplate(() =>
			{
				var title = new Label();
				title.SetBinding(Label.TextProperty, nameof(NotificationItem.Title));
				title.SetBinding(Label.FontAttributesProperty, nameof(NotificationItem.IsRead), converter: new UnreadBold());
				var body = Ui.Small();
				body.SetBinding(Label.TextProperty, nameof(NotificationItem.Body));
				var card = Ui.Card(new VerticalStackLayout { Children = { title, body } });
				var tap = new TapGestureRecognizer { Command = vm.ReadCommand };
				tap.SetBinding(TapGestureRecognizer.CommandParameterProperty, ".");
				card.GestureRecognizers.Add(tap);
				return new ContentView { Padding = new Thickness(16, 2), Content = card };
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

	private sealed class UnreadBold : IValueConverter
	{
		public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) => value is true ? FontAttributes.None : FontAttributes.Bold;
		public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
	}
}

public sealed class SettingsPage : ContentPage
{
	public SettingsPage(SettingsViewModel vm)
	{
		BindingContext = vm;
		Title = Ui.T("Settings");
		var languages = new Picker { Title = Ui.T("Language"), ItemsSource = new[] { "English", "اردو", "العربية" } };
		var codes = new[] { "en", "ur", "ar" };
		languages.SelectedIndex = Math.Max(0, Array.IndexOf(codes, vm.Language));
		languages.SelectedIndexChanged += (_, _) =>
		{
			if (languages.SelectedIndex < 0) return;
			vm.Language = codes[languages.SelectedIndex];
			Preferences.Default.Set("language", vm.Language);
			(Application.Current as App)?.ShowApp(); // rebuild the tabs in the new language and direction
		};
		Content = Ui.Page(Title, vm,
			new Label { Text = Ui.T("Language") }, languages,
			new Button { Text = Ui.T("SignOut"), Command = vm.SignOutCommand },
			Ui.Small(vm.VersionText)).Content;
		Ui.ApplyDirection(this);
	}
}
