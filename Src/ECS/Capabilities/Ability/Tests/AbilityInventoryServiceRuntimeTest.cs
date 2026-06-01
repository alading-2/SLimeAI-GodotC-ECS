using Godot;
using System;
using System.Linq;

namespace Slime.Test.Ability;

/// <summary>
/// AbilityInventoryService 运行时契约测试。
/// </summary>
public partial class AbilityInventoryServiceRuntimeTest : Node
{
    private static readonly Log _log = new(nameof(AbilityInventoryServiceRuntimeTest));
    private const string LegacyAbilityRelationshipType = "relationship.entity.ability";

    private int _passedCount;
    private int _failedCount;

    public override void _Ready()
    {
        _log.Info("开始 AbilityInventoryService 运行时测试");

        try
        {
            AbilityInventoryService_Attach_ShouldWriteOwnerProjectionAndQueryWithoutRelationship();
            AbilityInventoryService_Detach_ShouldClearOwnerProjectionAndList();
            AbilityInventoryService_DestroyCleanup_ShouldRemoveAbilityFromOwnerList();
        }
        catch (Exception ex)
        {
            Fail($"测试过程中发生异常: {ex}");
        }

        _log.Info($"AbilityInventoryService 运行时测试结束: PASS={_passedCount}, FAIL={_failedCount}");
        GetTree().Quit(_failedCount == 0 ? 0 : 1);
    }

    private void AbilityInventoryService_Attach_ShouldWriteOwnerProjectionAndQueryWithoutRelationship()
    {
        var registry = new EntityRegistry();
        var ownedReferences = new OwnedReferenceRegistry(registry.GetNode);
        var service = new AbilityInventoryService(registry.GetNode, ownedReferences);
        var ownerId = new EntityId("entity.ability-owner");
        var abilityId = new EntityId("entity.ability-child");
        var owner = new ProbeOwner("AbilityOwner", ownerId);
        var ability = new AbilityEntity { Name = "AbilityChild" };

        AddChild(owner);
        AddChild(ability);
        InitializeAbility(ability, abilityId, "Dash", AbilityTriggerMode.Manual, AbilityType.Active);

        AssertTrue("owner register", registry.Register(ownerId, owner));
        AssertTrue("ability register", registry.Register(abilityId, ability));
        AssertTrue("attach 应成功", service.Attach(owner, ability));
        AssertFalse("重复 attach 不应重复写入", service.Attach(owner, ability));

        var relationshipIds = EntityRelationshipManager.GetChildEntitiesByParentAndType(
            ownerId.Value,
            LegacyAbilityRelationshipType
        ).ToArray();

        AssertEqual("不应写旧 ENTITY_TO_ABILITY relationship", 0, relationshipIds.Length);
        AssertEqual("ability owner projection", ownerId.Value, ability.Data.Get(GeneratedDataKey.AbilityOwnerEntityId));
        AssertEqual("owner list projection count", 1, EntityIdList.FromStringArray(owner.Data.Get(GeneratedDataKey.OwnedAbilityIds)).Count);
        AssertEqual("service query count", 1, service.GetAbilities(owner).Count);
        AssertEqual("service query ability", ability, service.GetAbilities(owner)[0]);
        AssertEqual("manual query count", 1, service.GetManualAbilities(owner).Count);
        AssertEqual("owner resolve", owner, service.GetOwner(ability));
    }

    private void AbilityInventoryService_Detach_ShouldClearOwnerProjectionAndList()
    {
        var registry = new EntityRegistry();
        var ownedReferences = new OwnedReferenceRegistry(registry.GetNode);
        var service = new AbilityInventoryService(registry.GetNode, ownedReferences);
        var ownerId = new EntityId("entity.detach-owner");
        var abilityId = new EntityId("entity.detach-ability");
        var owner = new ProbeOwner("DetachOwner", ownerId);
        var ability = new AbilityEntity { Name = "DetachAbility" };

        AddChild(owner);
        AddChild(ability);
        InitializeAbility(ability, abilityId, "Slam", AbilityTriggerMode.Manual, AbilityType.Active);

        AssertTrue("owner register", registry.Register(ownerId, owner));
        AssertTrue("ability register", registry.Register(abilityId, ability));
        AssertTrue("attach", service.Attach(owner, ability));
        AssertTrue("detach 应成功", service.Detach(ability));

        AssertEqual("owner projection 应清空", string.Empty, ability.Data.Get(GeneratedDataKey.AbilityOwnerEntityId));
        AssertEqual("owner list 应移除 ability", 0, EntityIdList.FromStringArray(owner.Data.Get(GeneratedDataKey.OwnedAbilityIds)).Count);
        AssertEqual("service query 应为空", 0, service.GetAbilities(owner).Count);
    }

    private void AbilityInventoryService_DestroyCleanup_ShouldRemoveAbilityFromOwnerList()
    {
        var registry = new EntityRegistry();
        var ownedReferences = new OwnedReferenceRegistry(registry.GetNode);
        var service = new AbilityInventoryService(registry.GetNode, ownedReferences);
        var lifecycleTree = new LifecycleTree();
        var destroyPipeline = new EntityDestroyPipeline(
            registry,
            lifecycleTree,
            ownedReferences.CleanupDestroyedChild
        );
        var ownerId = new EntityId("entity.cleanup-owner");
        var abilityId = new EntityId("entity.cleanup-ability");
        var owner = new ProbeOwner("CleanupOwner", ownerId);
        var ability = new AbilityEntity { Name = "CleanupAbility" };

        AddChild(owner);
        AddChild(ability);
        InitializeAbility(ability, abilityId, "Lightning", AbilityTriggerMode.Manual, AbilityType.Active);

        AssertTrue("owner register", registry.Register(ownerId, owner));
        AssertTrue("ability register", registry.Register(abilityId, ability));
        AssertTrue("attach", service.Attach(owner, ability));

        var result = destroyPipeline.Destroy(ability);

        AssertTrue("ability destroy 应成功", result.Destroyed);
        AssertEqual("owner list 应自动 cleanup", 0, EntityIdList.FromStringArray(owner.Data.Get(GeneratedDataKey.OwnedAbilityIds)).Count);
        AssertEqual("owner 不应被 destroy cleanup 销毁", ownerId, registry.GetEntityId(owner));
    }

    private static void InitializeAbility(
        AbilityEntity ability,
        EntityId abilityId,
        string abilityName,
        AbilityTriggerMode triggerMode,
        AbilityType abilityType)
    {
        ability.Data.Set(GeneratedDataKey.Id, abilityId.Value);
        ability.Data.Set(GeneratedDataKey.Name, abilityName);
        ability.Data.Set(GeneratedDataKey.AbilityOwnerEntityId, string.Empty);
        ability.Data.Set(GeneratedDataKey.AbilityTriggerMode, triggerMode);
        ability.Data.Set(GeneratedDataKey.AbilityType, abilityType);
        ability.Data.Set(GeneratedDataKey.FeatureEnabled, true);
        ability.Data.Set(GeneratedDataKey.FeatureIsActive, false);
    }

    private void AssertTrue(string message, bool condition)
    {
        if (condition)
        {
            Pass(message);
            return;
        }

        Fail(message);
    }

    private void AssertFalse(string message, bool condition) => AssertTrue(message, !condition);

    private void AssertEqual<T>(string message, T expected, T actual)
    {
        if (Equals(expected, actual))
        {
            Pass(message);
            return;
        }

        Fail($"{message}: expected={expected}, actual={actual}");
    }

    private void Pass(string message)
    {
        _passedCount++;
        _log.Info($"[PASS] {message}");
    }

    private void Fail(string message)
    {
        _failedCount++;
        _log.Error($"[FAIL] {message}");
    }

    private sealed partial class ProbeOwner : Node, IEntity
    {
        public ProbeOwner(string name, EntityId entityId)
        {
            Name = name;
            Data = new Data(this);
            Data.Set(GeneratedDataKey.Id, entityId.Value);
            Data.Set(GeneratedDataKey.Name, name);
            Data.Set(GeneratedDataKey.OwnedAbilityIds, Array.Empty<string>());
        }

        public Data Data { get; private set; }
        public EventBus Events { get; } = new EventBus();
    }
}
