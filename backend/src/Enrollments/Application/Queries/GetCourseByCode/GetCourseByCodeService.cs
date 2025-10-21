using Enrollments.Application.Queries.GetCourses;
using Enrollments.Domain.CourseAggregate;

namespace Enrollments.Application.Queries.GetCourseByCode;

/// <summary>
/// コード指定コース取得サービス実装
/// </summary>
public class GetCourseByCodeService : IGetCourseByCodeService
{
    private readonly ICourseRepository _courseRepository;

    public GetCourseByCodeService(ICourseRepository courseRepository)
    {
        _courseRepository = courseRepository;
    }

    public async Task<CourseDto?> GetCourseByCodeAsync(string courseCode)
    {
        var code = new CourseCode(courseCode);
        var course = await _courseRepository.GetByCodeAsync(code);

        if (course == null)
            return null;

        return new CourseDto
        {
            CourseCode = course.Id.Value,
            Name = course.Name,
            Credits = course.Credits,
            MaxCapacity = course.MaxCapacity
        };
    }
}
