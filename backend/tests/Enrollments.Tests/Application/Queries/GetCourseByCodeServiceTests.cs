using Enrollments.Application.Queries.GetCourseByCode;
using Enrollments.Domain.Exceptions;
using Enrollments.Infrastructure.Persistence;
using Enrollments.Infrastructure.Persistence.Repositories;
using Enrollments.Tests.Builders;
using Microsoft.EntityFrameworkCore;

namespace Enrollments.Tests.Application.Queries;

/// <summary>
/// GetCourseByCodeServiceのテスト
/// </summary>
public class GetCourseByCodeServiceTests : IDisposable
{
    private readonly CoursesDbContext _context;
    private readonly GetCourseByCodeService _service;

    public GetCourseByCodeServiceTests()
    {
        // 各テストごとに新しいDbContextを作成
        var options = new DbContextOptionsBuilder<CoursesDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new CoursesDbContext(options);

        // サービスの依存関係を初期化
        var courseRepository = new CourseRepository(_context);
        _service = new GetCourseByCodeService(courseRepository);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }

    [Fact]
    public async Task 指定したコードのコースが取得できる()
    {
        // Arrange
        var course = new CourseBuilder()
            .WithCode("CS101")
            .WithName("プログラミング入門")
            .WithCredits(2)
            .WithMaxCapacity(30)
            .Build();

        await _context.Courses.AddAsync(course);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetCourseByCodeAsync("CS101");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("CS101", result.CourseCode);
        Assert.Equal("プログラミング入門", result.Name);
        Assert.Equal(2, result.Credits);
        Assert.Equal(30, result.MaxCapacity);
    }

    [Fact]
    public async Task 存在しないコードでnullが返される()
    {
        // Arrange
        // データを登録しない

        // Act
        var result = await _service.GetCourseByCodeAsync("CS999");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task 小文字のコードでも大文字に正規化されて取得できる()
    {
        // Arrange
        var course = new CourseBuilder()
            .WithCode("CS101")
            .Build();

        await _context.Courses.AddAsync(course);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetCourseByCodeAsync("cs101"); // 小文字で検索

        // Assert
        Assert.NotNull(result);
        Assert.Equal("CS101", result.CourseCode);
    }

    [Fact]
    public async Task 不正なコード形式でValidationExceptionがスローされる()
    {
        // Arrange
        // データを登録しない

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            async () => await _service.GetCourseByCodeAsync("INVALID"));
    }

    [Fact]
    public async Task 複数のコースがある中から指定したコースのみが取得できる()
    {
        // Arrange
        var course1 = new CourseBuilder()
            .WithCode("CS101")
            .WithName("プログラミング入門")
            .Build();

        var course2 = new CourseBuilder()
            .WithCode("MATH201")
            .WithName("線形代数")
            .Build();

        var course3 = new CourseBuilder()
            .WithCode("PHYS101")
            .WithName("物理学基礎")
            .Build();

        await _context.Courses.AddRangeAsync(course1, course2, course3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetCourseByCodeAsync("MATH201");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("MATH201", result.CourseCode);
        Assert.Equal("線形代数", result.Name);
    }

    [Fact]
    public async Task 取得したコースのDTOが正しくマッピングされる()
    {
        // Arrange
        var course = new CourseBuilder()
            .WithCode("CS101")
            .WithName("プログラミング入門")
            .WithCredits(3)
            .WithMaxCapacity(50)
            .Build();

        await _context.Courses.AddAsync(course);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.GetCourseByCodeAsync("CS101");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("CS101", result.CourseCode);
        Assert.Equal("プログラミング入門", result.Name);
        Assert.Equal(3, result.Credits);
        Assert.Equal(50, result.MaxCapacity);
    }

    [Fact]
    public async Task 空文字のコードでValidationExceptionがスローされる()
    {
        // Arrange
        // データを登録しない

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            async () => await _service.GetCourseByCodeAsync(""));
    }
}
