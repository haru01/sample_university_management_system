-- 履修登録テーブル
CREATE TABLE courses.enrollments (
    enrollment_id UUID PRIMARY KEY,
    student_id UUID NOT NULL,
    offering_id INT NOT NULL,
    status VARCHAR(20) NOT NULL,
    enrolled_at TIMESTAMP NOT NULL,
    completed_at TIMESTAMP,
    cancelled_at TIMESTAMP,
    CHECK (status IN ('Enrolled', 'Completed', 'Cancelled'))
);

-- 外部キー制約
ALTER TABLE courses.enrollments
ADD CONSTRAINT fk_enrollments_student_id
    FOREIGN KEY (student_id) REFERENCES courses.students(id);

ALTER TABLE courses.enrollments
ADD CONSTRAINT fk_enrollments_offering_id
    FOREIGN KEY (offering_id) REFERENCES courses.course_offerings(offering_id);

-- 部分一意制約: アクティブな履修登録のみに適用（Cancelledは除外）
-- これにより、学生はキャンセルした授業に再登録できる
CREATE UNIQUE INDEX ix_enrollments_student_offering_active
    ON courses.enrollments(student_id, offering_id)
    WHERE status != 'Cancelled';

-- インデックス: 学生IDでの検索用
CREATE INDEX ix_enrollments_student_id ON courses.enrollments(student_id);

-- インデックス: コース開講IDでの検索用
CREATE INDEX ix_enrollments_offering_id ON courses.enrollments(offering_id);

-- インデックス: ステータスでの検索用
CREATE INDEX ix_enrollments_status ON courses.enrollments(status);
