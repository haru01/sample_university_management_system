using Enrollments.Domain.StudentAggregate;
using Microsoft.EntityFrameworkCore;

namespace Enrollments.Infrastructure.Persistence.Repositories;

public class StudentRepository : IStudentRepository
{
    private readonly CoursesDbContext _context;

    public StudentRepository(CoursesDbContext context)
    {
        _context = context;
    }

    public async Task<Student?> GetByIdAsync(StudentId id, CancellationToken cancellationToken = default)
    {
        return await _context.Students
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
    }

    public async Task<Student?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Students
            .FirstOrDefaultAsync(s => s.Email == email, cancellationToken);
    }

    public async Task<List<Student>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Students
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Student>> GetFilteredAsync(GetStudentsQuery query, CancellationToken cancellationToken = default)
    {
        var sqlQuery = _context.Students
            .AsNoTracking()
            .AsQueryable();

        // SQLレベルでフィルタリング
        if (query.Grade.HasValue)
        {
            sqlQuery = sqlQuery.Where(s => s.Grade == query.Grade);
        }

        if (!string.IsNullOrWhiteSpace(query.Name))
        {
            sqlQuery = sqlQuery.Where(s => s.Name.Contains(query.Name));
        }

        if (!string.IsNullOrWhiteSpace(query.Email))
        {
            sqlQuery = sqlQuery.Where(s => s.Email.Contains(query.Email));
        }

        // 登録日時の昇順（IDでソート）
        // AsEnumerableで一旦メモリに読み込んでからソート
        var students = await sqlQuery.ToListAsync(cancellationToken);
        return [.. students.OrderBy(s => s.Id.Value)];
    }

    public async Task AddAsync(Student student, CancellationToken cancellationToken = default)
    {
        await _context.Students.AddAsync(student, cancellationToken);
    }

    public async Task DeleteAsync(Student student, CancellationToken cancellationToken = default)
    {
        _context.Students.Remove(student);
        await Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
