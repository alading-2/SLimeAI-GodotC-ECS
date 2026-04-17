/// <summary>
/// 系统生命周期域。
/// </summary>
public enum SystemLifetime
{
    /// <summary>跨流程常驻系统。</summary>
    Persistent,
    /// <summary>局内主玩法系统。</summary>
    Gameplay,
    /// <summary>覆盖层/菜单相关系统。</summary>
    Overlay,
    /// <summary>调试系统。</summary>
    Debug,
    /// <summary>测试系统。</summary>
    Test,
}
