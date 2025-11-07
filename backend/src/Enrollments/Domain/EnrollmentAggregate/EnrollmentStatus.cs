namespace Enrollments.Domain.EnrollmentAggregate;

/// <summary>
/// 履修登録のステータス
/// </summary>
public enum EnrollmentStatus
{
    /// <summary>
    /// 仮登録（履修登録済みだが確定していない）
    /// </summary>
    Enrolled,

    /// <summary>
    /// 本登録（履修登録が確定済み）
    /// </summary>
    Completed,

    /// <summary>
    /// キャンセル済み
    /// </summary>
    Cancelled
}
