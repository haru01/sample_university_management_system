using Enrollments.Domain.CourseAggregate;
using Enrollments.Domain.SemesterAggregate;

namespace Enrollments.Domain.CourseOfferingAggregate;

/// <summary>
/// コース開講リポジトリインターフェース
/// </summary>
public interface ICourseOfferingRepository
{
    /// <summary>
    /// コース開講を取得（IDで）
    /// </summary>
    Task<CourseOffering?> GetByIdAsync(OfferingId offeringId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 学期ごとのコース開講一覧を取得
    /// </summary>
    Task<List<CourseOffering>> SelectBySemesterAsync(
        SemesterId semesterId,
        OfferingStatus? statusFilter = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 特定のコースと学期の組み合わせでコース開講を取得
    /// </summary>
    Task<CourseOffering?> GetByCourseAndSemesterAsync(
        CourseCode courseCode,
        SemesterId semesterId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// コース開講が履修登録されているかを確認
    /// </summary>
    Task<bool> HasEnrollmentsAsync(OfferingId offeringId, CancellationToken cancellationToken = default);

    /// <summary>
    /// コース開講を追加
    /// </summary>
    void Add(CourseOffering courseOffering);

    /// <summary>
    /// コース開講を更新
    /// </summary>
    void Update(CourseOffering courseOffering);

    /// <summary>
    /// 変更を保存
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// 次のOfferingIdを取得
    /// </summary>
    Task<OfferingId> GetNextOfferingIdAsync(CancellationToken cancellationToken = default);
}
