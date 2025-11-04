-- 学期テーブル
CREATE TABLE courses.semesters (
    id_year INT NOT NULL,
    id_period VARCHAR(20) NOT NULL,
    start_date TIMESTAMP NOT NULL,
    end_date TIMESTAMP NOT NULL,
    PRIMARY KEY (id_year, id_period),
    CHECK (id_year >= 2000 AND id_year <= 2100),
    CHECK (id_period IN ('Spring', 'Fall')),
    CHECK (end_date > start_date)
);

-- インデックス
CREATE INDEX idx_semesters_dates ON courses.semesters(start_date, end_date);
