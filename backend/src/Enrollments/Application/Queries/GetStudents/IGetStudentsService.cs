using Enrollments.Application.Queries.Students;
using Enrollments.Domain.StudentAggregate;

namespace Enrollments.Application.Queries.GetStudents;

/// <summary>
/// 学生一覧取得サービスインターフェース
/// </summary>
public interface IGetStudentsService
{
    /// <summary>
    /// 学生一覧を取得
    /// </summary>
    /// <param name="query">フィルタクエリ（Grade、Name、Email はすべてオプション）</param>
    /// <returns>学生一覧（登録日時の昇順）</returns>
    Task<List<StudentDto>> GetStudentsAsync(GetStudentsQuery query);
}
