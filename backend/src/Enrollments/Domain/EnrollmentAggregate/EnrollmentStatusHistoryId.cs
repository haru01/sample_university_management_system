namespace Enrollments.Domain.EnrollmentAggregate;

/// <summary>
/// 履修登録ステータス履歴IDを表す値オブジェクト
/// </summary>
public record EnrollmentStatusHistoryId
{
    public Guid Value { get; }

    public EnrollmentStatusHistoryId(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("履歴IDは空にできません", nameof(value));
        }
        Value = value;
    }

    public EnrollmentStatusHistoryId() : this(Guid.NewGuid())
    {
    }

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(EnrollmentStatusHistoryId id) => id.Value;
}
