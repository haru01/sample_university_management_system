using Enrollments.Application.Queries.GetCourses;

namespace Enrollments.Application.Queries.GetCourseByCode;

/// <summary>
/// コード指定コース取得サービスインターフェース
/// </summary>
public interface IGetCourseByCodeService
{
    Task<CourseDto?> GetCourseByCodeAsync(string courseCode);
}
