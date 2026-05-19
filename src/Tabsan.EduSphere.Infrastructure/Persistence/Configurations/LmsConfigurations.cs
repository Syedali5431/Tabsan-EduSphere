using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabsan.EduSphere.Domain.Academic;
using Tabsan.EduSphere.Domain.Identity;
using Tabsan.EduSphere.Domain.Lms;
using Tabsan.EduSphere.Domain.Tenancy;

namespace Tabsan.EduSphere.Infrastructure.Persistence.Configurations;

// Final-Touches Phase 20 Stage 20.1 — EF configuration for CourseContentModule

/// <summary>EF Core configuration for the CourseContentModule entity.</summary>
public class CourseContentModuleConfiguration : IEntityTypeConfiguration<CourseContentModule>
{
    public void Configure(EntityTypeBuilder<CourseContentModule> builder)
    {
        builder.ToTable("course_content_modules");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Title).IsRequired().HasMaxLength(300);
        builder.Property(m => m.Body).HasMaxLength(50_000);
        builder.Property(m => m.RowVersion).IsRowVersion();

        builder.HasOne<Domain.Academic.CourseOffering>()
               .WithMany()
               .HasForeignKey(m => m.OfferingId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(m => m.Videos)
               .WithOne(v => v.Module)
               .HasForeignKey(v => v.ModuleId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(m => !m.IsDeleted);
    }
}

// Final-Touches Phase 20 Stage 20.2 — EF configuration for ContentVideo

/// <summary>EF Core configuration for the ContentVideo entity.</summary>
public class ContentVideoConfiguration : IEntityTypeConfiguration<ContentVideo>
{
    public void Configure(EntityTypeBuilder<ContentVideo> builder)
    {
        builder.ToTable("content_videos");
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Title).IsRequired().HasMaxLength(300);
        builder.Property(v => v.StorageUrl).HasMaxLength(1000);
        builder.Property(v => v.EmbedUrl).HasMaxLength(1000);
        builder.Property(v => v.RowVersion).IsRowVersion();

        builder.HasQueryFilter(v => !v.IsDeleted);
    }
}

// Final-Touches Phase 20 Stage 20.3 — EF configurations for DiscussionThread and DiscussionReply

/// <summary>EF Core configuration for the DiscussionThread entity.</summary>
public class DiscussionThreadConfiguration : IEntityTypeConfiguration<DiscussionThread>
{
    public void Configure(EntityTypeBuilder<DiscussionThread> builder)
    {
        builder.ToTable("discussion_threads");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Title).IsRequired().HasMaxLength(500);
        builder.Property(t => t.RowVersion).IsRowVersion();

        builder.HasOne<Domain.Academic.CourseOffering>()
               .WithMany()
               .HasForeignKey(t => t.OfferingId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.Replies)
               .WithOne(r => r.Thread)
               .HasForeignKey(r => r.ThreadId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(t => !t.IsDeleted);
    }
}

/// <summary>EF Core configuration for the DiscussionReply entity.</summary>
public class DiscussionReplyConfiguration : IEntityTypeConfiguration<DiscussionReply>
{
    public void Configure(EntityTypeBuilder<DiscussionReply> builder)
    {
        builder.ToTable("discussion_replies");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Body).IsRequired().HasMaxLength(10_000);
        builder.Property(r => r.RowVersion).IsRowVersion();

        builder.HasQueryFilter(r => !r.IsDeleted);
    }
}

// Final-Touches Phase 20 Stage 20.4 — EF configuration for CourseAnnouncement

/// <summary>EF Core configuration for the CourseAnnouncement entity.</summary>
public class CourseAnnouncementConfiguration : IEntityTypeConfiguration<CourseAnnouncement>
{
    public void Configure(EntityTypeBuilder<CourseAnnouncement> builder)
    {
        builder.ToTable("course_announcements");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Title).IsRequired().HasMaxLength(300);
        builder.Property(a => a.Body).IsRequired().HasMaxLength(10_000);
        builder.Property(a => a.RowVersion).IsRowVersion();

        // Optional FK — null means a department-wide announcement not tied to a specific offering
        builder.HasOne<Domain.Academic.CourseOffering>()
               .WithMany()
               .HasForeignKey(a => a.OfferingId)
               .IsRequired(false)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasQueryFilter(a => !a.IsDeleted);
    }
}

    /// <summary>EF Core configuration for the CourseMaterial entity.</summary>
    public class CourseMaterialConfiguration : IEntityTypeConfiguration<CourseMaterial>
    {
        public void Configure(EntityTypeBuilder<CourseMaterial> builder)
        {
         builder.ToTable("course_materials");
         builder.ToTable(t => t.HasCheckConstraint(
             "CK_course_materials_scope_required",
             "[TenantId] <> '00000000-0000-0000-0000-000000000000' AND [CampusId] <> '00000000-0000-0000-0000-000000000000' AND [DepartmentId] <> '00000000-0000-0000-0000-000000000000' AND [AcademicProgramId] <> '00000000-0000-0000-0000-000000000000' AND [SemesterId] <> '00000000-0000-0000-0000-000000000000' AND [CourseId] <> '00000000-0000-0000-0000-000000000000' AND [CreatedByUserId] <> '00000000-0000-0000-0000-000000000000'"));
         builder.ToTable(t => t.HasCheckConstraint(
             "CK_course_materials_material_type",
             "[MaterialType] IN (1,2,3)"));
         builder.ToTable(t => t.HasCheckConstraint(
             "CK_course_materials_location_by_type",
             "([MaterialType] = 1 AND [FilePath] IS NOT NULL AND LTRIM(RTRIM([FilePath])) <> '') OR ([MaterialType] = 2 AND [LinkUrl] IS NOT NULL AND LTRIM(RTRIM([LinkUrl])) <> '') OR ([MaterialType] = 3 AND (([FilePath] IS NOT NULL AND LTRIM(RTRIM([FilePath])) <> '') OR ([LinkUrl] IS NOT NULL AND LTRIM(RTRIM([LinkUrl])) <> '')))"));
         builder.HasKey(m => m.Id);

         builder.Property(m => m.Name).IsRequired().HasMaxLength(300);
         builder.Property(m => m.Description).HasMaxLength(4_000);
         builder.Property(m => m.LinkUrl).HasMaxLength(1_000);
         builder.Property(m => m.FilePath).HasMaxLength(1_000);
         builder.Property(m => m.MaterialType).HasConversion<int>().IsRequired();
         builder.Property(m => m.RowVersion).IsRowVersion();

         builder.HasIndex(m => m.TenantId)
             .HasDatabaseName("IX_course_materials_tenant_id");

         builder.HasIndex(m => m.CampusId)
             .HasDatabaseName("IX_course_materials_campus_id");

         builder.HasIndex(m => new { m.TenantId, m.CampusId, m.DepartmentId, m.AcademicProgramId, m.SemesterId, m.CourseId, m.IsActive })
             .HasDatabaseName("IX_course_materials_scope_lookup");

         builder.HasIndex(m => new { m.CourseId, m.SemesterId, m.IsActive })
             .HasDatabaseName("IX_course_materials_course_semester_active");

         builder.HasIndex(m => new { m.TenantId, m.CampusId, m.IsActive, m.Name, m.CreatedAt })
             .HasDatabaseName("IX_course_materials_scope_active_sort");

         builder.HasOne<Tenant>()
             .WithMany()
             .HasForeignKey(m => m.TenantId)
             .OnDelete(DeleteBehavior.Restrict);

         builder.HasOne<Campus>()
             .WithMany()
             .HasForeignKey(m => new { m.CampusId, m.TenantId })
             .HasPrincipalKey(c => new { c.Id, c.TenantId })
             .OnDelete(DeleteBehavior.Restrict);

         builder.HasOne<Department>()
             .WithMany()
             .HasForeignKey(m => m.DepartmentId)
             .OnDelete(DeleteBehavior.Restrict);

         builder.HasOne<AcademicProgram>()
             .WithMany()
             .HasForeignKey(m => m.AcademicProgramId)
             .OnDelete(DeleteBehavior.Restrict);

         builder.HasOne<Semester>()
             .WithMany()
             .HasForeignKey(m => m.SemesterId)
             .OnDelete(DeleteBehavior.Restrict);

         builder.HasOne<Course>()
             .WithMany()
             .HasForeignKey(m => m.CourseId)
             .OnDelete(DeleteBehavior.Restrict);

         builder.HasOne<User>()
             .WithMany()
             .HasForeignKey(m => m.CreatedByUserId)
             .OnDelete(DeleteBehavior.Restrict);

         builder.HasQueryFilter(m => !m.IsDeleted);
        }
    }
