using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabsan.EduSphere.Domain.Lti;

namespace Tabsan.EduSphere.Infrastructure.Persistence.Configurations;

// Plan M5 — LTI 1.3 platform tables. Keys are not store-generated (new children are inserted through navigations).

internal sealed class LtiToolConfiguration : IEntityTypeConfiguration<LtiTool>
{
    public void Configure(EntityTypeBuilder<LtiTool> b)
    {
        b.ToTable("lti_tools");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Name).IsRequired().HasMaxLength(200);
        b.Property(x => x.Description).HasMaxLength(1000);
        b.Property(x => x.ClientId).IsRequired().HasMaxLength(100);
        b.Property(x => x.OidcInitiationUrl).IsRequired().HasMaxLength(1000);
        b.Property(x => x.TargetLinkUri).IsRequired().HasMaxLength(1000);
        b.Property(x => x.DeepLinkingUrl).HasMaxLength(1000);
        b.Property(x => x.RedirectUris).IsRequired().HasMaxLength(4000);
        b.Property(x => x.JwksUrl).HasMaxLength(1000);
        b.Property(x => x.PublicKeyPem).HasMaxLength(4000);
        b.Property(x => x.CustomParameters).HasMaxLength(4000);
        b.Property(x => x.PrivacyLevel).HasConversion<string>().HasMaxLength(20);
        b.Ignore(x => x.RedirectUriList);
        b.HasIndex(x => x.ClientId).IsUnique().HasDatabaseName("IX_lti_tools_client_id");
        b.HasMany(x => x.Deployments).WithOne().HasForeignKey(d => d.ToolId).OnDelete(DeleteBehavior.Cascade);
        b.Navigation(x => x.Deployments).UsePropertyAccessMode(PropertyAccessMode.Field).HasField("_deployments");
    }
}

internal sealed class LtiToolDeploymentConfiguration : IEntityTypeConfiguration<LtiToolDeployment>
{
    public void Configure(EntityTypeBuilder<LtiToolDeployment> b)
    {
        b.ToTable("lti_tool_deployments");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.DeploymentId).IsRequired().HasMaxLength(100);
        b.Property(x => x.Scope).HasConversion<string>().HasMaxLength(20);
        b.HasIndex(x => x.DeploymentId).IsUnique().HasDatabaseName("IX_lti_tool_deployments_deployment_id");
    }
}

internal sealed class LtiResourceLinkConfiguration : IEntityTypeConfiguration<LtiResourceLink>
{
    public void Configure(EntityTypeBuilder<LtiResourceLink> b)
    {
        b.ToTable("lti_resource_links");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Title).IsRequired().HasMaxLength(200);
        b.Property(x => x.Description).HasMaxLength(1000);
        b.Property(x => x.Url).HasMaxLength(1000);
        b.Property(x => x.CustomParameters).HasMaxLength(4000);
        b.Property(x => x.PresentationTarget).IsRequired().HasMaxLength(10);
        b.HasOne<LtiTool>().WithMany().HasForeignKey(x => x.ToolId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.CourseOfferingId).HasDatabaseName("IX_lti_resource_links_offering");
    }
}

internal sealed class LtiLineItemConfiguration : IEntityTypeConfiguration<LtiLineItem>
{
    public void Configure(EntityTypeBuilder<LtiLineItem> b)
    {
        b.ToTable("lti_line_items");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Label).IsRequired().HasMaxLength(200);
        b.Property(x => x.ScoreMaximum).HasColumnType("decimal(10,2)");
        b.Property(x => x.Tag).HasMaxLength(200);
        b.Property(x => x.ResourceId).HasMaxLength(200);
        b.HasOne<LtiTool>().WithMany().HasForeignKey(x => x.ToolId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.ToolId, x.CourseOfferingId }).HasDatabaseName("IX_lti_line_items_tool_offering");
    }
}

internal sealed class LtiScoreConfiguration : IEntityTypeConfiguration<LtiScore>
{
    public void Configure(EntityTypeBuilder<LtiScore> b)
    {
        b.ToTable("lti_scores");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.ScoreGiven).HasColumnType("decimal(10,2)");
        b.Property(x => x.ScoreMaximum).HasColumnType("decimal(10,2)");
        b.Property(x => x.ActivityProgress).IsRequired().HasMaxLength(20);
        b.Property(x => x.GradingProgress).IsRequired().HasMaxLength(20);
        b.Property(x => x.Comment).HasMaxLength(2000);
        b.HasOne<LtiLineItem>().WithMany().HasForeignKey(x => x.LineItemId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.LineItemId, x.StudentProfileId }).HasDatabaseName("IX_lti_scores_line_item_student");
    }
}

internal sealed class LtiLaunchConfiguration : IEntityTypeConfiguration<LtiLaunch>
{
    public void Configure(EntityTypeBuilder<LtiLaunch> b)
    {
        b.ToTable("lti_launches");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.MessageType).IsRequired().HasMaxLength(50);
        b.Property(x => x.ReturnUrl).HasMaxLength(1000);
        b.HasIndex(x => x.ExpiresAt).HasDatabaseName("IX_lti_launches_expires");
    }
}

internal sealed class LtiUsedNonceConfiguration : IEntityTypeConfiguration<LtiUsedNonce>
{
    public void Configure(EntityTypeBuilder<LtiUsedNonce> b)
    {
        b.ToTable("lti_used_nonces");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Value).IsRequired().HasMaxLength(200);
        b.HasIndex(x => new { x.ToolId, x.Value }).IsUnique().HasDatabaseName("IX_lti_used_nonces_tool_value");
        b.HasIndex(x => x.ExpiresAt).HasDatabaseName("IX_lti_used_nonces_expires");
    }
}

internal sealed class LtiAccessTokenConfiguration : IEntityTypeConfiguration<LtiAccessToken>
{
    public void Configure(EntityTypeBuilder<LtiAccessToken> b)
    {
        b.ToTable("lti_access_tokens");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.TokenHash).IsRequired().HasMaxLength(64);
        b.Property(x => x.Scopes).IsRequired().HasMaxLength(1000);
        b.HasIndex(x => x.TokenHash).IsUnique().HasDatabaseName("IX_lti_access_tokens_hash");
    }
}
