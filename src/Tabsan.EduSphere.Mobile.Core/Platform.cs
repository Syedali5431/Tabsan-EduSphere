using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows.Input;

namespace Tabsan.EduSphere.Mobile.Core;

// Plan M12 — what the app needs from the phone (implemented with MAUI Essentials in the app) and small MVVM helpers.

/// <summary>Opens the system browser for single sign-on and waits for the redirect to the app's URL scheme.</summary>
public interface IBrowserAuthenticator
{
    Task<Uri?> AuthenticateAsync(Uri start, Uri callback);
}

/// <summary>Camera and file pickers for handing in work.</summary>
public interface IPickers
{
    Task<UploadFile?> TakePhotoAsync();
    Task<IReadOnlyList<UploadFile>> PickFilesAsync();
}

/// <summary>Opens a link (material, quiz in the portal, store page).</summary>
public interface ILauncherService
{
    Task OpenAsync(Uri uri);
}

/// <summary>The device's push token (Firebase Cloud Messaging); null when push is not set up in this build.</summary>
public interface IPushTokenProvider
{
    Task<string?> GetTokenAsync();
}

/// <summary>A place to keep small files (pending crash reports).</summary>
public interface IAppStorage
{
    Task<string?> ReadAsync(string name);
    Task WriteAsync(string name, string content);
    void Delete(string name);
}

public abstract class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>An async command that cannot run twice at once.</summary>
public sealed class AsyncCommand(Func<object?, Task> run, Func<object?, bool>? canRun = null) : ICommand
{
    private bool _running;

    public AsyncCommand(Func<Task> run) : this(_ => run()) { }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => !_running && (canRun?.Invoke(parameter) ?? true);

    public async void Execute(object? parameter) => await ExecuteAsync(parameter);

    public async Task ExecuteAsync(object? parameter = null)
    {
        if (!CanExecute(parameter)) return;
        _running = true;
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        try { await run(parameter); }
        finally
        {
            _running = false;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void Refresh() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

/// <summary>Is the app recent enough? (minimum version from the API → must update; latest → may update).</summary>
public static class VersionPolicy
{
    public enum State { Current, UpdateAvailable, UpdateRequired }

    public static State Check(string appVersion, string? minimum, string? latest)
    {
        var app = Parse(appVersion);
        if (app is null) return State.Current;
        if (Parse(minimum) is { } min && app < min) return State.UpdateRequired;
        if (Parse(latest) is { } last && app < last) return State.UpdateAvailable;
        return State.Current;
    }

    private static Version? Parse(string? v)
    {
        if (string.IsNullOrWhiteSpace(v)) return null;
        var core = v.Trim().TrimStart('v', 'V').Split('-', '+')[0];
        if (!core.Contains('.')) core += ".0";
        return Version.TryParse(core, out var version) ? version : null;
    }
}

/// <summary>
/// Self-hosted crash reporting: an unhandled error is written to the device first (the app may be about to close) and sent
/// to the institution's API on the next start. Nothing goes to third parties.
/// </summary>
public sealed class CrashReporter(IAppStorage storage, AppSettings settings, string? osVersion = null, string? deviceModel = null)
{
    private const string FileName = "pending-crash.json";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public string? CurrentScreen { get; set; }

    public async Task RecordAsync(Exception exception)
    {
        var report = new CrashReport(settings.Platform, settings.AppVersion, osVersion, deviceModel, exception.GetType().FullName ?? "Exception",
            exception.Message, exception.ToString(), CurrentScreen, DateTime.UtcNow);
        await storage.WriteAsync(FileName, JsonSerializer.Serialize(report, Json));
    }

    /// <summary>Sends a pending report (once signed in); keeps it when the API cannot be reached.</summary>
    public async Task<bool> SendPendingAsync(EduSphereClient client, CancellationToken ct = default)
    {
        var raw = await storage.ReadAsync(FileName);
        if (string.IsNullOrWhiteSpace(raw) || !client.IsSignedIn) return false;
        CrashReport? report;
        try { report = JsonSerializer.Deserialize<CrashReport>(raw, Json); }
        catch (JsonException) { storage.Delete(FileName); return false; }
        if (report is null) { storage.Delete(FileName); return false; }
        try
        {
            await client.ReportCrashAsync(report, ct);
            storage.Delete(FileName);
            return true;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException) { return false; }
        catch (ApiException) { storage.Delete(FileName); return false; }
    }
}
