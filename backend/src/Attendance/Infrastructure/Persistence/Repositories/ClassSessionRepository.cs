using Attendance.Domain.ClassSessionAggregate;
using Microsoft.EntityFrameworkCore;

namespace Attendance.Infrastructure.Persistence.Repositories;

public class ClassSessionRepository : IClassSessionRepository
{
    private readonly AttendanceDbContext _context;

    public ClassSessionRepository(AttendanceDbContext context)
    {
        _context = context;
    }

    public async Task<ClassSession> AddAsync(ClassSession classSession, CancellationToken cancellationToken = default)
    {
        var entry = await _context.ClassSessions.AddAsync(classSession, cancellationToken);
        return entry.Entity;
    }

    public async Task<ClassSession?> GetByIdAsync(SessionId sessionId, CancellationToken cancellationToken = default)
    {
        return await _context.ClassSessions
            .FirstOrDefaultAsync(cs => cs.Id == sessionId, cancellationToken);
    }

    public async Task<ClassSession?> GetByOfferingAndSessionNumberAsync(
        int offeringId,
        int sessionNumber,
        CancellationToken cancellationToken = default)
    {
        return await _context.ClassSessions
            .FirstOrDefaultAsync(
                cs => cs.OfferingId == offeringId && cs.SessionNumber == sessionNumber,
                cancellationToken);
    }

    public async Task<IReadOnlyList<ClassSession>> GetByOfferingIdAsync(
        int offeringId,
        CancellationToken cancellationToken = default)
    {
        return await _context.ClassSessions
            .AsNoTracking()
            .Where(cs => cs.OfferingId == offeringId)
            .OrderBy(cs => cs.SessionDate)
            .ThenBy(cs => cs.StartTime)
            .ToListAsync(cancellationToken);
    }

    public Task UpdateAsync(ClassSession classSession, CancellationToken cancellationToken = default)
    {
        _context.ClassSessions.Update(classSession);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<SessionId> GetNextSessionIdAsync(CancellationToken cancellationToken = default)
    {
        var sessions = await _context.ClassSessions
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var maxId = sessions.Count > 0
            ? sessions.Max(cs => cs.Id.Value)
            : 0;

        return new SessionId(maxId + 1);
    }
}
