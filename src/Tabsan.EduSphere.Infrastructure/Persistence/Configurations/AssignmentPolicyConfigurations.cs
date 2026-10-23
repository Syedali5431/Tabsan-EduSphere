using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabsan.EduSphere.Domain.Assignments;

namespace Tabsan.EduSphere.Infrastructure.Persistence.Configurations;

// Plan M8 — late policy, due-date overrides, student groups and peer reviews. Keys are not store-generated.

internal sealed class LatePolicyConfiguration : IEntityTypeConfiguration<LatePolicy>
{
    public void Configure(EntityTypeBuilder<LatePolicy> b)
    {
        b.ToTable("late_policies");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Interval).HasConversion<string>().HasMaxLength(10);
        b.Property(x => x.DeductionPercent).HasPrecision(5, 2);
        b.Property(x => x.LowestGradePercent).HasPrecision(5, 2);
        b.Property(x => x.MissingGradePercent).HasPrecision(5, 2);
        b.HasIndex(x => x.CourseOfferingId).IsUnique().HasDatabaseName("IX_late_policies_offering");
        b.HasOne<Domain.Academic.CourseOffering>().WithMany().HasForeignKey(x => x.CourseOfferingId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class AssignmentOverrideConfiguration : IEntityTypeConfiguration<AssignmentOverride>
{
    public void Configure(EntityTypeBuilder<AssignmentOverride> b)
    {
        b.ToTable("assignment_overrides");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Note).HasMaxLength(300);
        b.HasIndex(x => x.AssignmentId).HasDatabaseName("IX_assignment_overrides_assignment");
        b.HasOne<Assignment>().WithMany().HasForeignKey(x => x.AssignmentId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class GroupSetConfiguration : IEntityTypeConfiguration<GroupSet>
{
    public void Configure(EntityTypeBuilder<GroupSet> b)
    {
        b.ToTable("group_sets");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Name).IsRequired().HasMaxLength(150);
        b.HasIndex(x => x.CourseOfferingId).HasDatabaseName("IX_group_sets_offering");
        b.HasOne<Domain.Academic.CourseOffering>().WithMany().HasForeignKey(x => x.CourseOfferingId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class StudentGroupConfiguration : IEntityTypeConfiguration<StudentGroup>
{
    public void Configure(EntityTypeBuilder<StudentGroup> b)
    {
        b.ToTable("student_groups");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Name).IsRequired().HasMaxLength(150);
        b.HasIndex(x => x.GroupSetId).HasDatabaseName("IX_student_groups_set");
        b.HasOne<GroupSet>().WithMany().HasForeignKey(x => x.GroupSetId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class StudentGroupMemberConfiguration : IEntityTypeConfiguration<StudentGroupMember>
{
    public void Configure(EntityTypeBuilder<StudentGroupMember> b)
    {
        b.ToTable("student_group_members");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        // A student is in at most one group of a set.
        b.HasIndex(x => new { x.GroupSetId, x.StudentProfileId }).IsUnique().HasDatabaseName("IX_student_group_members_set_student");
        b.HasIndex(x => x.GroupId).HasDatabaseName("IX_student_group_members_group");
        b.HasOne<StudentGroup>().WithMany().HasForeignKey(x => x.GroupId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class PeerReviewConfiguration : IEntityTypeConfiguration<PeerReview>
{
    public void Configure(EntityTypeBuilder<PeerReview> b)
    {
        b.ToTable("peer_reviews");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Score).HasColumnType("decimal(8,2)");
        b.Property(x => x.Comment).HasMaxLength(4000);
        b.Property(x => x.RubricJson).HasMaxLength(8000);
        b.HasIndex(x => new { x.SubmissionId, x.ReviewerStudentProfileId }).IsUnique().HasDatabaseName("IX_peer_reviews_submission_reviewer");
        b.HasIndex(x => new { x.AssignmentId, x.ReviewerStudentProfileId }).HasDatabaseName("IX_peer_reviews_assignment_reviewer");
        b.HasOne<Assignment>().WithMany().HasForeignKey(x => x.AssignmentId).OnDelete(DeleteBehavior.NoAction);
        b.HasOne<AssignmentSubmission>().WithMany().HasForeignKey(x => x.SubmissionId).OnDelete(DeleteBehavior.NoAction);
    }
}
