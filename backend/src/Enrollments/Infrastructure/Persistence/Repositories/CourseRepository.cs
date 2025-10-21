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

    public async Task<Course?> GetByCodeAsync(CourseCode code)
    {
        return await _context.Courses
            .FirstOrDefaultAsync(c => c.Id == code);
    }

    public async Task<List<Course>> GetAllAsync()
    {
        return await _context.Courses
            .AsNoTracking()          // 読み取り専用クエリ - 変更追跡不要
            .OrderBy(c => c.Id)
            .ToListAsync();
    }

    public async Task AddAsync(Course course)
    {
        await _context.Courses.AddAsync(course);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
