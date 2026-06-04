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
            EntitySpawnPipeline_Spawn_ShouldComposeProfileBeforeComponentRegistration();
            EntityManager_RegisterComponents_ShouldComposeProfileBeforeScan();
            EntitySpawnPipeline_ComponentProfiles_ShouldMatchLegacyPresetSets();
            EntitySpawnPipeline_UnitCoreProfile_ShouldInjectOrientationSink();
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

    private void EntitySpawnPipeline_Spawn_ShouldComposeProfileBeforeComponentRegistration()
    {
        var sequence = new List<string>();
        var registry = new EntityRegistry();
        var lifecycleTree = new LifecycleTree();
        var componentRegistrar = new ComponentRegistrar(registry);
        var pipeline = new EntitySpawnPipeline(registry, lifecycleTree, componentRegistrar);
        var record = BuildRecord("entity.composable", "Composable");

        var result = pipeline.Spawn(new EntitySpawnRequest<ProbeComposableEntity>
        {
            CreateNode = () => new ProbeComposableEntity("Composable", sequence),
            Config = record,
            RuntimeDataBootstrap = DataRuntimeBootstrap.Default,
            RuntimeDataRecord = record,
            EntityId = new EntityId("entity.composable"),
            AddToSceneTree = node => AddChild(node)
        });

        var component = result.Node?.GetNodeOrNull<ConfiguredProbeComponent>("Component/ConfiguredProbeComponent");

        AssertTrue("composition spawn 应成功", result.Success);
        AssertTrue("composition 应创建组件", component != null);
        AssertEqual("typed options 应在注册前注入", "configured", component?.ConfiguredValueAtRegister);
        AssertEqual("composition component 注册回调应执行一次", 1, component?.RegisterCount ?? 0);
        AssertEqual("composition component 应进入 owner index", result.Node, componentRegistrar.GetEntityByComponent(component));
        AssertEqual("composition 顺序应为 configure -> registered", "configure,component-registered", string.Join(",", sequence));
    }

    private void EntityManager_RegisterComponents_ShouldComposeProfileBeforeScan()
    {
        var sequence = new List<string>();
        var entity = new ProbeComposableEntity("DirectComposable", sequence);

        AddChild(entity);
        EntityManager.RegisterComponents(entity);

        var component = entity.GetNodeOrNull<ConfiguredProbeComponent>("Component/ConfiguredProbeComponent");

        AssertTrue("EntityManager.RegisterComponents 应创建代码化组件", component != null);
        if (component == null)
            return;

        AssertEqual("EntityManager typed options 应在注册前注入", "configured", component.ConfiguredValueAtRegister);
        AssertEqual("EntityManager composition component 注册回调应执行一次", 1, component.RegisterCount);
        AssertEqual("EntityManager composition component 应进入 owner index", entity, EntityManager.GetEntityByComponent(component));
        AssertEqual("EntityManager composition 顺序应为 configure -> registered", "configure,component-registered", string.Join(",", sequence));
    }

    private void EntitySpawnPipeline_ComponentProfiles_ShouldMatchLegacyPresetSets()
    {
        AssertProfile(
            "Player profile 应复刻 UnitCore + Player Preset",
            new PlayerEntity(),
            "HealthComponent",
            "LifecycleComponent",
            "UnitStateComponent",
            "RecoveryComponent",
            "DataInitComponent",
            "UnitAnimationComponent",
            "EntityMovementComponent",
            "EntityOrientationComponent",
            "CollisionComponent",
            "PickupComponent",
            "ActiveSkillInputComponent");

        AssertProfile(
            "Enemy profile 应复刻 Enemy + UnitCore Preset",
            new EnemyEntity(),
            "AIComponent",
            "AttackComponent",
            "HealthComponent",
            "LifecycleComponent",
            "UnitStateComponent",
            "RecoveryComponent",
            "DataInitComponent",
            "UnitAnimationComponent",
            "EntityMovementComponent",
            "EntityOrientationComponent",
            "CollisionComponent");

        AssertProfile(
            "TargetingIndicator profile 应复刻 UnitCore Preset",
            new TargetingIndicatorEntity(),
            "HealthComponent",
            "LifecycleComponent",
            "UnitStateComponent",
            "RecoveryComponent",
            "DataInitComponent",
            "UnitAnimationComponent",
            "EntityMovementComponent",
            "EntityOrientationComponent",
            "CollisionComponent");

        AssertProfile(
            "Ability profile 应复刻 Ability Preset",
            new AbilityEntity(),
            "TriggerComponent",
            "CooldownComponent",
            "ChargeComponent",
            "CostComponent");
    }

    private void AssertProfile(string message, Node entity, params string[] expectedNames)
    {
        if (entity is not IComponentCompositionProvider provider)
        {
            Fail($"{message}: entity 未实现 IComponentCompositionProvider");
            return;
        }

        var profile = provider.GetComponentCompositionProfile();
        var actualNames = new List<string>();
        foreach (var entry in profile.Entries)
        {
            actualNames.Add(entry.NodeName);
        }

        AssertEqual($"{message} 数量", expectedNames.Length, actualNames.Count);
        for (var i = 0; i < expectedNames.Length && i < actualNames.Count; i++)
        {
            AssertEqual($"{message} 第 {i + 1} 项", expectedNames[i], actualNames[i]);
        }
    }

    private void EntitySpawnPipeline_UnitCoreProfile_ShouldInjectOrientationSink()
    {
        var entity = new PlayerEntity();
        AddChild(entity);

        var composed = ComponentComposer.Compose(entity, UnitComponentCompositionProfiles.UnitCore());
        var orientation = entity.GetNodeOrNull<EntityOrientationComponent>("Component/EntityOrientationComponent");

        AssertEqual("UnitCore profile 应创建 9 个组件", 9, composed);
        AssertTrue("UnitCore profile 应创建朝向组件", orientation != null);
        AssertEqual(
            "UnitCore profile 应把朝向输出注入为 VisualFlipX",
            OrientationSink.VisualFlipX,
            orientation?.Sink ?? OrientationSink.RootRotation);
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

    private sealed partial class ProbeComposableEntity : Node, IEntity, IComponentCompositionProvider
    {
        private readonly List<string> _sequence;

        public ProbeComposableEntity(string name, List<string> sequence)
        {
            Name = name;
            _sequence = sequence;
            Data = new Data(this);
        }

        public Data Data { get; private set; }
        public EventBus Events { get; } = new EventBus();

        public ComponentCompositionProfile GetComponentCompositionProfile()
        {
            return new ComponentCompositionProfile(new[]
            {
                new ComponentCompositionEntry(
                    "ConfiguredProbeComponent",
                    () => new ConfiguredProbeComponent(_sequence),
                    component => ((ConfiguredProbeComponent)component).Configure(
                        new ConfiguredProbeComponentOptions("configured")))
            });
        }
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

    private sealed partial class ConfiguredProbeComponent : Node, IComponent
    {
        private readonly List<string> _sequence;
        private string _configuredValue = string.Empty;

        public ConfiguredProbeComponent(List<string> sequence)
        {
            _sequence = sequence;
            Name = "ConfiguredProbeComponent";
        }

        public int RegisterCount { get; private set; }
        public string ConfiguredValueAtRegister { get; private set; } = string.Empty;

        public void Configure(ConfiguredProbeComponentOptions options)
        {
            _configuredValue = options.Value;
            _sequence.Add("configure");
        }

        public void OnComponentRegistered(Node entity)
        {
            RegisterCount++;
            ConfiguredValueAtRegister = _configuredValue;
            _sequence.Add("component-registered");
        }

        public void OnComponentUnregistered()
        {
        }
    }

    private readonly record struct ConfiguredProbeComponentOptions(string Value);
}
