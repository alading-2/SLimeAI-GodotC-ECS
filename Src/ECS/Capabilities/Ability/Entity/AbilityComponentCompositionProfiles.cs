using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Ability owner 的代码化 Component 组合 profile。
/// <para>该 profile 复刻旧 AbilityPreset 的组件集合。</para>
/// </summary>
public static class AbilityComponentCompositionProfiles
{
    public static ComponentCompositionProfile Default()
    {
        return new ComponentCompositionProfile(CreateEntries());
    }

    private static IEnumerable<ComponentCompositionEntry> CreateEntries()
    {
        yield return Entry<TriggerComponent>(nameof(TriggerComponent));
        yield return Entry<CooldownComponent>(nameof(CooldownComponent));
        yield return Entry<ChargeComponent>(nameof(ChargeComponent));
        yield return Entry<CostComponent>(nameof(CostComponent));
    }

    private static ComponentCompositionEntry Entry<T>(string nodeName) where T : Node, new()
    {
        return new ComponentCompositionEntry(nodeName, static () => new T());
    }
}
