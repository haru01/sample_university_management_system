# Attendance（出席管理）ユーザーストーリー

## 概要

このドキュメントは、出席管理システムのユーザーストーリーと受け入れ条件を定義します。
各ストーリーにはGiven-When-Then形式の受け入れ条件（Applicationレイヤーの統合テスト視点）と制約事項を記載しています。

**対象範囲:** Applicationレイヤーの統合テスト（CommandHandler, QueryHandler, Repository層を含む）

**実装パターン:** MediatRを使用したCQRSパターン

**前提条件:**

- 履修管理コンテキスト（Enrollments）が実装済みであること
- 学生、コース、学期、履修登録のデータが存在すること

## CommandHandler/QueryHandler一覧

### 授業セッション管理 (Phase 1 - 未実装)

| Handler | Command/Query | 説明 | 実装状態 |
|---------|--------------|------|----------|
| CreateSessionCommandHandler | CreateSessionCommand | 授業セッションを作成 | ⬜ 未実装 |
| GetCourseSessionsQueryHandler | GetCourseSessionsQuery | コースの授業セッション一覧を取得 | ⬜ 未実装 |
| GetSessionQueryHandler | GetSessionQuery | 授業セッションを取得 | ⬜ 未実装 |
| UpdateSessionCommandHandler | UpdateSessionCommand | 授業セッション情報を更新 | ⬜ 未実装 |

### 出席記録管理 (Phase 2 - 未実装)

| Handler | Command/Query | 説明 | 実装状態 |
|---------|--------------|------|----------|
| RecordAttendanceCommandHandler | RecordAttendanceCommand | 出席を記録 | ⬜ 未実装 |
| UpdateAttendanceCommandHandler | UpdateAttendanceCommand | 出席状態を更新 | ⬜ 未実装 |
| GetSessionAttendancesQueryHandler | GetSessionAttendancesQuery | セッションの出席記録一覧を取得 | ⬜ 未実装 |
| GetStudentAttendancesQueryHandler | GetStudentAttendancesQuery | 学生の出席記録一覧を取得 | ⬜ 未実装 |

### 出席統計 (Phase 3 - 未実装)

| Handler | Query | 説明 | 実装状態 |
|---------|------|------|----------|
| GetStudentAttendanceRateQueryHandler | GetStudentAttendanceRateQuery | 学生の出席率を取得 | ⬜ 未実装 |
| GetCourseAttendanceStatisticsQueryHandler | GetCourseAttendanceStatisticsQuery | コースの出席統計を取得 | ⬜ 未実装 |

---

## エピック1: 授業セッション管理

### ⬜ US-A01: 授業セッションを作成できる

**ストーリー:**
教員として、特定のコース・学期に対して授業セッションを作成できるようにしたい。なぜなら、出席を記録するためには授業の開催情報が必要だから。

**Handler:** `CreateSessionCommandHandler : IRequestHandler<CreateSessionCommand, SessionId>`

**受け入れ条件:**

```gherkin
Scenario: 新しい授業セッションを作成する
  Given CourseRepositoryに CourseCode "CS101" のコースが存在する
  And SemesterRepositoryに 2024年度 Spring学期 が存在する
  When CreateSessionCommandを実行する
    - CourseCode: "CS101"
    - SemesterId: (Year: 2024, Period: Spring)
    - SessionNumber: 1
    - SessionDate: 2024-04-08
    - StartTime: 10:00
    - EndTime: 11:30
    - Topic: "オリエンテーション"
  Then Sessionエンティティが作成される
  And SessionRepositoryに保存される
  And SessionIdが返される
  And 保存されたSessionのSessionNumberが1である
```

```gherkin
Scenario: 同じコース・学期・回数で重複して作成を試みる
  Given SessionRepositoryにCS101の2024年Spring学期の第1回授業が既に存在する
  When 同じCourseCode, SemesterId, SessionNumberでCreateSessionCommandを実行する
  Then DomainException "Session already exists for this course, semester, and session number" がスローされる
  And SessionRepositoryに保存されない
```

```gherkin
Scenario: 存在しないコースでセッション作成を試みる
  Given CourseRepositoryにコースコード "XXX999" が存在しない
  When CourseCode "XXX999" でCreateSessionCommandを実行する
  Then NotFoundException "Course with code XXX999 not found" がスローされる
```

```gherkin
Scenario: 終了時刻が開始時刻より前のセッション作成を試みる
  Given CourseとSemesterが存在する
  When CreateSessionCommandを実行する
    - StartTime: 10:00
    - EndTime: 09:00  # StartTimeより前
  Then DomainException "End time must be after start time" がスローされる
  And SessionRepositoryに保存されない
```

**制約:**

- セッション番号: 1以上の整数
- 開始時刻 < 終了時刻
- 同じコース・学期・セッション番号の組み合わせは一意
- 授業日は学期の開始日〜終了日の範囲内

**実装状態:** ⬜ 未実装

---

### ⬜ US-A02: コースの授業セッション一覧を取得できる

**ストーリー:**
教員・学生として、特定のコースの授業セッション一覧を取得できるようにしたい。なぜなら、授業スケジュールを確認する必要があるから。

**Handler:** `GetCourseSessionsQueryHandler : IRequestHandler<GetCourseSessionsQuery, List<SessionDto>>`

**受け入れ条件:**

```gherkin
Scenario: コースの全授業セッションを取得する
  Given SessionRepositoryに以下のSessionが存在する
    | CourseCode | SemesterId       | SessionNumber | SessionDate | Topic              |
    | CS101      | (2024, Spring)   | 1             | 2024-04-08  | オリエンテーション |
    | CS101      | (2024, Spring)   | 2             | 2024-04-15  | 変数と型           |
    | CS101      | (2024, Spring)   | 3             | 2024-04-22  | 制御構文           |
  When GetCourseSessionsQueryを実行する
    - CourseCode: "CS101"
  Then 3件のSessionDtoが返される
  And SessionNumberの昇順でソートされている
  And 1番目のSessionDtoのTopicが "オリエンテーション" である
```

```gherkin
Scenario: 学期を指定してセッションを取得する
  Given SessionRepositoryにCS101の2024年Springと2024年Fallのセッションが存在する
  When GetCourseSessionsQueryを実行する
    - CourseCode: "CS101"
    - SemesterId: (2024, Spring)
  Then 2024年Spring学期のSessionDtoのみが返される
  And 2024年Fall学期のSessionDtoは含まれない
```

```gherkin
Scenario: 授業セッションが存在しないコースを取得する
  Given CourseRepositoryにコース "CS101" が存在する
  And SessionRepositoryにCS101のSessionが存在しない
  When GetCourseSessionsQueryを実行する
    - CourseCode: "CS101"
  Then 空のリストが返される
```

**制約:**

- デフォルトソート: セッション番号の昇順
- 学期でフィルタリング可能（SemesterIdパラメータ）

**実装状態:** ⬜ 未実装

---

## エピック2: 出席記録管理

### ⬜ US-A03: 出席を記録できる

**ストーリー:**
教員として、授業セッションに対して学生の出席を記録できるようにしたい。なぜなら、学生の出席状況を管理する必要があるから。

**Handler:** `RecordAttendanceCommandHandler : IRequestHandler<RecordAttendanceCommand, AttendanceId>`

**受け入れ条件:**

```gherkin
Scenario: 学生の出席を記録する
  Given SessionRepositoryにSessionId "session-001" のSessionが存在する
  And StudentRepositoryにStudentId "student-001" のStudentが存在する
  And EnrollmentRepositoryにこのStudentのこのCourseへのEnrollmentが存在する
  When RecordAttendanceCommandを実行する
    - SessionId: "session-001"
    - StudentId: "student-001"
    - Status: Present
    - Remarks: ""
  Then Attendanceエンティティが作成される
  And AttendanceRepositoryに保存される
  And AttendanceIdが返される
  And 保存されたAttendanceのStatusがPresentである
```

```gherkin
Scenario: 履修登録していないコースの出席を記録しようとする
  Given SessionとStudentが存在する
  And EnrollmentRepositoryにこのStudentのEnrollmentが存在しない
  When RecordAttendanceCommandを実行する
  Then DomainException "Student not enrolled in this course" がスローされる
  And AttendanceRepositoryに保存されない
```

```gherkin
Scenario: 同じ学生・セッションで重複して出席を記録しようとする
  Given AttendanceRepositoryにStudentId "student-001" のSessionId "session-001" へのAttendanceが既に存在する
  When 同じSessionId, StudentIdでRecordAttendanceCommandを実行する
  Then DomainException "Attendance already recorded for this student and session" がスローされる
```

```gherkin
Scenario: 不正な出席状態で記録を試みる
  Given SessionとStudentとEnrollmentが存在する
  When RecordAttendanceCommandを実行する
    - Status: "InvalidStatus"  # 無効なステータス
  Then ArgumentException "Invalid attendance status" がスローされる
```

**制約:**

- 出席状態: Present（出席）, Absent（欠席）, Late（遅刻）, Excused（公欠）
- 同じ学生・セッションの組み合わせは一意
- 学生は対象コースを履修登録している必要がある
- 備考: 最大500文字（オプショナル）

**実装状態:** ⬜ 未実装

---

### ⬜ US-A04: 出席状態を更新できる

**ストーリー:**
教員として、記録済みの出席状態を更新できるようにしたい。なぜなら、記録ミスや遅刻の訂正が必要な場合があるから。

**Handler:** `UpdateAttendanceCommandHandler : IRequestHandler<UpdateAttendanceCommand, Unit>`

**受け入れ条件:**

```gherkin
Scenario: 出席状態を更新する
  Given AttendanceRepositoryにAttendanceId "att-001" のAttendanceが存在する
  And 現在のStatusがAbsentである
  When UpdateAttendanceCommandを実行する
    - AttendanceId: "att-001"
    - Status: Late
    - Remarks: "10分遅刻"
  Then AttendanceのStatusがLateに更新される
  And AttendanceのRemarksが "10分遅刻" に更新される
  And AttendanceRepositoryに保存される
  And UpdatedAtが更新される
```

```gherkin
Scenario: 存在しない出席記録IDで更新を試みる
  Given AttendanceRepositoryにAttendanceId "att-999" が存在しない
  When AttendanceId "att-999" でUpdateAttendanceCommandを実行する
  Then NotFoundException "Attendance with id att-999 not found" がスローされる
```

**制約:**

- 更新可能な項目: Status, Remarks
- StudentIdやSessionIdは変更不可
- 更新日時を自動記録

**実装状態:** ⬜ 未実装

---

### ⬜ US-A05: セッションの出席記録一覧を取得できる

**ストーリー:**
教員として、特定の授業セッションの出席記録一覧を取得できるようにしたい。なぜなら、授業ごとの出席状況を確認する必要があるから。

**Handler:** `GetSessionAttendancesQueryHandler : IRequestHandler<GetSessionAttendancesQuery, List<AttendanceDto>>`

**受け入れ条件:**

```gherkin
Scenario: セッションの全出席記録を取得する
  Given SessionRepositoryにSessionId "session-001" が存在する
  And AttendanceRepositoryに以下のAttendanceが存在する
    | StudentName | Status  | Remarks  |
    | 山田太郎    | Present |          |
    | 鈴木花子    | Late    | 5分遅刻  |
    | 田中次郎    | Absent  |          |
  When GetSessionAttendancesQueryを実行する
    - SessionId: "session-001"
  Then 3件のAttendanceDtoが返される
  And StudentNameの昇順でソートされている
```

```gherkin
Scenario: 出席状態でフィルタリングする
  Given SessionにPresentとAbsentのAttendanceが存在する
  When GetSessionAttendancesQueryを実行する
    - SessionId: "session-001"
    - StatusFilter: Absent
  Then StatusがAbsentのAttendanceDtoのみが返される
```

**制約:**

- デフォルトソート: 学生名の昇順
- 出席状態でフィルタリング可能

**実装状態:** ⬜ 未実装

---

### ⬜ US-A06: 学生の出席記録一覧を取得できる

**ストーリー:**
学生・教員として、特定の学生の出席記録一覧を取得できるようにしたい。なぜなら、学生個人の出席履歴を確認する必要があるから。

**Handler:** `GetStudentAttendancesQueryHandler : IRequestHandler<GetStudentAttendancesQuery, List<AttendanceDto>>`

**受け入れ条件:**

```gherkin
Scenario: 学生の全出席記録を取得する
  Given StudentRepositoryにStudentId "student-001" が存在する
  And AttendanceRepositoryに以下のAttendanceが存在する
    | CourseCode | SessionNumber | SessionDate | Status  |
    | CS101      | 1             | 2024-04-08  | Present |
    | CS101      | 2             | 2024-04-15  | Late    |
    | MATH201    | 1             | 2024-04-10  | Present |
  When GetStudentAttendancesQueryを実行する
    - StudentId: "student-001"
  Then 3件のAttendanceDtoが返される
  And SessionDateの降順でソートされている（最新が先頭）
```

```gherkin
Scenario: コースを指定して出席記録を取得する
  Given StudentにCS101とMATH201のAttendanceが存在する
  When GetStudentAttendancesQueryを実行する
    - StudentId: "student-001"
    - CourseCodeFilter: "CS101"
  Then CS101のAttendanceDtoのみが返される
  And MATH201のAttendanceDtoは含まれない
```

**制約:**

- デフォルトソート: 日付の降順（最新が先頭）
- コースコードでフィルタリング可能
- 学期でフィルタリング可能

**実装状態:** ⬜ 未実装

---

## エピック3: 出席統計

### ⬜ US-A07: 学生の出席率を取得できる

**ストーリー:**
学生・教員として、学生の出席率を計算して取得できるようにしたい。なぜなら、出席状況を評価する必要があるから。

**Handler:** `GetStudentAttendanceRateQueryHandler : IRequestHandler<GetStudentAttendanceRateQuery, AttendanceRateDto>`

**受け入れ条件:**

```gherkin
Scenario: 学生の全体出席率を取得する
  Given StudentRepositoryにStudentId "student-001" が存在する
  And StudentがCS101を履修しており、10回のSessionが実施されている
  And AttendanceRepositoryに以下のAttendanceが存在する
    | Status  | Count |
    | Present | 8     |
    | Late    | 1     |
    | Absent  | 1     |
  When GetStudentAttendanceRateQueryを実行する
    - StudentId: "student-001"
  Then AttendanceRateDtoが返される
  And TotalSessionsが10である
  And PresentCountが8である
  And LateCountが1である
  And AbsentCountが1である
  And AttendanceRateが90.0である
```

```gherkin
Scenario: コースを指定して出席率を取得する
  Given StudentがCS101とMATH201を履修している
  When GetStudentAttendanceRateQueryを実行する
    - StudentId: "student-001"
    - CourseCodeFilter: "CS101"
  Then CS101の出席率のみが返される
```

**制約:**

- 出席率 = (Present + Late + Excused) / totalSessions × 100
- 小数点第1位まで表示
- コースコードでフィルタリング可能
- 学期でフィルタリング可能

**実装状態:** ⬜ 未実装

---

### ⬜ US-A08: コースの出席統計を取得できる

**ストーリー:**
教員として、コース全体の出席統計を取得できるようにしたい。なぜなら、授業運営の改善に活用するためにクラス全体の傾向を把握する必要があるから。

**Handler:** `GetCourseAttendanceStatisticsQueryHandler : IRequestHandler<GetCourseAttendanceStatisticsQuery, CourseAttendanceStatisticsDto>`

**受け入れ条件:**

```gherkin
Scenario: コースの出席統計を取得する
  Given CourseRepositoryにCourseCode "CS101" が存在する
  And EnrollmentRepositoryに30名のEnrollmentが存在する
  And SessionRepositoryに10回のSessionが存在する
  And 各Sessionの平均出席率が85%である
  When GetCourseAttendanceStatisticsQueryを実行する
    - CourseCode: "CS101"
  Then CourseAttendanceStatisticsDtoが返される
  And TotalStudentsが30である
  And TotalSessionsが10である
  And AverageAttendanceRateが85.0である
  And SessionStatisticsが10件含まれる
```

**制約:**

- 平均出席率 = 全セッションの出席率の平均
- セッション統計はセッション番号順
- 学期でフィルタリング可能

**実装状態:** ⬜ 未実装

---

## ドメインルール・制約まとめ

### 授業セッション（Session）

- **セッション番号**: 1以上の整数
- **授業日**: 学期の開始日〜終了日の範囲内
- **開始時刻 < 終了時刻**
- **一意制約**: コース + 学期 + セッション番号
- **トピック**: 最大200文字

### 出席記録（Attendance）

- **出席状態**: Present, Absent, Late, Excused
- **一意制約**: 学生 + セッション
- **履修登録チェック**: 学生は対象コースを履修登録している必要がある
- **備考**: 最大500文字（オプショナル）
- **記録日時**: 自動記録
- **更新日時**: 自動記録

### 出席率計算

- **出席率** = (Present + Late + Excused) / 総セッション数 × 100
- **小数点第1位**まで表示
- Absentは出席率に含まない

---

## 実装優先順位

### Phase 1: 授業セッション管理

- ⬜ US-A01: 授業セッション作成
- ⬜ US-A02: 授業セッション一覧取得

### Phase 2: 出席記録管理

- ⬜ US-A03: 出席記録
- ⬜ US-A04: 出席状態更新
- ⬜ US-A05: セッション出席記録一覧取得
- ⬜ US-A06: 学生出席記録一覧取得

### Phase 3: 出席統計

- ⬜ US-A07: 学生出席率取得
- ⬜ US-A08: コース出席統計取得

---

## コンテキスト依存関係

```
Enrollments (履修管理)
  ↓
Attendance (出席管理) ← 本ドキュメント
  ↓
Grading (成績評価)
```

**必要な前提データ:**
- Students（学生）
- Courses（コース）
- Semesters（学期）
- Enrollments（履修登録）

---

## テスト実装戦略

詳細は [testing-strategy.md](impl-patterns/testing-strategy.md) を参照してください。
