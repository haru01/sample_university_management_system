# StudentRegistrations（学生在籍管理）ユーザーストーリー

## 概要

このドキュメントは、学生在籍管理システムのユーザーストーリーと受け入れ条件を定義します。
各ストーリーにはGiven-When-Then形式の受け入れ条件（Applicationレイヤーの統合テスト視点）と制約事項を記載しています。

**対象範囲:** Applicationレイヤーの統合テスト（CommandHandler, QueryHandler, Repository層を含む）

**実装パターン:** MediatRを使用したCQRSパターン

## CommandHandler/QueryHandler一覧

### 学生在籍管理 (Phase 1 - 完了)

| Handler | Command/Query | 説明 | 実装状態 |
|---------|--------------|------|----------|
| CreateStudentCommandHandler | CreateStudentCommand | 学生を登録 | ✅ 完了 |
| GetStudentsQueryHandler | GetStudentsQuery | 学生一覧を取得 | ✅ 完了 |
| GetStudentQueryHandler | GetStudentQuery | 学生を取得 | ✅ 完了 |
| UpdateStudentCommandHandler | UpdateStudentCommand | 学生情報を更新 | ✅ 完了 |

### 在籍ステータス履歴管理 (Phase 2 - 予定)

| Handler | Command/Query | 説明 | 実装状態 |
|---------|--------------|------|----------|
| RecordEnrollmentCommandHandler | RecordEnrollmentCommand | 入学を記録 | ⬜ 未実装 |
| RecordLeaveOfAbsenceCommandHandler | RecordLeaveOfAbsenceCommand | 休学を記録 | ⬜ 未実装 |
| RecordReturnFromLeaveCommandHandler | RecordReturnFromLeaveCommand | 復学を記録 | ⬜ 未実装 |
| RecordGraduationCommandHandler | RecordGraduationCommand | 卒業を記録 | ⬜ 未実装 |
| RecordWithdrawalCommandHandler | RecordWithdrawalCommand | 退学を記録 | ⬜ 未実装 |
| GetStudentStatusHistoryQueryHandler | GetStudentStatusHistoryQuery | 学生のステータス履歴を取得 | ⬜ 未実装 |
| GetCurrentStudentStatusQueryHandler | GetCurrentStudentStatusQuery | 学生の現在のステータスを取得 | ⬜ 未実装 |

### 学年管理 (Phase 3 - 予定)

| Handler | Command/Query | 説明 | 実装状態 |
|---------|--------------|------|----------|
| RecordGradeChangeCommandHandler | RecordGradeChangeCommand | 学年変更（進級・留年）を記録 | ⬜ 未実装 |
| GetGradeChangeHistoryQueryHandler | GetGradeChangeHistoryQuery | 学生の学年変更履歴を取得 | ⬜ 未実装 |

### 学生イベント管理 (Phase 2 - 未実装)

| Handler | Command/Query | 説明 | 実装状態 |
|---------|--------------|------|----------|
| RecordEnrollmentEventCommandHandler | RecordEnrollmentEventCommand | 入学イベントを記録 | ⬜ 未実装 |
| RecordPromotionEventCommandHandler | RecordPromotionEventCommand | 進級イベントを記録 | ⬜ 未実装 |
| RecordLeaveEventCommandHandler | RecordLeaveEventCommand | 休学イベントを記録 | ⬜ 未実装 |
| RecordReinstatementEventCommandHandler | RecordReinstatementEventCommand | 復学イベントを記録 | ⬜ 未実装 |
| RecordWithdrawalEventCommandHandler | RecordWithdrawalEventCommand | 退学イベントを記録 | ⬜ 未実装 |
| RecordGraduationEventCommandHandler | RecordGraduationEventCommand | 卒業イベントを記録 | ⬜ 未実装 |
| GetStudentEventsQueryHandler | GetStudentEventsQuery | 学生のイベント履歴を取得 | ⬜ 未実装 |

---

TODO: 入学、進級、休学、復学、退学、卒業などの履歴残す。在学中なのか判定できるなにか。

## エピック1: 学生在籍管理

### ✅ US-S01: 学生を登録できる（入学記録を含む）

**ストーリー:**
API利用者として、新しい学生をシステムに登録できるようにしたい。なぜなら、学生が履修登録するためにはアカウントが必要だから。また、学生登録と同時に入学記録も自動的に作成されるべきである。

**Handler:** `CreateStudentCommandHandler : IRequestHandler<CreateStudentCommand, Guid>`

**変更予定（Phase 2）:**

- 入学日（EnrollmentDate）パラメータを追加
- 学年度（AcademicYear）パラメータを追加
- 学生作成と同時に入学ステータス履歴（StudentStatusHistory）を自動作成

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
Scenario: 入学日と学年度を指定して学生を登録する（Phase 2で追加）
  Given データベースが利用可能である
  When CreateStudentCommandを実行する
    - Name: "山田太郎"
    - Email: "yamada@example.com"
    - Grade: 1
    - EnrollmentDate: "2024-04-01"
    - AcademicYear: "2024"
  Then 自動生成された学生ID（Guid）が返される
  And データベースに学生が保存されている
  And 入学ステータス履歴が自動的に作成されている
  And ステータス履歴のステータスが "Enrolled" である
  And ステータス履歴の日付が "2024-04-01" である
  And ステータス履歴の備考に "2024年度入学" が含まれる
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

**変更予定（Phase 3）:**

- 学年（Grade）の更新機能を廃止予定
- 学年変更は `RecordGradeChangeCommand` を使用するように変更
- 名前とメールアドレスの更新のみ対応

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

## エピック2: 在籍ステータス履歴管理

### ⬜ US-S05: 入学を記録できる

**ストーリー:**
API利用者として、学生の入学を記録できるようにしたい。なぜなら、学生がいつ入学したかを正確に記録し、在籍期間を管理する必要があるから。

**Handler:** `RecordEnrollmentCommandHandler : IRequestHandler<RecordEnrollmentCommand, Guid>`

**受け入れ条件:**

```gherkin
Scenario: 学生の入学を記録する
  Given データベースに学生ID "123e4567-e89b-12d3-a456-426614174000" の学生が登録されている
  And その学生にステータス履歴が存在しない
  When RecordEnrollmentCommandを実行する
    - StudentId: "123e4567-e89b-12d3-a456-426614174000"
    - EnrollmentDate: "2024-04-01"
    - AcademicYear: "2024"
    - Note: "令和6年度入学"
  Then 自動生成されたステータス履歴ID（Guid）が返される
  And データベースにステータス履歴が保存されている
  And ステータスが "Enrolled"（在学中）である
  And 入学日が "2024-04-01" である
```

```gherkin
Scenario: 既に在学中の学生に入学を記録しようとする
  Given データベースに学生ID "123e4567-e89b-12d3-a456-426614174000" の学生が登録されている
  And その学生が既に "Enrolled" ステータスである
  When RecordEnrollmentCommandを実行する
    - StudentId: "123e4567-e89b-12d3-a456-426614174000"
  Then InvalidOperationException がスローされる
  And エラーメッセージに "Student is already enrolled" が含まれる
```

```gherkin
Scenario: 存在しない学生IDに入学を記録しようとする
  Given データベースに学生ID "99999999-9999-9999-9999-999999999999" の学生が登録されていない
  When RecordEnrollmentCommandを実行する
    - StudentId: "99999999-9999-9999-9999-999999999999"
  Then KeyNotFoundException がスローされる
```

**制約:**

- 入学日: 必須
- 学年度: 必須（例: "2024"）
- 備考: オプション
- 学生は同時に複数の入学ステータスを持つことはできない
- 入学

**実装状態:** ⬜ 未実装

---

### ⬜ US-S06: 休学を記録できる

**ストーリー:**
API利用者として、学生の休学を記録できるようにしたい。なぜなら、病気や経済的理由などで一時的に学業を中断する学生を適切に管理する必要があるから。

**Handler:** `RecordLeaveOfAbsenceCommandHandler : IRequestHandler<RecordLeaveOfAbsenceCommand, Guid>`

**受け入れ条件:**

```gherkin
Scenario: 在学中の学生の休学を記録する
  Given データベースに学生ID "123e4567-e89b-12d3-a456-426614174000" の学生が登録されている
  And その学生が "Enrolled" ステータスである
  When RecordLeaveOfAbsenceCommandを実行する
    - StudentId: "123e4567-e89b-12d3-a456-426614174000"
    - StartDate: "2024-10-01"
    - PlannedEndDate: "2025-03-31"
    - Reason: "病気療養のため"
  Then 自動生成されたステータス履歴ID（Guid）が返される
  And データベースにステータス履歴が保存されている
  And ステータスが "OnLeave"（休学中）である
  And 開始日が "2024-10-01" である
  And 予定終了日が "2025-03-31" である
```

```gherkin
Scenario: 在学中でない学生の休学を記録しようとする
  Given データベースに学生ID "123e4567-e89b-12d3-a456-426614174000" の学生が登録されている
  And その学生が "Graduated" ステータスである
  When RecordLeaveOfAbsenceCommandを実行する
    - StudentId: "123e4567-e89b-12d3-a456-426614174000"
  Then InvalidOperationException がスローされる
  And エラーメッセージに "Student must be enrolled" が含まれる
```

```gherkin
Scenario: 既に休学中の学生に休学を記録しようとする
  Given データベースに学生ID "123e4567-e89b-12d3-a456-426614174000" の学生が登録されている
  And その学生が既に "OnLeave" ステータスである
  When RecordLeaveOfAbsenceCommandを実行する
    - StudentId: "123e4567-e89b-12d3-a456-426614174000"
  Then InvalidOperationException がスローされる
  And エラーメッセージに "Student is already on leave" が含まれる
```

**制約:**

- 開始日: 必須
- 予定終了日: オプション（無期限休学の場合）
- 理由: オプション
- 休学できるのは在学中の学生のみ
- 休学中の学生は二重に休学できない

**実装状態:** ⬜ 未実装

---

### ⬜ US-S07: 復学を記録できる

**ストーリー:**
API利用者として、休学中の学生の復学を記録できるようにしたい。なぜなら、休学期間が終了して学業に戻る学生のステータスを適切に管理する必要があるから。

**Handler:** `RecordReturnFromLeaveCommandHandler : IRequestHandler<RecordReturnFromLeaveCommand, Guid>`

**受け入れ条件:**

```gherkin
Scenario: 休学中の学生の復学を記録する
  Given データベースに学生ID "123e4567-e89b-12d3-a456-426614174000" の学生が登録されている
  And その学生が "OnLeave" ステータスである
  When RecordReturnFromLeaveCommandを実行する
    - StudentId: "123e4567-e89b-12d3-a456-426614174000"
    - ReturnDate: "2025-04-01"
    - Note: "病気療養完了により復学"
  Then 自動生成されたステータス履歴ID（Guid）が返される
  And データベースにステータス履歴が保存されている
  And ステータスが "Enrolled"（在学中）に戻る
  And 復学日が "2025-04-01" である
```

```gherkin
Scenario: 休学中でない学生の復学を記録しようとする
  Given データベースに学生ID "123e4567-e89b-12d3-a456-426614174000" の学生が登録されている
  And その学生が "Enrolled" ステータスである
  When RecordReturnFromLeaveCommandを実行する
    - StudentId: "123e4567-e89b-12d3-a456-426614174000"
  Then InvalidOperationException がスローされる
  And エラーメッセージに "Student is not on leave" が含まれる
```

**制約:**

- 復学日: 必須
- 備考: オプション
- 復学できるのは休学中の学生のみ

**実装状態:** ⬜ 未実装

---

### ⬜ US-S08: 卒業を記録できる

**ストーリー:**
API利用者として、学生の卒業を記録できるようにしたい。なぜなら、学生が全ての課程を修了して卒業したことを正式に記録する必要があるから。

**Handler:** `RecordGraduationCommandHandler : IRequestHandler<RecordGraduationCommand, Guid>`

**受け入れ条件:**

```gherkin
Scenario: 在学中の学生の卒業を記録する
  Given データベースに学生ID "123e4567-e89b-12d3-a456-426614174000" の学生が登録されている
  And その学生が "Enrolled" ステータスである
  And その学生の学年が 4 である
  When RecordGraduationCommandを実行する
    - StudentId: "123e4567-e89b-12d3-a456-426614174000"
    - GraduationDate: "2025-03-31"
    - DegreeType: "Bachelor"
    - Note: "令和6年度卒業"
  Then 自動生成されたステータス履歴ID（Guid）が返される
  And データベースにステータス履歴が保存されている
  And ステータスが "Graduated"（卒業）である
  And 卒業日が "2025-03-31" である
  And 学位種別が "Bachelor" である
```

```gherkin
Scenario: 在学中でない学生の卒業を記録しようとする
  Given データベースに学生ID "123e4567-e89b-12d3-a456-426614174000" の学生が登録されている
  And その学生が "OnLeave" ステータスである
  When RecordGraduationCommandを実行する
    - StudentId: "123e4567-e89b-12d3-a456-426614174000"
  Then InvalidOperationException がスローされる
  And エラーメッセージに "Student must be enrolled" が含まれる
```

```gherkin
Scenario: 既に卒業済みの学生に卒業を記録しようとする
  Given データベースに学生ID "123e4567-e89b-12d3-a456-426614174000" の学生が登録されている
  And その学生が既に "Graduated" ステータスである
  When RecordGraduationCommandを実行する
    - StudentId: "123e4567-e89b-12d3-a456-426614174000"
  Then InvalidOperationException がスローされる
  And エラーメッセージに "Student has already graduated" が含まれる
```

**制約:**

- 卒業日: 必須
- 学位種別: 必須（Bachelor, Master, Doctorなど）
- 備考: オプション
- 卒業できるのは在学中の学生のみ
- 一度卒業した学生は再度卒業できない

**実装状態:** ⬜ 未実装

---

### ⬜ US-S09: 退学を記録できる

**ストーリー:**
API利用者として、学生の退学を記録できるようにしたい。なぜなら、自主退学や除籍など、卒業以外の理由で在籍を終了する学生を適切に記録する必要があるから。

**Handler:** `RecordWithdrawalCommandHandler : IRequestHandler<RecordWithdrawalCommand, Guid>`

**受け入れ条件:**

```gherkin
Scenario: 在学中の学生の退学を記録する
  Given データベースに学生ID "123e4567-e89b-12d3-a456-426614174000" の学生が登録されている
  And その学生が "Enrolled" ステータスである
  When RecordWithdrawalCommandを実行する
    - StudentId: "123e4567-e89b-12d3-a456-426614174000"
    - WithdrawalDate: "2024-12-31"
    - Reason: "自主退学"
    - Note: "進路変更のため"
  Then 自動生成されたステータス履歴ID（Guid）が返される
  And データベースにステータス履歴が保存されている
  And ステータスが "Withdrawn"（退学）である
  And 退学日が "2024-12-31" である
  And 退学理由が "自主退学" である
```

```gherkin
Scenario: 休学中の学生の退学を記録する
  Given データベースに学生ID "123e4567-e89b-12d3-a456-426614174000" の学生が登録されている
  And その学生が "OnLeave" ステータスである
  When RecordWithdrawalCommandを実行する
    - StudentId: "123e4567-e89b-12d3-a456-426614174000"
    - WithdrawalDate: "2024-12-31"
    - Reason: "除籍"
  Then 自動生成されたステータス履歴ID（Guid）が返される
  And ステータスが "Withdrawn"（退学）である
```

```gherkin
Scenario: 既に卒業済みの学生の退学を記録しようとする
  Given データベースに学生ID "123e4567-e89b-12d3-a456-426614174000" の学生が登録されている
  And その学生が "Graduated" ステータスである
  When RecordWithdrawalCommandを実行する
    - StudentId: "123e4567-e89b-12d3-a456-426614174000"
  Then InvalidOperationException がスローされる
  And エラーメッセージに "Cannot withdraw a graduated student" が含まれる
```

**制約:**

- 退学日: 必須
- 退学理由: 必須（自主退学、除籍、懲戒退学など）
- 備考: オプション
- 退学できるのは在学中または休学中の学生のみ
- 卒業済みの学生は退学できない

**実装状態:** ⬜ 未実装

---

### ⬜ US-S10: 学生のステータス履歴を取得できる

**ストーリー:**
API利用者として、学生のステータス履歴の一覧を取得できるようにしたい。なぜなら、学生の入学から現在までの在籍状況の変遷を時系列で確認する必要があるから。

**Handler:** `GetStudentStatusHistoryQueryHandler : IRequestHandler<GetStudentStatusHistoryQuery, List<StudentStatusHistoryDto>>`

**受け入れ条件:**

```gherkin
Scenario: 学生のステータス履歴を時系列で取得する
  Given データベースに学生ID "123e4567-e89b-12d3-a456-426614174000" の学生が登録されている
  And その学生に以下のステータス履歴が存在する
    | ステータス | 日付 | 備考 |
    | Enrolled | 2024-04-01 | 入学 |
    | OnLeave | 2024-10-01 | 病気療養 |
    | Enrolled | 2025-04-01 | 復学 |
  When GetStudentStatusHistoryQueryを実行する
    - StudentId: "123e4567-e89b-12d3-a456-426614174000"
  Then 3件のStudentStatusHistoryDtoが返される
  And 履歴が日付の昇順でソートされている
  And 最初の履歴が "Enrolled" である
  And 最後の履歴が "Enrolled" である
```

```gherkin
Scenario: ステータス履歴がない学生の履歴を取得する
  Given データベースに学生ID "123e4567-e89b-12d3-a456-426614174000" の学生が登録されている
  And その学生にステータス履歴が存在しない
  When GetStudentStatusHistoryQueryを実行する
    - StudentId: "123e4567-e89b-12d3-a456-426614174000"
  Then 空のリスト（0件）が返される
```

```gherkin
Scenario: 存在しない学生IDの履歴を取得しようとする
  Given データベースに学生ID "99999999-9999-9999-9999-999999999999" の学生が登録されていない
  When GetStudentStatusHistoryQueryを実行する
    - StudentId: "99999999-9999-9999-9999-999999999999"
  Then KeyNotFoundException がスローされる
```

**制約:**

- デフォルトソート: 日付の昇順（古い順）
- 全ての履歴を取得（ページネーション未実装）

**実装状態:** ⬜ 未実装

---

### ⬜ US-S11: 学生の現在のステータスを取得できる

**ストーリー:**
API利用者として、学生の現在のステータスを取得できるようにしたい。なぜなら、学生が現在在学中か休学中かなどの状態を素早く確認する必要があるから。

**Handler:** `GetCurrentStudentStatusQueryHandler : IRequestHandler<GetCurrentStudentStatusQuery, StudentStatusDto>`

**受け入れ条件:**

```gherkin
Scenario: 学生の現在のステータスを取得する
  Given データベースに学生ID "123e4567-e89b-12d3-a456-426614174000" の学生が登録されている
  And その学生に以下のステータス履歴が存在する
    | ステータス | 日付 |
    | Enrolled | 2024-04-01 |
    | OnLeave | 2024-10-01 |
    | Enrolled | 2025-04-01 |
  When GetCurrentStudentStatusQueryを実行する
    - StudentId: "123e4567-e89b-12d3-a456-426614174000"
  Then StudentStatusDtoが返される
  And ステータスが "Enrolled" である
  And 日付が "2025-04-01" である
```

```gherkin
Scenario: ステータス履歴がない学生の現在のステータスを取得する
  Given データベースに学生ID "123e4567-e89b-12d3-a456-426614174000" の学生が登録されている
  And その学生にステータス履歴が存在しない
  When GetCurrentStudentStatusQueryを実行する
    - StudentId: "123e4567-e89b-12d3-a456-426614174000"
  Then null が返される
```

```gherkin
Scenario: 存在しない学生IDの現在のステータスを取得しようとする
  Given データベースに学生ID "99999999-9999-9999-9999-999999999999" の学生が登録されていない
  When GetCurrentStudentStatusQueryを実行する
    - StudentId: "99999999-9999-9999-9999-999999999999"
  Then KeyNotFoundException がスローされる
```

**制約:**

- 現在のステータスは履歴の中で最も新しい日付のステータス
- ステータス履歴がない場合はnullを返す

**実装状態:** ⬜ 未実装

---

## エピック3: 学年管理

### ⬜ US-S12: 学年変更を記録できる

**ストーリー:**
API利用者として、学生の学年変更（進級・留年）を記録できるようにしたい。なぜなら、学生の進級履歴を追跡し、留年の有無や時期を把握する必要があるから。また、学年の変更履歴を記録することで、学生の学業進捗を正確に管理できる。

**Handler:** `RecordGradeChangeCommandHandler : IRequestHandler<RecordGradeChangeCommand, Guid>`

**受け入れ条件:**

```gherkin
Scenario: 在学中の学生の進級を記録する
  Given データベースに学生ID "123e4567-e89b-12d3-a456-426614174000" の学生が登録されている
  And その学生が "Enrolled" ステータスである
  And その学生の現在の学年が 1 である
  When RecordGradeChangeCommandを実行する
    - StudentId: "123e4567-e89b-12d3-a456-426614174000"
    - NewGrade: 2
    - ChangeDate: "2025-04-01"
    - ChangeType: "Promotion"
    - AcademicYear: "2025"
  Then 自動生成された学年変更履歴ID（Guid）が返される
  And 学生の学年が 2 に更新される
  And データベースに学年変更履歴が保存されている
  And 変更タイプが "Promotion"（進級）である
  And 変更日が "2025-04-01" である
```

```gherkin
Scenario: 在学中の学生の留年を記録する
  Given データベースに学生ID "123e4567-e89b-12d3-a456-426614174000" の学生が登録されている
  And その学生が "Enrolled" ステータスである
  And その学生の現在の学年が 2 である
  When RecordGradeChangeCommandを実行する
    - StudentId: "123e4567-e89b-12d3-a456-426614174000"
    - NewGrade: 2
    - ChangeDate: "2025-04-01"
    - ChangeType: "Retention"
    - Reason: "必要単位数未取得"
  Then 自動生成された学年変更履歴ID（Guid）が返される
  And 学生の学年は 2 のまま（変更なし）
  And データベースに学年変更履歴が保存されている
  And 変更タイプが "Retention"（留年）である
  And 留年理由が "必要単位数未取得" である
```

```gherkin
Scenario: 在学中でない学生の学年変更を記録しようとする
  Given データベースに学生ID "123e4567-e89b-12d3-a456-426614174000" の学生が登録されている
  And その学生が "OnLeave" ステータスである
  When RecordGradeChangeCommandを実行する
    - StudentId: "123e4567-e89b-12d3-a456-426614174000"
  Then InvalidOperationException がスローされる
  And エラーメッセージに "Student must be enrolled" が含まれる
```

```gherkin
Scenario: 不正な学年変更を記録しようとする（2学年以上の飛び級）
  Given データベースに学生ID "123e4567-e89b-12d3-a456-426614174000" の学生が登録されている
  And その学生の現在の学年が 1 である
  When RecordGradeChangeCommandを実行する
    - StudentId: "123e4567-e89b-12d3-a456-426614174000"
    - NewGrade: 3
    - ChangeType: "Promotion"
  Then InvalidOperationException がスローされる
  And エラーメッセージに "Invalid grade change" が含まれる
```

```gherkin
Scenario: 降級を記録しようとする
  Given データベースに学生ID "123e4567-e89b-12d3-a456-426614174000" の学生が登録されている
  And その学生の現在の学年が 3 である
  When RecordGradeChangeCommandを実行する
    - StudentId: "123e4567-e89b-12d3-a456-426614174000"
    - NewGrade: 2
    - ChangeType: "Promotion"
  Then InvalidOperationException がスローされる
  And エラーメッセージに "Grade cannot be decreased" が含まれる
```

**制約:**

- 変更日: 必須（通常は学年度開始日の4月1日）
- 学年変更タイプ: Promotion（進級）、Retention（留年）
- 学年度: 必須（例: "2025"）
- 留年理由: 留年の場合は推奨（記録として残す）
- 学年変更できるのは在学中（Enrolled）の学生のみ
- 進級は1学年ずつのみ（飛び級不可）
- 降級は不可
- 留年の場合は現在の学年と同じ学年を指定

**実装状態:** ⬜ 未実装

---

### ⬜ US-S13: 学生の学年変更履歴を取得できる

**ストーリー:**
API利用者として、学生の学年変更履歴を取得できるようにしたい。なぜなら、学生の進級・留年の履歴を確認し、学業進捗状況を把握する必要があるから。

**Handler:** `GetGradeChangeHistoryQueryHandler : IRequestHandler<GetGradeChangeHistoryQuery, List<GradeChangeHistoryDto>>`

**受け入れ条件:**

```gherkin
Scenario: 学生の学年変更履歴を時系列で取得する
  Given データベースに学生ID "123e4567-e89b-12d3-a456-426614174000" の学生が登録されている
  And その学生に以下の学年変更履歴が存在する
    | 変更前学年 | 変更後学年 | 変更タイプ | 変更日 |
    | 1 | 2 | Promotion | 2025-04-01 |
    | 2 | 3 | Promotion | 2026-04-01 |
    | 3 | 3 | Retention | 2027-04-01 |
    | 3 | 4 | Promotion | 2028-04-01 |
  When GetGradeChangeHistoryQueryを実行する
    - StudentId: "123e4567-e89b-12d3-a456-426614174000"
  Then 4件のGradeChangeHistoryDtoが返される
  And 履歴が変更日の昇順でソートされている
  And 3番目の履歴が "Retention"（留年）である
```

```gherkin
Scenario: 学年変更履歴がない学生の履歴を取得する
  Given データベースに学生ID "123e4567-e89b-12d3-a456-426614174000" の学生が登録されている
  And その学生に学年変更履歴が存在しない
  When GetGradeChangeHistoryQueryを実行する
    - StudentId: "123e4567-e89b-12d3-a456-426614174000"
  Then 空のリスト（0件）が返される
```

```gherkin
Scenario: 存在しない学生IDの学年変更履歴を取得しようとする
  Given データベースに学生ID "99999999-9999-9999-9999-999999999999" の学生が登録されていない
  When GetGradeChangeHistoryQueryを実行する
    - StudentId: "99999999-9999-9999-9999-999999999999"
  Then KeyNotFoundException がスローされる
```

**制約:**

- デフォルトソート: 変更日の昇順（古い順）
- 全ての履歴を取得（ページネーション未実装）

**実装状態:** ⬜ 未実装

---

### ⬜ US-S05: 学生の入学イベントを記録できる

**ストーリー:**
API利用者として、学生の入学イベントを記録できるようにしたい。なぜなら、学生の在籍状況の履歴を追跡する必要があるから。

**Handler:** `RecordEnrollmentEventCommandHandler : IRequestHandler<RecordEnrollmentEventCommand, Guid>`

**受け入れ条件:**

```gherkin
Scenario: 学生の入学を記録する
  Given データベースに学生ID "123e4567-e89b-12d3-a456-426614174000" の学生が登録されている
  When RecordEnrollmentEventCommandを実行する
    - StudentId: "123e4567-e89b-12d3-a456-426614174000"
    - EventType: "Enrollment" (入学)
    - EventDate: "2024-04-01"
    - Grade: 1
    - Remarks: "令和6年度入学"
  Then 自動生成されたイベントID（Guid）が返される
  And データベースにイベントが保存されている
  And イベントタイプが "Enrollment" である
  And 学年が 1 である
```

```gherkin
Scenario: 存在しない学生IDで入学イベントを記録しようとする
  Given データベースに学生ID "99999999-9999-9999-9999-999999999999" の学生が登録されていない
  When RecordEnrollmentEventCommandを実行する
    - StudentId: "99999999-9999-9999-9999-999999999999"
    - EventType: "Enrollment"
  Then KeyNotFoundException がスローされる
  And エラーメッセージに "Student not found" が含まれる
```

**制約:**

- イベントID: UUID形式で自動生成
- 学生ID: 必須、存在する学生のみ
- イベント日時: 必須
- イベントタイプ: Enrollment, Promotion, Leave, Reinstatement, Withdrawal, Graduation のいずれか
- 備考: オプション

**実装状態:** ⬜ 未実装

---

### ⬜ US-S06: 学生の進級イベントを記録できる

**ストーリー:**
API利用者として、学生の進級イベントを記録できるようにしたい。なぜなら、学年の変更履歴を追跡する必要があるから。

**Handler:** `RecordPromotionEventCommandHandler : IRequestHandler<RecordPromotionEventCommand, Guid>`

**受け入れ条件:**

```gherkin
Scenario: 学生の進級を記録する
  Given データベースに学生ID "123e4567-e89b-12d3-a456-426614174000" の学生が登録されている
  And 現在の学年が 1 である
  When RecordPromotionEventCommandを実行する
    - StudentId: "123e4567-e89b-12d3-a456-426614174000"
    - EventType: "Promotion" (進級)
    - EventDate: "2025-04-01"
    - Grade: 2
    - Remarks: "2年次進級"
  Then 自動生成されたイベントID（Guid）が返される
  And データベースにイベントが保存されている
  And イベントタイプが "Promotion" である
  And 進級後の学年が 2 である
```

**制約:**

- 進級は1学年ずつのみ（1→2, 2→3, 3→4）
- 学年は1〜4の範囲内

**実装状態:** ⬜ 未実装

---

### ⬜ US-S07: 学生の休学イベントを記録できる

**ストーリー:**
API利用者として、学生の休学イベントを記録できるようにしたい。なぜなら、休学期間を追跡し、在籍状況を管理する必要があるから。

**Handler:** `RecordLeaveEventCommandHandler : IRequestHandler<RecordLeaveEventCommand, Guid>`

**受け入れ条件:**

```gherkin
Scenario: 学生の休学を記録する
  Given データベースに学生ID "123e4567-e89b-12d3-a456-426614174000" の学生が登録されている
  When RecordLeaveEventCommandを実行する
    - StudentId: "123e4567-e89b-12d3-a456-426614174000"
    - EventType: "Leave" (休学)
    - EventDate: "2024-10-01"
    - Remarks: "病気療養のため休学"
  Then 自動生成されたイベントID（Guid）が返される
  And データベースにイベントが保存されている
  And イベントタイプが "Leave" である
```

**制約:**

- 休学理由は備考に記録
- 休学期間の開始日を記録

**実装状態:** ⬜ 未実装

---

### ⬜ US-S08: 学生の復学イベントを記録できる

**ストーリー:**
API利用者として、学生の復学イベントを記録できるようにしたい。なぜなら、休学からの復帰を追跡し、在籍状況を管理する必要があるから。

**Handler:** `RecordReinstatementEventCommandHandler : IRequestHandler<RecordReinstatementEventCommand, Guid>`

**受け入れ条件:**

```gherkin
Scenario: 学生の復学を記録する
  Given データベースに学生ID "123e4567-e89b-12d3-a456-426614174000" の学生が登録されている
  And 過去に休学イベントが記録されている
  When RecordReinstatementEventCommandを実行する
    - StudentId: "123e4567-e89b-12d3-a456-426614174000"
    - EventType: "Reinstatement" (復学)
    - EventDate: "2025-04-01"
    - Remarks: "復学"
  Then 自動生成されたイベントID（Guid）が返される
  And データベースにイベントが保存されている
  And イベントタイプが "Reinstatement" である
```

**制約:**

- 復学前に休学イベントが存在すること（ビジネスルールとして推奨）

**実装状態:** ⬜ 未実装

---

### ⬜ US-S09: 学生の退学イベントを記録できる

**ストーリー:**
API利用者として、学生の退学イベントを記録できるようにしたい。なぜなら、退学による在籍終了を記録し、履歴を保持する必要があるから。

**Handler:** `RecordWithdrawalEventCommandHandler : IRequestHandler<RecordWithdrawalEventCommand, Guid>`

**受け入れ条件:**

```gherkin
Scenario: 学生の退学を記録する
  Given データベースに学生ID "123e4567-e89b-12d3-a456-426614174000" の学生が登録されている
  When RecordWithdrawalEventCommandを実行する
    - StudentId: "123e4567-e89b-12d3-a456-426614174000"
    - EventType: "Withdrawal" (退学)
    - EventDate: "2024-12-31"
    - Remarks: "自己都合退学"
  Then 自動生成されたイベントID（Guid）が返される
  And データベースにイベントが保存されている
  And イベントタイプが "Withdrawal" である
```

**制約:**

- 退学理由は備考に記録
- 退学後も学生データは保持（論理削除ではなくイベントで管理）

**実装状態:** ⬜ 未実装

---

### ⬜ US-S10: 学生の卒業イベントを記録できる

**ストーリー:**
API利用者として、学生の卒業イベントを記録できるようにしたい。なぜなら、卒業による在籍終了を記録し、履歴を保持する必要があるから。

**Handler:** `RecordGraduationEventCommandHandler : IRequestHandler<RecordGraduationEventCommand, Guid>`

**受け入れ条件:**

```gherkin
Scenario: 学生の卒業を記録する
  Given データベースに学生ID "123e4567-e89b-12d3-a456-426614174000" の学生が登録されている
  And 学年が 4 である
  When RecordGraduationEventCommandを実行する
    - StudentId: "123e4567-e89b-12d3-a456-426614174000"
    - EventType: "Graduation" (卒業)
    - EventDate: "2028-03-31"
    - Remarks: "令和9年度卒業"
  Then 自動生成されたイベントID（Guid）が返される
  And データベースにイベントが保存されている
  And イベントタイプが "Graduation" である
```

**制約:**

- 通常は4年生で卒業
- 卒業後も学生データは保持（論理削除ではなくイベントで管理）

**実装状態:** ⬜ 未実装

---

### ⬜ US-S11: 学生のイベント履歴を取得できる

**ストーリー:**
API利用者として、学生のイベント履歴を時系列で取得できるようにしたい。なぜなら、学生の在籍状況の変遷を確認する必要があるから。

**Handler:** `GetStudentEventsQueryHandler : IRequestHandler<GetStudentEventsQuery, List<StudentEventDto>>`

**受け入れ条件:**

```gherkin
Scenario: 学生のイベント履歴を取得する
  Given データベースに学生ID "123e4567-e89b-12d3-a456-426614174000" の学生が登録されている
  And 以下のイベントが記録されている
    | イベントタイプ | イベント日時 | 学年 |
    | Enrollment | 2024-04-01 | 1 |
    | Promotion | 2025-04-01 | 2 |
    | Leave | 2025-10-01 | - |
  When GetStudentEventsQueryを実行する
    - StudentId: "123e4567-e89b-12d3-a456-426614174000"
  Then 3件のStudentEventDtoが返される
  And イベントが日時の昇順でソートされている
  And 最初のイベントが "Enrollment" である
  And 最後のイベントが "Leave" である
```

```gherkin
Scenario: イベントが記録されていない学生の履歴を取得する
  Given データベースに学生ID "123e4567-e89b-12d3-a456-426614174000" の学生が登録されている
  And イベントが記録されていない
  When GetStudentEventsQueryを実行する
    - StudentId: "123e4567-e89b-12d3-a456-426614174000"
  Then 空のリスト（0件）が返される
```

```gherkin
Scenario: イベントタイプでフィルタリングして取得する
  Given データベースに学生ID "123e4567-e89b-12d3-a456-426614174000" の複数のイベントが記録されている
  When GetStudentEventsQueryを実行する
    - StudentId: "123e4567-e89b-12d3-a456-426614174000"
    - EventType: "Promotion"
  Then "Promotion" タイプのイベントのみが返される
```

**制約:**

- デフォルトソート: イベント日時の昇順
- イベントタイプでフィルタリング可能（オプション）
- ページネーション未実装

**実装状態:** ⬜ 未実装

---

## ドメインルール・制約まとめ

### 学生（Student）

- **学生ID**: UUID（自動生成）
- **メールアドレス**: 一意制約、メール形式
- **学年**: 1〜4

### 学生ステータス履歴（StudentStatusHistory）

- **履歴ID**: UUID（自動生成）
- **学生ID**: 必須（外部キー）
- **ステータス**: Enrolled（在学中）、OnLeave（休学中）、Graduated（卒業）、Withdrawn（退学）
- **日付**: 必須（そのステータスになった日）
- **備考**: オプション

### 学年変更履歴（GradeChangeHistory）

- **履歴ID**: UUID（自動生成）
- **学生ID**: 必須（外部キー）
- **変更前学年**: 必須（1〜4）
- **変更後学年**: 必須（1〜4）
- **変更タイプ**: Promotion（進級）、Retention（留年）
- **変更日**: 必須（通常は4月1日）
- **学年度**: 必須（例: "2025"）
- **理由**: オプション（留年の場合は推奨）

#### ステータス遷移ルール

- **入学（Enrolled）**:
  - ステータス履歴がない学生のみ可能
  - 既に在学中の学生は入学できない

- **休学（OnLeave）**:
  - 在学中（Enrolled）の学生のみ可能
  - 休学中の学生は二重に休学できない
  - 予定終了日はオプション

- **復学（Enrolled）**:
  - 休学中（OnLeave）の学生のみ可能

- **卒業（Graduated）**:
  - 在学中（Enrolled）の学生のみ可能
  - 一度卒業した学生は再度卒業できない

- **退学（Withdrawn）**:
  - 在学中（Enrolled）または休学中（OnLeave）の学生のみ可能
  - 卒業済み（Graduated）の学生は退学できない

#### 学年変更ルール

- **進級（Promotion）**:
  - 在学中（Enrolled）の学生のみ可能
  - 1学年ずつのみ進級可能（飛び級不可）
  - 例: 1年→2年、2年→3年、3年→4年

- **留年（Retention）**:
  - 在学中（Enrolled）の学生のみ可能
  - 現在の学年と同じ学年を指定
  - 留年理由の記録を推奨
  - 例: 2年→2年（留年）

- **共通ルール**:
  - 降級は不可（3年→2年などは禁止）
  - 休学中・卒業・退学済みの学生は学年変更不可
  - 変更日は必須（通常は学年度開始日の4月1日）

---

## 実装優先順位

### Phase 1: 学生在籍管理（完了済み）

- ✅ US-S01: 学生登録（新入生の在籍情報登録）
- ✅ US-S02: 学生情報更新（在籍情報の変更）
- ✅ US-S03: 学生一覧取得（在籍学生の検索・閲覧）
- ✅ US-S04: 学生取得（個別学生の在籍情報取得）

**理由**: 最もシンプルで他機能への依存なし。学生の在籍情報管理が全ての履修管理の前提。

### Phase 2: 在籍ステータス履歴管理（予定）

- ⬜ US-S05: 入学記録（学生の入学を記録）
- ⬜ US-S06: 休学記録（学生の休学を記録）
- ⬜ US-S07: 復学記録（休学からの復学を記録）
- ⬜ US-S08: 卒業記録（学生の卒業を記録）
- ⬜ US-S09: 退学記録（学生の退学を記録）
- ⬜ US-S10: ステータス履歴取得（学生のステータス変遷を時系列で取得）
- ⬜ US-S11: 現在のステータス取得（学生の現在の在籍状態を取得）

**理由**: 学生の在籍状態の履歴を管理することで、入学から卒業/退学までのライフサイクルを追跡可能にする。Phase 1の学生基本情報管理の上に構築される。

**実装順序の推奨**:

1. まず入学記録（US-S05）を実装
2. 次に現在のステータス取得（US-S11）とステータス履歴取得（US-S10）を実装
3. その後、休学・復学・卒業・退学の各記録機能を実装

### Phase 3: 学年管理（予定）

- ⬜ US-S12: 学年変更記録（進級・留年を記録）
- ⬜ US-S13: 学年変更履歴取得（学生の進級・留年履歴を取得）
- 🔄 US-S01: 学生登録機能の拡張（入学日・学年度パラメータ追加、入学記録自動作成）
- 🔄 US-S02: 学生情報更新機能の変更（学年更新機能を廃止、名前とメールアドレスのみ更新可能に）

**理由**: 学年変更の履歴を記録することで、進級・留年の状況を正確に追跡し、学生の学業進捗を管理できる。Phase 2の在籍ステータス管理と連携して、より詳細な学生ライフサイクル管理を実現する。

**実装順序の推奨**:

1. まず学年変更記録（US-S12）を実装
2. 次に学年変更履歴取得（US-S13）を実装
3. US-S01の拡張（入学記録の自動作成）
4. US-S02の学年更新機能を削除（破壊的変更のため慎重に）

**注意事項**:

- US-S02の変更は破壊的変更のため、既存のAPI利用者への影響を考慮すること
- US-S01の拡張は後方互換性を保つこと（EnrollmentDateとAcademicYearをオプションパラメータとして追加）

### Phase 2: 学生イベント履歴管理（未実装）

- ⬜ US-S05: 入学イベント記録
- ⬜ US-S06: 進級イベント記録
- ⬜ US-S07: 休学イベント記録
- ⬜ US-S08: 復学イベント記録
- ⬜ US-S09: 退学イベント記録
- ⬜ US-S10: 卒業イベント記録
- ⬜ US-S11: 学生イベント履歴取得

**理由**: 学生の在籍状況の変遷を追跡するために必要。Student集約に紐づく履歴管理。

---

## テスト実装戦略

詳細は [testing-strategy.md](impl-patterns/testing-strategy.md) を参照してください。
