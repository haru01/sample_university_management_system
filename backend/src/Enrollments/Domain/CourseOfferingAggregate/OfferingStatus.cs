namespace Enrollments.Domain.CourseOfferingAggregate;

/// <summary>
/// コース開講のステータス
/// </summary>
public enum OfferingStatus
{
    /// <summary>
    /// アクティブ（開講中）
    /// </summary>
    Active,

    /// <summary>
    /// キャンセル（開講中止）
    /// </summary>
    Cancelled
}
