namespace Tabsan.EduSphere.Domain.Common;

/// <summary>
/// The name part of an uploaded file's name. Some browsers on Windows send the whole path ("C:\Users\ann\Essay.pdf"), and
/// <see cref="Path.GetFileName(string)"/> splits on '\' only when the server runs on Windows, so on Linux the path would be
/// kept. This splits on both '\' and '/' wherever the server runs.
/// </summary>
public static class UploadFileName
{
    /// <summary>
    /// Characters a file name may not contain on Windows. <see cref="Path.GetInvalidFileNameChars"/> on Linux lists only '/'
    /// and '\0', so a name cleaned with it there could still not be saved by someone downloading it on a Windows PC.
    /// </summary>
    public static readonly char[] InvalidChars = [.. Enumerable.Range(0, 32).Select(i => (char)i), '"', '<', '>', '|', ':', '*', '?', '\\', '/'];

    public static string Of(string? name)
    {
        if (string.IsNullOrEmpty(name)) return string.Empty;
        var cut = name.LastIndexOfAny(['\\', '/']);
        return cut < 0 ? name : name[(cut + 1)..];
    }
}
