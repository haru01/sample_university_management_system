using Enrollments.Domain.CourseAggregate;
using Microsoft.EntityFrameworkCore;

namespace Enrollments.Infrastructure.Persistence.Repositories;

public class CourseRepository : ICourseRepository
{
    private readonly CoursesDbContext _context;

    public CourseRepository(CoursesDbContext context)
    {
        _context = context;
    }

    public async Task<Course?> GetByCodeAsync(CourseCode code, CancellationToken cancellationToken = default)
    {
        return await _context.Courses
            .FirstOrDefaultAsync(c => c.Id == code, cancellationToken);
    }

    public async Task<List<Course>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Courses
            .AsNoTracking()          // 読み取り専用クエリ - 変更追跡不要
            .OrderBy(c => c.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Course course, CancellationToken cancellationToken = default)
    {
        await _context.Courses.AddAsync(course, cancellationToken);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
