using Godot;
using System;
using System.Linq;

namespace Slime.Test.RuntimeMount;

/// <summary>
/// Runtime mount 注册表契约测试。
/// </summary>
public partial class RuntimeMountRegistryRuntimeTest : Node
{
    private static readonly Log _log = new(nameof(RuntimeMountRegistryRuntimeTest));
    private int _passedCount;
    private int _failedCount;

    public override void _Ready()
    {
        try
        {
            RuntimeMountRegistry_ShouldExposeDeferredRootPendingStatus();
            RuntimeMountRegistry_ShouldBeIdempotentForDuplicateMountId();
            RuntimeMountRegistry_ShouldReportInvalidMounts();
        }
        catch (Exception ex)
        {
            Fail($"测试过程中发生异常: {ex}");
        }

        _log.Info($"RuntimeMountRegistry 运行时测试结束: PASS={_passedCount}, FAIL={_failedCount}");
        GetTree().Quit(_failedCount == 0 ? 0 : 1);
    }

    private void RuntimeMountRegistry_ShouldExposeDeferredRootPendingStatus()
    {
        var registry = new RuntimeMountRegistry(GetTree().Root);

        var mount = registry.GetOrCreate(RuntimeMountIds.EntityRuntimeRoot);
        var snapshot = registry.GetSnapshot();
        var entry = snapshot.GetEntry(RuntimeMountIds.EntityRuntimeRoot);

        AssertEqual("mount 应创建节点", true, mount != null);
        AssertEqual("默认 runtime root 路径", "/root/SlimeAIRuntime/ECS/Entity", entry?.AbsolutePath ?? string.Empty);
        AssertEqual("root 下 deferred mount 应先是 Pending", RuntimeMountStatus.Pending, entry?.Status ?? RuntimeMountStatus.Invalid);
        AssertEqual("snapshot 应统计 pending", 1, snapshot.PendingCount);
        AssertEqual("snapshot 应记录 runtime root", "/root/SlimeAIRuntime", snapshot.RuntimeRootPath);
    }

    private void RuntimeMountRegistry_ShouldBeIdempotentForDuplicateMountId()
    {
        var registry = new RuntimeMountRegistry(this);

        var first = registry.GetOrCreate(RuntimeMountIds.PoolRuntimeRoot);
        var second = registry.GetOrCreate(RuntimeMountIds.PoolRuntimeRoot);
        var snapshot = registry.GetSnapshot();

        AssertEqual("重复 mount id 应返回同一节点", first, second);
        AssertEqual("重复 mount id 不应重复登记", 1, snapshot.Entries.Count(entry => entry.Id == RuntimeMountIds.PoolRuntimeRoot));
        AssertEqual("test root 下同步创建应在树中", RuntimeMountStatus.InTree, snapshot.GetEntry(RuntimeMountIds.PoolRuntimeRoot)?.Status);
    }

    private void RuntimeMountRegistry_ShouldReportInvalidMounts()
    {
        var registry = new RuntimeMountRegistry(this);
        var mount = registry.GetOrCreate(RuntimeMountIds.UiRuntimeRoot);

        mount.QueueFree();
        var snapshot = registry.GetSnapshot();

        AssertEqual("已排队释放的 mount 应被诊断为 Invalid", RuntimeMountStatus.Invalid, snapshot.GetEntry(RuntimeMountIds.UiRuntimeRoot)?.Status);
        AssertEqual("snapshot 应统计 invalid", 1, snapshot.InvalidCount);
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
}
