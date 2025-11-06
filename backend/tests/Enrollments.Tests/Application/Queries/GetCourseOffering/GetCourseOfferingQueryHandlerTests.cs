using Enrollments.Application.Queries.GetCourseOffering;
using Enrollments.Infrastructure.Persistence;
using Enrollments.Infrastructure.Persistence.Repositories;
using Enrollments.Tests.Builders;
using Microsoft.EntityFrameworkCore;

namespace Enrollments.Tests.Application.Queries.GetCourseOffering;

/// <summary>
/// GetCourseOfferingQueryHandlerのテスト
/// </summary>
public class GetCourseOfferingQueryHandlerTests : IDisposable
{
    private readonly CoursesDbContext _context;
    private readonly GetCourseOfferingQueryHandler _handler;

    public GetCourseOfferingQueryHandlerTests()
    {
        var options = new DbContextOptionsBuilder<CoursesDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new CoursesDbContext(options);

        var courseOfferingRepository = new CourseOfferingRepository(_context);
        var courseRepository = new CourseRepository(_context);
        _handler = new GetCourseOfferingQueryHandler(courseOfferingRepository, courseRepository);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }

    [Fact]
    public async Task 存在するOfferingIdでコース開講詳細を取得する()
    {
        // Arrange
        var course = new CourseBuilder()
            .WithCode("CS101")
            .WithName("プログラミング入門")
            .Build();
        await _context.Courses.AddAsync(course);

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

        var query = new GetCourseOfferingQuery
        {
            OfferingId = 1
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(1, result.OfferingId);
        Assert.Equal("CS101", result.CourseCode);
        Assert.Equal("プログラミング入門", result.CourseName);
        Assert.Equal(2024, result.Year);
        Assert.Equal("Spring", result.Period);
        Assert.Equal(3, result.Credits);
        Assert.Equal(30, result.MaxCapacity);
        Assert.Equal("田中教授", result.Instructor);
        Assert.Equal("Active", result.Status);
    }

    [Fact]
    public async Task 存在しないOfferingIdでKeyNotFoundExceptionがスローされる()
    {
        // Arrange
        var query = new GetCourseOfferingQuery
        {
            OfferingId = 999 // 存在しないID
        };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            async () => await _handler.Handle(query, CancellationToken.None));
    }

    [Fact]
    public async Task キャンセル済みのコース開講も取得できる()
    {
        // Arrange
        var course = new CourseBuilder()
            .WithCode("CS101")
            .WithName("プログラミング入門")
            .Build();
        await _context.Courses.AddAsync(course);

        var offering = new CourseOfferingBuilder()
            .WithOfferingId(1)
            .WithCourseCode("CS101")
            .Build();
        offering.Cancel(); // キャンセル
        await _context.CourseOfferings.AddAsync(offering);
        await _context.SaveChangesAsync();

        var query = new GetCourseOfferingQuery
        {
            OfferingId = 1
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Cancelled", result.Status);
    }

    [Fact]
    public async Task コースマスタが存在しない場合はCourseNameがUnknownになる()
    {
        // Arrange
        var offering = new CourseOfferingBuilder()
            .WithOfferingId(1)
            .WithCourseCode("XXX999") // 存在しないコースコード
            .Build();
        await _context.CourseOfferings.AddAsync(offering);
        await _context.SaveChangesAsync();

        var query = new GetCourseOfferingQuery
        {
            OfferingId = 1
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Unknown", result.CourseName);
    }
}
