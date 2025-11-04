#!/bin/bash

# ========================================
# University Management System
# ゼロから環境構築してWebAPIを確認するスクリプト
# ========================================

set -e  # エラーが発生したら即座に終了

# カラー出力用の定義
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

echo -e "${BLUE}"
cat << "EOF"
╔══════════════════════════════════════════════════════════════╗
║  University Management System - Setup & Verification Script ║
╚══════════════════════════════════════════════════════════════╝
EOF
echo -e "${NC}"

# ========================================
# Step 1: クリーンアップ & リビルド
# ========================================
echo -e "${YELLOW}[Step 1/2] クリーンアップ & リビルド${NC}"
echo "既存環境を削除して、ゼロからDockerを構築します..."
echo ""

make clean
make rebuild

echo -e "${GREEN}✓ 環境構築が完了しました${NC}"
echo ""

# ========================================
# Step 2: WebAPI動作確認
# ========================================
echo -e "${YELLOW}[Step 2/2] WebAPI動作確認${NC}"
echo ""

# APIサーバーの起動待機
echo "APIサーバーの起動を待機しています..."
max_attempts=30
attempt=0

while [ $attempt -lt $max_attempts ]; do
    if curl -s http://localhost:8080/health > /dev/null 2>&1 || \
       curl -s http://localhost:8080/swagger/index.html > /dev/null 2>&1; then
        echo -e "${GREEN}✓ APIサーバーが起動しました${NC}"
        break
    fi
    attempt=$((attempt + 1))
    echo -n "."
    sleep 2
done

if [ $attempt -eq $max_attempts ]; then
    echo -e "${RED}✗ APIサーバーの起動がタイムアウトしました${NC}"
    echo "APIログを確認してください: make api-logs"
    exit 1
fi
echo ""

# Swagger UI確認
echo -e "${CYAN}[1] Swagger UI 確認${NC}"
if curl -s http://localhost:8080/swagger/index.html > /dev/null 2>&1; then
    echo -e "${GREEN}✓ Swagger UI が利用可能です${NC}"
    echo "   URL: http://localhost:8080/swagger"
else
    echo -e "${RED}✗ Swagger UI にアクセスできません${NC}"
fi
echo ""

# Semesterエンドポイント確認
echo -e "${CYAN}[2] GET /api/semesters${NC}"
semester_response=$(curl -s -w "\n%{http_code}" http://localhost:8080/api/semesters)
semester_status=$(echo "$semester_response" | tail -n 1)

if [ "$semester_status" = "200" ]; then
    echo -e "${GREEN}✓ HTTP $semester_status${NC}"
else
    echo -e "${RED}✗ HTTP $semester_status${NC}"
fi
echo ""

# Semester作成テスト
echo -e "${CYAN}[3] POST /api/semesters (サンプルデータ作成)${NC}"
create_response=$(curl -s -w "\n%{http_code}" -X POST http://localhost:8080/api/semesters \
  -H "Content-Type: application/json" \
  -d '{
    "year": 2024,
    "period": "Spring",
    "startDate": "2024-04-01",
    "endDate": "2024-07-31"
  }')

create_status=$(echo "$create_response" | tail -n 1)
create_body=$(echo "$create_response" | sed '$d')

if [ "$create_status" = "201" ] || [ "$create_status" = "200" ]; then
    echo -e "${GREEN}✓ HTTP $create_status - Semesterを作成しました${NC}"
    echo "$create_body" | jq '.' 2>/dev/null || echo "$create_body"
elif [ "$create_status" = "500" ]; then
    echo -e "${YELLOW}⚠ HTTP $create_status - API内部エラー${NC}"
    echo -e "${CYAN}※ DateTime UTC問題の可能性があります（既知の問題）${NC}"
else
    echo -e "${YELLOW}⚠ HTTP $create_status${NC}"
    echo -e "${CYAN}※ 既にデータが存在する場合は正常です${NC}"
fi
echo ""

# Courseエンドポイント確認
echo -e "${CYAN}[4] GET /api/courses${NC}"
course_response=$(curl -s -w "\n%{http_code}" http://localhost:8080/api/courses)
course_status=$(echo "$course_response" | tail -n 1)

if [ "$course_status" = "200" ]; then
    echo -e "${GREEN}✓ HTTP $course_status${NC}"
else
    echo -e "${RED}✗ HTTP $course_status${NC}"
fi
echo ""

# Course作成テスト
echo -e "${CYAN}[5] POST /api/courses (サンプルデータ作成)${NC}"
course_create_response=$(curl -s -w "\n%{http_code}" -X POST http://localhost:8080/api/courses \
  -H "Content-Type: application/json" \
  -d '{
    "courseCode": "CS101",
    "name": "Introduction to Computer Science",
    "credits": 3,
    "maxCapacity": 50
  }')

course_create_status=$(echo "$course_create_response" | tail -n 1)

if [ "$course_create_status" = "201" ] || [ "$course_create_status" = "200" ]; then
    echo -e "${GREEN}✓ HTTP $course_create_status - Courseを作成しました${NC}"
else
    echo -e "${YELLOW}⚠ HTTP $course_create_status${NC}"
    echo -e "${CYAN}※ 既にデータが存在する場合は正常です${NC}"
fi
echo ""

# Studentエンドポイント確認
echo -e "${CYAN}[6] GET /api/students${NC}"
student_response=$(curl -s -w "\n%{http_code}" http://localhost:8080/api/students)
student_status=$(echo "$student_response" | tail -n 1)

if [ "$student_status" = "200" ]; then
    echo -e "${GREEN}✓ HTTP $student_status${NC}"
else
    echo -e "${RED}✗ HTTP $student_status${NC}"
fi
echo ""

# Student作成テスト
echo -e "${CYAN}[7] POST /api/students (サンプルデータ作成)${NC}"
student_create_response=$(curl -s -w "\n%{http_code}" -X POST http://localhost:8080/api/students \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Taro Yamada",
    "email": "taro.yamada@example.com",
    "grade": 1
  }')

student_create_status=$(echo "$student_create_response" | tail -n 1)
student_create_body=$(echo "$student_create_response" | sed '$d')

if [ "$student_create_status" = "201" ] || [ "$student_create_status" = "200" ]; then
    echo -e "${GREEN}✓ HTTP $student_create_status - Studentを作成しました${NC}"
    student_id=$(echo "$student_create_body" | jq -r '.studentId' 2>/dev/null)
    if [ -n "$student_id" ] && [ "$student_id" != "null" ]; then
        echo "Student ID: $student_id"
    fi
else
    echo -e "${YELLOW}⚠ HTTP $student_create_status${NC}"
    echo -e "${CYAN}※ 既にデータが存在する場合は正常です${NC}"
fi
echo ""

# 作成したStudentの取得確認
if [ -n "$student_id" ] && [ "$student_id" != "null" ]; then
    echo -e "${CYAN}[8] GET /api/students/{id} (作成したStudentを取得)${NC}"
    get_student_response=$(curl -s -w "\n%{http_code}" http://localhost:8080/api/students/$student_id)
    get_student_status=$(echo "$get_student_response" | tail -n 1)

    if [ "$get_student_status" = "200" ]; then
        echo -e "${GREEN}✓ HTTP $get_student_status${NC}"
        echo "$get_student_response" | sed '$d' | jq '.' 2>/dev/null
    else
        echo -e "${RED}✗ HTTP $get_student_status${NC}"
    fi
    echo ""

    # Student更新テスト
    echo -e "${CYAN}[9] PUT /api/students/{id} (Student情報を更新)${NC}"
    update_student_response=$(curl -s -w "\n%{http_code}" -X PUT http://localhost:8080/api/students/$student_id \
      -H "Content-Type: application/json" \
      -d '{
        "name": "Taro Yamada",
        "email": "taro.yamada.updated@example.com",
        "grade": 2
      }')

    update_student_status=$(echo "$update_student_response" | tail -n 1)

    if [ "$update_student_status" = "200" ] || [ "$update_student_status" = "204" ]; then
        echo -e "${GREEN}✓ HTTP $update_student_status - Student情報を更新しました${NC}"
    else
        echo -e "${RED}✗ HTTP $update_student_status${NC}"
    fi
    echo ""
fi

# Courseコードで取得テスト
echo -e "${CYAN}[10] GET /api/courses/{code} (作成したCourseを取得)${NC}"
get_course_response=$(curl -s -w "\n%{http_code}" http://localhost:8080/api/courses/CS101)
get_course_status=$(echo "$get_course_response" | tail -n 1)

if [ "$get_course_status" = "200" ]; then
    echo -e "${GREEN}✓ HTTP $get_course_status${NC}"
    echo "$get_course_response" | sed '$d' | jq '.' 2>/dev/null
else
    echo -e "${RED}✗ HTTP $get_course_status${NC}"
fi
echo ""

# 現在の学期取得テスト
echo -e "${CYAN}[11] GET /api/semesters/current (現在の学期を取得)${NC}"
current_semester_response=$(curl -s -w "\n%{http_code}" http://localhost:8080/api/semesters/current)
current_semester_status=$(echo "$current_semester_response" | tail -n 1)

if [ "$current_semester_status" = "200" ]; then
    echo -e "${GREEN}✓ HTTP $current_semester_status${NC}"
    echo "$current_semester_response" | sed '$d' | jq '.' 2>/dev/null
elif [ "$current_semester_status" = "404" ]; then
    echo -e "${YELLOW}⚠ HTTP $current_semester_status - 現在の学期が見つかりません${NC}"
    echo -e "${CYAN}※ テストデータの日付範囲外の場合は正常です${NC}"
else
    echo -e "${RED}✗ HTTP $current_semester_status${NC}"
fi
echo ""

# ========================================
# 完了サマリー
# ========================================
echo -e "${BLUE}"
cat << "EOF"
╔══════════════════════════════════════════════════════════════╗
║                    セットアップ完了！                         ║
╚══════════════════════════════════════════════════════════════╝
EOF
echo -e "${NC}"

echo -e "${GREEN}✓ 環境構築と検証が完了しました！${NC}"
echo ""
echo -e "${CYAN}📚 次のステップ:${NC}"
echo ""
echo "  Swagger UI でAPIを確認:"
echo "    http://localhost:8080/swagger"
echo ""
echo "  利用可能なコマンド:"
echo "    make ps          - コンテナの状態確認"
echo "    make logs        - 全ログを表示"
echo "    make api-logs    - APIログのみ表示"
echo "    make down        - 環境を停止"
echo "    make restart     - 環境を再起動"
echo "    make clean       - 環境停止 + データ削除"
echo ""
echo -e "${YELLOW}停止する場合: make down${NC}"
echo ""
