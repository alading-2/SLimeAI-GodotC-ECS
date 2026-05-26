using Godot;

/// <summary>
/// 瞄准系统相关事件定义
/// 用于技能Point类型目标选择的异步流程
/// </summary>
public static partial class GameEventType
{
    public static class Targeting
    {
        // ================= 瞄准流程事件 =================

        /// <summary>
        /// 开始瞄准 - 请求显示瞄准指示器
        /// 发送者：ActiveSkillInputComponent (当Point技能被激活时)
        /// 接收者：TargetingManager
        /// </summary>
        public readonly record struct StartTargeting(CastContext Context);

        /// <summary>
        /// 瞄准确认 - 玩家按下确认键
        /// 发送者：TargetingIndicatorComponent (当玩家按A确认时)
        /// 接收者：TargetingManager -> AbilitySystem
        /// </summary>
        public readonly record struct TargetConfirmed(Vector2 TargetPosition);

        /// <summary>
        /// 瞄准取消 - 玩家按下取消键
        /// 发送者：TargetingIndicatorComponent (当玩家按B取消时)
        /// 接收者：TargetingManager
        /// </summary>
        public readonly record struct TargetCancelled();

        /// <summary>
        /// 瞄准结束 - 瞄准流程完成(确认或取消后)
        /// 发送者：TargetingManager
        /// 接收者：UI系统等需要响应瞄准状态变化的模块
        /// </summary>
        public readonly record struct TargetingEnded(bool WasConfirmed);

    }
}
