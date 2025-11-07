using Enrollments.Application.Commands.CancelEnrollment;
using Enrollments.Domain.EnrollmentAggregate;
using Enrollments.Domain.Exceptions;
using Enrollments.Infrastructure.Persistence;
using Enrollments.Infrastructure.Persistence.Repositories;
using Enrollments.Tests.Builders;
using Microsoft.EntityFrameworkCore;

namespace Enrollments.Tests.Application.Commands.CancelEnrollment;

/// <summary>
/// CancelEnrollmentCommandHandlerのテスト
/// </summary>
public class CancelEnrollmentCommandHandlerTests : IDisposable
{
    private readonly CoursesDbContext _context;
    private readonly CancelEnrollmentCommandHandler _handler;
    private readonly EnrollmentRepository _enrollmentRepository;

    public CancelEnrollmentCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<CoursesDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new CoursesDbContext(options);
        _enrollmentRepository = new EnrollmentRepository(_context);
        _handler = new CancelEnrollmentCommandHandler(_enrollmentRepository);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }

    [Fact]
    public async Task Enrolledステータスの履修登録を正常にキャンセルする()
    {
        // Arrange
        var student = new StudentBuilder().Build();
        var enrollment = Enrollment.Create(student.Id, new(1), "student-001");
        _context.Students.Add(student);
        _enrollmentRepository.Add(enrollment);
        await _enrollmentRepository.SaveChangesAsync();

        var command = new CancelEnrollmentCommand
        {
            EnrollmentId = enrollment.Id.Value,
            CancelledBy = "student-001",
            Reason = "履修計画の変更"
        };

        // Act
        await _handler.Handle(command, default);

        // Assert
        var updated = await _enrollmentRepository.GetByIdAsync(enrollment.Id);
        Assert.NotNull(updated);
        Assert.Equal(EnrollmentStatus.Cancelled, updated.Status);
        Assert.NotNull(updated.CancelledAt);
        Assert.False(updated.IsActive());

        // StatusHistoryが2件（Enrolled, Cancelled）であることを確認
        Assert.Equal(2, updated.StatusHistory.Count);
        var cancelHistory = updated.StatusHistory.Last();
        Assert.Equal(EnrollmentStatus.Cancelled, cancelHistory.Status);
        Assert.Equal("student-001", cancelHistory.ChangedBy);
        Assert.Equal("履修計画の変更", cancelHistory.Reason);
    }

    [Fact]
    public async Task CancelledByなしでキャンセルを試みると例外がスローされる()
    {
        // Arrange
        var student = new StudentBuilder().Build();
        var enrollment = Enrollment.Create(student.Id, new(1), "student-001");
        _context.Students.Add(student);
        _enrollmentRepository.Add(enrollment);
        await _enrollmentRepository.SaveChangesAsync();

        var command = new CancelEnrollmentCommand
        {
            EnrollmentId = enrollment.Id.Value,
            CancelledBy = "",
            Reason = "理由"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _handler.Handle(command, default));
        Assert.Contains("実行者は必須です", exception.Message);
    }

    [Fact]
    public async Task 理由なしでキャンセルを試みると例外がスローされる()
    {
        // Arrange
        var student = new StudentBuilder().Build();
        var enrollment = Enrollment.Create(student.Id, new(1), "student-001");
        _context.Students.Add(student);
        _enrollmentRepository.Add(enrollment);
        await _enrollmentRepository.SaveChangesAsync();

        var command = new CancelEnrollmentCommand
        {
            EnrollmentId = enrollment.Id.Value,
            CancelledBy = "student-001",
            Reason = ""
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _handler.Handle(command, default));
        Assert.Contains("キャンセル理由は必須です", exception.Message);
    }

    [Fact]
    public async Task 存在しない履修登録IDでキャンセルを試みると例外がスローされる()
    {
        // Arrange
        var command = new CancelEnrollmentCommand
        {
            EnrollmentId = Guid.NewGuid(),
            CancelledBy = "student-001",
            Reason = "理由"
        };

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => _handler.Handle(command, default));
    }

    [Fact]
    public async Task 既にキャンセル済みの履修登録を再度キャンセルしようとすると例外がスローされる()
    {
        // Arrange
        var student = new StudentBuilder().Build();
        var enrollment = Enrollment.Create(student.Id, new(1), "student-001");
        enrollment.Cancel("student-001", "最初のキャンセル");
        _context.Students.Add(student);
        _enrollmentRepository.Add(enrollment);
        await _enrollmentRepository.SaveChangesAsync();

        var command = new CancelEnrollmentCommand
        {
            EnrollmentId = enrollment.Id.Value,
            CancelledBy = "student-001",
            Reason = "二度目のキャンセル"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, default));
        Assert.Contains("既にキャンセル済みです", exception.Message);
    }

    [Fact]
    public async Task 完了済みの履修登録をキャンセルしようとすると例外がスローされる()
    {
        // Arrange
        var student = new StudentBuilder().Build();
        var enrollment = Enrollment.Create(student.Id, new(1), "student-001");
        enrollment.Complete("system", "完了");
        _context.Students.Add(student);
        _enrollmentRepository.Add(enrollment);
        await _enrollmentRepository.SaveChangesAsync();

        var command = new CancelEnrollmentCommand
        {
            EnrollmentId = enrollment.Id.Value,
            CancelledBy = "student-001",
            Reason = "キャンセルの試み"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, default));
        Assert.Contains("完了した履修登録はキャンセルできません", exception.Message);
    }
}
