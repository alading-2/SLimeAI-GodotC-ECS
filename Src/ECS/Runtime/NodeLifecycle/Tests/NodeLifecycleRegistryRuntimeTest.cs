using Godot;
using System;

namespace Slime.Test.NodeLifecycle;

/// <summary>
/// NodeLifecycleRegistry 运行时契约测试。
/// </summary>
public partial class NodeLifecycleRegistryRuntimeTest : Node
{
    private static readonly Log _log = new(nameof(NodeLifecycleRegistryRuntimeTest));
    private int _passedCount;
    private int _failedCount;

    public override void _Ready()
    {
        try
        {
            NodeLifecycleRegistry_ShouldExposeOwnerSourceAndTypeSnapshot();
            NodeLifecycleRegistry_ShouldRejectDuplicateRegister();
            NodeLifecycleRegistry_ShouldUnregisterAndCleanupInvalidNodes();
        }
        catch (Exception ex)
        {
            Fail($"测试过程中发生异常: {ex}");
        }

        _log.Info($"NodeLifecycleRegistry 运行时测试结束: PASS={_passedCount}, FAIL={_failedCount}");
        GetTree().Quit(_failedCount == 0 ? 0 : 1);
    }

    private void NodeLifecycleRegistry_ShouldExposeOwnerSourceAndTypeSnapshot()
    {
        var registry = new NodeLifecycleRegistry();
        var entity = new ProbeNode("OwnerProbeEntity");
        var component = new ProbeComponentNode("OwnerProbeComponent");

        AddChild(entity);
        entity.AddChild(component);

        AssertTrue("entity register 应成功", registry.Register(entity, NodeLifecycleOwner.Entity("entity.owner-probe"), "unit-test"));
        AssertTrue("component register 应成功", registry.Register(component, NodeLifecycleOwner.Component("entity.owner-probe", "component.owner-probe"), "component-scan"));

        var snapshot = registry.GetSnapshot();

        AssertEqual("snapshot total", 2, snapshot.TotalCount);
        AssertEqual("Entity owner 计数", 1, snapshot.GetOwnerCount(NodeLifecycleOwnerKind.Entity));
        AssertEqual("Component owner 计数", 1, snapshot.GetOwnerCount(NodeLifecycleOwnerKind.Component));
        AssertEqual("Probe type 计数", 1, snapshot.GetTypeCount(nameof(ProbeNode)));
        AssertEqual("Component source 应可诊断", "component-scan", snapshot.GetEntry(component.GetInstanceId().ToString())?.Source ?? string.Empty);
    }

    private void NodeLifecycleRegistry_ShouldRejectDuplicateRegister()
    {
        var registry = new NodeLifecycleRegistry();
        var node = new ProbeNode("DuplicateProbe");

        AddChild(node);

        AssertTrue("首次注册应成功", registry.Register(node, NodeLifecycleOwner.Test("duplicate"), "first"));
        AssertFalse("重复注册应失败", registry.Register(node, NodeLifecycleOwner.Test("duplicate"), "second"));

        var snapshot = registry.GetSnapshot();
        AssertEqual("重复注册不应新增条目", 1, snapshot.TotalCount);
    }

    private void NodeLifecycleRegistry_ShouldUnregisterAndCleanupInvalidNodes()
    {
        var registry = new NodeLifecycleRegistry();
        var unregisterNode = new ProbeNode("UnregisterProbe");
        var invalidNode = new ProbeNode("InvalidProbe");

        AddChild(unregisterNode);
        AddChild(invalidNode);

        AssertTrue("待注销 node 注册", registry.Register(unregisterNode, NodeLifecycleOwner.Test("unregister"), "test"));
        AssertTrue("待 invalid node 注册", registry.Register(invalidNode, NodeLifecycleOwner.Test("invalid"), "test"));

        AssertTrue("Unregister 应成功", registry.Unregister(unregisterNode));
        invalidNode.QueueFree();

        var beforeCleanup = registry.GetSnapshot();
        AssertEqual("cleanup 前 invalid 应可诊断", 1, beforeCleanup.InvalidCount);

        var removed = registry.CleanupInvalid();
        var afterCleanup = registry.GetSnapshot();

        AssertEqual("CleanupInvalid 应移除一个 invalid node", 1, removed);
        AssertEqual("cleanup 后只剩 0 个注册 node", 0, afterCleanup.TotalCount);
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

    private void AssertFalse(string message, bool condition)
    {
        AssertTrue(message, !condition);
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

    private sealed partial class ProbeNode : Node
    {
        public ProbeNode(string name)
        {
            Name = name;
        }
    }

    private sealed partial class ProbeComponentNode : Node, IComponent
    {
        public ProbeComponentNode(string name)
        {
            Name = name;
        }

        public void OnComponentRegistered(Node entity)
        {
        }

        public void OnComponentUnregistered()
        {
        }
    }
}
