using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Runtime Data catalog，负责 stable key 到 typed key/id 的冻结解析。
/// </summary>
public sealed class DataCatalog
{
    private readonly Dictionary<string, IDataKey> _byStableKey;
    private readonly Dictionary<string, int> _ids;

    internal DataCatalog(IEnumerable<IDataKey> keys)
    {
        _byStableKey = new Dictionary<string, IDataKey>(StringComparer.Ordinal);
        _ids = new Dictionary<string, int>(StringComparer.Ordinal);

        int id = 0;
        foreach (var key in keys.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            if (_byStableKey.ContainsKey(key.Key))
            {
                continue;
            }

            _byStableKey.Add(key.Key, key);
            _ids.Add(key.Key, id++);
        }
    }

    public IReadOnlyCollection<IDataKey> Keys => _byStableKey.Values;

    public bool TryResolve(string stableKey, out IDataKey key)
    {
        return _byStableKey.TryGetValue(stableKey, out key!);
    }

    public bool TryResolve<T>(string stableKey, out DataKey<T> key)
    {
        if (_byStableKey.TryGetValue(stableKey, out var raw) && raw is DataKey<T> typed)
        {
            key = typed;
            return true;
        }

        key = null!;
        return false;
    }

    public int GetId(IDataKey key)
    {
        return _ids.TryGetValue(key.Key, out var id) ? id : -1;
    }
}

/// <summary>
/// Runtime Data catalog builder。
/// </summary>
public sealed class DataCatalogBuilder
{
    private readonly List<IDataKey> _keys = [];

    public DataCatalogBuilder Add(IDataKey key)
    {
        _keys.Add(key);
        return this;
    }

    public DataCatalog Build()
    {
        return new DataCatalog(_keys);
    }
}
