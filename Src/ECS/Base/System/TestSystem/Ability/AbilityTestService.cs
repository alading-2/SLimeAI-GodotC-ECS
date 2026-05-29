using slime.data.Abilities;
using System;
using System.Collections.Generic;
using ECS.Base.System.TestSystem.Core;

/// <summary>
/// 技能测试模块的数据与操作服务。
/// <para>
/// 负责技能配置目录缓存、分类解析、当前实体技能视图构建，以及增删启停等操作。
/// UI 只消费这里返回的视图模型，不直接关心资源扫描和业务细节。
/// </para>
/// </summary>
internal sealed class AbilityTestService
{
    /// <summary>技能分组兜底 ID：当配置与运行时数据都无法提供分组时使用。</summary>
    private const string DefaultFeatureGroupId = "技能.未分类";

    /// <summary>Feature 调试服务，用于复用正式链路执行授予、移除与启停。</summary>
    private readonly FeatureDebugService _featureDebugService = new();

    /// <summary>缓存全部技能配置（名称 → DataOS runtime table 配置对象）。</summary>
    private readonly Dictionary<string, AbilityData> _configByKey = new(StringComparer.Ordinal);

    /// <summary>缓存技能库顺序，供左侧分类树稳定展示。</summary>
    private readonly List<AbilityConfigEntry> _catalogEntries = new();

    /// <summary>记录 FeatureGroupId 的首次出现顺序，避免界面每次刷新都乱序。</summary>
    private readonly Dictionary<string, int> _featureGroupOrder = new(StringComparer.Ordinal);

    /// <summary>
    /// 技能配置缓存项。
    /// <para>
    /// 包含原始配置、资源键和解析后的分类信息，便于 UI 层直接消费。
    /// </para>
    /// </summary>
    private sealed record AbilityConfigEntry(
        string ResourceKey,
        string AbilityName,
        string DisplayName,
        string FeatureGroupId,
        string Description,
        AbilityType AbilityType,
        AbilityTriggerMode TriggerMode
    );

    /// <summary>
    /// 初始化技能测试服务并预热技能目录缓存。
    /// </summary>
    public AbilityTestService()
    {
        LoadAllAbilityConfigs();
    }

    /// <summary>
    /// 为当前实体添加一个技能实例。
    /// </summary>
    /// <param name="owner">技能拥有者实体，为空时返回失败结果。</param>
    /// <param name="resourceKey">技能资源键（ResourceKey）。</param>
    /// <returns>添加操作结果，包含成功标记与提示文本。</returns>
    public TestActionResult AddAbility(IEntity? owner, string resourceKey)
    {
        if (string.IsNullOrWhiteSpace(resourceKey))
        {
            return Fail("技能资源键不能为空");
        }

        if (!_configByKey.TryGetValue(resourceKey, out var config))
        {
            return Fail($"未找到技能配置: {resourceKey}");
        }

        return _featureDebugService.GrantAbility(owner, config, resourceKey);
    }

    /// <summary>
    /// 移除指定技能实例。
    /// </summary>
    /// <param name="owner">技能拥有者实体，为空时返回失败结果。</param>
    /// <param name="abilityId">运行时技能实例 Id。</param>
    /// <returns>移除操作结果，包含成功标记与提示文本。</returns>
    public TestActionResult RemoveAbility(IEntity? owner, string abilityId)
    {
        if (string.IsNullOrWhiteSpace(abilityId))
        {
            return Fail("技能实例 Id 不能为空");
        }

        var ability = FindOwnedAbility(owner, abilityId);
        return _featureDebugService.RemoveAbility(owner, ability);
    }

    /// <summary>
    /// 切换指定技能实例的启用状态。
    /// </summary>
    /// <param name="owner">技能拥有者实体，为空时返回失败结果。</param>
    /// <param name="abilityId">运行时技能实例 Id。</param>
    /// <param name="isEnabled">目标启用状态，true 为启用，false 为禁用。</param>
    /// <returns>启停操作结果，包含成功标记与提示文本。</returns>
    public TestActionResult SetAbilityEnabled(IEntity? owner, string abilityId, bool isEnabled)
    {
        if (string.IsNullOrWhiteSpace(abilityId))
        {
            return Fail("技能实例 Id 不能为空");
        }

        var ability = FindOwnedAbility(owner, abilityId);
        return _featureDebugService.SetFeatureEnabled(owner, ability, isEnabled);
    }

    /// <summary>
    /// 按技能实例 Id 查询当前实体的技能视图。
    /// </summary>
    /// <param name="owner">技能拥有者实体。</param>
    /// <param name="abilityId">运行时技能实例 Id。</param>
    /// <param name="itemView">输出的技能视图数据。</param>
    /// <returns>存在对应技能实例时返回 true，否则返回 false。</returns>
    public bool TryGetOwnedItem(IEntity? owner, string abilityId, out AbilityOwnedItemView itemView)
    {
        itemView = default;
        if (string.IsNullOrWhiteSpace(abilityId))
        {
            return false;
        }

        var ability = FindOwnedAbility(owner, abilityId);
        if (ability == null)
        {
            return false;
        }

        itemView = CreateOwnedItemView(ability);
        return true;
    }

    /// <summary>
    /// 获取技能库视图，并根据当前实体标记“已拥有”状态。
    /// </summary>
    /// <param name="owner">当前选中的实体，可为空。</param>
    /// <returns>按 FeatureGroupId 组织后的技能库视图集合。</returns>
    public IReadOnlyList<AbilityFeatureGroup<AbilityCatalogItemView>> GetCatalogGroups(IEntity? owner)
    {
        var ownedNames = new HashSet<string>(StringComparer.Ordinal);
        if (owner != null)
        {
            foreach (var ability in EntityManager.GetAbilities(owner))
            {
                var abilityName = ability.Data.Get<string>(GeneratedDataKey.Name.Key); // 解析 GeneratedDataKey.Name 键名
                if (!string.IsNullOrWhiteSpace(abilityName))
                {
                    ownedNames.Add(abilityName);
                }
            }
        }

        var views = new List<AbilityCatalogItemView>(_catalogEntries.Count);
        foreach (var entry in _catalogEntries)
        {
            views.Add(new AbilityCatalogItemView(
                entry.ResourceKey,
                entry.DisplayName,
                entry.FeatureGroupId,
                entry.Description,
                entry.AbilityType,
                entry.TriggerMode,
                ownedNames.Contains(entry.AbilityName)
            ));
        }

        return BuildFeatureGroupGroups(
            views,
            static item => item.FeatureGroupId,
            static item => item.DisplayName
        );
    }

    /// <summary>
    /// 获取当前实体已拥有技能的分类视图。
    /// </summary>
    /// <param name="owner">当前选中的实体，可为空。</param>
    /// <returns>按 FeatureGroupId 组织后的已拥有技能视图集合。</returns>
    public IReadOnlyList<AbilityFeatureGroup<AbilityOwnedItemView>> GetOwnedGroups(IEntity? owner)
    {
        var views = new List<AbilityOwnedItemView>();
        if (owner == null)
        {
            return Array.Empty<AbilityFeatureGroup<AbilityOwnedItemView>>();
        }

        foreach (var ability in EntityManager.GetAbilities(owner))
        {
            views.Add(CreateOwnedItemView(ability));
        }

        return BuildFeatureGroupGroups(
            views,
            static item => item.FeatureGroupId,
            static item => item.DisplayName
        );
    }

    /// <summary>
    /// 加载全部技能配置，并提前计算展示所需的分类信息。
    /// </summary>
    private void LoadAllAbilityConfigs()
    {
        _configByKey.Clear();
        _catalogEntries.Clear();
        _featureGroupOrder.Clear();

        LoadPureCSharpAbilityConfigs();

        _catalogEntries.Sort((left, right) =>
        {
            var featureGroupCompare = CompareFeatureGroupId(left.FeatureGroupId, right.FeatureGroupId);
            if (featureGroupCompare != 0)
            {
                return featureGroupCompare;
            }

            return string.Compare(left.DisplayName, right.DisplayName, StringComparison.Ordinal);
        });
    }

    /// <summary>
    /// 从 DataOS runtime table 纯 C# 表加载技能配置。
    /// </summary>
    private void LoadPureCSharpAbilityConfigs()
    {
        foreach (var config in AbilityData.All)
        {
            var abilityName = string.IsNullOrWhiteSpace(config.Name) ? config.GetType().Name : config.Name!;
            AddDataOsRuntimeTableAbilityConfigEntry(abilityName, config);
        }
    }

    /// <summary>
    /// 写入 DataOS runtime table 技能配置缓存和展示条目。
    /// </summary>
    /// <param name="resourceKey">测试面板内部使用的配置键，DataOS runtime table 模式下直接使用技能 Name。</param>
    /// <param name="config">DataOS runtime table 技能配置。</param>
    private void AddDataOsRuntimeTableAbilityConfigEntry(string resourceKey, AbilityData config)
    {
        var displayName = string.IsNullOrWhiteSpace(config.Name) ? resourceKey : config.Name!;
        var featureGroupId = ResolveFeatureGroupId(config, resourceKey);
        var description = config.Description;
        if (string.IsNullOrWhiteSpace(description))
        {
            description = "暂无描述";
        }

        _configByKey[resourceKey] = config;
        _catalogEntries.Add(new AbilityConfigEntry(
            resourceKey,
            displayName,
            displayName,
            featureGroupId,
            description,
            config.AbilityType,
            config.AbilityTriggerMode
        ));

        RegisterFeatureGroupOrder(featureGroupId);
    }

    /// <summary>
    /// 构建当前技能实例的视图模型。
    /// </summary>
    private AbilityOwnedItemView CreateOwnedItemView(AbilityEntity ability)
    {
        var abilityName = ability.Data.Get<string>(GeneratedDataKey.Name.Key);
        var featureGroupId = ResolveFeatureGroupId(ability);
        var description = ability.Data.Get<string>(GeneratedDataKey.Description.Key);
        var abilityId = ability.Data.Get<string>(GeneratedDataKey.Id.Key);
        var isEnabled = ability.Data.Get<bool>(GeneratedDataKey.FeatureEnabled.Key);
        var abilityType = ability.Data.Get<AbilityType>(GeneratedDataKey.AbilityType.Key);
        var triggerMode = ability.Data.Get<AbilityTriggerMode>(GeneratedDataKey.AbilityTriggerMode.Key);

        RegisterFeatureGroupOrder(featureGroupId);

        return new AbilityOwnedItemView(
            abilityId,
            abilityName,
            featureGroupId,
            string.IsNullOrWhiteSpace(description) ? "暂无描述" : description,
            abilityType,
            triggerMode,
            isEnabled
        );
    }

    /// <summary>
    /// 按运行时技能实例 Id 查找技能实体。
    /// </summary>
    private static AbilityEntity? FindOwnedAbility(IEntity? owner, string abilityId)
    {
        if (owner == null || string.IsNullOrWhiteSpace(abilityId))
        {
            return null;
        }

        foreach (var ability in EntityManager.GetAbilities(owner))
        {
            var currentAbilityId = ability.Data.Get<string>(GeneratedDataKey.Id.Key);
            if (string.Equals(currentAbilityId, abilityId, StringComparison.Ordinal))
            {
                return ability;
            }
        }

        return null;
    }

    /// <summary>
    /// 解析技能展示分组 ID。
    /// <para>
    /// 统一使用 FeatureGroupId；缺失时按技能类型 / 触发模式兜底。
    /// </para>
    /// </summary>
    private static string ResolveFeatureGroupId(AbilityData config, string resourceKey)
    {
        if (!string.IsNullOrWhiteSpace(config.FeatureGroupId))
        {
            return NormalizeFeatureGroupId(config.FeatureGroupId);
        }

        if ((config.AbilityTriggerMode & AbilityTriggerMode.Manual) != 0)
        {
            return NormalizeFeatureGroupId(FeatureId.Ability.Groups.Active);
        }

        return config.AbilityType switch
        {
            AbilityType.Active => NormalizeFeatureGroupId(FeatureId.Ability.Groups.Active),
            AbilityType.Passive => NormalizeFeatureGroupId(FeatureId.Ability.Groups.Passive),
            AbilityType.Weapon => NormalizeFeatureGroupId("技能.武器"),
            _ => DefaultFeatureGroupId
        };
    }

    /// <summary>
    /// 从运行时技能 Data 中解析展示分组 ID。
    /// </summary>
    private static string ResolveFeatureGroupId(AbilityEntity ability)
    {
        var featureGroup = ability.Data.Get<string>(GeneratedDataKey.AbilityFeatureGroup.Key);
        if (!string.IsNullOrWhiteSpace(featureGroup))
        {
            return NormalizeFeatureGroupId(featureGroup);
        }

        var abilityType = ability.Data.Get<AbilityType>(GeneratedDataKey.AbilityType.Key);
        return abilityType switch
        {
            AbilityType.Active => NormalizeFeatureGroupId(FeatureId.Ability.Groups.Active),
            AbilityType.Passive => NormalizeFeatureGroupId(FeatureId.Ability.Groups.Passive),
            AbilityType.Weapon => NormalizeFeatureGroupId("技能.武器"),
            _ => DefaultFeatureGroupId
        };
    }

    /// <summary>
    /// 标准化技能分组 ID，保留完整 FeatureGroupId 作为测试面板分类显示。
    /// </summary>
    private static string NormalizeFeatureGroupId(string? featureGroupId)
    {
        if (string.IsNullOrWhiteSpace(featureGroupId))
        {
            return DefaultFeatureGroupId;
        }

        var normalized = featureGroupId.Trim().Replace('/', '.');
        var parts = normalized.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return DefaultFeatureGroupId;
        }

        return string.Join('.', parts);
    }

    /// <summary>
    /// 根据 FeatureGroupId 与名称构建稳定的分组结果。
    /// </summary>
    private IReadOnlyList<AbilityFeatureGroup<TItem>> BuildFeatureGroupGroups<TItem>(
        List<TItem> items,
        Func<TItem, string> featureGroupSelector,
        Func<TItem, string> nameSelector)
    {
        items.Sort((left, right) =>
        {
            var leftFeatureGroupId = featureGroupSelector(left);
            var rightFeatureGroupId = featureGroupSelector(right);
            var featureGroupCompare = CompareFeatureGroupId(leftFeatureGroupId, rightFeatureGroupId);
            if (featureGroupCompare != 0)
            {
                return featureGroupCompare;
            }

            return string.Compare(nameSelector(left), nameSelector(right), StringComparison.Ordinal);
        });

        var groups = new List<AbilityFeatureGroup<TItem>>();
        string? currentFeatureGroupId = null;
        List<TItem>? currentItems = null;

        foreach (var item in items)
        {
            var featureGroupId = featureGroupSelector(item);
            if (!string.Equals(currentFeatureGroupId, featureGroupId, StringComparison.Ordinal))
            {
                currentItems = new List<TItem>();
                groups.Add(new AbilityFeatureGroup<TItem>(featureGroupId, currentItems));
                currentFeatureGroupId = featureGroupId;
            }

            currentItems!.Add(item);
        }

        return groups;
    }

    /// <summary>
    /// 记录 FeatureGroupId 首次出现顺序。
    /// </summary>
    private void RegisterFeatureGroupOrder(string featureGroupId)
    {
        if (_featureGroupOrder.ContainsKey(featureGroupId))
        {
            return;
        }

        _featureGroupOrder[featureGroupId] = _featureGroupOrder.Count;
    }

    /// <summary>
    /// 比较两个 FeatureGroupId 的稳定顺序。
    /// </summary>
    private int CompareFeatureGroupId(string left, string right)
    {
        var leftOrder = _featureGroupOrder.TryGetValue(left, out var existingLeftOrder)
            ? existingLeftOrder
            : int.MaxValue;
        var rightOrder = _featureGroupOrder.TryGetValue(right, out var existingRightOrder)
            ? existingRightOrder
            : int.MaxValue;

        if (leftOrder != rightOrder)
        {
            return leftOrder.CompareTo(rightOrder);
        }

        return string.Compare(left, right, StringComparison.Ordinal);
    }

    /// <summary>
    /// 创建成功结果。
    /// </summary>
    /// <param name="message">返回给 UI 的提示文本。</param>
    /// <returns>成功状态的操作结果。</returns>
    private static TestActionResult Success(string message) => new(true, message);

    /// <summary>
    /// 创建失败结果。
    /// </summary>
    /// <param name="message">返回给 UI 的提示文本。</param>
    /// <returns>失败状态的操作结果。</returns>
    private static TestActionResult Fail(string message) => new(false, message);
}
