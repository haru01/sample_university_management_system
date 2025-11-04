namespace Enrollments.Domain.SemesterAggregate;

/// <summary>
/// 学期リポジトリインターフェース
/// </summary>
public interface ISemesterRepository
{
    /// <summary>
    /// 学期IDで学期を取得
    /// </summary>
    Task<Semester?> GetByIdAsync(SemesterId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 全学期を取得
    /// </summary>
    Task<List<Semester>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 現在の学期を取得（現在日時が開始日と終了日の間にある学期）
    /// </summary>
    Task<Semester?> GetCurrentSemesterAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 学期を追加
    /// </summary>
    Task AddAsync(Semester semester, CancellationToken cancellationToken = default);

    /// <summary>
    /// 学期を削除
    /// </summary>
    Task DeleteAsync(Semester semester, CancellationToken cancellationToken = default);

    /// <summary>
    /// 変更を保存
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
