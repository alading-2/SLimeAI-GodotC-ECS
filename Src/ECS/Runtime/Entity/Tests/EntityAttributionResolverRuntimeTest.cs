using Godot;
using System;

namespace Slime.Test.Entity;

/// <summary>
/// Entity 归因解析运行时契约测试。
/// </summary>
public partial class EntityAttributionResolverRuntimeTest : Node
{
    private static readonly Log _log = new(nameof(EntityAttributionResolverRuntimeTest));

    private int _passedCount;
    private int _failedCount;

    public override void _Ready()
    {
        _log.Info("开始 EntityAttributionResolver 运行时测试");

        try
        {
            ResolveUnit_ShouldUseProjectileOwnerProjectionWithoutRelationship();
            ResolveChain_ShouldFollowSourceAndOriginProjection();
        }
        catch (Exception ex)
        {
            Fail($"测试过程中发生异常: {ex}");
        }

        _log.Info($"EntityAttributionResolver 运行时测试结束: PASS={_passedCount}, FAIL={_failedCount}");
        GetTree().Quit(_failedCount == 0 ? 0 : 1);
    }

    private void ResolveUnit_ShouldUseProjectileOwnerProjectionWithoutRelationship()
    {
        var owner = new ProbeUnit("ProjectileOwner", new EntityId("entity.projectile-owner"), Team.Player);
        var projectile = new ProbeEntity("Projectile", new EntityId("entity.projectile-child"), Team.Neutral, EntityType.Projectile);

        AddChild(owner);
        AddChild(projectile);
        EntityManager.Register(owner);
        EntityManager.Register(projectile);

        try
        {
            AssertTrue("projectile attach", ProjectileOwnershipService.Runtime.Attach(owner, projectile));

            var resolved = EntityAttributionResolver.ResolveUnit(projectile);

            AssertEqual("应通过 ProjectileOwnerEntityId 解析归属单位", owner, resolved);
            AssertFalse(
                "不应写旧 projectile relationship",
                EntityRelationshipManager.HasRelationship(
                    owner.Data.Get(GeneratedDataKey.Id),
                    projectile.Data.Get(GeneratedDataKey.Id),
                    "relationship.entity.projectile"));
        }
        finally
        {
            Cleanup(owner, projectile);
        }
    }

    private void ResolveChain_ShouldFollowSourceAndOriginProjection()
    {
        var owner = new ProbeUnit("OriginOwner", new EntityId("entity.origin-owner"), Team.Player);
        var weapon = new ProbeEntity("SourceWeapon", new EntityId("entity.source-weapon"), Team.Player, EntityType.Item);
        var projectile = new ProbeEntity("ChainedProjectile", new EntityId("entity.chained-projectile"), Team.Neutral, EntityType.Projectile);

        AddChild(owner);
        AddChild(weapon);
        AddChild(projectile);
        EntityManager.Register(owner);
        EntityManager.Register(weapon);
        EntityManager.Register(projectile);

        try
        {
            weapon.Data.Set(GeneratedDataKey.SourceEntityId, owner.Data.Get(GeneratedDataKey.Id));
            projectile.Data.Set(GeneratedDataKey.SourceEntityId, weapon.Data.Get(GeneratedDataKey.Id));
            projectile.Data.Set(GeneratedDataKey.OriginEntityId, owner.Data.Get(GeneratedDataKey.Id));

            var chain = EntityAttributionResolver.ResolveChain(projectile);

            AssertEqual("chain[0] 应是 projectile", projectile, chain[0]);
            AssertEqual("chain[1] 应是 weapon", weapon, chain[1]);
            AssertEqual("chain[2] 应是 owner", owner, chain[2]);
        }
        finally
        {
            Cleanup(owner, weapon, projectile);
        }
    }

    private static void Cleanup(params Node[] nodes)
    {
        foreach (var node in nodes)
        {
            EntityRelationshipManager.RemoveAllRelationships(node.GetInstanceId().ToString());
            if (node is IEntity entity)
                EntityRelationshipManager.RemoveAllRelationships(entity.Data.Get(GeneratedDataKey.Id));

            if (GodotObject.IsInstanceValid(node))
                node.QueueFree();
        }
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

    private sealed partial class ProbeEntity : Node2D, IEntity
    {
        public ProbeEntity(string name, EntityId entityId, Team team, EntityType entityType)
        {
            Name = name;
            Data.Set(GeneratedDataKey.Id, entityId.Value);
            Data.Set(GeneratedDataKey.Name, name);
            Data.Set(GeneratedDataKey.Team, team);
            Data.Set(GeneratedDataKey.EntityType, entityType);
        }

        public Data Data { get; } = new Data();
        public EventBus Events { get; } = new EventBus();
    }

    private sealed partial class ProbeUnit : Node2D, IUnit
    {
        public ProbeUnit(string name, EntityId entityId, Team team)
        {
            Name = name;
            Data.Set(GeneratedDataKey.Id, entityId.Value);
            Data.Set(GeneratedDataKey.Name, name);
            Data.Set(GeneratedDataKey.Team, team);
            Data.Set(GeneratedDataKey.EntityType, EntityType.Unit);
        }

        public Data Data { get; } = new Data();
        public EventBus Events { get; } = new EventBus();
        public int FactionId { get; set; }
    }
}
