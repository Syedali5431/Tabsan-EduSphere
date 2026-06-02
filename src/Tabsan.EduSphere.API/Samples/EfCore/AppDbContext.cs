using Microsoft.EntityFrameworkCore;

namespace Tabsan.EduSphere.API.Samples.EfCore;

// Sample model class requested for reference/testing.
public sealed class Student
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

// Sample model class requested for reference/testing.
public sealed class Course
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int CreditHours { get; set; }
}

// Sample DbContext requested by user; main app continues using ApplicationDbContext.
public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Student> Students => Set<Student>();
    public DbSet<Course> Courses => Set<Course>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Student>(entity =>
        {
            entity.ToTable("Students");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FullName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Email).HasMaxLength(320).IsRequired();
        });

        modelBuilder.Entity<Course>(entity =>
        {
            entity.ToTable("Courses");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.CreditHours).IsRequired();
        });
    }
}
