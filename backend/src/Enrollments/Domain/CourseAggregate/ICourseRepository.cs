namespace Enrollments.Domain.CourseAggregate;

/// <summary>
/// コースリポジトリインターフェース
/// </summary>
public interface ICourseRepository
{
    Task<Course?> GetByCodeAsync(CourseCode code);
    Task<List<Course>> GetAllAsync();
    Task AddAsync(Course course);

    /// <summary>
    /// 変更を永続化（Unit of Work パターン）
    /// </summary>
    Task SaveChangesAsync();
}
