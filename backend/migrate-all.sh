#!/bin/bash

# 全コンテキストのマイグレーションをまとめて実行

set -e  # エラーで停止

echo "🚀 Starting database migrations..."

# Enrollments コンテキスト
echo "📚 Migrating Enrollments context..."
flyway migrate -configFiles=flyway.conf

# 将来的に他のコンテキストを追加
# echo "📅 Migrating Attendances context..."
# flyway migrate -configFiles=flyway-attendances.conf

# echo "📊 Migrating Grading context..."
# flyway migrate -configFiles=flyway-grading.conf

echo "✅ All migrations completed successfully!"
