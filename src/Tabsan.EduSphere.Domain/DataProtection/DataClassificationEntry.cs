using Tabsan.EduSphere.Domain.Common;

namespace Tabsan.EduSphere.Domain.DataProtection;

/// <summary>
/// Phase 5: Tags a domain entity with a data classification level.
/// ISO 27001 A.8.2.1 — Classification of information.
/// </summary>
public class DataClassificationEntry : BaseEntity
{
    /// <summary>Name of the classified entity type (e.g. "User", "StudentProfile", "Result").</summary>
    public string EntityName { get; private set; } = default!;

    /// <summary>String representation of the classified entity's primary key.</summary>
    public string? EntityId { get; private set; }

    /// <summary>Classification level: Public, Internal, Confidential, or Restricted.</summary>
    public string ClassificationLevel { get; private set; } = default!;

    /// <summary>User ID of the admin who classified this entity.</summary>
    public Guid ClassifiedBy { get; private set; }

    /// <summary>UTC timestamp when classification was applied.</summary>
    public DateTime ClassifiedAt { get; private set; }

    /// <summary>Business justification for the classification level.</summary>
    public string? Justification { get; private set; }

    private DataClassificationEntry() { }

    public DataClassificationEntry(string entityName, string? entityId, string classificationLevel, Guid classifiedBy, string? justification = null)
    {
        EntityName = entityName;
        EntityId = entityId;
        ClassificationLevel = classificationLevel;
        ClassifiedBy = classifiedBy;
        ClassifiedAt = DateTime.UtcNow;
        Justification = justification;
    }
}
