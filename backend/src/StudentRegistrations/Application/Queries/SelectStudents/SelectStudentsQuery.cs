using StudentRegistrations.Application.Queries.Students;
using MediatR;

namespace StudentRegistrations.Application.Queries.SelectStudents;

/// <summary>
/// 学生一覧取得クエリ
/// </summary>
public record SelectStudentsQuery : IRequest<List<StudentDto>>
{
    /// <summary>
    /// 名前で部分一致検索（オプション）
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// メールアドレスで部分一致検索（オプション）
    /// </summary>
    public string? Email { get; init; }

    /// <summary>
    /// 学年でフィルタリング（オプション）
    /// </summary>
    public int? Grade { get; init; }
}
