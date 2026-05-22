/// <summary>
/// Ability 实体事件。请求/校验流程不再通过事件响应，保留通知型 payload。
/// </summary>
public static class AbilityEvents
{
    public readonly record struct Added(AbilityEntity Ability, IEntity Owner) : IEntityEvent;

    public readonly record struct Removed(string AbilityName, string AbilityId, IEntity Owner) : IEntityEvent;

    public readonly record struct LevelUp(AbilityEntity? Ability, int OldLevel, int NewLevel) : IEntityEvent;

    public readonly record struct Ready() : IEntityEvent;

    public readonly record struct ChargeRestored(int CurrentCharges, int MaxCharges) : IEntityEvent;

    public readonly record struct AddCharge(int Amount) : IEntityEvent;

    public readonly record struct CostConsumed(AbilityCostType CostType, float Amount) : IEntityEvent;

    public readonly record struct Activated(CastContext Context) : IEntityEvent;

    public readonly record struct Executed(AbilityExecutedResult? Result) : IEntityEvent;

    public readonly record struct Cancelled(string Reason) : IEntityEvent;
}
