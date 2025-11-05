using Enrollments.Application.Queries.SelectCourses;
using Enrollments.Infrastructure.Persistence;
using Enrollments.Infrastructure.Persistence.Repositories;
using Enrollments.Tests.Builders;
using Microsoft.EntityFrameworkCore;

namespace Enrollments.Tests.Application.Queries;

/// <summary>
/// SelectCoursesQueryHandlerのテスト
/// </summary>
public class SelectCoursesQueryHandlerTests : IDisposable
{
    private readonly CoursesDbContext _context;
    private readonly SelectCoursesQueryHandler _handler;

    public SelectCoursesQueryHandlerTests()
    {
        // 各テストごとに新しいDbContextを作成
        var options = new DbContextOptionsBuilder<CoursesDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new CoursesDbContext(options);

        // ハンドラーの依存関係を初期化
        var courseRepository = new CourseRepository(_context);
        _handler = new SelectCoursesQueryHandler(courseRepository);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }

    [Fact]
    public async Task 全てのコースが取得できる()
    {
        // Arrange
        var course1 = new CourseBuilder()
            .WithCode("CS101")
            .WithName("プログラミング入門")
            .Build();

        var course2 = new CourseBuilder()
            .WithCode("MATH201")
            .WithName("線形代数")
            .WithCredits(3)
            .Build();

        var course3 = new CourseBuilder()
            .WithCode("PHYS101")
            .WithName("物理学基礎")
            .WithCredits(4)
            .WithMaxCapacity(100)
            .Build();

        await _context.Courses.AddRangeAsync(course1, course2, course3);
        await _context.SaveChangesAsync();

        // Act
        var query = new SelectCoursesQuery();
        var results = await _handler.Handle(query, default);

        // Assert
        Assert.Equal(3, results.Count);
        Assert.Contains(results, c => c.CourseCode == "CS101");
        Assert.Contains(results, c => c.CourseCode == "MATH201");
        Assert.Contains(results, c => c.CourseCode == "PHYS101");
    }

    [Fact]
    public async Task コースがコードの昇順でソートされて取得できる()
    {
        // Arrange
        var course1 = new CourseBuilder().WithCode("MATH201").Build();
        var course2 = new CourseBuilder().WithCode("CS101").Build();
        var course3 = new CourseBuilder().WithCode("PHYS101").Build();

        // 順不同で登録
        await _context.Courses.AddRangeAsync(course1, course2, course3);
        await _context.SaveChangesAsync();

        // Act
        var query = new SelectCoursesQuery();
        var results = await _handler.Handle(query, default);

        // Assert
        Assert.Equal(3, results.Count);
        Assert.Equal("CS101", results[0].CourseCode);
        Assert.Equal("MATH201", results[1].CourseCode);
        Assert.Equal("PHYS101", results[2].CourseCode);
    }

    [Fact]
    public async Task コースが存在しない場合は空のリストが返される()
    {
        // Arrange
        // データを登録しない

        // Act
        var query = new SelectCoursesQuery();
        var results = await _handler.Handle(query, default);

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public async Task 取得したコースのDTOが正しくマッピングされる()
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
        var query = new SelectCoursesQuery();
        var results = await _handler.Handle(query, default);

        // Assert
        Assert.Single(results);
        var dto = results[0];
        Assert.Equal("CS101", dto.CourseCode);
        Assert.Equal("プログラミング入門", dto.Name);
        Assert.Equal(2, dto.Credits);
        Assert.Equal(30, dto.MaxCapacity);
    }

    [Fact]
    public async Task 複数のコースで各フィールドが正しく取得できる()
    {
        // Arrange
        var course1 = new CourseBuilder()
            .WithCode("CS101")
            .WithName("プログラミング入門")
            .WithCredits(2)
            .WithMaxCapacity(30)
            .Build();

        var course2 = new CourseBuilder()
            .WithCode("MATH201")
            .WithName("線形代数")
            .WithCredits(4)
            .WithMaxCapacity(50)
            .Build();

        await _context.Courses.AddRangeAsync(course1, course2);
        await _context.SaveChangesAsync();

        // Act
        var query = new SelectCoursesQuery();
        var results = await _handler.Handle(query, default);

        // Assert
        Assert.Equal(2, results.Count);

        var cs101 = results.First(c => c.CourseCode == "CS101");
        Assert.Equal("プログラミング入門", cs101.Name);
        Assert.Equal(2, cs101.Credits);
        Assert.Equal(30, cs101.MaxCapacity);

        var math201 = results.First(c => c.CourseCode == "MATH201");
        Assert.Equal("線形代数", math201.Name);
        Assert.Equal(4, math201.Credits);
        Assert.Equal(50, math201.MaxCapacity);
    }
}
