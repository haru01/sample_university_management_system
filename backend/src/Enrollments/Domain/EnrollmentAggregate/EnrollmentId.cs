namespace Enrollments.Domain.EnrollmentAggregate;

/// <summary>
/// 履修登録IDを表す値オブジェクト
/// </summary>
public record EnrollmentId
{
    public Guid Value { get; }

    public EnrollmentId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("履修登録IDは空にできません", nameof(value));
        }
        Value = value;
    }

    public EnrollmentId() : this(Guid.NewGuid())
    {
    }

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(EnrollmentId enrollmentId) => enrollmentId.Value;
}
