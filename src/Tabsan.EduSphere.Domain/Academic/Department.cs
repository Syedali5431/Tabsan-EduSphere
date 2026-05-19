using Tabsan.EduSphere.Domain.Common;
using Tabsan.EduSphere.Domain.Enums;

namespace Tabsan.EduSphere.Domain.Academic;

/// <summary>
/// Represents a university department (e.g. Computer Science, Business Administration).
/// Department is the core organisational unit — faculty, courses, programs, and student
/// data are all scoped to a department.
/// </summary>
public class Department : AuditableEntity
{
    /// <summary>Optional tenant association for SaaS-scoped ownership.</summary>
    public Guid? TenantId { get; private set; }

    /// <summary>Optional campus association under the tenant.</summary>
    public Guid? CampusId { get; private set; }

    /// <summary>Full name of the department as displayed in the UI.</summary>
    public string Name { get; private set; } = default!;

    /// <summary>
    /// Short unique code used in registration numbers and reports (e.g. "CS", "BBA").
    /// Unique constraint is enforced at the database level.
    /// </summary>
    public string Code { get; private set; } = default!;

    /// <summary>Controls whether the department is available for assignment and enrolment.</summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Canonical institute dimension for this department. Academic entities scoped by this
    /// department inherit this institution type for parity and governance enforcement.
    /// </summary>
    public InstitutionType InstitutionType { get; private set; } = InstitutionType.University;

    private Department() { }

    public Department(string name, string code, InstitutionType institutionType = InstitutionType.University)
    {
        Name = name;
        Code = code.ToUpperInvariant();
        InstitutionType = institutionType;
    }

    /// <summary>Updates the display name of the department.</summary>
    public void Rename(string newName)
    {
        Name = newName;
        Touch();
    }

    /// <summary>Deactivates the department so it is hidden from assignment dropdowns.</summary>
    public void Deactivate()
    {
        IsActive = false;
        Touch();
    }

    /// <summary>Re-activates a previously deactivated department.</summary>
    public void Activate()
    {
        IsActive = true;
        Touch();
    }

    /// <summary>Updates the canonical institution type for this department.</summary>
    public void SetInstitutionType(InstitutionType institutionType)
    {
        InstitutionType = institutionType;
        Touch();
    }

    /// <summary>Assigns or clears tenant/campus ownership for this department.</summary>
    public void SetTenantCampus(Guid? tenantId, Guid? campusId)
    {
        var hasTenant = tenantId.HasValue;
        var hasCampus = campusId.HasValue;
        if (hasTenant != hasCampus)
            throw new InvalidOperationException("TenantId and CampusId must be provided together.");

        TenantId = tenantId;
        CampusId = campusId;
        Touch();
    }
}
