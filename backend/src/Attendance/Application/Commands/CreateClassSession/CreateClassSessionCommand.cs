using MediatR;

namespace Attendance.Application.Commands.CreateClassSession;

/// <summary>
/// 授業セッション作成コマンド
/// </summary>
public record CreateClassSessionCommand : IRequest<int>
{
    public required int OfferingId { get; init; }
    public required int SessionNumber { get; init; }
    public required DateOnly SessionDate { get; init; }
    public required TimeOnly StartTime { get; init; }
    public required TimeOnly EndTime { get; init; }
    public string? Location { get; init; }
    public string? Topic { get; init; }
}
