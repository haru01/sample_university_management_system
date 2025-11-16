# テスト戦略

## 基本方針

1. **Application層を手厚くテスト**
   - CommandHandler/QueryHandlerに対する包括的なテスト
   - SQLiteインメモリデータベースを使用
   - ドメインロジックとリポジトリの統合テスト

2. **Domain層は複雑なロジックのみテスト**
   - 複雑なビジネスルールを持つドメインサービスのみ
   - 複雑な状態遷移を持つ集約のメソッドのみ
   - 単純なGetter/Setterや値オブジェクトの生成は不要

3. **テスト独立性の保証**
   - IAsyncLifetimeパターンで各テストメソッド専用のDBインスタンスを生成
   - テスト間でデータを共有しない
   - InitializeAsync/DisposeAsyncで明確にコンテキストを管理

4. **ビルダーパターンでテストデータを準備**
   - デフォルト値を持つビルダーで簡潔にテストデータを作成
   - テストごとに必要な値のみ上書きして、因果関係を明確に
   - 複数エンティティが必要な場合は明示的にID/Codeを指定

5. **E2Eテストは実施しない**
   - Application層の統合テストで十分なカバレッジを確保
   - インフラの複雑さを避け、テストの実行速度を優先

## テストピラミッド構成

```text
        ┌─────────────┐
        │  App層      │  多数（メインのテスト）
        │  統合テスト │  SQLiteインメモリDBで統合
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
- **複数エンティティ作成時は明示的にID/Codeを指定**

### ビルダー実装例

```csharp
public class StudentBuilder
{
    private string _email = $"default-{Guid.NewGuid()}@example.com";
    private string _name = "太郎";
    private string _familyName = "山田";
    private int _grade = 1;

    public StudentBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    public StudentBuilder WithName(string name, string familyName)
    {
        _name = name;
        _familyName = familyName;
        return this;
    }

    public StudentBuilder WithGrade(int grade)
    {
        _grade = grade;
        return this;
    }

    public Student Build() => Student.Create(_email, _name, _familyName, _grade);
}

public class CourseBuilder
{
    private CourseCode _code = new("CS101");
    private string _name = "プログラミング入門";
    private int _credits = 2;
    private int _maxCapacity = 30;

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
        _credits = credits;
        return this;
    }

    public CourseBuilder WithMaxCapacity(int maxCapacity)
    {
        _maxCapacity = maxCapacity;
        return this;
    }

    public Course Build() => Course.Create(_code, _name, _credits, _maxCapacity);
}

public class CourseOfferingBuilder
{
    private int _offeringId = 1;
    private CourseCode _courseCode = new("CS101");
    private SemesterId _semesterId = new(2024, "Spring");
    private int _credits = 3;
    private int _maxCapacity = 30;
    private string? _instructor = "田中教授";

    public CourseOfferingBuilder WithOfferingId(int offeringId)
    {
        _offeringId = offeringId;
        return this;
    }

    public CourseOfferingBuilder WithCourseCode(string courseCode)
    {
        _courseCode = new CourseCode(courseCode);
        return this;
    }

    public CourseOfferingBuilder WithSemesterId(int year, string period)
    {
        _semesterId = new SemesterId(year, period);
        return this;
    }

    public CourseOfferingBuilder WithCredits(int credits)
    {
        _credits = credits;
        return this;
    }

    public CourseOfferingBuilder WithMaxCapacity(int maxCapacity)
    {
        _maxCapacity = maxCapacity;
        return this;
    }

    public CourseOfferingBuilder WithInstructor(string? instructor)
    {
        _instructor = instructor;
        return this;
    }

    public CourseOffering Build() => CourseOffering.Create(
        new OfferingId(_offeringId),
        _courseCode,
        _semesterId,
        _credits,
        _maxCapacity,
        _instructor);
}
```

---

## Application層テスト実装

### 基本方針：IAsyncLifetimeパターンによる完全な独立性

各テストメソッドで専用のDBインスタンスを作成し、テスト間の完全な独立性を保証します。

**設計方針:**

- IAsyncLifetimeパターンで各テストメソッド専用のDBを作成
- SQLiteインメモリDBを使用（`:memory:`）
- SqliteConnectionを明示的に管理し、各テストで新規接続
- InitializeAsyncでDB初期化、DisposeAsyncでクリーンアップ
- ビルダーパターンでテストデータを準備し、必要な値のみ上書き
- 複数エンティティ作成時は明示的にID/Codeを指定
- テストの因果関係がメソッド内で完結し、可読性が向上

### CommandHandlerのテスト例

```csharp
public class CreateStudentCommandHandlerTests : IAsyncLifetime
{
    private StudentRegistrationsDbContext _context;
    private CreateStudentCommandHandler _handler;
    private SqliteConnection _connection;

    public async Task InitializeAsync()
    {
        // 各テストメソッドごとに新しいSQLiteインメモリDBを作成
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
    public async Task 正常な学生作成コマンドで学生が作成される()
    {
        // Arrange
        var command = new CreateStudentCommand
        {
            Email = "taro.yamada@example.com",
            Name = "太郎",
            FamilyName = "山田",
            Grade = 1
        };

        // Act
        var studentId = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotEqual(Guid.Empty, studentId);
        var savedStudent = await _context.Students
            .FindAsync(new StudentId(studentId));
        Assert.NotNull(savedStudent);
        Assert.Equal("taro.yamada@example.com", savedStudent!.Email);
    }

    [Fact]
    public async Task 重複したメールアドレスでDomainExceptionがスローされる()
    {
        // Arrange
        var existingStudent = new StudentBuilder()
            .WithEmail("duplicate@example.com")
            .Build();
        _context.Students.Add(existingStudent);
        await _context.SaveChangesAsync();

        var command = new CreateStudentCommand
        {
            Email = "duplicate@example.com", // 重複 ← 因果関係が明確
            Name = "次郎",
            FamilyName = "鈴木",
            Grade = 1
        };

        // Act & Assert
        await Assert.ThrowsAsync<DomainException>(
            async () => await _handler.Handle(command, CancellationToken.None));
    }
}
```

### QueryHandlerのテスト例

```csharp
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
    public async Task ステータスフィルターで履修登録をフィルタリングする()
    {
        // Arrange
        var studentId = new StudentId(Guid.NewGuid());

        // コースを作成
        var course1 = new CourseBuilder()
            .WithCode("CS101")
            .WithName("プログラミング入門")
            .Build();
        var course2 = new CourseBuilder()
            .WithCode("CS102")
            .WithName("データ構造")
            .Build();
        await _context.Courses.AddRangeAsync(course1, course2);
        await _context.SaveChangesAsync();

        // コース開講を作成（複数エンティティなのでIDを明示的に指定）
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
        Assert.Equal("CS101", result[0].CourseCode);
        Assert.Equal("Enrolled", result[0].Status);
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
public class EnrollmentAggregateTests
{
    [Fact]
    public void 進行中ステータスから完了ステータスに変更できる()
    {
        // Arrange
        var studentId = new StudentId(Guid.NewGuid());
        var offeringId = new OfferingId(1);
        var enrollment = Enrollment.Create(studentId, offeringId, "student-001");
        Assert.Equal("Enrolled", enrollment.Status); // 初期状態

        // Act
        enrollment.Complete("student-001", 85);

        // Assert
        Assert.Equal("Completed", enrollment.Status);
        Assert.Equal(85, enrollment.FinalGrade);
    }

    [Fact]
    public void 完了済みステータスから再度完了するとDomainExceptionがスローされる()
    {
        // Arrange
        var studentId = new StudentId(Guid.NewGuid());
        var offeringId = new OfferingId(1);
        var enrollment = Enrollment.Create(studentId, offeringId, "student-001");
        enrollment.Complete("student-001", 85); // 既に完了済み

        // Act & Assert - 完了済みの履修を再度完了できない
        Assert.Throws<DomainException>(() => enrollment.Complete("student-001", 90));
    }

    [Fact]
    public void 進行中ステータスからキャンセルステータスに変更できる()
    {
        // Arrange
        var studentId = new StudentId(Guid.NewGuid());
        var offeringId = new OfferingId(1);
        var enrollment = Enrollment.Create(studentId, offeringId, "student-001");

        // Act
        enrollment.Cancel("student-001", "履修取り消し");

        // Assert
        Assert.Equal("Cancelled", enrollment.Status);
    }
}
```

---

## テストのベストプラクティス

### 1. テスト独立性とIAsyncLifetimeパターン

```csharp
// ✅ 良い例：IAsyncLifetimeで各テストメソッド専用のDBを作成
public class CreateStudentCommandHandlerTests : IAsyncLifetime
{
    private StudentRegistrationsDbContext _context;
    private CreateStudentCommandHandler _handler;
    private SqliteConnection _connection;

    public async Task InitializeAsync()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        await _connection.OpenAsync();

        var options = new DbContextOptionsBuilder<StudentRegistrationsDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new StudentRegistrationsDbContext(options);
        await _context.Database.EnsureCreatedAsync();

        // 依存関係を明示的に初期化
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
}

// ❌ 悪い例：共有のDbContextを使い回す
private static readonly StudentRegistrationsDbContext SharedContext = ...;

// ❌ 悪い例：IDisposableでコンストラクタ初期化（テストメソッド間でDBが共有される）
public class CreateStudentCommandHandlerTests : IDisposable
{
    private StudentRegistrationsDbContext _context;

    public CreateStudentCommandHandlerTests()
    {
        // この初期化は全テストメソッドで1回だけ実行される
        _context = new StudentRegistrationsDbContext(options);
    }
}
```

### 2. 複数エンティティ作成時のID明示

```csharp
// ✅ 良い例：複数のCourseOfferingを作成する際、IDを明示的に指定
var courseOffering1 = new CourseOfferingBuilder()
    .WithOfferingId(1)  // 明示的にID指定
    .WithCourseCode("CS101")
    .Build();

var courseOffering2 = new CourseOfferingBuilder()
    .WithOfferingId(2)  // 明示的にID指定
    .WithCourseCode("CS102")
    .Build();

// ❌ 悪い例：デフォルト値のまま複数作成（UNIQUE制約違反の可能性）
var courseOffering1 = new CourseOfferingBuilder()
    .WithCourseCode("CS101")
    .Build(); // offeringId = 1 (デフォルト)

var courseOffering2 = new CourseOfferingBuilder()
    .WithCourseCode("CS102")
    .Build(); // offeringId = 1 (デフォルト) ← UNIQUE制約違反
```

### 3. AAA パターンの徹底

```csharp
[Fact]
public async Task 正常な学生作成コマンドで学生が作成される()
{
    // Arrange: テストデータの準備
    var command = new CreateStudentCommand { ... };

    // Act: 実行
    var result = await _handler.Handle(command, CancellationToken.None);

    // Assert: 検証
    Assert.NotNull(result);
}
```

### 4. テスト名の命名規則

```text
日本語で振る舞いを明確に表現

例:
- 正常な学生作成コマンドで学生が作成される
- 重複したメールアドレスでDomainExceptionがスローされる
- ステータスフィルターで履修登録をフィルタリングする
- 進行中ステータスから完了ステータスに変更できる
```

### 5. テストカバレッジの優先順位

- **高**: Application層のCommand/QueryHandler（統合テスト）
- **中**: 複雑なビジネスロジック（Domain層のユニットテスト）
- **中**: バリデーションロジック
- **低**: 単純なGetter/Setter、DTOマッピング
- **最小**: E2Eテストは実施しない（Application層の統合テストで十分）

### 6. ビルダーパターンの活用

テストデータビルダーの実装と使用例については、このドキュメントの「[テストデータビルダーパターン](#テストデータビルダーパターン)」セクションを参照してください。

**ポイント:**

- デフォルト値で簡潔にテストデータを作成
- 必要な値のみ `.WithXxx()` で上書き
- テストの因果関係が一目で理解できる
- 複数エンティティ作成時は明示的にID/Codeを指定

```csharp
// デフォルト値で作成
var student = new StudentBuilder().Build();

// 必要な値のみ上書き
var specialStudent = new StudentBuilder()
    .WithEmail("hanako.suzuki@example.com")
    .WithName("花子", "鈴木")
    .WithGrade(2)
    .Build();

// 複数エンティティ作成時は明示的にID指定
var offering1 = new CourseOfferingBuilder()
    .WithOfferingId(1)
    .WithCourseCode("CS101")
    .Build();

var offering2 = new CourseOfferingBuilder()
    .WithOfferingId(2)
    .WithCourseCode("CS102")
    .Build();
```

---

## テスト実行環境

### 必要なNuGetパッケージ

```xml
<ItemGroup>
  <!-- テストフレームワーク -->
  <PackageReference Include="xunit" Version="2.9.3" />
  <PackageReference Include="xunit.runner.visualstudio" Version="3.1.5" />
  <PackageReference Include="Microsoft.NET.Test.Sdk" Version="18.0.0" />

  <!-- SQLiteインメモリDB -->
  <PackageReference Include="Microsoft.Data.Sqlite" Version="9.0.10" />
  <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="9.0.10" />

  <!-- カバレッジ -->
  <PackageReference Include="coverlet.collector" Version="6.0.4" />

  <!-- モック -->
  <PackageReference Include="Moq" Version="4.20.72" />
</ItemGroup>
```

### テスト並列化設定

xunit.runner.json:
```json
{
  "$schema": "https://xunit.net/schema/current/xunit.runner.schema.json",
  "parallelizeAssembly": true,
  "parallelizeTestCollections": true,
  "maxParallelThreads": -1
}
```

.csproj:
```xml
<ItemGroup>
  <None Update="xunit.runner.json">
    <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
  </None>
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
      - uses: actions/checkout@v4

      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '9.0.x'

      - name: Restore dependencies
        run: dotnet restore

      - name: Run unit tests
        run: dotnet test --filter "Category=Unit" --logger "trx;LogFileName=unit-tests.trx"

      - name: Run integration tests
        run: dotnet test --filter "Category=Integration" --logger "trx;LogFileName=integration-tests.trx"
```

---

## テストカテゴリ分類

テストを明確に分類することで、CI/CDパイプラインでの実行制御や、開発中の部分的なテスト実行が容易になります。

### xUnitでのトレイト指定

```csharp
[Trait("Category", "Unit")]
public class EnrollmentAggregateTests
{
    // ドメインロジックの単体テスト
}

[Trait("Category", "Integration")]
public class CreateStudentCommandHandlerTests : IAsyncLifetime
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
dotnet test --filter "FullyQualifiedName~CreateStudentCommandHandlerTests"

# 複数カテゴリの組み合わせ
dotnet test --filter "Category=Unit|Category=Integration"
```

---

## テストの実行順序と戦略

### 開発時のテストフロー

1. **ローカル開発中**
   ```bash
   # 変更した部分のテストのみ実行
   dotnet test --filter "FullyQualifiedName~CreateStudentCommandHandlerTests"
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
```

この戦略により、早い段階で失敗を検出し、CI/CDパイプラインの実行時間を最適化できます。

---

## SQLiteインメモリDBの利点

### EF Core InMemoryProviderからの移行理由

1. **より現実的なテスト環境**
   - SQLiteは実際のRDBMSに近い動作
   - 制約（UNIQUE、FOREIGN KEY）が正しく機能
   - トランザクション動作が現実的

2. **バグの早期発見**
   - UNIQUE制約違反を検出可能
   - NULL制約違反を検出可能
   - SQLクエリの問題を検出可能

3. **本番環境との一貫性**
   - PostgreSQLとの動作の違いを最小化
   - スキーマ設定はSQLiteで無視され、PostgreSQLで適用される
   - マイグレーションと同じスキーマ定義を使用

### SQLite使用時の注意点

- スキーマはSQLiteで無視される（PostgreSQLでのみ使用）
- テストではスキーマなしでテーブルが作成される
- 本番環境（PostgreSQL）ではスキーマ付きでテーブルが作成される
- この違いは問題なく、両環境で正常に動作する
