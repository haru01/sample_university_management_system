using StudentRegistrations.Application.Queries.GetStudent;
using StudentRegistrations.Infrastructure.Persistence;
using StudentRegistrations.Infrastructure.Persistence.Repositories;
using StudentRegistrations.Tests.Builders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;

namespace StudentRegistrations.Tests.Application.Queries.GetStudent;

/// <summary>
/// GetStudentQueryHandlerのテスト
/// </summary>
public class GetStudentQueryHandlerTests : IAsyncLifetime
{
    private SqliteConnection _connection;
    private StudentRegistrationsDbContext _context;
    private GetStudentQueryHandler _handler;

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
        _handler = new GetStudentQueryHandler(studentRepository);
    }

    public async Task DisposeAsync()
    {
        if (_context != null)
            await _context.DisposeAsync();
        if (_connection != null)
            await _connection.DisposeAsync();
    }

    [Fact]
    public async Task 存在する学生IDで学生を取得する()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var student = new StudentBuilder()
            .WithId(studentId)
            .WithName("山田太郎")
            .WithEmail("yamada@example.com")
            .WithGrade(2)
            .Build();

        await _context.Students.AddAsync(student);
        await _context.SaveChangesAsync();

        var query = new GetStudentQuery { StudentId = studentId };

        // Act
        var result = await _handler.Handle(query, default);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(studentId, result.StudentId);
        Assert.Equal("山田太郎", result.Name);
        Assert.Equal("yamada@example.com", result.Email);
        Assert.Equal(2, result.Grade);
    }

    [Fact]
    public async Task 存在しない学生IDで取得を試みるとKeyNotFoundExceptionがスローされる()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        var query = new GetStudentQuery { StudentId = nonExistentId };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _handler.Handle(query, default)
        );
        Assert.Contains("not found", exception.Message);
        Assert.Contains(nonExistentId.ToString(), exception.Message);
    }

    [Fact]
    public async Task 複数の学生が存在する中から指定したIDの学生のみを取得する()
    {
        // Arrange
        var targetId = Guid.NewGuid();
        var student1 = new StudentBuilder()
            .WithId(Guid.NewGuid())
            .WithName("山田太郎")
            .Build();

        var student2 = new StudentBuilder()
            .WithId(targetId)
            .WithName("鈴木花子")
            .WithEmail("suzuki@example.com")
            .WithGrade(3)
            .Build();

        var student3 = new StudentBuilder()
            .WithId(Guid.NewGuid())
            .WithName("田中次郎")
            .Build();

        await _context.Students.AddRangeAsync(student1, student2, student3);
        await _context.SaveChangesAsync();

        var query = new GetStudentQuery { StudentId = targetId };

        // Act
        var result = await _handler.Handle(query, default);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(targetId, result.StudentId);
        Assert.Equal("鈴木花子", result.Name);
        Assert.Equal("suzuki@example.com", result.Email);
        Assert.Equal(3, result.Grade);
    }
}
