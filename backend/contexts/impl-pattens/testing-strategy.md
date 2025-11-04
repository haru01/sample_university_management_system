# テスト戦略

## 基本方針

1. **Application層を手厚くテスト**
   - CommandHandler/QueryHandlerに対する包括的なテスト
   - インメモリデータベース（EF Core In-Memory Provider）を使用
   - ドメインロジックとリポジトリの統合テスト

2. **Domain層は複雑なロジックのみテスト**
   - 複雑なビジネスルールを持つドメインサービスのみ
   - 複雑な状態遷移を持つ集約のメソッドのみ
   - 単純なGetter/Setterや値オブジェクトの生成は不要

3. **テスト独立性の保証**
   - 各テストメソッドで専用のDbContextインスタンスを生成
   - テスト間でデータを共有しない
   - SetUp/TearDownで明確にコンテキストを管理

4. **ビルダーパターンでテストデータを準備**
   - デフォルト値を持つビルダーで簡潔にテストデータを作成
   - テストごとに必要な値のみ上書きして、因果関係を明確に

5. **E2Eテストは実施しない**
   - Application層の統合テストで十分なカバレッジを確保
   - インフラの複雑さを避け、テストの実行速度を優先

## テストピラミッド構成

```text
        ┌─────────────┐
        │  App層      │  多数（メインのテスト）
        │  統合テスト │  インメモリDBで統合
        │             │
        ├─────────────┤
        │  Domain     │  少数（複雑なロジックのみ）
        │  Unit       │
        └─────────────┘
```

---

## テストデータビルダーパターン

### ビルダーの設計方針

- デフォルト値を持ち、引数なしで有効なエンティティを生成できる
- Fluentインターフェースで必要な値のみ上書き可能
- テストの可読性を最優先し、因果関係を明確に

### ビルダー実装例

```csharp
public class StudentBuilder
{
    private StudentName _name = new("太郎", "山田");
    private Email _email = new("taro.yamada@example.com");
    private int _enrollmentYear = 2024;

    public StudentBuilder WithName(string firstName, string lastName)
    {
        _name = new StudentName(firstName, lastName);
        return this;
    }

    public StudentBuilder WithEmail(string email)
    {
        _email = new Email(email);
        return this;
    }

    public StudentBuilder WithEnrollmentYear(int year)
    {
        _enrollmentYear = year;
        return this;
    }

    public Student Build() => Student.Create(_name, _email, _enrollmentYear);
}

public class CourseBuilder
{
    private CourseCode _code = new("CS101");
    private string _name = "プログラミング入門";
    private Credits _credits = new(2);
    private Department _department = new("CS");

    public CourseBuilder WithCode(string code)
    {
        _code = new CourseCode(code);
        return this;
    }

    public CourseBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public CourseBuilder WithCredits(int credits)
    {
        _credits = new Credits(credits);
        return this;
    }

    public Course Build() => Course.Create(_code, _name, _credits, _department);
}

public class EnrollmentBuilder
{
    private StudentId _studentId = new(Guid.NewGuid());
    private CourseCode _courseCode = new("CS101");
    private Semester _semester = new(2024, SemesterPeriod.Spring);

    public EnrollmentBuilder WithStudentId(StudentId studentId)
    {
        _studentId = studentId;
        return this;
    }

    public EnrollmentBuilder WithCourseCode(CourseCode courseCode)
    {
        _courseCode = courseCode;
        return this;
    }

    public EnrollmentBuilder WithSemester(int year, SemesterPeriod period)
    {
        _semester = new Semester(year, period);
        return this;
    }

    public Enrollment Build() => Enrollment.Create(_studentId, _courseCode, _semester);
}
```

---

## Application層テスト実装

### 基本方針：ベースクラスを使わないシンプルな設計

各テストクラスで直接DbContextを管理し、継承による暗黙的な依存を排除します。

**設計方針:**

- ベースクラスは使わず、各テストクラスで明示的にDbContextを管理
- ビルダーパターンでテストデータを準備し、必要な値のみ上書き
- 各テストメソッド内でArrangeセクションにテストデータを準備
- テストの因果関係がメソッド内で完結し、可読性が向上
- 継承による複雑さを排除

### CommandHandlerのテスト例

```csharp
public class EnrollStudentCommandHandlerTests : IDisposable
{
    private EnrollmentDbContext _context = null!;
    private EnrollStudentCommandHandler _handler = null!;

    public EnrollStudentCommandHandlerTests()
    {
        // 各テストごとに新しいDbContextを作成
        var options = new DbContextOptionsBuilder<EnrollmentDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new EnrollmentDbContext(options);

        // ハンドラーの依存関係を初期化
        var enrollmentRepository = new EnrollmentRepository(_context);
        var studentRepository = new StudentRepository(_context);
        var courseRepository = new CourseRepository(_context);
        var domainService = new EnrollmentDomainService();

        _handler = new EnrollStudentCommandHandler(
            enrollmentRepository,
            studentRepository,
            courseRepository,
            domainService,
            _context);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }

    [Fact]
    public async Task 正常な履修登録コマンドで履修登録が作成される()
    {
        // Arrange
        var student = new StudentBuilder().Build();
        var course = new CourseBuilder().Build();
        _context.Students.Add(student);
        _context.Courses.Add(course);
        await _context.SaveChangesAsync();

        var command = new EnrollStudentCommand
        {
            StudentId = student.Id.Value,
            CourseCode = course.Code.Value,
            SemesterYear = 2024,
            SemesterPeriod = "Spring"
        };

        // Act
        var enrollmentId = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(enrollmentId);
        var savedEnrollment = await _context.Enrollments
            .FirstOrDefaultAsync(e => e.Id == enrollmentId);
        Assert.NotNull(savedEnrollment);
        Assert.Equal(student.Id, savedEnrollment!.StudentId);
        Assert.Equal(course.Code, savedEnrollment.CourseCode);
    }

    [Fact]
    public void 存在しない学生IDでNotFoundExceptionがスローされる()
    {
        // Arrange
        var command = new EnrollStudentCommand
        {
            StudentId = Guid.NewGuid(), // 存在しない学生ID
            CourseCode = "CS101",
            SemesterYear = 2024,
            SemesterPeriod = "Spring"
        };

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            async () => await _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task 不正な科目コード形式でArgumentExceptionがスローされる()
    {
        // Arrange
        var student = new StudentBuilder().Build();
        _context.Students.Add(student);
        await _context.SaveChangesAsync();

        var command = new EnrollStudentCommand
        {
            StudentId = student.Id.Value,
            CourseCode = "INVALID", // 不正な形式 ← 因果関係が明確
            SemesterYear = 2024,
            SemesterPeriod = "Spring"
        };

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            async () => await _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task 重複した履修登録でInvalidOperationExceptionがスローされる()
    {
        // Arrange
        var student = new StudentBuilder().Build();
        var course = new CourseBuilder().Build();
        _context.Students.Add(student);
        _context.Courses.Add(course);

        // 既存の履修登録を作成 ← 因果関係が明確
        var existingEnrollment = new EnrollmentBuilder()
            .WithStudentId(student.Id)
            .WithCourseCode(course.Code)
            .Build();
        _context.Enrollments.Add(existingEnrollment);
        await _context.SaveChangesAsync();

        var command = new EnrollStudentCommand
        {
            StudentId = student.Id.Value,
            CourseCode = course.Code.Value,
            SemesterYear = 2024,
            SemesterPeriod = "Spring"
        };

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _handler.Handle(command, CancellationToken.None));
    }
}
```

### QueryHandlerのテスト例

```csharp
public class GetEnrollmentsByStudentQueryHandlerTests : IDisposable
{
    private EnrollmentDbContext _context = null!;
    private GetEnrollmentsByStudentQueryHandler _handler = null!;

    public GetEnrollmentsByStudentQueryHandlerTests()
    {
        // 各テストごとに新しいDbContextを作成
        var options = new DbContextOptionsBuilder<EnrollmentDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new EnrollmentDbContext(options);
        _handler = new GetEnrollmentsByStudentQueryHandler(_context);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }

    [Fact]
    public async Task 学生の全ての履修登録が取得できる()
    {
        // Arrange
        var student = new StudentBuilder().Build();
        var course1 = new CourseBuilder().Build();
        var course2 = new CourseBuilder()
            .WithCode("MATH201")
            .WithName("線形代数")
            .WithCredits(3)
            .Build();

        _context.Students.Add(student);
        _context.Courses.AddRange(course1, course2);

        var enrollment1 = new EnrollmentBuilder()
            .WithStudentId(student.Id)
            .WithCourseCode(course1.Code)
            .Build();
        var enrollment2 = new EnrollmentBuilder()
            .WithStudentId(student.Id)
            .WithCourseCode(course2.Code)
            .WithSemester(2024, SemesterPeriod.Fall)
            .Build();
        _context.Enrollments.AddRange(enrollment1, enrollment2);

        await _context.SaveChangesAsync();

        var query = new GetEnrollmentsByStudentQuery
        {
            StudentId = student.Id.Value
        };

        // Act
        var results = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Equal(new[] { "CS101", "MATH201" }, results.Select(r => r.CourseCode).OrderBy(c => c));
    }

    [Fact]
    public async Task ステータスでフィルタリングして履修登録が取得できる()
    {
        // Arrange
        var student = new StudentBuilder().Build();
        var course1 = new CourseBuilder().Build();
        var course2 = new CourseBuilder()
            .WithCode("MATH201")
            .Build();

        _context.Students.Add(student);
        _context.Courses.AddRange(course1, course2);

        var enrollment1 = new EnrollmentBuilder()
            .WithStudentId(student.Id)
            .WithCourseCode(course1.Code)
            .Build();

        var enrollment2 = new EnrollmentBuilder()
            .WithStudentId(student.Id)
            .WithCourseCode(course2.Code)
            .Build();
        enrollment2.Complete(); // 完了状態にする ← 因果関係が明確

        _context.Enrollments.AddRange(enrollment1, enrollment2);
        await _context.SaveChangesAsync();

        var query = new GetEnrollmentsByStudentQuery
        {
            StudentId = student.Id.Value,
            Status = "Completed" // 完了したもののみ取得 ← 因果関係が明確
        };

        // Act
        var results = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Single(results);
        Assert.Equal("MATH201", results[0].CourseCode);
        Assert.Equal("Completed", results[0].Status);
    }

    [Fact]
    public async Task 存在しない学生IDで空のリストが返される()
    {
        // Arrange
        var query = new GetEnrollmentsByStudentQuery
        {
            StudentId = Guid.NewGuid() // データベースに存在しない学生ID
        };

        // Act
        var results = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Empty(results);
    }
}
```

---

## Domain層テスト実装

### テスト対象の選定基準

Domain層のユニットテストは、**複雑なロジックのみ**に限定します。

**テストを書くべきもの:**

- 複雑なビジネスルールを持つドメインサービス
- 複雑な状態遷移を持つ集約のメソッド
- 複数の条件分岐を持つバリデーションロジック

**テストを書かないもの:**

- 単純なGetter/Setter
- 値オブジェクトの単純な生成
- 単純なプロパティ代入のみのメソッド

### Domain層テスト例

```csharp
public class EnrollmentDomainServiceTests
{
    private EnrollmentDomainService _service;

    public EnrollmentDomainServiceTests()
    {
        _service = new EnrollmentDomainService();
    }

    [Fact]
    public void 最大単位数を超える場合は履修登録できない()
    {
        // Arrange
        var student = new StudentBuilder().Build();
        var newCourse = new CourseBuilder()
            .WithCredits(4) // 4単位の科目
            .Build();

        // 既に20単位登録済み（上限ギリギリ）
        var existingEnrollments = new List<Enrollment>
        {
            new EnrollmentBuilder()
                .WithStudentId(student.Id)
                .Build(), // 2単位（デフォルト）
            new EnrollmentBuilder()
                .WithStudentId(student.Id)
                .Build(), // 2単位（デフォルト）
            // ... 合計20単位
        };

        // Act
        var canEnroll = _service.CanEnroll(student, newCourse, existingEnrollments);

        // Assert - 24単位になるので登録不可
        Assert.False(canEnroll);
    }

    [Fact]
    public void 最大単位数以内の場合は履修登録できる()
    {
        // Arrange
        var student = new StudentBuilder().Build();
        var newCourse = new CourseBuilder()
            .WithCredits(2)
            .Build();

        var existingEnrollments = new List<Enrollment>
        {
            new EnrollmentBuilder()
                .WithStudentId(student.Id)
                .Build() // 2単位
        };

        // Act
        var canEnroll = _service.CanEnroll(student, newCourse, existingEnrollments);

        // Assert - 合計4単位なので登録可能
        Assert.True(canEnroll);
    }
}

public class EnrollmentAggregateTests
{
    [Fact]
    public void 進行中ステータスから完了ステータスに変更できる()
    {
        // Arrange
        var enrollment = new EnrollmentBuilder().Build();
        Assert.Equal("InProgress", enrollment.Status.Value); // 初期状態

        // Act
        enrollment.Complete();

        // Assert
        Assert.Equal("Completed", enrollment.Status.Value);
    }

    [Fact]
    public void 完了済みステータスから再度完了するとInvalidOperationExceptionがスローされる()
    {
        // Arrange
        var enrollment = new EnrollmentBuilder().Build();
        enrollment.Complete(); // 既に完了済み

        // Act & Assert - 完了済みの履修を再度完了できない
        Assert.Throws<InvalidOperationException>(() => enrollment.Complete());
    }

    [Fact]
    public void 進行中ステータスからキャンセルステータスに変更できる()
    {
        // Arrange
        var enrollment = new EnrollmentBuilder().Build();

        // Act
        enrollment.Cancel();

        // Assert
        Assert.Equal("Cancelled", enrollment.Status.Value);
    }
}
```

---

## テストのベストプラクティス

### 1. テスト独立性とシンプルな設計

```csharp
// ✅ 良い例：各テストで新しいDbContextを明示的に作成
public class EnrollStudentCommandHandlerTests : IDisposable
{
    private EnrollmentDbContext _context = null!;
    private EnrollStudentCommandHandler _handler = null!;

    public EnrollStudentCommandHandlerTests()
    {
        var options = new DbContextOptionsBuilder<EnrollmentDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new EnrollmentDbContext(options);

        // 依存関係を明示的に初期化
        var enrollmentRepository = new EnrollmentRepository(_context);
        // ... 他の依存関係
        _handler = new EnrollStudentCommandHandler(enrollmentRepository, ...);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}

// ❌ 悪い例：共有のDbContextを使い回す
private static readonly EnrollmentDbContext SharedContext = ...;

// ❌ 悪い例：ベースクラスで暗黙的に初期化（継承の複雑さ）
public class EnrollStudentCommandHandlerTests : ApplicationTestBase
{
    // base.SetUp()への暗黙的な依存
}
```

### 2. AAA パターンの徹底

```csharp
[Fact]
public async Task 正常な履修登録コマンドで履修登録が作成される()
{
    // Arrange: テストデータの準備
    var command = new EnrollStudentCommand { ... };

    // Act: 実行
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert: 検証
    Assert.NotNull(result);
}
```

### 3. テスト名の命名規則

```text
日本語で振る舞いを明確に表現

例:
- 正常な履修登録コマンドで履修登録が作成される
- 存在しない学生IDでNotFoundExceptionがスローされる
- 重複した履修登録でEnrollmentDomainExceptionがスローされる
- 最大単位数を超える場合は履修登録できない
```

### 4. テストカバレッジの優先順位

- **高**: Application層のCommand/QueryHandler（統合テスト）
- **中**: 複雑なビジネスロジック（Domain層のユニットテスト）
- **中**: バリデーションロジック
- **低**: 単純なGetter/Setter、DTOマッピング
- **最小**: E2Eテストは実施しない（Application層の統合テストで十分）

### 5. ビルダーパターンの活用

テストデータビルダーの実装と使用例については、このドキュメントの「[テストデータビルダーパターン](#テストデータビルダーパターン)」セクションを参照してください。

**ポイント:**

- デフォルト値で簡潔にテストデータを作成
- 必要な値のみ `.WithXxx()` で上書き
- テストの因果関係が一目で理解できる

```csharp
// デフォルト値で作成
var student = new StudentBuilder().Build();

// 必要な値のみ上書き
var specialStudent = new StudentBuilder()
    .WithName("花子", "鈴木")
    .WithEnrollmentYear(2023)
    .Build();
```

---

## テスト実行環境

### 必要なNuGetパッケージ

```xml
<ItemGroup>
  <!-- テストフレームワーク -->
  <PackageReference Include="xunit" Version="2.6.6" />
  <PackageReference Include="xunit.runner.visualstudio" Version="2.5.6" />
  <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />

  <!-- インメモリDB -->
  <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="9.0.0" />

  <!-- E2Eテスト用（今回は使用しない） -->
  <!-- <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="9.0.0" /> -->
</ItemGroup>
```

### CI/CDでのテスト実行

```yaml
# GitHub Actions例
name: Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3

      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'

      - name: Restore dependencies
        run: dotnet restore

      - name: Run unit tests
        run: dotnet test --filter "Category=Unit" --logger "trx;LogFileName=unit-tests.trx"

      - name: Run integration tests
        run: dotnet test --filter "Category=Integration" --logger "trx;LogFileName=integration-tests.trx"

      - name: Run E2E tests
        run: dotnet test --filter "Category=E2E" --logger "trx;LogFileName=e2e-tests.trx"
```

---

## テストカテゴリ分類

テストを明確に分類することで、CI/CDパイプラインでの実行制御や、開発中の部分的なテスト実行が容易になります。

### xUnitでのトレイト指定

```csharp
[Trait("Category", "Unit")]
public class EnrollmentDomainTests
{
    // ドメインロジックの単体テスト
}

[Trait("Category", "Integration")]
public class EnrollStudentCommandHandlerTests : IDisposable
{
    // Application層の統合テスト
}

// E2Eテストは実施しない方針
```

### トレイト別実行コマンド

```bash
# ドメイン層の単体テストのみ実行
dotnet test --filter "Category=Unit"

# Application層の統合テストのみ実行
dotnet test --filter "Category=Integration"

# 特定のテストクラスのみ実行
dotnet test --filter "FullyQualifiedName~CreateCourseHandlerTests"

# 複数カテゴリの組み合わせ
dotnet test --filter "Category=Unit|Category=Integration"
```

---

## テストの実行順序と戦略

### 開発時のテストフロー

1. **ローカル開発中**
   ```bash
   # 変更した部分のユニットテストのみ実行
   dotnet test --filter "FullyQualifiedName~EnrollmentTests"
   ```

2. **コミット前**
   ```bash
   # 全ユニット・統合テスト実行（高速）
   dotnet test --filter "Category!=E2E"
   ```

3. **プルリクエスト作成時**
   ```bash
   # 全テスト実行（CI環境）
   dotnet test
   ```

### CI/CDでの段階的実行

```yaml
# 段階的テスト実行の例
steps:
  - name: Fast tests (Unit)
    run: dotnet test --filter "Category=Unit" --no-build

  - name: Integration tests
    run: dotnet test --filter "Category=Integration" --no-build
    if: success()

  - name: E2E tests
    run: dotnet test --filter "Category=E2E" --no-build
    if: success()
```

この戦略により、早い段階で失敗を検出し、CI/CDパイプラインの実行時間を最適化できます。
