using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabsan.EduSphere.Domain.Academic;
using Tabsan.EduSphere.Domain.Settings;

namespace Tabsan.EduSphere.Infrastructure.Persistence.Configurations;

// ─────────────────────────────────────────────────────────────────────────────
// Timetable
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>EF Core configuration for the Timetable entity.</summary>
public class TimetableConfiguration : IEntityTypeConfiguration<Timetable>
{
    public void Configure(EntityTypeBuilder<Timetable> builder)
    {
        builder.ToTable("timetables");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.AcademicProgramId)
               .IsRequired();

        builder.Property(t => t.SemesterNumber)
               .IsRequired();

        builder.Property(t => t.EffectiveDate)
               .IsRequired()
               .HasColumnType("date");

        builder.Property(t => t.IsPublished)
               .IsRequired()
               .HasDefaultValue(false);

        builder.Property(t => t.PublishedAt)
               .IsRequired(false);

        // Composite index: common query is "get all timetables for dept/program in semester"
        builder.HasIndex(t => new { t.DepartmentId, t.AcademicProgramId, t.SemesterId })
               .HasDatabaseName("IX_timetables_dept_program_semester");

        builder.HasOne(t => t.Department)
               .WithMany()
               .HasForeignKey(t => t.DepartmentId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Semester)
               .WithMany()
               .HasForeignKey(t => t.SemesterId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.AcademicProgram)
               .WithMany()
               .HasForeignKey(t => t.AcademicProgramId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(t => t.Entries)
               .WithOne(e => e.Timetable)
               .HasForeignKey(e => e.TimetableId)
               .OnDelete(DeleteBehavior.Cascade);

        // Soft-delete filter — exclude logically deleted timetables from normal queries.
        builder.HasQueryFilter(t => !t.IsDeleted);
    }
}

/// <summary>EF Core configuration for TimetableEntry.</summary>
public class TimetableEntryConfiguration : IEntityTypeConfiguration<TimetableEntry>
{
    public void Configure(EntityTypeBuilder<TimetableEntry> builder)
    {
        builder.ToTable("timetable_entries");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.DayOfWeek)
               .IsRequired();

        // EF Core 8 maps TimeOnly to SQL Server TIME(7) natively.
        builder.Property(e => e.StartTime)
               .IsRequired();

        builder.Property(e => e.EndTime)
               .IsRequired();

        builder.Property(e => e.SubjectName)
               .IsRequired()
               .HasMaxLength(200);

        builder.Property(e => e.RoomNumber)
               .HasMaxLength(50)
               .IsRequired(false);

        builder.Property(e => e.FacultyName)
               .HasMaxLength(200)
               .IsRequired(false);

        builder.Property(e => e.CourseId)
               .IsRequired(false);

        builder.Property(e => e.FacultyUserId)
               .IsRequired(false);

        builder.Property(e => e.RoomId)
               .IsRequired(false);

        builder.Property(e => e.BuildingId)
               .IsRequired(false);

        // FK: Course (nullable, Restrict to preserve entry if course is deleted)
        builder.HasOne(e => e.Course)
               .WithMany()
               .HasForeignKey(e => e.CourseId)
               .IsRequired(false)
               .OnDelete(DeleteBehavior.Restrict);

        // FK: User/Faculty (nullable, Restrict)
        builder.HasOne<Tabsan.EduSphere.Domain.Identity.User>()
               .WithMany()
               .HasForeignKey(e => e.FacultyUserId)
               .IsRequired(false)
               .OnDelete(DeleteBehavior.Restrict);

        // FK: Room (nullable, Restrict)
        builder.HasOne(e => e.Room)
               .WithMany()
               .HasForeignKey(e => e.RoomId)
               .IsRequired(false)
               .OnDelete(DeleteBehavior.Restrict);

        // FK: Building (nullable, Restrict — denormalized for query performance)
        builder.HasOne(e => e.Building)
               .WithMany()
               .HasForeignKey(e => e.BuildingId)
               .IsRequired(false)
               .OnDelete(DeleteBehavior.Restrict);

        // Index for quick day-of-week slot lookups within a timetable.
        builder.HasIndex(e => new { e.TimetableId, e.DayOfWeek })
               .HasDatabaseName("IX_timetable_entries_timetable_day");

        // Index for teacher-filtered dashboard query
        builder.HasIndex(e => e.FacultyUserId)
               .HasDatabaseName("IX_timetable_entries_faculty_user");

              // Match principal Timetable soft-delete filter to avoid required-relationship filter warnings.
              builder.HasQueryFilter(e => !e.Timetable.IsDeleted);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Report Settings
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>EF Core configuration for ReportDefinition.</summary>
public class ReportDefinitionConfiguration : IEntityTypeConfiguration<ReportDefinition>
{
    public void Configure(EntityTypeBuilder<ReportDefinition> builder)
    {
        builder.ToTable("report_definitions");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Key)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(r => r.Name)
               .IsRequired()
               .HasMaxLength(150);

        builder.Property(r => r.Purpose)
               .IsRequired()
               .HasMaxLength(500);

        builder.Property(r => r.IsActive)
               .IsRequired()
               .HasDefaultValue(true);

        // Stable key must be unique — prevents duplicate seeded reports.
        builder.HasIndex(r => r.Key)
               .IsUnique()
               .HasDatabaseName("IX_report_definitions_key");

        builder.HasMany(r => r.RoleAssignments)
               .WithOne(a => a.ReportDefinition)
               .HasForeignKey(a => a.ReportDefinitionId)
               .OnDelete(DeleteBehavior.Cascade);

        // Soft-delete filter.
        builder.HasQueryFilter(r => !r.IsDeleted);
    }
}

/// <summary>EF Core configuration for ReportRoleAssignment (junction).</summary>
public class ReportRoleAssignmentConfiguration : IEntityTypeConfiguration<ReportRoleAssignment>
{
    public void Configure(EntityTypeBuilder<ReportRoleAssignment> builder)
    {
        builder.ToTable("report_role_assignments");
        builder.HasKey(a => a.Id);

        // Composite unique constraint: one assignment per (report, role).
        builder.HasIndex(a => new { a.ReportDefinitionId, a.RoleName })
               .IsUnique()
               .HasDatabaseName("IX_report_role_assignments_unique");

        builder.Property(a => a.RoleName)
               .IsRequired()
               .HasMaxLength(50);

        builder.HasOne(a => a.ReportDefinition)
               .WithMany(r => r.RoleAssignments)
               .HasForeignKey(a => a.ReportDefinitionId)
               .OnDelete(DeleteBehavior.Cascade);

              // Match principal ReportDefinition soft-delete filter to avoid required-relationship filter warnings.
              builder.HasQueryFilter(a => !a.ReportDefinition.IsDeleted);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Module Role Assignments
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>EF Core configuration for ModuleRoleAssignment (junction).</summary>
public class ModuleRoleAssignmentConfiguration : IEntityTypeConfiguration<ModuleRoleAssignment>
{
    public void Configure(EntityTypeBuilder<ModuleRoleAssignment> builder)
    {
        builder.ToTable("module_role_assignments");
        builder.HasKey(a => a.Id);

        // Composite unique constraint: one assignment per (module, role).
        builder.HasIndex(a => new { a.ModuleId, a.RoleName })
               .IsUnique()
               .HasDatabaseName("IX_module_role_assignments_unique");

        builder.Property(a => a.RoleName)
               .IsRequired()
               .HasMaxLength(50);

        builder.HasOne(a => a.Module)
               .WithMany()
               .HasForeignKey(a => a.ModuleId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// Building & Room
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>EF Core configuration for the Building entity.</summary>
public class BuildingConfiguration : IEntityTypeConfiguration<Building>
{
    public void Configure(EntityTypeBuilder<Building> builder)
    {
        builder.ToTable("buildings");
        builder.HasKey(b => b.Id);

        builder.Property(b => b.Name)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(b => b.Code)
               .IsRequired()
               .HasMaxLength(20);

        builder.Property(b => b.TenantId)
               .IsRequired(false);

        builder.Property(b => b.CampusId)
               .IsRequired(false);

        builder.Property(b => b.IsActive)
               .IsRequired()
               .HasDefaultValue(true);

        builder.HasIndex(b => new { b.TenantId, b.CampusId, b.Code })
               .IsUnique()
               .HasDatabaseName("IX_buildings_scope_code");

        builder.HasMany(b => b.Rooms)
               .WithOne(r => r.Building)
               .HasForeignKey(r => r.BuildingId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(b => !b.IsDeleted);
    }
}

/// <summary>EF Core configuration for the Room entity.</summary>
public class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> builder)
    {
        builder.ToTable("rooms");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Number)
               .IsRequired()
               .HasMaxLength(50);

        builder.Property(r => r.Capacity)
               .IsRequired(false);

        builder.Property(r => r.TenantId)
               .IsRequired(false);

        builder.Property(r => r.CampusId)
               .IsRequired(false);

        builder.Property(r => r.IsActive)
               .IsRequired()
               .HasDefaultValue(true);

        // Unique room number within a building and scope.
        builder.HasIndex(r => new { r.TenantId, r.CampusId, r.BuildingId, r.Number })
               .IsUnique()
               .HasDatabaseName("IX_rooms_scope_building_number");

        builder.HasOne(r => r.Building)
               .WithMany(b => b.Rooms)
               .HasForeignKey(r => r.BuildingId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(r => !r.IsDeleted);
    }
}

// -----------------------------------------------------------------------------
// Sidebar Menu Settings
// -----------------------------------------------------------------------------

/// <summary>EF Core configuration for SidebarMenuItem.</summary>
public class SidebarMenuItemConfiguration : IEntityTypeConfiguration<SidebarMenuItem>
{
    public void Configure(EntityTypeBuilder<SidebarMenuItem> builder)
    {
        builder.ToTable("sidebar_menu_items");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Key)
               .IsRequired()
               .HasMaxLength(100);

        builder.HasIndex(x => x.Key)
               .IsUnique()
               .HasDatabaseName("IX_sidebar_menu_items_key");

        builder.Property(x => x.Name)
               .IsRequired()
               .HasMaxLength(150);

        builder.Property(x => x.Purpose)
               .IsRequired()
               .HasMaxLength(500);

        builder.Property(x => x.DisplayOrder)
               .IsRequired();

        builder.Property(x => x.IsActive)
               .IsRequired()
               .HasDefaultValue(true);

        builder.Property(x => x.IsSystemMenu)
               .IsRequired()
               .HasDefaultValue(false);

        // Self-referential parent / sub-menu relationship
        builder.HasOne(x => x.Parent)
               .WithMany(x => x.SubMenus)
               .HasForeignKey(x => x.ParentId)
               .IsRequired(false)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.RoleAccesses)
               .WithOne(x => x.SidebarMenuItem)
               .HasForeignKey(x => x.SidebarMenuItemId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

/// <summary>EF Core configuration for SidebarMenuRoleAccess.</summary>
public class SidebarMenuRoleAccessConfiguration : IEntityTypeConfiguration<SidebarMenuRoleAccess>
{
    public void Configure(EntityTypeBuilder<SidebarMenuRoleAccess> builder)
    {
        builder.ToTable("sidebar_menu_role_accesses");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.RoleName)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(x => x.IsAllowed)
               .IsRequired()
               .HasDefaultValue(true);

        builder.HasIndex(x => new { x.SidebarMenuItemId, x.RoleName })
               .IsUnique()
               .HasDatabaseName("IX_sidebar_menu_role_accesses_item_role");
    }
}

/// <summary>EF Core configuration for PortalSetting (institution-wide branding key-value store).</summary>
public class PortalSettingConfiguration : IEntityTypeConfiguration<PortalSetting>
{
    public void Configure(EntityTypeBuilder<PortalSetting> builder)
    {
        builder.ToTable("portal_settings");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Key)
               .IsRequired()
               .HasMaxLength(100);

        builder.Property(p => p.Value)
               .IsRequired()
               .HasColumnType("nvarchar(max)");

        builder.HasIndex(p => p.Key)
               .IsUnique()
               .HasDatabaseName("IX_portal_settings_key");
    }
}
