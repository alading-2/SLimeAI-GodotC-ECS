using Godot;
using System;

namespace Slime.Test.Entity;

/// <summary>
/// ComponentRegistrar 运行时契约测试。
/// </summary>
public partial class ComponentRegistrarRuntimeTest : Node
{
    private static readonly Log _log = new(nameof(ComponentRegistrarRuntimeTest));

    private int _passedCount;
    private int _failedCount;

    public override void _Ready()
    {
        _log.Info("开始 ComponentRegistrar 运行时测试");

        try
        {
            ComponentRegistrar_RegisterComponents_ShouldIndexOwnerAndInvokeCallbacks();
            ComponentRegistrar_RemoveComponent_ShouldUnregisterAndClearOwnerIndex();
            ComponentRegistrar_UnregisterComponents_ShouldUseInternalIndexNotRelationshipGraph();
        }
        catch (Exception ex)
        {
            Fail($"测试过程中发生异常: {ex}");
        }

        _log.Info($"ComponentRegistrar 运行时测试结束: PASS={_passedCount}, FAIL={_failedCount}");
        GetTree().Quit(_failedCount == 0 ? 0 : 1);
    }

    private void ComponentRegistrar_RegisterComponents_ShouldIndexOwnerAndInvokeCallbacks()
    {
        var registry = new EntityRegistry();
        var registrar = new ComponentRegistrar(registry);
        var entityId = new EntityId("entity.component-owner");
        var entity = new ProbeEntity("ComponentOwner", entityId);
        var component = new ProbeComponent("MovementComponent");

        AddChild(entity);
        entity.AddChild(component);

        AssertTrue("entity register", registry.Register(entityId, entity));

        var result = registrar.RegisterComponents(entity);

        AssertEqual("应注册一个 component", 1, result);
        AssertEqual("component registered 回调应执行一次", 1, component.RegisterCount);
        AssertEqual("component owner 应为 entity", entity, component.OwnerAtRegister);
        AssertEqual("GetComponent 应返回 component", component, registrar.GetComponent<ProbeComponent>(entity));
        AssertEqual("GetEntityByComponent 应返回 owner", entity, registrar.GetEntityByComponent(component));
        AssertEqual("GetComponentsByType 应包含 component", 1, Count(registrar.GetComponentsByType<ProbeComponent>()));
    }

    private void ComponentRegistrar_RemoveComponent_ShouldUnregisterAndClearOwnerIndex()
    {
        var registry = new EntityRegistry();
        var registrar = new ComponentRegistrar(registry);
        var entityId = new EntityId("entity.component-remove");
        var entity = new ProbeEntity("ComponentRemove", entityId);
        var component = new ProbeComponent("RemovableComponent");

        AddChild(entity);
        entity.AddChild(component);

        AssertTrue("entity register", registry.Register(entityId, entity));
        AssertEqual("register count", 1, registrar.RegisterComponents(entity));

        AssertTrue("remove component 应成功", registrar.RemoveComponent(entity, component));
        AssertEqual("component unregister 回调应执行一次", 1, component.UnregisterCount);
        AssertEqual("component owner index 应清空", null, registrar.GetEntityByComponent(component));
        AssertEqual("entity component index 应清空", null, registrar.GetComponent<ProbeComponent>(entity));
        AssertEqual("type index 应清空", 0, Count(registrar.GetComponentsByType<ProbeComponent>()));
    }

    private void ComponentRegistrar_UnregisterComponents_ShouldUseInternalIndexNotRelationshipGraph()
    {
        var registry = new EntityRegistry();
        var registrar = new ComponentRegistrar(registry);
        var entityId = new EntityId("entity.component-internal-index");
        var entity = new ProbeEntity("InternalIndex", entityId);
        var component = new ProbeComponent("RelationshipFreeComponent");
        var componentId = component.GetInstanceId().ToString();

        AddChild(entity);
        entity.AddChild(component);

        AssertTrue("entity register", registry.Register(entityId, entity));
        AssertEqual("register count", 1, registrar.RegisterComponents(entity));

        EntityRelationshipManager.RemoveAllRelationships(entityId.Value);
        EntityRelationshipManager.RemoveAllRelationships(componentId);

        var removed = registrar.UnregisterComponents(entity);

        AssertEqual("relationship 清空后仍应注销 component", 1, removed);
        AssertEqual("unregister 回调应执行一次", 1, component.UnregisterCount);
        AssertEqual("owner index 应清空", null, registrar.GetEntityByComponent(component));
    }

    private static int Count<T>(System.Collections.Generic.IEnumerable<T> values)
    {
        var count = 0;
        foreach (var _ in values)
        {
            count++;
        }

        return count;
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
        public ProbeEntity(string name, EntityId entityId)
        {
            Name = name;
            Data = new Data(this);
            Data.Set(GeneratedDataKey.Id, entityId.Value);
        }

        public Data Data { get; private set; }
        public EventBus Events { get; } = new EventBus();
    }

    private sealed partial class ProbeComponent : Node, IComponent
    {
        public ProbeComponent(string name)
        {
            Name = name;
        }

        public int RegisterCount { get; private set; }
        public int UnregisterCount { get; private set; }
        public Node? OwnerAtRegister { get; private set; }

        public void OnComponentRegistered(Node entity)
        {
            RegisterCount++;
            OwnerAtRegister = entity;
        }

        public void OnComponentUnregistered()
        {
            UnregisterCount++;
        }
    }
}
