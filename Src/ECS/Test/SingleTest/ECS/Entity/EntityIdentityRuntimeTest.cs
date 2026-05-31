using Godot;
using System;

namespace Slime.Test.Entity;

/// <summary>
/// Entity identity 运行时契约测试。
/// </summary>
public partial class EntityIdentityRuntimeTest : Node
{
    private static readonly Log _log = new(nameof(EntityIdentityRuntimeTest));

    private int _passedCount;
    private int _failedCount;

    public override void _Ready()
    {
        _log.Info("开始 Entity identity 运行时测试");

        try
        {
            EntityId_Empty_ShouldRepresentNoReference();
            EntityId_New_ShouldCreateStableNonEmptyId();
            EntityId_From_ShouldNormalizeEmptyInput();
            EntityRegistry_Register_ShouldRejectInvalidOrDuplicateEntries();
            EntityRegistry_GetEntityId_ShouldReturnEmptyWhenMissing();
            EntityRegistry_Snapshot_ShouldReturnCopy();
        }
        catch (Exception ex)
        {
            Fail($"测试过程中发生异常: {ex}");
        }

        _log.Info($"Entity identity 运行时测试结束: PASS={_passedCount}, FAIL={_failedCount}");
        GetTree().Quit(_failedCount == 0 ? 0 : 1);
    }

    private void EntityId_Empty_ShouldRepresentNoReference()
    {
        AssertTrue("Empty 应表达空引用", EntityId.Empty.IsEmpty);
        AssertEqual("Empty.Value 应为空字符串", string.Empty, EntityId.Empty.Value);
    }

    private void EntityId_New_ShouldCreateStableNonEmptyId()
    {
        var id = EntityId.New();
        var firstRead = id.Value;
        var secondRead = id.Value;

        AssertFalse("New 不应返回空引用", id.IsEmpty);
        AssertFalse("New.Value 不应为空", string.IsNullOrWhiteSpace(id.Value));
        AssertEqual("EntityId value 应稳定", firstRead, secondRead);
    }

    private void EntityId_From_ShouldNormalizeEmptyInput()
    {
        AssertEqual("null 应转为空引用", EntityId.Empty, EntityId.From(null));
        AssertEqual("空白字符串应转为空引用", EntityId.Empty, EntityId.From("   "));
        AssertEqual("显式字符串应保留", new EntityId("unit.player"), EntityId.From("unit.player"));
    }

    private void EntityRegistry_Register_ShouldRejectInvalidOrDuplicateEntries()
    {
        var registry = new EntityRegistry();
        var first = new Node { Name = "FirstEntity" };
        var second = new Node { Name = "SecondEntity" };
        var firstId = new EntityId("entity.first");
        var secondId = new EntityId("entity.second");

        AssertFalse("empty id 注册应被拒绝", registry.Register(EntityId.Empty, first));
        AssertTrue("首次注册应成功", registry.Register(firstId, first));
        AssertFalse("重复 id 注册应被拒绝", registry.Register(firstId, second));
        AssertFalse("重复 node 注册应被拒绝", registry.Register(secondId, first));

        first.QueueFree();
        second.QueueFree();
    }

    private void EntityRegistry_GetEntityId_ShouldReturnEmptyWhenMissing()
    {
        var registry = new EntityRegistry();
        var node = new Node { Name = "MissingEntity" };

        AssertEqual("未注册 node 应返回 EntityId.Empty", EntityId.Empty, registry.GetEntityId(node));

        node.QueueFree();
    }

    private void EntityRegistry_Snapshot_ShouldReturnCopy()
    {
        var registry = new EntityRegistry();
        var node = new Node { Name = "SnapshotEntity" };
        var id = new EntityId("entity.snapshot");

        AssertTrue("注册测试实体", registry.Register(id, node));
        var snapshot = registry.Snapshot();

        AssertEqual("snapshot 应包含注册项", 1, snapshot.Count);
        AssertTrue("注销应成功", registry.Unregister(id));
        AssertEqual("snapshot 不应随 registry 后续变化而变化", 1, snapshot.Count);
        AssertEqual("registry 注销后应查不到 node", EntityId.Empty, registry.GetEntityId(node));

        node.QueueFree();
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
}
