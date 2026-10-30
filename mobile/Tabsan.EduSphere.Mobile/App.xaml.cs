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
		AppDomain.CurrentDomain.UnhandledException += (_, e) => { if (e.ExceptionObject is Exception ex) _ = _crashes.RecordAsync(ex); };
		TaskScheduler.UnobservedTaskException += (_, e) => { _ = _crashes.RecordAsync(e.Exception); e.SetObserved(); };
		_client.SignedOut += (_, _) => MainThread.BeginInvokeOnMainThread(ShowSignIn);
		PushTokenProvider.TokenChanged += (_, _) => { if (_client.IsSignedIn) _ = RegisterForPushAsync(); };
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
			"assignments" => "courses",
			"results" or "attendance" => shell.HasRoute("results") ? "results" : "children",
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
		else ShowSignIn();
	}

	public void ShowSignIn()
	{
		var page = new SignInPage(new SignInViewModel(_client, _services.GetRequiredService<IBrowserAuthenticator>(), _services.GetRequiredService<AppSettings>()),
			_services.GetRequiredService<ILauncherService>(), _services.GetRequiredService<MauiProgram.WhiteLabel>());
		page.SignedIn += (_, _) => ShowApp();
		SetRoot(new NavigationPage(page));
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
