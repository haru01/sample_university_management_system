using Enrollments.Domain.CourseAggregate;

namespace Enrollments.Tests.Builders;

/// <summary>
/// テストデータビルダー - Course
/// </summary>
public class CourseBuilder
{
    private CourseCode _code = new("CS101");
    private string _name = "プログラミング入門";
    private int _credits = 2;
    private int _maxCapacity = 30;

    public CourseBuilder WithCode(string code)
    {
        _code = new CourseCode(code);
        return this;
    }

    public CourseBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public CourseBuilder WithCredits(int credits)
    {
        _credits = credits;
        return this;
    }

    public CourseBuilder WithMaxCapacity(int maxCapacity)
    {
        _maxCapacity = maxCapacity;
        return this;
    }

    public Course Build() => Course.Create(_code, _name, _credits, _maxCapacity);
}
