-- 授業セッションテーブル
CREATE TABLE courses.class_sessions (
    session_id SERIAL PRIMARY KEY,
    offering_id INT NOT NULL,
    session_number INT NOT NULL,
    session_date DATE NOT NULL,
    start_time TIME NOT NULL,
    end_time TIME NOT NULL,
    location VARCHAR(50),
    topic VARCHAR(200),
    status VARCHAR(20) NOT NULL,
    cancellation_reason VARCHAR(200),
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CHECK (status IN ('Scheduled', 'Completed', 'Cancelled')),
    CHECK (start_time < end_time)
);

-- 外部キー制約: CourseOfferingへの参照
ALTER TABLE courses.class_sessions
ADD CONSTRAINT fk_class_sessions_offering_id
    FOREIGN KEY (offering_id) REFERENCES courses.course_offerings(offering_id);

-- 一意制約: OfferingId + SessionNumberの組み合わせは一意
CREATE UNIQUE INDEX ix_class_sessions_offering_session
    ON courses.class_sessions(offering_id, session_number);

-- インデックス: コース開講IDでの検索用
CREATE INDEX ix_class_sessions_offering_id ON courses.class_sessions(offering_id);

-- インデックス: セッション日付での検索用
CREATE INDEX ix_class_sessions_session_date ON courses.class_sessions(session_date);

-- インデックス: ステータスでの検索用
CREATE INDEX ix_class_sessions_status ON courses.class_sessions(status);
