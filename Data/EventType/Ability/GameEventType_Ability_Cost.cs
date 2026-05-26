/// <summary>
/// Ability 消耗相关事件定义
/// </summary>
public static partial class GameEventType
{
    public static partial class Ability
    {
        /// <summary>
        /// 请求消耗成本 (魔法/能量/生命值等)。
        /// 发送者：AbilitySystem (技能激活时)
        /// 接收者：CostComponent
        /// </summary>
        /// <summary>消耗成本请求事件数据</summary>
        public readonly record struct ConsumeCost(EventContext Context);

        /// <summary>
        /// 成本消耗完成事件 (供 UI 监听)。
        /// 发送者：CostComponent
        /// 接收者：UI、统计系统等
        /// </summary>
        /// <summary>成本消耗完成事件数据</summary>
        public readonly record struct CostConsumed(AbilityCostType CostType, float Amount);
    }
}
