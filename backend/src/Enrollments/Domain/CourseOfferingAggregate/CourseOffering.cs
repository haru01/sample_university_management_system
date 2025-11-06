using Enrollments.Domain.CourseAggregate;
using Enrollments.Domain.Exceptions;
using Enrollments.Domain.SemesterAggregate;
using Shared;

namespace Enrollments.Domain.CourseOfferingAggregate;

/// <summary>
/// コース開講集約ルート
/// 特定の学期にコースが開講される情報を表す
/// </summary>
public class CourseOffering : AggregateRoot<OfferingId>
{
    private CourseCode _courseCode = null!;
    private SemesterId _semesterId = null!;

    public CourseCode CourseCode
    {
        get => _courseCode;
        private set => _courseCode = value;
    }

    public SemesterId SemesterId
    {
        get => _semesterId;
        private set => _semesterId = value;
    }

    public int Credits { get; private set; }
    public int MaxCapacity { get; private set; }
    public string? Instructor { get; private set; }
    public OfferingStatus Status { get; private set; }

    // EF Core用
    private CourseOffering() : base(null!)
    {
    }

    private CourseOffering(
        OfferingId offeringId,
        CourseCode courseCode,
        SemesterId semesterId,
        int credits,
        int maxCapacity,
        string? instructor)
        : base(offeringId)
    {
        _courseCode = courseCode;
        _semesterId = semesterId;
        Credits = credits;
        MaxCapacity = maxCapacity;
        Instructor = instructor;
        Status = OfferingStatus.Active;
    }

    /// <summary>
    /// コース開講を作成
    /// </summary>
    public static CourseOffering Create(
        OfferingId offeringId,
        CourseCode courseCode,
        SemesterId semesterId,
        int credits,
        int maxCapacity,
        string? instructor)
    {
        EnsureCreditsBetween1And10(credits);
        EnsureMaxCapacityGreaterThanZero(maxCapacity);

        return new CourseOffering(offeringId, courseCode, semesterId, credits, maxCapacity, instructor);
    }

    /// <summary>
    /// コース開講情報を更新
    /// </summary>
    public void Update(int credits, int maxCapacity, string? instructor)
    {
        EnsureNotCancelled();
        EnsureCreditsBetween1And10(credits);
        EnsureMaxCapacityGreaterThanZero(maxCapacity);

        Credits = credits;
        MaxCapacity = maxCapacity;
        Instructor = instructor;
    }

    /// <summary>
    /// コース開講をキャンセル
    /// </summary>
    public void Cancel()
    {
        EnsureNotCancelled();
        Status = OfferingStatus.Cancelled;
    }

    /// <summary>
    /// キャンセル済みでないことを保証
    /// </summary>
    private void EnsureNotCancelled()
    {
        if (Status == OfferingStatus.Cancelled)
            throw new InvalidOperationException("Cannot update cancelled offering");
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
