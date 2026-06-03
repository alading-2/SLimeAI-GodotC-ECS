using Godot;

/// <summary>
/// 池化节点业务碰撞 guard。
/// <para>Godot 原生碰撞信号仍可能到达，但业务入口必须先通过该 guard。</para>
/// </summary>
public static class CollisionLogicGuard
{
    /// <summary>
    /// 使用当前物理帧判断节点是否允许处理业务碰撞。
    /// </summary>
    public static bool CanProcessCollision(Node? self)
    {
        return CanProcessCollision(self, ObjectPoolRuntimeStateStore.CurrentPhysicsFrame);
    }

    /// <summary>
    /// 使用指定物理帧判断节点是否允许处理业务碰撞。
    /// </summary>
    public static bool CanProcessCollision(Node? self, long currentPhysicsFrame)
    {
        if (!IsNodeValid(self))
        {
            return false;
        }

        if (!ObjectPoolRuntimeStateStore.TryGet(self!, out var state))
        {
            return true;
        }

        if (state.IsInPool || !state.CollisionLogicActive)
        {
            return false;
        }

        return currentPhysicsFrame >= state.CollisionReadyPhysicsFrame;
    }

    /// <summary>
    /// 判断一次 self/target 业务碰撞是否允许继续处理。
    /// </summary>
    public static bool CanProcessCollision(Node? self, Node? target)
    {
        var currentFrame = ObjectPoolRuntimeStateStore.CurrentPhysicsFrame;
        return CanProcessCollision(self, target, currentFrame);
    }

    /// <summary>
    /// 判断一次 self/target 业务碰撞是否允许继续处理。
    /// </summary>
    public static bool CanProcessCollision(Node? self, Node? target, long currentPhysicsFrame)
    {
        return CanProcessCollision(self, currentPhysicsFrame)
            && CanProcessCollision(target, currentPhysicsFrame);
    }

    /// <summary>
    /// 查询节点是否已回池。
    /// </summary>
    public static bool IsInPool(Node? node)
    {
        return IsNodeValid(node) && ObjectPoolRuntimeStateStore.IsInPool(node!);
    }

    private static bool IsNodeValid(Node? node)
    {
        return node != null
            && GodotObject.IsInstanceValid(node)
            && !node.IsQueuedForDeletion();
    }
}
