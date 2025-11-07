# 履修登録（Enrollment）ユーザーストーリー - Phase 5 改訂版

> **設計方針**: Option 2 - EnrollmentStatusHistoryによる完全な監査証跡
>
> 状態変更の履歴を`EnrollmentStatusHistory`テーブルに記録し、誰が・いつ・なぜ変更したかを追跡可能にする。

---

## Phase 5: 履修登録管理（Enrollment Management） - 改訂版

### ✅ US-E01: 学生をコース開講に履修登録できる（改訂版）

**ストーリー:**
学生として、コース開講に履修登録できるようにしたい。なぜなら、履修したいコースを選択して学習を開始したいから。

**Handler:** `EnrollStudentCommandHandler : IRequestHandler<EnrollStudentCommand, Guid>`

**Command定義:**
```csharp
public record EnrollStudentCommand : IRequest<Guid>
{
    public required Guid StudentId { get; init; }
    public required int OfferingId { get; init; }
    public required string EnrolledBy { get; init; }  // 追加: 登録実行者（通常は学生本人）
    public string? InitialNote { get; init; }         // 追加: 初期メモ（オプション）
}
```

**受け入れ条件:**

```gherkin
Scenario: 学生が有効なコース開講を履修登録する
  Given データベースにStudentId "123e4567-e89b-12d3-a456-426614174000" の学生が登録されている
  And データベースにOfferingId 1 のCourseOfferingが存在する
    - CourseCode: "CS101"
    - SemesterId: (2024, Spring)
    - Credits: 3
    - MaxCapacity: 30
    - Status: Active
  And 現在のアクティブな履修登録数が 20件 である（定員30のうち）
  And 学生が既にOfferingId 1 に履修登録していない
  When EnrollStudentCommandを実行する
    - StudentId: "123e4567-e89b-12d3-a456-426614174000"
    - OfferingId: 1
    - EnrolledBy: "student-123e4567-e89b-12d3-a456-426614174000"
    - InitialNote: null
  Then 新しいEnrollmentIdが返される
  And データベースにEnrollmentが保存されている
    - Status: "Enrolled"
    - EnrolledAt: （実行時刻）
    - CompletedAt: null
    - CancelledAt: null
  And EnrollmentStatusHistoryに初期レコードが作成される
    - EnrollmentId: （上記で作成されたID）
    - Status: "Enrolled"
    - ChangedBy: "student-123e4567-e89b-12d3-a456-426614174000"
    - Reason: "Initial enrollment" または null
    - ChangedAt: （実行時刻）
    - Metadata: { "InitialNote": null } または null
```

```gherkin
Scenario: 定員に達したコース開講への登録を試みる
  Given データベースにOfferingId 1 のCourseOfferingが存在する
    - MaxCapacity: 30
  And 現在のアクティブな履修登録数が 30件 である（定員に達している）
  When EnrollStudentCommandを実行する
    - StudentId: "123e4567-e89b-12d3-a456-426614174000"
    - OfferingId: 1
    - EnrolledBy: "student-123e4567-e89b-12d3-a456-426614174000"
  Then ConflictException がスローされる
  And エラーメッセージに "定員に達しています" が含まれる
  And データベースにEnrollmentは作成されない
  And EnrollmentStatusHistoryにレコードは作成されない
```

```gherkin
Scenario: 同じコース開講を重複して登録を試みる
  Given データベースにOfferingId 1 のCourseOfferingが存在する
  And 学生が既にOfferingId 1 にStatus="Enrolled"で履修登録している
  When EnrollStudentCommandを実行する
    - StudentId: （同じ学生ID）
    - OfferingId: 1
    - EnrolledBy: "student-123e4567-e89b-12d3-a456-426614174000"
  Then ConflictException がスローされる
  And エラーメッセージに "既に履修登録しています" が含まれる
  And データベースに新しいEnrollmentは作成されない
```

```gherkin
Scenario: EnrolledByパラメータなしで登録を試みる
  Given データベースにStudentId "123e4567-e89b-12d3-a456-426614174000" の学生が登録されている
  And データベースにOfferingId 1 のCourseOfferingが存在する
  When EnrollStudentCommandを実行する
    - StudentId: "123e4567-e89b-12d3-a456-426614174000"
    - OfferingId: 1
    - EnrolledBy: null または空文字列
  Then ValidationException がスローされる
  And エラーメッセージに "登録実行者は必須です" が含まれる
```

**制約:**

- コース開講は Active 状態である必要がある
- 定員（MaxCapacity）を超える登録は不可
- 同一学生が同じOfferingIdに重複登録不可（一意制約）
- **EnrolledByパラメータ**: 必須（誰が登録したかを記録）
- **InitialNoteパラメータ**: オプション（特記事項があれば記録）
- **状態遷移ログ**: 履修登録作成時に必ずEnrollmentStatusHistoryに初期レコード作成
- **初期ステータス**: 常に "Enrolled"（仮登録）

**実装状態:** 🔄 部分実装（StatusHistory未実装）

---

### ✅ US-E02: 履修登録をキャンセルできる（改訂版）

**ストーリー:**
API利用者として、履修登録をキャンセルできるようにしたい。なぜなら、履修計画を変更したい場合があるから。

**Handler:** `CancelEnrollmentCommandHandler : IRequestHandler<CancelEnrollmentCommand, Unit>`

**Command定義:**
```csharp
public record CancelEnrollmentCommand : IRequest
{
    public required Guid EnrollmentId { get; init; }
    public required string CancelledBy { get; init; }  // 追加: キャンセル実行者
    public required string Reason { get; init; }       // 追加: キャンセル理由（必須）
}
```

**受け入れ条件:**

```gherkin
Scenario: 進行中の履修登録を理由付きでキャンセルする
  Given データベースに履修登録ID "abc-123" の履修登録が存在する
  And 履修登録ステータスが "Enrolled" である
  When CancelEnrollmentCommandを実行する
    - EnrollmentId: "abc-123"
    - CancelledBy: "student-001"
    - Reason: "履修計画の変更"
  Then 正常に完了する（戻り値なし）
  And Enrollment.Status が "Cancelled" になる
  And Enrollment.CancelledAt が設定される（実行時刻）
  And EnrollmentStatusHistoryに新しいレコードが追加される
    - EnrollmentId: "abc-123"
    - Status: "Cancelled"
    - ChangedBy: "student-001"
    - Reason: "履修計画の変更"
    - ChangedAt: （実行時刻）
```

```gherkin
Scenario: キャンセル理由なしでキャンセルを試みる
  Given データベースに履修登録ID "abc-123" の履修登録が存在する
  And 履修登録ステータスが "Enrolled" である
  When CancelEnrollmentCommandを実行する
    - EnrollmentId: "abc-123"
    - CancelledBy: "student-001"
    - Reason: null または空文字列
  Then ValidationException がスローされる
  And エラーメッセージに "キャンセル理由は必須です" が含まれる
  And Enrollment.Status は変更されない
  And EnrollmentStatusHistoryに新しいレコードは追加されない
```

```gherkin
Scenario: CancelledByパラメータなしでキャンセルを試みる
  Given データベースに履修登録ID "abc-123" の履修登録が存在する
  And 履修登録ステータスが "Enrolled" である
  When CancelEnrollmentCommandを実行する
    - EnrollmentId: "abc-123"
    - CancelledBy: null または空文字列
    - Reason: "履修計画の変更"
  Then ValidationException がスローされる
  And エラーメッセージに "実行者は必須です" が含まれる
```

```gherkin
Scenario: 既に完了している履修登録をキャンセルしようとする
  Given データベースに履修登録ID "abc-123" の履修登録が存在する
  And 履修登録ステータスが "Completed" である
  When CancelEnrollmentCommandを実行する
    - EnrollmentId: "abc-123"
    - CancelledBy: "student-001"
    - Reason: "履修計画の変更"
  Then InvalidOperationException がスローされる
  And エラーメッセージに "完了した履修登録はキャンセルできません" が含まれる
  And 履修登録ステータスは変更されない
  And EnrollmentStatusHistoryに新しいレコードは追加されない
```

```gherkin
Scenario: 既にキャンセル済みの履修登録を再度キャンセルしようとする
  Given データベースに履修登録ID "abc-123" の履修登録が存在する
  And 履修登録ステータスが "Cancelled" である
  When CancelEnrollmentCommandを実行する
    - EnrollmentId: "abc-123"
    - CancelledBy: "student-001"
    - Reason: "二重キャンセルの試み"
  Then InvalidOperationException がスローされる
  And エラーメッセージに "既にキャンセル済みです" が含まれる
  And EnrollmentStatusHistoryに新しいレコードは追加されない
```

**制約:**

- キャンセル可能なステータス: **Enrolled のみ**
- **CancelledBy（実行者）**: 必須、空文字列不可、最大100文字
- **Reason（キャンセル理由）**: 必須、空文字列不可、最大1000文字
- Completed または Cancelled ステータスはキャンセル不可
- **状態遷移ログ**: EnrollmentStatusHistoryテーブルに必ず記録（イミュータブル）
- **Statusの更新**: 状態遷移ログ追加後、Enrollment.Statusを更新
- **CancelledAtの記録**: キャンセル時刻を記録

**キャンセル理由の例:**
- "履修計画の変更"
- "他のコースとの時間重複"
- "授業内容が期待と異なる"
- "単位数調整のため"
- "健康上の理由"
- "システム管理者による強制キャンセル"

**実装状態:** 🔄 部分実装（StatusHistory未実装、Reason/CancelledByパラメータ未実装）

---

### ✅ US-E03: 履修登録一覧を取得できる

**ストーリー:**
学生・教員として、学生の履修登録一覧を取得できるようにしたい。なぜなら、現在の履修状況を確認する必要があるから。

**Handler:** `GetStudentEnrollmentsQueryHandler : IRequestHandler<GetStudentEnrollmentsQuery, List<EnrollmentDto>>`

**受け入れ条件:**

```gherkin
Scenario: 学生の全ての履修登録を取得する
  Given StudentRepositoryにStudentId "student-001" が存在する
  And EnrollmentRepositoryに以下のEnrollmentが存在する
    | OfferingId | CourseCode | SemesterId      | Status     |
    | 1          | CS101      | (2024, Spring)  | Enrolled   |
    | 2          | MATH201    | (2024, Spring)  | Enrolled   |
    | 3          | ENG101     | (2023, Fall)    | Completed  |
  When GetStudentEnrollmentsQueryを実行する
    - StudentId: "student-001"
  Then 3件のEnrollmentDtoが返される
  And Semesterの新しい順にソートされている
  And 各EnrollmentDtoにはStatusが含まれる
```

```gherkin
Scenario: ステータスでフィルタリングして履修登録を取得する
  Given StudentRepositoryにStudentId "student-001" が存在する
  And 上記と同じEnrollmentデータが存在する
  When GetStudentEnrollmentsQueryを実行する
    - StudentId: "student-001"
    - StatusFilter: "Enrolled"
  Then StatusがEnrolledのEnrollmentDtoのみが返される（2件）
```

**制約:**

- 学生は自分の履修登録のみ閲覧可能（認可は別途実装）
- デフォルトソート: 学期の新しい順
- ステータスフィルタリング: オプショナル

**実装状態:** ✅ 実装済み

---

### ✅ US-E04: 履修登録を完了できる（改訂版）

**ストーリー:**
システムとして、履修登録を完了（仮登録→本登録）できるようにしたい。なぜなら、学期終了時に履修を確定する必要があるから。

**Handler:** `CompleteEnrollmentCommandHandler : IRequestHandler<CompleteEnrollmentCommand, Unit>`

**Command定義:**
```csharp
public record CompleteEnrollmentCommand : IRequest
{
    public required Guid EnrollmentId { get; init; }
    public required string CompletedBy { get; init; }  // 追加: 完了実行者（通常はシステム）
    public string? Reason { get; init; }               // 追加: 完了理由（オプション）
}
```

**受け入れ条件:**

```gherkin
Scenario: 進行中の履修登録を完了する
  Given EnrollmentRepositoryにEnrollmentId "enrollment-001" が存在する
  And EnrollmentのStatusがEnrolledである
  When CompleteEnrollmentCommandを実行する
    - EnrollmentId: "enrollment-001"
    - CompletedBy: "system-grade-processor"
    - Reason: "学期終了による自動完了"
  Then Enrollment.StatusがCompletedに更新される
  And Enrollment.CompletedAtが記録される（実行時刻）
  And EnrollmentStatusHistoryに新しいレコードが追加される
    - EnrollmentId: "enrollment-001"
    - Status: "Completed"
    - ChangedBy: "system-grade-processor"
    - Reason: "学期終了による自動完了"
    - ChangedAt: （実行時刻）
```

```gherkin
Scenario: CompletedByパラメータなしで完了を試みる
  Given EnrollmentRepositoryにEnrollmentId "enrollment-001" が存在する
  And EnrollmentのStatusがEnrolledである
  When CompleteEnrollmentCommandを実行する
    - EnrollmentId: "enrollment-001"
    - CompletedBy: null または空文字列
    - Reason: "学期終了"
  Then ValidationException がスローされる
  And エラーメッセージに "実行者は必須です" が含まれる
```

```gherkin
Scenario: 既に完了している履修登録を再度完了しようとする
  Given EnrollmentRepositoryにEnrollmentId "enrollment-001" が存在する
  And EnrollmentのStatusがCompletedである
  When CompleteEnrollmentCommandを実行する
    - EnrollmentId: "enrollment-001"
    - CompletedBy: "system"
    - Reason: null
  Then InvalidOperationException がスローされる
  And エラーメッセージに "既に完了しています" が含まれる
  And Statusは変更されない
  And EnrollmentStatusHistoryに新しいレコードは追加されない
```

```gherkin
Scenario: キャンセル済みの履修登録を完了しようとする
  Given EnrollmentRepositoryにEnrollmentId "enrollment-001" が存在する
  And EnrollmentのStatusがCancelledである
  When CompleteEnrollmentCommandを実行する
    - EnrollmentId: "enrollment-001"
    - CompletedBy: "system"
    - Reason: null
  Then InvalidOperationException がスローされる
  And エラーメッセージに "キャンセル済みの履修登録は完了できません" が含まれる
  And Statusは変更されない
  And EnrollmentStatusHistoryに新しいレコードは追加されない
```

**制約:**

- 完了可能なステータス: **Enrolled のみ**
- Completed または Cancelled からの完了は不可
- **CompletedBy（実行者）**: 必須、システムID・管理者IDなど
- **Reason（理由）**: オプション（学期終了時の一括完了の場合など）
- **状態遷移ログ**: EnrollmentStatusHistoryテーブルに必ず記録（イミュータブル）
- **Statusの更新**: 状態遷移ログ追加後、Enrollment.Statusを更新
- **CompletedAtの記録**: 完了時刻を記録

**実装状態:** 🔄 部分実装（StatusHistory未実装、CompletedBy/Reasonパラメータ未実装）

---

## ドメインモデル定義（改訂版）

### Enrollment（集約ルート）

```csharp
public class Enrollment : AggregateRoot<EnrollmentId>
{
    public StudentId StudentId { get; private set; }
    public OfferingId OfferingId { get; private set; }
    public EnrollmentStatus Status { get; private set; }  // Enrolled, Completed, Cancelled
    public DateTime EnrolledAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }

    // StatusHistoryへのナビゲーション（読み取り専用）
    private readonly List<EnrollmentStatusHistory> _statusHistory = new();
    public IReadOnlyList<EnrollmentStatusHistory> StatusHistory => _statusHistory.AsReadOnly();

    // ビジネスロジック
    public static Enrollment Create(StudentId studentId, OfferingId offeringId, string enrolledBy, string? initialNote = null);
    public void Complete(string completedBy, string? reason = null);
    public void Cancel(string cancelledBy, string reason);  // reasonは必須
    public bool IsActive() => Status != EnrollmentStatus.Cancelled;
    public bool IsCompleted() => Status == EnrollmentStatus.Completed;
}
```

### EnrollmentStatusHistory（子エンティティ、値オブジェクト的扱い）

```csharp
public class EnrollmentStatusHistory : Entity<EnrollmentStatusHistoryId>
{
    public EnrollmentId EnrollmentId { get; private set; }     // 親への外部キー
    public EnrollmentStatus Status { get; private set; }       // Enrolled, Completed, Cancelled
    public DateTime ChangedAt { get; private set; }
    public string ChangedBy { get; private set; }              // 実行者ID（必須、最大100文字）
    public string? Reason { get; private set; }                // 変更理由（オプション、最大1000文字）
    public string? Metadata { get; private set; }              // JSON形式の追加情報（オプション）

    // Cancelの場合はReasonは必須
    // CreateやCompleteの場合はReasonはオプション

    private EnrollmentStatusHistory() { }  // EF Core用

    internal static EnrollmentStatusHistory Create(
        EnrollmentId enrollmentId,
        EnrollmentStatus status,
        string changedBy,
        string? reason = null,
        string? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(changedBy))
            throw new ValidationException("実行者は必須です");

        if (status == EnrollmentStatus.Cancelled && string.IsNullOrWhiteSpace(reason))
            throw new ValidationException("キャンセル理由は必須です");

        return new EnrollmentStatusHistory
        {
            Id = new EnrollmentStatusHistoryId(Guid.NewGuid()),
            EnrollmentId = enrollmentId,
            Status = status,
            ChangedAt = DateTime.UtcNow,
            ChangedBy = changedBy,
            Reason = reason,
            Metadata = metadata
        };
    }
}
```

### EnrollmentStatusHistoryId（値オブジェクト）

```csharp
public record EnrollmentStatusHistoryId
{
    public Guid Value { get; }

    public EnrollmentStatusHistoryId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("履歴IDは空にできません", nameof(value));
        Value = value;
    }
}
```

---

## データベーススキーマ（改訂版）

### enrollmentsテーブル（変更なし）

```sql
CREATE TABLE courses.enrollments (
    enrollment_id UUID PRIMARY KEY,
    student_id UUID NOT NULL,
    offering_id INT NOT NULL,
    status VARCHAR(20) NOT NULL CHECK (status IN ('Enrolled', 'Completed', 'Cancelled')),
    enrolled_at TIMESTAMP NOT NULL,
    completed_at TIMESTAMP,
    cancelled_at TIMESTAMP,
    CONSTRAINT fk_enrollments_student_id FOREIGN KEY (student_id) REFERENCES courses.students(student_id),
    CONSTRAINT fk_enrollments_offering_id FOREIGN KEY (offering_id) REFERENCES courses.course_offerings(offering_id)
);

CREATE UNIQUE INDEX ix_enrollments_student_offering ON courses.enrollments(student_id, offering_id);
CREATE INDEX ix_enrollments_student_id ON courses.enrollments(student_id);
CREATE INDEX ix_enrollments_offering_id ON courses.enrollments(offering_id);
CREATE INDEX ix_enrollments_status ON courses.enrollments(status);
```

### enrollment_status_historyテーブル（新規追加）

```sql
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

CREATE INDEX ix_enrollment_status_history_enrollment_id ON courses.enrollment_status_history(enrollment_id);
CREATE INDEX ix_enrollment_status_history_changed_at ON courses.enrollment_status_history(changed_at);
CREATE INDEX ix_enrollment_status_history_status ON courses.enrollment_status_history(status);
```

---

## 実装チェックリスト

### 新規追加が必要な項目:

- [ ] `EnrollmentStatusHistory`エンティティの作成
- [ ] `EnrollmentStatusHistoryId`値オブジェクトの作成
- [ ] `EnrollmentStatusHistoryConfiguration`（EF Core設定）の作成
- [ ] `V6__Create_EnrollmentStatusHistory.sql`マイグレーションファイルの作成
- [ ] `Enrollment`集約に`StatusHistory`コレクションを追加
- [ ] `Enrollment.Create()`メソッドに`enrolledBy`パラメータを追加し、初期履歴レコードを作成
- [ ] `Enrollment.Cancel()`メソッドに`cancelledBy`と`reason`パラメータを追加し、履歴レコードを作成
- [ ] `Enrollment.Complete()`メソッドに`completedBy`と`reason`パラメータを追加し、履歴レコードを作成
- [ ] `EnrollStudentCommand`に`EnrolledBy`パラメータを追加
- [ ] `CancelEnrollmentCommand`に`CancelledBy`と`Reason`パラメータを追加
- [ ] `CompleteEnrollmentCommand`に`CompletedBy`と`Reason`パラメータを追加
- [ ] 各CommandHandlerでStatusHistoryレコードを作成するロジックを実装
- [ ] APIコントローラーのリクエストDTOに対応するパラメータを追加

### 既存実装の修正が必要な項目:

- [ ] `CoursesDbContext`に`EnrollmentStatusHistory`のDbSetを追加
- [ ] `EnrollmentConfiguration`に`StatusHistory`コレクションのマッピングを追加
- [ ] `Program.cs`のDI設定（必要に応じて）

---

## 設計上の重要ポイント

1. **イミュータビリティ**: `EnrollmentStatusHistory`は一度作成したら変更・削除不可
2. **完全な監査証跡**: 誰が・いつ・なぜ変更したかを必ず記録
3. **集約境界**: `EnrollmentStatusHistory`は`Enrollment`集約の一部
4. **ステータス同期**: `Enrollment.Status`は常に最新の`StatusHistory`と一致
5. **キャンセル理由の強制**: Cancelledへの遷移時は理由が必須
6. **実行者の記録**: すべての状態変更に実行者IDを記録

---

**改訂日**: 2025年11月7日
**改訂理由**: Option 2（EnrollmentStatusHistoryによる完全な監査証跡実装）に合わせてユーザーストーリーを再定義
