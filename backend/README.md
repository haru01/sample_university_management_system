# University Management System - Backend API

C# (.NET 9) + Entity Framework Core + DDD + CQRS パターンによる大学管理システムAPI

## プロジェクト構成

```
backend/
├── src/
│   ├── Shared/                              # 共有カーネル
│   │   └── ValueObjects/
│   │       └── StudentId.cs                 # 学生ID（コンテキスト間で共有）
│   │
│   ├── StudentRegistrations/                # 学生在籍管理コンテキスト
│   │   ├── Domain/
│   │   │   └── StudentAggregate/
│   │   ├── Application/
│   │   │   ├── Commands/
│   │   │   └── Queries/
│   │   └── Infrastructure/
│   │       └── Persistence/
│   │           └── Migrations/              # Flywayマイグレーション
│   │
│   ├── Enrollments/                         # 履修登録管理コンテキスト
│   │   ├── Domain/
│   │   │   ├── CourseAggregate/
│   │   │   ├── SemesterAggregate/
│   │   │   ├── CourseOfferingAggregate/
│   │   │   └── EnrollmentAggregate/
│   │   ├── Application/
│   │   │   ├── Commands/
│   │   │   ├── Queries/
│   │   │   └── Services/                    # ACL (StudentRegistrations統合)
│   │   └── Infrastructure/
│   │       └── Persistence/
│   │           └── Migrations/              # Flywayマイグレーション
│   │
│   └── Api/                                 # 統合API (全コンテキスト)
│       ├── Controllers/
│       │   ├── StudentsController.cs
│       │   ├── CoursesController.cs
│       │   ├── SemestersController.cs
│       │   ├── CourseOfferingsController.cs
│       │   └── EnrollmentsController.cs
│       └── Program.cs
│
└── tests/
    ├── StudentRegistrations.Tests/
    └── Enrollments.Tests/
```

## クイックスタート

### 前提条件

- **Docker Desktop** がインストールされていること
- **Node.js** (v18以上) がインストールされていること

### 起動手順

```bash
# 1. Docker環境起動（PostgreSQL + Flyway + API）
npm run up

# または docker-compose を直接使用
docker-compose up -d

# 2. ブラウザでSwagger UIを開く
npm run swagger
# または: http://localhost:8080/index.html を直接開く
```

起動後、以下のURLで各サービスにアクセスできます:

- **API**: http://localhost:8080
- **Swagger UI**: http://localhost:8080/index.html
- **PostgreSQL**: localhost:5432

### 動作確認

```bash
# サンプルデータを投入してAPIをテスト
npm run sample

# または手動でテスト
curl -X POST http://localhost:8080/api/courses \
  -H "Content-Type: application/json" \
  -d '{"courseCode":"CS101","name":"Introduction to Computer Science","credits":3,"maxCapacity":50}'

# コース一覧取得
curl http://localhost:8080/api/courses
```

### 環境の停止

```bash
# 環境を停止
npm run down

# 環境を停止 + データベースをリセット
npm run clean
```

## 開発コマンド

### 環境管理

```bash
npm run up            # Docker環境を起動
npm run down          # Docker環境を停止
npm run restart       # Docker環境を再起動
npm run rebuild       # Docker環境をリビルド + 起動
npm run clean         # Docker環境を停止 + ボリューム削除（DBリセット）
npm run ps            # コンテナの状態を確認
```

### ログ・モニタリング

```bash
npm run logs          # 全サービスのログを表示
npm run logs:api      # APIのログのみ表示
npm run logs:db       # PostgreSQLのログのみ表示
npm run logs:migrate  # Flywayマイグレーションのログを表示
```

### テスト・開発

```bash
npm run swagger       # Swagger UIを開く
npm run sample        # サンプルデータでAPIをテスト
```

### docker-composeを直接使用する場合

```bash
# 環境起動
docker-compose up -d --build

# ログ確認
docker-compose logs -f api

# 環境停止
docker-compose down

# 環境停止 + データ削除
docker-compose down -v
```

## API使用例

### コース登録

```bash
curl -X POST http://localhost:8080/api/courses \
  -H "Content-Type: application/json" \
  -d '{
    "courseCode": "CS101",
    "name": "Introduction to Computer Science",
    "credits": 3,
    "maxCapacity": 50
  }'
```

### コース一覧取得

```bash
curl http://localhost:8080/api/courses
```

### コース単件取得

```bash
curl http://localhost:8080/api/courses/CS101
```

## アーキテクチャドキュメント

詳細な設計パターンと実装ガイドラインは以下を参照：

- [AGENTS.md](AGENTS.md) - プロジェクト全体概要・コマンドリファレンス
- [contexts/impl-patterns/architecture-principles.md](contexts/impl-patterns/architecture-principles.md) - アーキテクチャ原則
- [contexts/impl-patterns/domain-layer-patterns.md](contexts/impl-patterns/domain-layer-patterns.md) - Domain層パターン
- [contexts/impl-patterns/application-layer-patterns.md](contexts/impl-patterns/application-layer-patterns.md) - Application層パターン
- [contexts/impl-patterns/infrastructure-layer-patterns.md](contexts/impl-patterns/infrastructure-layer-patterns.md) - Infrastructure層パターン
- [contexts/impl-patterns/testing-strategy.md](contexts/impl-patterns/testing-strategy.md) - テスト戦略

## 技術スタック

- **.NET 9** - フレームワーク
- **ASP.NET Core** - Web API
- **Entity Framework Core 9** - ORM
- **Npgsql** - PostgreSQLプロバイダー
- **Swashbuckle** - Swagger/OpenAPI
- **Flyway** - データベースマイグレーション
- **xUnit** - テストフレームワーク
- **PostgreSQL 16** - データベース

## 開発者向け情報

### データベースマイグレーション

マイグレーションはFlywayを使用してDocker起動時に自動実行されます。

- マイグレーションファイル: `src/Enrollments/Infrastructure/Persistence/Migrations/`
- ログ確認: `npm run logs:migrate`

### ビルド・テスト

```bash
# パッケージ復元
npm run restore

# ソリューション全体のビルド
npm run build

# Release構成でビルド
npm run build:release

# 全テスト実行
npm run test:unit

# 特定コンテキストのテスト
dotnet test tests/Enrollments.Tests

# カバレッジ収集
npm run test:coverage

# カバレッジレポート生成
npm run coverage:report

# レポートを開く
npm run coverage:open
```

### コード品質

```bash
# コードフォーマットチェック
npm run format:check

# コードフォーマット適用
npm run format
```

## ライセンス

MIT
