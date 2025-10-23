using Enrollments.Domain.SemesterAggregate;
using Microsoft.EntityFrameworkCore;

namespace Enrollments.Infrastructure.Persistence.Repositories;

public class SemesterRepository : ISemesterRepository
{
    private readonly CoursesDbContext _context;

    public SemesterRepository(CoursesDbContext context)
    {
        _context = context;
    }

    public async Task<Semester?> GetByIdAsync(SemesterId id)
    {
        return await _context.Semesters
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<List<Semester>> GetAllAsync()
    {
        return await _context.Semesters
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task AddAsync(Semester semester)
    {
        await _context.Semesters.AddAsync(semester);
    }

    public async Task DeleteAsync(Semester semester)
    {
        _context.Semesters.Remove(semester);
        await Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
