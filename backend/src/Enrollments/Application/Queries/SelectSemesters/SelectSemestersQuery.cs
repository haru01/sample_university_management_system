using Enrollments.Application.Queries.Semesters;
using MediatR;

namespace Enrollments.Application.Queries.SelectSemesters;

/// <summary>
/// 学期一覧取得クエリ
/// </summary>
public record SelectSemestersQuery : IRequest<List<SemesterDto>>
{
}
