-- 学生テーブル
CREATE TABLE courses.students (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(200) NOT NULL,
    email VARCHAR(255) NOT NULL UNIQUE,
    grade INT NOT NULL CHECK (grade >= 1 AND grade <= 4),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- インデックス
CREATE INDEX idx_students_email ON courses.students(email);
