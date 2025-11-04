using Enrollments.Application.Queries.GetSemesters;
using Enrollments.Infrastructure.Persistence;
using Enrollments.Infrastructure.Persistence.Repositories;
using Enrollments.Tests.Builders;
using Microsoft.EntityFrameworkCore;

namespace Enrollments.Tests.Application.Queries.GetSemesters;

/// <summary>
/// GetSemestersQueryHandlerのテスト
/// </summary>
public class GetSemestersQueryHandlerTests : IDisposable
{
    private readonly CoursesDbContext _context;
    private readonly GetSemestersQueryHandler _handler;

    public GetSemestersQueryHandlerTests()
    {
        var options = new DbContextOptionsBuilder<CoursesDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new CoursesDbContext(options);

        var semesterRepository = new SemesterRepository(_context);
        _handler = new GetSemestersQueryHandler(semesterRepository);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }

    [Fact]
    public async Task 全学期を取得する()
    {
        // Arrange
        var semester1 = new SemesterBuilder()
            .WithYear(2024)
            .WithPeriod("Spring")
            .Build();

        var semester2 = new SemesterBuilder()
            .WithYear(2024)
            .WithPeriod("Fall")
            .Build();

        var semester3 = new SemesterBuilder()
            .WithYear(2025)
            .WithPeriod("Spring")
            .Build();

        await _context.Semesters.AddRangeAsync(semester1, semester2, semester3);
        await _context.SaveChangesAsync();

        var query = new GetSemestersQuery();

        // Act
        var result = await _handler.Handle(query, default);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task 学期を年度と期間の降順でソートして返す()
    {
        // Arrange
        var semester1 = new SemesterBuilder()
            .WithYear(2024)
            .WithPeriod("Spring")
            .Build();

        var semester2 = new SemesterBuilder()
            .WithYear(2025)
            .WithPeriod("Fall")
            .Build();

        var semester3 = new SemesterBuilder()
            .WithYear(2025)
            .WithPeriod("Spring")
            .Build();

        await _context.Semesters.AddRangeAsync(semester1, semester2, semester3);
        await _context.SaveChangesAsync();

        var query = new GetSemestersQuery();

        // Act
        var result = await _handler.Handle(query, default);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        // 2025-Fall, 2025-Spring, 2024-Spring の順
        Assert.Equal(2025, result[0].Year);
        Assert.Equal("Fall", result[0].Period);
        Assert.Equal(2025, result[1].Year);
        Assert.Equal("Spring", result[1].Period);
        Assert.Equal(2024, result[2].Year);
        Assert.Equal("Spring", result[2].Period);
    }

    [Fact]
    public async Task 学期が1件も登録されていない場合()
    {
        // Arrange
        var query = new GetSemestersQuery();

        // Act
        var result = await _handler.Handle(query, default);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task 学期DTOに正しく変換される()
    {
        // Arrange
        var startDate = new DateTime(2024, 4, 1);
        var endDate = new DateTime(2024, 9, 30);

        var semester = new SemesterBuilder()
            .WithYear(2024)
            .WithPeriod("Spring")
            .WithStartDate(startDate)
            .WithEndDate(endDate)
            .Build();

        await _context.Semesters.AddAsync(semester);
        await _context.SaveChangesAsync();

        var query = new GetSemestersQuery();

        // Act
        var result = await _handler.Handle(query, default);

        // Assert
        Assert.Single(result);
        Assert.Equal(2024, result[0].Year);
        Assert.Equal("Spring", result[0].Period);
        Assert.Equal(startDate, result[0].StartDate);
        Assert.Equal(endDate, result[0].EndDate);
    }
}
