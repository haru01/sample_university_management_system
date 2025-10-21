using Enrollments.Domain.CourseAggregate;
using Enrollments.Domain.StudentAggregate;
using Enrollments.Infrastructure.Persistence.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Enrollments.Infrastructure.Persistence;

public class CoursesDbContext : DbContext
{
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Student> Students => Set<Student>();

    public CoursesDbContext(DbContextOptions<CoursesDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("courses");

        // Entity Configuration適用
        modelBuilder.ApplyConfiguration(new CourseConfiguration());
        modelBuilder.ApplyConfiguration(new StudentConfiguration());
    }
}
