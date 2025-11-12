namespace Attendance.Domain.ClassSessionAggregate;

/// <summary>
/// 授業セッションステータス
/// </summary>
public enum SessionStatus
{
    /// <summary>予定</summary>
    Scheduled,

    /// <summary>完了</summary>
    Completed,

    /// <summary>キャンセル</summary>
    Cancelled
}
