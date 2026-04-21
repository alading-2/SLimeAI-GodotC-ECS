/// <summary>
/// 覆盖层阶段——四维状态坐标第三维：有没有 UI 覆盖层挡着？
/// <para>覆盖层会抢占交互焦点，通常伴随 ExecutionPhase 变为 Paused/Blocked。</para>
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
}
