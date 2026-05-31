using Godot;
using System;

namespace Slime.Test.Entity;

/// <summary>
/// LifecycleTree 运行时契约测试。
/// </summary>
public partial class LifecycleTreeRuntimeTest : Node
{
    private static readonly Log _log = new(nameof(LifecycleTreeRuntimeTest));

    private int _passedCount;
    private int _failedCount;

    public override void _Ready()
    {
        _log.Info("开始 LifecycleTree 运行时测试");

        try
        {
            LifecycleTree_Attach_ShouldStoreParentChildrenAndPolicy();
            LifecycleTree_Attach_ShouldRejectInvalidLinks();
            LifecycleTree_Detach_ShouldClearBothSides();
            LifecycleTree_DetachAll_ShouldClearParentAndChildSides();
            LifecycleTree_GetChildren_ShouldReturnSnapshot();
        }
        catch (Exception ex)
        {
            Fail($"测试过程中发生异常: {ex}");
        }

        _log.Info($"LifecycleTree 运行时测试结束: PASS={_passedCount}, FAIL={_failedCount}");
        GetTree().Quit(_failedCount == 0 ? 0 : 1);
    }

    private void LifecycleTree_Attach_ShouldStoreParentChildrenAndPolicy()
    {
        var tree = new LifecycleTree();
        var parent = new EntityId("entity.parent");
        var child = new EntityId("entity.child");

        AssertTrue("attach 应成功", tree.Attach(parent, child, ParentDestroyPolicy.Detach));
        AssertEqual("child parent 应可查", parent, tree.GetParent(child));

        var children = tree.GetChildren(parent);
        AssertEqual("children 数量", 1, children.Count);
        AssertEqual("link parent", parent, children[0].ParentId);
        AssertEqual("link child", child, children[0].ChildId);
        AssertEqual("destroy policy", ParentDestroyPolicy.Detach, children[0].DestroyPolicy);
    }

    private void LifecycleTree_Attach_ShouldRejectInvalidLinks()
    {
        var tree = new LifecycleTree();
        var parent = new EntityId("entity.parent");
        var child = new EntityId("entity.child");
        var grandchild = new EntityId("entity.grandchild");
        var otherParent = new EntityId("entity.other-parent");

        AssertFalse("empty parent 应拒绝", tree.Attach(EntityId.Empty, child, ParentDestroyPolicy.DestroyRecursively));
        AssertFalse("empty child 应拒绝", tree.Attach(parent, EntityId.Empty, ParentDestroyPolicy.DestroyRecursively));
        AssertFalse("self attach 应拒绝", tree.Attach(parent, parent, ParentDestroyPolicy.DestroyRecursively));

        AssertTrue("parent -> child attach", tree.Attach(parent, child, ParentDestroyPolicy.DestroyRecursively));
        AssertFalse("second parent 应拒绝", tree.Attach(otherParent, child, ParentDestroyPolicy.DestroyRecursively));

        AssertTrue("child -> grandchild attach", tree.Attach(child, grandchild, ParentDestroyPolicy.DestroyRecursively));
        AssertFalse("cycle attach 应拒绝", tree.Attach(grandchild, parent, ParentDestroyPolicy.DestroyRecursively));
    }

    private void LifecycleTree_Detach_ShouldClearBothSides()
    {
        var tree = new LifecycleTree();
        var parent = new EntityId("entity.parent");
        var child = new EntityId("entity.child");

        AssertTrue("attach 应成功", tree.Attach(parent, child, ParentDestroyPolicy.DestroyRecursively));
        AssertTrue("detach 应成功", tree.Detach(child));
        AssertEqual("child parent 应清空", EntityId.Empty, tree.GetParent(child));
        AssertEqual("parent children 应清空", 0, tree.GetChildren(parent).Count);
        AssertFalse("重复 detach 应返回 false", tree.Detach(child));
    }

    private void LifecycleTree_DetachAll_ShouldClearParentAndChildSides()
    {
        var tree = new LifecycleTree();
        var parent = new EntityId("entity.parent");
        var child = new EntityId("entity.child");
        var grandchild = new EntityId("entity.grandchild");

        AssertTrue("parent -> child attach", tree.Attach(parent, child, ParentDestroyPolicy.DestroyRecursively));
        AssertTrue("child -> grandchild attach", tree.Attach(child, grandchild, ParentDestroyPolicy.Detach));

        tree.DetachAll(child);

        AssertEqual("child parent 应清空", EntityId.Empty, tree.GetParent(child));
        AssertEqual("parent children 应移除 child", 0, tree.GetChildren(parent).Count);
        AssertEqual("grandchild parent 应清空", EntityId.Empty, tree.GetParent(grandchild));
        AssertEqual("child children 应清空", 0, tree.GetChildren(child).Count);
    }

    private void LifecycleTree_GetChildren_ShouldReturnSnapshot()
    {
        var tree = new LifecycleTree();
        var parent = new EntityId("entity.parent");
        var child = new EntityId("entity.child");

        AssertTrue("attach 应成功", tree.Attach(parent, child, ParentDestroyPolicy.DestroyRecursively));
        var children = tree.GetChildren(parent);

        AssertTrue("detach 应成功", tree.Detach(child));
        AssertEqual("snapshot 不应随 tree 后续变化而变化", 1, children.Count);
        AssertEqual("tree children 已清空", 0, tree.GetChildren(parent).Count);
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
