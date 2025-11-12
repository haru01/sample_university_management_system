namespace Attendance.Domain.ClassSessionAggregate;

/// <summary>
/// 授業セッションリポジトリインターフェース
/// </summary>
public interface IClassSessionRepository
{
    /// <summary>
    /// 授業セッションを追加
    /// </summary>
    Task<ClassSession> AddAsync(ClassSession classSession, CancellationToken cancellationToken = default);

    /// <summary>
    /// SessionIdで授業セッションを取得
    /// </summary>
    Task<ClassSession?> GetByIdAsync(SessionId sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// OfferingIdとSessionNumberで授業セッションを取得
    /// </summary>
    Task<ClassSession?> GetByOfferingAndSessionNumberAsync(
        int offeringId,
        int sessionNumber,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// OfferingIdで授業セッション一覧を取得
    /// </summary>
    Task<IReadOnlyList<ClassSession>> GetByOfferingIdAsync(
        int offeringId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 授業セッションを更新
    /// </summary>
    Task UpdateAsync(ClassSession classSession, CancellationToken cancellationToken = default);

    /// <summary>
    /// 変更を保存
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 次のSessionIdを取得（手動ID生成用）
    /// </summary>
    Task<SessionId> GetNextSessionIdAsync(CancellationToken cancellationToken = default);
}
