-- データ移行: courses.students → student_registrations.students
--
-- 注意:
-- このマイグレーションは StudentRegistrations の V1__Create_Students.sql 実行後に実行すること
-- StudentRegistrations API を先にデプロイし、マイグレーションを実行してから、
-- このマイグレーションを実行する必要があります

-- Step 1: courses.students から student_registrations.students にデータをコピー
INSERT INTO student_registrations.students (
    id,
    name,
    email,
    grade,
    created_at
)
SELECT
    id,
    name,
    email,
    grade,
    created_at
FROM courses.students
ON CONFLICT (id) DO NOTHING;       -- 既に存在する場合はスキップ

-- Step 2: enrollments テーブルの外部キー制約を確認・削除
-- 注意: 外部キー制約名は実際のスキーマに合わせて調整が必要
DO $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM information_schema.table_constraints
        WHERE constraint_name = 'fk_enrollments_student_id'
        AND table_name = 'enrollments'
        AND table_schema = 'courses'
    ) THEN
        ALTER TABLE courses.enrollments
        DROP CONSTRAINT fk_enrollments_student_id;
    END IF;
END $$;

-- Step 3: courses.students テーブルを削除
DROP TABLE IF EXISTS courses.students;

-- Step 4: コメント追加
-- クロススキーマ外部キー制約は作成せず、アプリケーションレベルで整合性を保証
COMMENT ON COLUMN courses.enrollments.student_id IS 'References student_registrations.students(id) - managed by application layer (ACL via HTTP)';

-- Step 5: 移行確認用のビュー作成（オプション）
CREATE OR REPLACE VIEW courses.v_enrollment_students AS
SELECT
    e.enrollment_id,
    e.student_id,
    s.name AS student_name,
    s.email AS student_email,
    s.grade AS student_grade
FROM courses.enrollments e
LEFT JOIN student_registrations.students s ON e.student_id = s.id;

COMMENT ON VIEW courses.v_enrollment_students IS '履修登録と学生情報の結合ビュー（参照用・読み取り専用）';
