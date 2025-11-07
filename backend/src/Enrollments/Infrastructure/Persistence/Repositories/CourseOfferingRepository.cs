using Enrollments.Domain.CourseAggregate;
using Enrollments.Domain.CourseOfferingAggregate;
using Enrollments.Domain.SemesterAggregate;
using Microsoft.EntityFrameworkCore;

namespace Enrollments.Infrastructure.Persistence.Repositories;

public class CourseOfferingRepository : ICourseOfferingRepository
{
    private readonly CoursesDbContext _context;

    public CourseOfferingRepository(CoursesDbContext context)
    {
        _context = context;
    }

    public async Task<CourseOffering?> GetByIdAsync(OfferingId offeringId, CancellationToken cancellationToken = default)
    {
        return await _context.CourseOfferings
            .FirstOrDefaultAsync(co => co.Id == offeringId, cancellationToken);
    }

    public async Task<List<CourseOffering>> SelectBySemesterAsync(
        SemesterId semesterId,
        OfferingStatus? statusFilter = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.CourseOfferings
            .AsNoTracking()
            .Where(co => co.SemesterId == semesterId);

        if (statusFilter.HasValue)
        {
            query = query.Where(co => co.Status == statusFilter.Value);
        }

        return await query
            .OrderBy(co => co.CourseCode)
            .ToListAsync(cancellationToken);
    }

    public async Task<CourseOffering?> GetByCourseAndSemesterAsync(
        CourseCode courseCode,
        SemesterId semesterId,
        CancellationToken cancellationToken = default)
    {
        return await _context.CourseOfferings
            .FirstOrDefaultAsync(
                co => co.CourseCode == courseCode && co.SemesterId == semesterId,
                cancellationToken);
    }

    public async Task<bool> HasEnrollmentsAsync(OfferingId offeringId, CancellationToken cancellationToken = default)
    {
        // TODO: Enrollmentエンティティが実装されたら実装する
        // 現時点では常にfalseを返す
        return await Task.FromResult(false);
    }

    public void Add(CourseOffering courseOffering)
    {
        _context.CourseOfferings.Add(courseOffering);
    }

    public void Update(CourseOffering courseOffering)
    {
        _context.CourseOfferings.Update(courseOffering);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<OfferingId> GetNextOfferingIdAsync(CancellationToken cancellationToken = default)
    {
        var offerings = await _context.CourseOfferings
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var maxId = offerings.Count > 0
            ? offerings.Max(co => co.Id.Value)
            : 0;

        return new OfferingId(maxId + 1);
    }
}
