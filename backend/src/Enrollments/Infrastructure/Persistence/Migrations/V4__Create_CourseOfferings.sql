-- コース開講テーブル
CREATE TABLE courses.course_offerings (
    offering_id INT PRIMARY KEY,
    course_code VARCHAR(10) NOT NULL,
    semester_id VARCHAR(50) NOT NULL,
    credits INT NOT NULL,
    max_capacity INT NOT NULL,
    instructor VARCHAR(200),
    status VARCHAR(20) NOT NULL,
    CHECK (credits >= 1 AND credits <= 10),
    CHECK (max_capacity >= 1),
    CHECK (status IN ('Active', 'Cancelled'))
);

-- 外部キー制約
ALTER TABLE courses.course_offerings
ADD CONSTRAINT fk_course_offerings_course_code
    FOREIGN KEY (course_code) REFERENCES courses.courses(code);

-- インデックス
CREATE INDEX idx_course_offerings_semester_id ON courses.course_offerings(semester_id);
CREATE INDEX idx_course_offerings_course_code ON courses.course_offerings(course_code);
CREATE INDEX idx_course_offerings_status ON courses.course_offerings(status);
