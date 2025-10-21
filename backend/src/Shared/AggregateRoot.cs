namespace Shared;

/// <summary>
/// 集約ルート基底クラス
/// ドメインイベントを発行する能力を持つエンティティ
/// トランザクション境界を定義
/// </summary>
public abstract class AggregateRoot<TId> : Entity<TId> where TId : notnull
{
    private readonly List<DomainEvent> _domainEvents = new();

    public IReadOnlyList<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected AggregateRoot(TId id) : base(id)
    {
    }

    /// <summary>
    /// ドメインイベントを追加
    /// </summary>
    protected void AddDomainEvent(DomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// ドメインイベントをクリア（SaveChanges後に実行）
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
