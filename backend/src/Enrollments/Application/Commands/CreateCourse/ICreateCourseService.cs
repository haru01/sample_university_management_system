namespace Enrollments.Application.Commands.CreateCourse;

/// <summary>
/// コース作成サービスインターフェース
/// </summary>
public interface ICreateCourseService
{
    Task<string> CreateCourseAsync(CreateCourseCommand command);
}
