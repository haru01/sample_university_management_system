# Domain層 実装パターン

## エンティティ基底クラス

全てのエンティティは `Entity<TId>` を継承し、ID による同一性を持つ。

```csharp
public abstract class Entity<TId> where TId : notnull
{
    public TId Id { get; protected set; }

    protected Entity(TId id) => Id = id;

    public override bool Equals(object? obj)
    {
        if (obj is not Entity<TId> entity) return false;
        return Id.Equals(entity.Id);
    }

    public override int GetHashCode() => Id.GetHashCode();
}
```

### 使用例
```csharp
public class Student : Entity<StudentId>
{
    public string Name { get; private set; }

    public Student(StudentId id, string name) : base(id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        Name = name;
    }
}
```

---

## 集約ルート

ドメインイベントを発行する能力を持つ特別なエンティティ。トランザクション境界を定義する。

```csharp
public abstract class AggregateRoot<TId> : Entity<TId> where TId : notnull
{
    private readonly List<DomainEvent> _domainEvents = new();
    public IReadOnlyList<DomainEvent> DomainEvents => _domainEvents;

    protected AggregateRoot(TId id) : base(id) { }

    protected void AddDomainEvent(DomainEvent @event) => _domainEvents.Add(@event);
    public void ClearDomainEvents() => _domainEvents.Clear();
}
```

### 使用例
```csharp
public class Enrollment : AggregateRoot<EnrollmentId>
{
    public StudentId StudentId { get; private set; }
    public CourseCode CourseCode { get; private set; }
    public EnrollmentStatus Status { get; private set; }

    private Enrollment(EnrollmentId id, StudentId studentId, CourseCode courseCode)
        : base(id)
    {
        StudentId = studentId;
        CourseCode = courseCode;
        Status = EnrollmentStatus.Pending;

        AddDomainEvent(new StudentEnrolledEvent(id, studentId, courseCode));
    }

    public static Enrollment Create(StudentId studentId, CourseCode courseCode)
    {
        return new Enrollment(EnrollmentId.New(), studentId, courseCode);
    }

    public void Approve()
    {
        if (Status != EnrollmentStatus.Pending)
            throw new EnrollmentDomainException("INVALID_STATUS", "承認できない状態です");

        Status = EnrollmentStatus.Approved;
        AddDomainEvent(new EnrollmentApprovedEvent(Id));
    }
}
```

---

## 値オブジェクト（C# 12 record使用）

不変で等価性が値に基づくオブジェクト。バリデーションロジックを内包する。

### 基本パターン（ID型）
```csharp
public record StudentId(Guid Value)
{
    public static StudentId New() => new(Guid.NewGuid());

    public static StudentId From(string value)
    {
        if (!Guid.TryParse(value, out var guid))
            throw new ArgumentException("Invalid StudentId format");
        return new StudentId(guid);
    }
}
```

### 検証ロジック付きパターン
```csharp
public record CourseCode
{
    private const string Pattern = @"^[A-Z]{2,4}\d{3,4}$";

    public string Value { get; }

    public CourseCode(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        if (!Regex.IsMatch(value, Pattern))
            throw new ArgumentException($"Invalid course code format: {value}");

        Value = value;
    }
}
```

### 複合値オブジェクト
```csharp
public record Semester
{
    public int Year { get; }
    public SemesterPeriod Period { get; }

    public Semester(int year, SemesterPeriod period)
    {
        if (year < 2000 || year > 2100)
            throw new ArgumentException("Year must be between 2000 and 2100");

        Year = year;
        Period = period;
    }
}

public enum SemesterPeriod
{
    Spring,
    Fall
}
```

---

## リポジトリインターフェース

集約ルートごとに1つのリポジトリを定義。Infrastructure層で実装される。

```csharp
public interface IEnrollmentRepository
{
    Task<Enrollment?> GetByIdAsync(EnrollmentId id);
    Task<List<Enrollment>> GetByStudentIdAsync(StudentId studentId);
    Task AddAsync(Enrollment enrollment);
    Task UpdateAsync(Enrollment enrollment);
    Task DeleteAsync(EnrollmentId id);
}
```

### 設計原則
- 集約ルート単位でリポジトリを作成
- 集約内のエンティティには個別リポジトリを作らない
- クエリメソッドは必要最小限に（複雑な検索はQueryハンドラーで）

---

## ドメインサービス

複数の集約にまたがるビジネスロジックを実装。

```csharp
public class EnrollmentDomainService
{
    public bool CanEnroll(Student student, Course course, List<Enrollment> existingEnrollments)
    {
        // 履修上限チェック
        if (existingEnrollments.Count >= student.MaxEnrollments)
            return false;

        // 前提科目チェック
        if (course.Prerequisites.Any(prereq =>
            !existingEnrollments.Any(e => e.CourseCode == prereq && e.IsCompleted)))
            return false;

        return true;
    }
}
```

---

## ドメインイベント

集約内で発生した重要な出来事を表現。

```csharp
public abstract record DomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}

public record StudentEnrolledEvent(
    EnrollmentId EnrollmentId,
    StudentId StudentId,
    CourseCode CourseCode
) : DomainEvent;

public record EnrollmentApprovedEvent(
    EnrollmentId EnrollmentId
) : DomainEvent;
```

---

## ドメイン例外

ビジネスルール違反を表現する例外。

```csharp
public class EnrollmentDomainException : Exception
{
    public string Code { get; }

    public EnrollmentDomainException(string code, string message)
        : base(message)
    {
        Code = code;
    }
}

// 使用例
public void Withdraw()
{
    if (Status == EnrollmentStatus.Completed)
        throw new EnrollmentDomainException(
            "CANNOT_WITHDRAW_COMPLETED",
            "完了済みの履修は取り消せません");

    Status = EnrollmentStatus.Withdrawn;
}
```

---

## ベストプラクティス

1. **不変性の維持**
   - 値オブジェクトは常に不変（record使用）
   - エンティティのプロパティは `private set` でカプセル化

2. **ビジネスロジックの配置**
   - エンティティ: 自身の状態に関するロジック
   - ドメインサービス: 複数エンティティにまたがるロジック
   - 集約ルート: トランザクション整合性の保証

3. **バリデーション**
   - 値オブジェクト生成時に必ず検証
   - 不正な状態のオブジェクトを作らせない（Always Valid原則）

4. **命名規則**
   - エンティティ: 名詞（Student, Course）
   - ドメインサービス: 動詞+名詞（EnrollmentDomainService）
   - イベント: 過去形（StudentEnrolledEvent）
