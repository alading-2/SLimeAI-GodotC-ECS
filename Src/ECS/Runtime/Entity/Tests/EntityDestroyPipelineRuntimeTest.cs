using Godot;
using System;
using System.Collections.Generic;

namespace Slime.Test.Entity;

/// <summary>
/// EntityDestroyPipeline 运行时契约测试。
/// </summary>
public partial class EntityDestroyPipelineRuntimeTest : Node
{
    private static readonly Log _log = new(nameof(EntityDestroyPipelineRuntimeTest));

    private int _passedCount;
    private int _failedCount;

    public override void _Ready()
    {
        _log.Info("开始 EntityDestroyPipeline 运行时测试");

        try
        {
            EntityDestroyPipeline_Destroy_ShouldRecurseAndRespectDetachPolicy();
            EntityDestroyPipeline_Destroy_ShouldReturnAlreadyDestroyedOnRepeatCall();
            EntityDestroyPipeline_Destroy_ShouldUnregisterComponentBeforeDataAndEventsClear();
        }
        catch (Exception ex)
        {
            Fail($"测试过程中发生异常: {ex}");
        }

        _log.Info($"EntityDestroyPipeline 运行时测试结束: PASS={_passedCount}, FAIL={_failedCount}");
        GetTree().Quit(_failedCount == 0 ? 0 : 1);
    }

    private void EntityDestroyPipeline_Destroy_ShouldRecurseAndRespectDetachPolicy()
    {
        var registry = new EntityRegistry();
        var lifecycleTree = new LifecycleTree();
        var pipeline = new EntityDestroyPipeline(registry, lifecycleTree);

        var parentId = new EntityId("entity.parent");
        var recursiveChildAId = new EntityId("entity.recursive-a");
        var recursiveChildBId = new EntityId("entity.recursive-b");
        var detachedChildId = new EntityId("entity.detached");

        var parent = new ProbeEntity("Parent", parentId);
        var recursiveChildA = new ProbeEntity("RecursiveA", recursiveChildAId);
        var recursiveChildB = new ProbeEntity("RecursiveB", recursiveChildBId);
        var detachedChild = new ProbeEntity("Detached", detachedChildId);

        AddChild(parent);
        AddChild(recursiveChildA);
        AddChild(recursiveChildB);
        AddChild(detachedChild);

        AssertTrue("parent register", registry.Register(parentId, parent));
        AssertTrue("recursive child A register", registry.Register(recursiveChildAId, recursiveChildA));
        AssertTrue("recursive child B register", registry.Register(recursiveChildBId, recursiveChildB));
        AssertTrue("detached child register", registry.Register(detachedChildId, detachedChild));

        AssertTrue("parent -> recursive child A attach", lifecycleTree.Attach(parentId, recursiveChildAId, ParentDestroyPolicy.DestroyRecursively));
        AssertTrue("parent -> recursive child B attach", lifecycleTree.Attach(parentId, recursiveChildBId, ParentDestroyPolicy.DestroyRecursively));
        AssertTrue("parent -> detached child attach", lifecycleTree.Attach(parentId, detachedChildId, ParentDestroyPolicy.Detach));

        var result = pipeline.Destroy(parent);

        AssertTrue("首次销毁应成功", result.Destroyed);
        AssertFalse("首次销毁不应标记为重复", result.AlreadyDestroyed);
        AssertEqual("父实体应从 registry 移除", EntityId.Empty, registry.GetEntityId(parent));
        AssertEqual("递归子实体 A 应被销毁", EntityId.Empty, registry.GetEntityId(recursiveChildA));
        AssertEqual("递归子实体 B 应被销毁", EntityId.Empty, registry.GetEntityId(recursiveChildB));
        AssertEqual("detach 子实体应存活", detachedChildId, registry.GetEntityId(detachedChild));
        AssertEqual("detach 子实体 parent 应已断开", EntityId.Empty, lifecycleTree.GetParent(detachedChildId));
        AssertEqual("parent children 应为空", 0, lifecycleTree.GetChildren(parentId).Count);
        AssertEqual("detach child 不应被销毁", false, detachedChild.WasDestroyed);
    }

    private void EntityDestroyPipeline_Destroy_ShouldReturnAlreadyDestroyedOnRepeatCall()
    {
        var registry = new EntityRegistry();
        var lifecycleTree = new LifecycleTree();
        var pipeline = new EntityDestroyPipeline(registry, lifecycleTree);

        var entityId = new EntityId("entity.repeat");
        var entity = new ProbeEntity("Repeat", entityId);

        AddChild(entity);

        AssertTrue("register repeat entity", registry.Register(entityId, entity));

        var first = pipeline.Destroy(entity);
        var second = pipeline.Destroy(entity);

        AssertTrue("首次销毁应成功", first.Destroyed);
        AssertFalse("首次销毁不应标记为重复", first.AlreadyDestroyed);
        AssertFalse("重复销毁不应再次成功", second.Destroyed);
        AssertTrue("重复销毁应返回 already destroyed", second.AlreadyDestroyed);
    }

    private void EntityDestroyPipeline_Destroy_ShouldUnregisterComponentBeforeDataAndEventsClear()
    {
        var sequence = new List<string>();
        var registry = new EntityRegistry();
        var lifecycleTree = new LifecycleTree();
        var entityId = new EntityId("entity.component-order");
        var pipeline = new EntityDestroyPipeline(
            registry,
            lifecycleTree,
            ownerCleanup: id =>
            {
                if (id == entityId)
                {
                    sequence.Add($"owner-cleanup:{id.Value}");
                }
            }
        );

        var entity = new ProbeEntity("ComponentOrder", entityId);
        var component = new ProbeComponent(sequence);

        AddChild(entity);
        entity.AddChild(component);

        entity.Data.Set(GeneratedDataKey.Id, entityId.Value);
        component.OnComponentRegistered(entity);

        AssertTrue("register entity", registry.Register(entityId, entity));

        var result = pipeline.Destroy(entity);

        AssertTrue("销毁应成功", result.Destroyed);
        AssertEqual("owner cleanup 应先于 component unregister", $"owner-cleanup:{entityId.Value}", sequence[0]);
        AssertEqual("component unregister 应在 owner cleanup 之后", "component-unregistered", sequence[1]);
        AssertEqual("component unregister 时应仍可读取 Data", entityId.Value, component.ObservedEntityIdAtUnregister);
        AssertEqual("component unregister 时应仍可触发 Events", 1, component.PulseCount);
        AssertEqual("component unregister 应仅执行一次", 1, component.UnregisterCount);
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
        public bool WasDestroyed { get; private set; }

        public override void _ExitTree()
        {
            WasDestroyed = true;
            base._ExitTree();
        }
    }

    private sealed partial class ProbeComponent : Node, IComponent
    {
        private readonly List<string> _sequence;
        private ProbeEntity? _owner;

        public ProbeComponent(List<string> sequence)
        {
            _sequence = sequence;
        }

        public string? ObservedEntityIdAtUnregister { get; private set; }
        public int PulseCount { get; private set; }
        public int UnregisterCount { get; private set; }

        public void OnComponentRegistered(Node entity)
        {
            _owner = entity as ProbeEntity;
            if (_owner != null)
            {
                _owner.Events.On<ProbePulseEvent>(OnPulse);
            }
        }

        public void OnComponentUnregistered()
        {
            UnregisterCount++;
            _sequence.Add("component-unregistered");

            if (_owner != null)
            {
                ObservedEntityIdAtUnregister = _owner.Data.Get<string>(GeneratedDataKey.Id);
                _owner.Events.Emit(new ProbePulseEvent());
            }
        }

        private void OnPulse(ProbePulseEvent _)
        {
            PulseCount++;
        }
    }

    private readonly record struct ProbePulseEvent;
}
