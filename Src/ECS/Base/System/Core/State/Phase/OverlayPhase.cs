using System;

/// <summary>
/// 覆盖层阶段——四维状态坐标第三维：有没有 UI 覆盖层挡着？
/// <para>覆盖层会抢占交互焦点，通常伴随 ExecutionPhase 变为 Paused/Blocked。</para>
/// <para>改为 Flags 枚举，支持多覆盖层组合和预设。</para>
/// </summary>
[Flags]
public enum OverlayPhase : byte
{
    None      = 0,
    /// <summary>暂停菜单覆盖层。</summary>
    PauseMenu = 1 << 0,
    /// <summary>模态窗口覆盖层。</summary>
    ModalUi   = 1 << 1,
    /// <summary>过场覆盖层。</summary>
    Cutscene  = 1 << 2,

    // 预设组合
    /// <summary>所有覆盖层。</summary>
    All       = PauseMenu | ModalUi | Cutscene,
    /// <summary>交互覆盖层（暂停菜单和模态窗口）。</summary>
    InteractiveOverlays = PauseMenu | ModalUi,
}
