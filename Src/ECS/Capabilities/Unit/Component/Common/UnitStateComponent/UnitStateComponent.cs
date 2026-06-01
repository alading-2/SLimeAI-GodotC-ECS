using Godot;

/// <summary>
/// 单位状态组件 - 空壳组件
///
/// 状态标记（IsDead, IsInvulnerable 等）已迁移至 Data 系统。
/// 通过 Data.Get/Set 直接访问：
///   - GeneratedDataKey.IsDead
///   - GeneratedDataKey.IsInvulnerable
///   - GeneratedDataKey.IsImmune
///   - GeneratedDataKey.IsStunned
///   - GeneratedDataKey.IsSilenced
///   - GeneratedDataKey.IsInvisible
///
/// 此组件保留用于未来扩展（如限时状态计时器管理）。
/// </summary>
public partial class UnitStateComponent : Node, IComponent
{
    public void OnComponentRegistered(Node entity) { }

    public void OnComponentUnregistered() { }
}
