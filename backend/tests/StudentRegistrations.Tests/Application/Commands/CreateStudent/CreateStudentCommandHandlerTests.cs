using StudentRegistrations.Application.Commands.CreateStudent;
using StudentRegistrations.Domain.Exceptions;
using StudentRegistrations.Infrastructure.Persistence;
using StudentRegistrations.Infrastructure.Persistence.Repositories;
using StudentRegistrations.Tests.Builders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Shared.ValueObjects;

namespace StudentRegistrations.Tests.Application.Commands;

/// <summary>
/// CreateStudentCommandHandlerのテスト
/// </summary>
public class CreateStudentCommandHandlerTests : IAsyncLifetime
{
    private SqliteConnection _connection;
    private StudentRegistrationsDbContext _context;
    private CreateStudentCommandHandler _handler;

    public async Task InitializeAsync()
    {
        // SQLiteのインメモリーデータベースを使用
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();

        var options = new DbContextOptionsBuilder<StudentRegistrationsDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new StudentRegistrationsDbContext(options);
        await _context.Database.EnsureCreatedAsync();

        // ハンドラーの依存関係を初期化
        var studentRepository = new StudentRepository(_context);
        _handler = new CreateStudentCommandHandler(studentRepository);
    }

    public async Task DisposeAsync()
    {
        if (_context != null)
            await _context.DisposeAsync();
        if (_connection != null)
            await _connection.DisposeAsync();
    }

    [Fact]
    public async Task 正常な学生情報で新しい学生を登録する()
    {
        // Arrange
        var command = new CreateStudentCommand
        {
            Name = "山田太郎",
            Email = "yamada@example.com",
            Grade = 1
        };

        // Act
        var studentId = await _handler.Handle(command, default);

        // Assert
        Assert.NotEqual(Guid.Empty, studentId);

        var savedStudent = await _context.Students
            .FindAsync(new StudentId(studentId));
        Assert.NotNull(savedStudent);
        Assert.Equal("山田太郎", savedStudent.Name);
        Assert.Equal("yamada@example.com", savedStudent.Email);
        Assert.Equal(1, savedStudent.Grade);
    }

    [Fact]
    public async Task 重複するメールアドレスで登録を試みるとConflictExceptionがスローされる()
    {
        // Arrange
        var existingStudent = new StudentBuilder()
            .WithEmail("existing@example.com")
            .Build();
        await _context.Students.AddAsync(existingStudent);
        await _context.SaveChangesAsync();

        var command = new CreateStudentCommand
        {
            Name = "田中次郎",
            Email = "existing@example.com", // 既に存在するメール
            Grade = 2
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ConflictException>(
            async () => await _handler.Handle(command, default));
        Assert.Contains("Email already exists", exception.Message);
    }

    [Fact]
    public async Task 不正なメール形式で登録を試みるとValidationExceptionがスローされる()
    {
        // Arrange
        var command = new CreateStudentCommand
        {
            Name = "山田太郎",
            Email = "invalid-email", // 不正なメール形式
            Grade = 1
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            async () => await _handler.Handle(command, default));
        Assert.Contains("Invalid email format", exception.Message);
    }

    [Fact]
    public async Task 学年が範囲外でValidationExceptionがスローされる()
    {
        // Arrange
        var command = new CreateStudentCommand
        {
            Name = "山田太郎",
            Email = "yamada@example.com",
            Grade = 5 // 範囲外（1-4のみ許可）
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            async () => await _handler.Handle(command, default));
        Assert.Contains("Grade must be between 1 and 4", exception.Message);
    }

    [Fact]
    public async Task 学生名が空文字でValidationExceptionがスローされる()
    {
        // Arrange
        var command = new CreateStudentCommand
        {
            Name = "", // 空文字
            Email = "yamada@example.com",
            Grade = 1
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            async () => await _handler.Handle(command, default));
        Assert.Contains("Student name cannot be empty", exception.Message);
    }

    [Fact]
    public async Task 複数の学生を登録できる()
    {
        // Arrange
        var command1 = new CreateStudentCommand
        {
            Name = "山田太郎",
            Email = "yamada@example.com",
            Grade = 1
        };

        var command2 = new CreateStudentCommand
        {
            Name = "田中次郎",
            Email = "tanaka@example.com",
            Grade = 2
        };

        // Act
        var studentId1 = await _handler.Handle(command1, default);
        var studentId2 = await _handler.Handle(command2, default);

        // Assert
        Assert.NotEqual(Guid.Empty, studentId1);
        Assert.NotEqual(Guid.Empty, studentId2);
        Assert.NotEqual(studentId1, studentId2);

        var allStudents = await _context.Students.ToListAsync();
        Assert.Equal(2, allStudents.Count);
    }

    [Theory]
    [InlineData("user@example.com")]
    [InlineData("test.user@example.co.jp")]
    [InlineData("user+tag@example.com")]
    public async Task 有効なメール形式で登録できる(string validEmail)
    {
        // Arrange
        var command = new CreateStudentCommand
        {
            Name = "山田太郎",
            Email = validEmail,
            Grade = 1
        };

        // Act
        var studentId = await _handler.Handle(command, default);

        // Assert
        Assert.NotEqual(Guid.Empty, studentId);

        var savedStudent = await _context.Students
            .FindAsync(new StudentId(studentId));
        Assert.NotNull(savedStudent);
        Assert.Equal(validEmail, savedStudent.Email);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public async Task 有効な学年で登録できる(int validGrade)
    {
        // Arrange
        var command = new CreateStudentCommand
        {
            Name = "山田太郎",
            Email = $"student{validGrade}@example.com",
            Grade = validGrade
        };

        // Act
        var studentId = await _handler.Handle(command, default);

        // Assert
        Assert.NotEqual(Guid.Empty, studentId);

        var savedStudent = await _context.Students
            .FindAsync(new StudentId(studentId));
        Assert.NotNull(savedStudent);
        Assert.Equal(validGrade, savedStudent.Grade);
    }
}
