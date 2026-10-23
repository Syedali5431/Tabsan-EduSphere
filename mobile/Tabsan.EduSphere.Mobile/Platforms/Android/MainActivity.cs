using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;

namespace Tabsan.EduSphere.Mobile;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
	protected override void OnCreate(Bundle? savedInstanceState)
	{
		base.OnCreate(savedInstanceState);
#if FIREBASE
		AndroidPush.Start(this);
#endif
		OpenFromNotification(Intent);
	}

	protected override void OnNewIntent(Intent? intent)
	{
		base.OnNewIntent(intent);
		OpenFromNotification(intent);
	}

	/// <summary>Plan M12: a tapped notification carries the screen to open ("screen" in the message data).</summary>
	private static void OpenFromNotification(Intent? intent)
	{
		if (intent?.GetStringExtra("screen") is { Length: > 0 } screen) App.OpenFromNotification(screen);
	}
}
