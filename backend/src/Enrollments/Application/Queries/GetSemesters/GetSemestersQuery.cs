using Enrollments.Application.Queries.Semesters;
using MediatR;

namespace Enrollments.Application.Queries.GetSemesters;

/// <summary>
/// 学期一覧取得クエリ
/// </summary>
public record GetSemestersQuery : IRequest<List<SemesterDto>>
{
}
