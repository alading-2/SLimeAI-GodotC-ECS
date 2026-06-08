using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;

internal static class Program
{
    private static int _passed;
    private static int _failed;

    private static int Main()
    {
        Run("Delay 到期只触发一次", DelayFiresOnce);
        Run("Loop 按 interval 重复", LoopFiresByInterval);
        Run("Repeat 达到次数后 complete", RepeatCompletesAfterCount);
        Run("Countdown 输出总进度并完成", CountdownTicksAndCompletes);
        Run("Cancel 后不触发 callback", CancelPreventsCallback);
        Run("Pause/Resume 保留 remaining", PauseResumePreservesRemaining);
        Run("Game clock pause 不影响 Real clock", GameClockPauseDoesNotStopRealClock);
        Run("stale handle 不能影响复用 slot", StaleHandleCannotCancelReusedSlot);
        Run("CancelByOwner 只取消目标 owner", CancelByOwnerOnlyCancelsOwnedTimers);
        Run("callback 内 schedule/cancel 不破坏派发", CallbackMutationIsStable);
        Run("per-frame update 与普通 timer 隔离", PerFrameUpdateIsIsolated);
        Run("普通 tick no due 不分配", TickNoDueDoesNotAllocate);
        Run("Timer benchmark 输出性能证据", RunBenchmarks);

        Console.WriteLine($"TimerSchedulerTdd PASS={_passed} FAIL={_failed}");
        return _failed == 0 ? 0 : 1;
    }

    private static void DelayFiresOnce()
    {
        var scheduler = new TimerScheduler();
        var fired = 0;
        var handle = scheduler.Delay(1f, Options("delay"), () => fired++);

        scheduler.Tick(TimerClock.Game, 0.99f);
        scheduler.DispatchDueCallbacks();
        AssertEqual("not due", 0, fired);
        AssertTrue("remaining query", scheduler.TryGetRemaining(handle, out var remaining));
        AssertNear("remaining", 0.01f, remaining, 0.0001f);

        scheduler.Tick(TimerClock.Game, 0.02f);
        scheduler.DispatchDueCallbacks();
        scheduler.Tick(TimerClock.Game, 1f);
        scheduler.DispatchDueCallbacks();
        AssertEqual("fires once", 1, fired);
        AssertFalse("completed handle no longer queryable", scheduler.TryGetRemaining(handle, out _));
    }

    private static void LoopFiresByInterval()
    {
        var scheduler = new TimerScheduler();
        var fired = 0;
        scheduler.Loop(0.25f, Options("loop"), () => fired++);

        scheduler.Tick(TimerClock.Game, 1.01f);
        scheduler.DispatchDueCallbacks();

        AssertEqual("loop catch-up", 4, fired);
    }

    private static void RepeatCompletesAfterCount()
    {
        var scheduler = new TimerScheduler();
        var repeats = new List<int>();
        var completed = 0;

        scheduler.Repeat(0.5f, 3, Options("repeat"), remaining => repeats.Add(remaining), () => completed++);
        scheduler.Tick(TimerClock.Game, 2f);
        scheduler.DispatchDueCallbacks();

        AssertSequence("repeat remaining", repeats, 2, 1, 0);
        AssertEqual("complete once", 1, completed);
        AssertEqual("active cleared", 0, scheduler.GetTimerDiagnostics().ActiveCount);
    }

    private static void CountdownTicksAndCompletes()
    {
        var scheduler = new TimerScheduler();
        var ticks = new List<(float elapsed, float progress)>();
        var completed = 0;

        scheduler.Countdown(1.5f, 0.5f, Options("countdown"),
            (elapsed, progress) => ticks.Add((elapsed, progress)),
            () => completed++);
        scheduler.Tick(TimerClock.Game, 1.6f);
        scheduler.DispatchDueCallbacks();

        AssertEqual("tick count", 3, ticks.Count);
        AssertNear("last elapsed", 1.5f, ticks[^1].elapsed, 0.0001f);
        AssertNear("last progress", 1f, ticks[^1].progress, 0.0001f);
        AssertEqual("complete once", 1, completed);
    }

    private static void CancelPreventsCallback()
    {
        var scheduler = new TimerScheduler();
        var fired = 0;
        var handle = scheduler.Delay(0.1f, Options("cancel"), () => fired++);

        AssertTrue("cancel returns true", scheduler.Cancel(handle, TimerCancelReason.Manual));
        scheduler.Tick(TimerClock.Game, 1f);
        scheduler.DispatchDueCallbacks();

        AssertEqual("cancelled callback", 0, fired);
        AssertFalse("cancel twice fails", scheduler.Cancel(handle, TimerCancelReason.Manual));
    }

    private static void PauseResumePreservesRemaining()
    {
        var scheduler = new TimerScheduler();
        var fired = 0;
        var handle = scheduler.Delay(1f, Options("pause"), () => fired++);

        scheduler.Tick(TimerClock.Game, 0.4f);
        AssertTrue("pause", scheduler.Pause(handle, TimerPauseMask.Manual));
        scheduler.Tick(TimerClock.Game, 10f);
        scheduler.DispatchDueCallbacks();
        AssertEqual("paused not fired", 0, fired);
        AssertTrue("remaining while paused", scheduler.TryGetRemaining(handle, out var remaining));
        AssertNear("remaining unchanged", 0.6f, remaining, 0.0001f);

        AssertTrue("resume", scheduler.Resume(handle, TimerPauseMask.Manual));
        scheduler.Tick(TimerClock.Game, 0.6f);
        scheduler.DispatchDueCallbacks();
        AssertEqual("fires after resume", 1, fired);
    }

    private static void GameClockPauseDoesNotStopRealClock()
    {
        var scheduler = new TimerScheduler();
        var gameFired = 0;
        var realFired = 0;

        scheduler.Delay(1f, Options("game", TimerClock.Game), () => gameFired++);
        scheduler.Delay(1f, Options("real", TimerClock.Real), () => realFired++);

        scheduler.SetClockPaused(TimerClock.Game, true);
        scheduler.Tick(TimerClock.Game, 2f);
        scheduler.Tick(TimerClock.Real, 2f);
        scheduler.DispatchDueCallbacks();

        AssertEqual("game paused", 0, gameFired);
        AssertEqual("real advances", 1, realFired);

        scheduler.SetClockPaused(TimerClock.Game, false);
        scheduler.Tick(TimerClock.Game, 1f);
        scheduler.DispatchDueCallbacks();
        AssertEqual("game resumes", 1, gameFired);
    }

    private static void StaleHandleCannotCancelReusedSlot()
    {
        var scheduler = new TimerScheduler();
        var first = scheduler.Delay(0.1f, Options("stale-a"), () => { });
        AssertTrue("first cancel", scheduler.Cancel(first, TimerCancelReason.Manual));

        var fired = 0;
        var second = scheduler.Delay(0.1f, Options("stale-b"), () => fired++);
        AssertEqual("slot id reused", first.Id, second.Id);
        AssertTrue("generation advanced", second.Generation > first.Generation);
        AssertFalse("stale cancel fails", scheduler.Cancel(first, TimerCancelReason.Manual));

        scheduler.Tick(TimerClock.Game, 0.2f);
        scheduler.DispatchDueCallbacks();
        AssertEqual("new timer survived", 1, fired);
    }

    private static void CancelByOwnerOnlyCancelsOwnedTimers()
    {
        var scheduler = new TimerScheduler();
        var ownerAFired = 0;
        var ownerBFired = 0;

        scheduler.Delay(0.1f, Options("owner-a"), () => ownerAFired++);
        scheduler.Delay(0.1f, Options("owner-a"), () => ownerAFired++);
        scheduler.Delay(0.1f, Options("owner-b"), () => ownerBFired++);

        var cancelled = scheduler.CancelByOwner(new TimerOwner(TimerOwnerType.Test, "owner-a"), TimerCancelReason.OwnerDestroyed);
        scheduler.Tick(TimerClock.Game, 0.2f);
        scheduler.DispatchDueCallbacks();

        AssertEqual("cancelled count", 2, cancelled);
        AssertEqual("owner a cancelled", 0, ownerAFired);
        AssertEqual("owner b fires", 1, ownerBFired);
    }

    private static void CallbackMutationIsStable()
    {
        var scheduler = new TimerScheduler();
        var cancelledFired = 0;
        var childFired = 0;
        var toCancel = scheduler.Delay(0.2f, Options("mutation-cancel"), () => cancelledFired++);

        scheduler.Delay(0.1f, Options("mutation-root"), () =>
        {
            scheduler.Cancel(toCancel, TimerCancelReason.Replaced);
            scheduler.Delay(0.1f, Options("mutation-child"), () => childFired++);
        });

        scheduler.Tick(TimerClock.Game, 0.1f);
        scheduler.DispatchDueCallbacks();
        scheduler.Tick(TimerClock.Game, 0.1f);
        scheduler.DispatchDueCallbacks();

        AssertEqual("cancelled due callback skipped", 0, cancelledFired);
        AssertEqual("scheduled child fires later", 1, childFired);
    }

    private static void PerFrameUpdateIsIsolated()
    {
        var scheduler = new TimerScheduler();
        var updates = 0;
        var progressOptions = Options("progress") with { OnUpdate = _ => updates++ };

        scheduler.Delay(10f, progressOptions, () => { });
        for (var i = 0; i < 100; i++)
        {
            scheduler.Delay(100f, Options($"regular-{i}"), () => { });
        }

        scheduler.Tick(TimerClock.Game, 0.016f);
        scheduler.DispatchDueCallbacks();
        var diagnostics = scheduler.GetTimerDiagnostics();

        AssertEqual("only progress timer updated", 1, updates);
        AssertEqual("per-frame count", 1, diagnostics.PerFrameUpdateCount);
        AssertEqual("active timers", 101, diagnostics.ActiveCount);
    }

    private static void TickNoDueDoesNotAllocate()
    {
        var scheduler = new TimerScheduler();
        for (var i = 0; i < 10_000; i++)
        {
            scheduler.Delay(1_000f, Options($"long-delay-{i}"), () => { });
        }

        // 预热一次，避免首次 JIT / runtime 初始化噪音进入断言。
        scheduler.Tick(TimerClock.Game, 0.016f);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var before = GC.GetAllocatedBytesForCurrentThread();
        scheduler.Tick(TimerClock.Game, 0.016f);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        AssertEqual("tick no due allocated bytes", 0L, allocated);
    }

    private static TimerOptions Options(string ownerId, TimerClock clock = TimerClock.Game, TimerPurpose purpose = TimerPurpose.Test)
    {
        return new TimerOptions(new TimerOwner(TimerOwnerType.Test, ownerId), purpose, clock);
    }

    private static void RunBenchmarks()
    {
        var results = new List<TimerBenchmarkResult>
        {
            BenchmarkLongDelayNoDue(1_000),
            BenchmarkLongDelayNoDue(10_000),
            BenchmarkStaggeredDue(10_000),
            BenchmarkCancelByOwner(10_000),
            BenchmarkPerFrameProgress(1_000, 10_000)
        };

        Directory.CreateDirectory(".ai-temp");
        var artifactPath = Path.Combine(".ai-temp", "timer-benchmark.json");
        File.WriteAllText(artifactPath, JsonSerializer.Serialize(results, new JsonSerializerOptions
        {
            WriteIndented = true
        }));

        foreach (var result in results)
        {
            Console.WriteLine($"[BENCH] {result.Name}: schedule={result.ScheduleMs:F4}ms tick={result.TickMs:F4}ms dispatch={result.DispatchMs:F4}ms cancel={result.CancelMs:F4}ms alloc={result.TickAllocatedBytes} active={result.ActiveCount} callbacks={result.CallbackCount}");
        }

        AssertTrue("benchmark artifact exists", File.Exists(artifactPath));
        AssertEqual("10k no due tick no allocation", 0L, results[1].TickAllocatedBytes);
        AssertEqual("cancel by owner count", 10_000, results[3].CancelledCount);
        AssertEqual("per-frame update count", 1_000, results[4].CallbackCount);
    }

    private static TimerBenchmarkResult BenchmarkLongDelayNoDue(int count)
    {
        var scheduler = new TimerScheduler();
        var start = Stopwatch.GetTimestamp();
        for (var i = 0; i < count; i++)
        {
            scheduler.Delay(10_000f, Options($"long-{i}"), () => { });
        }
        var scheduleMs = ElapsedMs(start);

        scheduler.Tick(TimerClock.Game, 0.016f);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        start = Stopwatch.GetTimestamp();
        scheduler.Tick(TimerClock.Game, 0.016f);
        var tickMs = ElapsedMs(start);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;

        start = Stopwatch.GetTimestamp();
        scheduler.DispatchDueCallbacks();
        var dispatchMs = ElapsedMs(start);

        return new TimerBenchmarkResult($"LongDelayNoDue-{count}", count, scheduleMs, tickMs, dispatchMs, 0, allocated, scheduler.GetTimerDiagnostics().ActiveCount, 0, 0);
    }

    private static TimerBenchmarkResult BenchmarkStaggeredDue(int count)
    {
        var scheduler = new TimerScheduler();
        var callbacks = 0;
        var start = Stopwatch.GetTimestamp();
        for (var i = 0; i < count; i++)
        {
            var delay = 0.001f + (i % 100) * 0.001f;
            scheduler.Delay(delay, Options($"due-{i}"), () => callbacks++);
        }
        var scheduleMs = ElapsedMs(start);

        start = Stopwatch.GetTimestamp();
        scheduler.Tick(TimerClock.Game, 0.2f);
        var tickMs = ElapsedMs(start);

        start = Stopwatch.GetTimestamp();
        scheduler.DispatchDueCallbacks();
        var dispatchMs = ElapsedMs(start);

        return new TimerBenchmarkResult("StaggeredDue-10000", count, scheduleMs, tickMs, dispatchMs, 0, 0, scheduler.GetTimerDiagnostics().ActiveCount, callbacks, 0);
    }

    private static TimerBenchmarkResult BenchmarkCancelByOwner(int count)
    {
        var scheduler = new TimerScheduler();
        var owner = new TimerOwner(TimerOwnerType.Test, "cancel-owner");
        var start = Stopwatch.GetTimestamp();
        for (var i = 0; i < count; i++)
        {
            scheduler.Delay(10_000f, new TimerOptions(owner, TimerPurpose.Test), () => { });
        }
        var scheduleMs = ElapsedMs(start);

        start = Stopwatch.GetTimestamp();
        var cancelled = scheduler.CancelByOwner(owner, TimerCancelReason.OwnerDestroyed);
        var cancelMs = ElapsedMs(start);

        start = Stopwatch.GetTimestamp();
        scheduler.Tick(TimerClock.Game, 0.016f);
        var tickMs = ElapsedMs(start);

        return new TimerBenchmarkResult("CancelByOwner-10000", count, scheduleMs, tickMs, 0, cancelMs, 0, scheduler.GetTimerDiagnostics().ActiveCount, 0, cancelled);
    }

    private static TimerBenchmarkResult BenchmarkPerFrameProgress(int progressCount, int regularCount)
    {
        var scheduler = new TimerScheduler();
        var updates = 0;
        var start = Stopwatch.GetTimestamp();
        for (var i = 0; i < progressCount; i++)
        {
            scheduler.Delay(10_000f, Options($"progress-{i}") with { OnUpdate = _ => updates++ }, () => { });
        }
        for (var i = 0; i < regularCount; i++)
        {
            scheduler.Delay(10_000f, Options($"regular-{i}"), () => { });
        }
        var scheduleMs = ElapsedMs(start);

        start = Stopwatch.GetTimestamp();
        scheduler.Tick(TimerClock.Game, 0.016f);
        var tickMs = ElapsedMs(start);

        return new TimerBenchmarkResult("PerFrameProgress-1000-of-11000", progressCount + regularCount, scheduleMs, tickMs, 0, 0, 0, scheduler.GetTimerDiagnostics().ActiveCount, updates, 0);
    }

    private static double ElapsedMs(long startTimestamp)
    {
        return Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;
    }

    private static void Run(string name, Action test)
    {
        try
        {
            test();
            _passed++;
            Console.WriteLine($"[PASS] {name}");
        }
        catch (Exception ex)
        {
            _failed++;
            Console.Error.WriteLine($"[FAIL] {name}: {ex.Message}");
        }
    }

    private static void AssertTrue(string message, bool condition)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void AssertFalse(string message, bool condition) => AssertTrue(message, !condition);

    private static void AssertEqual<T>(string message, T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"{message}: expected={expected}, actual={actual}");
        }
    }

    private static void AssertNear(string message, float expected, float actual, float epsilon)
    {
        if (Math.Abs(expected - actual) > epsilon)
        {
            throw new InvalidOperationException($"{message}: expected={expected}, actual={actual}, epsilon={epsilon}");
        }
    }

    private static void AssertSequence(string message, IReadOnlyList<int> actual, params int[] expected)
    {
        AssertEqual($"{message} count", expected.Length, actual.Count);
        for (var i = 0; i < expected.Length; i++)
        {
            AssertEqual($"{message}[{i}]", expected[i], actual[i]);
        }
    }

    private sealed record TimerBenchmarkResult(
        string Name,
        int TimerCount,
        double ScheduleMs,
        double TickMs,
        double DispatchMs,
        double CancelMs,
        long TickAllocatedBytes,
        int ActiveCount,
        int CallbackCount,
        int CancelledCount);
}
