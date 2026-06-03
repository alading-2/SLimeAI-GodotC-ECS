using System;
using System.Collections.Generic;

/// <summary>
/// 对象池只读观测元数据。
/// </summary>
public readonly record struct ObjectPoolMetadata(
    string PoolName, // 对象池名称
    int InitialSize, // 初始预热数量
    int MaxSize // 池最大容量
);

/// <summary>
/// 对象池观测元数据注册表。
/// <para>
/// 只负责缓存对象池的容量配置，供 TestSystem 等运行时工具读取。
/// </para>
/// </summary>
public static class ObjectPoolObservability
{
    private static readonly Dictionary<string, ObjectPoolMetadata> MetadataByPoolName = new(StringComparer.Ordinal);
    private static readonly object Lock = new();

    /// <summary>
    /// 注册或覆盖对象池观测元数据。
    /// </summary>
    /// <param name="config">对象池配置。</param>
    public static void RegisterMetadata(ObjectPoolConfig config)
    {
        if (string.IsNullOrWhiteSpace(config.Name))
        {
            return;
        }

        var metadata = new ObjectPoolMetadata(
            config.Name, // 池名称
            config.InitialSize, // 初始数量
            config.MaxSize // 最大容量
        );

        lock (Lock)
        {
            MetadataByPoolName[config.Name] = metadata;
        }
    }

    /// <summary>
    /// 注销单个对象池的观测元数据。
    /// </summary>
    /// <param name="poolName">对象池名称。</param>
    public static void UnregisterMetadata(string poolName)
    {
        if (string.IsNullOrWhiteSpace(poolName))
        {
            return;
        }

        lock (Lock)
        {
            MetadataByPoolName.Remove(poolName);
        }
    }

    /// <summary>
    /// 获取所有对象池元数据快照。
    /// </summary>
    public static Dictionary<string, ObjectPoolMetadata> GetAllMetadata()
    {
        lock (Lock)
        {
            return new Dictionary<string, ObjectPoolMetadata>(MetadataByPoolName, StringComparer.Ordinal);
        }
    }

    /// <summary>
    /// 查找单个对象池元数据。
    /// </summary>
    /// <param name="poolName">对象池名称。</param>
    /// <param name="metadata">查找到的元数据。</param>
    public static bool TryGetMetadata(string poolName, out ObjectPoolMetadata metadata)
    {
        lock (Lock)
        {
            return MetadataByPoolName.TryGetValue(poolName, out metadata);
        }
    }

    /// <summary>
    /// 清空所有观测元数据。
    /// </summary>
    public static void Clear()
    {
        lock (Lock)
        {
            MetadataByPoolName.Clear();
        }
    }
}
