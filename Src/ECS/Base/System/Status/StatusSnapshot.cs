/// <summary>
/// 状态聚合快照。
/// <para>这是系统消费层读取的统一视图，避免各系统直接解析原始实例表。</para>
/// </summary>
/// <param name="Flags">当前生效标记。</param>
public readonly record struct StatusSnapshot(StatusEffectFlags Flags)
{
    /// <summary>当前是否无敌。</summary>
    public bool IsInvulnerable => Flags.HasFlag(StatusEffectFlags.Invulnerable);

    /// <summary>当前是否免疫控制。</summary>
    public bool IsControlImmune => Flags.HasFlag(StatusEffectFlags.ControlImmune);

    /// <summary>当前是否允许思考。</summary>
    public bool CanThink => !Flags.HasFlag(StatusEffectFlags.SuppressAI);

    /// <summary>当前是否允许主动移动。</summary>
    /// <remarks>该值主要约束输入型策略；不直接阻止脚本外力位移。</remarks>
    public bool CanMoveInput => !Flags.HasFlag(StatusEffectFlags.SuppressMoveInput);

    /// <summary>当前是否允许攻击。</summary>
    public bool CanAttack => !Flags.HasFlag(StatusEffectFlags.SuppressAttack);

    /// <summary>当前是否允许施法。</summary>
    public bool CanCast => !Flags.HasFlag(StatusEffectFlags.SuppressCast);

    /// <summary>当前是否锁定移动。</summary>
    public bool IsMovementLocked => Flags.HasFlag(StatusEffectFlags.LockMovement);

    /// <summary>当前是否允许强制位移。</summary>
    /// <remarks>此字段用于给位移层额外判定，不会自动覆盖其他约束。</remarks>
    public bool AllowsForcedMovement => Flags.HasFlag(StatusEffectFlags.AllowForcedMovement);
}
