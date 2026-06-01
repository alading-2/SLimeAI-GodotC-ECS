using Godot;
using System;
using System.Collections.Generic;

namespace Slime.Test.Entity;

/// <summary>
/// EntitySpawnPipeline 运行时契约测试。
/// </summary>
public partial class EntitySpawnPipelineRuntimeTest : Node
{
    private static readonly Log _log = new(nameof(EntitySpawnPipelineRuntimeTest));

    private int _passedCount;
    private int _failedCount;

    public override void _Ready()
    {
        _log.Info("开始 EntitySpawnPipeline 运行时测试");

        try
        {
            EntitySpawnPipeline_Spawn_ShouldApplyDataRegisterComponentAndAttachLifecycle();
            EntitySpawnPipeline_Spawn_ShouldRollbackWhenDataApplyFails();
            EntitySpawnConfig_ShouldExposeLifecycleParentIdWithoutBusinessRelationFields();
        }
        catch (Exception ex)
        {
            Fail($"测试过程中发生异常: {ex}");
        }

        _log.Info($"EntitySpawnPipeline 运行时测试结束: PASS={_passedCount}, FAIL={_failedCount}");
        GetTree().Quit(_failedCount == 0 ? 0 : 1);
    }

    private void EntitySpawnPipeline_Spawn_ShouldApplyDataRegisterComponentAndAttachLifecycle()
    {
        var sequence = new List<string>();
        var registry = new EntityRegistry();
        var lifecycleTree = new LifecycleTree();
        var componentRegistrar = new ComponentRegistrar(registry);
        var pipeline = new EntitySpawnPipeline(registry, lifecycleTree, componentRegistrar);
        var parent = new ProbeEntity("SpawnParent");
        var parentId = new EntityId("entity.spawn-parent");
        var component = new ProbeComponent(sequence);
        var record = BuildRecord("entity.spawn-child", "SpawnChild");

        AddChild(parent);

        AssertTrue("parent registry register", registry.Register(parentId, parent));

        var result = pipeline.Spawn(new EntitySpawnRequest<ProbeEntity>
        {
            CreateNode = () =>
            {
                sequence.Add("create");
                var entity = new ProbeEntity("SpawnChild");
                entity.AddChild(component);
                return entity;
            },
            Config = record,
            RuntimeDataBootstrap = DataRuntimeBootstrap.Default,
            RuntimeDataRecord = record,
            EntityId = new EntityId("entity.spawn-child"),
            LifecycleParentId = parentId,
            ParentDestroyPolicy = ParentDestroyPolicy.Detach,
            AddToSceneTree = node => AddChild(node)
        });

        AssertTrue("spawn 应成功", result.Success);
        AssertEqual("spawn node 类型", typeof(ProbeEntity), result.Node?.GetType());
        AssertEqual("spawn id", new EntityId("entity.spawn-child"), result.EntityId);
        AssertEqual("Data 应写入 GeneratedDataKey.Id", "entity.spawn-child", result.Node!.Data.Get<string>(GeneratedDataKey.Id));
        AssertEqual("registry 应可反查 node", result.Node, registry.GetNode(result.EntityId));
        AssertEqual("lifecycle parent 应连接", parentId, lifecycleTree.GetParent(result.EntityId));
        AssertEqual("lifecycle policy 应写入", ParentDestroyPolicy.Detach, lifecycleTree.GetLink(result.EntityId)!.Value.DestroyPolicy);
        AssertEqual("component 应注册到 owner", result.Node, componentRegistrar.GetEntityByComponent(component));
        AssertEqual("component callback 应执行一次", 1, component.RegisterCount);
    }

    private void EntitySpawnPipeline_Spawn_ShouldRollbackWhenDataApplyFails()
    {
        var registry = new EntityRegistry();
        var lifecycleTree = new LifecycleTree();
        var componentRegistrar = new ComponentRegistrar(registry);
        var pipeline = new EntitySpawnPipeline(registry, lifecycleTree, componentRegistrar);
        var created = new ProbeEntity("Rollback");

        var result = pipeline.Spawn(new EntitySpawnRequest<ProbeEntity>
        {
            CreateNode = () => created,
            Config = new object(),
            RuntimeDataBootstrap = DataRuntimeBootstrap.Default,
            EntityId = new EntityId("entity.rollback"),
            AddToSceneTree = node => AddChild(node)
        });

        AssertFalse("Data record 缺失时 spawn 应失败", result.Success);
        AssertEqual("失败不应注册 entity", EntityId.Empty, registry.GetEntityId(created));
        AssertTrue("失败应释放或退出场景树", created.IsQueuedForDeletion() || created.GetParent() == null);
    }

    private void EntitySpawnConfig_ShouldExposeLifecycleParentIdWithoutBusinessRelationFields()
    {
        var configType = typeof(EntitySpawnConfig);

        AssertTrue("LifecycleParentId 字段应存在", configType.GetProperty("LifecycleParentId") != null);
        AssertTrue("ParentDestroyPolicy 字段应保留", configType.GetProperty("ParentDestroyPolicy") != null);
        AssertEqual("ParentEntity 字段应删除", null, configType.GetProperty("ParentEntity"));
        AssertEqual("AutoAddParentRelation 字段应删除", null, configType.GetProperty("AutoAddParentRelation"));
        AssertEqual("ParentRelationTypes 字段应删除", null, configType.GetProperty("ParentRelationTypes"));
    }

    private static RuntimeDataRecordDto BuildRecord(string id, string name)
    {
        return new RuntimeDataRecordDto
        {
            Table = "runtime.test.entity",
            Id = id,
            Name = name,
            Fields = new Dictionary<string, RuntimeDataFieldDto>
            {
                [GeneratedDataKey.Name.StableKey] = new() { Type = "string", Value = name },
                [GeneratedDataKey.EntityType.StableKey] = new() { Type = "enum", Value = nameof(EntityType.Projectile) }
            }
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
        _log.Info($"[PASS] {message}");
    }

    private void Fail(string message)
    {
        _failedCount++;
        _log.Error($"[FAIL] {message}");
    }

    private sealed partial class ProbeEntity : Node, IEntity
    {
        public ProbeEntity(string name)
        {
            Name = name;
            Data = new Data(this);
        }

        public Data Data { get; private set; }
        public EventBus Events { get; } = new EventBus();
    }

    private sealed partial class ProbeComponent : Node, IComponent
    {
        private readonly List<string> _sequence;

        public ProbeComponent(List<string> sequence)
        {
            _sequence = sequence;
            Name = "ProbeComponent";
        }

        public int RegisterCount { get; private set; }

        public void OnComponentRegistered(Node entity)
        {
            _sequence.Add("component-registered");
            RegisterCount++;
        }

        public void OnComponentUnregistered()
        {
        }
    }
}
