# University Management System - アーキテクチャコンテキスト

## システム概要
大学の履修管理・出席管理・成績評価を統合管理するシステム。
C# (.NET 8) + Entity Framework Core + DDD + レイヤーアーキテクチャ + CQRS パターンを採用。

## プロジェクト構造
```
UniversityManagement/
├── src/
│   ├── Enrollments/                    # 履修管理コンテキスト
│   │   ├── Domain/
│   │   │   ├── EnrollmentAggregate/
│   │   │   │   ├── Enrollment.cs
│   │   │   │   ├── EnrollmentId.cs
│   │   │   │   ├── EnrollmentStatus.cs
│   │   │   │   ├── IEnrollmentRepository.cs
│   │   │   │   └── Events/
│   │   │   ├── StudentAggregate/
│   │   │   │   ├── Student.cs
│   │   │   │   ├── StudentId.cs
│   │   │   │   └── IStudentRepository.cs
│   │   │   ├── CourseAggregate/
│   │   │   │   ├── Course.cs
│   │   │   │   ├── CourseCode.cs
│   │   │   │   └── ICourseRepository.cs
│   │   │   └── Services/
│   │   ├── Application/
│   │   │   ├── Commands/
│   │   │   │   └── EnrollStudent/
│   │   │   │       ├── EnrollStudentCommand.cs
│   │   │   │       ├── EnrollStudentCommandService.cs
│   │   │   │       └── EnrollStudentCommandValidator.cs
│   │   │   ├── Queries/
│   │   │   │   └── GetEnrollmentsByStudent/
│   │   │   │       ├── GetEnrollmentsByStudentQuery.cs
│   │   │   │       ├── GetEnrollmentsByStudentQueryService.cs
│   │   │   │       └── EnrollmentSummaryDto.cs
│   │   │   └── Common/
│   │   ├── Infrastructure/
│   │   │   ├── Persistence/
│   │   │   │   ├── EnrollmentDbContext.cs
│   │   │   │   └── Repositories/
│   │   │   └── External/
│   │   └── Api/
│   │       ├── Controllers/
│   │       └── Models/
│   │
│   ├── Attendances/                    # 出席管理コンテキスト
│   ├── Grading/                        # 成績評価コンテキスト
│   └── Shared/                         # 共有カーネル
│       ├── Entity.cs
│       ├── ValueObject.cs
│       └── DomainEvent.cs
│
├── tests/
│   ├── Enrollments.Tests/
│   ├── Attendances.Tests/
│   └── Grading.Tests/
│
├── Claude.md                           # このファイル
└── contexts/
    └── *.md                           # 各ドメインの詳細知識
```

## アーキテクチャ設計原則

詳細なアーキテクチャ原則と設計パターンについては、以下のドキュメントを参照してください：

### 📐 [アーキテクチャ原則](contexts/architecture-principles.md)
- 境界づけられたコンテキスト（Bounded Context）
- レイヤーアーキテクチャと依存関係
- 集約設計ルール
- CQRS パターン
- イベント駆動アーキテクチャ

### 🏛️ [Domain層 実装パターン](contexts/domain-layer-patterns.md)
- エンティティ / 集約ルート
- 値オブジェクト（Value Objects）
- リポジトリインターフェース
- ドメインサービス
- ドメインイベント
- ドメイン例外

### ⚙️ [Application層 実装パターン](contexts/impl-pattens/application-layer-patterns.md)

- Command/Query インターフェース（CQRS）
- CommandService / QueryService
- FluentValidation によるバリデーション
- トランザクション管理
- 例外ハンドリング

### 🧪 [テスト戦略](contexts/impl-pattens/testing-strategy.md)

- テストピラミッド（Application層中心の統合テスト戦略）
- インメモリDBを使ったテスト独立性の確保
- CommandService/QueryServiceのテストパターン
- E2Eテストの最小化戦略
- テストデータビルダーパターン
- CI/CDでのテスト実行

### 🗄️ [Infrastructure層 実装パターン](contexts/impl-pattens/infrastructure-layer-patterns.md)

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

詳細なテスト戦略については [テスト戦略ドキュメント](contexts/impl-pattens/testing-strategy.md) を参照してください。

- **Application層を手厚くテスト**: インメモリDBを使った統合テスト
- **テスト独立性の保証**: 各テストごとに専用のDbContextを生成
- **E2Eテストは最小限**: 重要なシナリオのみカバー
- **テストカテゴリ分類**: Unit/Integration/E2Eで明確に分類

## ビルド・テスト・実行コマンド

### ビルド

```bash
# ソリューション全体のビルド
dotnet build

# Release構成でビルド
dotnet build -c Release

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
dotnet test

# 特定コンテキストのテスト実行
dotnet test tests/Enrollments.Tests

# カバレッジ収集（coverlet使用）
dotnet test --collect:"XPlat Code Coverage"

# 詳細ログ出力
dotnet test --logger "console;verbosity=detailed"

# 特定テストクラス/メソッド実行
dotnet test --filter "FullyQualifiedName~EnrollmentTests"
dotnet test --filter "FullyQualifiedName=Enrollments.Tests.Domain.EnrollmentTests.Should_Enroll_Student"

# 並列実行無効化（デバッグ用）
dotnet test -- RunConfiguration.MaxCpuCount=1
```

### リント・コード品質チェック

```bash
# コードフォーマットチェック（.NET 6+）
dotnet format --verify-no-changes

# コードフォーマット自動適用
dotnet format

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

```bash
# マイグレーション情報確認
flyway info -configFiles=flyway.conf

# マイグレーション実行
flyway migrate -configFiles=flyway.conf

# マイグレーション検証
flyway validate -configFiles=flyway.conf

# マイグレーション履歴クリア（開発環境のみ）
flyway clean -configFiles=flyway.conf

# 最新マイグレーションのロールバック（Flywayコマーシャル版のみ）
flyway undo -configFiles=flyway.conf
```

**マイグレーションファイル作成例**:
```bash
# src/Enrollments/Infrastructure/Persistence/Migrations/ に以下のファイルを作成
# V1__Initial_Schema.sql
# V2__Add_Enrollment_Indexes.sql
# V3__Add_Student_Email_Unique.sql
```

詳細は [Infrastructure層パターン - マイグレーション](contexts/infrastructure-layer-patterns.md#マイグレーションflyway) を参照。

### アプリケーション実行

```bash
# 1. 際ビルドしてDocker環境起動（PostgreSQL + Flyway + API）
make rebuild

# または makeを使わない場合
docker-compose up -d --build

# 2. ブラウザでSwagger UIを開く
make swagger

# または直接ブラウザで開く
open http://localhost:8080/swagger
```

### パッケージ管理

```bash
# NuGetパッケージ復元
dotnet restore

# パッケージ追加
dotnet add src/Enrollments/Api package Swashbuckle.AspNetCore

# パッケージ削除
dotnet remove src/Enrollments/Api package PackageName

# 全プロジェクトのパッケージ一覧
dotnet list package
```

### CI/CD向けコマンド例

```bash
# クリーンビルド + テスト + カバレッジ
dotnet clean && \
dotnet build -c Release /p:TreatWarningsAsErrors=true && \
dotnet test --no-build -c Release --collect:"XPlat Code Coverage" && \
dotnet format --verify-no-changes

# 全てを一括実行（開発時チェック）
dotnet restore && \
dotnet build && \
dotnet format --verify-no-changes && \
dotnet test --logger "console;verbosity=normal"
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

## AIエージェントへのガイダンス

この Claude.md とcontexts配下のドキュメントにより、AIエージェントは：

- ✅ プロジェクトの全体構造を理解
- ✅ 各層の責務と実装パターンを把握
- ✅ 命名規則やコーディング標準に従う
- ✅ 具体的な実装例を参考にコード生成
- ✅ DDD、CQRS、レイヤーアーキテクチャの原則を遵守

### ドキュメント構成

```text
backend/
├── Agent.md                                   # このファイル（全体概要）
└── contexts/
    └── impl-pattens/                         # 詳細パターン集
        ├── architecture-principles.md         # アーキテクチャ原則
        ├── domain-layer-patterns.md          # Domain層パターン
        ├── application-layer-patterns.md     # Application層パターン
        ├── infrastructure-layer-patterns.md  # Infrastructure層パターン
        └── testing-strategy.md               # テスト戦略
```
