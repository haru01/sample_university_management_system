using Enrollments.Domain.Exceptions;
using Shared;

namespace Enrollments.Domain.SemesterAggregate;

/// <summary>
/// 学期集約ルート
/// </summary>
public class Semester : AggregateRoot<SemesterId>
{
    // EF Core用バッキングフィールド
    private int _idYear;
    private string _idPeriod = string.Empty;

    // Idプロパティをオーバーライドしてバッキングフィールドから再構築
    public new SemesterId Id => new(_idYear, _idPeriod);

    public DateTime StartDate { get; private set; }
    public DateTime EndDate { get; private set; }

    // EF Core用
    private Semester() : base(null!)
    {
    }

    private Semester(SemesterId id, DateTime startDate, DateTime endDate)
        : base(id)
    {
        _idYear = id.Year;
        _idPeriod = id.Period;
        StartDate = startDate;
        EndDate = endDate;
    }

    /// <summary>
    /// 学期を作成
    /// </summary>
    public static Semester Create(int year, string period, DateTime startDate, DateTime endDate)
    {
        EnsureEndDateIsAfterStartDate(startDate, endDate);

        // SemesterIdのコンストラクタで年度と期間のバリデーションが行われる
        var semesterId = new SemesterId(year, period);
        return new Semester(semesterId, startDate, endDate);
    }

    /// <summary>
    /// 学期情報を更新
    /// </summary>
    public void Update(DateTime startDate, DateTime endDate)
    {
        EnsureEndDateIsAfterStartDate(startDate, endDate);

        StartDate = startDate;
        EndDate = endDate;
    }

    /// <summary>
    /// 終了日が開始日より後であることを保証
    /// </summary>
    private static void EnsureEndDateIsAfterStartDate(DateTime startDate, DateTime endDate)
    {
        if (endDate <= startDate)
            throw new ValidationException("INVALID_DATE_RANGE", "End date must be after start date");
    }
}
