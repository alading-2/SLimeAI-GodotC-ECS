// Src/ECS/System/AbilitySystem/EntityManager_Ability.cs
// partial 扩展 EntityManager，提供 Ability 相关的增删查功能
// 放在 AbilitySystem 目录下，逻辑上属于 Ability 模块

using System.Collections.Generic;
using slime.config.Features;
using slime.data.Abilities;

/// <summary>
/// EntityManager 的 Ability 扩展
/// 
/// 职责：管理 Ability 的生命周期（增删查）
/// 注意：激活逻辑由 AbilitySystem 负责
/// </summary>
public static partial class EntityManager
{
    private static readonly Log _abilityLog = new("EntityManager_Ability", LogLevel.Warning);

    // ==================== Ability 管理 ====================

    /// <summary>
    /// 为单位添加 snapshot-backed 技能。
    /// </summary>
    /// <param name="owner">技能拥有者。</param>
    /// <param name="config">snapshot-backed 技能配置。</param>
    /// <returns>创建的技能实体，失败返回 null。</returns>
    public static AbilityEntity? AddAbility(IEntity owner, AbilityData config)
    {
        return AddAbilityCore(
            owner, // 技能拥有者
            config, // DataNew 技能配置
            config.Name ?? "", // 技能名称
            config.FeatureHandlerId ?? "", // FeatureHandlerId
            validateAbilityHandler: true // 技能必须绑定处理器
        );
    }

    /// <summary>
    /// 为单位添加运行时构造的通用 Feature。
    /// </summary>
    /// <param name="owner">技能拥有者。</param>
    /// <param name="config">运行时 Feature 定义。</param>
    /// <returns>创建的技能实体，失败返回 null。</returns>
    public static AbilityEntity? AddAbility(IEntity owner, FeatureDefinition config)
    {
        return AddAbilityCore(
            owner, // 技能拥有者
            config, // 运行时 Feature 定义
            config.Name ?? "", // Feature 名称
            config.FeatureHandlerId ?? "", // FeatureHandlerId
            validateAbilityHandler: false // 通用 Feature 可由测试系统临时构造
        );
    }

    /// <summary>
    /// 技能添加统一实现，外部优先使用 snapshot-backed 重载。
    /// </summary>
    /// <param name="owner">技能拥有者。</param>
    /// <param name="config">配置对象。</param>
    /// <param name="abilityName">技能名称。</param>
    /// <param name="handlerIdFromConfig">配置中声明的 FeatureHandlerId。</param>
    /// <param name="validateAbilityHandler">是否校验技能处理器。</param>
    /// <returns>创建的技能实体，失败返回 null。</returns>
    private static AbilityEntity? AddAbilityCore(
        IEntity owner,
        object config,
        string abilityName,
        string handlerIdFromConfig,
        bool validateAbilityHandler)
    {
        if (owner == null)
        {
            _abilityLog.Error("无法添加技能：拥有者为空");
            return null;
        }

        if (string.IsNullOrEmpty(abilityName))
        {
            _abilityLog.Error("无法添加技能：配置缺少 Name 属性");
            return null;
        }

        // 检查是否已拥有相同技能
        var existingAbility = GetAbilityByName(owner, abilityName);
        if (existingAbility != null)
        {
            _abilityLog.Warn($"单位已拥有技能 {abilityName}");
            return existingAbility;
        }

        if (validateAbilityHandler && !ValidateAbilityHandlerConfig(handlerIdFromConfig, abilityName))
        {
            return null;
        }

        // 创建技能实体
        AbilityEntity? ability;
        ability = Spawn<AbilityEntity>(new EntitySpawnConfig
        {
            Config = config, // 技能配置数据
            UsingObjectPool = true, // 技能实体统一走对象池
            PoolName = ObjectPoolNames.AbilityPool, // 技能对象池
            ParentEntity = owner, // 父实体/技能拥有者
            AutoAddParentRelation = true, // 自动补 PARENT，供统一溯源
            ParentDestroyPolicy = ParentDestroyPolicy.DestroyRecursively, // 拥有者销毁时递归销毁技能实体
            ParentRelationTypes = [EntityRelationshipType.ENTITY_TO_ABILITY] // 业务关系：拥有者 -> 技能
        });

        if (ability == null)
        {
            _abilityLog.Error($"创建技能实体失败: {abilityName}");
            return null;
        }

        var handlerId = ability.Data.Get<string>(DataKey.FeatureHandlerId);
        if (validateAbilityHandler && string.IsNullOrWhiteSpace(handlerId))
        {
            _abilityLog.Error($"无法添加技能 '{abilityName}'：FeatureHandlerId 为空");
            Destroy(ability);
            return null;
        }

        if (validateAbilityHandler && !FeatureHandlerRegistry.HasHandler(handlerId))
        {
            _abilityLog.Error($"无法添加技能 '{abilityName}'：未注册 FeatureHandlerId='{handlerId}'");
            Destroy(ability);
            return null;
        }

        // 获取 ID（从 Data 读取，由 EntityManager.Spawn 设置）
        var ownerId = owner.Data.Get<string>(DataKey.Id) ?? string.Empty;

        // 核心逻辑连通：订阅 TryTrigger 事件，由 AbilitySystem 统一处理
        ability.Events.On<GameEventType.Ability.TryTriggerEventData>(
            GameEventType.Ability.TryTrigger,
            AbilitySystem.HandleTryTrigger
        );

        // 发送事件
        owner.Events.Emit(
            GameEventType.Ability.Added,
            new GameEventType.Ability.AddedEventData(ability, owner)
        );

        // Feature 生命周期钩子：Granted
        FeatureSystem.OnFeatureGranted(ability, owner);

        _abilityLog.Info($"添加技能: {abilityName} -> {ownerId}");
        return ability;
    }

    /// <summary>
    /// 校验技能配置是否显式绑定了可用的 FeatureHandler。
    /// </summary>
    /// <param name="handlerId">FeatureHandlerId。</param>
    /// <param name="abilityName">技能名称，用于日志定位。</param>
    /// <returns>配置有效返回 true，否则返回 false。</returns>
    private static bool ValidateAbilityHandlerConfig(string handlerId, string abilityName)
    {
        if (string.IsNullOrWhiteSpace(handlerId))
        {
            _abilityLog.Error($"无法添加技能 '{abilityName}'：FeatureHandlerId 为空");
            return false;
        }

        if (!FeatureHandlerRegistry.HasHandler(handlerId))
        {
            _abilityLog.Error($"无法添加技能 '{abilityName}'：未注册 FeatureHandlerId='{handlerId}'");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 从单位移除技能
    /// </summary>
    /// <param name="owner">技能拥有者</param>
    /// <param name="abilityName">技能名称</param>
    public static bool RemoveAbility(IEntity owner, string abilityName)
    {
        if (owner == null || string.IsNullOrEmpty(abilityName)) return false;

        var ability = GetAbilityByName(owner, abilityName);
        if (ability == null)
        {
            _abilityLog.Warn($"单位不拥有技能 {abilityName}");
            return false;
        }

        return RemoveAbility(owner, ability);
    }

    /// <summary>
    /// 从单位移除指定技能实例。
    /// </summary>
    /// <param name="owner">技能拥有者</param>
    /// <param name="ability">要移除的技能实例</param>
    /// <returns>成功移除返回 true，否则返回 false</returns>
    public static bool RemoveAbility(IEntity owner, AbilityEntity ability)
    {
        if (owner == null || ability == null) return false;

        // 获取 ID（从 Data 读取）
        var ownerId = owner.Data.Get<string>(DataKey.Id) ?? string.Empty;
        var abilityId = ability.Data.Get<string>(DataKey.Id) ?? string.Empty;
        var abilityName = ability.Data.Get<string>(DataKey.Name) ?? string.Empty;

        // 移除关系
        EntityRelationshipManager.RemoveRelationship(
            ownerId,
            abilityId,
            EntityRelationshipType.ENTITY_TO_ABILITY
        );

        // Feature 生命周期钩子：Removed（在 Destroy 之前，使 handler 还能访问 feature.Data）
        FeatureSystem.OnFeatureRemoved(ability, owner);

        // 销毁技能实体（自动处理对象池归还）
        Destroy(ability);

        // 发送事件
        owner.Events.Emit(
            GameEventType.Ability.Removed,
            new GameEventType.Ability.RemovedEventData(abilityName, abilityId, owner)
        );

        _abilityLog.Info($"移除技能实例: {abilityName} ({abilityId}) <- {ownerId}");
        return true;
    }

    // ==================== Ability 查询 ====================

    /// <summary>
    /// 获取单位的所有技能
    /// </summary>
    public static List<AbilityEntity> GetAbilities(IEntity owner)
    {
        var abilities = new List<AbilityEntity>();
        if (owner == null) return abilities;

        var ownerId = owner.Data.Get<string>(DataKey.Id) ?? string.Empty;
        var abilityIds = EntityRelationshipManager.GetChildEntitiesByParentAndType(
            ownerId,
            EntityRelationshipType.ENTITY_TO_ABILITY
        );

        foreach (var abilityId in abilityIds)
        {
            var entity = GetEntityById(abilityId);
            if (entity is AbilityEntity ability)
            {
                abilities.Add(ability);
            }
        }

        return abilities;
    }

    /// <summary>
    /// 获取单位的所有手动触发主动技能（供输入组件和UI共用）
    /// 过滤规则：TriggerMode 包含 Manual 且 AbilityType 不是 Passive
    /// </summary>
    public static List<AbilityEntity> GetManualAbilities(IEntity owner)
    {
        var abilities = GetAbilities(owner);
        var result = new List<AbilityEntity>();
        foreach (var a in abilities)
        {
            var mode = a.Data.Get<AbilityTriggerMode>(DataKey.AbilityTriggerMode);
            var type = a.Data.Get<AbilityType>(DataKey.AbilityType);
            if (type != AbilityType.Passive && mode.HasFlag(AbilityTriggerMode.Manual))
            {
                result.Add(a);
            }
        }
        return result;
    }

    /// <summary>
    /// 根据名称获取技能
    /// </summary>
    public static AbilityEntity? GetAbilityByName(IEntity owner, string abilityName)
    {
        var abilities = GetAbilities(owner);
        foreach (var ability in abilities)
        {
            // 从 Data 读取技能名称（不使用便捷属性）
            var name = ability.Data.Get<string>(DataKey.Name);
            if (name == abilityName)
            {
                return ability;
            }
        }
        return null;
    }

    /// <summary>
    /// 根据运行时实例 ID 获取技能。
    /// </summary>
    public static AbilityEntity? GetAbilityById(IEntity owner, string abilityId)
    {
        if (owner == null || string.IsNullOrWhiteSpace(abilityId))
        {
            return null;
        }

        var abilities = GetAbilities(owner);
        foreach (var ability in abilities)
        {
            var currentId = ability.Data.Get<string>(DataKey.Id);
            if (currentId == abilityId)
            {
                return ability;
            }
        }

        return null;
    }
}
