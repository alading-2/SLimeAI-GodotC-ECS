using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// 对象池停车区策略。
/// <para>按池名和节点实例分配远离战场的分散坐标，避免所有碰撞体堆叠到同一点。</para>
/// </summary>
public static class PoolParkingStrategy
{
    private static readonly Vector2 BaseParkingPosition = new(1000000, 1000000);
    private const float ParkingSpacing = 4096f;
    private const int ColumnsPerPool = 64;
    private static readonly Dictionary<ulong, int> SlotByInstanceId = new();
    private static readonly object Lock = new();

    /// <summary>
    /// 为节点分配稳定停车坐标。
    /// </summary>
    public static Vector2 Allocate(Node node, string poolName)
    {
        if (node == null)
        {
            return BaseParkingPosition;
        }

        int slot;
        lock (Lock)
        {
            var id = node.GetInstanceId();
            if (!SlotByInstanceId.TryGetValue(id, out slot))
            {
                slot = SlotByInstanceId.Count;
                SlotByInstanceId[id] = slot;
            }
        }

        int poolOffset = Math.Abs(StringComparer.Ordinal.GetHashCode(poolName)) % 1024;
        int column = slot % ColumnsPerPool;
        int row = slot / ColumnsPerPool;
        return BaseParkingPosition + new Vector2(
            (poolOffset * ColumnsPerPool + column) * ParkingSpacing,
            row * ParkingSpacing);
    }

    /// <summary>
    /// 将节点移动到停车区。
    /// </summary>
    public static void Park(Node node, Vector2 parkingPosition)
    {
        if (node is not Node2D node2D)
        {
            return;
        }

        if (node2D is CharacterBody2D body)
        {
            body.Velocity = Vector2.Zero;
        }

        node2D.GlobalPosition = parkingPosition;
        if (node2D.IsInsideTree())
        {
            node2D.ForceUpdateTransform();
        }
    }

    /// <summary>
    /// 移除节点停车 slot，用于 discard / clear。
    /// </summary>
    public static void Remove(Node node)
    {
        if (node == null)
        {
            return;
        }

        lock (Lock)
        {
            SlotByInstanceId.Remove(node.GetInstanceId());
        }
    }

    /// <summary>
    /// 清空停车 slot。
    /// </summary>
    public static void Clear()
    {
        lock (Lock)
        {
            SlotByInstanceId.Clear();
        }
    }
}
