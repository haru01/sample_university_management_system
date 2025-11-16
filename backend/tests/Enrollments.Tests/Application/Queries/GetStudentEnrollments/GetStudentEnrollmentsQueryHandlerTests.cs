using Enrollments.Application.Queries.Enrollments;
using Enrollments.Application.Queries.GetStudentEnrollments;
using Enrollments.Application.Services;
using Enrollments.Domain.CourseAggregate;
using Enrollments.Domain.CourseOfferingAggregate;
using Enrollments.Domain.EnrollmentAggregate;
using Enrollments.Domain.Exceptions;
using Enrollments.Domain.SemesterAggregate;
using Enrollments.Infrastructure.Persistence;
using Enrollments.Infrastructure.Persistence.Repositories;
using Enrollments.Tests.Builders;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Shared.ValueObjects;

namespace Enrollments.Tests.Application.Queries.GetStudentEnrollments;

/// <summary>
/// GetStudentEnrollmentsQueryHandlerのテスト
/// </summary>
public class GetStudentEnrollmentsQueryHandlerTests : IAsyncLifetime
{
    private CoursesDbContext _context;
    private GetStudentEnrollmentsQueryHandler _handler;
    private EnrollmentRepository _enrollmentRepository;
    private CourseOfferingRepository _courseOfferingRepository;
    private CourseRepository _courseRepository;
    private Mock<IStudentServiceClient> _mockStudentServiceClient;
    private SqliteConnection _connection;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();

        var options = new DbContextOptionsBuilder<CoursesDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new CoursesDbContext(options);
        await _context.Database.EnsureCreatedAsync();

        _enrollmentRepository = new EnrollmentRepository(_context);
        _courseOfferingRepository = new CourseOfferingRepository(_context);
        _courseRepository = new CourseRepository(_context);
        _mockStudentServiceClient = new Mock<IStudentServiceClient>();

        _handler = new GetStudentEnrollmentsQueryHandler(
            _enrollmentRepository,
            _courseOfferingRepository,
            _courseRepository,
            _mockStudentServiceClient.Object);
    }

    public async Task DisposeAsync()
    {
        if (_context != null)
            await _context.DisposeAsync();
        if (_connection != null)
            await _connection.DisposeAsync();
    }

    [Fact]
    public async Task 学生の履修登録一覧を正常に取得する()
    {
        // Arrange
        var studentId = StudentId.CreateNew();
        var studentName = "山田太郎";

        // 学生が存在し、名前を返すことをモック
        _mockStudentServiceClient
            .Setup(x => x.GetStudentNameAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(studentName);

        // コースを作成
        var course = new CourseBuilder()
            .WithCode("CS101")
            .WithName("プログラミング基礎")
            .Build();
        await _context.Courses.AddAsync(course);
        await _context.SaveChangesAsync();

        // コース開講を作成
        var courseOffering = new CourseOfferingBuilder()
            .WithCourseCode("CS101")
            .WithSemesterId(2024, "Spring")
            .WithCredits(2)
            .WithInstructor("田中教授")
            .Build();
        _courseOfferingRepository.Add(courseOffering);
        await _courseOfferingRepository.SaveChangesAsync();

        // 履修登録を作成
        var enrollment = Enrollment.Create(studentId, courseOffering.Id, "student-001", "履修希望");
        _enrollmentRepository.Add(enrollment);
        await _enrollmentRepository.SaveChangesAsync();

        var query = new GetStudentEnrollmentsQuery
        {
            StudentId = studentId.Value
        };

        // Act
        var result = await _handler.Handle(query, default);

        // Assert
        Assert.Single(result);
        var enrollmentDto = result[0];
        Assert.Equal(enrollment.Id.Value, enrollmentDto.EnrollmentId);
        Assert.Equal(studentId.Value, enrollmentDto.StudentId);
        Assert.Equal(studentName, enrollmentDto.StudentName);
        Assert.Equal("CS101", enrollmentDto.CourseCode);
        Assert.Equal("プログラミング基礎", enrollmentDto.CourseName);
        Assert.Equal(2024, enrollmentDto.Year);
        Assert.Equal("Spring", enrollmentDto.Period);
        Assert.Equal(2, enrollmentDto.Credits);
        Assert.Equal("田中教授", enrollmentDto.Instructor);
        Assert.Equal("Enrolled", enrollmentDto.Status);

        _mockStudentServiceClient.Verify(
            x => x.GetStudentNameAsync(studentId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task 存在しない学生IDで検索すると例外がスローされる()
    {
        // Arrange
        var studentId = StudentId.CreateNew();

        // 学生が存在しないことをモック（nullを返す）
        _mockStudentServiceClient
            .Setup(x => x.GetStudentNameAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((string?)null);

        var query = new GetStudentEnrollmentsQuery
        {
            StudentId = studentId.Value
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _handler.Handle(query, default));
        Assert.Contains("学生ID", exception.Message);
        Assert.Contains("が見つかりません", exception.Message);

        _mockStudentServiceClient.Verify(
            x => x.GetStudentNameAsync(studentId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ステータスフィルターで履修登録をフィルタリングする()
    {
        // Arrange
        var studentId = StudentId.CreateNew();
        var studentName = "山田太郎";

        _mockStudentServiceClient
            .Setup(x => x.GetStudentNameAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(studentName);

        // コースを作成
        var course1 = new CourseBuilder()
            .WithCode("CS101")
            .WithName("プログラミング基礎")
            .Build();
        var course2 = new CourseBuilder()
            .WithCode("CS102")
            .WithName("データ構造")
            .Build();
        await _context.Courses.AddRangeAsync(course1, course2);
        await _context.SaveChangesAsync();

        // コース開講を作成
        var courseOffering1 = new CourseOfferingBuilder()
            .WithOfferingId(1)
            .WithCourseCode("CS101")
            .WithSemesterId(2024, "Spring")
            .Build();
        var courseOffering2 = new CourseOfferingBuilder()
            .WithOfferingId(2)
            .WithCourseCode("CS102")
            .WithSemesterId(2024, "Spring")
            .Build();
        _courseOfferingRepository.Add(courseOffering1);
        _courseOfferingRepository.Add(courseOffering2);
        await _courseOfferingRepository.SaveChangesAsync();

        // 履修登録を作成（Enrolled）
        var enrolledEnrollment = Enrollment.Create(studentId, courseOffering1.Id, "student-001");
        _enrollmentRepository.Add(enrolledEnrollment);
        await _enrollmentRepository.SaveChangesAsync();

        // 履修登録を作成してキャンセル（Cancelled）
        var cancelledEnrollment = Enrollment.Create(studentId, courseOffering2.Id, "student-001");
        cancelledEnrollment.Cancel("student-001", "履修取り消し");
        _enrollmentRepository.Add(cancelledEnrollment);
        await _enrollmentRepository.SaveChangesAsync();

        var query = new GetStudentEnrollmentsQuery
        {
            StudentId = studentId.Value,
            StatusFilter = "Enrolled"
        };

        // Act
        var result = await _handler.Handle(query, default);

        // Assert
        Assert.Single(result);
        Assert.Equal("Enrolled", result[0].Status);
    }

    [Fact]
    public async Task 履修登録がない学生は空のリストを返す()
    {
        // Arrange
        var studentId = StudentId.CreateNew();
        var studentName = "山田太郎";

        _mockStudentServiceClient
            .Setup(x => x.GetStudentNameAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(studentName);

        var query = new GetStudentEnrollmentsQuery
        {
            StudentId = studentId.Value
        };

        // Act
        var result = await _handler.Handle(query, default);

        // Assert
        Assert.Empty(result);

        _mockStudentServiceClient.Verify(
            x => x.GetStudentNameAsync(studentId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task 無効なステータスフィルターを指定すると例外がスローされる()
    {
        // Arrange
        var studentId = StudentId.CreateNew();
        var studentName = "山田太郎";

        _mockStudentServiceClient
            .Setup(x => x.GetStudentNameAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(studentName);

        var query = new GetStudentEnrollmentsQuery
        {
            StudentId = studentId.Value,
            StatusFilter = "InvalidStatus"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _handler.Handle(query, default));
        Assert.Contains("無効なステータスフィルター", exception.Message);
    }
}
