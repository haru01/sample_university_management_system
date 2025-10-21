.PHONY: help up down restart logs ps clean rebuild test api-logs db-logs migrate-logs swagger

# ========================================
# Docker環境（推奨）
# ========================================

## 環境の起動・停止

# Docker環境起動（DB + マイグレーション + API）
up:
	@echo "🐳 Starting Docker environment..."
	docker-compose up -d
	@echo "✅ Docker environment started!"
	@echo ""
	@echo "📝 API: http://localhost:8080"
	@echo "📚 Swagger UI: http://localhost:8080/swagger"
	@echo "🗄️  PostgreSQL: localhost:5432"

# Docker環境停止
down:
	@echo "🛑 Stopping Docker environment..."
	docker-compose down
	@echo "✅ Docker environment stopped!"

# Docker環境再起動
restart:
	@echo "🔄 Restarting Docker environment..."
	docker-compose restart
	@echo "✅ Docker environment restarted!"

# Docker環境停止 + ボリューム削除（データベースリセット）
clean:
	@echo "⚠️  Stopping Docker and removing volumes..."
	docker-compose down -v
	@echo "✅ Docker environment and volumes removed!"

# Docker環境リビルド + 起動
rebuild:
	@echo "🔨 Rebuilding and starting Docker environment..."
	docker-compose down
	docker-compose up -d --build
	@echo "✅ Docker environment rebuilt and started!"

## ログとモニタリング

# 全サービスのログ表示（リアルタイム）
logs:
	docker-compose logs -f

# APIのログのみ表示
api-logs:
	docker-compose logs -f api

# PostgreSQLのログのみ表示
db-logs:
	docker-compose logs -f postgres

# Flywayマイグレーションのログ表示
migrate-logs:
	docker-compose logs flyway

# コンテナ状態確認
ps:
	docker-compose ps

## テスト・開発

# Swagger UIを開く
swagger:
	@echo "🌐 Opening Swagger UI..."
	@open http://localhost:8080/swagger || xdg-open http://localhost:8080/swagger || start http://localhost:8080/swagger

# サンプルデータ投入（テスト用）
test:
	@echo "🧪 Creating sample course..."
	@curl -s -X POST http://localhost:8080/api/courses \
	  -H "Content-Type: application/json" \
	  -d '{"courseCode":"CS101","name":"Introduction to Computer Science","credits":3,"maxCapacity":50}' | jq .
	@echo ""
	@echo "📚 Listing all courses..."
	@curl -s http://localhost:8080/api/courses | jq .

# ========================================
# ヘルプ
# ========================================

help:
	@echo "📚 University Management System - Makefile Commands"
	@echo ""
	@echo "🐳 Docker環境（推奨）:"
	@echo "  make up          - Docker環境を起動（DB + マイグレーション + API）"
	@echo "  make down        - Docker環境を停止"
	@echo "  make restart     - Docker環境を再起動"
	@echo "  make rebuild     - Docker環境をリビルド + 起動"
	@echo "  make clean       - Docker環境を停止 + ボリューム削除"
	@echo ""
	@echo "📊 ログとモニタリング:"
	@echo "  make logs        - 全サービスのログを表示"
	@echo "  make api-logs    - APIのログを表示"
	@echo "  make db-logs     - PostgreSQLのログを表示"
	@echo "  make migrate-logs - Flywayマイグレーションのログを表示"
	@echo "  make ps          - コンテナの状態を確認"
	@echo ""
	@echo "🧪 テスト・開発:"
	@echo "  make swagger     - Swagger UIを開く"
	@echo "  make test        - サンプルデータを投入してAPIをテスト"
	@echo ""
	@echo "💡 クイックスタート:"
	@echo "  1. make up       - 環境を起動"
	@echo "  2. make swagger  - Swagger UIでAPIを確認"
	@echo "  3. make test     - サンプルデータでテスト"
	@echo "  4. make down     - 環境を停止"

# デフォルトターゲット
.DEFAULT_GOAL := help
