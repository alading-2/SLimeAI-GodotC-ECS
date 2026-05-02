using Godot;

/// <summary>
/// 拾取组件占位实现。
/// <para>
/// 当前拾取玩法尚未恢复；本组件只保证旧场景引用可实例化，避免 Godot C# 加载阶段报脚本类缺失。
/// </para>
/// </summary>
public partial class PickupComponent : Area2D, IComponent
{
    /// <summary>
    /// 注册时禁用自身物理监控，避免占位组件产生运行时碰撞语义。
    /// </summary>
    public void OnComponentRegistered(Node entity)
    {
        Monitoring = false;
        Monitorable = false;
    }

    /// <summary>
    /// 注销时继续保持禁用状态，兼容对象池复用。
    /// </summary>
    public void OnComponentUnregistered()
    {
        SetDeferred(Area2D.PropertyName.Monitoring, false);
        SetDeferred(Area2D.PropertyName.Monitorable, false);
    }
}
