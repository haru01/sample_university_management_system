using Enrollments.Domain.Exceptions;
using Shared;

namespace Enrollments.Domain.CourseAggregate;

/// <summary>
/// コース集約ルート
/// </summary>
public class Course : AggregateRoot<CourseCode>
{
    public string Name { get; private set; }
    public int Credits { get; private set; }
    public int MaxCapacity { get; private set; }

    // EF Core用
    private Course() : base(null!)
    {
        Name = string.Empty;
    }

    private Course(CourseCode code, string name, int credits, int maxCapacity)
        : base(code)
    {
        Name = name;
        Credits = credits;
        MaxCapacity = maxCapacity;
    }

    /// <summary>
    /// コース作成
    /// </summary>
    public static Course Create(CourseCode code, string name, int credits, int maxCapacity)
    {
        EnsureNameNotEmpty(name);
        EnsureCreditsBetween1And10(credits);
        EnsureMaxCapacityGreaterThanZero(maxCapacity);

        return new Course(code, name, credits, maxCapacity);
    }

    /// <summary>
    /// コース情報更新
    /// </summary>
    public void Update(string name, int credits, int maxCapacity)
    {
        EnsureNameNotEmpty(name);
        EnsureCreditsBetween1And10(credits);
        EnsureMaxCapacityGreaterThanZero(maxCapacity);

        Name = name;
        Credits = credits;
        MaxCapacity = maxCapacity;
    }

    /// <summary>
    /// コース名が空でないことを保証
    /// </summary>
    private static void EnsureNameNotEmpty(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ValidationException("COURSE_NAME_EMPTY", "Course name cannot be empty");
    }

    /// <summary>
    /// 単位数が1から10の範囲内であることを保証
    /// </summary>
    private static void EnsureCreditsBetween1And10(int credits)
    {
        if (credits < 1 || credits > 10)
            throw new ValidationException("INVALID_CREDITS", "Credits must be between 1 and 10");
    }

    /// <summary>
    /// 定員が1以上であることを保証
    /// </summary>
    private static void EnsureMaxCapacityGreaterThanZero(int maxCapacity)
    {
        if (maxCapacity < 1)
            throw new ValidationException("INVALID_MAX_CAPACITY", "Max capacity must be greater than 0");
    }
}
