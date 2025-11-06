namespace Enrollments.Domain.CourseOfferingAggregate;

/// <summary>
/// コース開講ID値オブジェクト
/// </summary>
public record OfferingId
{
    public int Value { get; }

    public OfferingId(int value)
    {
        if (value <= 0)
            throw new ArgumentException("OfferingId must be greater than 0", nameof(value));

        Value = value;
    }

    public override string ToString() => Value.ToString();

    public static implicit operator int(OfferingId offeringId) => offeringId.Value;
    public static implicit operator OfferingId(int value) => new(value);
}
