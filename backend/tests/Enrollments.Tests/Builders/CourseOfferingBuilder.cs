using Enrollments.Domain.CourseAggregate;
using Enrollments.Domain.CourseOfferingAggregate;
using Enrollments.Domain.SemesterAggregate;

namespace Enrollments.Tests.Builders;

/// <summary>
/// テストデータビルダー - CourseOffering
/// </summary>
public class CourseOfferingBuilder
{
    private int _offeringId = 1;
    private CourseCode _courseCode = new("CS101");
    private SemesterId _semesterId = new(2024, "Spring");
    private int _credits = 3;
    private int _maxCapacity = 30;
    private string? _instructor = "田中教授";

    public CourseOfferingBuilder WithOfferingId(int offeringId)
    {
        _offeringId = offeringId;
        return this;
    }

    public CourseOfferingBuilder WithCourseCode(string courseCode)
    {
        _courseCode = new CourseCode(courseCode);
        return this;
    }

    public CourseOfferingBuilder WithSemesterId(int year, string period)
    {
        _semesterId = new SemesterId(year, period);
        return this;
    }

    public CourseOfferingBuilder WithCredits(int credits)
    {
        _credits = credits;
        return this;
    }

    public CourseOfferingBuilder WithMaxCapacity(int maxCapacity)
    {
        _maxCapacity = maxCapacity;
        return this;
    }

    public CourseOfferingBuilder WithInstructor(string? instructor)
    {
        _instructor = instructor;
        return this;
    }

    public CourseOffering Build() => CourseOffering.Create(
        new OfferingId(_offeringId),
        _courseCode,
        _semesterId,
        _credits,
        _maxCapacity,
        _instructor);
}
