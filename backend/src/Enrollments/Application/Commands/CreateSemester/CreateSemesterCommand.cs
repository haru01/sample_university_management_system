namespace Enrollments.Application.Commands.CreateSemester;

/// <summary>
/// 学期作成コマンド
/// </summary>
public record CreateSemesterCommand
{
    public required int Year { get; init; }
    public required string Period { get; init; }
    public required DateTime StartDate { get; init; }
    public required DateTime EndDate { get; init; }
}
