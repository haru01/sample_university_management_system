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

    public async Task<Semester?> GetByIdAsync(SemesterId id, CancellationToken cancellationToken = default)
    {
        return await _context.Semesters
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<List<Semester>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Semesters
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Semester semester, CancellationToken cancellationToken = default)
    {
        await _context.Semesters.AddAsync(semester, cancellationToken);
    }

    public async Task DeleteAsync(Semester semester, CancellationToken cancellationToken = default)
    {
        _context.Semesters.Remove(semester);
        await Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
