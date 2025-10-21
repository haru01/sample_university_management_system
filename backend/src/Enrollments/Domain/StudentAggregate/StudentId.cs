namespace Enrollments.Domain.StudentAggregate;

/// <summary>
/// 学生ID値オブジェクト
/// </summary>
public record StudentId
{
    public Guid Value { get; }

    public StudentId(Guid value = default)
    {
        if (value == default)
            Value = Guid.NewGuid();
        else
            Value = value;
    }

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(StudentId studentId) => studentId.Value;
    public static implicit operator StudentId(Guid value) => new(value);
}
