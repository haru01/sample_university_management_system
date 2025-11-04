namespace Enrollments.Domain.StudentAggregate;

/// <summary>
/// 学生検索条件
/// </summary>
public record StudentSearchCriteria
{
    /// <summary>
    /// 学年でフィルタリング（オプション）
    /// </summary>
    public int? Grade { get; init; }

    /// <summary>
    /// 名前で部分一致検索（オプション）
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// メールアドレスで部分一致検索（オプション）
    /// </summary>
    public string? Email { get; init; }
}
