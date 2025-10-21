using Enrollments.Domain.CourseAggregate;
using Enrollments.Domain.Exceptions;

namespace Enrollments.Application.Commands.CreateCourse;

/// <summary>
/// コース作成サービス実装
/// </summary>
public class CreateCourseService : ICreateCourseService
{
    private readonly ICourseRepository _courseRepository;

    public CreateCourseService(ICourseRepository courseRepository)
    {
        _courseRepository = courseRepository;
    }

    public async Task<string> CreateCourseAsync(CreateCourseCommand command)
    {
        // 値オブジェクト構築
        var courseCode = new CourseCode(command.CourseCode);

        // 重複チェック
        var existing = await _courseRepository.GetByCodeAsync(courseCode);
        if (existing != null)
            throw new ConflictException("COURSE_ALREADY_EXISTS", $"Course with code {courseCode} already exists");

        // コース作成
        var course = Course.Create(
            courseCode,
            command.Name,
            command.Credits,
            command.MaxCapacity);

        // 永続化
        await _courseRepository.AddAsync(course);
        await _courseRepository.SaveChangesAsync();

        return course.Id.Value;
    }
}
