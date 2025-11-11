using Enrollments.Application.Commands.CancelCourseOffering;
using Enrollments.Domain.CourseOfferingAggregate;
using Enrollments.Infrastructure.Persistence;
using Enrollments.Infrastructure.Persistence.Repositories;
using Enrollments.Tests.Builders;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Enrollments.Tests.Application.Commands.CancelCourseOffering;

/// <summary>
/// CancelCourseOfferingCommandHandlerのテスト
/// </summary>
public class CancelCourseOfferingCommandHandlerTests : IAsyncLifetime
{
    private CoursesDbContext _context;
    private CancelCourseOfferingCommandHandler _handler;
    private SqliteConnection _connection;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();

        var options = new DbContextOptionsBuilder<CoursesDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new CoursesDbContext(options);
        await _context.Database.EnsureCreatedAsync();

        var courseOfferingRepository = new CourseOfferingRepository(_context);
        _handler = new CancelCourseOfferingCommandHandler(courseOfferingRepository);
    }

    public async Task DisposeAsync()
    {
        if (_context != null)
            await _context.DisposeAsync();
        if (_connection != null)
            await _connection.DisposeAsync();
    }

    [Fact]
    public async Task Activeなコース開講がキャンセルされる()
    {
        // Arrange
        var offering = new CourseOfferingBuilder()
            .WithOfferingId(1)
            .Build();
        await _context.CourseOfferings.AddAsync(offering);
        await _context.SaveChangesAsync();

        var command = new CancelCourseOfferingCommand
        {
            OfferingId = 1
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        var cancelledOffering = await _context.CourseOfferings
            .FirstOrDefaultAsync(co => co.Id == 1);
        Assert.NotNull(cancelledOffering);
        Assert.Equal(OfferingStatus.Cancelled, cancelledOffering.Status);
    }

    [Fact]
    public async Task 存在しないOfferingIdでKeyNotFoundExceptionがスローされる()
    {
        // Arrange
        var command = new CancelCourseOfferingCommand
        {
            OfferingId = 999 // 存在しないID
        };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            async () => await _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task 既にキャンセル済みのコース開講でInvalidOperationExceptionがスローされる()
    {
        // Arrange
        var offering = new CourseOfferingBuilder()
            .WithOfferingId(1)
            .Build();
        offering.Cancel(); // 既にキャンセル済み
        await _context.CourseOfferings.AddAsync(offering);
        await _context.SaveChangesAsync();

        var command = new CancelCourseOfferingCommand
        {
            OfferingId = 1
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _handler.Handle(command, CancellationToken.None));
    }

    // Note: 履修登録者がいる場合のテストは、Enrollmentエンティティが実装された後に追加する
    // 現時点ではHasEnrollmentsAsyncが常にfalseを返すため、テストは省略
}
