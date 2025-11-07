using Enrollments.Domain.CourseOfferingAggregate;
using Enrollments.Domain.StudentAggregate;

namespace Enrollments.Domain.EnrollmentAggregate;

/// <summary>
/// Enrollment集約のリポジトリインターフェース
/// </summary>
public interface IEnrollmentRepository
{
    /// <summary>
    /// IDで履修登録を1件取得
    /// </summary>
    Task<Enrollment?> GetByIdAsync(EnrollmentId enrollmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 学生とコース開講IDで履修登録を取得
    /// </summary>
    Task<Enrollment?> GetByStudentAndOfferingAsync(
        StudentId studentId,
        OfferingId offeringId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 学生の履修登録一覧を取得
    /// </summary>
    Task<List<Enrollment>> SelectByStudentAsync(
        StudentId studentId,
        EnrollmentStatus? statusFilter = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// コース開講の履修登録一覧を取得
    /// </summary>
    Task<List<Enrollment>> SelectByOfferingAsync(
        OfferingId offeringId,
        EnrollmentStatus? statusFilter = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// コース開講のアクティブな履修登録数をカウント
    /// </summary>
    Task<int> CountActiveEnrollmentsByOfferingAsync(
        OfferingId offeringId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 学生が特定のコース開講にアクティブな履修登録を持っているか確認
    /// </summary>
    Task<bool> HasActiveEnrollmentAsync(
        StudentId studentId,
        OfferingId offeringId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 新しい履修登録を追加
    /// </summary>
    void Add(Enrollment enrollment);

    /// <summary>
    /// 既存の履修登録を更新
    /// </summary>
    void Update(Enrollment enrollment);

    /// <summary>
    /// 保留中の変更を保存
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
