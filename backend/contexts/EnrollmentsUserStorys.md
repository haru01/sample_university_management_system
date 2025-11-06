# Enrollments（履修登録）ユーザーストーリー

## 概要

このドキュメントは、履修登録システムのユーザーストーリーと受け入れ条件を定義します。
各ストーリーにはGiven-When-Then形式の受け入れ条件（Applicationレイヤーの統合テスト視点）と制約事項を記載しています。

**対象範囲:** Applicationレイヤーの統合テスト（CommandHandler, QueryHandler, Repository層を含む）

**実装パターン:** MediatRを使用したCQRSパターン

## CommandHandler/QueryHandler一覧

### 学生管理 (Phase 1 - 完了) → エピック1

| Handler | Command/Query | 説明 | 実装状態 |
|---------|--------------|------|----------|
| CreateStudentCommandHandler | CreateStudentCommand | 学生を登録 | ✅ 完了 |
| GetStudentsQueryHandler | GetStudentsQuery | 学生一覧を取得 | ✅ 完了 |
| GetStudentQueryHandler | GetStudentQuery | 学生を取得 | ✅ 完了 |
| UpdateStudentCommandHandler | UpdateStudentCommand | 学生情報を更新 | ✅ 完了 |

### 学期管理 (Phase 2 - 完了) → エピック2

| Handler | Command/Query | 説明 | 実装状態 |
|---------|--------------|------|----------|
| CreateSemesterCommandHandler | CreateSemesterCommand | 学期を登録 | ✅ 完了 |
| GetSemestersQueryHandler | GetSemestersQuery | 学期一覧を取得 | ✅ 完了 |
| GetCurrentSemesterQueryHandler | GetCurrentSemesterQuery | 現在の学期を取得 | ✅ 完了 |

### コースマスタ管理 (Phase 3 - 完了) → エピック3

| Handler | Command/Query | 説明 | 実装状態 |
|---------|--------------|------|----------|
| CreateCourseCommandHandler | CreateCourseCommand | コースマスタを登録 | ✅ 完了 |
| GetCoursesQueryHandler | GetCoursesQuery | コースマスタ一覧を取得 | ✅ 完了 |
| GetCourseByCodeQueryHandler | GetCourseByCodeQuery | コースマスタを取得 | ✅ 完了 |

### コース開講管理 (Phase 4) → エピック4

| Handler | Command/Query | 説明 | 実装状態 |
|---------|--------------|------|----------|
| CreateCourseOfferingCommandHandler | CreateCourseOfferingCommand | コース開講を登録 | ✅ 完了 |
| UpdateCourseOfferingCommandHandler | UpdateCourseOfferingCommand | コース開講情報を更新 | ✅ 完了 |
| SelectCourseOfferingsBySemesterQueryHandler | SelectCourseOfferingsBySemesterQuery | 学期ごとのコース開講一覧を取得 | ✅ 完了 |
| GetCourseOfferingQueryHandler | GetCourseOfferingQuery | コース開講詳細を取得 | ✅ 完了 |
| CancelCourseOfferingCommandHandler | CancelCourseOfferingCommand | コース開講をキャンセル | ✅ 完了 |
| CopyCourseOfferingsFromPreviousSemesterCommandHandler | CopyCourseOfferingsFromPreviousSemesterCommand | 前学期のコース開講情報を一括コピー | ⬜ 未実装 |

### 履修登録 (Phase 5 - 未実装) → エピック5

| Handler | Command/Query | 説明 | 実装状態 |
|---------|--------------|------|----------|
| EnrollStudentCommandHandler | EnrollStudentCommand | 履修登録 | ⬜ 未実装 |
| CancelEnrollmentCommandHandler | CancelEnrollmentCommand | 履修登録キャンセル | ⬜ 未実装 |
| GetStudentEnrollmentsQueryHandler | GetStudentEnrollmentsQuery | 履修登録一覧を取得 | ⬜ 未実装 |
| CompleteEnrollmentCommandHandler | CompleteEnrollmentCommand | 履修登録を完了 | ⬜ 未実装 |

---

## エピック1: 学生管理

### ✅ US-S01: 学生を登録できる

**ストーリー:**
API利用者として、新しい学生をシステムに登録できるようにしたい。なぜなら、学生が履修登録するためにはアカウントが必要だから。

**Handler:** `CreateStudentCommandHandler : IRequestHandler<CreateStudentCommand, Guid>`

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

### ⬜ US-S02: 学生情報を更新できる

**ストーリー:**
API利用者として、学生情報を更新できるようにしたい。なぜなら、メールアドレスや学年情報が変更になることがあるから。

**Handler:** `UpdateStudentCommandHandler : IRequestHandler<UpdateStudentCommand, Unit>`

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

### ✅ US-S03: 学生一覧を取得できる

**ストーリー:**
API利用者として、登録されている学生の一覧を取得できるようにしたい。なぜなら、学生管理画面で全学生を確認したり、特定の条件で学生を検索する必要があるから。

**Handler:** `GetStudentsQueryHandler : IRequestHandler<GetStudentsQuery, List<StudentDto>>`

**受け入れ条件:**

```gherkin
Scenario: 全学生を取得する
  Given データベースに以下の学生が登録されている
    | 学生ID | 名前 | メールアドレス | 学年 |
    | id-001 | 山田太郎 | yamada@example.com | 1 |
    | id-002 | 鈴木花子 | suzuki@example.com | 2 |
    | id-003 | 田中次郎 | tanaka@example.com | 3 |
  When GetStudentsQueryを実行する
    - フィルタ条件: なし
  Then 3件のStudentDtoが返される
  And 学生が登録日時の昇順でソートされている
```

```gherkin
Scenario: 学年でフィルタリングして学生を取得する
  Given データベースに学年1, 2, 3の学生が混在して登録されている
  When GetStudentsQueryを実行する
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
  When GetStudentsQueryを実行する
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
  When GetStudentsQueryを実行する
    - Email: "example.com"
  Then 部分一致する学生が全て返される
```

```gherkin
Scenario: 複数の条件を組み合わせて検索する
  Given データベースに複数の学生が登録されている
  When GetStudentsQueryを実行する
    - Grade: 2
    - Name: "山田"
  Then 学年が 2 且つ名前に "山田" が含まれる学生のみが返される
```

```gherkin
Scenario: 検索結果が0件の場合
  Given データベースに学生が登録されている
  When GetStudentsQueryを実行する
    - Name: "存在しない名前"
  Then 空のリスト（0件）が返される
```

```gherkin
Scenario: 学生が1件も登録されていない場合
  Given データベースに学生が登録されていない
  When GetStudentsQueryを実行する
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

### ✅ US-S04: 学生を取得できる

**ストーリー:**
API利用者として、学生IDを指定して特定の学生情報を取得できるようにしたい。なぜなら、学生の詳細情報を表示したり、編集フォームに学生情報をロードする必要があるから。

**Handler:** `GetStudentQueryHandler : IRequestHandler<GetStudentQuery, StudentDto>`

**受け入れ条件:**

```gherkin
Scenario: 存在する学生IDで学生を取得する
  Given データベースに学生ID "123e4567-e89b-12d3-a456-426614174000" の学生が登録されている
    - StudentId: "123e4567-e89b-12d3-a456-426614174000"
    - Name: "山田太郎"
    - Email: "yamada@example.com"
    - Grade: 2
  When GetStudentQueryを実行する
    - StudentId: "123e4567-e89b-12d3-a456-426614174000"
  Then StudentDtoが返される
  And 学生IDが "123e4567-e89b-12d3-a456-426614174000" である
  And 学生名が "山田太郎" である
  And メールアドレスが "yamada@example.com" である
  And 学年が 2 である
```

```gherkin
Scenario: 存在しない学生IDで取得を試みる
  Given データベースに学生ID "99999999-9999-9999-9999-999999999999" の学生が登録されていない
  When GetStudentQueryを実行する
    - StudentId: "99999999-9999-9999-9999-999999999999"
  Then KeyNotFoundException がスローされる
  And エラーメッセージに "not found" が含まれる
```

**制約:**

- 学生IDはGUID形式
- 存在しない学生の場合はKeyNotFoundException

**実装状態:** ✅ 完了


---

## エピック2: 学期管理

### ⬜ US-M01: 学期情報を管理できる

**ストーリー:**
管理者として、学期情報（年度、学期、期間）を管理できるようにしたい。なぜなら、履修登録は学期ごとに管理される必要があるから。

**Handler:** `CreateSemesterCommandHandler : IRequestHandler<CreateSemesterCommand, SemesterId>`

**受け入れ条件:**

```gherkin
Scenario: 新しい学期を登録する
  Given SemesterRepositoryが利用可能である
  When CreateSemesterCommandを実行する
    - Year: 2024
    - Period: Spring
    - StartDate: 2024-04-01
    - EndDate: 2024-09-30
    - EnrollmentDeadline: 2024-04-15
    - CancellationDeadline: 2024-05-31
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

```gherkin
Scenario: 期限の順序が不正な学期を登録しようとする
  Given SemesterRepositoryが利用可能である
  When CreateSemesterCommandを実行する
    - Year: 2024
    - Period: Spring
    - StartDate: 2024-04-01
    - EndDate: 2024-09-30
    - EnrollmentDeadline: 2024-06-01
    - CancellationDeadline: 2024-05-31
  Then DomainException "Deadlines must be: StartDate < EnrollmentDeadline < CancellationDeadline < EndDate" がスローされる
  And SemesterRepositoryに保存されない
```

**制約:**

- 学期: Spring または Fall
- 年度: 2000〜2100の範囲
- 同じ年度・学期の組み合わせは一意
- 日付順序: StartDate < EnrollmentDeadline < CancellationDeadline < EndDate

**実装状態:** ⬜ 未実装

---

### ⬜ US-M02: 学期一覧を取得できる

**ストーリー:**
学生・教員として、登録されている学期の一覧を取得できるようにしたい。なぜなら、履修登録時に選択可能な学期を表示する必要があるから。

**Handler:** `GetSemestersQueryHandler : IRequestHandler<GetSemestersQuery, List<SemesterDto>>`

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

**Handler:** `GetCurrentSemesterQueryHandler : IRequestHandler<GetCurrentSemesterQuery, SemesterDto>`

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

## エピック3: コースマスタ管理

### ✅ US-E01: コースマスタを登録できる

**ストーリー:**
API利用者として、新しいコースマスタを登録できるようにしたい。なぜなら、コース開講や履修登録のためにコースの基本情報が必要だから。

**Handler:** `CreateCourseCommandHandler : IRequestHandler<CreateCourseCommand, string>`

**受け入れ条件:**

```gherkin
Scenario: 有効なコース情報で新しいコースマスタを登録する
  Given データベースが利用可能である
  When CreateCourseCommandを実行する
    - CourseCode: "CS101"
    - Name: "プログラミング入門"
    - Description: "プログラミングの基礎を学ぶ入門コース"
  Then コースコード "CS101" が返される
  And データベースにコースマスタが保存されている
  And コース名が "プログラミング入門" である
  And 説明が "プログラミングの基礎を学ぶ入門コース" である
```

```gherkin
Scenario: 不正なコースコード形式で登録を試みる
  Given データベースが利用可能である
  When CreateCourseCommandを実行する
    - CourseCode: "invalid-code"
    - Name: "テストコース"
    - Description: "テスト用の説明"
  Then ArgumentException がスローされる
  And エラーメッセージに "Invalid course code format" が含まれる
```

```gherkin
Scenario: 既に存在するコースコードで登録を試みる
  Given コースコード "CS101" のコースが既にデータベースに登録されている
  When CreateCourseCommandを実行する
    - CourseCode: "CS101"
    - Name: "新しいコース"
    - Description: "新しい説明"
  Then InvalidOperationException がスローされる
  And エラーメッセージに "already exists" が含まれる
```

**制約:**

- コースコード形式: 大文字2-4文字 + 数字3-4桁（例: CS101, MATH1001）
- コース名: 必須、空白不可
- 説明: オプション（nullまたは空文字列可）
- 注意: 単位数(Credits)と定員(MaxCapacity)はコース開講時(CourseOffering)に指定する

**実装状態:** ✅ 完了

---

### ✅ US-E02: コースマスタ一覧を取得できる

**ストーリー:**
API利用者として、登録されているコースマスタ一覧を取得できるようにしたい。なぜなら、コース開講の際にコースマスタ情報が必要だから。

**Handler:** `GetCoursesQueryHandler : IRequestHandler<GetCoursesQuery, List<CourseDto>>`

**受け入れ条件:**

```gherkin
Scenario: 登録されている全コースマスタを取得する
  Given データベースに以下のコースマスタが登録されている
    | コースコード | コース名       | 説明                   |
    | CS101        | プログラミング入門 | プログラミングの基礎   |
    | MATH201      | 線形代数       | 行列と線形変換         |
  When GetCoursesQueryを実行する
  Then 2件のCourseDtoが返される
  And 1件目のコースコードが "CS101" である
  And 1件目のコース名が "プログラミング入門" である
  And 2件目のコースコードが "MATH201" である
  And 2件目のコース名が "線形代数" である
```

```gherkin
Scenario: コースマスタが1件も登録されていない場合
  Given データベースにコースマスタが登録されていない
  When GetCoursesQueryを実行する
  Then 空のリスト（0件）が返される
```

**制約:**

- 全てのコースマスタを取得（ページネーション未実装）
- コース情報は読み取り専用
- **Phase 3.5実装後の注意**: 実際の開講情報(単位数、定員、教員)は `SelectCourseOfferingsBySemesterQuery` (US-CO03) で取得すること

**実装状態:** ✅ 完了

---

### ✅ US-E03: コースマスタを取得できる

**ストーリー:**
API利用者として、コースコードを指定して特定のコースマスタの詳細情報を取得できるようにしたい。なぜなら、コース開講時や履修登録前にコースの基本情報を確認する必要があるから。

**Handler:** `GetCourseByCodeQueryHandler : IRequestHandler<GetCourseByCodeQuery, CourseDto>`

**受け入れ条件:**

```gherkin
Scenario: 存在するコースコードでコースマスタを取得する
  Given データベースにコースコード "CS101" のコースマスタが登録されている
    - CourseCode: "CS101"
    - Name: "プログラミング入門"
    - Description: "プログラミングの基礎を学ぶ入門コース"
  When GetCourseByCodeQueryを実行する
    - CourseCode: "CS101"
  Then CourseDtoが返される
  And コースコードが "CS101" である
  And コース名が "プログラミング入門" である
  And 説明が "プログラミングの基礎を学ぶ入門コース" である
```

```gherkin
Scenario: 存在しないコースコードで取得を試みる
  Given データベースにコースコード "XXX999" のコースマスタが登録されていない
  When GetCourseByCodeQueryを実行する
    - CourseCode: "XXX999"
  Then KeyNotFoundException がスローされる
  And エラーメッセージに "not found" が含まれる
```

**制約:**

- コースコードは大文字小文字を区別しない（自動的に大文字変換）
- 存在しないコースの場合は404エラー
- **Phase 3.5実装後の注意**: 学期ごとの開講情報(単位数、定員、教員、ステータス)は `GetCourseOfferingQuery` (US-CO04) を使用すること

**実装状態:** ✅ 完了

---

## エピック4: コース開講管理

### US-CO01: コース開講を登録できる

**ストーリー:**
API利用者として、特定の学期にコースを開講登録できるようにしたい。なぜなら、学生が履修登録するためには学期ごとの開講情報が必要だから。

**Handler:** `CreateCourseOfferingCommandHandler : IRequestHandler<CreateCourseOfferingCommand, int>`

**受け入れ条件:**

```gherkin
Scenario: 有効なコース開講情報で新しいコース開講を登録する
  Given データベースにコースコード "CS101" のコースマスタが登録されている
  And データベースに学期 (2024, Spring) が登録されている
  When CreateCourseOfferingCommandを実行する
    - CourseCode: "CS101"
    - SemesterId: (2024, Spring)
    - Credits: 3
    - MaxCapacity: 30
    - Instructor: "田中教授"
  Then OfferingIdが返される
  And データベースにCourseOfferingが保存されている
  And コースコードが "CS101" である
  And 単位数が 3 である
  And 定員が 30 である
  And 教員が "田中教授" である
  And ステータスが "Active" である
```

```gherkin
Scenario: 存在しないコースコードで開講を試みる
  Given データベースにコースコード "XXX999" のコースマスタが登録されていない
  And データベースに学期 (2024, Spring) が登録されている
  When CreateCourseOfferingCommandを実行する
    - CourseCode: "XXX999"
    - SemesterId: (2024, Spring)
    - Credits: 3
    - MaxCapacity: 30
  Then KeyNotFoundException がスローされる
  And エラーメッセージに "Course not found" が含まれる
```

```gherkin
Scenario: 同一学期に既に開講されているコースで登録を試みる
  Given データベースにコースコード "CS101" のコースマスタが登録されている
  And データベースに学期 (2024, Spring) が登録されている
  And 学期 (2024, Spring) にコース "CS101" が既に開講されている
  When CreateCourseOfferingCommandを実行する
    - CourseCode: "CS101"
    - SemesterId: (2024, Spring)
    - Credits: 4
    - MaxCapacity: 25
  Then InvalidOperationException がスローされる
  And エラーメッセージに "already offered in this semester" が含まれる
```

**制約:**

- CourseCodeとSemesterIdの組み合わせはユニーク
- 単位数: 1〜10の範囲
- 定員: 1以上の整数
- 教員名: オプション（nullまたは空文字列可）
- ステータス: デフォルトで "Active"

**実装状態:** ⬜ 未実装

---

### US-CO02: コース開講情報を更新できる

**ストーリー:**
API利用者として、既に登録されたコース開講の情報（単位数、定員、教員）を更新できるようにしたい。なぜなら、開講後に変更が必要になる場合があるから。

**Handler:** `UpdateCourseOfferingCommandHandler : IRequestHandler<UpdateCourseOfferingCommand, Unit>`

**受け入れ条件:**

```gherkin
Scenario: 既存のコース開講情報を更新する
  Given データベースに以下のCourseOfferingが存在する
    - OfferingId: 1
    - CourseCode: "CS101"
    - SemesterId: (2024, Spring)
    - Credits: 3
    - MaxCapacity: 30
    - Instructor: "田中教授"
    - Status: Active
  When UpdateCourseOfferingCommandを実行する
    - OfferingId: 1
    - Credits: 4
    - MaxCapacity: 35
    - Instructor: "鈴木教授"
  Then 更新が成功する
  And データベースのCourseOfferingが更新されている
  And 単位数が 4 である
  And 定員が 35 である
  And 教員が "鈴木教授" である
```

```gherkin
Scenario: 存在しないOfferingIdで更新を試みる
  Given データベースにOfferingId 999 のCourseOfferingが存在しない
  When UpdateCourseOfferingCommandを実行する
    - OfferingId: 999
    - Credits: 3
    - MaxCapacity: 30
  Then KeyNotFoundException がスローされる
  And エラーメッセージに "CourseOffering not found" が含まれる
```

```gherkin
Scenario: キャンセル済みのコース開講を更新しようとする
  Given データベースに以下のCourseOfferingが存在する
    - OfferingId: 1
    - Status: Cancelled
  When UpdateCourseOfferingCommandを実行する
    - OfferingId: 1
    - Credits: 4
  Then InvalidOperationException がスローされる
  And エラーメッセージに "Cannot update cancelled offering" が含まれる
```

**制約:**

- CourseCodeとSemesterIdは変更不可（新規開講として作成すること）
- Statusが"Cancelled"の場合は更新不可
- 単位数: 1〜10の範囲
- 定員: 1以上の整数

**実装状態:** ⬜ 未実装

---

### US-CO03: 学期ごとのコース開講一覧を取得できる

**ストーリー:**
API利用者として、特定の学期に開講されているコース一覧を取得できるようにしたい。なぜなら、学生が履修登録可能なコースを確認する必要があるから。

**Handler:** `SelectCourseOfferingsBySemesterQueryHandler : IRequestHandler<SelectCourseOfferingsBySemesterQuery, List<CourseOfferingDto>>`

**受け入れ条件:**

```gherkin
Scenario: 特定学期の全コース開講を取得する
  Given データベースにコースコード "CS101" のコースマスタが登録されている
  And データベースにコースコード "MATH201" のコースマスタが登録されている
  And データベースに学期 (2024, Spring) が登録されている
  And 学期 (2024, Spring) に以下のCourseOfferingが存在する
    | CourseCode | Credits | MaxCapacity | Instructor | Status |
    | CS101      | 3       | 30          | 田中教授   | Active |
    | MATH201    | 4       | 25          | 鈴木教授   | Active |
  When SelectCourseOfferingsBySemesterQueryを実行する
    - SemesterId: (2024, Spring)
  Then 2件のCourseOfferingDtoが返される
  And 1件目のコースコードが "CS101" である
  And 1件目の単位数が 3 である
  And 2件目のコースコードが "MATH201" である
```

```gherkin
Scenario: Activeステータスのみをフィルタリングして取得する
  Given データベースに学期 (2024, Spring) が登録されている
  And 学期 (2024, Spring) に以下のCourseOfferingが存在する
    | CourseCode | Status    |
    | CS101      | Active    |
    | MATH201    | Cancelled |
    | ENG101     | Active    |
  When SelectCourseOfferingsBySemesterQueryを実行する
    - SemesterId: (2024, Spring)
    - StatusFilter: Active
  Then 2件のCourseOfferingDtoが返される
  And 全てのステータスが "Active" である
```

```gherkin
Scenario: 開講が1件も登録されていない学期
  Given データベースに学期 (2024, Fall) が登録されている
  And 学期 (2024, Fall) にCourseOfferingが存在しない
  When SelectCourseOfferingsBySemesterQueryを実行する
    - SemesterId: (2024, Fall)
  Then 空のリスト（0件）が返される
```

**制約:**

- StatusFilterパラメータはオプション（未指定時は全ステータスを返す）
- CourseOfferingDtoにはコースマスタ情報（Name, Description）も含める
- ページネーション未実装

**実装状態:** ✅ 完了

---

### US-CO04: コース開講詳細を取得できる

**ストーリー:**
API利用者として、OfferingIdを指定して特定のコース開講の詳細情報を取得できるようにしたい。なぜなら、履修登録前にコースの詳細を確認する必要があるから。

**Handler:** `GetCourseOfferingQueryHandler : IRequestHandler<GetCourseOfferingQuery, CourseOfferingDto>`

**受け入れ条件:**

```gherkin
Scenario: 存在するOfferingIdでコース開講詳細を取得する
  Given データベースに以下のCourseOfferingが存在する
    - OfferingId: 1
    - CourseCode: "CS101"
    - CourseName: "プログラミング入門"
    - SemesterId: (2024, Spring)
    - Credits: 3
    - MaxCapacity: 30
    - Instructor: "田中教授"
    - Status: Active
  When GetCourseOfferingQueryを実行する
    - OfferingId: 1
  Then CourseOfferingDtoが返される
  And OfferingIdが 1 である
  And コースコードが "CS101" である
  And コース名が "プログラミング入門" である
  And 単位数が 3 である
  And 定員が 30 である
  And 教員が "田中教授" である
```

```gherkin
Scenario: 存在しないOfferingIdで取得を試みる
  Given データベースにOfferingId 999 のCourseOfferingが存在しない
  When GetCourseOfferingQueryを実行する
    - OfferingId: 999
  Then KeyNotFoundException がスローされる
  And エラーメッセージに "CourseOffering not found" が含まれる
```

**制約:**

- CourseOfferingDtoにはコースマスタ情報（Name, Description）も含める
- 学期情報（Year, Term）も含める
- Statusに関わらず全ての開講情報を返す

**実装状態:** ⬜ 未実装

---

### US-CO05: コース開講をキャンセルできる

**ストーリー:**
API利用者として、既に登録されたコース開講をキャンセルできるようにしたい。なぜなら、教員の都合や履修者数不足により開講を取りやめる場合があるから。

**Handler:** `CancelCourseOfferingCommandHandler : IRequestHandler<CancelCourseOfferingCommand, Unit>`

**受け入れ条件:**

```gherkin
Scenario: Activeなコース開講をキャンセルする
  Given データベースに以下のCourseOfferingが存在する
    - OfferingId: 1
    - Status: Active
  When CancelCourseOfferingCommandを実行する
    - OfferingId: 1
  Then キャンセルが成功する
  And データベースのCourseOfferingのステータスが "Cancelled" に更新されている
```

```gherkin
Scenario: 既にキャンセル済みのコース開講をキャンセルしようとする
  Given データベースに以下のCourseOfferingが存在する
    - OfferingId: 1
    - Status: Cancelled
  When CancelCourseOfferingCommandを実行する
    - OfferingId: 1
  Then InvalidOperationException がスローされる
  And エラーメッセージに "already cancelled" が含まれる
```

```gherkin
Scenario: 履修登録者がいるコース開講をキャンセルしようとする
  Given データベースに以下のCourseOfferingが存在する
    - OfferingId: 1
    - Status: Active
  And OfferingId 1 に3名の学生が履修登録している
  When CancelCourseOfferingCommandを実行する
    - OfferingId: 1
  Then InvalidOperationException がスローされる
  And エラーメッセージに "Cannot cancel offering with enrollments" が含まれる
```

**制約:**

- キャンセル後の再有効化は不可（新規開講として作成すること）
- 履修登録者が1名でもいる場合はキャンセル不可
- キャンセルは論理削除（物理削除は行わない）

**実装状態:** ⬜ 未実装

---

### US-CO06: 前学期のコース開講情報を一括コピーできる

**ストーリー:**
API利用者として、前学期のコース開講情報を新学期に一括コピーできるようにしたい。なぜなら、毎学期同じコースを手動で登録するのは非効率だから。

**Handler:** `CopyCourseOfferingsFromPreviousSemesterCommandHandler : IRequestHandler<CopyCourseOfferingsFromPreviousSemesterCommand, CopyCourseOfferingsResult>`

**受け入れ条件:**

```gherkin
Scenario: 前学期の全コース開講情報を新学期にコピーする
  Given データベースに学期 (2024, Spring) が存在する
  And 学期 (2024, Spring) に以下のCourseOfferingが存在する
    | CourseCode | Credits | MaxCapacity | Instructor | Status |
    | CS101      | 3       | 30          | 田中教授   | Active |
    | MATH201    | 4       | 25          | 鈴木教授   | Active |
    | ENG101     | 2       | 50          | 佐藤教授   | Cancelled |
  And データベースに学期 (2024, Fall) が存在する
  And 学期 (2024, Fall) にCourseOfferingが存在しない
  When CopyCourseOfferingsFromPreviousSemesterCommandを実行する
    - FromSemesterId: (2024, Spring)
    - ToSemesterId: (2024, Fall)
  Then 2件のCourseOfferingが作成される（StatusがActiveのもののみ）
  And 結果のTotalCopiedが 2 である
  And 結果のSkippedが 1 である（Cancelledのため）
  And 新学期のCourseOfferingに "CS101" が存在する
  And 新学期のCourseOfferingに "MATH201" が存在する
  And 新学期のCourseOfferingに "ENG101" が存在しない
```

```gherkin
Scenario: 特定のコースのみをフィルタリングしてコピーする
  Given データベースに学期 (2024, Spring) が存在する
  And 学期 (2024, Spring) に以下のCourseOfferingが存在する
    | CourseCode | Status |
    | CS101      | Active |
    | MATH201    | Active |
    | ENG101     | Active |
  And データベースに学期 (2024, Fall) が存在する
  When CopyCourseOfferingsFromPreviousSemesterCommandを実行する
    - FromSemesterId: (2024, Spring)
    - ToSemesterId: (2024, Fall)
    - CourseCodeFilter: ["CS101", "MATH201"]
  Then 2件のCourseOfferingが作成される
  And 新学期のCourseOfferingに "CS101" が存在する
  And 新学期のCourseOfferingに "MATH201" が存在する
  And 新学期のCourseOfferingに "ENG101" が存在しない
```

```gherkin
Scenario: 定員を一括で上書きしてコピーする
  Given データベースに学期 (2024, Spring) が存在する
  And 学期 (2024, Spring) に以下のCourseOfferingが存在する
    | CourseCode | MaxCapacity |
    | CS101      | 30          |
    | MATH201    | 25          |
  And データベースに学期 (2024, Fall) が存在する
  When CopyCourseOfferingsFromPreviousSemesterCommandを実行する
    - FromSemesterId: (2024, Spring)
    - ToSemesterId: (2024, Fall)
    - OverrideMaxCapacity: 40
  Then 2件のCourseOfferingが作成される
  And 全てのMaxCapacityが 40 である
```

```gherkin
Scenario: 既に開講が存在するコースはスキップする
  Given データベースに学期 (2024, Spring) が存在する
  And 学期 (2024, Spring) に以下のCourseOfferingが存在する
    | CourseCode | Status |
    | CS101      | Active |
    | MATH201    | Active |
  And データベースに学期 (2024, Fall) が存在する
  And 学期 (2024, Fall) にコース "CS101" が既に開講されている
  When CopyCourseOfferingsFromPreviousSemesterCommandを実行する
    - FromSemesterId: (2024, Spring)
    - ToSemesterId: (2024, Fall)
  Then 1件のCourseOfferingが作成される（MATH201のみ）
  And 結果のTotalCopiedが 1 である
  And 結果のSkippedが 1 である（CS101は既存のため）
  And 結果のErrorsが空である
```

**制約:**

- Statusが"Cancelled"のコース開講はコピーしない
- CourseCodeFilterが指定された場合、そのコースのみコピーする
- OverrideMaxCapacityが指定された場合、全てのコースの定員を上書きする
- OverrideInstructorが指定された場合、全てのコースの教員を上書きする
- ToSemesterIdに既に同じCourseCodeの開講が存在する場合はスキップする
- エラーが発生したコースはスキップし、結果のErrorsリストに追加する

**Command定義:**

```csharp
public record CopyCourseOfferingsFromPreviousSemesterCommand : IRequest<CopyCourseOfferingsResult>
{
    public SemesterId FromSemesterId { get; init; }
    public SemesterId ToSemesterId { get; init; }
    public List<string>? CourseCodeFilter { get; init; }
    public int? OverrideMaxCapacity { get; init; }
    public string? OverrideInstructor { get; init; }
}

public class CopyCourseOfferingsResult
{
    public int TotalCopied { get; init; }
    public int Skipped { get; init; }
    public List<string> Errors { get; init; } = new();
}
```

**実装状態:** ⬜ 未実装

---

## エピック5: 履修登録

### ⬜ US-R01: コースを履修登録できる

**ストーリー:**
API利用者として、学生がコースを履修登録できるようにしたい。なぜなら、学期の授業を受講するためには事前に履修登録が必要だから。

**Handler:** `EnrollStudentCommandHandler : IRequestHandler<EnrollStudentCommand, EnrollmentId>`

**受け入れ条件:**

```gherkin
Scenario: 学生が有効なコース開講を履修登録する
  Given データベースに学生ID "123e4567-e89b-12d3-a456-426614174000" の学生が登録されている
  And データベースに以下のCourseOfferingが存在する
    - OfferingId: 1
    - CourseCode: "CS101"
    - SemesterId: (2024, Spring)
    - Credits: 3
    - MaxCapacity: 30
    - Status: Active
  And 学生の現在の履修単位数が 12単位 である
  When EnrollStudentCommandを実行する
    - StudentId: "123e4567-e89b-12d3-a456-426614174000"
    - OfferingId: 1
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
Scenario: 同じコース開講を重複して登録を試みる
  Given データベースにOfferingId 1 のCourseOfferingが存在する
  And 学生がOfferingId 1 を既に履修登録している
  When EnrollStudentCommandを実行する
    - StudentId: （同じ学生ID）
    - OfferingId: 1
  Then InvalidOperationException がスローされる
  And エラーメッセージに "Already enrolled" が含まれる
```

```gherkin
Scenario: 同一年度内で同じコースを異なる学期に重複登録を試みる
  Given データベースに学生ID "123e4567-e89b-12d3-a456-426614174000" の学生が登録されている
  And データベースに以下のCourseOfferingが存在する
    - OfferingId: 1, CourseCode: "CS101", SemesterId: (2024, Spring)
    - OfferingId: 2, CourseCode: "CS101", SemesterId: (2024, Fall)
  And 学生が2024年Spring学期のCS101 (OfferingId: 1) を既に履修登録している
  When EnrollStudentCommandを実行する
    - StudentId: "123e4567-e89b-12d3-a456-426614174000"
    - OfferingId: 2  # 同じ年度の別学期
  Then InvalidOperationException がスローされる
  And エラーメッセージに "Course CS101 already enrolled in year 2024" が含まれる
```

```gherkin
Scenario: 定員オーバーのコース開講を登録を試みる
  Given データベースに以下のCourseOfferingが存在する
    - OfferingId: 1
    - MaxCapacity: 30
  And OfferingId 1 に既に 30名 の学生が履修登録している
  When 31人目の学生がEnrollStudentCommandを実行する
    - OfferingId: 1
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
Scenario: 存在しないOfferingIdで登録を試みる
  Given データベースにOfferingId 999 のCourseOfferingが存在しない
  When EnrollStudentCommandを実行する
    - OfferingId: 999
  Then KeyNotFoundException がスローされる
  And エラーメッセージに "CourseOffering not found" が含まれる
```

```gherkin
Scenario: 履修登録期限を過ぎて登録を試みる
  Given データベースに学生ID "123e4567-e89b-12d3-a456-426614174000" の学生が登録されている
  And データベースに以下のCourseOfferingが存在する
    - OfferingId: 1, SemesterId: (2024, Spring)
  And 2024年Spring学期の EnrollmentDeadline が 2024-04-15 である
  And 現在日時が 2024-04-20 である
  When EnrollStudentCommandを実行する
    - StudentId: "123e4567-e89b-12d3-a456-426614174000"
    - OfferingId: 1
  Then InvalidOperationException がスローされる
  And エラーメッセージに "Enrollment deadline has passed" が含まれる
```

**制約:**

- 最大履修単位数: 24単位/学期
- 同じOfferingIdの重複登録は不可
- 同じCourseCodeを同一年度内で複数回履修不可（例: 2024年SpringでCS101履修済み → 2024年FallでCS101は不可）
- 定員オーバーは不可
- CourseOfferingが事前に登録されている必要がある
- CourseOfferingのStatusが"Active"である必要がある
- 履修登録は学期の EnrollmentDeadline までに行う必要がある
- 初期ステータス: InProgress

**実装状態:** ⬜ 未実装

---

### ⬜ US-R02: 履修登録をキャンセルできる

**ストーリー:**
API利用者として、履修登録をキャンセルできるようにしたい。なぜなら、履修計画を変更したい場合があるから。

**Handler:** `CancelEnrollmentCommandHandler : IRequestHandler<CancelEnrollmentCommand, Unit>`

**受け入れ条件:**

```gherkin
Scenario: 進行中の履修登録をキャンセルする
  Given データベースに履修登録ID "abc-123" の履修登録が存在する
  And 履修登録ステータスが "InProgress" である
  When CancelEnrollmentCommandを実行する
    - EnrollmentId: "abc-123"
  Then 正常に完了する（戻り値なし）
  And データベースの履修登録ステータスが "Cancelled" になる
  And 学生の履修単位数が減算される
```

```gherkin
Scenario: 既に完了している履修登録をキャンセルしようとする
  Given データベースに履修登録ID "abc-123" の履修登録が存在する
  And 履修登録ステータスが "Completed" である
  When CancelEnrollmentCommandを実行する
    - EnrollmentId: "abc-123"
  Then InvalidOperationException がスローされる
  And エラーメッセージに "Cannot cancel completed enrollment" が含まれる
  And 履修登録ステータスは変更されない
```

```gherkin
Scenario: 存在しない履修登録IDでキャンセルを試みる
  Given データベースに履修登録ID "99999999" の履修登録が存在しない
  When CancelEnrollmentCommandを実行する
    - EnrollmentId: "99999999"
  Then KeyNotFoundException がスローされる
  And エラーメッセージに "Enrollment not found" が含まれる
```

```gherkin
Scenario: キャンセル期限を過ぎてキャンセルを試みる
  Given データベースに履修登録ID "abc-123" の履修登録が存在する
  And 履修登録ステータスが "InProgress" である
  And 履修登録の学期が (2024, Spring) である
  And 2024年Spring学期の CancellationDeadline が 2024-05-15 である
  And 現在日時が 2024-05-20 である
  When CancelEnrollmentCommandを実行する
    - EnrollmentId: "abc-123"
  Then InvalidOperationException がスローされる
  And エラーメッセージに "Cancellation deadline has passed" が含まれる
```

**制約:**

- キャンセル可能なステータス: InProgress のみ
- キャンセル後は履修単位数から減算
- Completed または Cancelled ステータスはキャンセル不可
- キャンセルは学期の CancellationDeadline までに行う必要がある

**実装状態:** ⬜ 未実装

---

### ⬜ US-R03: 履修登録一覧を取得できる

**ストーリー:**
学生・教員として、学生の履修登録一覧を取得できるようにしたい。なぜなら、現在の履修状況を確認する必要があるから。

**Handler:** `GetStudentEnrollmentsQueryHandler : IRequestHandler<GetStudentEnrollmentsQuery, List<EnrollmentDto>>`

**受け入れ条件:**

```gherkin
Scenario: 学生の全ての履修登録を取得する
  Given StudentRepositoryにStudentId "student-001" が存在する
  And EnrollmentRepositoryに以下のEnrollmentが存在する
    | OfferingId | CourseCode | SemesterId      | Status     |
    | 1          | CS101      | (2024, Spring)  | InProgress |
    | 2          | MATH201    | (2024, Spring)  | InProgress |
    | 3          | ENG101     | (2023, Fall)    | Completed  |
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

**Handler:** `CompleteEnrollmentCommandHandler : IRequestHandler<CompleteEnrollmentCommand, Unit>`

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

### コースマスタ（Course）

- **コースコード**: `^[A-Z]{2,4}\d{3,4}$`（例: CS101, MATH1001）
- **コース名**: 必須、空白不可
- **説明**: オプション
- **注意**: 単位数(Credits)と定員(MaxCapacity)はコース開講(CourseOffering)で管理

### 学生（Student）

- **学生ID**: UUID（自動生成）
- **メールアドレス**: 一意制約、メール形式
- **学年**: 1〜4

### 履修登録（Enrollment）

- **エンティティ構造**:
  - EnrollmentId: UUID（自動生成）
  - StudentId: UUID（外部キー）
  - OfferingId: INT（外部キー）
  - SemesterId: 複合値オブジェクト（非正規化、検索・集計用）
  - CourseCode: VARCHAR(10)（非正規化、重複チェック用）
  - Credits: INT（非正規化、単位計算用）
  - Status: ENUM（InProgress, Completed, Cancelled）
- **非正規化の理由**:
  - 学期ごとの履修単位数集計の効率化
  - 同一コース重複チェックの高速化
  - CourseOfferingがキャンセルされても履修履歴を保持
- **最大履修単位数**: 24単位/学期
- **単位数計算ルール**:
  - 現在の履修単位数: 同一学期内で Status = InProgress の Credits 合計
  - 累積取得単位数: 全学期で Status = Completed の Credits 合計
  - Cancelled は計算から除外
- **ステータス遷移**:
  - `InProgress` → `Completed` ✅
  - `InProgress` → `Cancelled` ✅
  - `Completed` → (変更不可) ❌
  - `Cancelled` → (変更不可) ❌
- **重複登録防止**:
  - 同じOfferingIdは不可
  - 同じCourseCodeを同一年度内で複数回履修不可（再履修ケースは別途検討）
- **定員チェック**: CourseOfferingの定員オーバーは登録不可
- **開講状態チェック**: CourseOfferingのStatusが"Active"である必要がある
- **期限チェック**: Semesterの履修登録期限内のみ登録可能

### 学期（Semester）

- **年度**: 2000〜2100
- **学期**: Spring または Fall
- **一意制約**: 年度 + 学期
- **期間管理**:
  - StartDate: 学期開始日
  - EndDate: 学期終了日
  - EnrollmentDeadline: 履修登録締切日（この日まで新規登録可能）
  - CancellationDeadline: 履修キャンセル締切日（この日まで取消可能）
- **制約**: StartDate < EnrollmentDeadline < CancellationDeadline < EndDate

### コース開講（CourseOffering）

- **OfferingId**: INT（自動生成、主キー）
- **CourseCode + SemesterId**: 一意制約（同一学期に同じコースは1回のみ開講可能）
- **単位数（Credits）**: 1〜10
- **定員（MaxCapacity）**: 1以上
- **教員（Instructor）**: オプション、最大100文字
- **ステータス種別**: Active（開講中）、Cancelled（キャンセル済み）
- **ステータス遷移**:
  - 初期状態: `Active`（デフォルト）
  - `Active` → `Cancelled` ✅
  - `Cancelled` → `Active` ❌（再有効化不可、新規開講として作成）
- **履修登録への影響**:
  - Active状態のCourseOfferingのみ新規履修登録可能
  - 履修登録者が1名でもいる場合はキャンセル不可
- **一括コピー機能**:
  - Statusが"Cancelled"のCourseOfferingはコピー対象外
  - 既に同じCourseCodeの開講が存在する場合はスキップ

---

## 実装優先順位

**注**: Phase番号は実装順序を示します。エピック番号とは異なります。

### Phase 1: 学生管理（完了済み） → エピック1

- ✅ US-S01: 学生登録
- ✅ US-S02: 学生情報更新
- ✅ US-S03: 学生一覧取得（条件絞込可能）
- ✅ US-S04: 学生取得

**理由**: 最もシンプルで他機能への依存なし。学生IDの発行が全ての履修管理の前提。

### Phase 2: 学期管理（完了済み） → エピック2

- ✅ US-M01: 学期登録
- ✅ US-M02: 学期一覧取得
- ✅ US-M03: 現在の学期取得

**理由**: 学期の概念が確立されないとコース開講や履修登録ができない。学生管理の次に必要。

### Phase 3: コースマスタ管理（完了済み） → エピック3

- ✅ US-E01: コースマスタ登録
- ✅ US-E02: コースマスタ一覧閲覧
- ✅ US-E03: コースマスタ検索

**理由**: コース開講の前提となるカタログ情報。学期と独立して登録可能。

### Phase 4: コース開講管理（次フェーズ） → エピック3.5

**優先順位1: コア開講管理機能**

- ⬜ US-CO01: コース開講を登録
- ⬜ US-CO02: コース開講情報を更新
- ⬜ US-CO05: コース開講をキャンセル

**優先順位2: クエリ機能**

- ⬜ US-CO03: 学期ごとのコース開講一覧を取得
- ⬜ US-CO04: コース開講詳細を取得

**優先順位3: 効率化機能**

- ⬜ US-CO06: 前学期のコース開講情報を一括コピー

**理由**: 学期・コースマスタに依存。開講情報が揃わないと履修登録不可。

### Phase 5: 履修登録コア機能 → エピック4

- ⬜ US-R01: 履修登録（学期依存）
- ⬜ US-R02: 履修登録キャンセル
- ⬜ US-R03: 履修登録一覧閲覧
- ⬜ US-R04: 履修登録完了

**理由**: 全機能に依存。最も複雑なビジネスルール（重複防止、期限チェック、単位上限）を含む。

---

## テスト実装戦略

詳細は [testing-strategy.md](impl-patterns/testing-strategy.md) を参照してください。
