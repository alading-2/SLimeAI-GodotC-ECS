/// <summary>
/// 覆盖层阶段。
/// </summary>
public enum OverlayPhase
{
    /// <summary>无覆盖层。</summary>
    None,
    /// <summary>暂停菜单覆盖层。</summary>
    PauseMenu,
    /// <summary>模态窗口覆盖层。</summary>
    ModalUi,
    /// <summary>过场覆盖层。</summary>
    Cutscene,
    /// <summary>调试覆盖层。</summary>
    DebugOverlay,
}
