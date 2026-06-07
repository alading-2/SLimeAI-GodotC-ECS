using Godot;

/// <summary>
/// 对象池 Node 基础生命周期策略。
/// <para>只负责 Meta、显隐、处理模式、父节点挂载和销毁，不处理业务碰撞语义。</para>
/// </summary>
public static class PoolNodeLifecycleStrategy
{
    /// <summary>
    /// 初始化新创建节点的对象池元信息。
    /// </summary>
    public static void InitializeNode(Node node, string poolName)
    {
        node.SetMeta("ObjectPoolName", poolName);
        node.SetMeta("InPool", false);
        ObjectPoolRuntimeStateStore.Register(node, poolName);
    }

    /// <summary>
    /// 确保节点挂载到对象池父节点。
    /// </summary>
    public static void EnsureParent(Node node, string poolName)
    {
        if (node.GetParent() != null || string.IsNullOrEmpty(poolName))
        {
            return;
        }

        var parent = RuntimeMountService.Current.GetExisting(RuntimeMountIds.Pool(poolName));
        parent?.AddChild(node);
    }

    /// <summary>
    /// 标记节点已回池。
    /// </summary>
    public static void MarkInPool(Node node)
    {
        node.SetMeta("InPool", true);
    }

    /// <summary>
    /// 标记节点已出池。
    /// </summary>
    public static void MarkAcquired(Node node, string poolName)
    {
        node.SetMeta("InPool", false);
        ObjectPoolRuntimeStateStore.MarkAcquired(node, poolName);
    }

    /// <summary>
    /// 判断节点是否已在池中。
    /// </summary>
    public static bool IsMarkedInPool(Node node)
    {
        return node.HasMeta("InPool") && node.GetMeta("InPool").AsBool();
    }

    /// <summary>
    /// 应用回池基础状态。
    /// </summary>
    public static void ApplyInactive(Node node)
    {
        node.ProcessMode = Node.ProcessModeEnum.Disabled;
        if (node is CanvasItem item)
        {
            item.Visible = false;
        }
    }

    /// <summary>
    /// 应用激活基础状态。
    /// </summary>
    public static void ApplyActive(Node node)
    {
        node.ProcessMode = Node.ProcessModeEnum.Inherit;
        if (node is CanvasItem item)
        {
            item.Visible = true;
        }

        if (node is Node2D node2D && node2D.IsInsideTree())
        {
            node2D.ForceUpdateTransform();
        }
    }

    /// <summary>
    /// 销毁节点并清理对象池运行时状态。
    /// </summary>
    public static void Discard(Node node)
    {
        ObjectPoolRuntimeStateStore.Remove(node);
        PoolParkingStrategy.Remove(node);
        if (node.GetParent() == null)
        {
            node.Free();
            return;
        }

        node.QueueFree();
    }
}
