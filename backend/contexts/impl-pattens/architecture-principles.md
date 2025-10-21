# アーキテクチャ原則

## 境界づけられたコンテキスト（Bounded Context）

各ドメインコンテキストは独立した単位として設計され、明確な境界を持つ。

### コンテキスト分離の原則

```
UniversityManagement/
├── src/
│   ├── Enrollments/        # 履修管理コンテキスト
│   ├── Attendances/         # 出席管理コンテキスト
│   ├── Grading/             # 成績評価コンテキスト
│   └── Shared/              # 共有カーネル
```

### コンテキスト間の統合ルール

1. **直接参照の禁止**
   - コンテキスト間でドメインモデルを直接参照しない
   - 必要な情報はIDで参照

2. **統合方法**
   ```csharp
   // ❌ 悪い例: 他コンテキストのエンティティを直接参照
   public class Enrollment
   {
       public Student Student { get; set; }  // Attendancesコンテキストのエンティティ
   }

   // ✅ 良い例: IDで参照
   public class Enrollment
   {
       public StudentId StudentId { get; private set; }
   }
   ```

3. **データ統合パターン**
   - **Application Service経由**: 複数コンテキストのデータを組み合わせ
   - **ドメインイベント**: 非同期での情報伝達
   - **共有カーネル**: 本当に共有が必要な概念のみ（StudentId等）

---

## レイヤーアーキテクチャ

### 依存関係の方向

```
┌─────────────────┐
│   Api Layer     │  Controllers, Middleware
└────────┬────────┘
         │ depends on
         ▼
┌─────────────────┐
│Application Layer│  Commands, Queries, Services
└────────┬────────┘
         │ depends on
         ▼
┌─────────────────┐
│  Domain Layer   │◄──┐  Entities, Value Objects, Services
└─────────────────┘   │
         ▲             │
         │ depends on  │
         │             │
┌────────┴────────┐   │
│Infrastructure   │───┘  Repositories, DbContext
└─────────────────┘
```

### レイヤーごとの責務

#### 1. Domain層（中心）
**責務**: ビジネスロジックとドメインルールの表現

- ✅ 許可される依存
  - なし（完全に独立）

- ❌ 禁止される依存
  - Application層
  - Infrastructure層
  - Api層
  - 外部ライブラリ（最小限の例外あり）

**配置するもの**:
```
Domain/
├── {Aggregate}Aggregate/
│   ├── {AggregateRoot}.cs      # 集約ルート
│   ├── {Entity}.cs             # エンティティ
│   ├── {ValueObject}.cs        # 値オブジェクト
│   ├── I{Aggregate}Repository.cs  # リポジトリインターフェース
│   └── Events/                 # ドメインイベント
└── Services/                   # ドメインサービス
```

#### 2. Application層（ユースケース）
**責務**: アプリケーションロジックとワークフローの調整

- ✅ 許可される依存
  - Domain層

- ❌ 禁止される依存
  - Api層
  - Infrastructure層（インターフェース経由なら可）

**配置するもの**:
```
Application/
├── Commands/                   # 状態変更操作
│   └── {UseCase}/
│       ├── {UseCase}Command.cs
│       ├── {UseCase}CommandService.cs
│       └── {UseCase}CommandValidator.cs
├── Queries/                    # データ取得操作
│   └── {UseCase}/
│       ├── {UseCase}Query.cs
│       ├── {UseCase}QueryService.cs
│       └── {UseCase}Dto.cs
└── Common/                     # 共通インターフェース
    ├── ICommand.cs
    └── IQuery.cs
```

#### 3. Infrastructure層（技術的詳細）
**責務**: 永続化、外部サービス、技術的実装

- ✅ 許可される依存
  - Domain層（インターフェースの実装）
  - Application層（まれに）

- ❌ 禁止される依存
  - Api層

**配置するもの**:
```
Infrastructure/
├── Persistence/
│   ├── {Context}DbContext.cs   # EF Core DbContext
│   ├── Configurations/         # Entity設定
│   ├── Repositories/           # リポジトリ実装
│   └── Migrations/             # DBマイグレーション
└── External/                   # 外部サービス連携
    └── {Service}Adapter.cs
```

#### 4. Api層（エントリポイント）
**責務**: HTTPリクエスト/レスポンスの処理

- ✅ 許可される依存
  - Application層
  - Domain層（DTOマッピング等で最小限）

- ❌ 禁止される依存
  - Infrastructure層の実装（DIコンテナ設定除く）

**配置するもの**:
```
Api/
├── Controllers/                # エンドポイント
├── Middleware/                 # グローバルハンドラー
├── Models/                     # リクエスト/レスポンスDTO
└── Program.cs                  # DI設定
```

---

## 集約設計ルール

### 1. トランザクション境界 = 集約境界

```csharp
// ✅ 良い例: 1集約のみ変更
public class EnrollStudentCommandService
{
    public async Task<EnrollmentId> ExecuteAsync(EnrollStudentCommand command)
    {
        var enrollment = Enrollment.Create(...);
        await _enrollmentRepository.AddAsync(enrollment);
        await _dbContext.SaveChangesAsync(); // 1トランザクション
        return enrollment.Id;
    }
}

// ❌ 悪い例: 複数集約を同一トランザクションで変更
public async Task ExecuteAsync(UpdateStudentAndEnrollmentCommand command)
{
    var student = await _studentRepository.GetByIdAsync(command.StudentId);
    student.UpdateName(command.NewName);

    var enrollment = await _enrollmentRepository.GetByIdAsync(command.EnrollmentId);
    enrollment.Approve();

    await _dbContext.SaveChangesAsync(); // 複数集約を変更 → 結合度が高まる
}
```

**解決策**: ドメインイベントで分離
```csharp
// Student集約で名前変更
student.UpdateName(command.NewName);
// → StudentNameChangedEvent 発行

// イベント購読サービスで別トランザクションとして処理
public class StudentNameChangedEventService
{
    public async Task HandleAsync(StudentNameChangedEvent @event)
    {
        // 必要なら関連する履修情報を更新
        var enrollments = await _enrollmentRepository
            .GetByStudentIdAsync(@event.StudentId);
        // ...
    }
}
```

### 2. 集約間参照はIDのみ

```csharp
// ✅ 良い例
public class Enrollment : AggregateRoot<EnrollmentId>
{
    public StudentId StudentId { get; private set; }
    public CourseCode CourseCode { get; private set; }
}

// ❌ 悪い例
public class Enrollment : AggregateRoot<EnrollmentId>
{
    public Student Student { get; set; }  // 他集約を直接参照
    public Course Course { get; set; }
}
```

### 3. リポジトリは集約ルートごと

```csharp
// ✅ 良い例
public interface IEnrollmentRepository
{
    Task<Enrollment?> GetByIdAsync(EnrollmentId id);
    Task AddAsync(Enrollment enrollment);
}

// ❌ 悪い例: 集約内のエンティティに個別リポジトリ
public interface IEnrollmentStatusRepository  // EnrollmentStatusは値オブジェクト
{
    Task UpdateStatusAsync(EnrollmentId id, EnrollmentStatus status);
}
```

---

## 依存性逆転の原則（DIP）

### インターフェースによる抽象化

```csharp
// Domain層: インターフェース定義
public interface IEnrollmentRepository
{
    Task<Enrollment?> GetByIdAsync(EnrollmentId id);
    Task AddAsync(Enrollment enrollment);
}

// Infrastructure層: 実装
public class EnrollmentRepository : IEnrollmentRepository
{
    private readonly EnrollmentDbContext _context;
    // ...実装
}

// Application層: インターフェースに依存
public class EnrollStudentCommandService
{
    private readonly IEnrollmentRepository _repository; // 抽象に依存

    public EnrollStudentCommandService(IEnrollmentRepository repository)
    {
        _repository = repository;
    }
}
```

### DIコンテナでの解決（Api層）

```csharp
// Program.cs
builder.Services.AddScoped<IEnrollmentRepository, EnrollmentRepository>();
```

---

## CQRS（Command Query Responsibility Segregation）

### 書き込みと読み取りの分離

```
Command側（書き込み）              Query側（読み取り）
─────────────────────            ─────────────────
集約ルートを経由                   直接DBクエリ
トランザクション必須               AsNoTracking()
正規化されたモデル                 非正規化も許容
ビジネスルール適用                 パフォーマンス重視
```

### 実装例

**Command（状態変更）**:
```csharp
public record EnrollStudentCommand : ICommand<EnrollmentId>
{
    public Guid StudentId { get; init; }
    public string CourseCode { get; init; }
}

// Serviceで集約を通じて変更
public class EnrollStudentCommandService
{
    public async Task<EnrollmentId> ExecuteAsync(EnrollStudentCommand command)
    {
        var enrollment = Enrollment.Create(...);  // ドメインロジック
        await _repository.AddAsync(enrollment);
        await _dbContext.SaveChangesAsync();
        return enrollment.Id;
    }
}
```

**Query（読み取り）**:
```csharp
public record GetEnrollmentsByStudentQuery : IQuery<List<EnrollmentSummaryDto>>
{
    public Guid StudentId { get; init; }
}

// Serviceで直接クエリ
public class GetEnrollmentsByStudentQueryService
{
    public async Task<List<EnrollmentSummaryDto>> ExecuteAsync(
        GetEnrollmentsByStudentQuery query)
    {
        return await _context.Enrollments
            .AsNoTracking()  // 変更追跡不要
            .Where(e => e.StudentId == new StudentId(query.StudentId))
            .Select(e => new EnrollmentSummaryDto { ... })
            .ToListAsync();
    }
}
```

---

## イベント駆動アーキテクチャ

### ドメインイベントの流れ

```
1. 集約がイベント発行
   enrollment.Approve();
   → AddDomainEvent(new EnrollmentApprovedEvent(...))

2. DbContext.SaveChangesAsync()でイベント発行
   → イベントバスで発行

3. イベント購読サービスが処理（別トランザクション）
   → SendEmailService
   → UpdateStatisticsService
```

### 実装例

```csharp
// 1. イベント定義（Domain層）
public record EnrollmentApprovedEvent(
    EnrollmentId EnrollmentId,
    StudentId StudentId,
    CourseCode CourseCode
) : DomainEvent;

// 2. 集約でイベント発行
public void Approve()
{
    Status = EnrollmentStatus.Approved;
    AddDomainEvent(new EnrollmentApprovedEvent(Id, StudentId, CourseCode));
}

// 3. イベント購読サービス（Application層）
public class EnrollmentApprovedEventService
{
    public async Task HandleAsync(EnrollmentApprovedEvent notification)
    {
        // メール送信、統計更新など副作用処理
        await _emailService.SendApprovalNotificationAsync(...);
    }
}
```

---

## ベストプラクティス

1. **依存関係の方向は常に内側（Domain）へ**
   - Domainは外部に依存しない
   - 外部技術の変更がDomainに影響しない

2. **1集約 = 1トランザクション**
   - 複数集約の変更は避ける
   - どうしても必要ならイベント駆動で分離

3. **集約間はIDで疎結合**
   - 他集約のエンティティを直接参照しない
   - 必要な情報はApplication層で結合

4. **CQRS で読み書き最適化**
   - Commandは厳密な整合性
   - Queryは柔軟性とパフォーマンス優先
