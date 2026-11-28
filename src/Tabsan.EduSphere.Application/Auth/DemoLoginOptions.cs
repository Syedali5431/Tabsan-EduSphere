namespace Tabsan.EduSphere.Application.Auth;

/// <summary>
/// Demo installations only: one shared sign-in (<see cref="Username"/>, default <c>demo</c>) that opens the account of the
/// role chosen at sign-in, so a single login can show the app (and the portal) as a school admin, teacher, student or parent.
/// The password checked is the demo account's own; the session then belongs to the chosen role's account, so every page
/// behaves exactly as it does for that account. Off unless <c>DemoLogin:Enabled</c> is true — never switch it on for a real
/// institution. Configured, for example, with environment variables:
/// <c>DemoLogin__Enabled=true</c>, <c>DemoLogin__Accounts__0__Role=Admin</c>, <c>DemoLogin__Accounts__0__Username=demo.admin</c>, …
/// </summary>
public sealed class DemoLoginOptions
{
    public const string SectionName = "DemoLogin";

    public bool Enabled { get; set; }

    /// <summary>The shared username people type; its own account holds the password that is checked.</summary>
    public string Username { get; set; } = "demo";

    /// <summary>The roles offered, in order, and the account each one opens. The first is used when no role is chosen (the portal).</summary>
    public List<DemoLoginAccount> Accounts { get; set; } = [];

    /// <summary>Whether demo sign-in is on and <paramref name="username"/> is the shared demo username.</summary>
    public bool IsDemoUsername(string? username)
        => Enabled && Accounts.Count > 0 && !string.IsNullOrWhiteSpace(username)
           && string.Equals(username.Trim(), Username, StringComparison.OrdinalIgnoreCase);

    /// <summary>The account for the chosen role (case-insensitive); the first one when no role is given; null for an unknown role.</summary>
    public DemoLoginAccount? Resolve(string? role)
        => string.IsNullOrWhiteSpace(role)
            ? Accounts.FirstOrDefault()
            : Accounts.FirstOrDefault(a => string.Equals(a.Role, role.Trim(), StringComparison.OrdinalIgnoreCase));
}

/// <summary>One role offered by the demo sign-in.</summary>
public sealed class DemoLoginAccount
{
    /// <summary>The role as the app shows it: Admin, Faculty (teacher), Student or Parent.</summary>
    public string Role { get; set; } = "";

    /// <summary>The account it opens.</summary>
    public string Username { get; set; } = "";
}
