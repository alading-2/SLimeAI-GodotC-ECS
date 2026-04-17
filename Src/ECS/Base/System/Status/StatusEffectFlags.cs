using System;

/// <summary>
/// 状态效果聚合标记。
/// <para>多个来源状态按位或聚合后生成 StatusSnapshot。</para>
/// </summary>
[Flags]
public enum StatusEffectFlags
{
    None = 0,
    /// <summary>禁止 AI/思考逻辑。</summary>
    SuppressAI = 1 << 0,
    /// <summary>禁止主动输入型移动（不等于禁止外力位移）。</summary>
    SuppressMoveInput = 1 << 1,
    /// <summary>禁止普通攻击。</summary>
    SuppressAttack = 1 << 2,
    /// <summary>禁止施法。</summary>
    SuppressCast = 1 << 3,
    /// <summary>无敌：伤害结算应被忽略。</summary>
    Invulnerable = 1 << 4,
    /// <summary>免疫控制类效果。</summary>
    ControlImmune = 1 << 5,
    /// <summary>锁定移动。</summary>
    LockMovement = 1 << 6,
    /// <summary>允许强制位移（击退/牵引等）继续生效。</summary>
    AllowForcedMovement = 1 << 7,
}
