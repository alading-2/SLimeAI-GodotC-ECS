using System;

/// <summary>
/// 应用主阶段——四维状态坐标第一维：应用在哪个大阶段？
/// <para>从启动到关闭的宏观生命周期，与 Session/Overlay/Execution 同时存在。</para>
/// <para>改为 Flags 枚举，支持多阶段组合和预设。</para>
/// </summary>
[Flags]
public enum AppPhase : byte
{
    None         = 0,
    /// <summary>应用启动和框架引导阶段。</summary>
    Boot         = 1 << 0,
    /// <summary>前台界面阶段（菜单、配置、非局内界面）。</summary>
    FrontEnd     = 1 << 1,
    /// <summary>已进入单局会话流程。</summary>
    InSession    = 1 << 2,
    /// <summary>应用关闭流程。</summary>
    ShuttingDown = 1 << 3,

    // 预设组合
    /// <summary>所有阶段。</summary>
    All          = Boot | FrontEnd | InSession | ShuttingDown,
    /// <summary>游戏玩法阶段（局内）。</summary>
    GameplayPhases = InSession,
    /// <summary>菜单阶段（启动和前台）。</summary>
    MenuPhases   = Boot | FrontEnd,
}
