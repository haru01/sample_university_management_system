namespace Shared.ValueObjects;

/// <summary>
/// 学生ID値オブジェクト（Shared Kernel）
/// </summary>
public record StudentId
{
    public Guid Value { get; }

    public StudentId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Student ID cannot be empty", nameof(value));
        Value = value;
    }

    public static StudentId CreateNew() => new(Guid.NewGuid());

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(StudentId studentId) => studentId.Value;
    public static implicit operator StudentId(Guid value) => new(value);
}
