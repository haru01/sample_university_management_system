using MediatR;

namespace Enrollments.Application.Commands.EnrollStudent;

/// <summary>
/// 学生をコース開講に履修登録するコマンド
/// </summary>
public record EnrollStudentCommand : IRequest<Guid>
{
    /// <summary>
    /// 学生ID (GUID)
    /// </summary>
    public required Guid StudentId { get; init; }

    /// <summary>
    /// コース開講ID
    /// </summary>
    public required int OfferingId { get; init; }

    /// <summary>
    /// 登録実行者ID（通常は学生本人のID）
    /// </summary>
    public required string EnrolledBy { get; init; }

    /// <summary>
    /// 初期メモ（オプション）
    /// </summary>
    public string? InitialNote { get; init; }
}
