using Godot;

/// <summary>
/// 对象池 detach fallback 策略。
/// <para>默认不启用；仅用于后续验证对照或显式 fallback。</para>
/// </summary>
public static class DetachFallbackStrategy
{
    /// <summary>
    /// 当前默认策略不启用 detach fallback。
    /// </summary>
    public static bool IsEnabled(string poolName, Node node)
    {
        return false;
    }

    /// <summary>
    /// 显式从父节点脱离，并记录 fallback 状态。
    /// </summary>
    public static void Detach(Node node, string poolName, Vector2 parkingPosition)
    {
        if (node == null)
        {
            return;
        }

        ObjectPoolRuntimeStateStore.MarkDetachFallback(node, poolName, parkingPosition);
        node.GetParent()?.RemoveChild(node);
    }
}
