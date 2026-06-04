using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// Unit owner 的代码化 Component 组合 profile。
/// <para>这些 profile 复刻旧 UnitCore / Player / Enemy Component Preset 的组件集合。</para>
/// </summary>
public static class UnitComponentCompositionProfiles
{
    public static ComponentCompositionProfile Player()
    {
        var entries = new List<ComponentCompositionEntry>();
        entries.AddRange(CreateUnitCoreEntries());
        entries.AddRange(CreatePlayerEntries());
        return new ComponentCompositionProfile(entries);
    }

    public static ComponentCompositionProfile Enemy()
    {
        var entries = new List<ComponentCompositionEntry>();
        entries.AddRange(CreateEnemyEntries());
        entries.AddRange(CreateUnitCoreEntries());
        return new ComponentCompositionProfile(entries);
    }

    public static ComponentCompositionProfile UnitCore()
    {
        return new ComponentCompositionProfile(CreateUnitCoreEntries());
    }

    private static IEnumerable<ComponentCompositionEntry> CreateUnitCoreEntries()
    {
        yield return Entry<HealthComponent>(nameof(HealthComponent));
        yield return Entry<LifecycleComponent>(nameof(LifecycleComponent));
        yield return Entry<UnitStateComponent>(nameof(UnitStateComponent));
        yield return Entry<RecoveryComponent>(nameof(RecoveryComponent));
        yield return Entry<DataInitComponent>(nameof(DataInitComponent));
        yield return Entry<UnitAnimationComponent>(nameof(UnitAnimationComponent));
        yield return Entry<EntityMovementComponent>(nameof(EntityMovementComponent));
        yield return Entry<EntityOrientationComponent>(
            nameof(EntityOrientationComponent),
            component => component.Configure(
                new EntityOrientationComponentOptions(OrientationSink.VisualFlipX)));
        yield return Entry<CollisionComponent>(nameof(CollisionComponent));
    }

    private static IEnumerable<ComponentCompositionEntry> CreatePlayerEntries()
    {
        yield return Entry<PickupComponent>(nameof(PickupComponent));
        yield return Entry<ActiveSkillInputComponent>(nameof(ActiveSkillInputComponent));
    }

    private static IEnumerable<ComponentCompositionEntry> CreateEnemyEntries()
    {
        yield return Entry<AIComponent>(nameof(AIComponent));
        yield return Entry<AttackComponent>(nameof(AttackComponent));
    }

    private static ComponentCompositionEntry Entry<T>(
        string nodeName,
        Action<T>? configure = null) where T : Node, new()
    {
        return new ComponentCompositionEntry(
            nodeName,
            static () => new T(),
            configure == null ? null : node => configure((T)node));
    }
}
