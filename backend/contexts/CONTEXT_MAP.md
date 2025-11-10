# コンテキストマップ

このドキュメントは、大学管理システムにおける境界づけられたコンテキスト（Bounded Context）間の関係性を定義します。

## 境界づけられたコンテキスト一覧

### 1. StudentRegistrations（学生在籍管理）

**責務:**
- 学生の基本情報管理（氏名、メールアドレス、学年）
- 学生の存在確認（他コンテキストからの照会用）
- 将来的な拡張: 在籍ステータス管理（在籍中、休学中、退学、卒業）

**集約:**
- `Student` - 学生集約ルート

**公開API:**
- `GET /api/students/{id}` - 学生情報取得
- `POST /api/students` - 学生登録
- `PUT /api/students/{id}` - 学生情報更新
- `GET /api/students` - 学生一覧取得（検索・フィルタリング対応）

**データベーススキーマ:**
- `student_registrations.students`

**プロジェクト構成:**
```
StudentRegistrations/
├── Domain/
│   └── StudentAggregate/
│       ├── Student.cs
│       └── IStudentRepository.cs
├── Application/
│   ├── Commands/
│   │   ├── CreateStudent/
│   │   └── UpdateStudent/
│   └── Queries/
│       ├── GetStudent/
│       └── SelectStudents/
├── Infrastructure/
│   ├── Persistence/
│   │   ├── StudentRegistrationsDbContext.cs
│   │   ├── Configurations/StudentConfiguration.cs
│   │   ├── Repositories/StudentRepository.cs
│   │   └── Migrations/V1__Create_Students.sql
│   └── Services/
└── Api/
    ├── Controllers/StudentsController.cs
    └── Program.cs
```

---

### 2. Enrollments（履修登録管理）

**責務:**
- 履修登録管理（学生のコース登録、キャンセル、完了）
- コース開講管理
- コース管理
- 学期管理

**集約:**
- `Enrollment` - 履修登録集約ルート
- `CourseOffering` - コース開講集約ルート
- `Course` - コース集約ルート
- `Semester` - 学期集約ルート

**公開API:**
- `POST /api/enrollments` - 履修登録
- `PUT /api/enrollments/{id}/cancel` - 履修キャンセル
- `PUT /api/enrollments/{id}/complete` - 履修完了
- `GET /api/enrollments/students/{studentId}` - 学生の履修登録一覧取得
- `POST /api/courses` - コース作成
- `GET /api/courses` - コース一覧取得
- `POST /api/semesters` - 学期作成
- `GET /api/semesters` - 学期一覧取得
- `POST /api/course-offerings` - コース開講作成
- `GET /api/course-offerings` - コース開講一覧取得

**データベーススキーマ:**
- `courses.enrollments`
- `courses.course_offerings`
- `courses.courses`
- `courses.semesters`

**プロジェクト構成:**
```
Enrollments/
├── Domain/
│   ├── EnrollmentAggregate/
│   │   ├── Enrollment.cs
│   │   ├── EnrollmentId.cs
│   │   ├── EnrollmentStatus.cs
│   │   ├── EnrollmentStatusHistory.cs
│   │   └── IEnrollmentRepository.cs
│   ├── CourseOfferingAggregate/
│   │   ├── CourseOffering.cs
│   │   ├── OfferingId.cs
│   │   ├── OfferingStatus.cs
│   │   └── ICourseOfferingRepository.cs
│   ├── CourseAggregate/
│   │   ├── Course.cs
│   │   ├── CourseCode.cs
│   │   └── ICourseRepository.cs
│   └── SemesterAggregate/
│       ├── Semester.cs
│       ├── SemesterId.cs
│       └── ISemesterRepository.cs
├── Application/
│   ├── Commands/
│   │   ├── EnrollStudent/
│   │   ├── CancelEnrollment/
│   │   └── CompleteEnrollment/
│   ├── Queries/
│   │   └── GetStudentEnrollments/
│   └── Services/
│       └── IStudentServiceClient.cs  # ACL Interface
├── Infrastructure/
│   ├── Persistence/
│   │   ├── CoursesDbContext.cs
│   │   ├── Configurations/
│   │   ├── Repositories/
│   │   └── Migrations/
│   │       └── V7__Migrate_Students_To_StudentRegistrations.sql
│   └── Services/
│       └── StudentServiceClient.cs   # ACL Implementation (HTTP)
└── Api/
    ├── Controllers/
    │   ├── EnrollmentsController.cs
    │   ├── CoursesController.cs
    │   ├── SemestersController.cs
    │   └── CourseOfferingsController.cs
    ├── Middleware/GlobalExceptionMiddleware.cs
    └── Program.cs
```

---

## コンテキスト間の関係

### StudentRegistrations → Enrollments

**パターン:** Customer-Supplier

- **Supplier (上流):** StudentRegistrations
- **Customer (下流):** Enrollments
- **統合方式:** ACL（Anti-Corruption Layer） - HTTP通信

**関係性の説明:**

Enrollmentsコンテキストは、学生が履修登録を行う際に、その学生が実際に存在するかを確認する必要があります。しかし、Enrollmentsコンテキストは学生の詳細情報を管理する責務を持ちません。

そのため、StudentRegistrationsコンテキストが提供するREST APIを通じて、必要な学生情報のみを取得します。

**データフロー:**

```
[Enrollments Context]                      [StudentRegistrations Context]

EnrollStudentCommandHandler                StudentRegistrations API
        ↓                                           ↑
  IStudentServiceClient (Interface)                |
        ↓                                           |
  StudentServiceClient (Implementation)            |
        ↓                                           |
    HttpClient -------- HTTP GET /api/students/{id} -
                                                    ↓
                                          StudentsController
                                                    ↓
                                          GetStudentQueryHandler
                                                    ↓
                                          StudentRepository
                                                    ↓
                                          student_registrations.students
```

**メソッド呼び出し例:**

```csharp
// Enrollments側（Customer）
public class EnrollStudentCommandHandler : IRequestHandler<EnrollStudentCommand, Guid>
{
    private readonly IStudentServiceClient _studentServiceClient;

    public async Task<Guid> Handle(EnrollStudentCommand request, CancellationToken cancellationToken)
    {
        // 学生の存在確認（StudentRegistrations APIを呼び出す）
        var studentExists = await _studentServiceClient.ExistsAsync(studentId, cancellationToken);

        if (!studentExists)
        {
            throw new NotFoundException($"学生ID {request.StudentId} が見つかりません");
        }

        // 履修登録処理...
    }
}
```

**ACL実装:**

```csharp
// IStudentServiceClient.cs (Enrollments.Application)
public interface IStudentServiceClient
{
    Task<bool> ExistsAsync(StudentId studentId, CancellationToken cancellationToken = default);
    Task<string?> GetStudentNameAsync(StudentId studentId, CancellationToken cancellationToken = default);
}

// StudentServiceClient.cs (Enrollments.Infrastructure)
public class StudentServiceClient : IStudentServiceClient
{
    private readonly HttpClient _httpClient;

    public async Task<bool> ExistsAsync(StudentId studentId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"api/students/{studentId.Value}", cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }

        response.EnsureSuccessStatusCode();
        return true;
    }
}
```

---

## Shared Kernel（共有カーネル）

両コンテキストで共有される概念を定義します。

### StudentId

**定義場所:** `Shared/ValueObjects/StudentId.cs`

**用途:**
- StudentRegistrationsコンテキスト: Studentの識別子
- Enrollmentsコンテキスト: Enrollmentが参照する学生の識別子

**実装:**

```csharp
namespace Shared.ValueObjects;

public record StudentId
{
    public Guid Value { get; }

    public StudentId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("StudentId cannot be empty", nameof(value));
        Value = value;
    }

    public static StudentId CreateNew() => new(Guid.NewGuid());
}
```

**注意点:**
- StudentId は両コンテキストで同じ意味を持つため、Shared Kernelとして共有
- StudentIdの変更は両コンテキストに影響するため、慎重に行う

---

## データベーススキーマ分離

### student_registrations スキーマ

```sql
CREATE SCHEMA IF NOT EXISTS student_registrations;

CREATE TABLE student_registrations.students (
    id UUID PRIMARY KEY,
    name VARCHAR(200) NOT NULL,
    email VARCHAR(200) NOT NULL,
    grade INT NOT NULL CHECK (grade >= 1 AND grade <= 4),
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE UNIQUE INDEX ix_students_email ON student_registrations.students(email);
CREATE INDEX ix_students_grade ON student_registrations.students(grade);
```

### courses スキーマ

```sql
CREATE SCHEMA IF NOT EXISTS courses;

CREATE TABLE courses.students (
    -- このテーブルは V7 マイグレーションで削除され、
    -- データは student_registrations.students に移行されます
);

CREATE TABLE courses.courses (
    code VARCHAR(10) PRIMARY KEY,
    name VARCHAR(200) NOT NULL,
    -- ...
);

CREATE TABLE courses.semesters (
    year INT NOT NULL,
    period VARCHAR(10) NOT NULL,
    -- ...
    PRIMARY KEY (year, period)
);

CREATE TABLE courses.course_offerings (
    id INT PRIMARY KEY,
    course_code VARCHAR(10) NOT NULL,
    semester_year INT NOT NULL,
    semester_period VARCHAR(10) NOT NULL,
    -- ...
    FOREIGN KEY (course_code) REFERENCES courses.courses(code),
    FOREIGN KEY (semester_year, semester_period) REFERENCES courses.semesters(year, period)
);

CREATE TABLE courses.enrollments (
    id UUID PRIMARY KEY,
    student_id UUID NOT NULL,  -- References student_registrations.students(id) - アプリケーションレベルのみ
    offering_id INT NOT NULL,
    status VARCHAR(20) NOT NULL,
    -- ...
    FOREIGN KEY (offering_id) REFERENCES courses.course_offerings(id)
    -- student_id への外部キー制約はなし（コンテキスト独立性を維持）
);

-- 参照用ビュー（オプション）
CREATE OR REPLACE VIEW courses.v_enrollment_students AS
SELECT
    e.id AS enrollment_id,
    e.student_id,
    s.name AS student_name,
    s.email AS student_email,
    s.grade AS student_grade
FROM courses.enrollments e
LEFT JOIN student_registrations.students s ON e.student_id = s.id;
```

**重要な設計判断:**

1. **クロススキーマ外部キー制約を設けない**
   - `courses.enrollments.student_id` は `student_registrations.students.id` を参照するが、データベースレベルの外部キー制約は設定しない
   - 理由: コンテキストの独立性を保ち、個別にデプロイ・スケーリング可能にするため

2. **整合性はアプリケーションレベルで保証**
   - `IStudentServiceClient`を通じて学生の存在確認を行う
   - 存在しない学生IDでの履修登録は拒否される

3. **参照用ビューは読み取り専用**
   - レポート生成や管理画面での表示用
   - ビューを通じた更新操作は行わない

---

## 設計原則

### コンテキスト境界の尊重

- 各コンテキストは独立してデプロイ可能
- コンテキスト間の通信はHTTP経由のみ
- データベーススキーマは完全に分離

### Anti-Corruption Layer（ACL）の役割

- 上流コンテキスト（StudentRegistrations）のAPIの変更から下流コンテキスト（Enrollments）を保護
- ドメインモデルの純粋性を保つ
- 外部システムとの統合ポイントを明確化

### Shared Kernelの最小化

- 本当に両コンテキストで共有が必要な概念のみを含める
- Shared Kernelの変更は両コンテキストに影響するため、慎重に行う
- 現在はStudentIdのみ

---

## 今後の拡張

### 考えられる新しいコンテキスト

1. **Attendances（出席管理）**
   - StudentRegistrationsとEnrollmentsの両方に依存
   - 学生の出席状況を記録

2. **Grading（成績評価）**
   - Enrollmentsに依存
   - 履修登録に対する成績評価を管理

3. **Notifications（通知）**
   - すべてのコンテキストからイベントを購読
   - メール、SMS、プッシュ通知などを送信

### コンテキスト間統合の進化

現在はHTTP同期通信のみですが、将来的には以下を検討:

- イベント駆動アーキテクチャ（メッセージングによる非同期通信）
- CQRS読み取りモデルの共有
- GraphQL APIによる統合レイヤー

---

## 参考資料

- [AGENTS.md](../AGENTS.md) - プロジェクト全体構造
- [REFACTORING_PLAN.md](../REFACTORING_PLAN.md) - コンテキスト分離のリファクタリング計画
- [architecture-principles.md](impl-patterns/architecture-principles.md) - アーキテクチャ原則
