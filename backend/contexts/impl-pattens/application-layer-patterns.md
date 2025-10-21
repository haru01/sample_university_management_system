# Application層 実装パターン（CQRS）

## CQRS 基本パターン

Command（書き込み）とQuery（読み取り）を明確に分離。

```csharp
// Command: 状態を変更する操作（単純なrecord型で十分）
public record CreateCourseCommand
{
    public required string CourseCode { get; init; }
    public required string Name { get; init; }
    public required int Credits { get; init; }
    public required int MaxCapacity { get; init; }
}

// Query: データを取得する操作（単純なrecord型で十分）
public record GetCoursesQuery
{
    // フィルタ条件などがあれば定義
}

// Service: 各ユースケースごとに専用インターフェースを定義
public interface ICreateCourseService
{
    Task<string> CreateCourseAsync(CreateCourseCommand command);
}

public interface IGetCoursesService
{
    Task<List<CourseDto>> GetCoursesAsync();
}
```

**設計方針:**
- CommandやQueryに汎用的なインターフェース（`ICommand<T>`等）は不要（YAGNI原則）
- 各Serviceに専用インターフェースを定義し、明確な契約を提供
- 命名規則: `I{動詞}{名詞}Service` (例: `ICreateCourseService`, `IGetCoursesService`)

---

## Command パターン

### Command定義
```csharp
public record EnrollStudentCommand
{
    public required Guid StudentId { get; init; }
    public required string CourseCode { get; init; }
    public required int SemesterYear { get; init; }
    public required string SemesterPeriod { get; init; }
}
```

### Service インターフェース定義
```csharp
public interface IEnrollStudentService
{
    Task<EnrollmentId> EnrollStudentAsync(EnrollStudentCommand command);
}
```

### Service実装
```csharp
public class EnrollStudentService : IEnrollStudentService
{
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly ICourseRepository _courseRepository;
    private readonly EnrollmentDomainService _domainService;

    public EnrollStudentService(
        IEnrollmentRepository enrollmentRepository,
        IStudentRepository studentRepository,
        ICourseRepository courseRepository,
        EnrollmentDomainService domainService)
    {
        _enrollmentRepository = enrollmentRepository;
        _studentRepository = studentRepository;
        _courseRepository = courseRepository;
        _domainService = domainService;
    }

    public async Task<EnrollmentId> EnrollStudentAsync(EnrollStudentCommand command)
    {
        // 1. 値オブジェクトの構築
        var studentId = new StudentId(command.StudentId);
        var courseCode = new CourseCode(command.CourseCode);
        var semester = new Semester(
            command.SemesterYear,
            Enum.Parse<SemesterPeriod>(command.SemesterPeriod));

        // 2. 必要なエンティティの取得
        var student = await _studentRepository.GetByIdAsync(studentId)
            ?? throw new NotFoundException($"Student {studentId} not found");

        var course = await _courseRepository.GetByCodeAsync(courseCode)
            ?? throw new NotFoundException($"Course {courseCode} not found");

        var existingEnrollments = await _enrollmentRepository
            .GetByStudentIdAsync(studentId);

        // 3. ビジネスルールの検証（ドメインサービス）
        if (!_domainService.CanEnroll(student, course, existingEnrollments))
            throw new EnrollmentDomainException(
                "ENROLLMENT_NOT_ALLOWED",
                "履修条件を満たしていません");

        // 4. 集約の生成
        var enrollment = Enrollment.Create(studentId, courseCode, semester);

        // 5. リポジトリへの永続化
        await _enrollmentRepository.AddAsync(enrollment);

        // 6. Unit of Workパターンでトランザクションコミット
        await _enrollmentRepository.SaveChangesAsync();

        return enrollment.Id;
    }
}
```

### Serviceのパターン（Command側）
1. **入力の変換**: プリミティブ型 → 値オブジェクト
2. **エンティティ取得**: リポジトリから必要な集約を取得
3. **ビジネスロジック実行**: ドメインサービスや集約メソッド呼び出し
4. **永続化**: リポジトリのSaveChangesAsync()でUnit of Work完結
5. **結果返却**: 生成されたIDやサマリーを返す

---

## Query パターン

### Query定義
```csharp
public record GetEnrollmentsByStudentQuery
{
    public required Guid StudentId { get; init; }
    public string? Status { get; init; }
}
```

### DTO定義
```csharp
public record EnrollmentSummaryDto
{
    public required Guid EnrollmentId { get; init; }
    public required string CourseCode { get; init; }
    public required string CourseName { get; init; }
    public required string Status { get; init; }
    public required int SemesterYear { get; init; }
    public required string SemesterPeriod { get; init; }
}
```

### Service インターフェース定義
```csharp
public interface IGetEnrollmentsByStudentService
{
    Task<List<EnrollmentSummaryDto>> GetEnrollmentsByStudentAsync(GetEnrollmentsByStudentQuery query);
}
```

### Service実装
```csharp
public class GetEnrollmentsByStudentService : IGetEnrollmentsByStudentService
{
    private readonly EnrollmentDbContext _context;

    public GetEnrollmentsByStudentService(EnrollmentDbContext context)
    {
        _context = context;
    }

    public async Task<List<EnrollmentSummaryDto>> GetEnrollmentsByStudentAsync(
        GetEnrollmentsByStudentQuery query)
    {
        var studentId = new StudentId(query.StudentId);

        var queryable = _context.Enrollments
            .AsNoTracking() // 読み取り専用のため変更追跡を無効化
            .Include(e => e.Course) // N+1問題回避
            .Where(e => e.StudentId == studentId);

        // オプショナルフィルタ
        if (!string.IsNullOrEmpty(query.Status))
        {
            queryable = queryable.Where(e => e.Status.Value == query.Status);
        }

        return await queryable
            .Select(e => new EnrollmentSummaryDto
            {
                EnrollmentId = e.Id.Value,
                CourseCode = e.CourseCode.Value,
                CourseName = e.Course.Name,
                Status = e.Status.Value,
                SemesterYear = e.Semester.Year,
                SemesterPeriod = e.Semester.Period.ToString()
            })
            .OrderBy(e => e.SemesterYear)
            .ThenBy(e => e.CourseCode)
            .ToListAsync();
    }
}
```

### Serviceのパターン（Query側）
1. **AsNoTracking()**: 変更追跡を無効化してパフォーマンス向上
2. **Include()**: N+1問題を回避
3. **Select()でDTO投影**: 必要な列のみ取得
4. **生SQL許容**: 複雑なクエリではDapperやストアドプロシージャも可

---

## バリデーション戦略

### 基本方針: 集約内バリデーション

このプロジェクトでは**FluentValidationは使用せず、集約内でバリデーションを実行**します。

**理由:**
- シンプルなドメインロジックでは集約内バリデーションで十分
- ビジネスルールが集約に集約され、責務が明確
- 外部ライブラリへの依存を最小化
- FluentValidationは将来的に複雑なバリデーションルールが必要になった場合に導入検討

### 集約内バリデーションの実装例

```csharp
public class Course : AggregateRoot<CourseCode>
{
    public string Name { get; private set; }
    public int Credits { get; private set; }
    public int MaxCapacity { get; private set; }

    // ファクトリメソッドでバリデーション実行
    public static Course Create(CourseCode code, string name, int credits, int maxCapacity)
    {
        EnsureNameNotEmpty(name);
        EnsureCreditsBetween1And10(credits);
        EnsureMaxCapacityGreaterThanZero(maxCapacity);

        return new Course(code, name, credits, maxCapacity);
    }

    // ビジネスルール検証メソッド
    private static void EnsureNameNotEmpty(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
    }

    private static void EnsureCreditsBetween1And10(int credits)
    {
        if (credits < 1 || credits > 10)
            throw new ArgumentException("Credits must be between 1 and 10", nameof(credits));
    }

    private static void EnsureMaxCapacityGreaterThanZero(int maxCapacity)
    {
        if (maxCapacity < 1)
            throw new ArgumentException("Max capacity must be greater than 0", nameof(maxCapacity));
    }
}
```

### 値オブジェクトでのバリデーション

```csharp
public partial record CourseCode
{
    private const string Pattern = @"^[A-Z]{2,4}\d{3,4}$";

    public string Value { get; }

    public CourseCode(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(value));

        var upperValue = value.ToUpperInvariant();

        if (!CourseCodeRegex().IsMatch(upperValue))
            throw new ArgumentException(
                $"Invalid course code format: {value}. Expected format: XX000 (e.g., CS101, MATH1001)",
                nameof(value));

        Value = upperValue;
    }

    [GeneratedRegex(Pattern)]
    private static partial Regex CourseCodeRegex();
}
```

### Application Serviceでのバリデーション流れ

```csharp
public class CreateCourseService : ICreateCourseService
{
    public async Task<string> CreateCourseAsync(CreateCourseCommand command)
    {
        // 1. 値オブジェクト構築（ここで形式バリデーション実行）
        var courseCode = new CourseCode(command.CourseCode); // ← ArgumentException発生の可能性

        // 2. ビジネスルールバリデーション
        var existing = await _courseRepository.GetByCodeAsync(courseCode);
        if (existing != null)
            throw new InvalidOperationException($"Course with code {courseCode} already exists");

        // 3. 集約生成（ここでドメインルールバリデーション実行）
        var course = Course.Create(courseCode, command.Name, command.Credits, command.MaxCapacity);
        // ← ArgumentException発生の可能性

        await _courseRepository.AddAsync(course);
        await _courseRepository.SaveChangesAsync();

        return course.Id.Value;
    }
}
```

### バリデーション階層のまとめ

| 階層 | バリデーション内容 | 実装場所 | 例外型 |
|------|------------------|---------|--------|
| 値オブジェクト | 形式・フォーマット | コンストラクタ内 | `ArgumentException` |
| 集約 | ビジネスルール | ファクトリメソッド・更新メソッド内 | `ArgumentException` |
| Application Service | 重複チェック・存在確認 | Service内 | `InvalidOperationException` |
| データベース | データ整合性制約 | DB制約（UNIQUE等） | `DbUpdateException` |

---

## トランザクション管理

### DbContextによる暗黙的トランザクション
```csharp
// SaveChangesAsync()が自動的にトランザクションを開始・コミット
await _dbContext.SaveChangesAsync(cancellationToken);
```

### 明示的トランザクション（複雑な操作時）
```csharp
public async Task<Result> Handle(ComplexCommand command, CancellationToken cancellationToken)
{
    await using var transaction = await _dbContext.Database
        .BeginTransactionAsync(cancellationToken);

    try
    {
        // 複数の操作
        await _enrollmentRepository.AddAsync(enrollment);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _externalService.NotifyAsync(enrollment.Id);

        await transaction.CommitAsync(cancellationToken);
        return Result.Success();
    }
    catch
    {
        await transaction.RollbackAsync(cancellationToken);
        throw;
    }
}
```

---

## 例外ハンドリング

### カスタム例外
```csharp
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message) { }
}

public class ValidationException : Exception
{
    public IEnumerable<ValidationFailure> Errors { get; }

    public ValidationException(IEnumerable<ValidationFailure> errors)
        : base("Validation failed")
    {
        Errors = errors;
    }
}
```

### グローバル例外ハンドラー（Api層で実装）
```csharp
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            await HandleValidationExceptionAsync(context, ex);
        }
        catch (EnrollmentDomainException ex)
        {
            await HandleDomainExceptionAsync(context, ex);
        }
        catch (NotFoundException ex)
        {
            await HandleNotFoundExceptionAsync(context, ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await HandleUnhandledExceptionAsync(context, ex);
        }
    }

    private static Task HandleValidationExceptionAsync(
        HttpContext context,
        ValidationException exception)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return context.Response.WriteAsJsonAsync(new
        {
            Type = "ValidationError",
            Errors = exception.Errors.Select(e => new
            {
                e.PropertyName,
                e.ErrorMessage
            })
        });
    }
}
```

---

## ベストプラクティス

1. **Command/Query分離**
   - Commandは状態変更のみ、Queryは読み取りのみ
   - Commandの戻り値は最小限（IDやサマリー）
   - Queryは常にAsNoTracking()を使用

2. **バリデーション階層**
   - 入力形式: FluentValidationで検証
   - ビジネスルール: ドメイン層で検証
   - データ整合性: データベース制約で保証

3. **トランザクション原則**
   - 1 Command = 1トランザクション
   - 複数集約の更新は避ける（イベント駆動で分離）
   - 長時間トランザクションは禁止

4. **パフォーマンス最適化**
   - Query側ではInclude()でN+1問題を回避
   - 大量データはページング必須
   - 複雑な集計はDapperや生SQLも検討

---

## テスト戦略

Application層のテスト戦略については、専用ドキュメントを参照してください：

### 📋 [テスト戦略の詳細](testing-strategy.md)

- テストピラミッド（Application層中心の統合テスト戦略）
- インメモリDBを使ったテスト独立性の確保
- CommandService/QueryServiceのテストパターン
- E2Eテストの最小化戦略
- テストデータビルダーパターン
- CI/CDでのテスト実行

### 基本方針の概要

1. **Application層を手厚くテスト**
   - CommandService/QueryServiceに対する包括的なテスト
   - インメモリデータベース（EF Core In-Memory Provider）を使用
   - ドメインロジックとリポジトリの統合テスト

2. **テスト独立性の保証**
   - 各テストメソッドで専用のDbContextインスタンスを生成
   - テスト間でデータを共有しない
   - SetUp/TearDownで明確にコンテキストを管理

3. **E2Eテストは最小限**
   - WebAPI → Application → Domain → DBまでの重要なシナリオのみ
   - 正常系とクリティカルな異常系のみカバー

詳細な実装例、ベストプラクティス、CI/CD設定については [テスト戦略ドキュメント](testing-strategy.md) を参照してください。
