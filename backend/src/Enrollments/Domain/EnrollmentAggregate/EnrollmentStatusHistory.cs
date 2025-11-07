using Enrollments.Domain.Exceptions;
using Shared;

namespace Enrollments.Domain.EnrollmentAggregate;

/// <summary>
/// 履修登録ステータス履歴エンティティ
/// 状態変更の完全な監査証跡を提供
/// </summary>
public class EnrollmentStatusHistory : Entity<EnrollmentStatusHistoryId>
{
    public EnrollmentId EnrollmentId { get; private set; } = null!;
    public EnrollmentStatus Status { get; private set; }
    public DateTime ChangedAt { get; private set; }
    public string ChangedBy { get; private set; } = null!;
    public string? Reason { get; private set; }
    public string? Metadata { get; private set; }

    // EF Core用コンストラクター
    private EnrollmentStatusHistory() : base(null!)
    {
    }

    private EnrollmentStatusHistory(
        EnrollmentStatusHistoryId id,
        EnrollmentId enrollmentId,
        EnrollmentStatus status,
        DateTime changedAt,
        string changedBy,
        string? reason,
        string? metadata)
        : base(id)
    {
        EnrollmentId = enrollmentId;
        Status = status;
        ChangedAt = changedAt;
        ChangedBy = changedBy;
        Reason = reason;
        Metadata = metadata;
    }

    /// <summary>
    /// 新しいステータス履歴レコードを作成
    /// </summary>
    /// <param name="enrollmentId">履修登録ID</param>
    /// <param name="status">変更後のステータス</param>
    /// <param name="changedBy">実行者ID（必須）</param>
    /// <param name="reason">変更理由（Cancelの場合は必須）</param>
    /// <param name="metadata">追加のメタデータ（JSON形式）</param>
    /// <returns>新しい履歴レコード</returns>
    internal static EnrollmentStatusHistory Create(
        EnrollmentId enrollmentId,
        EnrollmentStatus status,
        string changedBy,
        string? reason = null,
        string? metadata = null)
    {
        if (string.IsNullOrWhiteSpace(changedBy))
        {
            throw new ValidationException("実行者は必須です");
        }

        if (changedBy.Length > 100)
        {
            throw new ValidationException("実行者IDは100文字以内である必要があります");
        }

        if (status == EnrollmentStatus.Cancelled && string.IsNullOrWhiteSpace(reason))
        {
            throw new ValidationException("キャンセル理由は必須です");
        }

        if (!string.IsNullOrWhiteSpace(reason) && reason.Length > 1000)
        {
            throw new ValidationException("理由は1000文字以内である必要があります");
        }

        return new EnrollmentStatusHistory(
            new EnrollmentStatusHistoryId(),
            enrollmentId,
            status,
            DateTime.UtcNow,
            changedBy,
            reason,
            metadata);
    }
}
