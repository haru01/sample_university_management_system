namespace Enrollments.Domain.SemesterAggregate;

/// <summary>
/// 学期ID値オブジェクト
/// 年度と学期期間の組み合わせで一意に識別
/// </summary>
public record SemesterId
{
    public int Year { get; }
    public string Period { get; }

    public SemesterId(int year, string period)
    {
        if (year < 2000 || year > 2100)
            throw new ArgumentException("Year must be between 2000 and 2100", nameof(year));

        if (period != "Spring" && period != "Fall")
            throw new ArgumentException("Invalid semester period. Must be Spring or Fall", nameof(period));

        Year = year;
        Period = period;
    }

    public override string ToString() => $"{Year}-{Period}";
}
