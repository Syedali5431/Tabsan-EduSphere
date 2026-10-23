using Tabsan.EduSphere.Mobile.Core;

namespace Tabsan.EduSphere.Mobile.Services;

// Plan M12 — the phone's features behind the interfaces the core uses (MAUI Essentials).

/// <summary>Tokens in the platform's secure store (Android Keystore, iOS Keychain).</summary>
public sealed class MauiSecureStore : ISecureStore
{
	public Task<string?> GetAsync(string key) => SecureStorage.Default.GetAsync(key);
	public Task SetAsync(string key, string value) => SecureStorage.Default.SetAsync(key, value);
	public void Remove(string key) => SecureStorage.Default.Remove(key);
}

public sealed class FileAppStorage : IAppStorage
{
	private static string PathOf(string name) => Path.Combine(FileSystem.AppDataDirectory, name);

	public async Task<string?> ReadAsync(string name) => File.Exists(PathOf(name)) ? await File.ReadAllTextAsync(PathOf(name)) : null;
	public Task WriteAsync(string name, string content) => File.WriteAllTextAsync(PathOf(name), content);
	public void Delete(string name) { if (File.Exists(PathOf(name))) File.Delete(PathOf(name)); }
}

/// <summary>Single sign-on in the system browser (shared cookies with the identity provider; the app never sees the password).</summary>
public sealed class MauiBrowserAuthenticator : IBrowserAuthenticator
{
	public async Task<Uri?> AuthenticateAsync(Uri start, Uri callback)
	{
		try
		{
			var result = await WebAuthenticator.Default.AuthenticateAsync(new WebAuthenticatorOptions { Url = start, CallbackUrl = callback, PrefersEphemeralWebBrowserSession = false });
			var query = string.Join("&", result.Properties.Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));
			return new Uri($"{callback.GetLeftPart(UriPartial.Path)}?{query}");
		}
		catch (TaskCanceledException) { return null; }
	}
}

/// <summary>Camera and files for handing in work.</summary>
public sealed class MauiPickers : IPickers
{
	public async Task<UploadFile?> TakePhotoAsync()
	{
		if (!MediaPicker.Default.IsCaptureSupported) return null;
		var photo = await MediaPicker.Default.CapturePhotoAsync();
		return photo is null ? null : new UploadFile(photo.FileName, photo.ContentType ?? "image/jpeg", () => photo.OpenReadAsync());
	}

	public async Task<IReadOnlyList<UploadFile>> PickFilesAsync()
	{
		var files = await FilePicker.Default.PickMultipleAsync(new PickOptions());
		return files?.Where(f => f is not null).Select(f => new UploadFile(f!.FileName, f.ContentType ?? "application/octet-stream", () => f.OpenReadAsync())).ToList()
			?? [];
	}
}

public sealed class MauiLauncher : ILauncherService
{
	public Task OpenAsync(Uri uri) => Browser.Default.OpenAsync(uri, BrowserLaunchMode.SystemPreferred);
}

/// <summary>
/// The Firebase Cloud Messaging token. Builds without Firebase return null (no push); builds with the institution's
/// google-services.json start Firebase (Platforms/Android/AndroidPush.cs), which sets <see cref="Fetch"/> and reports new
/// tokens through <see cref="SetToken"/> (see mobile/README.md, "Push notifications").
/// </summary>
public sealed class PushTokenProvider : IPushTokenProvider
{
	private static string? _token;

	/// <summary>Asks the platform's messaging service for the token (set by the Android Firebase start-up).</summary>
	public static Func<Task<string?>>? Fetch { get; set; }

	/// <summary>Raised when the messaging service hands out a new token (the app registers it again).</summary>
	public static event EventHandler? TokenChanged;

	public static void SetToken(string? token)
	{
		if (string.IsNullOrEmpty(token) || token == _token) return;
		_token = token;
		TokenChanged?.Invoke(null, EventArgs.Empty);
	}

	public async Task<string?> GetTokenAsync()
	{
		if (_token is null && Fetch is { } fetch)
		{
			try { _token = await fetch(); }
			catch (Exception) { /* no push this time */ }
		}
		return _token;
	}
}

/// <summary>Who is signed in, for building the role's tabs.</summary>
public sealed class SessionState(EduSphereClient client)
{
	public string Role => client.Session?.Role ?? "";
	public bool IsStudent => Role.Equals("Student", StringComparison.OrdinalIgnoreCase);
	public bool IsTeacher => Role.Equals("Faculty", StringComparison.OrdinalIgnoreCase);
	public bool IsParent => Role.Equals("Parent", StringComparison.OrdinalIgnoreCase);
}
