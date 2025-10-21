# 成績評価コンテキスト - ユーザーストーリー

## コンテキスト概要

**Grading Bounded Context（成績評価境界づけられたコンテキスト）**

成績評価コンテキストは、学生の成績評価・成績管理・成績証明書発行を担当する境界づけられたコンテキストです。

**対象範囲:** Applicationレイヤーの統合テスト（CommandHandler, QueryHandler, Repository層を含む）

### 責務
- 成績評価ルールの定義・管理
- 課題・試験の成績記録
- 最終成績の計算・確定
- 成績証明書の発行
- GPA計算

### 依存コンテキスト
- **Enrollments（履修管理）**: 履修情報、学生情報、コース情報の取得
- **Attendance（出席管理）**: 出席率の取得（成績評価に使用する場合）

### 集約ルート
1. **GradingPolicy（成績評価ルールAggregate）**
   - 成績評価の方法（課題・試験・出席の配分）
   - 合格基準の定義

2. **Assessment（評価項目Aggregate）**
   - 課題・試験・レポート等の評価項目
   - 配点・提出期限の管理

3. **Grade（成績Aggregate）**
   - 学生の成績記録
   - 最終成績の計算結果
   - 成績確定状態の管理

---

## Epoch 1: 基本機能（MVP）

### Phase 1: 成績評価ルール管理

#### US-G01: コースの成績評価ルールを登録する

**ストーリー:**
教員（または管理者）として、コースごとに成績評価ルールを設定できるようにしたい。なぜなら、成績計算の基準を明確化する必要があるから。

**Application Service:** `CreateGradingPolicyCommandHandler`

**受け入れ基準:**

```gherkin
Scenario: 成績評価ルールの正常登録
  Given CourseRepositoryにCourseCode "CS101" が存在する
  And SemesterRepositoryに (2024, Spring) が存在する
  When CreateGradingPolicyCommandを実行する
    - CourseCode: "CS101"
    - SemesterId: (2024, Spring)
    - EvaluationItems: [
        {Name: "中間試験", Type: Exam, Weight: 30},
        {Name: "期末試験", Type: Exam, Weight: 40},
        {Name: "課題", Type: Assignment, Weight: 20},
        {Name: "出席", Type: Attendance, Weight: 10}
      ]
    - PassingGrade: 60
    - GradingScale: {A: 90-100, B: 80-89, C: 70-79, D: 60-69, F: 0-59}
  Then GradingPolicyエンティティが作成される
  And GradingPolicyRepositoryに保存される
  And EvaluationItemsのWeightの合計が100である
  And PassingGradeが60である

Scenario: 評価項目の配分が100%でない場合のエラー
  Given CourseとSemesterが存在する
  When CreateGradingPolicyCommandを実行する
    - EvaluationItems: [
        {Name: "中間試験", Weight: 30},
        {Name: "期末試験", Weight: 40}
      ]  # 合計70%
  Then DomainException "Total weight of evaluation items must be 100" がスローされる
  And GradingPolicyRepositoryに保存されない

Scenario: 既存の成績評価ルールがある場合のエラー
  Given GradingPolicyRepositoryにCS101の(2024, Spring)のGradingPolicyが既に存在する
  When 同じCourseCodeとSemesterIdでCreateGradingPolicyCommandを実行する
  Then DomainException "Grading policy already exists for this course and semester" がスローされる
```

**Domain Rules**:
- 評価項目の配分合計は必ず100%でなければならない
- 1コース1学期につき1つの成績評価ルールのみ設定可能
- 成績評価ルールは学期開始前または学期中に設定可能
- 評価項目の種類: Exam（試験）, Assignment（課題）, Attendance（出席）, Other（その他）
- 合格基準は0-100の範囲で設定

---

#### US-G02: コースの成績評価ルールを取得する

**ストーリー:**
学生または教員として、コースの成績評価ルールを確認できるようにしたい。なぜなら、成績の計算方法を理解する必要があるから。

**Application Service:** `GetGradingPolicyQueryHandler`

**受け入れ基準:**

```gherkin
Scenario: 成績評価ルールを取得する
  Given GradingPolicyRepositoryにCS101の(2024, Spring)のGradingPolicyが存在する
  When GetGradingPolicyQueryを実行する
    - CourseCode: "CS101"
    - SemesterId: (2024, Spring)
  Then GradingPolicyDtoが返される
  And EvaluationItemsが4件含まれる
  And PassingGradeが60である

Scenario: 存在しない成績評価ルールを取得しようとする
  Given GradingPolicyRepositoryにCS999のGradingPolicyが存在しない
  When CourseCode "CS999" でGetGradingPolicyQueryを実行する
  Then NotFoundException "Grading policy not found" がスローされる
```

---

### Phase 2: 評価項目の成績記録

#### US-G03: 評価項目（課題・試験）を作成する

**ストーリー:**
教員として、課題や試験などの評価項目を作成できるようにしたい。なぜなら、学生の成績を記録する必要があるから。

**Application Service:** `CreateAssessmentCommandHandler`

**受け入れ基準:**

```gherkin
Scenario: 評価項目の正常作成
  Given CourseRepositoryにCS101が存在する
  And SemesterRepositoryに(2024, Spring)が存在する
  When CreateAssessmentCommandを実行する
    - CourseCode: "CS101"
    - SemesterId: (2024, Spring)
    - Name: "中間試験"
    - Type: Exam
    - MaxScore: 100
    - DueDate: 2024-05-15T23:59:59Z
  Then Assessmentエンティティが作成される
  And AssessmentRepositoryに保存される
  And Statusが"Open"である

Scenario: 最大スコアが0以下の場合のエラー
  Given CourseとSemesterが存在する
  When CreateAssessmentCommandを実行する
    - MaxScore: -10
  Then DomainException "Max score must be greater than 0" がスローされる
```

**Domain Rules**:
- 最大スコアは0より大きい値でなければならない
- 評価項目の種類: Exam（試験）, Assignment（課題）, Quiz（小テスト）, Report（レポート）
- ステータス: Open（受付中）, Closed（締切）, Graded（採点済み）

---

#### US-G04: 学生の評価項目スコアを記録する

**ストーリー:**
教員として、学生の課題・試験のスコアを記録できるようにしたい。なぜなら、成績計算の基礎データを蓄積する必要があるから。

**Application Service:** `RecordAssessmentScoreCommandHandler`

**受け入れ基準:**

```gherkin
Scenario: スコアの正常記録
  Given EnrollmentRepositoryにStudentId "student-001" のCS101へのEnrollmentが存在する
  And AssessmentRepositoryにAssessmentId "assessment-001" が存在する（MaxScore: 100）
  When RecordAssessmentScoreCommandを実行する
    - AssessmentId: "assessment-001"
    - StudentId: "student-001"
    - Score: 85
  Then AssessmentScoreエンティティが作成される
  And AssessmentScoreRepositoryに保存される
  And Scoreが85である
  And Percentageが85.0である

Scenario: スコアが最大スコアを超える場合のエラー
  Given AssessmentのMaxScoreが100である
  When RecordAssessmentScoreCommandを実行する
    - Score: 120
  Then DomainException "Score cannot exceed max score" がスローされる

Scenario: 履修登録していない学生へのスコア記録エラー
  Given EnrollmentRepositoryにStudentのEnrollmentが存在しない
  When RecordAssessmentScoreCommandを実行する
  Then DomainException "Student not enrolled in this course" がスローされる
```

**Domain Rules**:
- スコアは0から最大スコアの範囲内でなければならない
- 学生は対象コースに履修登録している必要がある
- 同じ学生・評価項目の組み合わせで複数回記録可能（上書き）
- スコア記録時に自動的にパーセンテージを計算

---

#### US-G05: 学生の評価項目スコア一覧を取得する

**ストーリー:**
学生または教員として、学生の評価項目ごとのスコアを確認できるようにしたい。なぜなら、現在の成績状況を把握する必要があるから。

**Application Service:** `GetAssessmentScoresQueryHandler`

**受け入れ基準:**

```gherkin
Scenario: 学生の評価項目スコア一覧を取得する
  Given AssessmentScoreRepositoryにStudentId "student-001" のCS101のスコアが2件存在する
  When GetAssessmentScoresQueryを実行する
    - StudentId: "student-001"
    - CourseCode: "CS101"
    - SemesterId: (2024, Spring)
  Then 2件のAssessmentScoreDtoが返される
```

---

### Phase 3: 最終成績の計算・確定

#### US-G06: 学生の最終成績を計算する

**ストーリー:**
教員として、学生の最終成績を自動計算できるようにしたい。なぜなら、成績評価ルールに基づいた正確な成績を算出する必要があるから。

**Application Service:** `CalculateGradeCommandHandler`

**受け入れ基準:**

```gherkin
Scenario: 最終成績の正常計算
  Given EnrollmentRepositoryにStudentId "student-001" のCS101へのEnrollmentが存在する
  And GradingPolicyRepositoryにCS101の(2024, Spring)のGradingPolicyが存在する
  And 全てのEvaluationItemsに対応するAssessmentScoreが記録されている
    | Name     | Score | Weight |
    | 中間試験 | 85    | 30     |
    | 期末試験 | 78    | 40     |
    | 課題     | 90    | 20     |
    | 出席     | 95    | 10     |
  When CalculateGradeCommandを実行する
    - CourseCode: "CS101"
    - SemesterId: (2024, Spring)
    - StudentId: "student-001"
  Then Gradeエンティティが作成される
  And GradeRepositoryに保存される
  And TotalScoreが84.2である
  And LetterGradeが"B"である
  And IsPassingがtrueである
  And Statusが"Calculated"である

Scenario: 評価項目のスコアが不足している場合のエラー
  Given GradingPolicyに4つのEvaluationItemsがあるが、AssessmentScoreが3件しかない
  When CalculateGradeCommandを実行する
  Then DomainException "All assessment scores must be recorded" がスローされる

Scenario: 成績評価ルールが設定されていない場合のエラー
  Given GradingPolicyRepositoryにCS101のGradingPolicyが存在しない
  When CalculateGradeCommandを実行する
  Then NotFoundException "Grading policy not found" がスローされる
```

**Domain Rules**:
- 全ての評価項目のスコアが記録されている必要がある
- 総合点 = Σ(各評価項目のスコア × 配分%)
- レターグレードは成績評価ルールの基準に基づいて決定
- 合格判定は総合点が合格基準以上である場合にtrue
- ステータス: Calculated（計算済み）, Finalized（確定）

---

#### US-G07: 学生の最終成績を確定する

**ストーリー:**
教員として、計算済みの最終成績を確定できるようにしたい。なぜなら、成績を正式に記録し、変更不可にする必要があるから。

**Application Service:** `FinalizeGradeCommandHandler`

**受け入れ基準:**

```gherkin
Scenario: 成績の正常確定
  Given GradeRepositoryにGradeId "grade-001" のGradeが存在する
  And Gradeのステータスが"Calculated"である
  When FinalizeGradeCommandを実行する
    - GradeId: "grade-001"
  Then GradeのStatusが"Finalized"に更新される
  And FinalizedAtが記録される
  And GradeRepositoryに保存される

Scenario: 既に確定済みの成績を再確定しようとするエラー
  Given GradeのStatusが"Finalized"である
  When FinalizeGradeCommandを実行する
  Then DomainException "Grade already finalized" がスローされる

Scenario: 計算されていない成績を確定しようとするエラー
  Given GradeRepositoryにGradeが存在しない
  When FinalizeGradeCommandを実行する
  Then NotFoundException "Grade not found" がスローされる
```

**Domain Rules**:
- 確定前のステータスは"Calculated"である必要がある
- 確定後は成績の変更・削除が不可能
- 確定時に確定日時と確定者を記録
- 確定後はステータスが"Finalized"になる

---

#### US-G08: 学生の成績一覧を取得する

**ストーリー:**
学生として、自分の全コースの成績を確認できるようにしたい。なぜなら、学業成績を把握する必要があるから。

**Application Service:** `GetStudentGradesQueryHandler`

**受け入れ基準:**

```gherkin
Scenario: 学生の学期成績一覧を取得する
  Given GradeRepositoryにStudentId "student-001" の(2024, Spring)のGradeが2件存在する
  When GetStudentGradesQueryを実行する
    - StudentId: "student-001"
    - SemesterId: (2024, Spring)
  Then 2件のGradeDtoが返される
  And TotalCoursesが2である
  And TotalCreditsが7である
```

---

## Epoch 2: 拡張機能

### Phase 4: GPA計算・成績証明書

#### US-G09: 学生の累積GPAを計算する

**As a** 学生または教職員
**I want to** 学生の累積GPAを確認できる
**So that** 学業成績の総合評価を把握できる

**API仕様**:
```http
GET /api/gpa?studentId=323e4567-e89b-12d3-a456-426614174000
```

**Response**:
```json
{
  "studentId": "323e4567-e89b-12d3-a456-426614174000",
  "cumulativeGpa": 3.45,
  "totalCreditsEarned": 45,
  "totalCreditsTaken": 48,
  "semesterGpas": [
    {
      "semesterId": {
        "year": 2024,
        "period": "Spring"
      },
      "gpa": 3.5,
      "creditsEarned": 7,
      "creditsTaken": 7
    },
    {
      "semesterId": {
        "year": 2023,
        "period": "Fall"
      },
      "gpa": 3.4,
      "creditsEarned": 14,
      "creditsTaken": 15
    }
  ]
}
```

**Domain Rules**:
- GPA = Σ(レターグレードポイント × 単位数) / Σ単位数
- レターグレードポイント: A=4.0, B=3.0, C=2.0, D=1.0, F=0.0
- 不合格（F）のコースも計算に含む（分母には含むが単位は付与されない）
- 累積GPAは全学期の成績を対象

---

#### US-G10: 成績証明書を発行する

**As a** 学生
**I want to** 成績証明書を発行できる
**So that** 就職活動や進学に使用できる

**API仕様**:
```http
POST /api/transcripts
Content-Type: application/json

{
  "studentId": "323e4567-e89b-12d3-a456-426614174000",
  "includeInProgress": false,
  "language": "ja"
}
```

**Response**:
```json
{
  "transcriptId": "723e4567-e89b-12d3-a456-426614174000",
  "studentId": "323e4567-e89b-12d3-a456-426614174000",
  "studentName": "山田太郎",
  "studentEmail": "yamada@example.com",
  "issuedAt": "2024-07-20T10:00:00Z",
  "cumulativeGpa": 3.45,
  "totalCreditsEarned": 45,
  "semesters": [
    {
      "semesterId": {
        "year": 2024,
        "period": "Spring"
      },
      "courses": [
        {
          "courseCode": "CS101",
          "courseName": "Introduction to Computer Science",
          "credits": 3,
          "letterGrade": "B",
          "status": "Finalized"
        }
      ],
      "semesterGpa": 3.5
    }
  ],
  "pdfUrl": "/api/transcripts/723e4567-e89b-12d3-a456-426614174000/pdf"
}
```

**Domain Rules**:
- 成績証明書には確定済み（Finalized）の成績のみ含む
- 学生情報、履修コース、成績、GPAを含む
- PDF形式でダウンロード可能
- 発行履歴を記録

---

## 実装優先順位

### 高優先度（MVP必須）
1. **US-G01**: 成績評価ルール登録
2. **US-G02**: 成績評価ルール取得
3. **US-G03**: 評価項目作成
4. **US-G04**: 評価項目スコア記録
5. **US-G06**: 最終成績計算
6. **US-G07**: 最終成績確定

### 中優先度
7. **US-G05**: 評価項目スコア一覧取得
8. **US-G08**: 成績一覧取得

### 低優先度（拡張機能）
9. **US-G09**: GPA計算
10. **US-G10**: 成績証明書発行

---

## コンテキスト間の依存関係

### Enrollmentsコンテキストへの依存
- **学生情報**: 成績記録時に学生の履修登録状態を確認
- **コース情報**: 成績評価ルール作成時にコースの存在確認
- **学期情報**: 成績評価ルールは学期単位で管理

### Attendanceコンテキストへの依存（オプション）
- **出席率**: 出席率を成績評価項目として使用する場合に参照

---

## 集約設計

### 1. GradingPolicy Aggregate
**集約ルート**: GradingPolicy
**値オブジェクト**: EvaluationItem, GradingScale, SemesterId
**不変条件**:
- 評価項目の配分合計は必ず100%
- 1コース1学期につき1つの成績評価ルールのみ

### 2. Assessment Aggregate
**集約ルート**: Assessment
**エンティティ**: AssessmentScore
**不変条件**:
- スコアは0から最大スコアの範囲内
- 学生は対象コースに履修登録している必要あり

### 3. Grade Aggregate
**集約ルート**: Grade
**値オブジェクト**: GradeBreakdown, LetterGrade
**不変条件**:
- 確定済み（Finalized）の成績は変更不可
- 成績計算には全評価項目のスコアが必要

---

## ドメインイベント

### 成績評価ルール関連
- `GradingPolicyCreated`: 成績評価ルールが作成された
- `GradingPolicyUpdated`: 成績評価ルールが更新された

### 評価項目関連
- `AssessmentCreated`: 評価項目が作成された
- `AssessmentScoreRecorded`: 評価項目スコアが記録された

### 成績関連
- `GradeCalculated`: 成績が計算された
- `GradeFinalized`: 成績が確定された（→ 通知サービスへ）
- `TranscriptIssued`: 成績証明書が発行された

---

## 技術的な考慮事項

### パフォーマンス
- 成績計算は複数の評価項目を集計するため、適切なインデックスが必要
- GPA計算は全学期の成績を対象とするため、キャッシュ戦略を検討
- 成績証明書PDFの生成は非同期処理を検討

### セキュリティ
- 学生は自分の成績のみ閲覧可能
- 教員は担当コースの学生の成績のみ編集可能
- 成績確定後の変更は特別な権限が必要

### データ整合性
- 成績評価ルールの変更は既存の成績に影響しない（スナップショット方式）
- 履修登録の取り消しは確定済み成績がある場合は不可
- 評価項目の削除は既にスコアが記録されている場合は不可
