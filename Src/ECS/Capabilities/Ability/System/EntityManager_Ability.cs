// Src/ECS/System/AbilitySystem/EntityManager_Ability.cs
// partial 扩展 EntityManager，保留 Ability 旧入口兼容层。
// 真实 Ability owner 清单逻辑已迁入 AbilityInventoryService。

using System.Collections.Generic;

/// <summary>
/// EntityManager 的 Ability 兼容扩展。
/// <para>新代码优先直接使用 <see cref="AbilityInventoryService"/>。</para>
/// </summary>
public static partial class EntityManager
{
    /// <summary>
    /// 为单位添加 runtime snapshot 技能。
    /// </summary>
    public static AbilityEntity? AddAbility(IEntity owner, AbilityDefinitionView config)
    {
        return AbilityInventoryService.Runtime.AddAbility(owner, config);
    }

    /// <summary>
    /// 为单位添加运行时构造的通用 Feature record。
    /// </summary>
    public static AbilityEntity? AddRuntimeFeature(IEntity owner, RuntimeDataRecordDto record)
    {
        return AbilityInventoryService.Runtime.AddRuntimeFeature(owner, record);
    }

    /// <summary>
    /// 从单位移除技能。
    /// </summary>
    public static bool RemoveAbility(IEntity owner, string abilityName)
    {
        return AbilityInventoryService.Runtime.RemoveAbility(owner, abilityName);
    }

    /// <summary>
    /// 从单位移除指定技能实例。
    /// </summary>
    public static bool RemoveAbility(IEntity owner, AbilityEntity ability)
    {
        return AbilityInventoryService.Runtime.RemoveAbility(owner, ability);
    }

    /// <summary>
    /// 获取单位的所有技能。
    /// </summary>
    public static List<AbilityEntity> GetAbilities(IEntity owner)
    {
        return AbilityInventoryService.Runtime.GetAbilities(owner);
    }

    /// <summary>
    /// 获取单位的所有手动触发主动技能（供输入组件和UI共用）
    /// 过滤规则：TriggerMode 包含 Manual 且 AbilityType 不是 Passive
    /// </summary>
    public static List<AbilityEntity> GetManualAbilities(IEntity owner)
    {
        return AbilityInventoryService.Runtime.GetManualAbilities(owner);
    }

    /// <summary>
    /// 根据名称获取技能。
    /// </summary>
    public static AbilityEntity? GetAbilityByName(IEntity owner, string abilityName)
    {
        return AbilityInventoryService.Runtime.GetAbilityByName(owner, abilityName);
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

        return AbilityInventoryService.Runtime.GetAbilityById(owner, EntityId.From(abilityId));
    }
}
