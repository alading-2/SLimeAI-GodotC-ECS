/// <summary>
/// Unit 生命/伤害相关事件定义
/// </summary>
public static partial class GameEventType
{
    public static partial class Unit
    {
        /// <summary>单位受到伤害</summary>
        public readonly record struct Damaged(
            IEntity Victim,
            float Amount,
            IEntity? Attacker = null,
            DamageType Type = DamageType.True,
            bool IsCritical = false);

        /// <summary>单位闪避成功（结果事件：DodgeProcessor -> UI 飘字）</summary>
        public readonly record struct Dodged(IEntity Victim, IEntity? Attacker = null);

        /// <summary>请求治疗（命令事件：外部 -> HealthComponent）</summary>
        public readonly record struct HealRequest(float Amount, HealSource Source = HealSource.Unknown);

        /// <summary>治疗已应用（结果事件：HealthComponent -> UI/统计）</summary>
        public readonly record struct HealApplied(
            IEntity Victim,
            float RequestedAmount,
            float ActualAmount,
            HealSource Source
        );

        /// <summary>
        /// 单位被击杀（建议使用 GlobalEventBus 广播）
        /// </summary>
        /// <remarks>
        /// <para>发送者：HealthComponent（HP<=0）</para>
        /// <para>监听者：DamageStatisticsSystem（击杀统计）、LifecycleComponent（通过 Victim 筛选）</para>
        /// </remarks>
        public readonly record struct Killed(
            IEntity? Victim,
            IEntity? Killer,
            DeathType DeathType = DeathType.Normal,
            DamageType DamageType = DamageType.True
        );
    }
}
