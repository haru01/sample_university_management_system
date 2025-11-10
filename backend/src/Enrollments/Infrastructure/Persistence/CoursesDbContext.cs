using Enrollments.Domain.CourseAggregate;
using Enrollments.Domain.CourseOfferingAggregate;
using Enrollments.Domain.EnrollmentAggregate;
using Enrollments.Domain.SemesterAggregate;
using Enrollments.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Shared.ValueObjects;

namespace Enrollments.Infrastructure.Persistence;

public class CoursesDbContext : DbContext
{
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Semester> Semesters => Set<Semester>();
    public DbSet<CourseOffering> CourseOfferings => Set<CourseOffering>();
    public DbSet<Enrollment> Enrollments => Set<Enrollment>();

    public CoursesDbContext(DbContextOptions<CoursesDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("courses");

        // Entity Configuration適用
        modelBuilder.ApplyConfiguration(new CourseConfiguration());
        modelBuilder.ApplyConfiguration(new SemesterConfiguration());
        modelBuilder.ApplyConfiguration(new CourseOfferingConfiguration());
        modelBuilder.ApplyConfiguration(new EnrollmentConfiguration());
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // PostgreSQL: すべてのDateTimeプロパティをUTCとして扱う
        configurationBuilder.Properties<DateTime>()
            .HaveConversion<DateTimeUtcConverter>();
    }

    private class DateTimeUtcConverter : Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<DateTime, DateTime>
    {
        public DateTimeUtcConverter()
            : base(
                v => v.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(v, DateTimeKind.Utc) : v.ToUniversalTime(),
                v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
        {
        }
    }
}
