using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Student> Students => Set<Student>();
    public DbSet<AcademicProgram> Programs => Set<AcademicProgram>();
    public DbSet<Professor> Professors => Set<Professor>();
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();
    public DbSet<EnrollmentCourse> EnrollmentCourses => Set<EnrollmentCourse>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<AcademicProgram>(e =>
        {
            e.ToTable("Programs");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(150).IsRequired();
            e.Property(x => x.Code).HasMaxLength(20).IsRequired();
            e.HasIndex(x => x.Code).IsUnique();
        });

        b.Entity<Professor>(e =>
        {
            e.ToTable("Professors");
            e.HasKey(x => x.Id);
            e.Property(x => x.FullName).HasMaxLength(150).IsRequired();
            e.Property(x => x.Email).HasMaxLength(150);
            e.HasIndex(x => x.Email).IsUnique();
            e.Ignore(x => x.Courses);
        });

        b.Entity<Course>(e =>
        {
            e.ToTable("Courses");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(150).IsRequired();
            e.Property(x => x.Code).HasMaxLength(20).IsRequired();
            e.Property(x => x.Credits).IsRequired();
            e.HasIndex(x => x.Code).IsUnique();
            e.HasIndex(x => x.ProfessorId);
            e.HasOne<Professor>().WithMany().HasForeignKey(x => x.ProfessorId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<Student>(e =>
        {
            e.ToTable("Students");
            e.HasKey(x => x.Id);
            e.Property(x => x.FullName).HasMaxLength(150).IsRequired();
            e.Property(x => x.Email).HasMaxLength(150).IsRequired();
            e.Property(x => x.DocumentId).HasMaxLength(50).IsRequired();
            e.Property(x => x.CreatedAt).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();
            e.HasIndex(x => x.DocumentId).IsUnique();
            e.HasOne<AcademicProgram>().WithMany().HasForeignKey(x => x.ProgramId).OnDelete(DeleteBehavior.Restrict);
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        b.Entity<Enrollment>(e =>
        {
            e.ToTable("Enrollments");
            e.HasKey(x => x.Id);
            e.Property(x => x.StudentId).IsRequired();
            e.Property(x => x.Period).HasMaxLength(20).IsRequired();
            e.Property(x => x.CreatedAt).IsRequired();
            e.Ignore(x => x.Items);
            e.HasIndex(x => new { x.StudentId, x.Period }).IsUnique();
            e.HasOne<Student>().WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Cascade);
            e.HasQueryFilter(x => !x.IsDeleted);
        });

        b.Entity<EnrollmentCourse>(e =>
        {
            e.ToTable("EnrollmentCourses");
            e.HasKey(x => new { x.EnrollmentId, x.CourseId });
            e.HasOne<Enrollment>().WithMany().HasForeignKey(x => x.EnrollmentId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne<Course>().WithMany().HasForeignKey(x => x.CourseId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => x.CourseId);
        });
    }
}
