using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabsan.EduSphere.Domain.Lms;

namespace Tabsan.EduSphere.Infrastructure.Persistence.Configurations;

// Plan M7 — module items, item progress and module prerequisites. Keys are not store-generated.

internal sealed class ModuleItemConfiguration : IEntityTypeConfiguration<ModuleItem>
{
    public void Configure(EntityTypeBuilder<ModuleItem> b)
    {
        b.ToTable("module_items");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.ItemType).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.RequirementType).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Title).IsRequired().HasMaxLength(300);
        b.Property(x => x.Url).HasMaxLength(2000);
        b.Property(x => x.Body).HasMaxLength(50_000);
        b.Property(x => x.MinScorePercent).HasPrecision(5, 2);
        b.HasIndex(x => new { x.ModuleId, x.Position }).HasDatabaseName("IX_module_items_module_position");
        b.HasIndex(x => x.OfferingId).HasDatabaseName("IX_module_items_offering");
        b.HasIndex(x => new { x.ItemType, x.TargetId }).HasDatabaseName("IX_module_items_target");
        b.HasOne<CourseContentModule>().WithMany().HasForeignKey(x => x.ModuleId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ModuleItemProgressConfiguration : IEntityTypeConfiguration<ModuleItemProgress>
{
    public void Configure(EntityTypeBuilder<ModuleItemProgress> b)
    {
        b.ToTable("module_item_progress");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.HasIndex(x => new { x.StudentProfileId, x.ModuleItemId }).IsUnique().HasDatabaseName("IX_module_item_progress_student_item");
        b.HasIndex(x => new { x.OfferingId, x.StudentProfileId }).HasDatabaseName("IX_module_item_progress_offering_student");
        b.HasOne<ModuleItem>().WithMany().HasForeignKey(x => x.ModuleItemId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ModulePrerequisiteConfiguration : IEntityTypeConfiguration<ModulePrerequisite>
{
    public void Configure(EntityTypeBuilder<ModulePrerequisite> b)
    {
        b.ToTable("module_prerequisites");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.HasIndex(x => new { x.ModuleId, x.PrerequisiteModuleId }).IsUnique().HasDatabaseName("IX_module_prerequisites_pair");
        b.HasOne<CourseContentModule>().WithMany().HasForeignKey(x => x.ModuleId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<CourseContentModule>().WithMany().HasForeignKey(x => x.PrerequisiteModuleId).OnDelete(DeleteBehavior.NoAction);
    }
}
