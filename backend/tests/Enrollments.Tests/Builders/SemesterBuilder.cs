using Enrollments.Domain.SemesterAggregate;

namespace Enrollments.Tests.Builders;

/// <summary>
/// テストデータビルダー - Semester
/// </summary>
public class SemesterBuilder
{
    private int _year = 2024;
    private string _period = "Spring";
    private DateTime _startDate = new DateTime(2024, 4, 1);
    private DateTime _endDate = new DateTime(2024, 9, 30);

    public SemesterBuilder WithYear(int year)
    {
        _year = year;
        return this;
    }

    public SemesterBuilder WithPeriod(string period)
    {
        _period = period;
        return this;
    }

    public SemesterBuilder WithStartDate(DateTime startDate)
    {
        _startDate = startDate;
        return this;
    }

    public SemesterBuilder WithEndDate(DateTime endDate)
    {
        _endDate = endDate;
        return this;
    }

    public Semester Build() => Semester.Create(_year, _period, _startDate, _endDate);
}
