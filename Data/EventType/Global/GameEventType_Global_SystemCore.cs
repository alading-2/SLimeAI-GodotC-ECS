/// <summary>
/// Global 项目状态相关事件定义。
/// </summary>
public static partial class GameEventType
{
    public static partial class Global
    {
        /// <summary>项目状态切换前。</summary>
        public const string ProjectStateChanging = "global:project_state:changing";

        /// <summary>项目状态已切换。</summary>
        public const string ProjectStateChanged = "global:project_state:changed";

        /// <summary>项目状态切换完成。</summary>
        public const string ProjectStateChangedCompleted = "global:project_state:changed_completed";

        /// <summary>项目状态切换事件数据。</summary>
        public readonly record struct ProjectStateTransitionEventData(
            ProjectStateSnapshot Previous,
            ProjectStateSnapshot Current);
    }
}
