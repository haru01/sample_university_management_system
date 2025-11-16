using Enrollments.Application.Commands.UpdateCourseOffering;
using Enrollments.Domain.CourseOfferingAggregate;
using Enrollments.Domain.Exceptions;
using Enrollments.Infrastructure.Persistence;
using Enrollments.Infrastructure.Persistence.Repositories;
using Enrollments.Tests.Builders;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Enrollments.Tests.Application.Commands.UpdateCourseOffering;

/// <summary>
/// UpdateCourseOfferingCommandHandlerのテスト
/// </summary>
public class UpdateCourseOfferingCommandHandlerTests : IAsyncLifetime
{
    private CoursesDbContext _context;
    private UpdateCourseOfferingCommandHandler _handler;
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
        _handler = new UpdateCourseOfferingCommandHandler(courseOfferingRepository);
    }

    public async Task DisposeAsync()
    {
        if (_context != null)
            await _context.DisposeAsync();
        if (_connection != null)
            await _connection.DisposeAsync();
    }

    [Fact]
    public async Task 正常なコマンドでコース開講が更新される()
    {
        // Arrange
        var offering = new CourseOfferingBuilder()
            .WithOfferingId(1)
            .WithCourseCode("CS101")
            .WithSemesterId(2024, "Spring")
            .WithCredits(3)
            .WithMaxCapacity(30)
            .WithInstructor("田中教授")
            .Build();
        await _context.CourseOfferings.AddAsync(offering);
        await _context.SaveChangesAsync();

        var command = new UpdateCourseOfferingCommand
        {
            OfferingId = 1,
            Credits = 4,
            MaxCapacity = 35,
            Instructor = "鈴木教授"
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        var updatedOffering = await _context.CourseOfferings
            .FirstOrDefaultAsync(co => co.Id == 1);
        Assert.NotNull(updatedOffering);
        Assert.Equal(4, updatedOffering.Credits);
        Assert.Equal(35, updatedOffering.MaxCapacity);
        Assert.Equal("鈴木教授", updatedOffering.Instructor);
    }

    [Fact]
    public async Task 存在しないOfferingIdでKeyNotFoundExceptionがスローされる()
    {
        // Arrange
        var command = new UpdateCourseOfferingCommand
        {
            OfferingId = 999, // 存在しないID
            Credits = 3,
            MaxCapacity = 30
        };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            async () => await _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task キャンセル済みのコース開講でInvalidOperationExceptionがスローされる()
    {
        // Arrange
        var offering = new CourseOfferingBuilder()
            .WithOfferingId(1)
            .Build();
        offering.Cancel(); // キャンセル
        await _context.CourseOfferings.AddAsync(offering);
        await _context.SaveChangesAsync();

        var command = new UpdateCourseOfferingCommand
        {
            OfferingId = 1,
            Credits = 4,
            MaxCapacity = 35
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task 単位数が範囲外でValidationExceptionがスローされる()
    {
        // Arrange
        var offering = new CourseOfferingBuilder()
            .WithOfferingId(1)
            .Build();
        await _context.CourseOfferings.AddAsync(offering);
        await _context.SaveChangesAsync();

        var command = new UpdateCourseOfferingCommand
        {
            OfferingId = 1,
            Credits = 11, // 範囲外（1-10のみ許可）
            MaxCapacity = 30
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            async () => await _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task 定員が0以下でValidationExceptionがスローされる()
    {
        // Arrange
        var offering = new CourseOfferingBuilder()
            .WithOfferingId(1)
            .Build();
        await _context.CourseOfferings.AddAsync(offering);
        await _context.SaveChangesAsync();

        var command = new UpdateCourseOfferingCommand
        {
            OfferingId = 1,
            Credits = 3,
            MaxCapacity = 0 // 不正な値
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            async () => await _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task 教員名をnullに更新できる()
    {
        // Arrange
        var offering = new CourseOfferingBuilder()
            .WithOfferingId(1)
            .WithInstructor("田中教授")
            .Build();
        await _context.CourseOfferings.AddAsync(offering);
        await _context.SaveChangesAsync();

        var command = new UpdateCourseOfferingCommand
        {
            OfferingId = 1,
            Credits = 3,
            MaxCapacity = 30,
            Instructor = null // nullに更新
        };

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        var updatedOffering = await _context.CourseOfferings
            .FirstOrDefaultAsync(co => co.Id == 1);
        Assert.NotNull(updatedOffering);
        Assert.Null(updatedOffering.Instructor);
    }
}
