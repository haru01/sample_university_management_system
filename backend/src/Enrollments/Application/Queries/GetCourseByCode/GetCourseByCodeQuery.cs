using Enrollments.Application.Queries.SelectCourses;
using MediatR;

namespace Enrollments.Application.Queries.GetCourseByCode;

/// <summary>
/// コード指定コース取得クエリ
/// </summary>
public record GetCourseByCodeQuery : IRequest<CourseDto?>
{
    public required string CourseCode { get; init; }
}
