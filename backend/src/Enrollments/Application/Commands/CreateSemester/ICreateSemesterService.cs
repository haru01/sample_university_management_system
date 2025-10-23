using Enrollments.Domain.SemesterAggregate;

namespace Enrollments.Application.Commands.CreateSemester;

/// <summary>
/// 学期作成サービスインターフェース
/// </summary>
public interface ICreateSemesterService
{
    /// <summary>
    /// 学期を作成
    /// </summary>
    /// <param name="command">学期作成コマンド</param>
    /// <returns>作成した学期のID（年度と期間）</returns>
    Task<SemesterId> CreateSemesterAsync(CreateSemesterCommand command);
}
