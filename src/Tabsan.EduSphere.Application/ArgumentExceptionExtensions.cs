namespace Tabsan.EduSphere.Application;

public static class ArgumentExceptionExtensions
{
    /// <summary>The message as written, without the " (Parameter 'x')" that ArgumentException adds (domain messages are
    /// shown to people as they are).</summary>
    public static string Plain(this ArgumentException ex)
    {
        var message = ex.Message;
        var cut = ex.ParamName is null ? -1 : message.LastIndexOf($" (Parameter '{ex.ParamName}')", StringComparison.Ordinal);
        return cut > 0 ? message[..cut] : message;
    }
}
