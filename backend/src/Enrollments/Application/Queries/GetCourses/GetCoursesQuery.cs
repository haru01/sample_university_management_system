using MediatR;

namespace Enrollments.Application.Queries.GetCourses;

/// <summary>
/// コース一覧取得クエリ
/// </summary>
public record GetCoursesQuery : IRequest<List<CourseDto>>;
