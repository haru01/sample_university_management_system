# Attendance（出席管理）ユーザーストーリー

## 概要

このドキュメントは、出席管理システムのユーザーストーリーと受け入れ条件を定義します。
各ストーリーにはGiven-When-Then形式の受け入れ条件（Applicationレイヤーの統合テスト視点）と制約事項を記載しています。

**対象範囲:** Applicationレイヤーの統合テスト（CommandHandler, QueryHandler, Repository層を含む）

**実装パターン:** MediatRを使用したCQRSパターン

## CommandHandler/QueryHandler一覧

### 授業セッション管理 (Phase 1 - 未実装) → エピック1

| Handler | Command/Query | 説明 | 実装状態 |
|---------|--------------|------|----------|
| CreateClassSessionCommandHandler | CreateClassSessionCommand | 授業セッションを登録 | ✅ 実装済み |
| SelectClassSessionsByOfferingQueryHandler | SelectClassSessionsByOfferingQuery | コース開講の授業セッション一覧を取得 | ⬜ 未実装 |
| GetClassSessionQueryHandler | GetClassSessionQuery | 授業セッション詳細を取得 | ⬜ 未実装 |
| UpdateClassSessionCommandHandler | UpdateClassSessionCommand | 授業セッション情報を更新 | ⬜ 未実装 |
| CancelClassSessionCommandHandler | CancelClassSessionCommand | 授業セッションをキャンセル | ⬜ 未実装 |

### 出席記録管理 (Phase 2 - 未実装) → エピック2

| Handler | Command/Query | 説明 | 実装状態 |
|---------|--------------|------|----------|
| RecordAttendanceCommandHandler | RecordAttendanceCommand | 出席を記録 | ⬜ 未実装 |
| UpdateAttendanceCommandHandler | UpdateAttendanceCommand | 出席記録を更新 | ⬜ 未実装 |
| BulkRecordAttendanceCommandHandler | BulkRecordAttendanceCommand | 出席を一括記録 | ⬜ 未実装 |
| GetAttendanceBySessionQueryHandler | GetAttendanceBySessionQuery | セッションごとの出席記録一覧を取得 | ⬜ 未実装 |
| GetStudentAttendanceQueryHandler | GetStudentAttendanceQuery | 学生の出席記録一覧を取得 | ⬜ 未実装 |

### 出席統計 (Phase 3 - 未実装) → エピック3

| Handler | Command/Query | 説明 | 実装状態 |
|---------|--------------|------|----------|
| GetAttendanceStatisticsByOfferingQueryHandler | GetAttendanceStatisticsByOfferingQuery | コース開講の出席統計を取得 | ⬜ 未実装 |
| GetStudentAttendanceStatisticsQueryHandler | GetStudentAttendanceStatisticsQuery | 学生の出席統計を取得 | ⬜ 未実装 |

---

## エピック1: 授業セッション管理

### US-CS01: 授業セッションを登録できる

**ストーリー:**
API利用者として、コース開講に対する授業セッション（授業回）を登録できるようにしたい。なぜなら、出席を記録するためには授業セッションの情報が必要だから。

**Handler:** `CreateClassSessionCommandHandler : IRequestHandler<CreateClassSessionCommand, int>`

**受け入れ条件:**

```gherkin
Scenario: 有効な授業セッション情報で新しいセッションを登録する
  Given データベースにOfferingId 1 のCourseOfferingが存在する
  And CourseOfferingのStatusが "Active" である
  When CreateClassSessionCommandを実行する
    - OfferingId: 1
    - SessionNumber: 1
    - SessionDate: 2024-04-10
    - StartTime: "09:00"
    - EndTime: "10:30"
    - Location: "A棟201教室"
    - Topic: "プログラミング基礎：変数とデータ型"
  Then SessionIdが返される
  And データベースにClassSessionが保存されている
  And セッション番号が 1 である
  And 開催日が 2024-04-10 である
  And ステータスが "Scheduled" である
```

```gherkin
Scenario: 存在しないOfferingIdでセッションを登録しようとする
  Given データベースにOfferingId 999 のCourseOfferingが存在しない
  When CreateClassSessionCommandを実行する
    - OfferingId: 999
    - SessionNumber: 1
    - SessionDate: 2024-04-10
  Then KeyNotFoundException がスローされる
  And エラーメッセージに "CourseOffering not found" が含まれる
```

```gherkin
Scenario: 同じコース開講で重複するセッション番号で登録を試みる
  Given データベースにOfferingId 1 のCourseOfferingが存在する
  And OfferingId 1 にSessionNumber 1 のClassSessionが既に存在する
  When CreateClassSessionCommandを実行する
    - OfferingId: 1
    - SessionNumber: 1
    - SessionDate: 2024-04-15
  Then InvalidOperationException がスローされる
  And エラーメッセージに "Session number already exists for this offering" が含まれる
```

```gherkin
Scenario: 開始時刻が終了時刻より後の時刻でセッションを登録しようとする
  Given データベースにOfferingId 1 のCourseOfferingが存在する
  When CreateClassSessionCommandを実行する
    - OfferingId: 1
    - SessionNumber: 1
    - SessionDate: 2024-04-10
    - StartTime: "10:30"
    - EndTime: "09:00"
  Then ArgumentException がスローされる
  And エラーメッセージに "End time must be after start time" が含まれる
```

```gherkin
Scenario: 学期期間外の日付でセッションを登録しようとする
  Given データベースにOfferingId 1 のCourseOfferingが存在する
  And OfferingIdの学期期間が 2024-04-01 から 2024-09-30 である
  When CreateClassSessionCommandを実行する
    - OfferingId: 1
    - SessionNumber: 1
    - SessionDate: 2024-10-01
  Then InvalidOperationException がスローされる
  And エラーメッセージに "Session date must be within semester period" が含まれる
```

**制約:**

- OfferingId + SessionNumberの組み合わせは一意
- セッション番号: 1以上の整数
- 開催日: 学期の開始日〜終了日の範囲内
- 時刻: StartTime < EndTime
- 教室: オプション、最大50文字
- トピック: オプション、最大200文字
- 初期ステータス: "Scheduled"

**実装状態:** ⬜ 未実装

---

### US-CS02: コース開講の授業セッション一覧を取得できる

**ストーリー:**
API利用者として、特定のコース開講に紐づく授業セッション一覧を取得できるようにしたい。なぜなら、教員が授業計画を確認したり、学生が授業スケジュールを確認する必要があるから。

**Handler:** `SelectClassSessionsByOfferingQueryHandler : IRequestHandler<SelectClassSessionsByOfferingQuery, List<ClassSessionDto>>`

**受け入れ条件:**

```gherkin
Scenario: 特定のコース開講の全授業セッションを取得する
  Given データベースにOfferingId 1 のCourseOfferingが存在する
  And OfferingId 1 に以下のClassSessionが存在する
    | SessionNumber | SessionDate | StartTime | EndTime | Status    |
    | 1             | 2024-04-10  | 09:00     | 10:30   | Scheduled |
    | 2             | 2024-04-17  | 09:00     | 10:30   | Scheduled |
    | 3             | 2024-04-24  | 09:00     | 10:30   | Cancelled |
  When SelectClassSessionsByOfferingQueryを実行する
    - OfferingId: 1
  Then 3件のClassSessionDtoが返される
  And SessionDateの昇順でソートされている
```

```gherkin
Scenario: ステータスでフィルタリングしてセッションを取得する
  Given データベースにOfferingId 1 のCourseOfferingが存在する
  And OfferingId 1 に複数のStatusのClassSessionが存在する
  When SelectClassSessionsByOfferingQueryを実行する
    - OfferingId: 1
    - StatusFilter: "Scheduled"
  Then Statusが "Scheduled" のClassSessionDtoのみが返される
```

```gherkin
Scenario: 日付範囲でフィルタリングしてセッションを取得する
  Given データベースにOfferingId 1 のCourseOfferingが存在する
  And OfferingId 1 に異なる日付のClassSessionが存在する
  When SelectClassSessionsByOfferingQueryを実行する
    - OfferingId: 1
    - FromDate: 2024-04-15
    - ToDate: 2024-04-30
  Then 2024-04-15 から 2024-04-30 の範囲のClassSessionDtoのみが返される
```

```gherkin
Scenario: セッションが1件も登録されていないコース開講
  Given データベースにOfferingId 1 のCourseOfferingが存在する
  And OfferingId 1 にClassSessionが存在しない
  When SelectClassSessionsByOfferingQueryを実行する
    - OfferingId: 1
  Then 空のリスト（0件）が返される
```

**制約:**

- デフォルトソート: SessionDateの昇順、同日の場合はStartTimeの昇順
- StatusFilterパラメータはオプション（未指定時は全ステータスを返す）
- FromDate/ToDateパラメータはオプション（未指定時は全期間）

**実装状態:** ⬜ 未実装

---

### US-CS03: 授業セッション詳細を取得できる

**ストーリー:**
API利用者として、SessionIdを指定して特定の授業セッションの詳細情報を取得できるようにしたい。なぜなら、出席記録画面で授業の詳細を表示する必要があるから。

**Handler:** `GetClassSessionQueryHandler : IRequestHandler<GetClassSessionQuery, ClassSessionDto>`

**受け入れ条件:**

```gherkin
Scenario: 存在するSessionIdでセッション詳細を取得する
  Given データベースに以下のClassSessionが存在する
    - SessionId: 1
    - OfferingId: 1
    - SessionNumber: 1
    - SessionDate: 2024-04-10
    - StartTime: "09:00"
    - EndTime: "10:30"
    - Location: "A棟201教室"
    - Topic: "プログラミング基礎：変数とデータ型"
    - Status: "Scheduled"
  When GetClassSessionQueryを実行する
    - SessionId: 1
  Then ClassSessionDtoが返される
  And SessionIdが 1 である
  And SessionNumberが 1 である
  And 開催日が 2024-04-10 である
```

```gherkin
Scenario: 存在しないSessionIdで取得を試みる
  Given データベースにSessionId 999 のClassSessionが存在しない
  When GetClassSessionQueryを実行する
    - SessionId: 999
  Then KeyNotFoundException がスローされる
  And エラーメッセージに "ClassSession not found" が含まれる
```

**制約:**

- ClassSessionDtoにはCourseOffering情報（CourseCode, CourseName）も含める
- Statusに関わらず全てのセッション情報を返す

**実装状態:** ⬜ 未実装

---

### US-CS04: 授業セッション情報を更新できる

**ストーリー:**
API利用者として、既に登録された授業セッションの情報を更新できるようにしたい。なぜなら、教室変更や日程変更が発生する場合があるから。

**Handler:** `UpdateClassSessionCommandHandler : IRequestHandler<UpdateClassSessionCommand, Unit>`

**受け入れ条件:**

```gherkin
Scenario: 既存の授業セッション情報を更新する
  Given データベースに以下のClassSessionが存在する
    - SessionId: 1
    - OfferingId: 1
    - SessionNumber: 1
    - SessionDate: 2024-04-10
    - StartTime: "09:00"
    - EndTime: "10:30"
    - Location: "A棟201教室"
    - Status: "Scheduled"
  When UpdateClassSessionCommandを実行する
    - SessionId: 1
    - SessionDate: 2024-04-11
    - StartTime: "10:00"
    - EndTime: "11:30"
    - Location: "B棟305教室"
    - Topic: "プログラミング基礎：変数とデータ型（補講）"
  Then 更新が成功する
  And データベースのClassSessionが更新されている
  And 開催日が 2024-04-11 である
  And 教室が "B棟305教室" である
```

```gherkin
Scenario: 存在しないSessionIdで更新を試みる
  Given データベースにSessionId 999 のClassSessionが存在しない
  When UpdateClassSessionCommandを実行する
    - SessionId: 999
  Then KeyNotFoundException がスローされる
  And エラーメッセージに "ClassSession not found" が含まれる
```

```gherkin
Scenario: 完了済みのセッションを更新しようとする
  Given データベースに以下のClassSessionが存在する
    - SessionId: 1
    - Status: "Completed"
  When UpdateClassSessionCommandを実行する
    - SessionId: 1
    - Location: "C棟101教室"
  Then InvalidOperationException がスローされる
  And エラーメッセージに "Cannot update completed session" が含まれる
```

**制約:**

- OfferingIdとSessionNumberは変更不可
- Statusが"Completed"または"Cancelled"の場合は更新不可
- 時刻: StartTime < EndTime
- 開催日: 学期の開始日〜終了日の範囲内

**実装状態:** ⬜ 未実装

---

### US-CS05: 授業セッションをキャンセルできる

**ストーリー:**
API利用者として、予定されている授業セッションをキャンセルできるようにしたい。なぜなら、教員の都合や祝日などで休講になる場合があるから。

**Handler:** `CancelClassSessionCommandHandler : IRequestHandler<CancelClassSessionCommand, Unit>`

**受け入れ条件:**

```gherkin
Scenario: Scheduledな授業セッションをキャンセルする
  Given データベースに以下のClassSessionが存在する
    - SessionId: 1
    - Status: "Scheduled"
  When CancelClassSessionCommandを実行する
    - SessionId: 1
    - Reason: "教員体調不良のため"
  Then キャンセルが成功する
  And データベースのClassSessionのStatusが "Cancelled" に更新されている
  And キャンセル理由が記録されている
```

```gherkin
Scenario: 既にキャンセル済みのセッションをキャンセルしようとする
  Given データベースに以下のClassSessionが存在する
    - SessionId: 1
    - Status: "Cancelled"
  When CancelClassSessionCommandを実行する
    - SessionId: 1
  Then InvalidOperationException がスローされる
  And エラーメッセージに "already cancelled" が含まれる
```

```gherkin
Scenario: 完了済みのセッションをキャンセルしようとする
  Given データベースに以下のClassSessionが存在する
    - SessionId: 1
    - Status: "Completed"
  When CancelClassSessionCommandを実行する
    - SessionId: 1
  Then InvalidOperationException がスローされる
  And エラーメッセージに "Cannot cancel completed session" が含まれる
```

```gherkin
Scenario: 出席記録が既に存在するセッションをキャンセルしようとする
  Given データベースに以下のClassSessionが存在する
    - SessionId: 1
    - Status: "Scheduled"
  And SessionId 1 に出席記録が既に存在する
  When CancelClassSessionCommandを実行する
    - SessionId: 1
  Then InvalidOperationException がスローされる
  And エラーメッセージに "Cannot cancel session with attendance records" が含まれる
```

**制約:**

- キャンセル可能なステータス: "Scheduled"のみ
- 出席記録が1件でも存在する場合はキャンセル不可
- キャンセル理由: オプション、最大200文字
- キャンセル後の再有効化は不可

**実装状態:** ⬜ 未実装

---

## エピック2: 出席記録管理

### US-AT01: 出席を記録できる

**ストーリー:**
API利用者として、特定の授業セッションに対して学生の出席を記録できるようにしたい。なぜなら、出席管理は成績評価の重要な要素だから。

**Handler:** `RecordAttendanceCommandHandler : IRequestHandler<RecordAttendanceCommand, int>`

**受け入れ条件:**

```gherkin
Scenario: 有効な出席情報で新しい出席記録を登録する
  Given データベースにSessionId 1 のClassSessionが存在する
  And ClassSessionのStatusが "Scheduled" である
  And データベースに学生ID "student-001" の学生が登録されている
  And 学生がSessionIdのCourseOfferingに履修登録している
  When RecordAttendanceCommandを実行する
    - SessionId: 1
    - StudentId: "student-001"
    - Status: "Present"
    - RecordedAt: 2024-04-10T09:05:00
  Then AttendanceIdが返される
  And データベースにAttendanceが保存されている
  And 出席ステータスが "Present" である
```

```gherkin
Scenario: 履修登録していない学生の出席を記録しようとする
  Given データベースにSessionId 1 のClassSessionが存在する
  And データベースに学生ID "student-001" の学生が登録されている
  And 学生がSessionIdのCourseOfferingに履修登録していない
  When RecordAttendanceCommandを実行する
    - SessionId: 1
    - StudentId: "student-001"
    - Status: "Present"
  Then InvalidOperationException がスローされる
  And エラーメッセージに "Student not enrolled in this course offering" が含まれる
```

```gherkin
Scenario: 同じセッションに既に出席記録が存在する学生の記録を試みる
  Given データベースにSessionId 1 のClassSessionが存在する
  And 学生ID "student-001" のSessionId 1 に対する出席記録が既に存在する
  When RecordAttendanceCommandを実行する
    - SessionId: 1
    - StudentId: "student-001"
    - Status: "Present"
  Then InvalidOperationException がスローされる
  And エラーメッセージに "Attendance already recorded for this session" が含まれる
```

```gherkin
Scenario: キャンセル済みのセッションに出席を記録しようとする
  Given データベースに以下のClassSessionが存在する
    - SessionId: 1
    - Status: "Cancelled"
  When RecordAttendanceCommandを実行する
    - SessionId: 1
    - StudentId: "student-001"
    - Status: "Present"
  Then InvalidOperationException がスローされる
  And エラーメッセージに "Cannot record attendance for cancelled session" が含まれる
```

```gherkin
Scenario: 存在しない学生IDで出席を記録しようとする
  Given データベースにSessionId 1 のClassSessionが存在する
  And データベースに学生ID "student-999" の学生が存在しない
  When RecordAttendanceCommandを実行する
    - SessionId: 1
    - StudentId: "student-999"
    - Status: "Present"
  Then KeyNotFoundException がスローされる
  And エラーメッセージに "Student not found" が含まれる
```

**制約:**

- SessionId + StudentIdの組み合わせは一意
- 出席ステータス: "Present"（出席）、"Absent"（欠席）、"Late"（遅刻）、"Excused"（公欠）
- 学生は対象のCourseOfferingに履修登録している必要がある
- ClassSessionのStatusが"Scheduled"または"Completed"である必要がある
- RecordedAt: オプション、未指定時は現在日時
- メモ: オプション、最大500文字

**実装状態:** ⬜ 未実装

---

### US-AT02: 出席記録を更新できる

**ストーリー:**
API利用者として、既に記録された出席情報を更新できるようにしたい。なぜなら、記録ミスや遅刻から出席への変更などがあるから。

**Handler:** `UpdateAttendanceCommandHandler : IRequestHandler<UpdateAttendanceCommand, Unit>`

**受け入れ条件:**

```gherkin
Scenario: 既存の出席記録を更新する
  Given データベースに以下のAttendanceが存在する
    - AttendanceId: 1
    - SessionId: 1
    - StudentId: "student-001"
    - Status: "Absent"
  When UpdateAttendanceCommandを実行する
    - AttendanceId: 1
    - Status: "Present"
    - Note: "記録ミスのため訂正"
  Then 更新が成功する
  And データベースのAttendanceが更新されている
  And ステータスが "Present" である
  And メモが記録されている
```

```gherkin
Scenario: 存在しないAttendanceIdで更新を試みる
  Given データベースにAttendanceId 999 のAttendanceが存在しない
  When UpdateAttendanceCommandを実行する
    - AttendanceId: 999
    - Status: "Present"
  Then KeyNotFoundException がスローされる
  And エラーメッセージに "Attendance not found" が含まれる
```

```gherkin
Scenario: セッション終了から一定期間後に更新を試みる
  Given データベースに以下のAttendanceが存在する
    - AttendanceId: 1
    - SessionId: 1
  And SessionId 1 の開催日が 2024-04-10 である
  And 現在日時が 2024-05-20 である（30日以上経過）
  When UpdateAttendanceCommandを実行する
    - AttendanceId: 1
    - Status: "Present"
  Then InvalidOperationException がスローされる
  And エラーメッセージに "Cannot update attendance after 30 days" が含まれる
```

**制約:**

- SessionIdとStudentIdは変更不可
- セッション開催日から30日以内のみ更新可能
- 出席ステータス: "Present"、"Absent"、"Late"、"Excused"

**実装状態:** ⬜ 未実装

---

### US-AT03: 出席を一括記録できる

**ストーリー:**
API利用者として、特定の授業セッションに対して複数の学生の出席を一括で記録できるようにしたい。なぜなら、1人ずつ記録するのは非効率だから。

**Handler:** `BulkRecordAttendanceCommandHandler : IRequestHandler<BulkRecordAttendanceCommand, BulkRecordAttendanceResult>`

**受け入れ条件:**

```gherkin
Scenario: セッションの全履修登録者の出席を一括記録する
  Given データベースにSessionId 1 のClassSessionが存在する
  And OfferingId 1 に以下の学生が履修登録している
    | StudentId   |
    | student-001 |
    | student-002 |
    | student-003 |
  When BulkRecordAttendanceCommandを実行する
    - SessionId: 1
    - Attendances:
      - StudentId: "student-001", Status: "Present"
      - StudentId: "student-002", Status: "Absent"
      - StudentId: "student-003", Status: "Late"
  Then 3件のAttendanceが作成される
  And 結果のTotalRecordedが 3 である
  And 結果のFailedが 0 である
```

```gherkin
Scenario: 一部の学生が履修登録していない場合
  Given データベースにSessionId 1 のClassSessionが存在する
  And 学生ID "student-001" がOfferingId 1 に履修登録している
  And 学生ID "student-002" がOfferingId 1 に履修登録していない
  When BulkRecordAttendanceCommandを実行する
    - SessionId: 1
    - Attendances:
      - StudentId: "student-001", Status: "Present"
      - StudentId: "student-002", Status: "Present"
  Then 1件のAttendanceが作成される
  And 結果のTotalRecordedが 1 である
  And 結果のFailedが 1 である
  And 結果のErrorsに "student-002" のエラーが含まれる
```

```gherkin
Scenario: 既に出席記録が存在する学生がリストに含まれる場合
  Given データベースにSessionId 1 のClassSessionが存在する
  And 学生ID "student-001" のSessionId 1 に対する出席記録が既に存在する
  And 学生ID "student-002" がOfferingId 1 に履修登録している
  When BulkRecordAttendanceCommandを実行する
    - SessionId: 1
    - Attendances:
      - StudentId: "student-001", Status: "Present"
      - StudentId: "student-002", Status: "Present"
  Then 1件のAttendanceが作成される（student-002のみ）
  And 結果のTotalRecordedが 1 である
  And 結果のSkippedが 1 である（student-001は既存）
```

**制約:**

- 既に記録が存在する学生はスキップ
- 履修登録していない学生はエラーリストに追加
- 存在しない学生IDはエラーリストに追加
- トランザクション: 全体がロールバックされるのではなく、成功したものは保存

**Command定義:**

```csharp
public record BulkRecordAttendanceCommand : IRequest<BulkRecordAttendanceResult>
{
    public int SessionId { get; init; }
    public List<AttendanceRecord> Attendances { get; init; } = new();
}

public class AttendanceRecord
{
    public Guid StudentId { get; init; }
    public string Status { get; init; } = string.Empty;
    public string? Note { get; init; }
}

public class BulkRecordAttendanceResult
{
    public int TotalRecorded { get; init; }
    public int Skipped { get; init; }
    public int Failed { get; init; }
    public List<string> Errors { get; init; } = new();
}
```

**実装状態:** ⬜ 未実装

---

### US-AT04: セッションごとの出席記録一覧を取得できる

**ストーリー:**
API利用者として、特定の授業セッションの出席記録一覧を取得できるようにしたい。なぜなら、教員がその回の出席状況を確認する必要があるから。

**Handler:** `GetAttendanceBySessionQueryHandler : IRequestHandler<GetAttendanceBySessionQuery, List<AttendanceDto>>`

**受け入れ条件:**

```gherkin
Scenario: 特定セッションの全出席記録を取得する
  Given データベースにSessionId 1 のClassSessionが存在する
  And SessionId 1 に以下のAttendanceが存在する
    | StudentId   | StudentName | Status  |
    | student-001 | 山田太郎    | Present |
    | student-002 | 鈴木花子    | Absent  |
    | student-003 | 田中次郎    | Late    |
  When GetAttendanceBySessionQueryを実行する
    - SessionId: 1
  Then 3件のAttendanceDtoが返される
  And 学生名の昇順でソートされている
```

```gherkin
Scenario: ステータスでフィルタリングして出席記録を取得する
  Given データベースにSessionId 1 のClassSessionが存在する
  And SessionId 1 に複数のStatusのAttendanceが存在する
  When GetAttendanceBySessionQueryを実行する
    - SessionId: 1
    - StatusFilter: "Absent"
  Then Statusが "Absent" のAttendanceDtoのみが返される
```

```gherkin
Scenario: 出席記録が1件も登録されていないセッション
  Given データベースにSessionId 1 のClassSessionが存在する
  And SessionId 1 にAttendanceが存在しない
  When GetAttendanceBySessionQueryを実行する
    - SessionId: 1
  Then 空のリスト（0件）が返される
```

**制約:**

- AttendanceDtoには学生情報（Name, Email）も含める
- デフォルトソート: 学生名の昇順
- StatusFilterパラメータはオプション

**実装状態:** ⬜ 未実装

---

### US-AT05: 学生の出席記録一覧を取得できる

**ストーリー:**
API利用者として、特定の学生の出席記録一覧を取得できるようにしたい。なぜなら、学生が自分の出席状況を確認したり、教員が特定の学生の出席履歴を確認する必要があるから。

**Handler:** `GetStudentAttendanceQueryHandler : IRequestHandler<GetStudentAttendanceQuery, List<AttendanceDto>>`

**受け入れ条件:**

```gherkin
Scenario: 学生の全出席記録を取得する
  Given データベースに学生ID "student-001" の学生が登録されている
  And 学生ID "student-001" に以下のAttendanceが存在する
    | SessionId | CourseCode | SessionDate | Status  |
    | 1         | CS101      | 2024-04-10  | Present |
    | 2         | CS101      | 2024-04-17  | Present |
    | 3         | MATH201    | 2024-04-11  | Absent  |
  When GetStudentAttendanceQueryを実行する
    - StudentId: "student-001"
  Then 3件のAttendanceDtoが返される
  And SessionDateの降順でソートされている（最新が先頭）
```

```gherkin
Scenario: 特定のコース開講でフィルタリングして取得する
  Given 学生ID "student-001" が複数のCourseOfferingに履修登録している
  When GetStudentAttendanceQueryを実行する
    - StudentId: "student-001"
    - OfferingId: 1
  Then OfferingId 1 のAttendanceDtoのみが返される
```

```gherkin
Scenario: 学期でフィルタリングして取得する
  Given 学生ID "student-001" が複数の学期のCourseOfferingに履修登録している
  When GetStudentAttendanceQueryを実行する
    - StudentId: "student-001"
    - SemesterId: (2024, Spring)
  Then 2024年Spring学期のAttendanceDtoのみが返される
```

```gherkin
Scenario: 出席記録が存在しない学生の一覧を取得する
  Given データベースに学生ID "student-001" の学生が登録されている
  And 学生ID "student-001" にAttendanceが存在しない
  When GetStudentAttendanceQueryを実行する
    - StudentId: "student-001"
  Then 空のリスト（0件）が返される
```

**制約:**

- AttendanceDtoにはClassSession情報（SessionDate, CourseCode, CourseName）も含める
- デフォルトソート: SessionDateの降順（最新が先頭）
- OfferingIdFilterパラメータはオプション
- SemesterIdFilterパラメータはオプション

**実装状態:** ⬜ 未実装

---

## エピック3: 出席統計

### US-AS01: コース開講の出席統計を取得できる

**ストーリー:**
API利用者として、特定のコース開講の出席統計を取得できるようにしたい。なぜなら、教員がコース全体の出席状況を把握する必要があるから。

**Handler:** `GetAttendanceStatisticsByOfferingQueryHandler : IRequestHandler<GetAttendanceStatisticsByOfferingQuery, AttendanceStatisticsDto>`

**受け入れ条件:**

```gherkin
Scenario: コース開講の出席統計を取得する
  Given データベースにOfferingId 1 のCourseOfferingが存在する
  And OfferingId 1 に以下の履修登録者がいる
    | StudentId   |
    | student-001 |
    | student-002 |
    | student-003 |
  And OfferingId 1 に以下のClassSessionが存在する
    | SessionId | SessionNumber |
    | 1         | 1             |
    | 2         | 2             |
  And 以下のAttendanceが存在する
    | SessionId | StudentId   | Status  |
    | 1         | student-001 | Present |
    | 1         | student-002 | Absent  |
    | 1         | student-003 | Present |
    | 2         | student-001 | Present |
    | 2         | student-002 | Present |
    | 2         | student-003 | Late    |
  When GetAttendanceStatisticsByOfferingQueryを実行する
    - OfferingId: 1
  Then AttendanceStatisticsDtoが返される
  And TotalStudentsが 3 である
  And TotalSessionsが 2 である
  And TotalPresentが 4 である（Present: 4件）
  And TotalAbsentが 1 である（Absent: 1件）
  And TotalLateが 1 である（Late: 1件）
  And AverageAttendanceRateが約 83.3% である（5/6）
```

```gherkin
Scenario: 出席記録が存在しないコース開講の統計を取得する
  Given データベースにOfferingId 1 のCourseOfferingが存在する
  And OfferingId 1 にClassSessionが存在する
  And OfferingId 1 にAttendanceが存在しない
  When GetAttendanceStatisticsByOfferingQueryを実行する
    - OfferingId: 1
  Then AttendanceStatisticsDtoが返される
  And TotalPresentが 0 である
  And AverageAttendanceRateが 0% である
```

**制約:**

- 出席率計算: (Present + Late) / (Present + Absent + Late + Excused)
- Excusedは出席扱い
- StudentListオプション: 各学生の出席率も含める

**DTO定義:**

```csharp
public class AttendanceStatisticsDto
{
    public int OfferingId { get; init; }
    public int TotalStudents { get; init; }
    public int TotalSessions { get; init; }
    public int TotalPresent { get; init; }
    public int TotalAbsent { get; init; }
    public int TotalLate { get; init; }
    public int TotalExcused { get; init; }
    public decimal AverageAttendanceRate { get; init; }
    public List<StudentAttendanceSummary>? StudentList { get; init; }
}

public class StudentAttendanceSummary
{
    public Guid StudentId { get; init; }
    public string StudentName { get; init; } = string.Empty;
    public int PresentCount { get; init; }
    public int AbsentCount { get; init; }
    public int LateCount { get; init; }
    public decimal AttendanceRate { get; init; }
}
```

**実装状態:** ⬜ 未実装

---

### US-AS02: 学生の出席統計を取得できる

**ストーリー:**
API利用者として、特定の学生の出席統計を取得できるようにしたい。なぜなら、学生が自分の出席率を確認したり、教員が個別指導の参考にする必要があるから。

**Handler:** `GetStudentAttendanceStatisticsQueryHandler : IRequestHandler<GetStudentAttendanceStatisticsQuery, StudentAttendanceStatisticsDto>`

**受け入れ条件:**

```gherkin
Scenario: 学生の全体出席統計を取得する
  Given データベースに学生ID "student-001" の学生が登録されている
  And 学生ID "student-001" が以下のCourseOfferingに履修登録している
    | OfferingId | CourseCode |
    | 1          | CS101      |
    | 2          | MATH201    |
  And 学生ID "student-001" に以下のAttendanceが存在する
    | SessionId | OfferingId | Status  |
    | 1         | 1          | Present |
    | 2         | 1          | Present |
    | 3         | 1          | Absent  |
    | 4         | 2          | Present |
    | 5         | 2          | Late    |
  When GetStudentAttendanceStatisticsQueryを実行する
    - StudentId: "student-001"
  Then StudentAttendanceStatisticsDtoが返される
  And TotalPresentが 3 である
  And TotalAbsentが 1 である
  And TotalLateが 1 である
  And OverallAttendanceRateが 80% である（4/5）
```

```gherkin
Scenario: 特定の学期の出席統計を取得する
  Given 学生ID "student-001" が複数の学期のCourseOfferingに履修登録している
  When GetStudentAttendanceStatisticsQueryを実行する
    - StudentId: "student-001"
    - SemesterId: (2024, Spring)
  Then 2024年Spring学期のAttendanceのみが集計される
```

```gherkin
Scenario: コースごとの内訳を含めて取得する
  Given 学生ID "student-001" が複数のCourseOfferingに履修登録している
  When GetStudentAttendanceStatisticsQueryを実行する
    - StudentId: "student-001"
    - IncludeCourseBreakdown: true
  Then CourseBreakdownリストが含まれる
  And 各コースの出席統計が返される
```

**制約:**

- 出席率計算: (Present + Late + Excused) / (Present + Absent + Late + Excused)
- SemesterIdFilterパラメータはオプション
- IncludeCourseBreakdownオプション: コース別の統計も含める

**DTO定義:**

```csharp
public class StudentAttendanceStatisticsDto
{
    public Guid StudentId { get; init; }
    public string StudentName { get; init; } = string.Empty;
    public int TotalPresent { get; init; }
    public int TotalAbsent { get; init; }
    public int TotalLate { get; init; }
    public int TotalExcused { get; init; }
    public decimal OverallAttendanceRate { get; init; }
    public List<CourseAttendanceBreakdown>? CourseBreakdown { get; init; }
}

public class CourseAttendanceBreakdown
{
    public int OfferingId { get; init; }
    public string CourseCode { get; init; } = string.Empty;
    public string CourseName { get; init; } = string.Empty;
    public int PresentCount { get; init; }
    public int AbsentCount { get; init; }
    public int LateCount { get; init; }
    public decimal AttendanceRate { get; init; }
}
```

**実装状態:** ⬜ 未実装

---

## ドメインルール・制約まとめ

### 授業セッション（ClassSession）

- **エンティティ構造**:
  - SessionId: INT（自動生成、主キー）
  - OfferingId: INT（外部キー、CourseOffering）
  - SessionNumber: INT（1以上、コース内で一意）
  - SessionDate: DATE（学期期間内）
  - StartTime: TIME
  - EndTime: TIME
  - Location: VARCHAR(50)（オプション）
  - Topic: VARCHAR(200)（オプション）
  - Status: ENUM（Scheduled, Completed, Cancelled）
- **一意制約**: OfferingId + SessionNumber
- **時刻制約**: StartTime < EndTime
- **日付制約**: SessionDateは学期の開始日〜終了日の範囲内
- **ステータス遷移**:
  - `Scheduled` → `Completed` ✅
  - `Scheduled` → `Cancelled` ✅（出席記録なしの場合のみ）
  - `Completed` → (変更不可) ❌
  - `Cancelled` → (変更不可) ❌

### 出席記録（Attendance）

- **エンティティ構造**:
  - AttendanceId: INT（自動生成、主キー）
  - SessionId: INT（外部キー、ClassSession）
  - StudentId: UUID（外部キー、Student）
  - Status: ENUM（Present, Absent, Late, Excused）
  - RecordedAt: DATETIME（記録日時）
  - Note: VARCHAR(500)（オプション、メモ）
- **一意制約**: SessionId + StudentId（1セッション1学生1記録）
- **履修登録チェック**: StudentはSessionのOfferingIdに履修登録している必要がある
- **セッション状態チェック**: ClassSessionのStatusが"Scheduled"または"Completed"である必要がある
- **出席ステータス**:
  - `Present`: 出席
  - `Absent`: 欠席
  - `Late`: 遅刻（出席扱い）
  - `Excused`: 公欠（出席扱い）
- **更新制限**: セッション開催日から30日以内のみ更新可能

### 出席統計

- **出席率計算式**:
  - 出席率 = (Present + Late + Excused) / (Present + Absent + Late + Excused) × 100
  - 分母が0の場合は0%
- **集計単位**:
  - コース開講単位: OfferingId
  - 学生単位: StudentId
  - 学期単位: SemesterId（オプションフィルタ）

---

## 実装優先順位

### Phase 1: 授業セッション管理 → エピック1

**優先順位1: コア機能**

- ⬜ US-CS01: 授業セッションを登録
- ⬜ US-CS02: 授業セッション一覧を取得
- ⬜ US-CS03: 授業セッション詳細を取得

**優先順位2: 管理機能**

- ⬜ US-CS04: 授業セッション情報を更新
- ⬜ US-CS05: 授業セッションをキャンセル

**理由**: 出席記録の前提となるセッション情報。コース開講に依存。

### Phase 2: 出席記録管理 → エピック2

**優先順位1: コア機能**

- ⬜ US-AT01: 出席を記録
- ⬜ US-AT04: セッションごとの出席記録一覧を取得
- ⬜ US-AT05: 学生の出席記録一覧を取得

**優先順位2: 管理機能**

- ⬜ US-AT02: 出席記録を更新
- ⬜ US-AT03: 出席を一括記録

**理由**: セッション管理に依存。一括記録は効率化のため後回し可能。

### Phase 3: 出席統計 → エピック3

- ⬜ US-AS01: コース開講の出席統計を取得
- ⬜ US-AS02: 学生の出席統計を取得

**理由**: 出席記録が蓄積された後に有用。分析・レポート機能。

---

## 依存関係

### 外部エンティティへの依存

- **CourseOffering**: ClassSessionはOfferingIdを参照
- **Student**: AttendanceはStudentIdを参照
- **Enrollment**: 出席記録時に履修登録状態をチェック
- **Semester**: セッション日付の妥当性チェック

### エンティティ間の依存

```
CourseOffering (Phase 4)
    ↓
ClassSession (Phase 1)
    ↓
Attendance (Phase 2)
    ↓
AttendanceStatistics (Phase 3)
```

---

## テスト実装戦略

詳細は [testing-strategy.md](impl-patterns/testing-strategy.md) を参照してください。

**テストの重点項目:**

1. **授業セッション管理**
   - OfferingId + SessionNumberの一意性
   - 学期期間内の日付バリデーション
   - 時刻順序のバリデーション
   - ステータス遷移の制約

2. **出席記録管理**
   - SessionId + StudentIdの一意性
   - 履修登録状態のチェック
   - セッションステータスのチェック
   - 重複記録の防止

3. **一括処理**
   - 部分的成功のハンドリング
   - エラーレポートの正確性
   - トランザクション境界

4. **統計計算**
   - 出席率の計算ロジック
   - 各種ステータスの集計
   - フィルタリング条件の正確性
