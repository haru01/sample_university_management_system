using Enrollments.Application.Commands.CompleteEnrollment;
using Enrollments.Domain.EnrollmentAggregate;
using Enrollments.Domain.Exceptions;
using Enrollments.Infrastructure.Persistence;
using Enrollments.Infrastructure.Persistence.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Shared.ValueObjects;

namespace Enrollments.Tests.Application.Commands.CompleteEnrollment;

/// <summary>
/// CompleteEnrollmentCommandHandlerのテスト
/// </summary>
public class CompleteEnrollmentCommandHandlerTests : IAsyncLifetime
{
    private CoursesDbContext _context;
    private CompleteEnrollmentCommandHandler _handler;
    private EnrollmentRepository _enrollmentRepository;
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
        _handler = new CompleteEnrollmentCommandHandler(_enrollmentRepository);
    }

    public async Task DisposeAsync()
    {
        if (_context != null)
            await _context.DisposeAsync();
        if (_connection != null)
            await _connection.DisposeAsync();
    }

    [Fact]
    public async Task Enrolledステータスの履修登録を正常に完了する()
    {
        // Arrange
        var studentId = new StudentId(Guid.NewGuid());
        var enrollment = Enrollment.Create(studentId, new(1), "student-001");
        _enrollmentRepository.Add(enrollment);
        await _enrollmentRepository.SaveChangesAsync();

        var command = new CompleteEnrollmentCommand
        {
            EnrollmentId = enrollment.Id.Value,
            CompletedBy = "system",
            Reason = "学期終了"
        };

        // Act
        await _handler.Handle(command, default);

        // Assert
        var updated = await _enrollmentRepository.GetByIdAsync(enrollment.Id);
        Assert.NotNull(updated);
        Assert.Equal(EnrollmentStatus.Completed, updated.Status);
        Assert.NotNull(updated.CompletedAt);
        Assert.True(updated.IsCompleted());

        // StatusHistoryが2件（Enrolled, Completed）であることを確認
        Assert.Equal(2, updated.StatusHistory.Count);
        var completeHistory = updated.StatusHistory.Last();
        Assert.Equal(EnrollmentStatus.Completed, completeHistory.Status);
        Assert.Equal("system", completeHistory.ChangedBy);
        Assert.Equal("学期終了", completeHistory.Reason);
    }

    [Fact]
    public async Task CompletedByなしで完了を試みると例外がスローされる()
    {
        // Arrange
        var studentId = new StudentId(Guid.NewGuid());
        var enrollment = Enrollment.Create(studentId, new(1), "student-001");
        _enrollmentRepository.Add(enrollment);
        await _enrollmentRepository.SaveChangesAsync();

        var command = new CompleteEnrollmentCommand
        {
            EnrollmentId = enrollment.Id.Value,
            CompletedBy = "",
            Reason = "学期終了"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _handler.Handle(command, default));
        Assert.Contains("実行者は必須です", exception.Message);
    }

    [Fact]
    public async Task 存在しない履修登録IDで完了を試みると例外がスローされる()
    {
        // Arrange
        var command = new CompleteEnrollmentCommand
        {
            EnrollmentId = Guid.NewGuid(),
            CompletedBy = "system",
            Reason = "学期終了"
        };

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => _handler.Handle(command, default));
    }

    [Fact]
    public async Task 既に完了している履修登録を再度完了しようとすると例外がスローされる()
    {
        // Arrange
        var studentId = new StudentId(Guid.NewGuid());
        var enrollment = Enrollment.Create(studentId, new(1), "student-001");
        enrollment.Complete("system", "最初の完了");
        _enrollmentRepository.Add(enrollment);
        await _enrollmentRepository.SaveChangesAsync();

        var command = new CompleteEnrollmentCommand
        {
            EnrollmentId = enrollment.Id.Value,
            CompletedBy = "system",
            Reason = "二度目の完了"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, default));
        Assert.Contains("既に完了しています", exception.Message);
    }

    [Fact]
    public async Task キャンセル済みの履修登録を完了しようとすると例外がスローされる()
    {
        // Arrange
        var studentId = new StudentId(Guid.NewGuid());
        var enrollment = Enrollment.Create(studentId, new(1), "student-001");
        enrollment.Cancel("student-001", "履修計画の変更");
        _enrollmentRepository.Add(enrollment);
        await _enrollmentRepository.SaveChangesAsync();

        var command = new CompleteEnrollmentCommand
        {
            EnrollmentId = enrollment.Id.Value,
            CompletedBy = "system",
            Reason = "完了の試み"
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _handler.Handle(command, default));
        Assert.Contains("キャンセル済みの履修登録は完了できません", exception.Message);
    }
}
