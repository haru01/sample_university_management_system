namespace Enrollments.Domain.CourseAggregate;

/// <summary>
/// コースリポジトリインターフェース
/// </summary>
public interface ICourseRepository
{
    Task<Course?> GetByCodeAsync(CourseCode code, CancellationToken cancellationToken = default);
    Task<List<Course>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Course course, CancellationToken cancellationToken = default);

    /// <summary>
    /// 変更を永続化（Unit of Work パターン）
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
