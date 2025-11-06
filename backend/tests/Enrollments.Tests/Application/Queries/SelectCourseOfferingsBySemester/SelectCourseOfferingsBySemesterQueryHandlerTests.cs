using Enrollments.Application.Queries.SelectCourseOfferingsBySemester;
using Enrollments.Infrastructure.Persistence;
using Enrollments.Infrastructure.Persistence.Repositories;
using Enrollments.Tests.Builders;
using Microsoft.EntityFrameworkCore;

namespace Enrollments.Tests.Application.Queries.SelectCourseOfferingsBySemester;

/// <summary>
/// SelectCourseOfferingsBySemesterQueryHandlerのテスト
/// </summary>
public class SelectCourseOfferingsBySemesterQueryHandlerTests : IDisposable
{
    private readonly CoursesDbContext _context;
    private readonly SelectCourseOfferingsBySemesterQueryHandler _handler;

    public SelectCourseOfferingsBySemesterQueryHandlerTests()
    {
        var options = new DbContextOptionsBuilder<CoursesDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new CoursesDbContext(options);

        var courseOfferingRepository = new CourseOfferingRepository(_context);
        var courseRepository = new CourseRepository(_context);
        _handler = new SelectCourseOfferingsBySemesterQueryHandler(
            courseOfferingRepository,
            courseRepository);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }

    [Fact]
    public async Task 特定学期の全コース開講を取得する()
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
        await _context.Courses.AddAsync(course1);
        await _context.Courses.AddAsync(course2);

        var offering1 = new CourseOfferingBuilder()
            .WithOfferingId(1)
            .WithCourseCode("CS101")
            .WithSemesterId(2024, "Spring")
            .WithCredits(3)
            .WithMaxCapacity(30)
            .WithInstructor("田中教授")
            .Build();
        var offering2 = new CourseOfferingBuilder()
            .WithOfferingId(2)
            .WithCourseCode("MATH201")
            .WithSemesterId(2024, "Spring")
            .WithCredits(4)
            .WithMaxCapacity(25)
            .WithInstructor("鈴木教授")
            .Build();
        await _context.CourseOfferings.AddAsync(offering1);
        await _context.CourseOfferings.AddAsync(offering2);
        await _context.SaveChangesAsync();

        var query = new SelectCourseOfferingsBySemesterQuery
        {
            Year = 2024,
            Period = "Spring"
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("CS101", result[0].CourseCode);
        Assert.Equal("プログラミング入門", result[0].CourseName);
        Assert.Equal(3, result[0].Credits);
        Assert.Equal("MATH201", result[1].CourseCode);
        Assert.Equal("線形代数", result[1].CourseName);
    }

    [Fact]
    public async Task Activeステータスのみをフィルタリングして取得する()
    {
        // Arrange
        var course1 = new CourseBuilder()
            .WithCode("CS101")
            .Build();
        var course2 = new CourseBuilder()
            .WithCode("MATH201")
            .Build();
        var course3 = new CourseBuilder()
            .WithCode("ENG101")
            .Build();
        await _context.Courses.AddAsync(course1);
        await _context.Courses.AddAsync(course2);
        await _context.Courses.AddAsync(course3);

        var offering1 = new CourseOfferingBuilder()
            .WithOfferingId(1)
            .WithCourseCode("CS101")
            .WithSemesterId(2024, "Spring")
            .Build();
        var offering2 = new CourseOfferingBuilder()
            .WithOfferingId(2)
            .WithCourseCode("MATH201")
            .WithSemesterId(2024, "Spring")
            .Build();
        offering2.Cancel(); // キャンセル
        var offering3 = new CourseOfferingBuilder()
            .WithOfferingId(3)
            .WithCourseCode("ENG101")
            .WithSemesterId(2024, "Spring")
            .Build();

        await _context.CourseOfferings.AddAsync(offering1);
        await _context.CourseOfferings.AddAsync(offering2);
        await _context.CourseOfferings.AddAsync(offering3);
        await _context.SaveChangesAsync();

        var query = new SelectCourseOfferingsBySemesterQuery
        {
            Year = 2024,
            Period = "Spring",
            StatusFilter = "Active"
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, dto => Assert.Equal("Active", dto.Status));
        Assert.Contains(result, dto => dto.CourseCode == "CS101");
        Assert.Contains(result, dto => dto.CourseCode == "ENG101");
        Assert.DoesNotContain(result, dto => dto.CourseCode == "MATH201");
    }

    [Fact]
    public async Task 開講が1件も登録されていない学期で空のリストが返される()
    {
        // Arrange
        var query = new SelectCourseOfferingsBySemesterQuery
        {
            Year = 2024,
            Period = "Fall"
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task 別の学期の開講は含まれない()
    {
        // Arrange
        var course = new CourseBuilder()
            .WithCode("CS101")
            .Build();
        await _context.Courses.AddAsync(course);

        var offering1 = new CourseOfferingBuilder()
            .WithOfferingId(1)
            .WithCourseCode("CS101")
            .WithSemesterId(2024, "Spring")
            .Build();
        var offering2 = new CourseOfferingBuilder()
            .WithOfferingId(2)
            .WithCourseCode("CS101")
            .WithSemesterId(2024, "Fall")
            .Build();
        await _context.CourseOfferings.AddAsync(offering1);
        await _context.CourseOfferings.AddAsync(offering2);
        await _context.SaveChangesAsync();

        var query = new SelectCourseOfferingsBySemesterQuery
        {
            Year = 2024,
            Period = "Spring"
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Single(result);
        Assert.Equal(1, result[0].OfferingId);
    }
}
