using MediatR;

namespace Enrollments.Application.Commands.UpdateStudent;

/// <summary>
/// 学生情報更新コマンド
/// </summary>
public record UpdateStudentCommand : IRequest
{
    public required Guid StudentId { get; init; }
    public required string Name { get; init; }
    public required string Email { get; init; }
    public required int Grade { get; init; }
}
