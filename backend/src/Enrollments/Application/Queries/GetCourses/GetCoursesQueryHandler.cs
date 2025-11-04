using Enrollments.Domain.CourseAggregate;
using MediatR;

namespace Enrollments.Application.Queries.GetCourses;

/// <summary>
/// コース一覧取得クエリハンドラー
/// </summary>
public class GetCoursesQueryHandler : IRequestHandler<GetCoursesQuery, List<CourseDto>>
{
    private readonly ICourseRepository _courseRepository;

    public GetCoursesQueryHandler(ICourseRepository courseRepository)
    {
        _courseRepository = courseRepository;
    }

    public async Task<List<CourseDto>> Handle(GetCoursesQuery request, CancellationToken cancellationToken)
    {
        var courses = await _courseRepository.GetAllAsync(cancellationToken);

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
