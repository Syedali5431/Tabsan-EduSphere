using Tabsan.EduSphere.Mobile.Core;
using Tabsan.EduSphere.Mobile.Pages;
using Tabsan.EduSphere.Mobile.Services;

namespace Tabsan.EduSphere.Mobile;

/// <summary>
/// Plan M12 — starts on the sign-in page or, with a saved session, on the role's tabs (student, teacher, parent).
/// Unhandled errors are kept on the device and sent as crash reports at the next start.
/// </summary>
public partial class App : Application
{
	private readonly IServiceProvider _services;
	private readonly EduSphereClient _client;
	private readonly CrashReporter _crashes;
	private Window? _window;

	public App(IServiceProvider services, EduSphereClient client, CrashReporter crashes)
	{
		InitializeComponent();
		_services = services;
		_client = client;
		_crashes = crashes;
		AppText.Language = client.Language;
		ApplyTheme();
		AppDomain.CurrentDomain.UnhandledException += (_, e) => { if (e.ExceptionObject is Exception ex) _ = _crashes.RecordAsync(ex); };
		TaskScheduler.UnobservedTaskException += (_, e) => { _ = _crashes.RecordAsync(e.Exception); e.SetObserved(); };
		_client.SignedOut += (_, _) => MainThread.BeginInvokeOnMainThread(ShowSignIn);
		PushTokenProvider.TokenChanged += (_, _) => { if (_client.IsSignedIn) _ = RegisterForPushAsync(); };
	}

	/// <summary>Settings › Theme: "system" follows the phone; "light" and "dark" fix it.</summary>
	public const string ThemeKey = "theme";
	public static readonly string[] Themes = ["system", "light", "dark"];

	public static void ApplyTheme()
	{
		if (Current is null) return;
		Current.UserAppTheme = Preferences.Default.Get(ThemeKey, Themes[0]) switch
		{
			"light" => AppTheme.Light,
			"dark" => AppTheme.Dark,
			_ => AppTheme.Unspecified
		};
	}

	private static string? _pendingScreen;

	/// <summary>
	/// Plan M12: opens the tab a tapped notification points to ("screen" in the message data, see MobileService.BuildMessage);
	/// remembered until the tabs exist. Screens the role has no tab for open the notifications list.
	/// </summary>
	public static void OpenFromNotification(string screen)
	{
		_pendingScreen = screen;
		if (Current is App app && app.Windows.Count > 0 && app.Windows[0].Page is AppShell) MainThread.BeginInvokeOnMainThread(() => _ = GoToPendingAsync());
	}

	private static async Task GoToPendingAsync()
	{
		if (_pendingScreen is not { } screen || Shell.Current is not AppShell shell) return;
		_pendingScreen = null;
		var route = screen switch
		{
			"assignments" => shell.HasRoute("homework") ? "homework" : "courses",
			"results" => shell.HasRoute("results") ? "results" : "home",
			"attendance" => shell.HasRoute("attendance") ? "attendance" : "home",
			"home" => "home",
			_ => "notifications"
		};
		if (!shell.HasRoute(route)) route = "notifications";
		try { await shell.GoToAsync("//" + route); }
		catch (ArgumentException) { /* the role's tabs changed meanwhile */ }
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		// Same as the splash screen (the Tabsan mark on the brand colour) while the saved session is read.
		var starting = new VerticalStackLayout
		{
			Spacing = 24, VerticalOptions = LayoutOptions.Center,
			Children = { new Image { Source = "tabsan_mark.png", HeightRequest = 96 }, new ActivityIndicator { IsRunning = true, Color = Colors.White } }
		};
		_window = new Window(new ContentPage { BackgroundColor = Ui.Brand, Content = starting });
		_ = StartAsync();
		return _window;
	}

	private async Task StartAsync()
	{
		if (await _client.RestoreAsync()) ShowApp();
		else if (!_client.HasServer) ShowServer(); // the standard build's first start: which institute?
		else ShowSignIn();
	}

	/// <summary>The institute's server page as the first page (nothing to go back to).</summary>
	public void ShowServer()
	{
		var page = NewServerPage(canGoBack: false);
		SetRoot(new NavigationPage(page));
	}

	private ServerPage NewServerPage(bool canGoBack)
	{
		var page = new ServerPage(new ServerViewModel(_client), _services.GetRequiredService<MauiProgram.WhiteLabel>(), canGoBack);
		page.Connected += (_, _) => MainThread.BeginInvokeOnMainThread(ShowSignIn);
		return page;
	}

	private bool _serverNext;

	/// <summary>Settings › Change server: signs out, then opens the server page over the sign-in page (Cancel goes back to it).</summary>
	public async Task ChangeServerAsync(Func<Task> signOut)
	{
		_serverNext = true;
		await signOut(); // raises SignedOut → ShowSignIn, which opens the server page while _serverNext is set
		MainThread.BeginInvokeOnMainThread(() => { if (_serverNext) ShowSignIn(); });
	}

	public void ShowSignIn()
	{
		var openServer = _serverNext;
		_serverNext = false;
		var page = new SignInPage(new SignInViewModel(_client, _services.GetRequiredService<IBrowserAuthenticator>(), _services.GetRequiredService<AppSettings>()),
			_services.GetRequiredService<ILauncherService>(), _services.GetRequiredService<MauiProgram.WhiteLabel>());
		var navigation = new NavigationPage(page);
		page.SignedIn += (_, _) => ShowApp();
		page.ChangeServer += async (_, _) => await navigation.PushAsync(NewServerPage(canGoBack: true));
		SetRoot(navigation);
		if (openServer) _ = navigation.PushAsync(NewServerPage(canGoBack: true), animated: false);
	}

	public void ShowApp()
	{
		SetRoot(new AppShell(_services));
		_ = AfterSignInAsync();
		if (_pendingScreen is not null) MainThread.BeginInvokeOnMainThread(() => _ = GoToPendingAsync());
	}

	private async Task AfterSignInAsync()
	{
		await _crashes.SendPendingAsync(_client);
		if (DeviceInfo.Current.Platform == DevicePlatform.Android)
		{
			try { await MainThread.InvokeOnMainThreadAsync(() => Permissions.RequestAsync<Permissions.PostNotifications>()); }
			catch (Exception ex) when (ex is PermissionException or InvalidOperationException) { }
		}
		await RegisterForPushAsync();
	}

	private Task RegisterForPushAsync()
	{
		var settings = new SettingsViewModel(_client, _services.GetRequiredService<IPushTokenProvider>(), _services.GetRequiredService<AppSettings>());
		return settings.RegisterForPushAsync($"{DeviceInfo.Current.Manufacturer} {DeviceInfo.Current.Model}");
	}

	private void SetRoot(Page page)
	{
		page.FlowDirection = AppText.IsRtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
		// The window made in CreateWindow, not Windows[0]: when reading the saved session finishes at once (the iOS keychain,
		// Windows), the first page is set before MAUI has added that window to Windows, and the app stayed on the spinner.
		if ((_window ?? Windows.FirstOrDefault()) is { } window) window.Page = page;
	}
}
