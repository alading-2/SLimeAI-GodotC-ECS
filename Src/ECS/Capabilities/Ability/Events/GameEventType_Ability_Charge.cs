/// <summary>
/// Ability 充能相关事件定义
/// </summary>
public static partial class GameEventType
{
    public static partial class Ability
    {
        /// <summary>充能恢复</summary>
        public readonly record struct ChargeRestored(int CurrentCharges, int MaxCharges);

        /// <summary>
        /// 使用技能消耗充能事件。
        /// 发送者：AbilitySystem (技能激活时)
        /// 接收者：ChargeComponent
        /// </summary>
        /// <summary>消耗充能事件数据</summary>
        public readonly record struct ConsumeCharge(EventContext Context);

        /// <summary>
        /// 请求增加充能事件。
        /// 发送者：道具系统、Buff 等外部逻辑
        /// 接收者：ChargeComponent
        /// </summary>
        /// <summary>增加充能事件数据</summary>
        public readonly record struct AddCharge(int Amount);
    }
}
