using Enrollments.Application.Commands.CreateSemester;
using Enrollments.Domain.Exceptions;
using Enrollments.Infrastructure.Persistence;
using Enrollments.Infrastructure.Persistence.Repositories;
using Enrollments.Tests.Builders;
using Microsoft.EntityFrameworkCore;

namespace Enrollments.Tests.Application.Commands.CreateSemester;

/// <summary>
/// CreateSemesterCommandHandlerのテスト
/// </summary>
public class CreateSemesterCommandHandlerTests : IDisposable
{
    private readonly CoursesDbContext _context;
    private readonly CreateSemesterCommandHandler _handler;

    public CreateSemesterCommandHandlerTests()
    {
        // 各テストごとに新しいDbContextを作成
        var options = new DbContextOptionsBuilder<CoursesDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new CoursesDbContext(options);

        // ハンドラーの依存関係を初期化
        var semesterRepository = new SemesterRepository(_context);
        _handler = new CreateSemesterCommandHandler(semesterRepository);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }

    [Fact]
    public async Task 新しい学期を登録する()
    {
        // Arrange
        var command = new CreateSemesterCommand
        {
            Year = 2024,
            Period = "Spring",
            StartDate = new DateTime(2024, 4, 1),
            EndDate = new DateTime(2024, 9, 30)
        };

        // Act
        var semesterId = await _handler.Handle(command, default);

        // Assert
        Assert.NotNull(semesterId);
        Assert.Equal(2024, semesterId.Year);
        Assert.Equal("Spring", semesterId.Period);

        var savedSemester = await _context.Semesters
            .FirstOrDefaultAsync(s => s.Id.Year == 2024 && s.Id.Period == "Spring");
        Assert.NotNull(savedSemester);
        Assert.Equal(new DateTime(2024, 4, 1), savedSemester.StartDate);
        Assert.Equal(new DateTime(2024, 9, 30), savedSemester.EndDate);
    }

    [Fact]
    public async Task 重複する学期を登録しようとするとConflictExceptionがスローされる()
    {
        // Arrange
        var existingSemester = new SemesterBuilder()
            .WithYear(2024)
            .WithPeriod("Spring")
            .Build();
        await _context.Semesters.AddAsync(existingSemester);
        await _context.SaveChangesAsync();

        var command = new CreateSemesterCommand
        {
            Year = 2024,
            Period = "Spring", // 既に存在する学期
            StartDate = new DateTime(2024, 4, 1),
            EndDate = new DateTime(2024, 9, 30)
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ConflictException>(
            async () => await _handler.Handle(command, default));
        Assert.Contains("Semester already exists", exception.Message);
    }

    [Fact]
    public async Task 不正な学期期間で登録を試みるとArgumentExceptionがスローされる()
    {
        // Arrange
        var command = new CreateSemesterCommand
        {
            Year = 2024,
            Period = "InvalidPeriod", // 不正な期間
            StartDate = new DateTime(2024, 4, 1),
            EndDate = new DateTime(2024, 9, 30)
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            async () => await _handler.Handle(command, default));
        Assert.Contains("Invalid semester period. Must be Spring or Fall", exception.Message);
    }

    [Fact]
    public async Task 終了日が開始日より前の学期を登録しようとするとValidationExceptionがスローされる()
    {
        // Arrange
        var command = new CreateSemesterCommand
        {
            Year = 2024,
            Period = "Spring",
            StartDate = new DateTime(2024, 9, 30),
            EndDate = new DateTime(2024, 4, 1) // 終了日が開始日より前
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            async () => await _handler.Handle(command, default));
        Assert.Contains("End date must be after start date", exception.Message);
    }

    [Fact]
    public async Task 複数の学期を登録できる()
    {
        // Arrange
        var command1 = new CreateSemesterCommand
        {
            Year = 2024,
            Period = "Spring",
            StartDate = new DateTime(2024, 4, 1),
            EndDate = new DateTime(2024, 9, 30)
        };

        var command2 = new CreateSemesterCommand
        {
            Year = 2024,
            Period = "Fall",
            StartDate = new DateTime(2024, 10, 1),
            EndDate = new DateTime(2025, 3, 31)
        };

        // Act
        var semesterId1 = await _handler.Handle(command1, default);
        var semesterId2 = await _handler.Handle(command2, default);

        // Assert
        Assert.NotEqual(semesterId1, semesterId2);

        var allSemesters = await _context.Semesters.ToListAsync();
        Assert.Equal(2, allSemesters.Count);
    }

    [Theory]
    [InlineData("Spring")]
    [InlineData("Fall")]
    public async Task 有効な学期期間で登録できる(string validPeriod)
    {
        // Arrange
        var command = new CreateSemesterCommand
        {
            Year = 2024,
            Period = validPeriod,
            StartDate = new DateTime(2024, 4, 1),
            EndDate = new DateTime(2024, 9, 30)
        };

        // Act
        var semesterId = await _handler.Handle(command, default);

        // Assert
        Assert.Equal(validPeriod, semesterId.Period);

        var savedSemester = await _context.Semesters
            .FirstOrDefaultAsync(s => s.Id.Year == 2024 && s.Id.Period == validPeriod);
        Assert.NotNull(savedSemester);
    }

    [Theory]
    [InlineData(2000)]
    [InlineData(2024)]
    [InlineData(2100)]
    public async Task 有効な年度で登録できる(int validYear)
    {
        // Arrange
        var command = new CreateSemesterCommand
        {
            Year = validYear,
            Period = "Spring",
            StartDate = new DateTime(validYear, 4, 1),
            EndDate = new DateTime(validYear, 9, 30)
        };

        // Act
        var semesterId = await _handler.Handle(command, default);

        // Assert
        Assert.Equal(validYear, semesterId.Year);

        var savedSemester = await _context.Semesters
            .FirstOrDefaultAsync(s => s.Id.Year == validYear && s.Id.Period == "Spring");
        Assert.NotNull(savedSemester);
    }

    [Theory]
    [InlineData(1999)]
    [InlineData(2101)]
    public async Task 無効な年度で登録を試みるとValidationExceptionがスローされる(int invalidYear)
    {
        // Arrange
        var command = new CreateSemesterCommand
        {
            Year = invalidYear,
            Period = "Spring",
            StartDate = new DateTime(2024, 4, 1),
            EndDate = new DateTime(2024, 9, 30)
        };

        // Act & Assert
        var exception = await Assert.ThrowsAnyAsync<Exception>(
            async () => await _handler.Handle(command, default));
        Assert.True(
            exception is ValidationException || exception is ArgumentException,
            $"Expected ValidationException or ArgumentException but got {exception.GetType().Name}");
        Assert.Contains("Year must be between 2000 and 2100", exception.Message);
    }
}
