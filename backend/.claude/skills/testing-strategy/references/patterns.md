# Test Patterns

## Test Class Templates

### Application Layer (IAsyncLifetime)

```csharp
public class SomeHandlerTests : IAsyncLifetime
{
    private SomeDbContext _context = null!;
    private SomeHandler _handler = null!;
    private SqliteConnection _connection = null!;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();

        var options = new DbContextOptionsBuilder<SomeDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new SomeDbContext(options);
        await _context.Database.EnsureCreatedAsync();

        var repository = new SomeRepository(_context);
        _handler = new SomeHandler(repository);
    }

    public async Task DisposeAsync()
    {
        if (_context != null) await _context.DisposeAsync();
        if (_connection != null) await _connection.DisposeAsync();
    }
}
```

### Domain Layer

```csharp
public class SomeAggregateTests
{
    [Fact]
    public void 状態遷移の日本語説明()
    {
        // Arrange → Act → Assert
    }
}
```

---

## Test Data Builders

Key rules:

- `new XxxBuilder().Build()` → valid entity with defaults
- **Multiple entities require explicit unique IDs**

```csharp
public class CourseOfferingBuilder
{
    private int _offeringId = 1;
    private CourseCode _courseCode = new("CS101");
    private SemesterId _semesterId = new(2024, "Spring");
    private int _credits = 3;
    private int _maxCapacity = 30;
    private string? _instructor = "田中教授";

    public CourseOfferingBuilder WithOfferingId(int id) { _offeringId = id; return this; }
    public CourseOfferingBuilder WithCourseCode(string code) { _courseCode = new(code); return this; }
    // ... other With methods

    public CourseOffering Build() => CourseOffering.Create(
        new OfferingId(_offeringId), _courseCode, _semesterId, _credits, _maxCapacity, _instructor);
}

// Usage
var offering1 = new CourseOfferingBuilder().WithOfferingId(1).WithCourseCode("CS101").Build();
var offering2 = new CourseOfferingBuilder().WithOfferingId(2).WithCourseCode("CS102").Build();
```

---

## Test Examples

### CommandHandler - Success

```csharp
[Fact]
public async Task 正常なコマンドで期待する結果が返される()
{
    // Arrange
    var command = new CreateStudentCommand
    {
        Email = "taro@example.com",
        Name = "太郎",
        FamilyName = "山田",
        Grade = 1
    };

    // Act
    var studentId = await _handler.Handle(command, CancellationToken.None);

    // Assert
    Assert.NotEqual(Guid.Empty, studentId);
    var saved = await _context.Students.FindAsync(new StudentId(studentId));
    Assert.NotNull(saved);
}
```

### CommandHandler - Validation Error

```csharp
[Fact]
public async Task 重複データでDomainExceptionがスローされる()
{
    // Arrange - existing data
    var existing = new StudentBuilder().WithEmail("dup@example.com").Build();
    _context.Students.Add(existing);
    await _context.SaveChangesAsync();

    var command = new CreateStudentCommand { Email = "dup@example.com" /* ... */ };

    // Act & Assert
    await Assert.ThrowsAsync<DomainException>(
        () => _handler.Handle(command, CancellationToken.None));
}
```

### QueryHandler - Related Data

```csharp
[Fact]
public async Task 関連データを含むクエリが正しく取得できる()
{
    // Arrange - dependency order
    var course = new CourseBuilder().WithCode("CS101").Build();
    await _context.Courses.AddAsync(course);

    var offering = new CourseOfferingBuilder()
        .WithOfferingId(1)
        .WithCourseCode("CS101")
        .Build();
    await _context.CourseOfferings.AddAsync(offering);
    await _context.SaveChangesAsync();

    // Act
    var result = await _handler.Handle(new GetOfferingQuery { OfferingId = 1 }, default);

    // Assert
    Assert.NotNull(result);
    Assert.Equal("CS101", result.CourseCode);
}
```

### Domain - State Transition

```csharp
[Fact]
public void 進行中から完了に状態遷移できる()
{
    var enrollment = Enrollment.Create(studentId, offeringId, "user");

    enrollment.Complete("user", grade: 85);

    Assert.Equal(EnrollmentStatus.Completed, enrollment.Status);
}

[Fact]
public void 完了済みから再度完了するとDomainExceptionがスローされる()
{
    var enrollment = Enrollment.Create(studentId, offeringId, "user");
    enrollment.Complete("user", 85);

    Assert.Throws<DomainException>(() => enrollment.Complete("user", 90));
}
```

---

## Anti-Patterns

| Pattern | Problem | Fix |
| ------- | ------- | --- |
| Shared DbContext | Test pollution | IAsyncLifetime |
| Constructor init | State shared | InitializeAsync |
| Default IDs × multiple | UNIQUE violation | Explicit IDs |
| Over-mocking | Unrealistic | Real repos + SQLite |
| Assert impl details | Brittle | Assert behavior |

```csharp
// WRONG
var offering1 = new CourseOfferingBuilder().Build();  // id=1
var offering2 = new CourseOfferingBuilder().Build();  // id=1 CRASH

// CORRECT
var offering1 = new CourseOfferingBuilder().WithOfferingId(1).Build();
var offering2 = new CourseOfferingBuilder().WithOfferingId(2).Build();
```

---

## Organization

```text
tests/ModuleName.Tests/
├── Application/Commands/  ← Integration tests
├── Application/Queries/   ← Integration tests
├── Domain/                ← Unit tests (complex logic only)
└── Builders/
```

| Element | Convention |
| ------- | ---------- |
| Class | `{ClassUnderTest}Tests` |
| Method | Japanese behavior description |

```csharp
[Trait("Category", "Integration")]
public class HandlerTests : IAsyncLifetime { }

[Trait("Category", "Unit")]
public class AggregateTests { }
```
