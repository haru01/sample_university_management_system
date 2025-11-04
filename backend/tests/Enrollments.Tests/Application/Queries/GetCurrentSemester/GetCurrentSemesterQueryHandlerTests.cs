using Enrollments.Application.Queries.GetCurrentSemester;
using Enrollments.Infrastructure.Persistence;
using Enrollments.Infrastructure.Persistence.Repositories;
using Enrollments.Tests.Builders;
using Microsoft.EntityFrameworkCore;

namespace Enrollments.Tests.Application.Queries.GetCurrentSemester;

/// <summary>
/// GetCurrentSemesterQueryHandlerのテスト
/// </summary>
public class GetCurrentSemesterQueryHandlerTests : IDisposable
{
    private readonly CoursesDbContext _context;
    private readonly GetCurrentSemesterQueryHandler _handler;

    public GetCurrentSemesterQueryHandlerTests()
    {
        var options = new DbContextOptionsBuilder<CoursesDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new CoursesDbContext(options);

        var semesterRepository = new SemesterRepository(_context);
        _handler = new GetCurrentSemesterQueryHandler(semesterRepository);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }

    [Fact]
    public async Task 現在日時が範囲内の学期を取得する()
    {
        // Arrange
        var now = DateTime.UtcNow;

        // 過去の学期
        var pastSemester = new SemesterBuilder()
            .WithYear(2023)
            .WithPeriod("Spring")
            .WithStartDate(now.AddMonths(-12))
            .WithEndDate(now.AddMonths(-6))
            .Build();

        // 現在の学期
        var currentSemester = new SemesterBuilder()
            .WithYear(2024)
            .WithPeriod("Spring")
            .WithStartDate(now.AddMonths(-1))
            .WithEndDate(now.AddMonths(5))
            .Build();

        // 未来の学期
        var futureSemester = new SemesterBuilder()
            .WithYear(2025)
            .WithPeriod("Spring")
            .WithStartDate(now.AddMonths(6))
            .WithEndDate(now.AddMonths(12))
            .Build();

        await _context.Semesters.AddRangeAsync(pastSemester, currentSemester, futureSemester);
        await _context.SaveChangesAsync();

        var query = new GetCurrentSemesterQuery();

        // Act
        var result = await _handler.Handle(query, default);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2024, result.Year);
        Assert.Equal("Spring", result.Period);
    }

    [Fact]
    public async Task 現在の学期が存在しない場合nullを返す()
    {
        // Arrange
        var now = DateTime.UtcNow;

        // 全て過去の学期
        var pastSemester = new SemesterBuilder()
            .WithYear(2023)
            .WithPeriod("Spring")
            .WithStartDate(now.AddMonths(-12))
            .WithEndDate(now.AddMonths(-6))
            .Build();

        await _context.Semesters.AddAsync(pastSemester);
        await _context.SaveChangesAsync();

        var query = new GetCurrentSemesterQuery();

        // Act
        var result = await _handler.Handle(query, default);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task 学期が1件も登録されていない場合nullを返す()
    {
        // Arrange
        var query = new GetCurrentSemesterQuery();

        // Act
        var result = await _handler.Handle(query, default);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task 現在日時が開始日と同じ場合も取得する()
    {
        // Arrange
        var now = DateTime.UtcNow;

        var semester = new SemesterBuilder()
            .WithYear(2024)
            .WithPeriod("Spring")
            .WithStartDate(now)
            .WithEndDate(now.AddMonths(6))
            .Build();

        await _context.Semesters.AddAsync(semester);
        await _context.SaveChangesAsync();

        var query = new GetCurrentSemesterQuery();

        // Act
        var result = await _handler.Handle(query, default);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2024, result.Year);
        Assert.Equal("Spring", result.Period);
    }

    [Fact]
    public async Task 現在日時が終了日と同じ場合も取得する()
    {
        // Arrange
        var now = DateTime.UtcNow;

        var semester = new SemesterBuilder()
            .WithYear(2024)
            .WithPeriod("Spring")
            .WithStartDate(now.AddMonths(-6))
            .WithEndDate(now.AddSeconds(1)) // Add a small buffer to account for timing differences
            .Build();

        await _context.Semesters.AddAsync(semester);
        await _context.SaveChangesAsync();

        var query = new GetCurrentSemesterQuery();

        // Act
        var result = await _handler.Handle(query, default);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2024, result.Year);
        Assert.Equal("Spring", result.Period);
    }
}
