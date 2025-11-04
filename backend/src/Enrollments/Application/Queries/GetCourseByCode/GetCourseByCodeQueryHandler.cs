using Enrollments.Application.Queries.GetCourses;
using Enrollments.Domain.CourseAggregate;
using MediatR;

namespace Enrollments.Application.Queries.GetCourseByCode;

/// <summary>
/// コード指定コース取得クエリハンドラー
/// </summary>
public class GetCourseByCodeQueryHandler : IRequestHandler<GetCourseByCodeQuery, CourseDto?>
{
    private readonly ICourseRepository _courseRepository;

    public GetCourseByCodeQueryHandler(ICourseRepository courseRepository)
    {
        _courseRepository = courseRepository;
    }

    public async Task<CourseDto?> Handle(GetCourseByCodeQuery request, CancellationToken cancellationToken)
    {
        var code = new CourseCode(request.CourseCode);
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
