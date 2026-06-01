/// <summary>
/// Ability 执行流程事件定义
/// </summary>
public static partial class GameEventType
{
    public static partial class Ability
    {
        /// <summary>技能激活成功 (开始执行效果)</summary>
        public readonly record struct Activated(CastContext Context);

        /// <summary>技能效果执行完成</summary>
        public readonly record struct Executed(AbilityExecutedResult? Result);

        /// <summary>技能被取消 (如蓄力被打断)</summary>
        public readonly record struct Cancelled(string Reason);

    }
}
