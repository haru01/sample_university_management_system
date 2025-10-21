namespace Enrollments.Application.Queries.GetCourses;

/// <summary>
/// コース一覧取得サービスインターフェース
/// </summary>
public interface IGetCoursesService
{
    Task<List<CourseDto>> GetCoursesAsync();
}
