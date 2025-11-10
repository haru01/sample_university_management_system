using StudentRegistrations.Application.Queries.Students;
using MediatR;

namespace StudentRegistrations.Application.Queries.GetStudent;

/// <summary>
/// 学生取得クエリ
/// </summary>
public record GetStudentQuery : IRequest<StudentDto>
{
    /// <summary>
    /// 学生ID
    /// </summary>
    public required Guid StudentId { get; init; }
}
