using Shared.ValueObjects;

namespace Enrollments.Application.Services;

/// <summary>
/// StudentRegistrationsコンテキストへのAnti-Corruption Layer
/// 学生情報を取得するためのクライアントインターフェース
/// </summary>
/// <remarks>
/// HTTP通信でStudentRegistrations APIを呼び出して学生情報を取得します。
/// これにより、EnrollmentsコンテキストとStudentRegistrationsコンテキストが
/// 疎結合に保たれ、それぞれ独立したデプロイが可能になります。
/// </remarks>
public interface IStudentServiceClient
{
    /// <summary>
    /// 学生が存在するか確認する
    /// </summary>
    Task<bool> ExistsAsync(StudentId studentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 学生の名前を取得する
    /// </summary>
    Task<string?> GetStudentNameAsync(StudentId studentId, CancellationToken cancellationToken = default);
}
