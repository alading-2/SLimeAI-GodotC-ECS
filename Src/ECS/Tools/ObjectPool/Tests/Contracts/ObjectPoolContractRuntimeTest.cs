using Godot;
using System;
using System.Collections.Generic;

namespace Slime.Test;

/// <summary>
/// ObjectPool 运行时契约测试。
/// <para>该场景用于自动验证对象池统计、节点停放和碰撞逻辑 guard 的基础契约。</para>
/// </summary>
public partial class ObjectPoolContractRuntimeTest : Node
{
    private static readonly Log _log = new(nameof(ObjectPoolContractRuntimeTest));

    private sealed class PlainPoolItem : IPoolable
    {
        public int AcquireCount { get; private set; }
        public int ReleaseCount { get; private set; }
        public int ResetCount { get; private set; }

        public void OnPoolAcquire() => AcquireCount++;
        public void OnPoolRelease() => ReleaseCount++;
        public void OnPoolReset() => ResetCount++;
    }

    private int _passedCount;
    private int _failedCount;
    private readonly List<Action> _cleanupActions = new();

    public override void _Ready()
    {
        try
        {
            AddChild(new Node { Name = "Pools" });
            RuntimeMountService.Initialize(this);
            ObjectPoolRuntimeStateStore.Clear();
            ObjectPoolObservability.Clear();

            TestWarmupStats();
            TestPlainObjectStaticReturn();
            TestDuplicateReleaseGuard();
            TestCapacityDiscard();
            TestStaticReturnNode();
            TestActiveSnapshotIsCopy();
            TestManagerPoolIsolation();
            TestManagerUsesRuntimePoolInterface();
            TestAreaReleaseStaysInTreeAndRecordsState();
            TestActivationReadyFrameGuard();
        }
        catch (Exception ex)
        {
            Fail($"测试过程中发生异常: {ex}");
        }
        finally
        {
            CleanupCreatedPools();
            ObjectPoolRuntimeStateStore.Clear();
            ObjectPoolObservability.Clear();
        }

        _log.Info($"ObjectPool 运行时契约测试结束: PASS={_passedCount}, FAIL={_failedCount}");
        GetTree().Quit(_failedCount == 0 ? 0 : 1);
    }

    private void TestWarmupStats()
    {
        var pool = CreatePool(
            () => new PlainPoolItem(),
            new ObjectPoolConfig
            {
                Name = "Test/ObjectPool/WarmupStats",
                InitialSize = 2,
                MaxSize = 4
            });

        var stats = pool.GetStats();
        AssertEqual("预热后闲置数量", 2, stats.Count);
        AssertEqual("预热后总创建数量", 2, stats.TotalCreated);
        AssertEqual("预热后活跃数量", 0, stats.ActiveCount);

        var item = pool.Get();
        pool.Release(item);

        stats = pool.GetStats();
        AssertEqual("Get/Release 后闲置数量恢复", 2, stats.Count);
        AssertEqual("Get 后应统计一次获取", 1, stats.TotalAcquired);
        AssertEqual("Release 后应统计一次归还", 1, stats.TotalReleased);
        AssertEqual("生命周期 Acquire 回调", 1, item.AcquireCount);
        AssertEqual("生命周期 Release 回调", 1, item.ReleaseCount);
        AssertEqual("生命周期 Reset 回调", 1, item.ResetCount);
    }

    private void TestPlainObjectStaticReturn()
    {
        var pool = CreatePool(
            () => new PlainPoolItem(),
            new ObjectPoolConfig
            {
                Name = "Test/ObjectPool/PlainStaticReturn",
                InitialSize = 0,
                MaxSize = 2
            });

        var item = pool.Get();
        ObjectPoolManager.ReturnToPool(item);

        var stats = pool.GetStats();
        AssertEqual("纯对象可通过 ObjectPoolManager 静态归还", 1, stats.TotalReleased);
        AssertEqual("静态归还后进入闲置栈", 1, stats.Count);
    }

    private void TestDuplicateReleaseGuard()
    {
        var pool = CreatePool(
            () => new PlainPoolItem(),
            new ObjectPoolConfig
            {
                Name = "Test/ObjectPool/DuplicateReleaseGuard",
                InitialSize = 0,
                MaxSize = 2
            });

        var item = pool.Get();
        pool.Release(item);
        pool.Release(item);

        var stats = pool.GetStats();
        AssertEqual("重复归还不应重复统计", 1, stats.TotalReleased);
        AssertEqual("重复归还不应让 ActiveCount 为负", 0, stats.ActiveCount);
        AssertEqual("重复归还不应重复入栈", 1, stats.Count);
    }

    private void TestCapacityDiscard()
    {
        var pool = CreatePool(
            () => new PlainPoolItem(),
            new ObjectPoolConfig
            {
                Name = "Test/ObjectPool/CapacityDiscard",
                InitialSize = 0,
                MaxSize = 1
            });

        var first = pool.Get();
        var second = pool.Get();
        pool.Release(first);
        pool.Release(second);

        var stats = pool.GetStats();
        AssertEqual("超过容量的归还应丢弃", 1, stats.TotalDiscarded);
        AssertEqual("容量丢弃后闲置数不超过 MaxSize", 1, stats.Count);
        AssertEqual("容量丢弃后活跃数归零", 0, stats.ActiveCount);
    }

    private void TestStaticReturnNode()
    {
        var pool = CreatePool(
            () => new Area2D { Name = "StaticReturnArea" },
            new ObjectPoolConfig
            {
                Name = "Test/ObjectPool/StaticReturnNode",
                InitialSize = 0,
                MaxSize = 2,
                ParentPath = "Pools/StaticReturnNode"
            });

        var area = pool.Get();
        ObjectPoolManager.ReturnToPool(area);

        var stats = pool.GetStats();
        AssertEqual("Node 可通过 ObjectPoolName meta 静态归还", 1, stats.TotalReleased);
        AssertEqual("Node 静态归还后进入闲置栈", 1, stats.Count);
        AssertEqual("Node 静态归还后仍在树中", true, area.IsInsideTree());
    }

    private void TestActiveSnapshotIsCopy()
    {
        var pool = CreatePool(
            () => new PlainPoolItem(),
            new ObjectPoolConfig
            {
                Name = "Test/ObjectPool/ActiveSnapshotIsCopy",
                InitialSize = 0,
                MaxSize = 4
            });

        var first = pool.Get();
        var second = pool.Get();
        var snapshot = pool.GetActiveSnapshot();
        pool.Release(first);

        AssertEqual("活跃快照应是副本", 2, snapshot.Count);
        AssertEqual("释放后新快照应反映当前活跃数量", 1, pool.GetActiveSnapshot().Count);
        pool.Release(second);
    }

    private void TestManagerPoolIsolation()
    {
        var forbiddenNames = new HashSet<string>(StringComparer.Ordinal)
        {
            ObjectPoolNames.ProjectilePool,
            ObjectPoolNames.EffectPool,
            ObjectPoolNames.EnemyPool,
            ObjectPoolNames.AbilityPool
        };

        var statsBefore = ObjectPoolManager.GetAllStats();

        var pool = CreatePool(
            () => new PlainPoolItem(),
            new ObjectPoolConfig
            {
                Name = "Test/ObjectPool/ManagerPoolIsolation",
                InitialSize = 1,
                MaxSize = 2
            });

        var statsAfter = ObjectPoolManager.GetAllStats();
        AssertEqual("隔离测试池应注册到 manager", true, statsAfter.ContainsKey(pool.PoolName));
        AssertEqual("隔离测试池必须使用 Test/ObjectPool 命名空间", true, pool.PoolName.StartsWith("Test/ObjectPool/", StringComparison.Ordinal));
        AssertEqual("隔离测试池名不能使用真实池名", false, forbiddenNames.Contains(pool.PoolName));

        foreach (var forbiddenName in forbiddenNames)
        {
            var existedBefore = statsBefore.TryGetValue(forbiddenName, out var beforeStats);
            var existsAfter = statsAfter.TryGetValue(forbiddenName, out var afterStats);
            AssertEqual($"真实池 {forbiddenName} 存在性不应被测试池改变", existedBefore, existsAfter);

            if (existedBefore && existsAfter)
            {
                // 当前框架 project.godot 会通过 autoload 预先注册真实池；这里只验证测试池没有替换这些池。
                AssertEqual($"真实池 {forbiddenName} 的池名不应被替换", beforeStats.PoolName, afterStats.PoolName);
                AssertEqual($"真实池 {forbiddenName} 的创建计数不应被替换", beforeStats.TotalCreated, afterStats.TotalCreated);
            }
        }
    }

    private void TestManagerUsesRuntimePoolInterface()
    {
        var pool = CreatePool(
            () => new PlainPoolItem(),
            new ObjectPoolConfig
            {
                Name = "Test/ObjectPool/RuntimeInterface",
                InitialSize = 0,
                MaxSize = 2
            });

        var runtime = ObjectPoolManager.GetRuntimePool(pool.PoolName);
        AssertEqual("manager 应暴露非泛型 runtime pool", true, runtime != null);
        AssertEqual("runtime pool 应保留池名", pool.PoolName, runtime!.PoolName);
        AssertEqual("错误类型 untyped release 应被拒绝", false, runtime.ReleaseUntyped(new object()));

        var item = pool.Get();
        AssertEqual("正确类型 untyped release 应成功", true, runtime.ReleaseUntyped(item));

        var stats = runtime.GetStats();
        AssertEqual("runtime release 应更新归还统计", 1, stats.TotalReleased);
    }

    private void TestAreaReleaseStaysInTreeAndRecordsState()
    {
        var pool = CreatePool(
            () => new Area2D { Name = "ContractArea" },
            new ObjectPoolConfig
            {
                Name = "Test/ObjectPool/AreaParkedInTree",
                InitialSize = 0,
                MaxSize = 2,
                ParentPath = "Pools/AreaParkedInTree"
            });

        var area = pool.Get();
        AssertEqual("Area 出池后应在树中", true, area.IsInsideTree());

        pool.Release(area);

        AssertEqual("Area 回池后仍应在树中", true, area.IsInsideTree());
        AssertEqual("Area 回池后应隐藏", false, area.Visible);
        AssertEqual("Area 回池后应停处理", Node.ProcessModeEnum.Disabled, area.ProcessMode);

        bool hasState = ObjectPoolRuntimeStateStore.TryGet(area, out var state);
        AssertEqual("Area 回池后应写 runtime state", true, hasState);
        AssertEqual("runtime state 标记 IsInPool", true, state.IsInPool);
        AssertEqual("runtime state 标记 CollisionLogicActive=false", false, state.CollisionLogicActive);
        AssertEqual("runtime state 记录池名", pool.PoolName, state.PoolName);
        AssertEqual("guard 应拒绝回池节点", false, CollisionLogicGuard.CanProcessCollision(area));
    }

    private void TestActivationReadyFrameGuard()
    {
        var pool = CreatePool(
            () => new Area2D { Name = "EmbargoArea" },
            new ObjectPoolConfig
            {
                Name = "Test/ObjectPool/ActivationReadyFrame",
                InitialSize = 0,
                MaxSize = 2,
                ParentPath = "Pools/ActivationReadyFrame"
            });

        var area = pool.Get(activateNode: false);
        pool.Activate(area);

        bool hasState = ObjectPoolRuntimeStateStore.TryGet(area, out var state);
        AssertEqual("Activate 后应写 runtime state", true, hasState);
        AssertEqual("Activate 后碰撞逻辑标记 active", true, state.CollisionLogicActive);
        AssertEqual("Activate 当前 physics frame 仍应被 embargo", false, CollisionLogicGuard.CanProcessCollision(area, state.LastAcquirePhysicsFrame));
        AssertEqual("到达 ready frame 后 guard 应放行", true, CollisionLogicGuard.CanProcessCollision(area, state.CollisionReadyPhysicsFrame));
    }

    private void AssertEqual<T>(string name, T expected, T actual)
    {
        if (Equals(expected, actual))
        {
            Pass($"{name} | expected={expected} actual={actual}");
            return;
        }

        Fail($"{name} | expected={expected} actual={actual}");
    }

    private void Pass(string message)
    {
        _passedCount++;
        _log.Success(message, outcome: LogOutcome.Succeeded, validationStatus: LogValidationStatus.Pass, channel: LogChannel.Validation);
    }

    private void Fail(string message)
    {
        _failedCount++;
        _log.Error(message, outcome: LogOutcome.Failed, validationStatus: LogValidationStatus.Fail, channel: LogChannel.Validation);
    }

    private ObjectPool<T> CreatePool<T>(Func<T> createFunc, ObjectPoolConfig config) where T : class
    {
        var pool = new ObjectPool<T>(createFunc, config);
        _cleanupActions.Add(pool.Destroy);
        return pool;
    }

    private void CleanupCreatedPools()
    {
        for (var i = _cleanupActions.Count - 1; i >= 0; i--)
        {
            try
            {
                _cleanupActions[i]();
            }
            catch (Exception ex)
            {
                _log.Warn($"清理测试池失败: {ex.Message}");
            }
        }

        _cleanupActions.Clear();
    }
}
