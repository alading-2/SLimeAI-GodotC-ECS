using System.Collections.Generic;
using Godot;

/// <summary>
/// 一次技能触发的施法上下文。
/// </summary>
public sealed class CastContext
{
    public IEntity? Caster { get; set; }

    public AbilityEntity? Ability { get; set; }

    public List<IEntity>? Targets { get; set; }

    public Vector2? TargetPosition { get; set; }

    public Vector2 InputDirection { get; set; }

    public object? SourceEventData { get; set; }

    public bool HasPreselectedTargets => Targets != null && Targets.Count > 0;

    public bool HasPreselectedPosition => TargetPosition.HasValue;
}
