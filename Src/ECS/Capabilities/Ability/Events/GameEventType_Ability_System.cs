/// <summary>
/// Ability 系统/通用事件定义
/// </summary>
public static partial class GameEventType
{
    public static partial class Ability
    {
        /// <summary>技能被添加到单位</summary>
        public readonly record struct Added(AbilityEntity Ability, IEntity Owner);

        /// <summary>技能被移除</summary>
        public readonly record struct Removed(string abilityName, string abilityId, IEntity Owner);

        /// <summary>技能升级</summary>
        public readonly record struct LevelUp(AbilityEntity? Ability, int OldLevel, int NewLevel);

        /// <summary>
        /// 请求检查技能是否可用。
        /// 响应者：所有资源/限制类组件 (CooldownComponent是否冷却完成, ChargeComponent是否充能足够, CostComponent是否消耗得起 等)
        /// </summary>
        /// <summary>检查可用性事件数据</summary>
        public readonly record struct CheckCanUse(EventContext Context);

        /// <summary>
        /// 尝试激活技能。
        /// 发送者：TriggerComponent (当满足触发条件时，如按下按键或周期已到)
        /// 接收者：AbilitySystem (执行具体激活逻辑，如目标选择)
        /// </summary>
        /// <summary>
        /// 尝试激活事件数据
        /// Context: 施法上下文（包含 ResponseContext，用于回传 TriggerResult）
        /// </summary>
        public readonly record struct TryTrigger(CastContext Context);
    }
}
