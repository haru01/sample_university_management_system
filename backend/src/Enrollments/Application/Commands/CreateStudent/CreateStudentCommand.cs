using MediatR;

namespace Enrollments.Application.Commands.CreateStudent;

/// <summary>
/// 学生作成コマンド
/// </summary>
public record CreateStudentCommand : IRequest<Guid>
{
    public required string Name { get; init; }
    public required string Email { get; init; }
    public required int Grade { get; init; }
}
