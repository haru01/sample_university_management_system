using MediatR;

namespace Enrollments.Application.Queries.SelectCourses;

/// <summary>
/// コース一覧取得クエリ
/// </summary>
public record SelectCoursesQuery : IRequest<List<CourseDto>>;
