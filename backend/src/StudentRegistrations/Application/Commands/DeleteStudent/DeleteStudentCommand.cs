using MediatR;

namespace StudentRegistrations.Application.Commands.DeleteStudent;

/// <summary>
/// 学生削除コマンド
/// </summary>
public record DeleteStudentCommand : IRequest
{
    public required Guid StudentId { get; init; }
}
