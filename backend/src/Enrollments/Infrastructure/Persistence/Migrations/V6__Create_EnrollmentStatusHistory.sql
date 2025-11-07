-- V6: Create EnrollmentStatusHistory table
-- 履修登録の状態変更履歴テーブルを作成
-- Option 2 - 完全な監査証跡実装

CREATE TABLE courses.enrollment_status_history (
    history_id UUID PRIMARY KEY,
    enrollment_id UUID NOT NULL,
    status VARCHAR(20) NOT NULL CHECK (status IN ('Enrolled', 'Completed', 'Cancelled')),
    changed_at TIMESTAMP NOT NULL,
    changed_by VARCHAR(100) NOT NULL,
    reason TEXT,
    metadata JSONB,
    CONSTRAINT fk_enrollment_status_history_enrollment_id
        FOREIGN KEY (enrollment_id) REFERENCES courses.enrollments(enrollment_id) ON DELETE CASCADE
);

-- インデックス作成
CREATE INDEX ix_enrollment_status_history_enrollment_id ON courses.enrollment_status_history(enrollment_id);
CREATE INDEX ix_enrollment_status_history_changed_at ON courses.enrollment_status_history(changed_at);
CREATE INDEX ix_enrollment_status_history_status ON courses.enrollment_status_history(status);

-- コメント追加
COMMENT ON TABLE courses.enrollment_status_history IS '履修登録ステータス履歴 - 状態変更の完全な監査証跡';
COMMENT ON COLUMN courses.enrollment_status_history.history_id IS '履歴レコードの一意識別子';
COMMENT ON COLUMN courses.enrollment_status_history.enrollment_id IS '履修登録ID（外部キー）';
COMMENT ON COLUMN courses.enrollment_status_history.status IS '変更後のステータス（Enrolled/Completed/Cancelled）';
COMMENT ON COLUMN courses.enrollment_status_history.changed_at IS '状態変更日時';
COMMENT ON COLUMN courses.enrollment_status_history.changed_by IS '変更実行者ID（学生ID、システムID、管理者IDなど）';
COMMENT ON COLUMN courses.enrollment_status_history.reason IS '変更理由（キャンセルの場合は必須）';
COMMENT ON COLUMN courses.enrollment_status_history.metadata IS '追加のメタデータ（JSON形式）';
