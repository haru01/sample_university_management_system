using Enrollments.Domain.CourseAggregate;

namespace Enrollments.Application.Queries.GetCourses;

/// <summary>
/// コース一覧取得サービス実装
/// </summary>
public class GetCoursesService : IGetCoursesService
{
    private readonly ICourseRepository _courseRepository;

    public GetCoursesService(ICourseRepository courseRepository)
    {
        _courseRepository = courseRepository;
    }

    public async Task<List<CourseDto>> GetCoursesAsync()
    {
        var courses = await _courseRepository.GetAllAsync();

        return courses.Select(c => new CourseDto
        {
            CourseCode = c.Id.Value,
            Name = c.Name,
            Credits = c.Credits,
            MaxCapacity = c.MaxCapacity
        })
        .ToList();
    }
}
