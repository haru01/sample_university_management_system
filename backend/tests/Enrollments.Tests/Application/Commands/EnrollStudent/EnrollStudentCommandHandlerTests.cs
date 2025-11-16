using Enrollments.Application.Commands.EnrollStudent;
using Enrollments.Application.Services;
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

namespace Enrollments.Tests.Application.Commands.EnrollStudent;

/// <summary>
/// EnrollStudentCommandHandlerのテスト
/// </summary>
public class EnrollStudentCommandHandlerTests : IAsyncLifetime
{
    private CoursesDbContext _context;
    private EnrollStudentCommandHandler _handler;
    private EnrollmentRepository _enrollmentRepository;
    private CourseOfferingRepository _courseOfferingRepository;
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
        _mockStudentServiceClient = new Mock<IStudentServiceClient>();

        _handler = new EnrollStudentCommandHandler(
            _enrollmentRepository,
            _courseOfferingRepository,
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
    public async Task 有効な学生とコース開講で履修登録を正常に作成する()
    {
        // Arrange
        var studentId = StudentId.CreateNew();
        var courseOffering = new CourseOfferingBuilder()
            .WithSemesterId(2024, "Spring")
            .WithMaxCapacity(30)
            .Build();

        _courseOfferingRepository.Add(courseOffering);
        await _courseOfferingRepository.SaveChangesAsync();

        // 学生が存在することをモック
        _mockStudentServiceClient
            .Setup(x => x.ExistsAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new EnrollStudentCommand
        {
            StudentId = studentId.Value,
            OfferingId = courseOffering.Id.Value,
            EnrolledBy = "student-001",
            InitialNote = "履修希望"
        };

        // Act
        var enrollmentId = await _handler.Handle(command, default);

        // Assert
        Assert.NotEqual(Guid.Empty, enrollmentId);

        var enrollment = await _enrollmentRepository.GetByIdAsync(new EnrollmentId(enrollmentId));
        Assert.NotNull(enrollment);
        Assert.Equal(studentId, enrollment.StudentId);
        Assert.Equal(courseOffering.Id, enrollment.OfferingId);
        Assert.Equal(EnrollmentStatus.Enrolled, enrollment.Status);

        // StudentServiceClientが呼ばれたことを検証
        _mockStudentServiceClient.Verify(
            x => x.ExistsAsync(studentId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task 存在しない学生IDで履修登録を試みると例外がスローされる()
    {
        // Arrange
        var studentId = StudentId.CreateNew();
        var courseOffering = new CourseOfferingBuilder()
            .WithSemesterId(2024, "Spring")
            .Build();

        _courseOfferingRepository.Add(courseOffering);
        await _courseOfferingRepository.SaveChangesAsync();

        // 学生が存在しないことをモック
        _mockStudentServiceClient
            .Setup(x => x.ExistsAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var command = new EnrollStudentCommand
        {
            StudentId = studentId.Value,
            OfferingId = courseOffering.Id.Value,
            EnrolledBy = "student-001"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _handler.Handle(command, default));
        Assert.Contains("学生ID", exception.Message);
        Assert.Contains("が見つかりません", exception.Message);

        _mockStudentServiceClient.Verify(
            x => x.ExistsAsync(studentId, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task 存在しないコース開講IDで履修登録を試みると例外がスローされる()
    {
        // Arrange
        var studentId = StudentId.CreateNew();
        var offeringId = new OfferingId(999);

        // 学生が存在することをモック
        _mockStudentServiceClient
            .Setup(x => x.ExistsAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new EnrollStudentCommand
        {
            StudentId = studentId.Value,
            OfferingId = offeringId.Value,
            EnrolledBy = "student-001"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            () => _handler.Handle(command, default));
        Assert.Contains("コース開講ID", exception.Message);
        Assert.Contains("が見つかりません", exception.Message);
    }

    [Fact]
    public async Task キャンセル済みのコース開講に履修登録を試みると例外がスローされる()
    {
        // Arrange
        var studentId = StudentId.CreateNew();
        var courseOffering = new CourseOfferingBuilder()
            .WithSemesterId(2024, "Spring")
            .Build();

        courseOffering.Cancel();
        _courseOfferingRepository.Add(courseOffering);
        await _courseOfferingRepository.SaveChangesAsync();

        // 学生が存在することをモック
        _mockStudentServiceClient
            .Setup(x => x.ExistsAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new EnrollStudentCommand
        {
            StudentId = studentId.Value,
            OfferingId = courseOffering.Id.Value,
            EnrolledBy = "student-001"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _handler.Handle(command, default));
        Assert.Contains("キャンセル済み", exception.Message);
    }

    [Fact]
    public async Task 既に履修登録済みのコース開講に再度履修登録を試みると例外がスローされる()
    {
        // Arrange
        var studentId = StudentId.CreateNew();
        var courseOffering = new CourseOfferingBuilder()
            .WithSemesterId(2024, "Spring")
            .Build();

        _courseOfferingRepository.Add(courseOffering);
        await _courseOfferingRepository.SaveChangesAsync();

        // 既存の履修登録を作成
        var existingEnrollment = Enrollment.Create(studentId, courseOffering.Id, "student-001");
        _enrollmentRepository.Add(existingEnrollment);
        await _enrollmentRepository.SaveChangesAsync();

        // 学生が存在することをモック
        _mockStudentServiceClient
            .Setup(x => x.ExistsAsync(studentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new EnrollStudentCommand
        {
            StudentId = studentId.Value,
            OfferingId = courseOffering.Id.Value,
            EnrolledBy = "student-001"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => _handler.Handle(command, default));
        Assert.Contains("既にこのコース開講に履修登録しています", exception.Message);
    }

    [Fact]
    public async Task 定員に達したコース開講に履修登録を試みると例外がスローされる()
    {
        // Arrange
        var courseOffering = new CourseOfferingBuilder()
            .WithSemesterId(2024, "Spring")
            .WithMaxCapacity(1) // 定員1名
            .Build();

        _courseOfferingRepository.Add(courseOffering);
        await _courseOfferingRepository.SaveChangesAsync();

        // 定員を埋める
        var existingStudentId = StudentId.CreateNew();
        var existingEnrollment = Enrollment.Create(existingStudentId, courseOffering.Id, "student-001");
        _enrollmentRepository.Add(existingEnrollment);
        await _enrollmentRepository.SaveChangesAsync();

        // 新しい学生が登録を試みる
        var newStudentId = StudentId.CreateNew();

        // 学生が存在することをモック
        _mockStudentServiceClient
            .Setup(x => x.ExistsAsync(newStudentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new EnrollStudentCommand
        {
            StudentId = newStudentId.Value,
            OfferingId = courseOffering.Id.Value,
            EnrolledBy = "student-002"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ConflictException>(
            () => _handler.Handle(command, default));
        Assert.Contains("定員に達しています", exception.Message);
    }
}
