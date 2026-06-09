using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Slime.Test.Entity;

/// <summary>
/// OwnedReferenceRegistry 运行时契约测试。
/// </summary>
public partial class OwnedReferenceRegistryRuntimeTest : Node
{
    private static readonly Log _log = new(nameof(OwnedReferenceRegistryRuntimeTest));
    private static readonly DataKey<string> OwnerEntityKey = new("Test.OwnerEntityId");
    private static readonly DataKey<string[]> OwnedEntityIdsKey = new("Test.OwnedEntityIds");

    private int _passedCount;
    private int _failedCount;

    public override void _Ready()
    {
        _log.Info("开始 OwnedReferenceRegistry 运行时测试");

        try
        {
            EntityIdList_AddRemove_ShouldDeduplicateAndStayImmutable();
            OwnedReferenceRegistry_Register_ShouldStoreDescriptorOnce();
            OwnedReferenceRegistry_AddReference_ShouldProjectTypedIdsToData();
            OwnedReferenceRegistry_CleanupDestroyedChild_ShouldRemoveChildFromOwnerList();
            OwnedReferenceRegistry_DestroyOwner_ShouldNotDestroyChildrenByOwnerList();
        }
        catch (Exception ex)
        {
            Fail($"测试过程中发生异常: {ex}");
        }

        _log.Info($"OwnedReferenceRegistry 运行时测试结束: PASS={_passedCount}, FAIL={_failedCount}");
        GetTree().Quit(_failedCount == 0 ? 0 : 1);
    }

    private void EntityIdList_AddRemove_ShouldDeduplicateAndStayImmutable()
    {
        var childA = new EntityId("entity.child-a");
        var childB = new EntityId("entity.child-b");

        var empty = EntityIdList.Empty;
        var withChildren = empty.Add(childA).Add(childA).Add(EntityId.Empty).Add(childB);
        var withoutA = withChildren.Remove(childA);

        AssertEqual("empty list 应保持不变", 0, empty.Count);
        AssertEqual("Add 应去重并忽略 Empty", 2, withChildren.Count);
        AssertTrue("list 应包含 child A", withChildren.Contains(childA));
        AssertTrue("list 应包含 child B", withChildren.Contains(childB));
        AssertEqual("Remove 后新 list 只剩 child B", 1, withoutA.Count);
        AssertFalse("Remove 不应修改原 list", withoutA.Contains(childA));
        AssertTrue("原 list 仍应包含 child A", withChildren.Contains(childA));
        AssertEqual("string_array projection 顺序稳定", "entity.child-b", withoutA.ToStringArray()[0]);
    }

    private void OwnedReferenceRegistry_Register_ShouldStoreDescriptorOnce()
    {
        var registry = new OwnedReferenceRegistry();
        var descriptor = new OwnedReferenceDescriptor(OwnerEntityKey, OwnedEntityIdsKey);

        AssertTrue("首次注册 descriptor 应成功", registry.Register(descriptor));
        AssertFalse("重复注册 descriptor 应返回 false", registry.Register(descriptor));
        AssertEqual("descriptor 数量应为 1", 1, registry.Descriptors.Count);
    }

    private void OwnedReferenceRegistry_AddReference_ShouldProjectTypedIdsToData()
    {
        var entityRegistry = new EntityRegistry();
        var registry = new OwnedReferenceRegistry(entityRegistry.GetNode);
        var descriptor = new OwnedReferenceDescriptor(OwnerEntityKey, OwnedEntityIdsKey);
        var catalog = BuildCatalog();
        var ownerId = new EntityId("entity.owner");
        var childId = new EntityId("entity.child");
        var owner = new ProbeEntity("Owner", ownerId, catalog);
        var child = new ProbeEntity("Child", childId, catalog);

        AddChild(owner);
        AddChild(child);
        AssertTrue("owner register", entityRegistry.Register(ownerId, owner));
        AssertTrue("child register", entityRegistry.Register(childId, child));

        AssertTrue("add reference 应成功", registry.AddReference(owner, child, descriptor));
        AssertFalse("重复 add reference 不应重复写入 owner list", registry.AddReference(owner, child, descriptor));

        AssertEqual("child -> owner projection", ownerId.Value, child.Data.Get(OwnerEntityKey));
        var ownerList = EntityIdList.FromStringArray(owner.Data.Get(OwnedEntityIdsKey));
        AssertEqual("owner list 应只有一个 child", 1, ownerList.Count);
        AssertTrue("owner list 应包含 child id", ownerList.Contains(childId));
    }

    private void OwnedReferenceRegistry_CleanupDestroyedChild_ShouldRemoveChildFromOwnerList()
    {
        var entityRegistry = new EntityRegistry();
        var referenceRegistry = new OwnedReferenceRegistry(entityRegistry.GetNode);
        var lifecycleTree = new LifecycleTree();
        var descriptor = new OwnedReferenceDescriptor(OwnerEntityKey, OwnedEntityIdsKey);
        var catalog = BuildCatalog();
        var ownerId = new EntityId("entity.cleanup-owner");
        var childId = new EntityId("entity.cleanup-child");
        var owner = new ProbeEntity("CleanupOwner", ownerId, catalog);
        var child = new ProbeEntity("CleanupChild", childId, catalog);
        var destroyPipeline = new EntityDestroyPipeline(
            entityRegistry,
            lifecycleTree,
            referenceRegistry.CleanupDestroyedChild
        );

        AddChild(owner);
        AddChild(child);
        AssertTrue("owner register", entityRegistry.Register(ownerId, owner));
        AssertTrue("child register", entityRegistry.Register(childId, child));
        AssertTrue("add reference", referenceRegistry.AddReference(owner, child, descriptor));

        var result = destroyPipeline.Destroy(child);

        AssertTrue("child destroy 应成功", result.Destroyed);
        AssertEqual("child 应从 owner list 自动移除", 0, EntityIdList.FromStringArray(owner.Data.Get(OwnedEntityIdsKey)).Count);
        AssertEqual("owner 不应被 cleanup 销毁", ownerId, entityRegistry.GetEntityId(owner));
    }

    private void OwnedReferenceRegistry_DestroyOwner_ShouldNotDestroyChildrenByOwnerList()
    {
        var entityRegistry = new EntityRegistry();
        var referenceRegistry = new OwnedReferenceRegistry(entityRegistry.GetNode);
        var lifecycleTree = new LifecycleTree();
        var descriptor = new OwnedReferenceDescriptor(OwnerEntityKey, OwnedEntityIdsKey);
        var catalog = BuildCatalog();
        var ownerId = new EntityId("entity.owner-list-parent");
        var childId = new EntityId("entity.owner-list-child");
        var owner = new ProbeEntity("OwnerListParent", ownerId, catalog);
        var child = new ProbeEntity("OwnerListChild", childId, catalog);
        var destroyPipeline = new EntityDestroyPipeline(
            entityRegistry,
            lifecycleTree,
            referenceRegistry.CleanupDestroyedChild
        );

        AddChild(owner);
        AddChild(child);
        AssertTrue("owner register", entityRegistry.Register(ownerId, owner));
        AssertTrue("child register", entityRegistry.Register(childId, child));
        AssertTrue("add reference", referenceRegistry.AddReference(owner, child, descriptor));

        var result = destroyPipeline.Destroy(owner);

        AssertTrue("owner destroy 应成功", result.Destroyed);
        AssertEqual("owner list 不应作为 lifecycle children 销毁 child", childId, entityRegistry.GetEntityId(child));
        AssertFalse("child 不应退出场景树", child.WasDestroyed);
    }

    private static DataDefinitionCatalog BuildCatalog()
    {
        var catalog = new DataDefinitionCatalog();
        catalog.Register(Definition(GeneratedDataKey.Id.StableKey, DataValueType.String, string.Empty));
        catalog.Register(Definition(OwnerEntityKey.StableKey, DataValueType.String, string.Empty));
        catalog.Register(Definition(OwnedEntityIdsKey.StableKey, DataValueType.StringArray, Array.Empty<string>()));
        catalog.ValidateAndBuildIndexes();
        return catalog;
    }

    private static DataDefinition Definition(string stableKey, DataValueType valueType, object? defaultValue)
    {
        return new DataDefinition
        {
            StableKey = stableKey,
            ValueType = valueType,
            DefaultValue = defaultValue
        };
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
        _log.Info(message, outcome: LogOutcome.Succeeded, validationStatus: LogValidationStatus.Pass, channel: LogChannel.Validation);
    }

    private void Fail(string message)
    {
        _failedCount++;
        _log.Error(message, outcome: LogOutcome.Failed, validationStatus: LogValidationStatus.Fail, channel: LogChannel.Validation);
    }

    private sealed partial class ProbeEntity : Node, IEntity
    {
        public ProbeEntity(string name, EntityId entityId, DataDefinitionCatalog catalog)
        {
            Name = name;
            Data = new Data(this, catalog);
            Data.Set(GeneratedDataKey.Id, entityId.Value);
        }

        public Data Data { get; private set; }
        public EventBus Events { get; } = new EventBus();
        public bool WasDestroyed { get; private set; }

        public override void _ExitTree()
        {
            WasDestroyed = true;
            base._ExitTree();
        }
    }
}
