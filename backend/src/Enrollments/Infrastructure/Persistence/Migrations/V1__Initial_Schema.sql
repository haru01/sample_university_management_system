-- コーススキーマ作成
CREATE SCHEMA IF NOT EXISTS courses;

-- コーステーブル
CREATE TABLE courses.courses (
    code VARCHAR(10) PRIMARY KEY,
    name VARCHAR(200) NOT NULL,
    credits INT NOT NULL CHECK (credits >= 1 AND credits <= 10),
    max_capacity INT NOT NULL CHECK (max_capacity >= 1)
);

-- インデックス
CREATE INDEX idx_courses_name ON courses.courses(name);
