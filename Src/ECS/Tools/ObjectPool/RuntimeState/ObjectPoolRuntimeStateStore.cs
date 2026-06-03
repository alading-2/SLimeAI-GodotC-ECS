using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// 对象池运行时状态仓。
/// <para>按 Godot instance id 记录池化节点状态，供 Collision / Movement / ContactDamage 入口做业务 guard。</para>
/// </summary>
public static class ObjectPoolRuntimeStateStore
{
    private static readonly Dictionary<ulong, PoolRuntimeState> StateByInstanceId = new();
    private static readonly Dictionary<ulong, WeakReference<Node>> NodeByInstanceId = new();
    private static readonly object Lock = new();

    /// <summary>
    /// 当前物理帧编号。
    /// </summary>
    public static long CurrentPhysicsFrame => unchecked((long)Engine.GetPhysicsFrames());

    /// <summary>
    /// 初始化节点状态。
    /// </summary>
    public static void Register(Node node, string poolName)
    {
        if (!IsNodeValid(node))
        {
            return;
        }

        var position = ResolvePosition(node);
        var state = new PoolRuntimeState(
            poolName,
            node.Name,
            node.GetType().Name,
            IsInPool: false,
            CollisionLogicActive: true,
            CollisionReadyPhysicsFrame: CurrentPhysicsFrame,
            LastAcquirePhysicsFrame: -1,
            LastReleasePhysicsFrame: -1,
            ParkingPosition: Vector2.Zero,
            LastAcquirePosition: position,
            LastReleasePosition: Vector2.Zero,
            DetachFallbackEnabled: false);

        Set(node, state);
    }

    /// <summary>
    /// 标记节点已经回池并进入停车区。
    /// </summary>
    public static void MarkReleased(Node node, string poolName, Vector2 parkingPosition, bool detachFallbackEnabled = false)
    {
        if (!IsNodeValid(node))
        {
            return;
        }

        var previous = TryGet(node, out var existing) ? existing : default;
        var state = new PoolRuntimeState(
            poolName,
            node.Name,
            node.GetType().Name,
            IsInPool: true,
            CollisionLogicActive: false,
            CollisionReadyPhysicsFrame: long.MaxValue,
            LastAcquirePhysicsFrame: previous.LastAcquirePhysicsFrame,
            LastReleasePhysicsFrame: CurrentPhysicsFrame,
            ParkingPosition: parkingPosition,
            LastAcquirePosition: previous.LastAcquirePosition,
            LastReleasePosition: ResolvePosition(node),
            DetachFallbackEnabled: detachFallbackEnabled);

        Set(node, state);
    }

    /// <summary>
    /// 标记节点已从池取出，但尚未进入业务激活态。
    /// </summary>
    public static void MarkAcquired(Node node, string poolName)
    {
        if (!IsNodeValid(node))
        {
            return;
        }

        var previous = TryGet(node, out var existing) ? existing : default;
        var state = new PoolRuntimeState(
            poolName,
            node.Name,
            node.GetType().Name,
            IsInPool: false,
            CollisionLogicActive: false,
            CollisionReadyPhysicsFrame: long.MaxValue,
            LastAcquirePhysicsFrame: CurrentPhysicsFrame,
            LastReleasePhysicsFrame: previous.LastReleasePhysicsFrame,
            ParkingPosition: previous.ParkingPosition,
            LastAcquirePosition: ResolvePosition(node),
            LastReleasePosition: previous.LastReleasePosition,
            DetachFallbackEnabled: previous.DetachFallbackEnabled);

        Set(node, state);
    }

    /// <summary>
    /// 标记节点完成业务激活，下一物理帧后才允许处理业务碰撞。
    /// </summary>
    public static void MarkActivated(Node node, string poolName)
    {
        if (!IsNodeValid(node))
        {
            return;
        }

        var currentFrame = CurrentPhysicsFrame;
        var previous = TryGet(node, out var existing) ? existing : default;
        var state = new PoolRuntimeState(
            poolName,
            node.Name,
            node.GetType().Name,
            IsInPool: false,
            CollisionLogicActive: true,
            CollisionReadyPhysicsFrame: currentFrame + 1,
            LastAcquirePhysicsFrame: currentFrame,
            LastReleasePhysicsFrame: previous.LastReleasePhysicsFrame,
            ParkingPosition: previous.ParkingPosition,
            LastAcquirePosition: ResolvePosition(node),
            LastReleasePosition: previous.LastReleasePosition,
            DetachFallbackEnabled: previous.DetachFallbackEnabled);

        Set(node, state);
    }

    /// <summary>
    /// 标记节点进入显式 detach fallback 路径。
    /// </summary>
    public static void MarkDetachFallback(Node node, string poolName, Vector2 parkingPosition)
    {
        MarkReleased(node, poolName, parkingPosition, detachFallbackEnabled: true);
    }

    /// <summary>
    /// 查询节点运行时状态。
    /// </summary>
    public static bool TryGet(Node node, out PoolRuntimeState state)
    {
        state = default;
        if (!IsNodeValid(node))
        {
            return false;
        }

        lock (Lock)
        {
            return StateByInstanceId.TryGetValue(node.GetInstanceId(), out state);
        }
    }

    /// <summary>
    /// 查询节点是否处于池中。
    /// </summary>
    public static bool IsInPool(Node node)
    {
        return TryGet(node, out var state) && state.IsInPool;
    }

    /// <summary>
    /// 获取所有节点状态快照。
    /// </summary>
    public static List<PoolNodeStateSnapshot> GetAllNodeStateSnapshots()
    {
        lock (Lock)
        {
            var snapshots = new List<PoolNodeStateSnapshot>(StateByInstanceId.Count);
            foreach (var kv in StateByInstanceId)
            {
                Node? node = null;
                if (NodeByInstanceId.TryGetValue(kv.Key, out var weakNode)
                    && weakNode.TryGetTarget(out var target)
                    && IsNodeValid(target))
                {
                    node = target;
                }

                snapshots.Add(ToSnapshot(kv.Key, node, kv.Value));
            }

            return snapshots;
        }
    }

    /// <summary>
    /// 移除单个节点状态。
    /// </summary>
    public static void Remove(Node node)
    {
        if (node == null)
        {
            return;
        }

        lock (Lock)
        {
            var id = node.GetInstanceId();
            StateByInstanceId.Remove(id);
            NodeByInstanceId.Remove(id);
        }
    }

    /// <summary>
    /// 清空所有运行时状态。
    /// </summary>
    public static void Clear()
    {
        lock (Lock)
        {
            StateByInstanceId.Clear();
            NodeByInstanceId.Clear();
        }
    }

    private static void Set(Node node, PoolRuntimeState state)
    {
        lock (Lock)
        {
            var id = node.GetInstanceId();
            StateByInstanceId[id] = state;
            NodeByInstanceId[id] = new WeakReference<Node>(node);
        }
    }

    private static PoolNodeStateSnapshot ToSnapshot(ulong instanceId, Node? node, PoolRuntimeState state)
    {
        bool isInPoolMeta = node != null
            && node.HasMeta("InPool")
            && node.GetMeta("InPool").AsBool();

        return new PoolNodeStateSnapshot(
            instanceId,
            state.PoolName,
            state.NodeName,
            state.NodeType,
            node?.IsInsideTree() ?? false,
            isInPoolMeta,
            state.IsInPool,
            state.CollisionLogicActive,
            state.CollisionReadyPhysicsFrame,
            state.LastAcquirePhysicsFrame,
            state.LastReleasePhysicsFrame,
            state.ParkingPosition,
            state.LastAcquirePosition,
            state.LastReleasePosition,
            state.DetachFallbackEnabled);
    }

    private static Vector2 ResolvePosition(Node node)
    {
        return node is Node2D node2D ? node2D.GlobalPosition : Vector2.Zero;
    }

    private static bool IsNodeValid(Node? node)
    {
        return node != null
            && GodotObject.IsInstanceValid(node)
            && !node.IsQueuedForDeletion();
    }
}
