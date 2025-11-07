using Enrollments.Application.Queries.Enrollments;
using MediatR;

namespace Enrollments.Application.Queries.GetStudentEnrollments;

/// <summary>
/// 学生の履修登録一覧を取得するクエリ
/// </summary>
public record GetStudentEnrollmentsQuery : IRequest<List<EnrollmentDto>>
{
    /// <summary>
    /// 学生ID
    /// </summary>
    public required Guid StudentId { get; init; }

    /// <summary>
    /// オプションのステータスフィルター (Enrolled, Completed, Cancelled)
    /// </summary>
    public string? StatusFilter { get; init; }
}
