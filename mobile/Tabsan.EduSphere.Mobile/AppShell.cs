using Tabsan.EduSphere.Mobile.Core;
using Tabsan.EduSphere.Mobile.Pages;
using Tabsan.EduSphere.Mobile.Services;

namespace Tabsan.EduSphere.Mobile;

/// <summary>
/// Plan M12 — the tabs for the signed-in role. Students and parents: Home, Timetable, Courses, Homework, Attendance, Results;
/// teachers the same without Results; admins Home with their campus. All end with Notifications and Settings (Android puts the
/// tabs after the fourth under More). The child, class and campus chosen on one tab apply to all (one <see cref="Selection"/>).
/// </summary>
public sealed class AppShell : Shell
{
	public AppShell(IServiceProvider services)
	{
		var client = services.GetRequiredService<EduSphereClient>();
		var state = services.GetRequiredService<SessionState>();
		var launcher = services.GetRequiredService<ILauncherService>();
		var pickers = services.GetRequiredService<IPickers>();
		var settings = services.GetRequiredService<AppSettings>();
		string? portal = null;
		_ = Task.Run(async () =>
		{
			try { portal = (await client.GetConfigAsync())?.PortalBaseUrl; }
			catch (Exception ex) when (ex is HttpRequestException or ApiException or TaskCanceledException) { }
		});
		var selection = new Selection(client);
		_ = LoadSelectionAsync(selection);

		var tabs = new TabBar();
		tabs.Items.Add(Tab("Home", new HomePage(new HomeViewModel(client), selection, client, launcher, pickers, () => portal)));
		if (state.IsStudent || state.IsTeacher || state.IsParent)
		{
			tabs.Items.Add(Tab("Timetable", new TimetablePage(new TimetableViewModel(client, selection), selection)));
			tabs.Items.Add(Tab("Courses", new CoursesPage(new CoursesViewModel(client, selection), selection, client, launcher, pickers, () => portal)));
			tabs.Items.Add(Tab("Homework", new HomeworkPage(new HomeworkViewModel(client, selection), selection, client, pickers)));
			tabs.Items.Add(Tab("Attendance", new AttendancePage(new AttendanceScreenViewModel(client, selection), selection, client)));
		}
		if (state.IsStudent || state.IsParent)
			tabs.Items.Add(Tab("Results", new ResultsPage(new ResultsViewModel(client, selection), new ReportCardsViewModel(client), selection)));
		tabs.Items.Add(Tab("Notifications", new NotificationsPage(new NotificationsViewModel(client))));
		tabs.Items.Add(Tab("Settings", new SettingsPage(new SettingsViewModel(client, services.GetRequiredService<IPushTokenProvider>(), settings),
			client.Session?.Username, client.Session?.Role, client.Server)));
		Items.Add(tabs);
		Shell.SetBackgroundColor(this, Ui.Brand); // the institution's colour in the title bar
		FlowDirection = AppText.IsRtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
	}

	private static async Task LoadSelectionAsync(Selection selection)
	{
		try { await selection.LoadAsync(); }
		catch (Exception ex) when (ex is HttpRequestException or ApiException or TaskCanceledException) { /* each tab shows its own error */ }
	}

	/// <summary>Whether the signed-in role has this tab (routes are the tab keys in lower case).</summary>
	public bool HasRoute(string route) => Items.SelectMany(i => i.Items).SelectMany(s => s.Items).Any(c => c.Route == route);

	/// <summary>A tab with its icon (Resources/Images/tab_&lt;key&gt;.svg).</summary>
	private static ShellContent Tab(string key, Page page)
		=> new() { Title = AppText.Get(key), Route = key.ToLowerInvariant(), Content = page, Icon = $"tab_{key.ToLowerInvariant()}.png" };
}
