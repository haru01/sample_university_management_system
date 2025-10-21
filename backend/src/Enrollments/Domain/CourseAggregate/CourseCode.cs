using System.Text.RegularExpressions;
using Enrollments.Domain.Exceptions;

namespace Enrollments.Domain.CourseAggregate;

/// <summary>
/// 科目コード値オブジェクト
/// 形式: 2-4文字のアルファベット + 3-4桁の数字 (例: CS101, MATH1001)
/// </summary>
public partial record CourseCode : IComparable<CourseCode>, IComparable
{
    private const string Pattern = @"^[A-Z]{2,4}\d{3,4}$";

    public string Value { get; }

    public CourseCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ValidationException("COURSE_CODE_EMPTY", "Course code cannot be empty");

        var upperValue = value.ToUpperInvariant();

        if (!CourseCodeRegex().IsMatch(upperValue))
            throw new ValidationException(
                "INVALID_COURSE_CODE_FORMAT",
                $"Invalid course code format: {value}. Expected format: XX000 (e.g., CS101, MATH1001)");

        Value = upperValue;
    }

    [GeneratedRegex(Pattern)]
    private static partial Regex CourseCodeRegex();

    public override string ToString() => Value;

    public int CompareTo(CourseCode? other)
    {
        if (other is null) return 1;
        return string.Compare(Value, other.Value, StringComparison.Ordinal);
    }

    public int CompareTo(object? obj)
    {
        if (obj is null) return 1;
        if (obj is CourseCode other) return CompareTo(other);
        throw new ArgumentException("Object is not a CourseCode");
    }
}
