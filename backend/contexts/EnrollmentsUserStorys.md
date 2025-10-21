# Enrollments（履修登録）ユーザーストーリー

## 概要

このドキュメントは、履修登録システムのユーザーストーリーと受け入れ条件を定義します。
各ストーリーにはGiven-When-Then形式の受け入れ条件（Applicationレイヤーの統合テスト視点）と制約事項を記載しています。

**対象範囲:** Applicationレイヤーの統合テスト（CommandHandler, QueryHandler, Repository層を含む）

## Application Service一覧

### コース管理 (Phase 1 - 完了)

| Command/Query | 説明 | 実装状態 |
|--------------|------|----------|
| CreateCourseCommand | コースを登録 | ✅ 完了 |
| GetCoursesQuery | コース一覧を取得 | ✅ 完了 |
| GetCourseQuery | コースを取得 | ✅ 完了 |

### 学生管理 (Phase 2 - 完了)

| Command/Query | 説明 | 実装状態 |
|--------------|------|----------|
| CreateStudentCommand | 学生を登録 | ✅ 完了 |
| GetStudentsQuery | 学生一覧を取得 | ✅ 完了 |
| GetStudentQuery | 学生を取得 | ⬜ 未実装 |
| UpdateStudentCommand | 学生情報を更新 | ✅ 完了 |

### 学期管理 (Phase 3 - 未実装)

| Command/Query | 説明 | 実装状態 |
|--------------|------|----------|
| CreateSemesterCommand | 学期を登録 | ⬜ 未実装 |
| GetSemestersQuery | 学期一覧を取得 | ⬜ 未実装 |
| GetCurrentSemesterQuery | 現在の学期を取得 | ⬜ 未実装 |

### 履修登録 (Phase 4 - 未実装)

| Command/Query | 説明 | 実装状態 |
|--------------|------|----------|
| EnrollStudentCommand | 履修登録 | ⬜ 未実装 |
| CancelEnrollmentCommand | 履修登録キャンセル | ⬜ 未実装 |
| GetStudentEnrollmentsQuery | 履修登録一覧を取得 | ⬜ 未実装 |
| CompleteEnrollmentCommand | 履修登録を完了 | ⬜ 未実装 |

---

## エピック1: コース管理

### ✅ US-E01: CreateCourseService - コースを登録できる

**ストーリー:**
API利用者として、新しいコースを登録できるようにしたい。なぜなら、学生が履修登録するためにはコース情報が必要だから。

**Service:** `ICreateCourseService.CreateCourseAsync(CreateCourseCommand)`

**受け入れ条件:**

```gherkin
Scenario: 有効なコース情報で新しいコースを登録する
  Given データベースが利用可能である
  When CreateCourseCommandを実行する
    - CourseCode: "CS101"
    - Name: "プログラミング入門"
    - Credits: 3
    - MaxCapacity: 30
  Then コースコード "CS101" が返される
  And データベースにコースが保存されている
  And コース名が "プログラミング入門" である
  And 単位数が 3 である
```

```gherkin
Scenario: 不正なコースコード形式で登録を試みる
  Given データベースが利用可能である
  When CreateCourseCommandを実行する
    - CourseCode: "invalid-code"
    - Name: "テストコース"
    - Credits: 3
    - MaxCapacity: 30
  Then ArgumentException がスローされる
  And エラーメッセージに "Invalid course code format" が含まれる
```

```gherkin
Scenario: 単位数が範囲外のコースを登録を試みる
  Given データベースが利用可能である
  When CreateCourseCommandを実行する
    - CourseCode: "CS101"
    - Name: "テストコース"
    - Credits: 11
    - MaxCapacity: 30
  Then ArgumentException がスローされる
  And エラーメッセージに "Credits must be between 1 and 10" が含まれる
```

```gherkin
Scenario: 既に存在するコースコードで登録を試みる
  Given コースコード "CS101" のコースが既にデータベースに登録されている
  When CreateCourseCommandを実行する
    - CourseCode: "CS101"
    - Name: "新しいコース"
    - Credits: 3
    - MaxCapacity: 30
  Then InvalidOperationException がスローされる
  And エラーメッセージに "already exists" が含まれる
```

**制約:**

- コースコード形式: 大文字2-4文字 + 数字3-4桁（例: CS101, MATH1001）
- 単位数: 1〜10の範囲
- 定員: 1以上の整数
- コース名: 必須、空白不可

**実装状態:** ✅ 完了

---

### ✅ US-E02: GetCoursesService - コース一覧を取得できる

**ストーリー:**
API利用者として、登録されているコース一覧を取得できるようにしたい。なぜなら、履修可能なコースを表示するためにコース情報が必要だから。

**Service:** `IGetCoursesService.GetCoursesAsync()`

**受け入れ条件:**

```gherkin
Scenario: 登録されている全コースを取得する
  Given データベースに以下のコースが登録されている
    | コースコード | コース名       | 単位数 | 定員 |
    | CS101        | プログラミング入門 | 3      | 30   |
    | MATH201      | 線形代数       | 4      | 25   |
  When GetCoursesServiceを実行する
  Then 2件のCourseDtoが返される
  And 1件目のコースコードが "CS101" である
  And 1件目のコース名が "プログラミング入門" である
  And 2件目のコースコードが "MATH201" である
  And 2件目のコース名が "線形代数" である
```

```gherkin
Scenario: コースが1件も登録されていない場合
  Given データベースにコースが登録されていない
  When GetCoursesServiceを実行する
  Then 空のリスト（0件）が返される
```

**制約:**

- 全てのコースを取得（ページネーション未実装）
- コース情報は読み取り専用

**実装状態:** ✅ 完了

---

### ✅ US-E03: GetCourseByCodeService - コースを取得できる

**ストーリー:**
API利用者として、コースコードを指定して特定のコースの詳細情報を取得できるようにしたい。なぜなら、履修登録前にコースの詳細を確認する必要があるから。

**Service:** `IGetCourseByCodeService.GetCourseByCodeAsync(string courseCode)`

**受け入れ条件:**

```gherkin
Scenario: 存在するコースコードでコースを取得する
  Given データベースにコースコード "CS101" のコースが登録されている
    - CourseCode: "CS101"
    - Name: "プログラミング入門"
    - Credits: 3
    - MaxCapacity: 30
  When GetCourseByCodeServiceを実行する
    - CourseCode: "CS101"
  Then CourseDtoが返される
  And コースコードが "CS101" である
  And コース名が "プログラミング入門" である
  And 単位数が 3 である
  And 定員が 30 である
```

```gherkin
Scenario: 存在しないコースコードで取得を試みる
  Given データベースにコースコード "XXX999" のコースが登録されていない
  When GetCourseByCodeServiceを実行する
    - CourseCode: "XXX999"
  Then KeyNotFoundException がスローされる
  And エラーメッセージに "not found" が含まれる
```

**制約:**

- コースコードは大文字小文字を区別しない（自動的に大文字変換）
- 存在しないコースの場合は404エラー

**実装状態:** ✅ 完了

---

## エピック2: 学生管理

### ✅ US-S01: CreateStudentService - 学生を登録できる

**ストーリー:**
API利用者として、新しい学生をシステムに登録できるようにしたい。なぜなら、学生が履修登録するためにはアカウントが必要だから。

**Service:** `ICreateStudentService.CreateStudentAsync(CreateStudentCommand)`

**受け入れ条件:**

```gherkin
Scenario: 有効な学生情報で新しい学生を登録する
  Given データベースが利用可能である
  When CreateStudentCommandを実行する
    - Name: "山田太郎"
    - Email: "yamada@example.com"
    - Grade: 1
  Then 自動生成された学生ID（Guid）が返される
  And データベースに学生が保存されている
  And 学生名が "山田太郎" である
  And メールアドレスが "yamada@example.com" である
```

```gherkin
Scenario: 重複するメールアドレスで登録を試みる
  Given メールアドレス "existing@example.com" の学生が既にデータベースに登録されている
  When CreateStudentCommandを実行する
    - Name: "田中次郎"
    - Email: "existing@example.com"
    - Grade: 2
  Then InvalidOperationException がスローされる
  And エラーメッセージに "Email already exists" が含まれる
```

```gherkin
Scenario: 不正なメール形式で登録を試みる
  Given データベースが利用可能である
  When CreateStudentCommandを実行する
    - Name: "山田太郎"
    - Email: "invalid-email"
    - Grade: 1
  Then ArgumentException がスローされる
  And エラーメッセージに "Invalid email format" が含まれる
```

**制約:**

- 学生ID: UUID形式で自動生成
- 名前: 必須、空白不可
- メールアドレス: 必須、一意制約、メール形式
- 学年: 1〜4の範囲

**実装状態:** ✅ 完了

---

### ⬜ US-S02: UpdateStudentService - 学生情報を更新できる

**ストーリー:**
API利用者として、学生情報を更新できるようにしたい。なぜなら、メールアドレスや学年情報が変更になることがあるから。

**Service:** `IUpdateStudentService.UpdateStudentAsync(UpdateStudentCommand)`

**受け入れ条件:**

```gherkin
Scenario: 学生情報を更新する
  Given データベースに学生ID "123e4567-e89b-12d3-a456-426614174000" の学生が登録されている
  When UpdateStudentCommandを実行する
    - StudentId: "123e4567-e89b-12d3-a456-426614174000"
    - Name: "山田太郎"
    - Email: "new@example.com"
    - Grade: 2
  Then 正常に完了する（戻り値なし）
  And データベースの学生情報が更新されている
  And メールアドレスが "new@example.com" に変更されている
  And 学年が 2 に変更されている
```

```gherkin
Scenario: 存在しない学生IDで更新を試みる
  Given データベースに学生ID "99999999-9999-9999-9999-999999999999" の学生が登録されていない
  When UpdateStudentCommandを実行する
    - StudentId: "99999999-9999-9999-9999-999999999999"
  Then KeyNotFoundException がスローされる
  And エラーメッセージに "Student not found" が含まれる
```

**制約:**

- 学生は自分の情報のみ更新可能
- メールアドレスの一意制約は維持される

**実装状態:** ✅ 完了

---

### ✅ US-S03: GetStudentsService - 学生一覧を取得できる

**ストーリー:**
API利用者として、登録されている学生の一覧を取得できるようにしたい。なぜなら、学生管理画面で全学生を確認したり、特定の条件で学生を検索する必要があるから。

**Service:** `IGetStudentsService.GetStudentsAsync(GetStudentsQuery query)`

**受け入れ条件:**

```gherkin
Scenario: 全学生を取得する
  Given データベースに以下の学生が登録されている
    | 学生ID | 名前 | メールアドレス | 学年 |
    | id-001 | 山田太郎 | yamada@example.com | 1 |
    | id-002 | 鈴木花子 | suzuki@example.com | 2 |
    | id-003 | 田中次郎 | tanaka@example.com | 3 |
  When GetStudentsServiceを実行する
    - フィルタ条件: なし
  Then 3件のStudentDtoが返される
  And 学生が登録日時の昇順でソートされている
```

```gherkin
Scenario: 学年でフィルタリングして学生を取得する
  Given データベースに学年1, 2, 3の学生が混在して登録されている
  When GetStudentsServiceを実行する
    - Grade: 2
  Then 学年が 2 の学生のみが返される
  And 複数該当する場合は登録日時の昇順でソートされている
```

```gherkin
Scenario: 名前で部分一致検索して学生を取得する
  Given データベースに以下の学生が登録されている
    | 名前 |
    | 山田太郎 |
    | 山田花子 |
    | 田中次郎 |
  When GetStudentsServiceを実行する
    - Name: "山田"
  Then "山田太郎" と "山田花子" のみが返される
  And 大文字小文字を区別しない
```

```gherkin
Scenario: メールアドレスで検索して学生を取得する
  Given データベースに以下の学生が登録されている
    | メールアドレス |
    | yamada@example.com |
    | suzuki@example.com |
    | suzuki@hoge.com |
  When GetStudentsServiceを実行する
    - Email: "example.com"
  Then 部分一致する学生が全て返される
```

```gherkin
Scenario: 複数の条件を組み合わせて検索する
  Given データベースに複数の学生が登録されている
  When GetStudentsServiceを実行する
    - Grade: 2
    - Name: "山田"
  Then 学年が 2 且つ名前に "山田" が含まれる学生のみが返される
```

```gherkin
Scenario: 検索結果が0件の場合
  Given データベースに学生が登録されている
  When GetStudentsServiceを実行する
    - Name: "存在しない名前"
  Then 空のリスト（0件）が返される
```

```gherkin
Scenario: 学生が1件も登録されていない場合
  Given データベースに学生が登録されていない
  When GetStudentsServiceを実行する
    - フィルタ条件: なし
  Then 空のリスト（0件）が返される
```

**制約:**

- フィルタ条件はオプション（全て指定なしで全件取得可能）
- デフォルトソート: 登録日時の昇順
- 名前とメールアドレスは部分一致
- ページネーション未実装

**実装状態:** ✅ 完了

---

## エピック3: 学期管理

### ⬜ US-M01: 学期情報を管理できる

**ストーリー:**
管理者として、学期情報（年度、学期、期間）を管理できるようにしたい。なぜなら、履修登録は学期ごとに管理される必要があるから。

**Application Service:** `CreateSemesterCommandHandler`

**受け入れ条件:**

```gherkin
Scenario: 新しい学期を登録する
  Given SemesterRepositoryが利用可能である
  When CreateSemesterCommandを実行する
    - Year: 2024
    - Period: Spring
    - StartDate: 2024-04-01
    - EndDate: 2024-09-30
  Then Semesterエンティティが作成される
  And SemesterRepositoryに保存される
  And SemesterId (2024, Spring) が返される
```

```gherkin
Scenario: 重複する学期を登録しようとする
  Given SemesterRepositoryに (2024, Spring) の学期が既に存在する
  When 同じYear, PeriodでCreateSemesterCommandを実行する
  Then DomainException "Semester already exists" がスローされる
  And SemesterRepositoryに保存されない
```

```gherkin
Scenario: 不正な学期期間で登録を試みる
  Given SemesterRepositoryが利用可能である
  When CreateSemesterCommandを実行する
    - Year: 2024
    - Period: "InvalidPeriod"
  Then ArgumentException "Invalid semester period. Must be Spring or Fall" がスローされる
```

```gherkin
Scenario: 終了日が開始日より前の学期を登録しようとする
  Given SemesterRepositoryが利用可能である
  When CreateSemesterCommandを実行する
    - StartDate: 2024-09-30
    - EndDate: 2024-04-01
  Then DomainException "End date must be after start date" がスローされる
  And SemesterRepositoryに保存されない
```

**制約:**

- 学期: Spring または Fall
- 年度: 2000〜2100の範囲
- 同じ年度・学期の組み合わせは一意
- 終了日 > 開始日

**実装状態:** ⬜ 未実装

---

### ⬜ US-M02: 学期一覧を取得できる

**ストーリー:**
学生・教員として、登録されている学期の一覧を取得できるようにしたい。なぜなら、履修登録時に選択可能な学期を表示する必要があるから。

**Application Service:** `GetSemestersQueryHandler`

**受け入れ条件:**

```gherkin
Scenario: 全学期を取得する
  Given SemesterRepositoryに以下のSemesterが存在する
    | Year | Period | StartDate  | EndDate    |
    | 2024 | Spring | 2024-04-01 | 2024-09-30 |
    | 2024 | Fall   | 2024-10-01 | 2025-03-31 |
    | 2023 | Fall   | 2023-10-01 | 2024-03-31 |
  When GetSemestersQueryを実行する
  Then 3件のSemesterDtoが返される
  And Yearの降順、Periodの降順でソートされている（最新が先頭）
```

```gherkin
Scenario: 現在の学期のみを取得する
  Given SemesterRepositoryに複数のSemesterが存在する
  And 現在日時が 2024-05-15 である
  When GetSemestersQueryを実行する
    - CurrentOnly: true
  Then 2024年SpringのSemesterDtoのみが返される
  And StartDate <= 現在日時 <= EndDate を満たす
```

```gherkin
Scenario: 学期が1件も登録されていない場合
  Given SemesterRepositoryにSemesterが存在しない
  When GetSemestersQueryを実行する
  Then 空のリストが返される
```

**制約:**

- デフォルトソート: 年度・学期の降順（最新が先頭）
- `CurrentOnly`パラメータで現在の学期のみフィルタリング可能

**実装状態:** ⬜ 未実装

---

### ⬜ US-M03: 現在の学期を取得できる

**ストーリー:**
学生・教員として、現在の学期情報を簡単に取得できるようにしたい。なぜなら、履修登録画面で現在の学期を表示する必要があるから。

**Application Service:** `GetCurrentSemesterQueryHandler`

**受け入れ条件:**

```gherkin
Scenario: 現在の学期を取得する
  Given SemesterRepositoryに以下のSemesterが存在する
    | Year | Period | StartDate  | EndDate    |
    | 2024 | Spring | 2024-04-01 | 2024-09-30 |
    | 2024 | Fall   | 2024-10-01 | 2025-03-31 |
  And 現在日時が 2024-05-15 である
  When GetCurrentSemesterQueryを実行する
  Then SemesterDtoが返される
  And Yearが2024である
  And PeriodがSpringである
```

```gherkin
Scenario: 現在の学期が存在しない場合
  Given SemesterRepositoryにSemesterが存在する
  And 現在日時がどのSemesterの期間にも含まれない
  When GetCurrentSemesterQueryを実行する
  Then NotFoundException "No current semester found" がスローされる
```

**制約:**

- 現在日時が開始日〜終了日の範囲内の学期を返す
- 該当する学期が存在しない場合はNotFoundException

**実装状態:** ⬜ 未実装

---

## エピック4: 履修登録

### ⬜ US-R01: EnrollStudentService - コースを履修登録できる

**ストーリー:**
API利用者として、学生がコースを履修登録できるようにしたい。なぜなら、学期の授業を受講するためには事前に履修登録が必要だから。

**Service:** `IEnrollStudentService.EnrollStudentAsync(EnrollStudentCommand)`

**受け入れ条件:**

```gherkin
Scenario: 学生が有効なコースを履修登録する
  Given データベースに学生ID "123e4567-e89b-12d3-a456-426614174000" の学生が登録されている
  And データベースにコースコード "CS101" のコースが登録されている
  And 学生の現在の履修単位数が 12単位 である
  And CS101 の単位数が 3単位 である
  And データベースに 2024年度 Spring学期 が登録されている
  When EnrollStudentCommandを実行する
    - StudentId: "123e4567-e89b-12d3-a456-426614174000"
    - CourseCode: "CS101"
    - SemesterId: (2024, Spring)
  Then 履修登録ID（EnrollmentId）が返される
  And データベースに履修登録が保存されている
  And 履修登録ステータスが "InProgress" である
```

```gherkin
Scenario: 履修単位数上限を超えて登録を試みる
  Given 学生の現在の履修単位数が 22単位 である
  And 履修しようとするコースの単位数が 3単位 である
  And 単位数上限が 24単位 である
  When EnrollStudentCommandを実行する
  Then ArgumentException がスローされる
  And エラーメッセージに "Exceeds maximum credits" が含まれる
```

```gherkin
Scenario: 同じコースを重複して登録を試みる
  Given 学生がコースコード "CS101" を2024年度 Spring学期で既に履修登録している
  When EnrollStudentCommandを実行する
    - StudentId: （同じ学生ID）
    - CourseCode: "CS101"
    - SemesterId: (2024, Spring)
  Then InvalidOperationException がスローされる
  And エラーメッセージに "Already enrolled" が含まれる
```

```gherkin
Scenario: 定員オーバーのコースを登録を試みる
  Given コースコード "CS101" の定員が 30名 である
  And 既に 30名 の学生が履修登録している
  When 31人目の学生がEnrollStudentCommandを実行する
  Then InvalidOperationException がスローされる
  And エラーメッセージに "Course is full" が含まれる
```

```gherkin
Scenario: 存在しない学生IDで登録を試みる
  Given データベースに学生ID "99999999-9999-9999-9999-999999999999" の学生が存在しない
  When EnrollStudentCommandを実行する
    - StudentId: "99999999-9999-9999-9999-999999999999"
  Then KeyNotFoundException がスローされる
  And エラーメッセージに "Student not found" が含まれる
```

```gherkin
Scenario: 存在しないコースコードで登録を試みる
  Given データベースにコースコード "XXX999" のコースが存在しない
  When EnrollStudentCommandを実行する
    - CourseCode: "XXX999"
  Then KeyNotFoundException がスローされる
  And エラーメッセージに "Course not found" が含まれる
```

**制約:**

- 最大履修単位数: 24単位/学期
- 同じコース・同じ学期の重複登録は不可
- 定員オーバーは不可
- 学期は事前にSemesterエンティティとして登録されている必要がある
- 初期ステータス: InProgress

**実装状態:** ⬜ 未実装

---

### ⬜ US-R02: CancelEnrollmentService - 履修登録をキャンセルできる

**ストーリー:**
API利用者として、履修登録をキャンセルできるようにしたい。なぜなら、履修計画を変更したい場合があるから。

**Service:** `ICancelEnrollmentService.CancelEnrollmentAsync(EnrollmentId)`

**受け入れ条件:**

```gherkin
Scenario: 進行中の履修登録をキャンセルする
  Given データベースに履修登録ID "abc-123" の履修登録が存在する
  And 履修登録ステータスが "InProgress" である
  When CancelEnrollmentServiceを実行する
    - EnrollmentId: "abc-123"
  Then 正常に完了する（戻り値なし）
  And データベースの履修登録ステータスが "Cancelled" になる
  And 学生の履修単位数が減算される
```

```gherkin
Scenario: 既に完了している履修登録をキャンセルしようとする
  Given データベースに履修登録ID "abc-123" の履修登録が存在する
  And 履修登録ステータスが "Completed" である
  When CancelEnrollmentServiceを実行する
    - EnrollmentId: "abc-123"
  Then InvalidOperationException がスローされる
  And エラーメッセージに "Cannot cancel completed enrollment" が含まれる
  And 履修登録ステータスは変更されない
```

```gherkin
Scenario: 存在しない履修登録IDでキャンセルを試みる
  Given データベースに履修登録ID "99999999" の履修登録が存在しない
  When CancelEnrollmentServiceを実行する
    - EnrollmentId: "99999999"
  Then KeyNotFoundException がスローされる
  And エラーメッセージに "Enrollment not found" が含まれる
```

**制約:**

- キャンセル可能なステータス: InProgress のみ
- キャンセル後は履修単位数から減算
- Completed または Cancelled ステータスはキャンセル不可

**実装状態:** ⬜ 未実装

---

### ⬜ US-R03: 履修登録一覧を取得できる

**ストーリー:**
学生・教員として、学生の履修登録一覧を取得できるようにしたい。なぜなら、現在の履修状況を確認する必要があるから。

**Application Service:** `GetStudentEnrollmentsQueryHandler`

**受け入れ条件:**

```gherkin
Scenario: 学生の全ての履修登録を取得する
  Given StudentRepositoryにStudentId "student-001" が存在する
  And EnrollmentRepositoryに以下のEnrollmentが存在する
    | CourseCode | SemesterId      | Status     |
    | CS101      | (2024, Spring)  | InProgress |
    | MATH201    | (2024, Spring)  | InProgress |
    | ENG101     | (2023, Fall)    | Completed  |
  When GetStudentEnrollmentsQueryを実行する
    - StudentId: "student-001"
  Then 3件のEnrollmentDtoが返される
  And Semesterの新しい順にソートされている
```

```gherkin
Scenario: ステータスでフィルタリングして履修登録を取得する
  Given Studentに複数のEnrollmentが存在する
  When GetStudentEnrollmentsQueryを実行する
    - StudentId: "student-001"
    - StatusFilter: InProgress
  Then StatusがInProgressのEnrollmentDtoのみが返される
```

```gherkin
Scenario: 履修登録が存在しない学生の一覧を取得する
  Given StudentRepositoryにStudentId "student-001" が存在する
  And EnrollmentRepositoryにこのStudentのEnrollmentが存在しない
  When GetStudentEnrollmentsQueryを実行する
    - StudentId: "student-001"
  Then 空のリストが返される
```

```gherkin
Scenario: 存在しない学生IDで一覧を取得しようとする
  Given StudentRepositoryにStudentId "student-999" が存在しない
  When StudentId "student-999" でGetStudentEnrollmentsQueryを実行する
  Then NotFoundException "Student not found" がスローされる
```

**制約:**

- 学生は自分の履修登録のみ閲覧可能
- デフォルトソート: 学期の新しい順
- ステータスフィルタリング: オプショナル

**実装状態:** ⬜ 未実装

---

### ⬜ US-R04: 履修登録を完了できる

**ストーリー:**
教員・管理者として、学期終了時に履修登録ステータスを完了にできるようにしたい。なぜなら、成績評価に移行するためには履修登録を完了状態にする必要があるから。

**Application Service:** `CompleteEnrollmentCommandHandler`

**受け入れ条件:**

```gherkin
Scenario: 進行中の履修登録を完了する
  Given EnrollmentRepositoryにEnrollmentId "enrollment-001" が存在する
  And EnrollmentのStatusがInProgressである
  When CompleteEnrollmentCommandを実行する
    - EnrollmentId: "enrollment-001"
  Then EnrollmentのStatusがCompletedに更新される
  And CompletedAtが記録される
  And EnrollmentRepositoryに保存される
```

```gherkin
Scenario: 既に完了している履修登録を再度完了しようとする
  Given EnrollmentRepositoryにEnrollmentId "enrollment-001" が存在する
  And EnrollmentのStatusがCompletedである
  When CompleteEnrollmentCommandを実行する
  Then DomainException "Already completed" がスローされる
  And Statusは変更されない
```

```gherkin
Scenario: キャンセル済みの履修登録を完了しようとする
  Given EnrollmentRepositoryにEnrollmentId "enrollment-001" が存在する
  And EnrollmentのStatusがCancelledである
  When CompleteEnrollmentCommandを実行する
  Then DomainException "Cannot complete cancelled enrollment" がスローされる
  And Statusは変更されない
```

```gherkin
Scenario: 存在しない履修登録IDで完了を試みる
  Given EnrollmentRepositoryにEnrollmentId "enrollment-999" が存在しない
  When EnrollmentId "enrollment-999" でCompleteEnrollmentCommandを実行する
  Then NotFoundException "Enrollment not found" がスローされる
```

**制約:**

- 完了可能なステータス: InProgress のみ
- Completed または Cancelled からの完了は不可
- 完了日時を自動記録

**実装状態:** ⬜ 未実装

---

## ドメインルール・制約まとめ

### コース（Course）

- **コースコード**: `^[A-Z]{2,4}\d{3,4}$`（例: CS101, MATH1001）
- **単位数**: 1〜10
- **定員**: 1以上
- **コース名**: 必須、空白不可

### 学生（Student）

- **学生ID**: UUID（自動生成）
- **メールアドレス**: 一意制約、メール形式
- **学年**: 1〜4

### 履修登録（Enrollment）

- **最大履修単位数**: 24単位/学期
- **ステータス遷移**:
  - `InProgress` → `Completed` ✅
  - `InProgress` → `Cancelled` ✅
  - `Completed` → (変更不可) ❌
  - `Cancelled` → (変更不可) ❌
- **重複登録**: 同じコース・同じ学期は不可
- **定員チェック**: 定員オーバーは登録不可

### 学期（Semester）

- **年度**: 2000〜2100
- **学期**: Spring または Fall
- **一意制約**: 年度 + 学期

---

## 実装優先順位

### Phase 1: 基本機能（完了済み）

- ✅ US-E01: コース登録
- ✅ US-E02: コース一覧閲覧
- ✅ US-E03: コース検索

### Phase 2: 学生管理（完了）

- ✅ US-S01: 学生登録
- ✅ US-S02: 学生情報更新
- ✅ US-S03: 学生一覧取得（条件絞込可能）

### Phase 3: 学期管理（次フェーズ）

- ⬜ US-M01: 学期登録
- ⬜ US-M02: 学期一覧取得
- ⬜ US-M03: 現在の学期取得

### Phase 4: 履修登録コア機能

- ⬜ US-R01: 履修登録（学期依存）
- ⬜ US-R02: 履修登録キャンセル
- ⬜ US-R03: 履修登録一覧閲覧
- ⬜ US-R04: 履修登録完了

---

## テスト実装戦略

詳細は [testing-strategy.md](impl-pattens/testing-strategy.md) を参照してください。
