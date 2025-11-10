using Enrollments.Domain.CourseOfferingAggregate;
using Enrollments.Domain.EnrollmentAggregate;
using Enrollments.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Shared.ValueObjects;

namespace Enrollments.Infrastructure.Persistence.Repositories;

/// <summary>
/// Enrollment集約のリポジトリ実装
/// </summary>
public class EnrollmentRepository : IEnrollmentRepository
{
    private readonly CoursesDbContext _context;

    public EnrollmentRepository(CoursesDbContext context)
    {
        _context = context;
    }

    public async Task<Enrollment?> GetByIdAsync(
        EnrollmentId enrollmentId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Enrollments
            .FirstOrDefaultAsync(e => e.Id == enrollmentId, cancellationToken);
    }

    public async Task<Enrollment?> GetByStudentAndOfferingAsync(
        StudentId studentId,
        OfferingId offeringId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Enrollments
            .FirstOrDefaultAsync(
                e => e.StudentId == studentId && e.OfferingId == offeringId,
                cancellationToken);
    }

    public async Task<List<Enrollment>> SelectByStudentAsync(
        StudentId studentId,
        EnrollmentStatus? statusFilter = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Enrollments
            .AsNoTracking()
            .Where(e => e.StudentId == studentId);

        if (statusFilter.HasValue)
        {
            query = query.Where(e => e.Status == statusFilter.Value);
        }

        return await query
            .OrderByDescending(e => e.EnrolledAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Enrollment>> SelectByOfferingAsync(
        OfferingId offeringId,
        EnrollmentStatus? statusFilter = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Enrollments
            .AsNoTracking()
            .Where(e => e.OfferingId == offeringId);

        if (statusFilter.HasValue)
        {
            query = query.Where(e => e.Status == statusFilter.Value);
        }

        return await query
            .OrderBy(e => e.EnrolledAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountActiveEnrollmentsByOfferingAsync(
        OfferingId offeringId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Enrollments
            .AsNoTracking()
            .CountAsync(
                e => e.OfferingId == offeringId && e.Status != EnrollmentStatus.Cancelled,
                cancellationToken);
    }

    public async Task<bool> HasActiveEnrollmentAsync(
        StudentId studentId,
        OfferingId offeringId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Enrollments
            .AsNoTracking()
            .AnyAsync(
                e => e.StudentId == studentId
                    && e.OfferingId == offeringId
                    && e.Status != EnrollmentStatus.Cancelled,
                cancellationToken);
    }

    public void Add(Enrollment enrollment)
    {
        _context.Enrollments.Add(enrollment);
    }

    public void Update(Enrollment enrollment)
    {
        _context.Enrollments.Update(enrollment);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is Npgsql.PostgresException pgEx &&
                  pgEx.SqlState == "23505" && // Unique violation
                  pgEx.ConstraintName == "ix_enrollments_student_offering_active")
        {
            throw new ConflictException("学生は既にこのコース開講に履修登録しています");
        }
    }
}
