using Enrollments.Domain.CourseOfferingAggregate;
using Enrollments.Domain.Exceptions;
using Enrollments.Domain.StudentAggregate;
using Shared;

namespace Enrollments.Domain.EnrollmentAggregate;

/// <summary>
/// 履修登録集約ルート
/// 学生のコース開講への履修登録を表す
/// </summary>
public class Enrollment : AggregateRoot<EnrollmentId>
{
    public StudentId StudentId { get; private set; } = null!;
    public OfferingId OfferingId { get; private set; } = null!;
    public EnrollmentStatus Status { get; private set; }
    public DateTime EnrolledAt { get; private set; }
    public DateTime? CompletedAt { get; private set; }
    public DateTime? CancelledAt { get; private set; }

    // StatusHistoryへのナビゲーション（読み取り専用）
    private readonly List<EnrollmentStatusHistory> _statusHistory = new();
    public IReadOnlyList<EnrollmentStatusHistory> StatusHistory => _statusHistory.AsReadOnly();

    // EF Core用コンストラクター
    private Enrollment() : base(null!)
    {
    }

    private Enrollment(
        EnrollmentId id,
        StudentId studentId,
        OfferingId offeringId,
        EnrollmentStatus status,
        DateTime enrolledAt)
        : base(id)
    {
        StudentId = studentId;
        OfferingId = offeringId;
        Status = status;
        EnrolledAt = enrolledAt;
    }

    /// <summary>
    /// 新しい履修登録を作成
    /// </summary>
    /// <param name="studentId">学生ID</param>
    /// <param name="offeringId">コース開講ID</param>
    /// <param name="enrolledBy">登録実行者ID（必須）</param>
    /// <param name="initialNote">初期メモ（オプション）</param>
    /// <returns>新しい履修登録</returns>
    public static Enrollment Create(
        StudentId studentId,
        OfferingId offeringId,
        string enrolledBy,
        string? initialNote = null)
    {
        if (studentId == null)
        {
            throw new ValidationException("学生IDは必須です");
        }

        if (offeringId == null)
        {
            throw new ValidationException("コース開講IDは必須です");
        }

        if (string.IsNullOrWhiteSpace(enrolledBy))
        {
            throw new ValidationException("登録実行者は必須です");
        }

        var enrollmentId = new EnrollmentId();
        var enrollment = new Enrollment(
            enrollmentId,
            studentId,
            offeringId,
            EnrollmentStatus.Enrolled,
            DateTime.UtcNow);

        // 初期ステータス履歴レコードを作成
        var metadata = !string.IsNullOrWhiteSpace(initialNote)
            ? System.Text.Json.JsonSerializer.Serialize(new { InitialNote = initialNote })
            : null;

        var history = EnrollmentStatusHistory.Create(
            enrollmentId,
            EnrollmentStatus.Enrolled,
            enrolledBy,
            "Initial enrollment",
            metadata);

        enrollment._statusHistory.Add(history);

        return enrollment;
    }

    /// <summary>
    /// 履修登録を完了する（仮登録 → 本登録）
    /// </summary>
    /// <param name="completedBy">完了実行者ID（必須、通常はシステムID）</param>
    /// <param name="reason">完了理由（オプション）</param>
    public void Complete(string completedBy, string? reason = null)
    {
        if (string.IsNullOrWhiteSpace(completedBy))
        {
            throw new ValidationException("実行者は必須です");
        }

        if (Status == EnrollmentStatus.Cancelled)
        {
            throw new InvalidOperationException("キャンセル済みの履修登録は完了できません");
        }

        if (Status == EnrollmentStatus.Completed)
        {
            throw new InvalidOperationException("既に完了しています");
        }

        // ステータス履歴レコードを追加
        var history = EnrollmentStatusHistory.Create(
            Id,
            EnrollmentStatus.Completed,
            completedBy,
            reason);

        _statusHistory.Add(history);

        // ステータスを更新
        Status = EnrollmentStatus.Completed;
        CompletedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 履修登録をキャンセルする
    /// </summary>
    /// <param name="cancelledBy">キャンセル実行者ID（必須）</param>
    /// <param name="reason">キャンセル理由（必須）</param>
    public void Cancel(string cancelledBy, string reason)
    {
        if (string.IsNullOrWhiteSpace(cancelledBy))
        {
            throw new ValidationException("実行者は必須です");
        }

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ValidationException("キャンセル理由は必須です");
        }

        if (Status == EnrollmentStatus.Completed)
        {
            throw new InvalidOperationException("完了した履修登録はキャンセルできません");
        }

        if (Status == EnrollmentStatus.Cancelled)
        {
            throw new InvalidOperationException("既にキャンセル済みです");
        }

        // ステータス履歴レコードを追加
        var history = EnrollmentStatusHistory.Create(
            Id,
            EnrollmentStatus.Cancelled,
            cancelledBy,
            reason);

        _statusHistory.Add(history);

        // ステータスを更新
        Status = EnrollmentStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
    }

    /// <summary>
    /// 履修登録がアクティブか確認（キャンセルされていない）
    /// </summary>
    public bool IsActive() => Status != EnrollmentStatus.Cancelled;

    /// <summary>
    /// 履修登録が完了しているか確認
    /// </summary>
    public bool IsCompleted() => Status == EnrollmentStatus.Completed;
}
