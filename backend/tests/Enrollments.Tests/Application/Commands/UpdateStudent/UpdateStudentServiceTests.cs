using Enrollments.Application.Commands.UpdateStudent;
using Enrollments.Domain.Exceptions;
using Enrollments.Infrastructure.Persistence;
using Enrollments.Infrastructure.Persistence.Repositories;
using Enrollments.Tests.Builders;
using Microsoft.EntityFrameworkCore;

namespace Enrollments.Tests.Application.Commands;

/// <summary>
/// UpdateStudentServiceのテスト
/// </summary>
public class UpdateStudentServiceTests : IDisposable
{
    private readonly CoursesDbContext _context;
    private readonly UpdateStudentService _service;

    public UpdateStudentServiceTests()
    {
        // 各テストごとに新しいDbContextを作成
        var options = new DbContextOptionsBuilder<CoursesDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new CoursesDbContext(options);

        // サービスの依存関係を初期化
        var studentRepository = new StudentRepository(_context);
        _service = new UpdateStudentService(studentRepository);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }

    [Fact]
    public async Task 学生情報を更新する()
    {
        // Arrange
        var existingStudent = new StudentBuilder()
            .WithName("山田太郎")
            .WithEmail("old@example.com")
            .WithGrade(1)
            .Build();

        await _context.Students.AddAsync(existingStudent);
        await _context.SaveChangesAsync();

        var command = new UpdateStudentCommand
        {
            StudentId = existingStudent.Id.Value,
            Name = "山田太郎",
            Email = "new@example.com",
            Grade = 2
        };

        // Act
        await _service.UpdateStudentAsync(command);

        // Assert
        var updatedStudent = await _context.Students
            .FirstOrDefaultAsync(s => s.Id.Value == existingStudent.Id.Value);
        Assert.NotNull(updatedStudent);
        Assert.Equal("山田太郎", updatedStudent.Name);
        Assert.Equal("new@example.com", updatedStudent.Email);
        Assert.Equal(2, updatedStudent.Grade);
    }

    [Fact]
    public async Task 存在しない学生IDで更新を試みるとNotFoundExceptionがスローされる()
    {
        // Arrange
        var nonExistentStudentId = Guid.NewGuid();
        var command = new UpdateStudentCommand
        {
            StudentId = nonExistentStudentId,
            Name = "山田太郎",
            Email = "new@example.com",
            Grade = 2
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<NotFoundException>(
            async () => await _service.UpdateStudentAsync(command));
        Assert.Contains("Student not found", exception.Message);
    }

    [Fact]
    public async Task 他の学生が使用しているメールアドレスで更新を試みるとConflictExceptionがスローされる()
    {
        // Arrange
        var student1 = new StudentBuilder()
            .WithEmail("student1@example.com")
            .Build();
        var student2 = new StudentBuilder()
            .WithEmail("student2@example.com")
            .Build();

        await _context.Students.AddAsync(student1);
        await _context.Students.AddAsync(student2);
        await _context.SaveChangesAsync();

        var command = new UpdateStudentCommand
        {
            StudentId = student2.Id.Value,
            Name = "太郎",
            Email = "student1@example.com", // student1が既に使用
            Grade = 2
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ConflictException>(
            async () => await _service.UpdateStudentAsync(command));
        Assert.Contains("Email already exists", exception.Message);
    }

    [Fact]
    public async Task 同じメールアドレスに更新する()
    {
        // Arrange
        var existingStudent = new StudentBuilder()
            .WithName("山田太郎")
            .WithEmail("yamada@example.com")
            .WithGrade(1)
            .Build();

        await _context.Students.AddAsync(existingStudent);
        await _context.SaveChangesAsync();

        var command = new UpdateStudentCommand
        {
            StudentId = existingStudent.Id.Value,
            Name = "田中太郎", // 名前のみ変更
            Email = "yamada@example.com", // 同じメールアドレス
            Grade = 2
        };

        // Act
        await _service.UpdateStudentAsync(command);

        // Assert
        var updatedStudent = await _context.Students
            .FirstOrDefaultAsync(s => s.Id.Value == existingStudent.Id.Value);
        Assert.NotNull(updatedStudent);
        Assert.Equal("田中太郎", updatedStudent.Name);
        Assert.Equal("yamada@example.com", updatedStudent.Email);
        Assert.Equal(2, updatedStudent.Grade);
    }

    [Fact]
    public async Task 不正なメール形式で更新を試みるとValidationExceptionがスローされる()
    {
        // Arrange
        var existingStudent = new StudentBuilder()
            .WithEmail("valid@example.com")
            .Build();

        await _context.Students.AddAsync(existingStudent);
        await _context.SaveChangesAsync();

        var command = new UpdateStudentCommand
        {
            StudentId = existingStudent.Id.Value,
            Name = "山田太郎",
            Email = "invalid-email", // 不正なメール形式
            Grade = 1
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            async () => await _service.UpdateStudentAsync(command));
        Assert.Contains("Invalid email format", exception.Message);
    }

    [Fact]
    public async Task 不正な学年で更新を試みるとValidationExceptionがスローされる()
    {
        // Arrange
        var existingStudent = new StudentBuilder()
            .WithGrade(1)
            .Build();

        await _context.Students.AddAsync(existingStudent);
        await _context.SaveChangesAsync();

        var command = new UpdateStudentCommand
        {
            StudentId = existingStudent.Id.Value,
            Name = "山田太郎",
            Email = "yamada@example.com",
            Grade = 5 // 不正な学年（1-4の範囲外）
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            async () => await _service.UpdateStudentAsync(command));
        Assert.Contains("Grade must be between 1 and 4", exception.Message);
    }

    [Fact]
    public async Task 空の名前で更新を試みるとValidationExceptionがスローされる()
    {
        // Arrange
        var existingStudent = new StudentBuilder()
            .WithName("山田太郎")
            .Build();

        await _context.Students.AddAsync(existingStudent);
        await _context.SaveChangesAsync();

        var command = new UpdateStudentCommand
        {
            StudentId = existingStudent.Id.Value,
            Name = "", // 空の名前
            Email = "yamada@example.com",
            Grade = 1
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            async () => await _service.UpdateStudentAsync(command));
        Assert.Contains("Student name cannot be empty", exception.Message);
    }
}
