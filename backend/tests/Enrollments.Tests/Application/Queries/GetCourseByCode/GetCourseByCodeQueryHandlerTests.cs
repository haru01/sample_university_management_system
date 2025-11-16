using Enrollments.Application.Queries.GetCourseByCode;
using Enrollments.Domain.Exceptions;
using Enrollments.Infrastructure.Persistence;
using Enrollments.Infrastructure.Persistence.Repositories;
using Enrollments.Tests.Builders;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Enrollments.Tests.Application.Queries;

/// <summary>
/// GetCourseByCodeQueryHandlerのテスト
/// </summary>
public class GetCourseByCodeQueryHandlerTests : IAsyncLifetime
{
    private CoursesDbContext _context;
    private GetCourseByCodeQueryHandler _handler;
    private SqliteConnection _connection;

    public async Task InitializeAsync()
    {
        // 各テストごとに新しいDbContextを作成
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();

        var options = new DbContextOptionsBuilder<CoursesDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new CoursesDbContext(options);
        await _context.Database.EnsureCreatedAsync();

        // ハンドラーの依存関係を初期化
        var courseRepository = new CourseRepository(_context);
        _handler = new GetCourseByCodeQueryHandler(courseRepository);
    }

    public async Task DisposeAsync()
    {
        if (_context != null)
            await _context.DisposeAsync();
        if (_connection != null)
            await _connection.DisposeAsync();
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
        var query = new GetCourseByCodeQuery { CourseCode = "CS101" };
        var result = await _handler.Handle(query, default);

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
        var query = new GetCourseByCodeQuery { CourseCode = "CS999" };
        var result = await _handler.Handle(query, default);

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
        var query = new GetCourseByCodeQuery { CourseCode = "cs101" };
        var result = await _handler.Handle(query, default); // 小文字で検索

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
        var query = new GetCourseByCodeQuery { CourseCode = "INVALID" };
        await Assert.ThrowsAsync<ValidationException>(
            async () => await _handler.Handle(query, default));
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
        var query = new GetCourseByCodeQuery { CourseCode = "MATH201" };
        var result = await _handler.Handle(query, default);

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
        var query = new GetCourseByCodeQuery { CourseCode = "CS101" };
        var result = await _handler.Handle(query, default);

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
        var query = new GetCourseByCodeQuery { CourseCode = "" };
        await Assert.ThrowsAsync<ValidationException>(
            async () => await _handler.Handle(query, default));
    }
}
