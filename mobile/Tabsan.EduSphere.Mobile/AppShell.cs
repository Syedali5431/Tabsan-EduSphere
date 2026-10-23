using Tabsan.EduSphere.Mobile.Core;
using Tabsan.EduSphere.Mobile.Pages;
using Tabsan.EduSphere.Mobile.Services;

namespace Tabsan.EduSphere.Mobile;

/// <summary>Plan M12 — the tabs for the signed-in role: students, teachers and parents each see their own app.</summary>
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

		var tabs = new TabBar();
		tabs.Items.Add(Tab("Home", new HomePage(new HomeViewModel(client), client, launcher, pickers, () => portal)));
		if (state.IsStudent || state.IsTeacher)
		{
			tabs.Items.Add(Tab("Timetable", new TimetablePage(new TimetableViewModel(client))));
			tabs.Items.Add(Tab("Courses", new CoursesPage(new CoursesViewModel(client), client, launcher, pickers, () => portal)));
		}
		if (state.IsStudent)
		{
			tabs.Items.Add(Tab("Results", new ResultsPage(new ResultsViewModel(client), new AttendanceViewModel(client), new ReportCardsViewModel(client))));
		}
		if (state.IsParent)
			tabs.Items.Add(Tab("Children", new ChildrenPage(new ChildrenViewModel(client), client)));
		tabs.Items.Add(Tab("Notifications", new NotificationsPage(new NotificationsViewModel(client))));
		tabs.Items.Add(Tab("Settings", new SettingsPage(new SettingsViewModel(client, services.GetRequiredService<IPushTokenProvider>(), settings))));
		Items.Add(tabs);
		FlowDirection = AppText.IsRtl ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
	}

	/// <summary>Whether the signed-in role has this tab (routes are the tab keys in lower case).</summary>
	public bool HasRoute(string route) => Items.SelectMany(i => i.Items).SelectMany(s => s.Items).Any(c => c.Route == route);

	private static ShellContent Tab(string key, Page page) => new() { Title = AppText.Get(key), Route = key.ToLowerInvariant(), Content = page };
}
