namespace Attendance.Domain.ClassSessionAggregate;

/// <summary>
/// 授業セッションID値オブジェクト
/// </summary>
public record SessionId
{
    public int Value { get; }

    public SessionId(int value)
    {
        // 0はEF Coreの自動生成用に許可、負の値のみ禁止
        if (value < 0)
            throw new ArgumentException("SessionId must not be negative");

        Value = value;
    }

    public static implicit operator int(SessionId sessionId) => sessionId.Value;
    public static implicit operator SessionId(int value) => new(value);

    public override string ToString() => Value.ToString();
}
