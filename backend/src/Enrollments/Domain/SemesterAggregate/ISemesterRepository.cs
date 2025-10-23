namespace Enrollments.Domain.SemesterAggregate;

/// <summary>
/// 学期リポジトリインターフェース
/// </summary>
public interface ISemesterRepository
{
    /// <summary>
    /// 学期IDで学期を取得
    /// </summary>
    Task<Semester?> GetByIdAsync(SemesterId id);

    /// <summary>
    /// 全学期を取得
    /// </summary>
    Task<List<Semester>> GetAllAsync();

    /// <summary>
    /// 学期を追加
    /// </summary>
    Task AddAsync(Semester semester);

    /// <summary>
    /// 学期を削除
    /// </summary>
    Task DeleteAsync(Semester semester);

    /// <summary>
    /// 変更を保存
    /// </summary>
    Task SaveChangesAsync();
}
