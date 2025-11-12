using Attendance.Domain.ClassSessionAggregate;
using Attendance.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;
using Shared.ValueObjects;

namespace Attendance.Infrastructure.Persistence;

public class AttendanceDbContext : DbContext
{
    public DbSet<ClassSession> ClassSessions => Set<ClassSession>();

    public AttendanceDbContext(DbContextOptions<AttendanceDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("courses");

        // Entity Configuration適用
        modelBuilder.ApplyConfiguration(new ClassSessionConfiguration());
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
