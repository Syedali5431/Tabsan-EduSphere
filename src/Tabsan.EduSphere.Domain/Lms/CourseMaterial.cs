using Tabsan.EduSphere.Domain.Common;

namespace Tabsan.EduSphere.Domain.Lms;

public enum CourseMaterialType
{
    File = 1,
    Link = 2,
    FileAndLink = 3
}

/// <summary>
/// Course material uploaded for a scoped academic slice (tenant/campus/department/program/semester/subject-course).
/// </summary>
public class CourseMaterial : AuditableEntity
{
    public Guid TenantId { get; private set; }
    public Guid CampusId { get; private set; }
    public Guid DepartmentId { get; private set; }
    public Guid AcademicProgramId { get; private set; }
    public Guid SemesterId { get; private set; }
    public Guid CourseId { get; private set; }

    public string Name { get; private set; } = default!;
    public string? Description { get; private set; }
    public string? LinkUrl { get; private set; }
    public string? FilePath { get; private set; }

    public CourseMaterialType MaterialType { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public bool IsActive { get; private set; } = true;

    private CourseMaterial() { }

    public CourseMaterial(
        Guid tenantId,
        Guid campusId,
        Guid departmentId,
        Guid academicProgramId,
        Guid semesterId,
        Guid courseId,
        string name,
        Guid createdByUserId,
        CourseMaterialType materialType,
        string? linkUrl = null,
        string? filePath = null,
        string? description = null)
    {
        EnsureRequiredScope(
            tenantId,
            campusId,
            departmentId,
            academicProgramId,
            semesterId,
            courseId,
            createdByUserId);

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Course material name is required.", nameof(name));

        TenantId = tenantId;
        CampusId = campusId;
        DepartmentId = departmentId;
        AcademicProgramId = academicProgramId;
        SemesterId = semesterId;
        CourseId = courseId;
        Name = name.Trim();
        CreatedByUserId = createdByUserId;
        MaterialType = materialType;
        LinkUrl = string.IsNullOrWhiteSpace(linkUrl) ? null : linkUrl.Trim();
        FilePath = string.IsNullOrWhiteSpace(filePath) ? null : filePath.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

        EnsureMaterialLocation();
    }

    public void UpdateMetadata(string name, string? description)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Course material name is required.", nameof(name));

        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        Touch();
    }

    public void UpdateLocation(CourseMaterialType materialType, string? linkUrl, string? filePath)
    {
        MaterialType = materialType;
        LinkUrl = string.IsNullOrWhiteSpace(linkUrl) ? null : linkUrl.Trim();
        FilePath = string.IsNullOrWhiteSpace(filePath) ? null : filePath.Trim();

        EnsureMaterialLocation();
        Touch();
    }

    public void Deactivate()
    {
        IsActive = false;
        Touch();
    }

    public void Activate()
    {
        IsActive = true;
        Touch();
    }

    private void EnsureMaterialLocation()
    {
        var hasLink = !string.IsNullOrWhiteSpace(LinkUrl);
        var hasFile = !string.IsNullOrWhiteSpace(FilePath);

        switch (MaterialType)
        {
            case CourseMaterialType.File when !hasFile:
                throw new InvalidOperationException("File material requires a file path.");
            case CourseMaterialType.Link when !hasLink:
                throw new InvalidOperationException("Link material requires a link URL.");
            case CourseMaterialType.FileAndLink when !hasFile && !hasLink:
                throw new InvalidOperationException("FileAndLink material requires at least one location (file or link).");
        }
    }

    private static void EnsureRequiredScope(
        Guid tenantId,
        Guid campusId,
        Guid departmentId,
        Guid academicProgramId,
        Guid semesterId,
        Guid courseId,
        Guid createdByUserId)
    {
        if (tenantId == Guid.Empty)
            throw new ArgumentException("Tenant scope is required.", nameof(tenantId));
        if (campusId == Guid.Empty)
            throw new ArgumentException("Campus scope is required.", nameof(campusId));
        if (departmentId == Guid.Empty)
            throw new ArgumentException("Department scope is required.", nameof(departmentId));
        if (academicProgramId == Guid.Empty)
            throw new ArgumentException("Academic program scope is required.", nameof(academicProgramId));
        if (semesterId == Guid.Empty)
            throw new ArgumentException("Semester scope is required.", nameof(semesterId));
        if (courseId == Guid.Empty)
            throw new ArgumentException("Course scope is required.", nameof(courseId));
        if (createdByUserId == Guid.Empty)
            throw new ArgumentException("Created-by user is required.", nameof(createdByUserId));
    }
}