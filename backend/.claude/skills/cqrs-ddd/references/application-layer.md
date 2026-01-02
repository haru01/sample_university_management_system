# Application層 実装パターン（CQRS + MediatR）

## CQRS 基本パターン

Command（書き込み）とQuery（読み取り）を明確に分離し、**MediatR**を使用してハンドラーへの自動ディスパッチを実現します。

```csharp
// Command: 状態を変更する操作
public record CreateCourseCommand : IRequest<string>
{
    public required string CourseCode { get; init; }
    public required string Name { get; init; }
    public required int Credits { get; init; }
    public required int MaxCapacity { get; init; }
}

// Query: データを取得する操作
public record GetCoursesQuery : IRequest<List<CourseDto>>
{
    // フィルタ条件などがあれば定義
}

// CommandHandler: Commandを処理
public class CreateCourseCommandHandler : IRequestHandler<CreateCourseCommand, string>
{
    public Task<string> Handle(CreateCourseCommand request, CancellationToken cancellationToken)
    {
        // 実装
    }
}

// QueryHandler: Queryを処理
public class GetCoursesQueryHandler : IRequestHandler<GetCoursesQuery, List<CourseDto>>
{
    public Task<List<CourseDto>> Handle(GetCoursesQuery request, CancellationToken cancellationToken)
    {
        // 実装
    }
}
```

**設計方針:**

- **MediatR**を使用してCommandとQueryを型安全に自動ディスパッチ
- Command/Queryは`IRequest<TResponse>`を実装
- 各Handlerは`IRequestHandler<TRequest, TResponse>`を実装
- コントローラはMediatorを通じてHandlerを呼び出す（Handlerの具体実装を知る必要がない）
- 命名規則:
  - Handler: `{Command/Query}Handler` (例: `CreateCourseCommandHandler`, `SelectCoursesQueryHandler`)
  - Query:
    - 単一エンティティ取得: `Get*Query` (例: `GetCourseQuery`, `GetCourseOfferingQuery`)
    - 複数エンティティ取得: `Select*Query` (例: `SelectCoursesQuery`, `SelectCourseOfferingsBySemesterQuery`)

---

## Command パターン

### Command定義

```csharp
public record EnrollStudentCommand : IRequest<EnrollmentId>
{
    public required Guid StudentId { get; init; }
    public required string CourseCode { get; init; }
    public required int SemesterYear { get; init; }
    public required string SemesterPeriod { get; init; }
}
```

### CommandHandler実装

```csharp
public class EnrollStudentCommandHandler : IRequestHandler<EnrollStudentCommand, EnrollmentId>
{
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IStudentRepository _studentRepository;
    private readonly ICourseRepository _courseRepository;
    private readonly EnrollmentDomainService _domainService;

    public EnrollStudentCommandHandler(
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

    public async Task<EnrollmentId> Handle(
        EnrollStudentCommand request,
        CancellationToken cancellationToken)
    {
        // 1. 値オブジェクトの構築
        var studentId = new StudentId(request.StudentId);
        var courseCode = new CourseCode(request.CourseCode);
        var semester = new Semester(
            request.SemesterYear,
            Enum.Parse<SemesterPeriod>(request.SemesterPeriod));

        // 2. 必要なエンティティの取得
        var student = await _studentRepository.GetByIdAsync(studentId, cancellationToken)
            ?? throw new NotFoundException($"Student {studentId} not found");

        var course = await _courseRepository.GetByCodeAsync(courseCode, cancellationToken)
            ?? throw new NotFoundException($"Course {courseCode} not found");

        var existingEnrollments = await _enrollmentRepository
            .GetByStudentIdAsync(studentId, cancellationToken);

        // 3. ビジネスルールの検証（ドメインサービス）
        if (!_domainService.CanEnroll(student, course, existingEnrollments))
            throw new EnrollmentDomainException(
                "ENROLLMENT_NOT_ALLOWED",
                "履修条件を満たしていません");

        // 4. 集約の生成
        var enrollment = Enrollment.Create(studentId, courseCode, semester);

        // 5. リポジトリへの永続化
        await _enrollmentRepository.AddAsync(enrollment, cancellationToken);

        // 6. Unit of Workパターンでトランザクションコミット
        await _enrollmentRepository.SaveChangesAsync(cancellationToken);

        return enrollment.Id;
    }
}
```

### コントローラからの呼び出し

```csharp
[ApiController]
[Route("api/enrollments")]
public class EnrollmentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public EnrollmentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> EnrollStudent(
        EnrollStudentCommand command,
        CancellationToken cancellationToken)
    {
        var enrollmentId = await _mediator.Send(command, cancellationToken);
        return Ok(new { EnrollmentId = enrollmentId.Value });
    }
}
```

### CommandHandlerのパターン

1. **入力の変換**: プリミティブ型 → 値オブジェクト
2. **エンティティ取得**: リポジトリから必要な集約を取得
3. **ビジネスロジック実行**: ドメインサービスや集約メソッド呼び出し
4. **永続化**: リポジトリのSaveChangesAsync()でUnit of Work完結
5. **結果返却**: 生成されたIDやサマリーを返す
6. **CancellationToken**: 全ての非同期メソッドでCancellationTokenを伝播

---

## Query パターン

### Query定義

```csharp
public record GetEnrollmentsByStudentQuery : IRequest<List<EnrollmentSummaryDto>>
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

### QueryHandler実装

```csharp
public class GetEnrollmentsByStudentQueryHandler
    : IRequestHandler<GetEnrollmentsByStudentQuery, List<EnrollmentSummaryDto>>
{
    private readonly EnrollmentDbContext _context;

    public GetEnrollmentsByStudentQueryHandler(EnrollmentDbContext context)
    {
        _context = context;
    }

    public async Task<List<EnrollmentSummaryDto>> Handle(
        GetEnrollmentsByStudentQuery request,
        CancellationToken cancellationToken)
    {
        var studentId = new StudentId(request.StudentId);

        var queryable = _context.Enrollments
            .AsNoTracking() // 読み取り専用のため変更追跡を無効化
            .Include(e => e.Course) // N+1問題回避
            .Where(e => e.StudentId == studentId);

        // オプショナルフィルタ
        if (!string.IsNullOrEmpty(request.Status))
        {
            queryable = queryable.Where(e => e.Status.Value == request.Status);
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
            .ToListAsync(cancellationToken);
    }
}
```

### コントローラからの呼び出し

```csharp
[ApiController]
[Route("api/enrollments")]
public class EnrollmentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public EnrollmentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("student/{studentId}")]
    public async Task<IActionResult> GetEnrollmentsByStudent(
        Guid studentId,
        [FromQuery] string? status,
        CancellationToken cancellationToken)
    {
        var query = new GetEnrollmentsByStudentQuery
        {
            StudentId = studentId,
            Status = status
        };
        var enrollments = await _mediator.Send(query, cancellationToken);
        return Ok(enrollments);
    }
}
```

### QueryHandlerのパターン

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

### CommandHandlerでのバリデーション流れ

```csharp
public class CreateCourseCommandHandler : IRequestHandler<CreateCourseCommand, string>
{
    private readonly ICourseRepository _courseRepository;

    public CreateCourseCommandHandler(ICourseRepository courseRepository)
    {
        _courseRepository = courseRepository;
    }

    public async Task<string> Handle(CreateCourseCommand request, CancellationToken cancellationToken)
    {
        // 1. 値オブジェクト構築（ここで形式バリデーション実行）
        var courseCode = new CourseCode(request.CourseCode); // ← ArgumentException発生の可能性

        // 2. ビジネスルールバリデーション
        var existing = await _courseRepository.GetByCodeAsync(courseCode, cancellationToken);
        if (existing != null)
            throw new InvalidOperationException($"Course with code {courseCode} already exists");

        // 3. 集約生成（ここでドメインルールバリデーション実行）
        var course = Course.Create(courseCode, request.Name, request.Credits, request.MaxCapacity);
        // ← ArgumentException発生の可能性

        await _courseRepository.AddAsync(course, cancellationToken);
        await _courseRepository.SaveChangesAsync(cancellationToken);

        return course.Id.Value;
    }
}
```

### バリデーション階層のまとめ

| 階層 | バリデーション内容 | 実装場所 | 例外型 |
| ---- | ---------------- | ------- | ------ |
| 値オブジェクト | 形式・フォーマット | コンストラクタ内 | `ArgumentException` |
| 集約 | ビジネスルール | ファクトリメソッド・更新メソッド内 | `ArgumentException` |
| CommandHandler | 重複チェック・存在確認 | Handler内 | `InvalidOperationException` |
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
public class ComplexCommandHandler : IRequestHandler<ComplexCommand, Result>
{
    private readonly EnrollmentDbContext _dbContext;
    private readonly IEnrollmentRepository _enrollmentRepository;
    private readonly IExternalService _externalService;

    public ComplexCommandHandler(
        EnrollmentDbContext dbContext,
        IEnrollmentRepository enrollmentRepository,
        IExternalService externalService)
    {
        _dbContext = dbContext;
        _enrollmentRepository = enrollmentRepository;
        _externalService = externalService;
    }

    public async Task<Result> Handle(ComplexCommand request, CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database
            .BeginTransactionAsync(cancellationToken);

        try
        {
            // 複数の操作
            await _enrollmentRepository.AddAsync(enrollment, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            await _externalService.NotifyAsync(enrollment.Id, cancellationToken);

            await transaction.CommitAsync(cancellationToken);
            return Result.Success();
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
```

### MediatRパイプライン Behavior（トランザクション自動化）

複数のCommandHandlerで共通するトランザクション処理をPipeline Behaviorとして実装することで、重複コードを削減できます。

```csharp
public class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly EnrollmentDbContext _dbContext;
    private readonly ILogger<TransactionBehavior<TRequest, TResponse>> _logger;

    public TransactionBehavior(
        EnrollmentDbContext dbContext,
        ILogger<TransactionBehavior<TRequest, TResponse>> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        // Queryの場合はトランザクション不要
        if (request is IQuery<TResponse>)
        {
            return await next();
        }

        // Commandの場合のみトランザクション開始
        _logger.LogInformation("Starting transaction for {CommandName}", typeof(TRequest).Name);

        await using var transaction = await _dbContext.Database
            .BeginTransactionAsync(cancellationToken);

        try
        {
            var response = await next();
            await transaction.CommitAsync(cancellationToken);
            _logger.LogInformation("Committed transaction for {CommandName}", typeof(TRequest).Name);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Rolling back transaction for {CommandName}", typeof(TRequest).Name);
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}

// マーカーインターフェース（Query識別用）
public interface IQuery<out TResponse> : IRequest<TResponse> { }

// 使用例
public record GetCoursesQuery : IQuery<List<CourseDto>> { }
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
   - QueryにはIQuery<T>マーカーインターフェースを使用（トランザクション制御の判定に利用）

2. **MediatR活用**
   - コントローラはMediatorのみに依存（具体的なHandlerを知らない）
   - Pipeline Behaviorで横断的関心事を実装（バリデーション、トランザクション、ログ等）
   - Handlerは単一責任原則に従う（1 Handler = 1ユースケース）
   - Handlerの命名は`{Command/Query名}Handler`とする

3. **バリデーション階層**
   - 入力形式: 値オブジェクトのコンストラクタで検証
   - ビジネスルール: ドメイン層（集約、ドメインサービス）で検証
   - データ整合性: データベース制約で保証
   - 複雑なバリデーションはPipeline Behaviorで実装可能

4. **トランザクション原則**
   - 1 Command = 1トランザクション
   - Pipeline Behaviorで自動トランザクション管理を推奨
   - 複数集約の更新は避ける（イベント駆動で分離）
   - 長時間トランザクションは禁止

5. **パフォーマンス最適化**
   - Query側ではInclude()でN+1問題を回避
   - 大量データはページング必須
   - 複雑な集計はDapperや生SQLも検討
   - CancellationTokenを全ての非同期メソッドで伝播

6. **依存性注入**
   - MediatR自動登録: `services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()))`
   - Pipeline Behaviorの登録順序に注意（バリデーション → トランザクション → ログ）
   - Handlerはスコープライフタイムで登録される

---

## テスト戦略

Application層のテスト戦略については、専用スキルを参照してください：

### 📋 [testing-strategy](../testing-strategy/SKILL.md)

- テストピラミッド（Application層中心の統合テスト戦略）
- インメモリDBを使ったテスト独立性の確保
- CommandHandler/QueryHandlerのテストパターン
- E2Eテストの最小化戦略
- テストデータビルダーパターン
- CI/CDでのテスト実行
