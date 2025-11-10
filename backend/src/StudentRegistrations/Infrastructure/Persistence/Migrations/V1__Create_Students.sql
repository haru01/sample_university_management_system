-- StudentRegistrations コンテキストのスキーマ作成
CREATE SCHEMA IF NOT EXISTS student_registrations;

-- Students テーブル作成
-- 注: 現在はシンプルな構造。将来的に在籍ステータス管理機能を追加予定
CREATE TABLE student_registrations.students (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(200) NOT NULL,
    email VARCHAR(200) NOT NULL,
    grade INT NOT NULL CHECK (grade >= 1 AND grade <= 4),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- インデックス作成
CREATE UNIQUE INDEX ix_students_email ON student_registrations.students(email);
CREATE INDEX ix_students_grade ON student_registrations.students(grade);

COMMENT ON TABLE student_registrations.students IS '学生マスタ - 学生の基本情報を管理';
COMMENT ON COLUMN student_registrations.students.id IS '学生ID (UUID)';
COMMENT ON COLUMN student_registrations.students.name IS '学生氏名';
COMMENT ON COLUMN student_registrations.students.email IS 'メールアドレス (一意制約)';
COMMENT ON COLUMN student_registrations.students.grade IS '学年 (1-4)';
COMMENT ON COLUMN student_registrations.students.created_at IS '作成日時';
