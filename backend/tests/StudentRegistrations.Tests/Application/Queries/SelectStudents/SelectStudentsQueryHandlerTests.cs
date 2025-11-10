using StudentRegistrations.Application.Queries.SelectStudents;
using StudentRegistrations.Infrastructure.Persistence;
using StudentRegistrations.Infrastructure.Persistence.Repositories;
using StudentRegistrations.Tests.Builders;
using Microsoft.EntityFrameworkCore;

namespace StudentRegistrations.Tests.Application.Queries;

/// <summary>
/// SelectStudentsQueryHandlerのテスト
/// </summary>
public class SelectStudentsQueryHandlerTests : IDisposable
{
    private readonly StudentRegistrationsDbContext _context;
    private readonly SelectStudentsQueryHandler _handler;

    public SelectStudentsQueryHandlerTests()
    {
        // 各テストごとに新しいDbContextを作成
        var options = new DbContextOptionsBuilder<StudentRegistrationsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new StudentRegistrationsDbContext(options);

        // ハンドラーの依存関係を初期化
        var studentRepository = new StudentRepository(_context);
        _handler = new SelectStudentsQueryHandler(studentRepository);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }

    [Fact]
    public async Task 条件なしの場合は全学生を取得する()
    {
        // Arrange
        var student1 = new StudentBuilder()
            .WithId(Guid.NewGuid())
            .Build();

        var student2 = new StudentBuilder()
            .WithId(Guid.NewGuid())
            .Build();

        var student3 = new StudentBuilder()
            .WithId(Guid.NewGuid())
            .Build();

        await _context.Students.AddRangeAsync(student1, student2, student3);
        await _context.SaveChangesAsync();

        var query = new SelectStudentsQuery(); // 条件なし

        // Act
        var result = await _handler.Handle(query, default);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task 学年でフィルタリングして学生を取得する()
    {
        // Arrange
        var student1 = new StudentBuilder()
            .WithId(Guid.NewGuid())
            .WithGrade(1)
            .Build();

        var student2 = new StudentBuilder()
            .WithId(Guid.NewGuid())
            .WithName("鈴木花子")
            .WithGrade(2)
            .Build();

        var student3 = new StudentBuilder()
            .WithId(Guid.NewGuid())
            .WithName("田中次郎")
            .WithGrade(2)
            .Build();

        await _context.Students.AddRangeAsync(student1, student2, student3);
        await _context.SaveChangesAsync();

        var query = new SelectStudentsQuery { Grade = 2 };

        // Act
        var result = await _handler.Handle(query, default);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.All(result, s => Assert.Equal(2, s.Grade));
        Assert.Contains("鈴木花子", result.Select(s => s.Name));
        Assert.Contains("田中次郎", result.Select(s => s.Name));
    }

    [Fact]
    public async Task 名前で部分一致検索して学生を取得する()
    {
        // Arrange
        var student1 = new StudentBuilder()
            .WithName("山田太郎")
            .Build();

        var student2 = new StudentBuilder()
            .WithName("山田花子")
            .Build();

        var student3 = new StudentBuilder()
            .WithName("田中次郎")
            .Build();

        await _context.Students.AddRangeAsync(student1, student2, student3);
        await _context.SaveChangesAsync();

        var query = new SelectStudentsQuery { Name = "山田" };

        // Act
        var result = await _handler.Handle(query, default);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.All(result, s => Assert.Contains("山田", s.Name));
    }

    [Fact]
    public async Task メールアドレスで検索して学生を取得する()
    {
        // Arrange
        var student1 = new StudentBuilder()
            .WithId(Guid.NewGuid())
            .WithEmail("yamada@example.com")
            .Build();

        var student2 = new StudentBuilder()
            .WithId(Guid.NewGuid())
            .WithEmail("suzuki@example.com")
            .Build();

        var student3 = new StudentBuilder()
            .WithId(Guid.NewGuid())
            .WithEmail("suzuki@hoge.com")
            .Build();

        await _context.Students.AddRangeAsync(student1, student2, student3);
        await _context.SaveChangesAsync();

        var query = new SelectStudentsQuery { Email = "example.com" };

        // Act
        var result = await _handler.Handle(query, default);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.All(result, s => Assert.Contains("example.com", s.Email));
    }

    [Fact]
    public async Task 複数の条件を組み合わせて検索する()
    {
        // Arrange
        var student1 = new StudentBuilder()
            .WithId(Guid.NewGuid())
            .WithGrade(1)
            .Build();

        var student2 = new StudentBuilder()
            .WithId(Guid.NewGuid())
            .WithName("山田花子")
            .WithGrade(2)
            .Build();

        var student3 = new StudentBuilder()
            .WithId(Guid.NewGuid())
            .WithName("田中次郎")
            .WithGrade(2)
            .Build();

        await _context.Students.AddRangeAsync(student1, student2, student3);
        await _context.SaveChangesAsync();

        var query = new SelectStudentsQuery { Grade = 2, Name = "山田" };

        // Act
        var result = await _handler.Handle(query, default);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("山田花子", result[0].Name);
        Assert.Equal(2, result[0].Grade);
    }

    [Fact]
    public async Task 検索結果が0件の場合()
    {
        // Arrange
        var student1 = new StudentBuilder()
            .WithId(Guid.NewGuid())
            .Build();

        await _context.Students.AddAsync(student1);
        await _context.SaveChangesAsync();

        var query = new SelectStudentsQuery { Name = "存在しない名前" };

        // Act
        var result = await _handler.Handle(query, default);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task 学生が1件も登録されていない場合()
    {
        // Arrange
        var query = new SelectStudentsQuery();

        // Act
        var result = await _handler.Handle(query, default);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }
}
