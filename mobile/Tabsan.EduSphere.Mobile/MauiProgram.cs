using System.Text.Json;
using Microsoft.Extensions.Logging;
using Tabsan.EduSphere.Mobile.Core;
using Tabsan.EduSphere.Mobile.Services;

namespace Tabsan.EduSphere.Mobile;

public static class MauiProgram
{
	/// <summary>The institution's settings packaged with this build (see the WriteWhiteLabel target in the project file).</summary>
	public sealed record WhiteLabel(string AppName, string ApiBaseUrl, string AppColor, string AppScheme, string AppVersion);

	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		var label = LoadWhiteLabel();
		if (Color.TryParse(label.AppColor, out var brand)) Pages.Ui.Brand = brand;
#if ANDROID
		// Fields are drawn in rounded frames (Pages/Ui.Field), so drop Android's underline.
		static void NoUnderline(Android.Views.View view) => view.BackgroundTintList = Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
		Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("Framed", (h, _) => NoUnderline(h.PlatformView));
		Microsoft.Maui.Handlers.EditorHandler.Mapper.AppendToMapping("Framed", (h, _) => NoUnderline(h.PlatformView));
		Microsoft.Maui.Handlers.PickerHandler.Mapper.AppendToMapping("Framed", (h, _) => NoUnderline(h.PlatformView));
		Microsoft.Maui.Handlers.DatePickerHandler.Mapper.AppendToMapping("Framed", (h, _) => NoUnderline(h.PlatformView));
#endif
		var platform = DeviceInfo.Platform == DevicePlatform.iOS ? "ios" : "android";
		var settings = new AppSettings(label.ApiBaseUrl, AppInfo.Current.VersionString is { Length: > 0 } v && v != "1.0" ? v : label.AppVersion, platform, label.AppName);

		builder.Services.AddSingleton(label);
		builder.Services.AddSingleton(settings);
		builder.Services.AddSingleton<ISecureStore, MauiSecureStore>();
		builder.Services.AddSingleton<IAppStorage, FileAppStorage>();
		builder.Services.AddSingleton<IBrowserAuthenticator, MauiBrowserAuthenticator>();
		builder.Services.AddSingleton<IPickers, MauiPickers>();
		builder.Services.AddSingleton<ILauncherService, MauiLauncher>();
		builder.Services.AddSingleton<IPushTokenProvider, PushTokenProvider>();
		builder.Services.AddSingleton(sp => new EduSphereClient(new HttpClient { Timeout = TimeSpan.FromSeconds(30) },
			sp.GetRequiredService<ISecureStore>(), settings) { Language = Preferences.Default.Get("language", "en") });
		builder.Services.AddSingleton(sp => new CrashReporter(sp.GetRequiredService<IAppStorage>(), settings,
			DeviceInfo.Current.VersionString, $"{DeviceInfo.Current.Manufacturer} {DeviceInfo.Current.Model}"));
		builder.Services.AddSingleton<SessionState>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}

	private static WhiteLabel LoadWhiteLabel()
	{
		try
		{
			using var stream = FileSystem.OpenAppPackageFileAsync("whitelabel.json").GetAwaiter().GetResult();
			return JsonSerializer.Deserialize<WhiteLabel>(stream, new JsonSerializerOptions(JsonSerializerDefaults.Web))
				?? new WhiteLabel("EduSphere", "https://localhost:7061", "#0F2A4A", "edusphere", "1.0.0");
		}
		catch (Exception ex) when (ex is FileNotFoundException or JsonException)
		{
			return new WhiteLabel("EduSphere", "https://localhost:7061", "#0F2A4A", "edusphere", "1.0.0");
		}
	}
}
