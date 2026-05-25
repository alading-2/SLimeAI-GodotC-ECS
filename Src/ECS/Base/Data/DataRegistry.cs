using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 数据注册表 - 管理所有 DataMeta（含运行时约束 + 展示字段）
/// </summary>
public static class DataRegistry
{
    private static readonly Log _log = new(nameof(DataRegistry));

    private static readonly Dictionary<string, DataMeta> _metaRegistry = new();
    private static readonly Dictionary<string, IDataKey> _keyRegistry = new();
    private static DataCatalog? _catalog;

    /// <summary>
    /// Category → DataMeta[] 缓存（懒构建，首次查询时从 _metaRegistry 中筛选并缓存）
    /// </summary>
    private static readonly Dictionary<Enum, DataMeta[]> _categoryCache = new();

    // --- 注册接口 ---

    /// <summary>
    /// 注册 typed key，返回同一实例（便于静态字段直接赋值）。
    /// </summary>
    public static DataKey<T> Register<T>(DataMeta meta)
    {
        var key = meta is DataKey<T> typed ? typed : new DataKey<T>(meta);
        RegisterUntyped(key);
        return key;
    }

    /// <summary>
    /// 注册元数据。仅用于迁移兼容；内部会转换为 typed key。
    /// 新代码应使用 Register<T>(DataMeta) typed 重载。
    /// </summary>
    public static DataMeta Register(DataMeta meta)
    {
        var keyType = typeof(DataKey<>).MakeGenericType(meta.Type);
        var typedKey = (IDataKey)Activator.CreateInstance(keyType, meta)!;
        RegisterUntyped(typedKey);
        return (DataMeta)typedKey;
    }

    private static void RegisterUntyped(IDataKey key)
    {
        _metaRegistry[key.Key] = (DataMeta)key;
        _keyRegistry[key.Key] = key;
        _catalog = null;

        // 注册时清除对应 Category 缓存，下次查询时重新构建
        if (key.Category != null)
            _categoryCache.Remove(key.Category);
    }

    // === 公共查询接口 ===

    /// <summary>
    /// 获取元数据（未注册返回 null，走快速路径）
    /// </summary>
    public static DataMeta? GetMeta(string key)
    {
        return _metaRegistry.GetValueOrDefault(key);
    }

    /// <summary>
    /// 通过 stable key 解析 typed key。
    /// </summary>
    public static bool TryResolve(string stableKey, out IDataKey key)
    {
        return _keyRegistry.TryGetValue(stableKey, out key!);
    }

    /// <summary>
    /// 通过 stable key 解析指定类型 typed key。
    /// </summary>
    public static bool TryResolve<T>(string stableKey, out DataKey<T> key)
    {
        if (_keyRegistry.TryGetValue(stableKey, out var raw) && raw is DataKey<T> typed)
        {
            key = typed;
            return true;
        }

        key = null!;
        return false;
    }

    /// <summary>
    /// 当前 frozen catalog。新注册 key 后会懒刷新。
    /// </summary>
    public static DataCatalog Catalog => _catalog ??= new DataCatalog(_keyRegistry.Values);

    /// <summary>
    /// 检查是否为计算数据
    /// </summary>
    public static bool IsComputed(string key)
    {
        return GetMeta(key)?.IsComputed ?? false;
    }

    /// <summary>
    /// 检查数据是否支持修改器
    /// </summary>
    public static bool SupportModifiers(string key)
    {
        return GetMeta(key)?.SupportModifiers ?? false;
    }

    /// <summary>
    /// 获取依赖指定 baseKey 的所有计算键（用于 MarkDirty 级联失效）
    /// </summary>
    public static IEnumerable<string> GetDependentComputedKeys(string baseKey)
    {
        return _metaRegistry.Values
            .Where(m => m.IsComputed && m.Dependencies != null && m.Dependencies.Contains(baseKey))
            .Select(m => m.Key);
    }

    /// <summary>
    /// 获取指定分类的所有元数据（通过 DataMeta.Category 筛选）
    /// </summary>
    public static IEnumerable<DataMeta> GetMetaByCategory(Enum category)
    {
        return _metaRegistry.Values.Where(m => Equals(m.Category, category));
    }

    /// <summary>
    /// 获取指定分类的所有元数据（缓存版本，返回数组引用，禁止修改）
    /// <para>首次查询时从注册表筛选并缓存，后续直接返回。适合高频/批量场景。</para>
    /// </summary>
    public static DataMeta[] GetCachedMetaByCategory(Enum category)
    {
        if (!_categoryCache.TryGetValue(category, out var cached))
        {
            cached = _metaRegistry.Values.Where(m => Equals(m.Category, category)).ToArray();
            _categoryCache[category] = cached;
        }
        return cached;
    }

    /// <summary>
    /// 获取所有已注册的数据键
    /// </summary>
    public static IEnumerable<string> GetAllKeys()
    {
        return _metaRegistry.Keys;
    }

    /// <summary>
    /// 获取所有 typed key。
    /// </summary>
    public static IEnumerable<IDataKey> GetAllDataKeys()
    {
        return _keyRegistry.Values;
    }

    /// <summary>
    /// 获取所有计算数据键
    /// </summary>
    public static IEnumerable<string> GetAllComputedKeys()
    {
        return _metaRegistry.Values.Where(m => m.IsComputed).Select(m => m.Key);
    }
}
