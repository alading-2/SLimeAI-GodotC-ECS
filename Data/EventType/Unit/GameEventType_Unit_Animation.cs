/// <summary>
/// Unit 动画相关事件定义
/// </summary>
public static partial class GameEventType
{
    public static partial class Unit
    {
        /// <summary>动画播放完毕（结果事件：UnitAnimationComponent -> 其他组件）</summary>
        public readonly record struct AnimationFinished(string AnimName);

        /// <summary>请求停止当前动画立即回 idle（命令事件：外部 -> UnitAnimationComponent）</summary>
        public readonly record struct StopAnimationRequested();

        /// <summary>请求播放动画（命令事件：外部 -> UnitAnimationComponent）</summary>
        public readonly record struct PlayAnimationRequested(
            string AnimName,
            bool ForceRestart = false,
            float Duration = -1f
        );
    }
}
