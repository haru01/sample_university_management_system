using Enrollments.Application.Queries.Semesters;
using MediatR;

namespace Enrollments.Application.Queries.GetCurrentSemester;

/// <summary>
/// 現在の学期取得クエリ
/// </summary>
public record GetCurrentSemesterQuery : IRequest<SemesterDto?>
{
}
