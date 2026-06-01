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
        var poolNames = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var poolName in statsByPool.Keys)
        {
            poolNames.Add(poolName);
        }

        foreach (var poolName in metadataByPool.Keys)
        {
            poolNames.Add(poolName);
        }

        var snapshots = new List<ObjectPoolInfoSnapshot>(poolNames.Count);
        foreach (var poolName in poolNames)
        {
            statsByPool.TryGetValue(poolName, out var stats);
            var hasMetadata = metadataByPool.TryGetValue(poolName, out var metadata);
            snapshots.Add(new ObjectPoolInfoSnapshot(
                poolName, // 对象池名称
                stats, // 运行时统计
                hasMetadata, // 是否存在元数据
                hasMetadata ? metadata.InitialSize : -1, // 初始容量
                hasMetadata ? metadata.MaxSize : -1, // 最大容量
                BuildRiskHint(stats, hasMetadata, metadata) // 风险提示
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
    private static string BuildRiskHint(PoolStats stats, bool hasMetadata, ObjectPoolMetadata metadata)
    {
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
}
