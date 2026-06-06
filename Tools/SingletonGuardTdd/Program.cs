using System;

internal static class Program
{
    private static int _passed;
    private static int _failed;

    private static int Main()
    {
        Run("首次绑定会写入实例", FirstBindStoresInstance);
        Run("重复绑定不会覆盖已有实例", DuplicateBindKeepsExistingInstance);
        Run("只有当前实例退出时才清空", ReleaseOnlyClearsMatchingInstance);

        Console.WriteLine($"SingletonGuardTdd PASS={_passed} FAIL={_failed}");
        return _failed == 0 ? 0 : 1;
    }

    private static void FirstBindStoresInstance()
    {
        Probe? instance = null;
        var candidate = new Probe("first");

        var bound = SingletonInstanceGuard.TryBind(candidate, ref instance);

        AssertTrue("首次绑定应成功", bound);
        AssertSame("实例应指向候选对象", candidate, instance);
    }

    private static void DuplicateBindKeepsExistingInstance()
    {
        var first = new Probe("first");
        var duplicate = new Probe("duplicate");
        Probe? instance = first;
        Probe? rejected = null;

        var bound = SingletonInstanceGuard.TryBind(duplicate, ref instance, node => rejected = node);

        AssertFalse("重复绑定应失败", bound);
        AssertSame("重复绑定不应覆盖旧实例", first, instance);
        AssertSame("重复绑定应回调候选对象", duplicate, rejected);
    }

    private static void ReleaseOnlyClearsMatchingInstance()
    {
        var current = new Probe("current");
        var stale = new Probe("stale");
        Probe? instance = current;

        var staleReleased = SingletonInstanceGuard.Release(stale, ref instance);
        AssertFalse("非当前实例释放应失败", staleReleased);
        AssertSame("非当前实例不应清空 Instance", current, instance);

        var currentReleased = SingletonInstanceGuard.Release(current, ref instance);
        AssertTrue("当前实例释放应成功", currentReleased);
        AssertNull("当前实例释放后应清空 Instance", instance);
    }

    private static void Run(string name, Action test)
    {
        try
        {
            test();
            _passed++;
            Console.WriteLine($"PASS {name}");
        }
        catch (Exception ex)
        {
            _failed++;
            Console.WriteLine($"FAIL {name}: {ex.Message}");
        }
    }

    private static void AssertTrue(string name, bool condition)
    {
        if (!condition)
        {
            throw new InvalidOperationException(name);
        }
    }

    private static void AssertFalse(string name, bool condition)
    {
        AssertTrue(name, !condition);
    }

    private static void AssertSame<T>(string name, T expected, T? actual) where T : class
    {
        if (!ReferenceEquals(expected, actual))
        {
            throw new InvalidOperationException(name);
        }
    }

    private static void AssertNull<T>(string name, T? actual) where T : class
    {
        if (actual != null)
        {
            throw new InvalidOperationException(name);
        }
    }

    private sealed record Probe(string Id);
}
