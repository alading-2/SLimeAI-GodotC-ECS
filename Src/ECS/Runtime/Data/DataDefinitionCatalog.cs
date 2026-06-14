using System;
using System.Collections.Generic;

/// <summary>
/// Data 字段定义运行时索引。
/// </summary>
public sealed class DataDefinitionCatalog
{
    /// <summary>stable key → DataDefinition 的运行时索引。</summary>
    private readonly Dictionary<string, DataDefinition> _definitions = new(StringComparer.Ordinal);
    /// <summary>computed 字段的反向依赖索引：被依赖 key → 依赖它的 computed key 列表。</summary>
    private readonly Dictionary<string, List<string>> _dependentComputedKeys = new(StringComparer.Ordinal);
    /// <summary>computed resolver 注册表，校验时绑定。</summary>
    private DataComputeRegistry? _computeRegistry;
    /// <summary>索引构建完成后冻结，禁止继续注册。</summary>
    private bool _isFrozen;

    /// <summary>
    /// 绑定 computed resolver 注册表。
    /// </summary>
    /// <param name="computeRegistry">computed resolver 注册表。</param>
    public void BindComputeRegistry(DataComputeRegistry computeRegistry)
    {
        ArgumentNullException.ThrowIfNull(computeRegistry);
        _computeRegistry = computeRegistry; // computed resolver 注册表
    }

    /// <summary>
    /// 获取绑定的 computed resolver 注册表。
    /// </summary>
    public DataComputeRegistry ComputeRegistry
    {
        get
        {
            if (_computeRegistry == null)
            {
                throw new InvalidOperationException("DataDefinitionCatalog 未绑定 DataComputeRegistry。");
            }

            return _computeRegistry;
        }
    }

    /// <summary>
    /// 已注册字段定义数量。
    /// </summary>
    public int Count => _definitions.Count;

    /// <summary>
    /// 是否已完成索引构建并冻结注册入口。
    /// </summary>
    public bool IsFrozen => _isFrozen;

    /// <summary>
    /// 枚举已注册字段定义。
    /// </summary>
    public IReadOnlyCollection<DataDefinition> Definitions => _definitions.Values;

    /// <summary>
    /// 注册字段定义。
    /// </summary>
    /// <param name="definition">字段定义。</param>
    public void Register(DataDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        if (_isFrozen)
        {
            throw new InvalidOperationException($"DataDefinitionCatalog 已 frozen，不能继续注册：{definition.StableKey}");
        }

        if (string.IsNullOrWhiteSpace(definition.StableKey))
        {
            throw new InvalidOperationException("DataDefinition.StableKey 不能为空。");
        }

        if (!_definitions.TryAdd(definition.StableKey, definition))
        {
            throw new InvalidOperationException($"重复 DataDefinition stable key：{definition.StableKey}");
        }
    }

    /// <summary>
    /// 根据 stable key 查询字段定义。
    /// </summary>
    /// <param name="stableKey">稳定字段键。</param>
    /// <param name="definition">字段定义。</param>
    public bool TryGet(string stableKey, out DataDefinition definition)
    {
        return _definitions.TryGetValue(stableKey, out definition!);
    }

    /// <summary>
    /// 根据 stable key 获取字段定义，未找到时报错。
    /// </summary>
    /// <param name="stableKey">稳定字段键。</param>
    public DataDefinition GetRequired(string stableKey)
    {
        if (TryGet(stableKey, out var definition))
        {
            return definition;
        }

        throw new KeyNotFoundException($"未注册 DataDefinition：{stableKey}");
    }

    /// <summary>
    /// 获取依赖指定 key 的 computed 字段列表。
    /// </summary>
    /// <param name="stableKey">被依赖的 stable key。</param>
    public IReadOnlyList<string> GetDependentComputedKeys(string stableKey)
    {
        return _dependentComputedKeys.TryGetValue(stableKey, out var dependents)
            ? dependents
            : Array.Empty<string>();
    }

    /// <summary>
    /// 完成注册后的依赖校验与索引冻结。
    /// </summary>
    public void ValidateAndBuildIndexes()
    {
        _dependentComputedKeys.Clear();

        foreach (var definition in _definitions.Values)
        {
            if (definition.IsComputed)
            {
                if (string.IsNullOrWhiteSpace(definition.ComputeId))
                {
                    throw new InvalidOperationException($"computed DataDefinition 必须声明 compute_id：{definition.StableKey}");
                }

                if (_computeRegistry != null)
                {
                    if (!_computeRegistry.Contains(definition.ComputeId))
                    {
                        throw new InvalidOperationException($"DataDefinition 缺少 resolver：{definition.StableKey} -> {definition.ComputeId}");
                    }

                    _computeRegistry.ValidateResolver(definition);
                }
            }

            for (var i = 0; i < definition.Dependencies.Count; i++)
            {
                var dependency = definition.Dependencies[i];
                if (!_definitions.ContainsKey(dependency))
                {
                    throw new InvalidOperationException($"DataDefinition dependency 不存在：{definition.StableKey} -> {dependency}");
                }

                if (definition.IsComputed)
                {
                    if (!_dependentComputedKeys.TryGetValue(dependency, out var dependents))
                    {
                        dependents = new List<string>();
                        _dependentComputedKeys[dependency] = dependents;
                    }

                    dependents.Add(definition.StableKey);
                }
            }
        }

        ValidateComputeCycles();
        _isFrozen = true; // 冻结后禁止继续注册
    }

    /// <summary>
    /// 使用 DFS 检测 computed 字段依赖环。
    /// visiting = 灰色（当前遍历路径上的节点），visited = 黑色（已完成遍历）。
    /// </summary>
    private void ValidateComputeCycles()
    {
        var visiting = new HashSet<string>(StringComparer.Ordinal);
        var visited = new HashSet<string>(StringComparer.Ordinal);

        foreach (var definition in _definitions.Values)
        {
            Visit(definition.StableKey, visiting, visited);
        }
    }

    /// <summary>
    /// DFS 单节点访问：如果节点在当前路径（visiting）中再次出现则检测到环。
    /// </summary>
    private void Visit(string stableKey, HashSet<string> visiting, HashSet<string> visited)
    {
        if (visited.Contains(stableKey)) // 黑色节点，已完成遍历
        {
            return;
        }

        if (!visiting.Add(stableKey)) // 灰色节点，已在当前路径 → 成环
        {
            throw new InvalidOperationException($"DataDefinition compute cycle detected：{stableKey}");
        }

        var definition = GetRequired(stableKey);
        for (var i = 0; i < definition.Dependencies.Count; i++)
        {
            var dependency = definition.Dependencies[i];
            if (TryGet(dependency, out var dependencyDefinition) && dependencyDefinition.IsComputed)
            {
                Visit(dependency, visiting, visited); // 递归访问 computed 依赖
            }
        }

        visiting.Remove(stableKey); // 退出当前路径，移除灰色标记
        visited.Add(stableKey);     // 标记为黑色，后续不再访问
    }
}
