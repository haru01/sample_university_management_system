namespace Enrollments.Domain.StudentAggregate;

/// <summary>
/// 学生リポジトリインターフェース
/// </summary>
public interface IStudentRepository
{
    /// <summary>
    /// 学生IDで学生を取得
    /// </summary>
    Task<Student?> GetByIdAsync(StudentId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// メールアドレスで学生を取得
    /// </summary>
    Task<Student?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// 全学生を取得
    /// </summary>
    Task<List<Student>> SelectAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 条件付きで学生を取得
    /// </summary>
    /// <param name="criteria">検索条件（Grade、Name、Email はすべてオプション）</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>フィルタリング済み学生一覧（登録日時昇順）</returns>
    Task<List<Student>> SelectFilteredAsync(StudentSearchCriteria criteria, CancellationToken cancellationToken = default);

    /// <summary>
    /// 学生を追加
    /// </summary>
    Task AddAsync(Student student, CancellationToken cancellationToken = default);

    /// <summary>
    /// 学生を削除
    /// </summary>
    Task DeleteAsync(Student student, CancellationToken cancellationToken = default);

    /// <summary>
    /// 変更を保存
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
