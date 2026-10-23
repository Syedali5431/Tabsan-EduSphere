#if FIREBASE
using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using Firebase.Messaging;
using Tabsan.EduSphere.Mobile.Services;

namespace Tabsan.EduSphere.Mobile;

// Plan M12 — push notifications on Android through Firebase Cloud Messaging. Compiled only into builds that include the
// institution's google-services.json (see the project file and mobile/README.md, "Push notifications").

/// <summary>Receives new tokens and the messages that arrive while the app is open.</summary>
[Service(Exported = false)]
[IntentFilter(["com.google.firebase.MESSAGING_EVENT"])]
public sealed class PushMessagingService : FirebaseMessagingService
{
	public override void OnNewToken(string token) => PushTokenProvider.SetToken(token);

	/// <summary>In the background Android shows the notification itself; in the foreground the app shows it the same way.</summary>
	public override void OnMessageReceived(RemoteMessage message)
	{
		if (message.GetNotification() is { } n)
			AndroidPush.Show(this, n.Title ?? "EduSphere", n.Body ?? "", message.Data);
	}
}

internal static class AndroidPush
{
	/// <summary>The channel the API names in every message (FcmPushSender: android.notification.channel_id).</summary>
	public const string ChannelId = "edusphere";

	/// <summary>Starts Firebase and lets the app ask for the token; without a usable configuration the app runs without push.</summary>
	public static void Start(Context context)
	{
		CreateChannel(context);
		try
		{
			Firebase.FirebaseApp.InitializeApp(context);
			PushTokenProvider.Fetch = () =>
			{
				var source = new TaskCompletionSource<string?>();
				FirebaseMessaging.Instance.GetToken().AddOnCompleteListener(new TokenListener(source));
				return source.Task;
			};
		}
		catch (Exception ex) when (ex is Java.Lang.Exception or InvalidOperationException)
		{
			PushTokenProvider.Fetch = null;
		}
	}

	public static void Show(Context context, string title, string body, IDictionary<string, string> data)
	{
		var intent = context.PackageManager?.GetLaunchIntentForPackage(context.PackageName!);
		if (intent is null) return;
		intent.AddFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop);
		foreach (var (key, value) in data) intent.PutExtra(key, value);
		var tap = PendingIntent.GetActivity(context, Random.Shared.Next(), intent, PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent);
		var notification = new NotificationCompat.Builder(context, ChannelId)
			.SetContentTitle(title)
			.SetContentText(body)
			.SetStyle(new NotificationCompat.BigTextStyle().BigText(body))
			.SetSmallIcon(context.ApplicationInfo?.Icon ?? 0)
			.SetAutoCancel(true)
			.SetContentIntent(tap)
			.Build();
		NotificationManagerCompat.From(context).Notify(Random.Shared.Next(), notification);
	}

	private static void CreateChannel(Context context)
	{
		if (Build.VERSION.SdkInt < BuildVersionCodes.O) return;
		if (context.GetSystemService(Context.NotificationService) is NotificationManager manager)
			manager.CreateNotificationChannel(new NotificationChannel(ChannelId, "EduSphere", NotificationImportance.High));
	}

	private sealed class TokenListener(TaskCompletionSource<string?> source) : Java.Lang.Object, Android.Gms.Tasks.IOnCompleteListener
	{
		public void OnComplete(Android.Gms.Tasks.Task task) => source.TrySetResult(task.IsSuccessful ? task.Result?.ToString() : null);
	}
}
#endif
