# University Management System - アーキテクチャコンテキスト

## システム概要

大学の履修管理・出席管理・成績評価を統合管理するシステム。
C# (.NET 9) + Entity Framework Core + DDD + レイヤーアーキテクチャ + CQRS パターンを採用。

## プロジェクト構造
```
UniversityManagement/
├── src/
│   ├── Shared/                         # 共有カーネル（Shared Kernel）
│   │   ├── ValueObjects/
│   │   │   └── StudentId.cs           # 学生ID（両コンテキストで共有）
│   │   ├── Entity.cs
│   │   ├── AggregateRoot.cs
│   │   └── DomainEvent.cs
│   │
│   ├── StudentRegistrations/          # 学生在籍管理コンテキスト
│   │   ├── Domain/
│   │   │   └── StudentAggregate/
│   │   │       ├── Student.cs
│   │   │       └── IStudentRepository.cs
│   │   ├── Application/
│   │   │   ├── Commands/
│   │   │   │   ├── CreateStudent/
│   │   │   │   └── UpdateStudent/
│   │   │   └── Queries/
│   │   │       ├── GetStudent/
│   │   │       └── SelectStudents/
│   │   └── Infrastructure/
│   │       └── Persistence/
│   │           ├── StudentRegistrationsDbContext.cs
│   │           ├── Configurations/
│   │           ├── Repositories/
│   │           └── Migrations/
│   │               └── V1__Create_Students.sql
│   │
│   ├── Enrollments/                   # 履修登録管理コンテキスト
│   │   ├── Domain/
│   │   │   ├── EnrollmentAggregate/
│   │   │   │   ├── Enrollment.cs
│   │   │   │   ├── EnrollmentId.cs
│   │   │   │   ├── EnrollmentStatus.cs
│   │   │   │   └── IEnrollmentRepository.cs
│   │   │   ├── CourseAggregate/
│   │   │   │   ├── Course.cs
│   │   │   │   ├── CourseCode.cs
│   │   │   │   └── ICourseRepository.cs
│   │   │   ├── CourseOfferingAggregate/
│   │   │   │   ├── CourseOffering.cs
│   │   │   │   ├── OfferingId.cs
│   │   │   │   └── ICourseOfferingRepository.cs
│   │   │   └── SemesterAggregate/
│   │   │       ├── Semester.cs
│   │   │       ├── SemesterId.cs
│   │   │       └── ISemesterRepository.cs
│   │   ├── Application/
│   │   │   ├── Commands/
│   │   │   │   ├── EnrollStudent/
│   │   │   │   ├── CancelEnrollment/
│   │   │   │   └── CompleteEnrollment/
│   │   │   ├── Queries/
│   │   │   │   └── GetStudentEnrollments/
│   │   │   └── Services/
│   │   │       └── IStudentServiceClient.cs  # ACL: StudentRegistrations統合
│   │   └── Infrastructure/
│   │       └── Persistence/
│   │           ├── CoursesDbContext.cs
│   │           ├── Configurations/
│   │           ├── Repositories/
│   │           └── Migrations/
│   │               └── V7__Migrate_Students_To_StudentRegistrations.sql
│   │
│   ├── Api/                           # 統合API（全コンテキスト）
│   │   ├── Controllers/
│   │   │   ├── StudentsController.cs
│   │   │   ├── CoursesController.cs
│   │   │   ├── SemestersController.cs
│   │   │   ├── CourseOfferingsController.cs
│   │   │   └── EnrollmentsController.cs
│   │   └── Program.cs
│   │
│   ├── Attendances/                   # 出席管理コンテキスト（未実装）
│   └── Grading/                       # 成績評価コンテキスト（未実装）
│
├── tests/
│   ├── StudentRegistrations.Tests/
│   └── Enrollments.Tests/
│
├── AGENTS.md                          # このファイル
├── REFACTORING_PLAN.md               # リファクタリング計画書
└── contexts/
    ├── CONTEXT_MAP.md                # コンテキストマップ
    └── impl-patterns/                # 実装パターン集
```

## アーキテクチャ設計原則

詳細なアーキテクチャ原則と設計パターンについては、以下のドキュメントを参照してください：

### 📐 [アーキテクチャ原則](contexts/impl-patterns/architecture-principles.md)
- 境界づけられたコンテキスト（Bounded Context）
- レイヤーアーキテクチャと依存関係
- 集約設計ルール
- CQRS パターン

### 🏛️ [Domain層 実装パターン](contexts/impl-patterns/domain-layer-patterns.md)
- エンティティ / 集約ルート
- 値オブジェクト（Value Objects）
- リポジトリインターフェース
- ドメインサービス
- ドメインイベント
- ドメイン例外

### ⚙️ [Application層 実装パターン](contexts/impl-patterns/application-layer-patterns.md)

- Command/Query インターフェース（CQRS）
- CommandHandler / QueryHandler（MediatR）
- トランザクション管理
- 例外ハンドリング

### 🧪 [テスト戦略]

- テストピラミッド（Application層中心の統合テスト戦略）
- インメモリDBを使ったテスト独立性の確保
- CommandHandler/QueryHandlerのテストパターン
- E2Eテストの最小化戦略
- テストデータビルダーパターン
- CI/CDでのテスト実行

### 🗄️ [Infrastructure層 実装パターン](contexts/impl-patterns/infrastructure-layer-patterns.md)

- DbContext（Unit of Work）
- Entity Configuration（Fluent API）
- リポジトリ実装
- 依存性注入の設定
- マイグレーション
- 外部サービス統合

## 開発ガイドライン

### 命名規則
- 集約フォルダ: `{Name}Aggregate/`
- Command/Query: `{動詞}{名詞}Command/Query`
- DTO: `{用途}Dto`
- 値オブジェクト: 単数形（`StudentId`、`CourseCode`）

### エラーハンドリング
```csharp
// ドメイン例外
public class EnrollmentDomainException : Exception
{
    public string Code { get; }
    public EnrollmentDomainException(string code, string message)
        : base(message) => Code = code;
}

// グローバルエラーハンドラー（Api層）
public class GlobalExceptionMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (EnrollmentDomainException ex)
        {
            await HandleDomainExceptionAsync(context, ex);
        }
    }
}
```

### 新規Aggregateの実装チェックリスト（Domain駆動開発）

新しい集約を実装する際は、必ず以下の順序で実装してください：

1. **Domain層** ✅
   - [ ] Aggregateフォルダ作成: `{Name}Aggregate/`
   - [ ] エンティティクラス: `{Name}.cs`
   - [ ] 値オブジェクト（ID等）: `{Name}Id.cs`
   - [ ] リポジトリインターフェース: `I{Name}Repository.cs`
   - [ ] 必要に応じてドメインサービス追加

2. **Infrastructure層** ✅ ← **ここがよく忘れられる**
   - [ ] **Fluent APIでEntity Configuration作成**: `{Name}Configuration.cs`
   - [ ] **DbContext に DbSet<{Name}> を追加**
   - [ ] **マイグレーションファイルに CREATE TABLE を追加**: `Vn__{Name}_Table.sql`
   - [ ] リポジトリ実装: `{Name}Repository.cs`

3. **Application層**
   - [ ] コマンド/クエリの定義と Service 実装
   - [ ] FluentValidation によるバリデーション

4. **API層**
   - [ ] Controller実装
   - [ ] 依存性注入設定を Program.cs に追加

5. **テスト層**
   - [ ] Domain層テスト（必要な場合のみ）
   - [ ] Application層統合テスト（テストコード）
   - [ ] APIエンドポイント テスト（curlで実行）

### テスト方針

詳細なテスト戦略については [テスト戦略ドキュメント](.claude/skills/testing-strategy/SKILL.md) を参照してください。

- **Application層を手厚くテスト**: インメモリDBを使った統合テスト
- **テスト独立性の保証**: 各テストごとに専用のDbContextを生成
- **E2Eテストは最小限**: 重要なシナリオのみカバー
- **テストカテゴリ分類**: Unit/Integration/E2Eで明確に分類

## ビルド・テスト・実行コマンド

### ビルド

```bash
# ソリューション全体のビルド
npm run build
# または: dotnet build

# Release構成でビルド
npm run build:release
# または: dotnet build -c Release

# 特定プロジェクトのビルド
dotnet build src/Enrollments/Api

# 警告をエラーとして扱う
dotnet build /p:TreatWarningsAsErrors=true

# 並列ビルド無効化（トラブルシューティング用）
dotnet build --no-incremental
```

### テスト

```bash
# 全テスト実行
npm run test:unit
# または: dotnet test

# 特定コンテキストのテスト実行
dotnet test tests/Enrollments.Tests

# カバレッジ収集（XML形式）
npm run test:coverage
# または: dotnet test --collect:"XPlat Code Coverage"

# カバレッジ収集 + HTMLレポート生成
npm run coverage:report

# HTMLレポートを開く
npm run coverage:open

# 詳細ログ出力
dotnet test --logger "console;verbosity=detailed"

# 特定テストクラス/メソッド実行
dotnet test --filter "FullyQualifiedName~EnrollmentTests"
dotnet test --filter "FullyQualifiedName=Enrollments.Tests.Domain.EnrollmentTests.Should_Enroll_Student"

# 並列実行無効化（デバッグ用）
dotnet test -- RunConfiguration.MaxCpuCount=1
```

**カバレッジレポートの前提条件**:

```bash
# ReportGeneratorをインストール（初回のみ）
dotnet tool install -g dotnet-reportgenerator-globaltool
```

### リント・コード品質チェック

```bash
# コードフォーマットチェック
npm run format:check
# または: dotnet format --verify-no-changes

# コードフォーマット自動適用
npm run format
# または: dotnet format

# 特定プロジェクトのみフォーマット
dotnet format src/Enrollments/Api

# 静的コード分析（Roslyn Analyzers）
dotnet build /p:EnforceCodeStyleInBuild=true

# セキュリティ脆弱性チェック
dotnet list package --vulnerable
dotnet list package --outdated
```

### マイグレーション（Flyway）

このプロジェクトではデータベーススキーマ管理に **Flyway** を使用します。

マイグレーションはDocker起動時に自動実行されます。

```bash
# マイグレーションログ確認
npm run logs:migrate

# 環境を再起動してマイグレーションを再実行
npm run restart
```

**マイグレーションファイル作成例**:
```bash
# src/Enrollments/Infrastructure/Persistence/Migrations/ に以下のファイルを作成
# V1__Initial_Schema.sql
# V2__Add_Enrollment_Indexes.sql
# V3__Add_Student_Email_Unique.sql
```

新しいマイグレーションファイルを追加した後は `npm run restart` で自動的に適用されます。

詳細は [Infrastructure層パターン - マイグレーション](contexts/infrastructure-layer-patterns.md#マイグレーションflyway) を参照。

### アプリケーション実行

```bash
# 1. Docker環境起動（PostgreSQL + Flyway + API）
npm run up

# または再ビルドして起動
npm run rebuild

# 2. ブラウザでSwagger UIを開く
npm run swagger

# または直接ブラウザで開く
open http://localhost:8080/index.html
```

### パッケージ管理

```bash
# NuGetパッケージ復元
npm run restore
# または: dotnet restore

# パッケージ追加
dotnet add src/Enrollments/Api package Swashbuckle.AspNetCore

# パッケージ削除
dotnet remove src/Enrollments/Api package PackageName

# 全プロジェクトのパッケージ一覧
dotnet list package
```

### CI/CD向けコマンド例

```bash
# クリーンビルド + テスト + カバレッジ（npm scripts使用）
npm run restore && \
npm run build:release && \
npm run test:coverage && \
npm run format:check

# 全てを一括実行（開発時チェック）
npm run restore && \
npm run build && \
npm run format:check && \
npm run test:unit

# または dotnet コマンドで直接実行
dotnet clean && \
dotnet build -c Release /p:TreatWarningsAsErrors=true && \
dotnet test --no-build -c Release --collect:"XPlat Code Coverage" && \
dotnet format --verify-no-changes
```

## 注意事項

1. **トランザクション境界**
   - EntityFrameworkのDbContextが自動的にトランザクションを管理
   - SaveChangesAsync()呼び出し時に全ての変更が1トランザクションで実行
   - 複数集約の更新は避ける

2. **パフォーマンス**
   - Query側では生SQLやストアドプロシージャも許容
   - N+1問題に注意（Include使用）
   - AsNoTracking()を活用してRead専用クエリを最適化

3. **セキュリティ**
   - 全APIエンドポイントに認証・認可
   - 入力値は必ずバリデーション

---

## コンテキストマップ

このシステムは、DDD の Bounded Context パターンに基づいて、複数の独立したコンテキストに分割されています。

### StudentRegistrations ←→ Enrollments

**関係性:** Customer-Supplier（Enrollments が Customer、StudentRegistrations が Supplier）

**統合方式:** ACL (Anti-Corruption Layer) - HTTP通信

- **Enrollments** は **StudentRegistrations** の公開APIを経由して学生情報を参照
- **StudentId** は Shared Kernel として両コンテキストで共有
- 直接的なデータベーススキーマ結合は避け、アプリケーションレベルで整合性を保証

**データフロー:**
```
Enrollments.EnrollStudentCommandHandler
  → IStudentServiceClient (ACL Interface)
    → StudentServiceClient (ACL Implementation, HTTP)
      → HTTP GET /api/students/{id}
        → StudentRegistrations API
          → StudentRepository
            → student_registrations.students table
```

**Shared Kernel:**
- `StudentId` 値オブジェクト (Shared/ValueObjects/)

**データベーススキーマ分離:**
- **student_registrations** スキーマ: `students` テーブル
- **courses** スキーマ: `courses`, `semesters`, `course_offerings`, `enrollments` テーブル
  - `enrollments.student_id` は `student_registrations.students.id` を参照（アプリケーションレベルのみ）
  - データベース外部キー制約なし（コンテキスト独立性を維持）

詳細は [contexts/CONTEXT_MAP.md](contexts/CONTEXT_MAP.md) を参照してください。

---

## AIエージェントへのガイダンス

この AGENTS.md とcontexts配下のドキュメントにより、AIエージェントは：

- ✅ プロジェクトの全体構造を理解
- ✅ 各層の責務と実装パターンを把握
- ✅ 命名規則やコーディング標準に従う
- ✅ 具体的な実装例を参考にコード生成
- ✅ DDD、CQRS、レイヤーアーキテクチャの原則を遵守
