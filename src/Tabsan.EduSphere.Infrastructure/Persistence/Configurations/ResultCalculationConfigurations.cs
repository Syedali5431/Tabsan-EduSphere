using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabsan.EduSphere.Domain.Assignments;

namespace Tabsan.EduSphere.Infrastructure.Persistence.Configurations;

public class GpaScaleRuleConfiguration : IEntityTypeConfiguration<GpaScaleRule>
{
    public void Configure(EntityTypeBuilder<GpaScaleRule> builder)
    {
        builder.ToTable("gpa_scale_rules");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.InstitutionType).HasConversion<int>().IsRequired();
        builder.Property(x => x.GradePoint).HasColumnType("decimal(4,2)");
        builder.Property(x => x.MinimumScore).HasColumnType("decimal(5,2)");

        builder.HasIndex(x => new { x.InstitutionType, x.MinimumScore })
            .IsUnique()
            .HasDatabaseName("IX_gpa_scale_rules_institution_minimum_score");
    }
}

public class ResultComponentRuleConfiguration : IEntityTypeConfiguration<ResultComponentRule>
{
    public void Configure(EntityTypeBuilder<ResultComponentRule> builder)
    {
        builder.ToTable("result_component_rules");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.InstitutionType).HasConversion<int>().IsRequired();
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Weightage).HasColumnType("decimal(5,2)");

        builder.HasIndex(x => new { x.InstitutionType, x.Name })
            .IsUnique()
            .HasDatabaseName("IX_result_component_rules_institution_name");
    }
}