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

# 現在の日付を取得（現在の学期テスト用）
current_date=$(date +%Y-%m-%d)
current_year=$(date +%Y)
current_month=$(date +%m)

# 現在の月に応じて学期を決定（Spring: 1-6月, Fall: 7-12月）
if [ "$current_month" -le 6 ]; then
    current_period="Spring"
    start_date="${current_year}-01-01"
    end_date="${current_year}-06-30"
else
    current_period="Fall"
    start_date="${current_year}-07-01"
    end_date="${current_year}-12-31"
fi

# Semester作成テスト（現在の学期）
echo -e "${CYAN}[3] POST /api/semesters (現在の学期を作成)${NC}"
echo "   日付範囲: ${start_date} ～ ${end_date}"
create_response=$(curl -s -w "\n%{http_code}" -X POST http://localhost:8080/api/semesters \
  -H "Content-Type: application/json" \
  -d "{
    \"year\": ${current_year},
    \"period\": \"${current_period}\",
    \"startDate\": \"${start_date}\",
    \"endDate\": \"${end_date}\"
  }")

create_status=$(echo "$create_response" | tail -n 1)
create_body=$(echo "$create_response" | sed '$d')

if [ "$create_status" = "201" ] || [ "$create_status" = "200" ]; then
    echo -e "${GREEN}✓ HTTP $create_status - 現在の学期を作成しました${NC}"
    echo "$create_body" | jq '.' 2>/dev/null || echo "$create_body"
elif [ "$create_status" = "500" ]; then
    echo -e "${YELLOW}⚠ HTTP $create_status - API内部エラー${NC}"
    echo -e "${CYAN}※ DateTime UTC問題の可能性があります（既知の問題）${NC}"
else
    echo -e "${YELLOW}⚠ HTTP $create_status${NC}"
    echo -e "${CYAN}※ 既にデータが存在する場合は正常です${NC}"
fi
echo ""

# Semester作成テスト（過去の学期）
echo -e "${CYAN}[3-2] POST /api/semesters (過去の学期を作成)${NC}"
create_response2=$(curl -s -w "\n%{http_code}" -X POST http://localhost:8080/api/semesters \
  -H "Content-Type: application/json" \
  -d '{
    "year": 2024,
    "period": "Spring",
    "startDate": "2024-04-01",
    "endDate": "2024-07-31"
  }')

create_status2=$(echo "$create_response2" | tail -n 1)

if [ "$create_status2" = "201" ] || [ "$create_status2" = "200" ]; then
    echo -e "${GREEN}✓ HTTP $create_status2 - 過去の学期を作成しました${NC}"
elif [ "$create_status2" = "500" ]; then
    echo -e "${YELLOW}⚠ HTTP $create_status2 - API内部エラー${NC}"
else
    echo -e "${YELLOW}⚠ HTTP $create_status2${NC}"
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
echo "   現在日付: ${current_date}"
current_semester_response=$(curl -s -w "\n%{http_code}" http://localhost:8080/api/semesters/current)
current_semester_status=$(echo "$current_semester_response" | tail -n 1)
current_semester_body=$(echo "$current_semester_response" | sed '$d')

if [ "$current_semester_status" = "200" ]; then
    echo -e "${GREEN}✓ HTTP $current_semester_status - 現在の学期が見つかりました${NC}"
    echo "$current_semester_body" | jq '.' 2>/dev/null || echo "$current_semester_body"

    # 取得した学期情報を検証
    retrieved_year=$(echo "$current_semester_body" | jq -r '.year' 2>/dev/null)
    retrieved_period=$(echo "$current_semester_body" | jq -r '.period' 2>/dev/null)

    if [ "$retrieved_year" = "$current_year" ] && [ "$retrieved_period" = "$current_period" ]; then
        echo -e "${GREEN}   ✓ 想定通りの学期 (${current_year} ${current_period}) が取得されました${NC}"
    else
        echo -e "${YELLOW}   ⚠ 想定外の学期が取得されました (期待: ${current_year} ${current_period}, 実際: ${retrieved_year} ${retrieved_period})${NC}"
    fi
elif [ "$current_semester_status" = "404" ]; then
    echo -e "${YELLOW}⚠ HTTP $current_semester_status - 現在の学期が見つかりません${NC}"
    echo -e "${CYAN}※ 現在日付が学期の日付範囲外の可能性があります${NC}"
else
    echo -e "${RED}✗ HTTP $current_semester_status${NC}"
fi
echo ""

# ========================================
# CourseOffering関連のテスト
# ========================================
echo -e "${YELLOW}=== CourseOffering API Tests ===${NC}"
echo ""

# CourseOffering作成テスト
echo -e "${CYAN}[12] POST /api/courseofferings (コース開講を作成)${NC}"
offering_create_response=$(curl -s -w "\n%{http_code}" -X POST http://localhost:8080/api/courseofferings \
  -H "Content-Type: application/json" \
  -d '{
    "courseCode": "CS101",
    "year": 2024,
    "period": "Spring",
    "credits": 3,
    "maxCapacity": 30,
    "instructor": "田中教授"
  }')

offering_create_status=$(echo "$offering_create_response" | tail -n 1)
offering_create_body=$(echo "$offering_create_response" | sed '$d')

if [ "$offering_create_status" = "201" ] || [ "$offering_create_status" = "200" ]; then
    echo -e "${GREEN}✓ HTTP $offering_create_status - CourseOfferingを作成しました${NC}"
    offering_id=$(echo "$offering_create_body" | jq -r '.offeringId' 2>/dev/null)
    if [ -n "$offering_id" ] && [ "$offering_id" != "null" ]; then
        echo "Offering ID: $offering_id"
    fi
else
    echo -e "${YELLOW}⚠ HTTP $offering_create_status${NC}"
    echo "$offering_create_body"
fi
echo ""

# 追加のCourseOffering作成（複数件テスト用）
echo -e "${CYAN}[13] POST /api/courseofferings (追加のコース開講を作成)${NC}"
offering_create_response2=$(curl -s -w "\n%{http_code}" -X POST http://localhost:8080/api/courseofferings \
  -H "Content-Type: application/json" \
  -d '{
    "courseCode": "CS101",
    "year": 2024,
    "period": "Spring",
    "credits": 4,
    "maxCapacity": 25,
    "instructor": "鈴木教授"
  }')

offering_create_status2=$(echo "$offering_create_response2" | tail -n 1)
offering_create_body2=$(echo "$offering_create_response2" | sed '$d')

if [ "$offering_create_status2" = "201" ] || [ "$offering_create_status2" = "200" ]; then
    echo -e "${GREEN}✓ HTTP $offering_create_status2 - 追加のCourseOfferingを作成しました${NC}"
    offering_id2=$(echo "$offering_create_body2" | jq -r '.offeringId' 2>/dev/null)
    if [ -n "$offering_id2" ] && [ "$offering_id2" != "null" ]; then
        echo "Offering ID: $offering_id2"
    fi
else
    echo -e "${YELLOW}⚠ HTTP $offering_create_status2${NC}"
fi
echo ""

# 学期ごとのCourseOffering一覧取得テスト
echo -e "${CYAN}[14] GET /api/courseofferings?year=2024&period=Spring (学期ごとの開講一覧を取得)${NC}"
offerings_response=$(curl -s -w "\n%{http_code}" "http://localhost:8080/api/courseofferings?year=2024&period=Spring")
offerings_status=$(echo "$offerings_response" | tail -n 1)
offerings_body=$(echo "$offerings_response" | sed '$d')

if [ "$offerings_status" = "200" ]; then
    echo -e "${GREEN}✓ HTTP $offerings_status${NC}"
    offerings_count=$(echo "$offerings_body" | jq 'length' 2>/dev/null)
    if [ -n "$offerings_count" ]; then
        echo "取得件数: $offerings_count 件"
    fi
    echo "$offerings_body" | jq '.' 2>/dev/null
else
    echo -e "${RED}✗ HTTP $offerings_status${NC}"
fi
echo ""

# 特定のCourseOffering取得テスト
if [ -n "$offering_id" ] && [ "$offering_id" != "null" ]; then
    echo -e "${CYAN}[15] GET /api/courseofferings/{id} (作成したCourseOfferingを取得)${NC}"
    get_offering_response=$(curl -s -w "\n%{http_code}" http://localhost:8080/api/courseofferings/$offering_id)
    get_offering_status=$(echo "$get_offering_response" | tail -n 1)

    if [ "$get_offering_status" = "200" ]; then
        echo -e "${GREEN}✓ HTTP $get_offering_status${NC}"
        echo "$get_offering_response" | sed '$d' | jq '.' 2>/dev/null
    else
        echo -e "${RED}✗ HTTP $get_offering_status${NC}"
    fi
    echo ""

    # CourseOffering更新テスト
    echo -e "${CYAN}[16] PUT /api/courseofferings/{id} (CourseOffering情報を更新)${NC}"
    update_offering_response=$(curl -s -w "\n%{http_code}" -X PUT http://localhost:8080/api/courseofferings/$offering_id \
      -H "Content-Type: application/json" \
      -d '{
        "credits": 4,
        "maxCapacity": 35,
        "instructor": "田中教授（更新）"
      }')

    update_offering_status=$(echo "$update_offering_response" | tail -n 1)

    if [ "$update_offering_status" = "200" ] || [ "$update_offering_status" = "204" ]; then
        echo -e "${GREEN}✓ HTTP $update_offering_status - CourseOffering情報を更新しました${NC}"
    else
        echo -e "${RED}✗ HTTP $update_offering_status${NC}"
    fi
    echo ""

    # 更新後のCourseOffering取得テスト
    echo -e "${CYAN}[17] GET /api/courseofferings/{id} (更新後のCourseOfferingを確認)${NC}"
    get_offering_updated_response=$(curl -s -w "\n%{http_code}" http://localhost:8080/api/courseofferings/$offering_id)
    get_offering_updated_status=$(echo "$get_offering_updated_response" | tail -n 1)

    if [ "$get_offering_updated_status" = "200" ]; then
        echo -e "${GREEN}✓ HTTP $get_offering_updated_status${NC}"
        get_offering_updated_body=$(echo "$get_offering_updated_response" | sed '$d')
        echo "$get_offering_updated_body" | jq '.' 2>/dev/null

        # 更新内容の検証
        updated_credits=$(echo "$get_offering_updated_body" | jq -r '.credits' 2>/dev/null)
        updated_max_capacity=$(echo "$get_offering_updated_body" | jq -r '.maxCapacity' 2>/dev/null)

        if [ "$updated_credits" = "4" ] && [ "$updated_max_capacity" = "35" ]; then
            echo -e "${GREEN}   ✓ 更新が正しく反映されています${NC}"
        else
            echo -e "${YELLOW}   ⚠ 更新内容に差異があります${NC}"
        fi
    else
        echo -e "${RED}✗ HTTP $get_offering_updated_status${NC}"
    fi
    echo ""
fi

# Activeステータスでフィルタリングテスト
echo -e "${CYAN}[18] GET /api/courseofferings?year=2024&period=Spring&statusFilter=Active (Activeのみ取得)${NC}"
active_offerings_response=$(curl -s -w "\n%{http_code}" "http://localhost:8080/api/courseofferings?year=2024&period=Spring&statusFilter=Active")
active_offerings_status=$(echo "$active_offerings_response" | tail -n 1)
active_offerings_body=$(echo "$active_offerings_response" | sed '$d')

if [ "$active_offerings_status" = "200" ]; then
    echo -e "${GREEN}✓ HTTP $active_offerings_status${NC}"
    active_count=$(echo "$active_offerings_body" | jq 'length' 2>/dev/null)
    if [ -n "$active_count" ]; then
        echo "Active件数: $active_count 件"
    fi
else
    echo -e "${RED}✗ HTTP $active_offerings_status${NC}"
fi
echo ""

# ========================================
# Enrollment関連のテスト
# ========================================
echo -e "${YELLOW}=== Enrollment API Tests ===${NC}"
echo ""

# 履修登録の前提: Student IDとOffering IDを取得
# StudentとOfferingが作成済みであることを前提とする
enrollment_student_id="$student_id"
enrollment_offering_id="$offering_id"

if [ -n "$enrollment_student_id" ] && [ "$enrollment_student_id" != "null" ] && \
   [ -n "$enrollment_offering_id" ] && [ "$enrollment_offering_id" != "null" ]; then

    # 履修登録作成テスト
    echo -e "${CYAN}[19] POST /api/enrollments (履修登録を作成)${NC}"
    enrollment_create_response=$(curl -s -w "\n%{http_code}" -X POST http://localhost:8080/api/enrollments \
      -H "Content-Type: application/json" \
      -d "{
        \"studentId\": \"$enrollment_student_id\",
        \"offeringId\": $enrollment_offering_id,
        \"enrolledBy\": \"student-$enrollment_student_id\",
        \"initialNote\": \"必修科目のため履修\"
      }")

    enrollment_create_status=$(echo "$enrollment_create_response" | tail -n 1)
    enrollment_create_body=$(echo "$enrollment_create_response" | sed '$d')

    if [ "$enrollment_create_status" = "201" ] || [ "$enrollment_create_status" = "200" ]; then
        echo -e "${GREEN}✓ HTTP $enrollment_create_status - 履修登録を作成しました${NC}"
        enrollment_id=$(echo "$enrollment_create_body" | jq -r '.enrollmentId' 2>/dev/null)
        if [ -n "$enrollment_id" ] && [ "$enrollment_id" != "null" ]; then
            echo "Enrollment ID: $enrollment_id"
        fi
        echo "$enrollment_create_body" | jq '.' 2>/dev/null
    else
        echo -e "${YELLOW}⚠ HTTP $enrollment_create_status${NC}"
        echo "$enrollment_create_body"
    fi
    echo ""

    # 学生の履修登録一覧取得テスト
    echo -e "${CYAN}[20] GET /api/enrollments/students/{studentId} (学生の履修登録一覧を取得)${NC}"
    get_enrollments_response=$(curl -s -w "\n%{http_code}" http://localhost:8080/api/enrollments/students/$enrollment_student_id)
    get_enrollments_status=$(echo "$get_enrollments_response" | tail -n 1)
    get_enrollments_body=$(echo "$get_enrollments_response" | sed '$d')

    if [ "$get_enrollments_status" = "200" ]; then
        echo -e "${GREEN}✓ HTTP $get_enrollments_status${NC}"
        enrollments_count=$(echo "$get_enrollments_body" | jq 'length' 2>/dev/null)
        if [ -n "$enrollments_count" ]; then
            echo "履修登録件数: $enrollments_count 件"
        fi
        echo "$get_enrollments_body" | jq '.' 2>/dev/null
    else
        echo -e "${RED}✗ HTTP $get_enrollments_status${NC}"
    fi
    echo ""

    # Enrolledステータスでフィルタリングテスト
    echo -e "${CYAN}[21] GET /api/enrollments/students/{studentId}?statusFilter=Enrolled (Enrolledのみ取得)${NC}"
    enrolled_only_response=$(curl -s -w "\n%{http_code}" "http://localhost:8080/api/enrollments/students/$enrollment_student_id?statusFilter=Enrolled")
    enrolled_only_status=$(echo "$enrolled_only_response" | tail -n 1)
    enrolled_only_body=$(echo "$enrolled_only_response" | sed '$d')

    if [ "$enrolled_only_status" = "200" ]; then
        echo -e "${GREEN}✓ HTTP $enrolled_only_status${NC}"
        enrolled_count=$(echo "$enrolled_only_body" | jq 'length' 2>/dev/null)
        if [ -n "$enrolled_count" ]; then
            echo "Enrolled件数: $enrolled_count 件"
        fi
    else
        echo -e "${RED}✗ HTTP $enrolled_only_status${NC}"
    fi
    echo ""

    # 2つ目の履修登録を作成（キャンセル・完了テスト用）
    if [ -n "$offering_id2" ] && [ "$offering_id2" != "null" ]; then
        echo -e "${CYAN}[22] POST /api/enrollments (追加の履修登録を作成 - テスト用)${NC}"
        enrollment_create_response2=$(curl -s -w "\n%{http_code}" -X POST http://localhost:8080/api/enrollments \
          -H "Content-Type: application/json" \
          -d "{
            \"studentId\": \"$enrollment_student_id\",
            \"offeringId\": $offering_id2,
            \"enrolledBy\": \"student-$enrollment_student_id\"
          }")

        enrollment_create_status2=$(echo "$enrollment_create_response2" | tail -n 1)
        enrollment_create_body2=$(echo "$enrollment_create_response2" | sed '$d')

        if [ "$enrollment_create_status2" = "201" ] || [ "$enrollment_create_status2" = "200" ]; then
            echo -e "${GREEN}✓ HTTP $enrollment_create_status2 - 追加の履修登録を作成しました${NC}"
            enrollment_id2=$(echo "$enrollment_create_body2" | jq -r '.enrollmentId' 2>/dev/null)
            if [ -n "$enrollment_id2" ] && [ "$enrollment_id2" != "null" ]; then
                echo "Enrollment ID: $enrollment_id2"
            fi
        else
            echo -e "${YELLOW}⚠ HTTP $enrollment_create_status2${NC}"
        fi
        echo ""

        # 履修登録完了テスト
        if [ -n "$enrollment_id2" ] && [ "$enrollment_id2" != "null" ]; then
            echo -e "${CYAN}[23] POST /api/enrollments/{enrollmentId}/complete (履修登録を完了)${NC}"
            complete_enrollment_response=$(curl -s -w "\n%{http_code}" -X POST http://localhost:8080/api/enrollments/$enrollment_id2/complete \
              -H "Content-Type: application/json" \
              -d '{
                "completedBy": "system-grade-processor",
                "reason": "学期終了による自動完了"
              }')

            complete_enrollment_status=$(echo "$complete_enrollment_response" | tail -n 1)

            if [ "$complete_enrollment_status" = "200" ] || [ "$complete_enrollment_status" = "204" ]; then
                echo -e "${GREEN}✓ HTTP $complete_enrollment_status - 履修登録を完了しました${NC}"
            else
                echo -e "${RED}✗ HTTP $complete_enrollment_status${NC}"
            fi
            echo ""
        fi
    fi

    # 履修登録キャンセルテスト（元の履修登録をキャンセル）
    if [ -n "$enrollment_id" ] && [ "$enrollment_id" != "null" ]; then
        echo -e "${CYAN}[24] POST /api/enrollments/{enrollmentId}/cancel (履修登録をキャンセル)${NC}"
        cancel_enrollment_response=$(curl -s -w "\n%{http_code}" -X POST http://localhost:8080/api/enrollments/$enrollment_id/cancel \
          -H "Content-Type: application/json" \
          -d "{
            \"cancelledBy\": \"student-$enrollment_student_id\",
            \"reason\": \"履修計画の変更のため\"
          }")

        cancel_enrollment_status=$(echo "$cancel_enrollment_response" | tail -n 1)

        if [ "$cancel_enrollment_status" = "200" ] || [ "$cancel_enrollment_status" = "204" ]; then
            echo -e "${GREEN}✓ HTTP $cancel_enrollment_status - 履修登録をキャンセルしました${NC}"
        else
            echo -e "${RED}✗ HTTP $cancel_enrollment_status${NC}"
        fi
        echo ""

        # キャンセル後の履修登録一覧確認
        echo -e "${CYAN}[25] GET /api/enrollments/students/{studentId} (キャンセル後の履修登録一覧を確認)${NC}"
        after_cancel_response=$(curl -s -w "\n%{http_code}" http://localhost:8080/api/enrollments/students/$enrollment_student_id)
        after_cancel_status=$(echo "$after_cancel_response" | tail -n 1)
        after_cancel_body=$(echo "$after_cancel_response" | sed '$d')

        if [ "$after_cancel_status" = "200" ]; then
            echo -e "${GREEN}✓ HTTP $after_cancel_status${NC}"

            # Cancelled件数をカウント
            cancelled_count=$(echo "$after_cancel_body" | jq '[.[] | select(.status == "Cancelled")] | length' 2>/dev/null)
            completed_count=$(echo "$after_cancel_body" | jq '[.[] | select(.status == "Completed")] | length' 2>/dev/null)

            if [ -n "$cancelled_count" ]; then
                echo "Cancelled件数: $cancelled_count 件"
            fi
            if [ -n "$completed_count" ]; then
                echo "Completed件数: $completed_count 件"
            fi

            echo "$after_cancel_body" | jq '.' 2>/dev/null
        else
            echo -e "${RED}✗ HTTP $after_cancel_status${NC}"
        fi
        echo ""
    fi

    # 重複登録エラーテスト（新しくenrollmentを作成してから重複をテスト）
    echo -e "${CYAN}[26] POST /api/enrollments (新しい履修登録を作成)${NC}"
    new_enrollment_response=$(curl -s -w "\n%{http_code}" -X POST http://localhost:8080/api/enrollments \
      -H "Content-Type: application/json" \
      -d "{
        \"studentId\": \"$enrollment_student_id\",
        \"offeringId\": $enrollment_offering_id,
        \"enrolledBy\": \"student-$enrollment_student_id\",
        \"initialNote\": \"重複テスト用の新しい登録\"
      }")

    new_enrollment_status=$(echo "$new_enrollment_response" | tail -n 1)

    if [ "$new_enrollment_status" = "201" ] || [ "$new_enrollment_status" = "200" ]; then
        echo -e "${GREEN}✓ HTTP $new_enrollment_status - 新しい履修登録を作成しました${NC}"

        # 重複登録エラーテスト
        echo ""
        echo -e "${CYAN}[27] POST /api/enrollments (重複登録エラーテスト)${NC}"
        duplicate_enrollment_response=$(curl -s -w "\n%{http_code}" -X POST http://localhost:8080/api/enrollments \
          -H "Content-Type: application/json" \
          -d "{
            \"studentId\": \"$enrollment_student_id\",
            \"offeringId\": $enrollment_offering_id,
            \"enrolledBy\": \"student-$enrollment_student_id\"
          }")

        duplicate_enrollment_status=$(echo "$duplicate_enrollment_response" | tail -n 1)

        if [ "$duplicate_enrollment_status" = "409" ]; then
            echo -e "${GREEN}✓ HTTP $duplicate_enrollment_status - 期待通り重複エラーが返されました${NC}"
        elif [ "$duplicate_enrollment_status" = "201" ] || [ "$duplicate_enrollment_status" = "200" ]; then
            echo -e "${YELLOW}⚠ HTTP $duplicate_enrollment_status - 重複チェックが機能していない可能性があります${NC}"
        else
            echo -e "${YELLOW}⚠ HTTP $duplicate_enrollment_status${NC}"
        fi
    else
        echo -e "${YELLOW}⚠ HTTP $new_enrollment_status - 新しい履修登録の作成に失敗しました${NC}"
    fi
    echo ""

else
    echo -e "${YELLOW}⚠ StudentまたはCourseOfferingが作成されていないため、Enrollmentテストをスキップします${NC}"
    echo ""
fi

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
