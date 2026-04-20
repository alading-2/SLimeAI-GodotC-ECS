/// <summary>
/// 应用主阶段。
/// </summary>
public enum AppPhase
{
    /// <summary>应用启动和框架引导阶段。</summary>
    Boot,
    /// <summary>前台界面阶段（菜单、配置、非局内界面）。</summary>
    FrontEnd,
    /// <summary>已进入单局会话流程。</summary>
    InSession,
    /// <summary>应用关闭流程。</summary>
    ShuttingDown,
}
