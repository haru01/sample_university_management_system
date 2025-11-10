using StudentRegistrations.Domain.StudentAggregate;
using StudentRegistrations.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Shared.ValueObjects;

namespace StudentRegistrations.Infrastructure.Persistence;

public class StudentRegistrationsDbContext : DbContext
{
    public DbSet<Student> Students => Set<Student>();

    public StudentRegistrationsDbContext(DbContextOptions<StudentRegistrationsDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("student_registrations");

        // Entity Configuration適用
        modelBuilder.ApplyConfiguration(new StudentConfiguration());
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
