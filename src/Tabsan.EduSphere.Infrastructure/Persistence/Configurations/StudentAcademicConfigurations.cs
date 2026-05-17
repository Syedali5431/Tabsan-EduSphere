using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tabsan.EduSphere.Domain.Academic;

namespace Tabsan.EduSphere.Infrastructure.Persistence.Configurations;

/// <summary>EF Core configuration for StudentProfile.</summary>
public class StudentProfileConfiguration : IEntityTypeConfiguration<StudentProfile>
{
    public void Configure(EntityTypeBuilder<StudentProfile> builder)
    {
        builder.ToTable("student_profiles");
        builder.HasKey(sp => sp.Id);
        builder.Property(sp => sp.RegistrationNumber).IsRequired().HasMaxLength(50);
        builder.Property(sp => sp.Cgpa).HasColumnType("decimal(4,2)");
       builder.Property(sp => sp.CurrentSemesterGpa).HasColumnType("decimal(4,2)");
        builder.Property(sp => sp.RowVersion).IsRowVersion();

        builder.HasIndex(sp => sp.RegistrationNumber)
               .IsUnique()
               .HasDatabaseName("IX_student_profiles_reg_no");

        builder.HasIndex(sp => sp.UserId)
               .IsUnique()
               .HasDatabaseName("IX_student_profiles_user_id");

        // Stage 1.2 — supports institute/department-scoped student listing and lifecycle dashboards.
        builder.HasIndex(sp => new { sp.DepartmentId, sp.Status })
               .HasDatabaseName("IX_student_profiles_dept_status");

        // Stage 1.2 — supports program roster and progression pipelines.
        builder.HasIndex(sp => new { sp.ProgramId, sp.Status })
               .HasDatabaseName("IX_student_profiles_program_status");

        builder.HasOne(sp => sp.Program)
               .WithMany()
               .HasForeignKey(sp => sp.ProgramId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(sp => sp.Department)
               .WithMany()
               .HasForeignKey(sp => sp.DepartmentId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasQueryFilter(sp => !sp.IsDeleted);
    }
}

/// <summary>EF Core configuration for Enrollment — append-only academic history row.</summary>
public class EnrollmentConfiguration : IEntityTypeConfiguration<Enrollment>
{
    public void Configure(EntityTypeBuilder<Enrollment> builder)
    {
        builder.ToTable("enrollments");
        builder.HasKey(e => e.Id);

        // Store enrollment status as a string for human-readable DB values.
        builder.Property(e => e.Status)
               .HasConversion<string>()
               .HasMaxLength(32);

        // A student can only have one enrollment row per offering (active OR dropped).
        builder.HasIndex(e => new { e.StudentProfileId, e.CourseOfferingId })
               .IsUnique()
               .HasDatabaseName("IX_enrollments_student_offering");

        // Stage 1.2 — common roster and active enrollment checks.
        builder.HasIndex(e => new { e.CourseOfferingId, e.Status })
               .HasDatabaseName("IX_enrollments_offering_status");

        builder.HasIndex(e => new { e.StudentProfileId, e.Status })
               .HasDatabaseName("IX_enrollments_student_status");

        builder.HasOne(e => e.StudentProfile)
               .WithMany()
               .HasForeignKey(e => e.StudentProfileId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.CourseOffering)
               .WithMany()
               .HasForeignKey(e => e.CourseOfferingId)
               .OnDelete(DeleteBehavior.Restrict);

       // Match principal CourseOffering soft-delete filter to avoid required-relationship filter warnings.
       builder.HasQueryFilter(e => !e.CourseOffering.IsDeleted);

        // No query filter here — enrollment history is never filtered out.
    }
}

/// <summary>EF Core configuration for RegistrationWhitelist.</summary>
public class RegistrationWhitelistConfiguration : IEntityTypeConfiguration<RegistrationWhitelist>
{
    public void Configure(EntityTypeBuilder<RegistrationWhitelist> builder)
    {
        builder.ToTable("registration_whitelist");
        builder.HasKey(rw => rw.Id);
        builder.Property(rw => rw.IdentifierValue).IsRequired().HasMaxLength(256);
        builder.Property(rw => rw.IdentifierType).HasConversion<string>();

        // Fast lookup by identifier value during self-registration.
        builder.HasIndex(rw => rw.IdentifierValue)
               .HasDatabaseName("IX_registration_whitelist_identifier");
    }
}

/// <summary>EF Core configuration for FacultyDepartmentAssignment.</summary>
public class FacultyDepartmentAssignmentConfiguration : IEntityTypeConfiguration<FacultyDepartmentAssignment>
{
    public void Configure(EntityTypeBuilder<FacultyDepartmentAssignment> builder)
    {
        builder.ToTable("faculty_department_assignments");
        builder.HasKey(a => a.Id);

        // Prevents assigning the same faculty to the same dept twice (when active).
        // Only one active assignment per faculty per department.
        builder.HasIndex(a => new { a.FacultyUserId, a.DepartmentId })
               .HasDatabaseName("IX_faculty_dept_assignments_faculty_dept");

        // Stage 1.2 — accelerates active assignment lookups used by scope guards.
        builder.HasIndex(a => new { a.FacultyUserId, a.RemovedAt, a.DepartmentId })
               .HasDatabaseName("IX_faculty_dept_assignments_active_lookup");

        builder.HasOne(a => a.Department)
               .WithMany()
               .HasForeignKey(a => a.DepartmentId)
               .OnDelete(DeleteBehavior.Restrict);

       // Match principal Department soft-delete filter to avoid required-relationship filter warnings.
       builder.HasQueryFilter(a => !a.Department.IsDeleted);
    }
}

/// <summary>EF Core configuration for AdminDepartmentAssignment.</summary>
public class AdminDepartmentAssignmentConfiguration : IEntityTypeConfiguration<AdminDepartmentAssignment>
{
       public void Configure(EntityTypeBuilder<AdminDepartmentAssignment> builder)
       {
              builder.ToTable("admin_department_assignments");
              builder.HasKey(a => a.Id);

              builder.HasIndex(a => new { a.AdminUserId, a.DepartmentId })
                        .HasDatabaseName("IX_admin_dept_assignments_admin_dept");

              // Stage 1.2 — accelerates active assignment lookups used by role scope guards.
              builder.HasIndex(a => new { a.AdminUserId, a.RemovedAt, a.DepartmentId })
                        .HasDatabaseName("IX_admin_dept_assignments_active_lookup");

              builder.HasOne(a => a.Department)
                        .WithMany()
                        .HasForeignKey(a => a.DepartmentId)
                        .OnDelete(DeleteBehavior.Restrict);

              // Match principal Department soft-delete filter to avoid required-relationship filter warnings.
              builder.HasQueryFilter(a => !a.Department.IsDeleted);
       }
}
