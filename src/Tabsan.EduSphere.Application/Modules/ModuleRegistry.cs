using Tabsan.EduSphere.Domain.Enums;
using Tabsan.EduSphere.Domain.Modules;

namespace Tabsan.EduSphere.Application.Modules;

/// <summary>
/// Compile-time catalogue of every module's access requirements.
/// Keys must match the stable keys seeded in the <c>modules</c> table.
/// </summary>
public static class ModuleRegistry
{
    private static readonly string[] AllRoles   = [];
    private static readonly string[] AdminTier  = ["SuperAdmin", "Admin"];
    private static readonly string[] SuperOnly  = ["SuperAdmin"];
    private static readonly string[] StaffRoles = ["SuperAdmin", "Admin", "Faculty"];

    private static readonly IReadOnlyDictionary<string, ModuleDescriptor> _descriptors =
        new Dictionary<string, ModuleDescriptor>(StringComparer.OrdinalIgnoreCase)
        {
            // ── Core / Mandatory ────────────────────────────────────────────────
            ["authentication"]  = new("authentication",  AllRoles,   null, false),
            ["departments"]     = new("departments",     AdminTier,  null, false),
            ["courses"]         = new("courses",         AllRoles,   null, false),
            ["sis"]             = new("sis",             AllRoles,   null, false),

            // ── Learning & Assessment ────────────────────────────────────────────
            ["assignments"]     = new("assignments",     AllRoles,   null, false),
            ["attendance"]      = new("attendance",      AllRoles,   null, false),
            ["results"]         = new("results",         AllRoles,   null, false),
            ["quizzes"]         = new("quizzes",         AllRoles,   null, false),

            // ── University-specific ──────────────────────────────────────────────
            ["fyp"]             = new("fyp",             AllRoles,   [InstitutionType.University], false),

            // ── Communication / Utility ──────────────────────────────────────────
            ["notifications"]   = new("notifications",   AllRoles,   null, false),
            ["ai_chat"]         = new("ai_chat",         AllRoles,   null, true),   // license-gated

            // ── Reporting / Admin ────────────────────────────────────────────────
            ["reports"]         = new("reports",         StaffRoles, null, false),
            ["themes"]          = new("themes",          AdminTier,  null, false),
            ["advanced_audit"]  = new("advanced_audit",  SuperOnly,  null, false),

            // ── Compliance & Governance (SuperAdmin only) ───────────────────────
            ["iso_compliance"]      = new("iso_compliance",      SuperOnly, null, false),
            ["backup_dr"]           = new("backup_dr",           SuperOnly, null, false),
            ["document_management"] = new("document_management", SuperOnly, null, false),
        };

    /// <summary>Returns the descriptor for <paramref name="key"/>, or null if not registered.</summary>
    public static ModuleDescriptor? Get(string key)
        => _descriptors.TryGetValue(key, out var d) ? d : null;

    /// <summary>Returns all registered descriptors.</summary>
    public static IEnumerable<ModuleDescriptor> All() => _descriptors.Values;
}
