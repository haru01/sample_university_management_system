using Enrollments.Application.Commands.CreateCourse;
using Enrollments.Domain.Exceptions;
using Enrollments.Infrastructure.Persistence;
using Enrollments.Infrastructure.Persistence.Repositories;
using Enrollments.Tests.Builders;
using Microsoft.EntityFrameworkCore;

namespace Enrollments.Tests.Application.Commands;

/// <summary>
/// CreateCourseCommandHandlerのテスト
/// </summary>
public class CreateCourseCommandHandlerTests : IDisposable
{
    private readonly CoursesDbContext _context;
    private readonly CreateCourseCommandHandler _handler;

    public CreateCourseCommandHandlerTests()
    {
        // 各テストごとに新しいDbContextを作成
        var options = new DbContextOptionsBuilder<CoursesDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new CoursesDbContext(options);

        // Handlerの依存関係を初期化
        var courseRepository = new CourseRepository(_context);
        _handler = new CreateCourseCommandHandler(courseRepository);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }

    [Fact]
    public async Task 正常なコマンドでコースが作成される()
    {
        // Arrange
        var command = new CreateCourseCommand
        {
            CourseCode = "CS102",
            Name = "データ構造とアルゴリズム",
            Credits = 3,
            MaxCapacity = 50
        };

        // Act
        var courseId = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(courseId);
        Assert.Equal("CS102", courseId);

        var savedCourse = await _context.Courses
            .FirstOrDefaultAsync(c => c.Id.Value == courseId);
        Assert.NotNull(savedCourse);
        Assert.Equal("データ構造とアルゴリズム", savedCourse.Name);
        Assert.Equal(3, savedCourse.Credits);
        Assert.Equal(50, savedCourse.MaxCapacity);
    }

    [Fact]
    public async Task 既に存在するコースコードでConflictExceptionがスローされる()
    {
        // Arrange
        var existingCourse = new CourseBuilder()
            .WithCode("CS101")
            .Build();
        await _context.Courses.AddAsync(existingCourse);
        await _context.SaveChangesAsync();

        var command = new CreateCourseCommand
        {
            CourseCode = "CS101", // 既に存在するコード
            Name = "新しいコース",
            Credits = 2,
            MaxCapacity = 30
        };

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(
            async () => await _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task 不正な科目コード形式でValidationExceptionがスローされる()
    {
        // Arrange
        var command = new CreateCourseCommand
        {
            CourseCode = "INVALID", // 不正な形式
            Name = "テストコース",
            Credits = 2,
            MaxCapacity = 30
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            async () => await _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task 単位数が範囲外でValidationExceptionがスローされる()
    {
        // Arrange
        var command = new CreateCourseCommand
        {
            CourseCode = "CS103",
            Name = "テストコース",
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
        var command = new CreateCourseCommand
        {
            CourseCode = "CS104",
            Name = "テストコース",
            Credits = 2,
            MaxCapacity = 0 // 不正な値
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            async () => await _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task 科目名が空文字でValidationExceptionがスローされる()
    {
        // Arrange
        var command = new CreateCourseCommand
        {
            CourseCode = "CS105",
            Name = "", // 空文字
            Credits = 2,
            MaxCapacity = 30
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            async () => await _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task 小文字の科目コードが大文字に正規化される()
    {
        // Arrange
        var command = new CreateCourseCommand
        {
            CourseCode = "cs106", // 小文字
            Name = "テストコース",
            Credits = 2,
            MaxCapacity = 30
        };

        // Act
        var courseId = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal("CS106", courseId); // 大文字に正規化される

        var savedCourse = await _context.Courses
            .FirstOrDefaultAsync(c => c.Id.Value == "CS106");
        Assert.NotNull(savedCourse);
    }
}
