using MediatR;

namespace Enrollments.Application.Commands.CompleteEnrollment;

/// <summary>
/// 履修登録を完了するコマンド（仮登録 → 本登録）
/// </summary>
public record CompleteEnrollmentCommand : IRequest
{
    /// <summary>
    /// 完了する履修登録ID
    /// </summary>
    public required Guid EnrollmentId { get; init; }

    /// <summary>
    /// 完了実行者ID（必須、通常はシステムID）
    /// </summary>
    public required string CompletedBy { get; init; }

    /// <summary>
    /// 完了理由（オプション）
    /// </summary>
    public string? Reason { get; init; }
}
