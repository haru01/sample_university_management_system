namespace Enrollments.Application.Commands.UpdateStudent;

/// <summary>
/// 学生情報更新サービスインターフェース
/// </summary>
public interface IUpdateStudentService
{
    /// <summary>
    /// 学生情報を更新
    /// </summary>
    /// <param name="command">学生更新コマンド</param>
    /// <returns>更新完了時のTask</returns>
    Task UpdateStudentAsync(UpdateStudentCommand command);
}
