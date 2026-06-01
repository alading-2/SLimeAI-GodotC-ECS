using System;
using Godot;

/// <summary>
/// 全局事件总线
/// <para>采用混合模式以平衡性能与灵活性：</para>
/// <para>1. 传统 C# 静态事件 (Legacy)：用于强类型、极高性能要求的核心流程。</para>
/// <para>2. 新版通用事件总线 (Global)：用于模块间解耦、动态事件订阅及跨模块通信。</para>
/// </summary>
public static class GlobalEventBus
{
    private static readonly Log _log = new Log("GlobalEventBus");

    /// <summary>
    /// 全局通用事件总线实例。
    /// <para>推荐用于大多数非核心高频的游戏逻辑事件（如 UI 更新、成就触发、关卡状态变更等）。</para>
    /// </summary>
    public static readonly EventBus Global = new EventBus();

    // ============================================================
    // 便捷触发方法 (Convenience Trigger Methods)
    // ============================================================

    /// <summary>
    /// 触发波次开始事件
    /// </summary>
    /// <param name="waveIndex">当前开始的波次索引（从 0 或 1 开始，取决于配置）</param>
    public static void TriggerWaveStarted(int waveIndex)
    {
        Global.Emit(new GameEventType.Global.WaveStarted(waveIndex));
    }

    /// <summary>
    /// 触发波次完成事件
    /// </summary>
    /// <param name="waveIndex">刚刚完成的波次索引</param>
    public static void TriggerWaveCompleted(int waveIndex)
    {
        Global.Emit(new GameEventType.Global.WaveCompleted(waveIndex));
    }

    /// <summary>
    /// 触发游戏开始事件
    /// </summary>
    public static void TriggerGameStart()
    {
        Global.Emit(new GameEventType.Global.GameStart());
    }

    /// <summary>
    /// 触发游戏暂停事件
    /// </summary>
    public static void TriggerGamePause()
    {
        Global.Emit(new GameEventType.Global.GamePause());
    }

    /// <summary>
    /// 触发游戏恢复事件
    /// </summary>
    public static void TriggerGameResume()
    {
        Global.Emit(new GameEventType.Global.GameResume());
    }

    /// <summary>
    /// 触发游戏结束事件
    /// </summary>
    /// <param name="isVictory">是否胜利</param>
    public static void TriggerGameOver(bool isVictory)
    {
        Global.Emit(new GameEventType.Global.GameOver(isVictory));
    }

    /// <summary>
    /// 触发单位击杀事件（全局广播）
    /// </summary>
    public static void TriggerUnitKilled(IEntity victim, IEntity killer)
    {
        Global.Emit(new GameEventType.Unit.Killed(
            Victim: victim,
            Killer: killer
        ));
    }

    /// <summary>
    /// 触发单位等级提升事件
    /// </summary>
    public static void TriggerLevelUp(IEntity entity, int oldLevel, int newLevel)
    {
        Global.Emit(new GameEventType.Unit.LevelUp(entity, oldLevel, newLevel));
    }

    // ============================================================
    // 战斗结果事件便捷方法
    // ============================================================

    /// <summary>
    /// 触发单位受伤事件（全局广播）
    /// </summary>
    public static void TriggerDamaged(IEntity victim, float amount, IEntity? attacker = null,
        DamageType type = DamageType.True, bool isCritical = false)
    {
        Global.Emit(new GameEventType.Unit.Damaged(victim, amount, attacker, type, isCritical));
    }

    /// <summary>
    /// 触发治疗已应用事件（全局广播）
    /// </summary>
    public static void TriggerHealApplied(IEntity victim, float requestedAmount, float actualAmount, HealSource source)
    {
        Global.Emit(new GameEventType.Unit.HealApplied(victim, requestedAmount, actualAmount, source));
    }

    /// <summary>
    /// 触发闪避事件（全局广播）
    /// </summary>
    public static void TriggerDodged(IEntity victim, IEntity? attacker = null)
    {
        Global.Emit(new GameEventType.Unit.Dodged(victim, attacker));
    }
}
