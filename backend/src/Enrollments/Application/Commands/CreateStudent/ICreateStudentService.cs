namespace Enrollments.Application.Commands.CreateStudent;

/// <summary>
/// 学生作成サービスインターフェース
/// </summary>
public interface ICreateStudentService
{
    /// <summary>
    /// 学生を作成
    /// </summary>
    /// <param name="command">学生作成コマンド</param>
    /// <returns>作成した学生のID（Guid）</returns>
    Task<Guid> CreateStudentAsync(CreateStudentCommand command);
}
