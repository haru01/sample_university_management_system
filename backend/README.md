# University Management System - Backend API

C# (.NET 9) + Entity Framework Core + DDD + CQRS パターンによる大学管理システムAPI

## プロジェクト構成

```
backend/
├── src/
│   ├── Shared/                        # 共有カーネル（エンティティ基底クラス等）
│   └── Enrollments/                   # 履修管理コンテキスト
│       ├── Domain/                    # ドメイン層
│       │   ├── CourseAggregate/       # コース集約
│       │   ├── StudentAggregate/      # 学生集約
│       │   └── EnrollmentAggregate/   # 履修集約
│       ├── Application/               # アプリケーション層（CQRS）
│       │   ├── Commands/              # コマンド（書き込み）
│       │   └── Queries/               # クエリ（読み取り）
│       ├── Infrastructure/            # インフラ層
│       │   └── Persistence/           # EF Core設定、リポジトリ、マイグレーション
│       └── Api/                       # API層（コントローラー）
└── tests/
    └── Enrollments.Tests/             # テストプロジェクト（xUnit）
```

## 実装済み機能

### コース管理API

- **POST /api/courses** - コース登録
- **GET /api/courses** - コース一覧取得
- **GET /api/courses/{code}** - コース単件取得

## セットアップ（Docker環境）

### 前提条件

- **Docker Desktop** がインストールされていること
- **Make** がインストールされていること（オプション）

### 環境構築手順

#### 1. データベース接続情報の設定

**重要: セキュリティのため、データベース認証情報は環境変数で管理します。**

```bash
# .env.example をコピーして .env ファイルを作成
cp .env.example .env

# .env ファイルを編集してパスワードを設定
# DATABASE_CONNECTION_STRING=Host=localhost;Port=5432;Database=university_management;Username=postgres;Password=your_secure_password
```

または、環境変数を直接設定:

```bash
# bashの場合
export DATABASE_CONNECTION_STRING="Host=localhost;Port=5432;Database=university_management;Username=postgres;Password=your_password"

# PowerShellの場合
$env:DATABASE_CONNECTION_STRING="Host=localhost;Port=5432;Database=university_management;Username=postgres;Password=your_password"
```

**注意**:
- `.env` ファイルは `.gitignore` に登録済みのため、誤ってコミットされることはありません
- 本番環境では必ず強力なパスワードを使用してください
- デフォルトの `postgres/postgres` は開発環境専用です

#### 2. Docker環境起動

```bash
# Docker環境起動（PostgreSQL + Flyway + API）
make up

# または makeを使わない場合
docker-compose up -d --build

# 3. ブラウザでSwagger UIを開く
make swagger

# または直接ブラウザで開く
open http://localhost:8080/swagger
```

起動後、以下のURLで各サービスにアクセスできます:

- **API**: http://localhost:8080
- **Swagger UI**: http://localhost:8080/swagger
- **PostgreSQL**: localhost:5432

### 動作確認

```bash
# サンプルデータを投入してAPIをテスト
make test

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
make down

# 環境を停止 + データベースをリセット
make clean
```

## 開発コマンド（Make）

```bash
# 環境管理
make up               # Docker環境を起動
make down             # Docker環境を停止
make restart          # Docker環境を再起動
make rebuild          # Docker環境をリビルド + 起動
make clean            # Docker環境を停止 + ボリューム削除（DBリセット）

# ログ・モニタリング
make logs             # 全サービスのログを表示
make api-logs         # APIのログのみ表示
make db-logs          # PostgreSQLのログのみ表示
make migrate-logs     # Flywayマイグレーションのログを表示
make ps               # コンテナの状態を確認

# テスト・開発
make swagger          # Swagger UIを開く
make test             # サンプルデータでAPIをテスト

# ヘルプ
make help             # 全コマンド一覧表示
```

### makeを使わない場合

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

## 開発ガイドライン

### ローカル開発環境のセットアップ

Docker環境以外でローカル開発する場合:

```bash
# 1. 環境変数を設定
export DATABASE_CONNECTION_STRING="Host=localhost;Port=5432;Database=university_management;Username=postgres;Password=your_password"

# 2. データベースを起動（PostgreSQLのみ）
docker-compose up -d postgres

# 3. APIをローカルで実行
cd src/Enrollments/Api
dotnet run

# APIは http://localhost:5000 で起動します
```

### ビルド・テスト

```bash
# ソリューション全体のビルド
dotnet build

# 全テスト実行
dotnet test

# 特定コンテキストのテスト
dotnet test tests/Enrollments.Tests

# カバレッジ収集
dotnet test --collect:"XPlat Code Coverage"
```

### コード品質

```bash
# コードフォーマットチェック
dotnet format --verify-no-changes

# コードフォーマット適用
dotnet format
```

### マイグレーション（Flyway）

```bash
# マイグレーション情報確認
flyway info -configFiles=flyway.conf

# マイグレーション実行
flyway migrate -configFiles=flyway.conf

# マイグレーション検証
flyway validate -configFiles=flyway.conf
```

マイグレーションファイルは `src/Enrollments/Infrastructure/Persistence/Migrations/` に配置します。

## セキュリティ設定

### 環境変数の管理

本プロジェクトでは、機密情報（データベース認証情報など）を環境変数で管理します。

**開発環境**:

```bash
# .envファイルを使用（推奨）
cp .env.example .env
# .envファイルを編集してパスワードを設定
```

**本番環境**:

```bash
# CI/CDパイプラインでsecretから環境変数を注入
# 例: GitHub Actions
env:
  DATABASE_CONNECTION_STRING: ${{ secrets.DATABASE_CONNECTION_STRING }}

# 例: Kubernetes
# ConfigMapやSecretから環境変数を注入
```

### 重要な注意事項

⚠️ **絶対にやってはいけないこと**:

- `appsettings.json` に本番環境のパスワードを記載してコミット
- `.env` ファイルをGitにコミット（`.gitignore`で除外済み）
- デフォルトパスワード `postgres/postgres` を本番環境で使用

✅ **推奨される方法**:

- 環境変数 `DATABASE_CONNECTION_STRING` を使用
- 本番環境では強力なパスワードを使用
- CI/CDツールのsecret管理機能を活用

## 本番環境へのデプロイ

### 環境変数の設定例

**Docker**:

```bash
docker run -e DATABASE_CONNECTION_STRING="Host=db;Port=5432;Database=university_management;Username=app_user;Password=${SECURE_PASSWORD}" your-image
```

**Kubernetes**:

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: db-secret
type: Opaque
stringData:
  connection-string: "Host=db;Port=5432;Database=university_management;Username=app_user;Password=xxx"
---
apiVersion: apps/v1
kind: Deployment
spec:
  template:
    spec:
      containers:
      - name: api
        env:
        - name: DATABASE_CONNECTION_STRING
          valueFrom:
            secretKeyRef:
              name: db-secret
              key: connection-string
```

## ライセンス

MIT
