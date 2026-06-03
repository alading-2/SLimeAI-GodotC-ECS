using System;
using System.Collections.Generic;

/// <summary>
/// 单个对象池的测试面板视图数据。
/// </summary>
internal readonly record struct ObjectPoolInfoSnapshot(
    string PoolName, // 对象池名称
    PoolStats Stats, // 运行时统计
    bool HasMetadata, // 是否存在容量元数据
    int InitialSize, // 初始预热数量
    int MaxSize, // 最大容量
    int NodeStateCount, // 当前记录的节点状态数量
    int PooledNodeCount, // 当前回池节点数量
    int EmbargoedNodeCount, // 当前处于激活首帧保护的节点数量
    int FallbackNodeCount, // 当前使用 detach fallback 的节点数量
    IReadOnlyList<PoolNodeStateSnapshot> NodeStates, // 节点级运行时状态快照
    string RiskHint // 风险提示文案
);

/// <summary>
/// 对象池信息查询服务。
/// <para>
/// 负责把运行时统计与容量元数据合并成 TestSystem 可直接展示的快照。
/// </para>
/// </summary>
internal sealed class ObjectPoolInfoService
{
    /// <summary>
    /// 获取全部对象池快照。
    /// </summary>
    public IReadOnlyList<ObjectPoolInfoSnapshot> GetSnapshots()
    {
        var statsByPool = ObjectPoolManager.GetAllStats();
        var metadataByPool = ObjectPoolObservability.GetAllMetadata();
        var nodeStatesByPool = BuildNodeStatesByPool();
        var poolNames = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var poolName in statsByPool.Keys)
        {
            poolNames.Add(poolName);
        }

        foreach (var poolName in metadataByPool.Keys)
        {
            poolNames.Add(poolName);
        }

        foreach (var poolName in nodeStatesByPool.Keys)
        {
            poolNames.Add(poolName);
        }

        var snapshots = new List<ObjectPoolInfoSnapshot>(poolNames.Count);
        foreach (var poolName in poolNames)
        {
            statsByPool.TryGetValue(poolName, out var stats);
            var hasMetadata = metadataByPool.TryGetValue(poolName, out var metadata);
            nodeStatesByPool.TryGetValue(poolName, out var nodeStates);
            nodeStates ??= Array.Empty<PoolNodeStateSnapshot>();
            var pooledNodeCount = CountWhere(nodeStates, static state => state.IsInPool);
            var embargoedNodeCount = CountWhere(nodeStates, IsEmbargoed);
            var fallbackNodeCount = CountWhere(nodeStates, static state => state.DetachFallbackEnabled);
            snapshots.Add(new ObjectPoolInfoSnapshot(
                poolName, // 对象池名称
                stats, // 运行时统计
                hasMetadata, // 是否存在元数据
                hasMetadata ? metadata.InitialSize : -1, // 初始容量
                hasMetadata ? metadata.MaxSize : -1, // 最大容量
                nodeStates.Count, // 节点状态数量
                pooledNodeCount, // 已回池节点数量
                embargoedNodeCount, // 激活首帧保护节点数量
                fallbackNodeCount, // fallback 节点数量
                nodeStates, // 节点状态快照
                BuildRiskHint(stats, hasMetadata, metadata, fallbackNodeCount) // 风险提示
            ));
        }

        return snapshots;
    }

    /// <summary>
    /// 构建对象池风险提示。
    /// </summary>
    /// <param name="stats">运行时统计。</param>
    /// <param name="hasMetadata">是否存在元数据。</param>
    /// <param name="metadata">容量元数据。</param>
    private static string BuildRiskHint(PoolStats stats, bool hasMetadata, ObjectPoolMetadata metadata, int fallbackNodeCount)
    {
        if (fallbackNodeCount > 0)
        {
            return "存在 detach fallback 节点";
        }

        if (stats.TotalDiscarded > 0)
        {
            return "发生过容量丢弃";
        }

        if (!hasMetadata)
        {
            return "缺少容量元数据";
        }

        if (metadata.MaxSize > 0 && stats.ActiveCount >= metadata.MaxSize)
        {
            return "活跃数达到容量上限";
        }

        if (metadata.MaxSize > 0 && stats.ActiveCount + stats.Count >= metadata.MaxSize)
        {
            return "池容量接近上限";
        }

        return "正常";
    }

    /// <summary>
    /// 按对象池名称分组节点状态。
    /// </summary>
    private static Dictionary<string, IReadOnlyList<PoolNodeStateSnapshot>> BuildNodeStatesByPool()
    {
        var grouped = new Dictionary<string, List<PoolNodeStateSnapshot>>(StringComparer.Ordinal);
        foreach (var state in ObjectPoolRuntimeStateStore.GetAllNodeStateSnapshots())
        {
            if (!grouped.TryGetValue(state.PoolName, out var list))
            {
                list = new List<PoolNodeStateSnapshot>();
                grouped[state.PoolName] = list;
            }

            list.Add(state);
        }

        var result = new Dictionary<string, IReadOnlyList<PoolNodeStateSnapshot>>(StringComparer.Ordinal);
        foreach (var kv in grouped)
        {
            kv.Value.Sort(static (a, b) => string.CompareOrdinal(a.NodeName, b.NodeName));
            result[kv.Key] = kv.Value;
        }

        return result;
    }

    private static int CountWhere(IReadOnlyList<PoolNodeStateSnapshot> states, Predicate<PoolNodeStateSnapshot> predicate)
    {
        var count = 0;
        foreach (var state in states)
        {
            if (predicate(state))
            {
                count++;
            }
        }

        return count;
    }

    private static bool IsEmbargoed(PoolNodeStateSnapshot state)
    {
        return !state.IsInPool
            && state.CollisionLogicActive
            && ObjectPoolRuntimeStateStore.CurrentPhysicsFrame < state.CollisionReadyPhysicsFrame;
    }
}
