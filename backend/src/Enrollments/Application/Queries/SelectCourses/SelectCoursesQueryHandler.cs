using Enrollments.Domain.CourseAggregate;
using MediatR;

namespace Enrollments.Application.Queries.SelectCourses;

/// <summary>
/// コース一覧取得クエリハンドラー
/// </summary>
public class SelectCoursesQueryHandler : IRequestHandler<SelectCoursesQuery, List<CourseDto>>
{
    private readonly ICourseRepository _courseRepository;

    public SelectCoursesQueryHandler(ICourseRepository courseRepository)
    {
        _courseRepository = courseRepository;
    }

    public async Task<List<CourseDto>> Handle(SelectCoursesQuery request, CancellationToken cancellationToken)
    {
        var courses = await _courseRepository.SelectAllAsync(cancellationToken);

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
