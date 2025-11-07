using MediatR;

namespace Enrollments.Application.Commands.CancelEnrollment;

/// <summary>
/// 履修登録をキャンセルするコマンド
/// </summary>
public record CancelEnrollmentCommand : IRequest
{
    /// <summary>
    /// キャンセルする履修登録ID
    /// </summary>
    public required Guid EnrollmentId { get; init; }

    /// <summary>
    /// キャンセル実行者ID（必須）
    /// </summary>
    public required string CancelledBy { get; init; }

    /// <summary>
    /// キャンセル理由（必須）
    /// </summary>
    public required string Reason { get; init; }
}
