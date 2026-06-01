using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Ability owner 清单服务。
/// <para>负责 ability -> owner 单引用、owner -> ability list 查询和 cleanup，不再使用 EntityRelationshipManager。</para>
/// </summary>
public sealed class AbilityInventoryService
{
    private static readonly Log _log = new(nameof(AbilityInventoryService), LogLevel.Warning);

    private readonly Func<EntityId, Node?> _resolveNode;
    private readonly Func<OwnedReferenceDescriptor, bool> _registerReference;
    private readonly Func<IEntity, IEntity, OwnedReferenceDescriptor, bool> _addReference;
    private readonly Func<IEntity, OwnedReferenceDescriptor, bool> _removeReference;

    private static AbilityInventoryService? _runtime;

    /// <summary>Ability owner Data projection descriptor。</summary>
    public static readonly OwnedReferenceDescriptor OwnerDescriptor = new(
        GeneratedDataKey.AbilityOwnerEntityId,
        GeneratedDataKey.OwnedAbilityIds
    );

    public AbilityInventoryService(Func<EntityId, Node?> resolveNode, OwnedReferenceRegistry ownedReferenceRegistry)
        : this(
            resolveNode,
            ownedReferenceRegistry.Register,
            ownedReferenceRegistry.AddReference,
            ownedReferenceRegistry.RemoveReference)
    {
    }

    public AbilityInventoryService(
        Func<EntityId, Node?> resolveNode,
        Func<OwnedReferenceDescriptor, bool> registerReference,
        Func<IEntity, IEntity, OwnedReferenceDescriptor, bool> addReference,
        Func<IEntity, OwnedReferenceDescriptor, bool> removeReference)
    {
        _resolveNode = resolveNode ?? throw new ArgumentNullException(nameof(resolveNode));
        _registerReference = registerReference ?? throw new ArgumentNullException(nameof(registerReference));
        _addReference = addReference ?? throw new ArgumentNullException(nameof(addReference));
        _removeReference = removeReference ?? throw new ArgumentNullException(nameof(removeReference));
        _registerReference(OwnerDescriptor);
    }

    /// <summary>
    /// 默认运行时服务，使用 EntityManager 的 registry 与 owned-reference cleanup hook。
    /// </summary>
    public static AbilityInventoryService Runtime =>
        _runtime ??= new AbilityInventoryService(
            EntityManager.ResolveEntityNode,
            EntityManager.RegisterOwnedReference,
            EntityManager.AddOwnedReference,
            EntityManager.RemoveOwnedReference
        );

    /// <summary>
    /// 为 owner 添加 snapshot ability。
    /// </summary>
    public AbilityEntity? AddAbility(IEntity owner, AbilityDefinitionView config)
    {
        return AddAbilityCore(
            owner,
            config,
            config.Name,
            config.FeatureHandlerId,
            validateAbilityHandler: true,
            runtimeDataRecordTable: config.Table,
            runtimeDataRecordId: config.RecordId
        );
    }

    /// <summary>
    /// 为 owner 添加运行时构造的通用 Feature record。
    /// </summary>
    public AbilityEntity? AddRuntimeFeature(IEntity owner, RuntimeDataRecordDto record)
    {
        var abilityName = ReadRecordString(record, GeneratedDataKey.Name.StableKey);
        var handlerId = ReadRecordString(record, GeneratedDataKey.FeatureHandlerId.StableKey);
        return AddAbilityCore(
            owner,
            record,
            abilityName,
            handlerId,
            validateAbilityHandler: false,
            runtimeDataRecord: record
        );
    }

    /// <summary>
    /// 建立 owner -> ability 引用。
    /// </summary>
    public bool Attach(IEntity? owner, AbilityEntity? ability)
    {
        if (owner == null || ability == null)
            return false;

        return _addReference(owner, ability, OwnerDescriptor);
    }

    public bool RemoveAbility(IEntity owner, string abilityName)
    {
        if (owner == null || string.IsNullOrWhiteSpace(abilityName))
            return false;

        var ability = GetAbilityByName(owner, abilityName);
        if (ability == null)
        {
            _log.Warn($"单位不拥有技能 {abilityName}");
            return false;
        }

        return RemoveAbility(owner, ability);
    }

    public bool RemoveAbility(IEntity owner, AbilityEntity ability)
    {
        if (owner == null || ability == null)
            return false;

        var abilityId = ability.Data.Get(GeneratedDataKey.Id);
        var abilityName = ability.Data.Get(GeneratedDataKey.Name);

        // Feature 生命周期钩子必须在 Destroy 前调用，使 handler 仍能访问 feature.Data。
        FeatureSystem.OnFeatureRemoved(ability, owner);
        Detach(ability);

        EntityManager.Destroy(ability);
        owner.Events.Emit(new GameEventType.Ability.Removed(abilityName, abilityId, owner));

        _log.Info($"移除技能实例: {abilityName} ({abilityId})");
        return true;
    }

    /// <summary>
    /// 移除 ability 当前 owner 引用。
    /// </summary>
    public bool Detach(AbilityEntity? ability)
    {
        if (ability == null)
            return false;

        return _removeReference(ability, OwnerDescriptor);
    }

    /// <summary>
    /// 获取 ability owner。
    /// </summary>
    public IEntity? GetOwner(AbilityEntity? ability)
    {
        if (ability == null || !ability.Data.Has(GeneratedDataKey.AbilityOwnerEntityId))
            return null;

        var ownerId = EntityId.From(ability.Data.Get(GeneratedDataKey.AbilityOwnerEntityId));
        if (ownerId.IsEmpty)
            return null;

        return _resolveNode(ownerId) as IEntity;
    }

    /// <summary>
    /// 获取 owner 拥有的全部 AbilityEntity。
    /// </summary>
    public List<AbilityEntity> GetAbilities(IEntity? owner)
    {
        var abilities = new List<AbilityEntity>();
        if (owner == null || !owner.Data.Has(GeneratedDataKey.OwnedAbilityIds))
            return abilities;

        var ownerId = ResolveEntityId(owner);
        if (ownerId.IsEmpty)
            return abilities;

        var abilityIds = EntityIdList.FromStringArray(owner.Data.Get(GeneratedDataKey.OwnedAbilityIds));
        foreach (var abilityId in abilityIds.Values)
        {
            if (_resolveNode(abilityId) is not AbilityEntity ability)
                continue;

            var currentOwnerId = ability.Data.Has(GeneratedDataKey.AbilityOwnerEntityId)
                ? EntityId.From(ability.Data.Get(GeneratedDataKey.AbilityOwnerEntityId))
                : EntityId.Empty;
            if (currentOwnerId == ownerId)
                abilities.Add(ability);
        }

        return abilities;
    }

    /// <summary>
    /// 获取 owner 拥有的手动主动技能。
    /// </summary>
    public List<AbilityEntity> GetManualAbilities(IEntity? owner)
    {
        return GetAbilities(owner)
            .Where(ability =>
            {
                var mode = ability.Data.Get<AbilityTriggerMode>(GeneratedDataKey.AbilityTriggerMode);
                var type = ability.Data.Get<AbilityType>(GeneratedDataKey.AbilityType);
                return type != AbilityType.Passive && mode.HasFlag(AbilityTriggerMode.Manual);
            })
            .ToList();
    }

    public AbilityEntity? GetAbilityByName(IEntity? owner, string abilityName)
    {
        if (string.IsNullOrWhiteSpace(abilityName))
            return null;

        return GetAbilities(owner)
            .FirstOrDefault(ability => ability.Data.Get<string>(GeneratedDataKey.Name) == abilityName);
    }

    public AbilityEntity? GetAbilityById(IEntity? owner, EntityId abilityId)
    {
        if (abilityId.IsEmpty)
            return null;

        return GetAbilities(owner)
            .FirstOrDefault(ability => ResolveEntityId(ability) == abilityId);
    }

    private AbilityEntity? AddAbilityCore(
        IEntity owner,
        object config,
        string abilityName,
        string handlerIdFromConfig,
        bool validateAbilityHandler,
        RuntimeDataRecordDto? runtimeDataRecord = null,
        string? runtimeDataRecordTable = null,
        string? runtimeDataRecordId = null)
    {
        if (owner == null)
        {
            _log.Error("无法添加技能：拥有者为空");
            return null;
        }

        if (string.IsNullOrEmpty(abilityName))
        {
            _log.Error("无法添加技能：配置缺少 Name 属性");
            return null;
        }

        var existingAbility = GetAbilityByName(owner, abilityName);
        if (existingAbility != null)
        {
            _log.Warn($"单位已拥有技能 {abilityName}");
            return existingAbility;
        }

        if (validateAbilityHandler && !ValidateAbilityHandlerConfig(handlerIdFromConfig, abilityName))
            return null;

        var ability = EntityManager.Spawn<AbilityEntity>(new EntitySpawnConfig
        {
            Config = config,
            RuntimeDataRecord = runtimeDataRecord,
            RuntimeDataRecordTable = runtimeDataRecordTable,
            RuntimeDataRecordId = runtimeDataRecordId,
            UsingObjectPool = true,
            PoolName = ObjectPoolNames.AbilityPool,
            LifecycleParentId = ResolveEntityId(owner),
            ParentDestroyPolicy = ParentDestroyPolicy.DestroyRecursively
        });

        if (ability == null)
        {
            _log.Error($"创建技能实体失败: {abilityName}");
            return null;
        }

        var handlerId = ability.Data.Get(GeneratedDataKey.FeatureHandlerId);
        if (validateAbilityHandler && string.IsNullOrWhiteSpace(handlerId))
        {
            _log.Error($"无法添加技能 '{abilityName}'：FeatureHandlerId 为空");
            EntityManager.Destroy(ability);
            return null;
        }

        if (validateAbilityHandler && !FeatureHandlerRegistry.HasHandler(handlerId))
        {
            _log.Error($"无法添加技能 '{abilityName}'：未注册 FeatureHandlerId='{handlerId}'");
            EntityManager.Destroy(ability);
            return null;
        }

        if (!Attach(owner, ability))
        {
            _log.Error($"技能 owner 引用写入失败: {abilityName}");
            EntityManager.Destroy(ability);
            return null;
        }

        ability.Events.On<GameEventType.Ability.TryTrigger>(AbilitySystem.HandleTryTrigger);
        owner.Events.Emit(new GameEventType.Ability.Added(ability, owner));
        FeatureSystem.OnFeatureGranted(ability, owner);

        _log.Info($"添加技能: {abilityName} -> {ResolveEntityId(owner).Value}");
        return ability;
    }

    private static string ReadRecordString(RuntimeDataRecordDto record, string stableKey)
    {
        if (record.Fields.TryGetValue(stableKey, out var field) && field.Value is string text)
            return text;

        return string.Empty;
    }

    private static bool ValidateAbilityHandlerConfig(string handlerId, string abilityName)
    {
        if (string.IsNullOrWhiteSpace(handlerId))
        {
            _log.Error($"无法添加技能 '{abilityName}'：FeatureHandlerId 为空");
            return false;
        }

        if (!FeatureHandlerRegistry.HasHandler(handlerId))
        {
            _log.Error($"无法添加技能 '{abilityName}'：未注册 FeatureHandlerId='{handlerId}'");
            return false;
        }

        return true;
    }

    private static EntityId ResolveEntityId(IEntity entity)
    {
        var id = EntityId.From(entity.Data.Get(GeneratedDataKey.Id));
        if (!id.IsEmpty || entity is not Node node)
            return id;

        return EntityId.From(node.GetInstanceId().ToString());
    }
}
