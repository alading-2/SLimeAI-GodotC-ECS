using System;

/// <summary>
/// Ability OnEvent 触发器的受控 typed 订阅表。
/// 这里的 key 是数据配置标识，不是 EventBus 的动态事件名。
/// </summary>
public static class AbilityTriggerEventKey
{
    public const string UnitDamaged = nameof(UnitDamaged);
    public const string UnitKilled = nameof(UnitKilled);
    public const string UnitDodged = nameof(UnitDodged);
    public const string UnitHealApplied = nameof(UnitHealApplied);
    public const string GlobalWaveStarted = nameof(GlobalWaveStarted);
    public const string GlobalWaveCompleted = nameof(GlobalWaveCompleted);
}

public static class AbilityTriggerEventRegistry
{
    public static bool TrySubscribe(
        string triggerKey,
        IEntity owner,
        EventSubscriptionCollector subscriptions,
        Action<object?> handler)
    {
        if (string.IsNullOrWhiteSpace(triggerKey))
        {
            return false;
        }

        switch (triggerKey.Trim())
        {
            case AbilityTriggerEventKey.UnitDamaged:
                subscriptions.Subscribe<UnitEvents.Damaged>(owner.Events, evt => handler(evt));
                return true;

            case AbilityTriggerEventKey.UnitKilled:
                subscriptions.Subscribe<UnitEvents.Killed>(owner.Events, evt => handler(evt));
                return true;

            case AbilityTriggerEventKey.UnitDodged:
                subscriptions.Subscribe<UnitEvents.Dodged>(owner.Events, evt => handler(evt));
                return true;

            case AbilityTriggerEventKey.UnitHealApplied:
                subscriptions.Subscribe<UnitEvents.HealApplied>(owner.Events, evt => handler(evt));
                return true;

            case AbilityTriggerEventKey.GlobalWaveStarted:
                subscriptions.Subscribe<GlobalEvents.WaveStarted>(WorldEvents.World, evt => handler(evt));
                return true;

            case AbilityTriggerEventKey.GlobalWaveCompleted:
                subscriptions.Subscribe<GlobalEvents.WaveCompleted>(WorldEvents.World, evt => handler(evt));
                return true;

            default:
                return false;
        }
    }
}
