namespace Shared;

/// <summary>
/// ドメインイベント基底クラス
/// 集約内で発生した重要な出来事を表現
/// </summary>
public abstract record DomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
