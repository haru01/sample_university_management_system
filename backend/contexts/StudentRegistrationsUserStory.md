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

---

TODO: 入学、進級、休学、復学、退学、卒業などの履歴残す。在学中なのか判定できるなにか。

## エピック1: 学生在籍管理

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

## ドメインルール・制約まとめ

### 学生（Student）

- **学生ID**: UUID（自動生成）
- **メールアドレス**: 一意制約、メール形式
- **学年**: 1〜4

---

## 実装優先順位

### Phase 1: 学生在籍管理（完了済み）

- ✅ US-S01: 学生登録（新入生の在籍情報登録）
- ✅ US-S02: 学生情報更新（在籍情報の変更）
- ✅ US-S03: 学生一覧取得（在籍学生の検索・閲覧）
- ✅ US-S04: 学生取得（個別学生の在籍情報取得）

**理由**: 最もシンプルで他機能への依存なし。学生の在籍情報管理が全ての履修管理の前提。

---

## テスト実装戦略

詳細は [testing-strategy.md](impl-patterns/testing-strategy.md) を参照してください。
