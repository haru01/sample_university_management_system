using Enrollments.Application.Commands.CreateCourseOffering;
using Enrollments.Domain.Exceptions;
using Enrollments.Infrastructure.Persistence;
using Enrollments.Infrastructure.Persistence.Repositories;
using Enrollments.Tests.Builders;
using Microsoft.EntityFrameworkCore;

namespace Enrollments.Tests.Application.Commands.CreateCourseOffering;

/// <summary>
/// CreateCourseOfferingCommandHandlerのテスト
/// </summary>
public class CreateCourseOfferingCommandHandlerTests : IDisposable
{
    private readonly CoursesDbContext _context;
    private readonly CreateCourseOfferingCommandHandler _handler;

    public CreateCourseOfferingCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<CoursesDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new CoursesDbContext(options);

        var courseOfferingRepository = new CourseOfferingRepository(_context);
        var courseRepository = new CourseRepository(_context);
        var semesterRepository = new SemesterRepository(_context);
        _handler = new CreateCourseOfferingCommandHandler(
            courseOfferingRepository,
            courseRepository,
            semesterRepository);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }

    [Fact]
    public async Task 正常なコマンドでコース開講が作成される()
    {
        // Arrange
        var course = new CourseBuilder()
            .WithCode("CS101")
            .Build();
        await _context.Courses.AddAsync(course);

        var semester = new SemesterBuilder()
            .WithYear(2024)
            .WithPeriod("Spring")
            .Build();
        await _context.Semesters.AddAsync(semester);
        await _context.SaveChangesAsync();

        var command = new CreateCourseOfferingCommand
        {
            CourseCode = "CS101",
            Year = 2024,
            Period = "Spring",
            Credits = 3,
            MaxCapacity = 30,
            Instructor = "田中教授"
        };

        // Act
        var offeringId = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(offeringId > 0);

        var savedOffering = await _context.CourseOfferings
            .FirstOrDefaultAsync(co => co.Id == offeringId);
        Assert.NotNull(savedOffering);
        Assert.Equal("CS101", savedOffering.CourseCode.Value);
        Assert.Equal(2024, savedOffering.SemesterId.Year);
        Assert.Equal("Spring", savedOffering.SemesterId.Period);
        Assert.Equal(3, savedOffering.Credits);
        Assert.Equal(30, savedOffering.MaxCapacity);
        Assert.Equal("田中教授", savedOffering.Instructor);
        Assert.Equal(Enrollments.Domain.CourseOfferingAggregate.OfferingStatus.Active, savedOffering.Status);
    }

    [Fact]
    public async Task 存在しないコースコードでKeyNotFoundExceptionがスローされる()
    {
        // Arrange
        var semester = new SemesterBuilder()
            .WithYear(2024)
            .WithPeriod("Spring")
            .Build();
        await _context.Semesters.AddAsync(semester);
        await _context.SaveChangesAsync();

        var command = new CreateCourseOfferingCommand
        {
            CourseCode = "XXX999", // 存在しないコード
            Year = 2024,
            Period = "Spring",
            Credits = 3,
            MaxCapacity = 30
        };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            async () => await _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task 存在しない学期でKeyNotFoundExceptionがスローされる()
    {
        // Arrange
        var course = new CourseBuilder()
            .WithCode("CS101")
            .Build();
        await _context.Courses.AddAsync(course);
        await _context.SaveChangesAsync();

        var command = new CreateCourseOfferingCommand
        {
            CourseCode = "CS101",
            Year = 2099, // 存在しない学期
            Period = "Spring",
            Credits = 3,
            MaxCapacity = 30
        };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            async () => await _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task 同一学期に既に開講されているコースでInvalidOperationExceptionがスローされる()
    {
        // Arrange
        var course = new CourseBuilder()
            .WithCode("CS101")
            .Build();
        await _context.Courses.AddAsync(course);

        var semester = new SemesterBuilder()
            .WithYear(2024)
            .WithPeriod("Spring")
            .Build();
        await _context.Semesters.AddAsync(semester);

        var existingOffering = new CourseOfferingBuilder()
            .WithOfferingId(1)
            .WithCourseCode("CS101")
            .WithSemesterId(2024, "Spring")
            .Build();
        await _context.CourseOfferings.AddAsync(existingOffering);
        await _context.SaveChangesAsync();

        var command = new CreateCourseOfferingCommand
        {
            CourseCode = "CS101",
            Year = 2024,
            Period = "Spring",
            Credits = 4,
            MaxCapacity = 25
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task 単位数が範囲外でValidationExceptionがスローされる()
    {
        // Arrange
        var course = new CourseBuilder()
            .WithCode("CS101")
            .Build();
        await _context.Courses.AddAsync(course);

        var semester = new SemesterBuilder()
            .WithYear(2024)
            .WithPeriod("Spring")
            .Build();
        await _context.Semesters.AddAsync(semester);
        await _context.SaveChangesAsync();

        var command = new CreateCourseOfferingCommand
        {
            CourseCode = "CS101",
            Year = 2024,
            Period = "Spring",
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
        var course = new CourseBuilder()
            .WithCode("CS101")
            .Build();
        await _context.Courses.AddAsync(course);

        var semester = new SemesterBuilder()
            .WithYear(2024)
            .WithPeriod("Spring")
            .Build();
        await _context.Semesters.AddAsync(semester);
        await _context.SaveChangesAsync();

        var command = new CreateCourseOfferingCommand
        {
            CourseCode = "CS101",
            Year = 2024,
            Period = "Spring",
            Credits = 3,
            MaxCapacity = 0 // 不正な値
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(
            async () => await _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task 教員名がnullでも開講が作成される()
    {
        // Arrange
        var course = new CourseBuilder()
            .WithCode("CS101")
            .Build();
        await _context.Courses.AddAsync(course);

        var semester = new SemesterBuilder()
            .WithYear(2024)
            .WithPeriod("Spring")
            .Build();
        await _context.Semesters.AddAsync(semester);
        await _context.SaveChangesAsync();

        var command = new CreateCourseOfferingCommand
        {
            CourseCode = "CS101",
            Year = 2024,
            Period = "Spring",
            Credits = 3,
            MaxCapacity = 30,
            Instructor = null // nullでもOK
        };

        // Act
        var offeringId = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(offeringId > 0);

        var savedOffering = await _context.CourseOfferings
            .FirstOrDefaultAsync(co => co.Id == offeringId);
        Assert.NotNull(savedOffering);
        Assert.Null(savedOffering.Instructor);
    }
}
