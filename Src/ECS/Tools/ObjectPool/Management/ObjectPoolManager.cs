using System;
using System.Collections.Generic;
using Godot;

// 采用混合命名空间策略：核心工具类放在全局命名空间，方便调用

/// <summary>
/// 全局对象池管理器
/// 提供静态访问接口，支持自动归还和全局池生命周期管理
/// </summary>
public static class ObjectPoolManager
{
    private static readonly Log _log = new(nameof(ObjectPoolManager));

    // 全局池字典，按 PoolName 索引，管理层只依赖非泛型 runtime interface。
    private static readonly Dictionary<string, IObjectPoolRuntime> _pools = [];

    // 非 Node 对象到池名称的映射（用于纯 C# 对象的静态归还）
    private static readonly Dictionary<object, string> _objectToPoolMap = [];

    // 线程锁对象
    private static readonly object _lock = new();

    /// <summary> 注册一个对象池到管理器（泛型版本） </summary>
    public static void Register<T>(ObjectPool<T> pool) where T : class
    {
        lock (_lock)
        {
            if (_pools.ContainsKey(pool.PoolName))
            {
                _log.Warn($"池名称 '{pool.PoolName}' 已存在。将覆盖旧池。");
            }
            _pools[pool.PoolName] = pool;
        }
    }

    /// <summary> 从管理器中注销一个对象池（泛型版本） </summary>
    public static void Unregister<T>(ObjectPool<T> pool) where T : class
    {
        lock (_lock)
        {
            _pools.Remove(pool.PoolName);
            // 注意：这里很难高效清理 _objectToPoolMap 中属于该池的对象，
            // 但通常 Unregister 只在销毁时发生，此时 Map 也会被清理。
        }

        ObjectPoolObservability.UnregisterMetadata(pool.PoolName);
    }

    /// <summary>
    /// 注册对象归属关系（供 ObjectPool 内部调用）
    /// </summary>
    public static void MapObject(object obj, string poolName)
    {
        // Node 对象使用 MetaData 存储，不需要字典映射
        if (obj is Node) return;

        lock (_lock)
        {
            _objectToPoolMap[obj] = poolName;
        }
    }

    /// <summary>
    /// 解除对象归属关系（供 ObjectPool 内部调用）
    /// </summary>
    public static void UnmapObject(object obj)
    {
        if (obj is Node) return;

        lock (_lock)
        {
            _objectToPoolMap.Remove(obj);
        }
    }

    /// <summary>
    /// 静态归还方法（核心功能）
    /// 自动查找对象所属的对象池并执行归还操作。
    /// 支持 Node 对象（通过 MetaData）和纯 C# 对象（通过内部映射）。
    /// </summary>
    /// <param name="instance">要归还的对象</param>
    public static void ReturnToPool(object instance)
    {
        if (instance == null) return;

        string? poolName = null;

        // 1. 尝试从 Node MetaData 获取
        if (instance is Node node)
        {
            if (node.HasMeta("ObjectPoolName"))
            {
                poolName = node.GetMeta("ObjectPoolName").AsString();
            }

            if (poolName == null)
            {
                _log.Warn($"Node {node.Name} 未找到 ObjectPoolName MetaData。将退回到 QueueFree。");
                node.QueueFree();
                return;
            }
        }
        // 2. 尝试从内部映射获取 (纯 C# 对象)
        else
        {
            lock (_lock)
            {
                poolName = _objectToPoolMap.GetValueOrDefault(instance);
            }

            if (poolName == null)
            {
                _log.Error($"实例 {instance} 未注册到 ObjectPoolManager，无法自动归还。请确保它是通过 Spawn/Get 获取的。");
                return;
            }
        }

        // 3. 查找池并通过 runtime interface 调用 Release
        IObjectPoolRuntime? runtimePool;
        lock (_lock)
        {
            _pools.TryGetValue(poolName, out runtimePool);
        }

        if (runtimePool != null)
        {
            if (runtimePool.ReleaseUntyped(instance))
            {
                return;
            }

            _log.Error($"池 '{poolName}' 拒绝归还对象 {instance}，期望类型 {runtimePool.ItemType.Name}。");
            return;
        }

        // 池不存在
        if (instance is Node n)
        {
            _log.Warn($"池 '{poolName}' 不存在。Node {n.Name} 将退回到 QueueFree。");
            n.QueueFree();
        }
        else
        {
            _log.Error($"池 '{poolName}' 不存在。对象 {instance} 无法归还。");
        }
    }

    /// <summary> 根据名称获取对象池实例（泛型版本，提供类型安全） </summary>
    public static ObjectPool<T>? GetPool<T>(string name) where T : class
    {
        lock (_lock)
        {
            if (_pools.TryGetValue(name, out var poolObj))
            {
                return poolObj as ObjectPool<T>;
            }
            return null;
        }
    }

    /// <summary>根据名称获取非泛型 runtime pool，供管理器、测试和诊断使用。</summary>
    public static IObjectPoolRuntime? GetRuntimePool(string name)
    {
        lock (_lock)
        {
            return _pools.GetValueOrDefault(name);
        }
    }

    /// <summary> 获取当前所有池的详细统计信息 </summary>
    public static Dictionary<string, PoolStats> GetAllStats()
    {
        lock (_lock)
        {
            var stats = new Dictionary<string, PoolStats>();
            foreach (var kvp in _pools)
            {
                stats[kvp.Key] = kvp.Value.GetStats();
            }
            return stats;
        }
    }

    /// <summary> 清理所有池中的闲置对象，保留每个池指定的最小数量 </summary>
    public static void CleanupAll(int retainCount)
    {
        lock (_lock)
        {
            foreach (var poolObj in _pools.Values)
            {
                poolObj.Cleanup(retainCount);
            }
        }
    }

    /// <summary>
    /// 彻底销毁所有池
    /// 在场景切换或游戏结束时调用，确保内存释放
    /// </summary>
    public static void DestroyAll()
    {
        lock (_lock)
        {
            foreach (var poolObj in _pools.Values)
            {
                poolObj.Clear();
            }
            _pools.Clear();
            _objectToPoolMap.Clear();
        }

        ObjectPoolObservability.Clear();
        ObjectPoolRuntimeStateStore.Clear();
        PoolParkingStrategy.Clear();
        _log.Info("所有对象池已销毁并清空。");
    }
}
