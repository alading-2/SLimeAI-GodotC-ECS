using Godot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;

/// <summary>
/// Timer scheduler 压力验证场景。场景自动运行并输出可供 scene-gate 读取的 JSON artifact。
/// </summary>
public partial class TimerStressValidation : Node
{
    private const string ArtifactPath = ".ai-temp/scene-tests/artifacts/timer-stress-validation.json";
    private const string ExpectedInputs = "headless TimerScheduler stress sequence driven by synthetic Game/Real clock deltas";
    private const string ExpectedObservations = "no-due timers stay active without callbacks, due timers dispatch on main thread, owner/purpose diagnostics expose leak hints";
    private const string PassCriteria = "all TimerStressValidation checks pass and artifact checks are complete";
    private const string FailCriteria = "any timer callback count, owner cancel, pause clock, diagnostics or main-thread dispatch expectation fails";

    private readonly List<TimerStressCheck> _checks = new();
    private readonly Log _log = new("TimerStressValidation", owner: "Timer", operation: "TimerStressValidation");

    public override void _Ready()
    {
        try
        {
            RunAllChecks();
        }
        catch (Exception ex)
        {
            _checks.Add(new TimerStressCheck
            {
                Name = "Fatal",
                Passed = false,
                Message = ex.ToString()
            });
        }

        var passed = _checks.All(static check => check.Passed);
        try
        {
            WriteArtifact(passed);
        }
        catch (Exception ex)
        {
            passed = false;
            _log.Error(
                "TimerStressValidation artifact write failed",
                fields: new LogFields { ["exception"] = ex.ToString() },
                channel: LogChannel.Validation,
                operation: "TimerStressValidation");
        }

        using var validation = ValidationSession.Start(new ValidationSessionOptions
        {
            Name = "TimerStressValidation",
            Owner = "Timer",
            ArtifactPath = ArtifactPath,
            ExpectedInputs = ExpectedInputs,
            ExpectedObservations = ExpectedObservations,
            PassCriteria = PassCriteria,
            FailCriteria = FailCriteria
        });
        foreach (var check in _checks)
        {
            validation.Check(
                check.Name,
                check.Passed,
                expected: "pass",
                actual: check.Passed ? "pass" : check.Message,
                reasonCode: check.Passed ? "timer-check-pass" : "timer-check-fail",
                message: check.Message);
        }
        validation.Complete();
        Log.Flush();

        GetTree().Quit(passed ? 0 : 1);
    }

    private void RunAllChecks()
    {
        RunCheck("LongDelayNoDue", LongDelayNoDue);
        RunCheck("StaggeredDue", StaggeredDue);
        RunCheck("CancelByOwner", CancelByOwner);
        RunCheck("LoopRepeatCountdown", LoopRepeatCountdown);
        RunCheck("GamePauseRealClock", GamePauseRealClock);
        RunCheck("PerFrameUpdateIsolation", PerFrameUpdateIsolation);
        RunCheck("OwnerPurposeLeakHints", OwnerPurposeLeakHints);
        RunCheck("MainThreadDispatch", MainThreadDispatch);
    }

    private void RunCheck(string name, Action<TimerStressCheck> action)
    {
        var check = new TimerStressCheck { Name = name };
        try
        {
            action(check);
            check.Passed = true;
            check.Message = "PASS";
        }
        catch (Exception ex)
        {
            check.Passed = false;
            check.Message = ex.Message;
        }

        _checks.Add(check);
    }

    private static void LongDelayNoDue(TimerStressCheck check)
    {
        var scheduler = new TimerScheduler();
        var callbacks = 0;

        for (var i = 0; i < 10_000; i++)
        {
            scheduler.Delay(10_000f, Options($"long-delay-{i}"), () => callbacks++);
        }

        // 预热后测量 no-due tick，避免首次 JIT 噪音进入分配断言。
        scheduler.Tick(TimerClock.Game, 0.016f);
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var allocatedBefore = GC.GetAllocatedBytesForCurrentThread();
        scheduler.Tick(TimerClock.Game, 0.016f);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - allocatedBefore;
        scheduler.DispatchDueCallbacks();

        var diagnostics = scheduler.GetTimerDiagnostics(new TimerDiagnosticsFilter(MaxEntries: 0));
        Add(check, "activeCount", diagnostics.ActiveCount);
        Add(check, "callbackCount", callbacks);
        Add(check, "tickAllocatedBytes", allocated);
        Add(check, "lastTickCostMs", diagnostics.LastTickCostMs);

        AssertEqual("long delay active count", 10_000, diagnostics.ActiveCount);
        AssertEqual("long delay callback count", 0, callbacks);
        AssertEqual("long delay no-due tick allocated bytes", 0L, allocated);
    }

    private static void StaggeredDue(TimerStressCheck check)
    {
        var scheduler = new TimerScheduler();
        var callbacks = 0;

        for (var i = 0; i < 10_000; i++)
        {
            var delay = 0.001f + (i % 100) * 0.001f;
            scheduler.Delay(delay, Options($"staggered-{i}"), () => callbacks++);
        }

        scheduler.Tick(TimerClock.Game, 0.2f);
        scheduler.DispatchDueCallbacks();

        var diagnostics = scheduler.GetTimerDiagnostics(new TimerDiagnosticsFilter(MaxEntries: 0));
        Add(check, "activeCount", diagnostics.ActiveCount);
        Add(check, "callbackCount", callbacks);
        Add(check, "maxCallbacksDispatchedInFrame", diagnostics.MaxCallbacksDispatchedInFrame);

        AssertEqual("staggered callbacks", 10_000, callbacks);
        AssertEqual("staggered active cleared", 0, diagnostics.ActiveCount);
    }

    private static void CancelByOwner(TimerStressCheck check)
    {
        var scheduler = new TimerScheduler();
        var owner = new TimerOwner(TimerOwnerType.Test, "timer-stress-cancel-owner");
        var callbacks = 0;

        for (var i = 0; i < 10_000; i++)
        {
            scheduler.Delay(10_000f, new TimerOptions(owner, TimerPurpose.Test, Source: "TimerStressValidation"), () => callbacks++);
        }

        var cancelled = scheduler.CancelByOwner(owner, TimerCancelReason.OwnerDestroyed);
        scheduler.Tick(TimerClock.Game, 20_000f);
        scheduler.DispatchDueCallbacks();

        var diagnostics = scheduler.GetTimerDiagnostics(new TimerDiagnosticsFilter(MaxEntries: 0));
        Add(check, "cancelledCount", cancelled);
        Add(check, "callbackCount", callbacks);
        Add(check, "activeCount", diagnostics.ActiveCount);
        Add(check, "cancelledLazyHeapItems", diagnostics.CancelledLazyHeapItems);

        AssertEqual("owner cancelled count", 10_000, cancelled);
        AssertEqual("owner cancel callbacks", 0, callbacks);
        AssertEqual("owner cancel active cleared", 0, diagnostics.ActiveCount);
    }

    private static void LoopRepeatCountdown(TimerStressCheck check)
    {
        var scheduler = new TimerScheduler();
        var loopCount = 0;
        var repeatRemaining = new List<int>();
        var repeatCompleted = 0;
        var countdownTicks = new List<float>();
        var countdownCompleted = 0;

        var loopHandle = scheduler.Loop(0.1f, Options("loop-repeat-countdown-loop"), () => loopCount++);
        scheduler.Repeat(0.1f, 3, Options("loop-repeat-countdown-repeat"), repeatRemaining.Add, () => repeatCompleted++);
        scheduler.Countdown(0.3f, 0.1f, Options("loop-repeat-countdown-countdown"),
            (_, progress) => countdownTicks.Add(progress),
            () => countdownCompleted++);

        scheduler.Tick(TimerClock.Game, 0.35f);
        scheduler.DispatchDueCallbacks();
        scheduler.Cancel(loopHandle, TimerCancelReason.TestCleanup);

        var diagnostics = scheduler.GetTimerDiagnostics(new TimerDiagnosticsFilter(MaxEntries: 0));
        Add(check, "loopCount", loopCount);
        Add(check, "repeatRemaining", string.Join(",", repeatRemaining));
        Add(check, "repeatCompleted", repeatCompleted);
        Add(check, "countdownTickCount", countdownTicks.Count);
        Add(check, "countdownCompleted", countdownCompleted);
        Add(check, "activeCount", diagnostics.ActiveCount);

        AssertEqual("loop count", 3, loopCount);
        AssertSequence("repeat remaining", repeatRemaining, 2, 1, 0);
        AssertEqual("repeat completed", 1, repeatCompleted);
        AssertEqual("countdown tick count", 3, countdownTicks.Count);
        AssertNear("countdown final progress", 1f, countdownTicks[^1], 0.0001f);
        AssertEqual("countdown completed", 1, countdownCompleted);
        AssertEqual("loop cleanup active count", 0, diagnostics.ActiveCount);
    }

    private static void GamePauseRealClock(TimerStressCheck check)
    {
        var scheduler = new TimerScheduler();
        var gameCallbacks = 0;
        var realCallbacks = 0;

        scheduler.Delay(1f, Options("game-clock", clock: TimerClock.Game), () => gameCallbacks++);
        scheduler.Delay(1f, Options("real-clock", clock: TimerClock.Real), () => realCallbacks++);

        scheduler.SetClockPaused(TimerClock.Game, true);
        scheduler.Tick(TimerClock.Game, 2f);
        scheduler.Tick(TimerClock.Real, 2f);
        scheduler.DispatchDueCallbacks();

        Add(check, "gameCallbacksWhilePaused", gameCallbacks);
        Add(check, "realCallbacksWhileGamePaused", realCallbacks);

        AssertEqual("game clock paused", 0, gameCallbacks);
        AssertEqual("real clock advances", 1, realCallbacks);

        scheduler.SetClockPaused(TimerClock.Game, false);
        scheduler.Tick(TimerClock.Game, 1f);
        scheduler.DispatchDueCallbacks();

        Add(check, "gameCallbacksAfterResume", gameCallbacks);
        AssertEqual("game clock resumes", 1, gameCallbacks);
    }

    private static void PerFrameUpdateIsolation(TimerStressCheck check)
    {
        var scheduler = new TimerScheduler();
        var updates = 0;

        scheduler.Delay(10f, Options("progress") with { OnUpdate = _ => updates++ }, () => { });
        for (var i = 0; i < 1_000; i++)
        {
            scheduler.Delay(10f, Options($"regular-{i}"), () => { });
        }

        scheduler.Tick(TimerClock.Game, 0.016f);
        scheduler.DispatchDueCallbacks();

        var diagnostics = scheduler.GetTimerDiagnostics(new TimerDiagnosticsFilter(MaxEntries: 0));
        Add(check, "updateCount", updates);
        Add(check, "perFrameUpdateCount", diagnostics.PerFrameUpdateCount);
        Add(check, "activeCount", diagnostics.ActiveCount);

        AssertEqual("per-frame update count", 1, updates);
        AssertEqual("diagnostics per-frame update count", 1, diagnostics.PerFrameUpdateCount);
        AssertEqual("regular timers stay isolated", 1_001, diagnostics.ActiveCount);
    }

    private static void OwnerPurposeLeakHints(TimerStressCheck check)
    {
        var scheduler = new TimerScheduler();
        var owner = new TimerOwner(TimerOwnerType.System, "timer-stress-diagnostics");

        scheduler.Delay(10f, new TimerOptions(TimerOwner.None, TimerPurpose.None, Source: "TimerStressValidation"), () => { });
        scheduler.Delay(10f, new TimerOptions(owner, TimerPurpose.Debug, Source: "TimerStressValidation"), () => { });

        var diagnostics = scheduler.GetTimerDiagnostics(new TimerDiagnosticsFilter(MaxEntries: 10));
        var hasOwner = diagnostics.TopOwners.Any(item => item.Owner == owner && item.Count == 1);
        var hasPurpose = diagnostics.ActiveCountByPurpose.TryGetValue(TimerPurpose.Debug, out var debugPurposeCount) && debugPurposeCount == 1;

        Add(check, "activeCount", diagnostics.ActiveCount);
        Add(check, "leakHintCount", diagnostics.LeakHints.Count);
        Add(check, "hasExpectedTopOwner", hasOwner);
        Add(check, "hasDebugPurpose", hasPurpose);

        AssertEqual("diagnostics active count", 2, diagnostics.ActiveCount);
        AssertTrue("leak hints include missing owner and purpose", diagnostics.LeakHints.Count >= 2);
        AssertTrue("diagnostics top owner includes system owner", hasOwner);
        AssertTrue("diagnostics purpose count includes debug", hasPurpose);
    }

    private static void MainThreadDispatch(TimerStressCheck check)
    {
        var scheduler = new TimerScheduler();
        var mainThreadId = Thread.CurrentThread.ManagedThreadId;
        var callbackThreadId = -1;

        scheduler.Delay(0.1f, Options("main-thread-dispatch"), () => callbackThreadId = Thread.CurrentThread.ManagedThreadId);
        scheduler.Tick(TimerClock.Game, 0.2f);

        Add(check, "callbackThreadBeforeDispatch", callbackThreadId);
        AssertEqual("callback queued before dispatch", -1, callbackThreadId);

        scheduler.DispatchDueCallbacks();

        Add(check, "mainThreadId", mainThreadId);
        Add(check, "callbackThreadId", callbackThreadId);
        AssertEqual("callback dispatched on main thread", mainThreadId, callbackThreadId);
    }

    private static TimerOptions Options(string ownerId, TimerPurpose purpose = TimerPurpose.Test, TimerClock clock = TimerClock.Game)
    {
        return new TimerOptions(new TimerOwner(TimerOwnerType.Test, ownerId), purpose, clock, Source: "TimerStressValidation");
    }

    private void WriteArtifact(bool passed)
    {
        var artifact = new TimerStressArtifact
        {
            Status = passed ? "PASS" : "FAIL",
            ExpectedInputs = ExpectedInputs,
            ExpectedObservations = ExpectedObservations,
            PassCriteria = PassCriteria,
            FailCriteria = FailCriteria,
            ArtifactPath = ArtifactPath,
            GeneratedAt = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture),
            Checks = new List<TimerStressCheck>(_checks)
        };

        var absolutePath = Path.Combine(ProjectSettings.GlobalizePath("res://"), ArtifactPath);
        var directory = Path.GetDirectoryName(absolutePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(absolutePath, JsonSerializer.Serialize(artifact, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        }));
    }

    private static void Add(TimerStressCheck check, string key, object? value)
    {
        check.Observations[key] = Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private static void AssertTrue(string label, bool condition)
    {
        if (!condition)
        {
            throw new InvalidOperationException(label);
        }
    }

    private static void AssertEqual<T>(string label, T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
        {
            throw new InvalidOperationException($"{label}: expected={expected}, actual={actual}");
        }
    }

    private static void AssertNear(string label, float expected, float actual, float epsilon)
    {
        if (Math.Abs(expected - actual) > epsilon)
        {
            throw new InvalidOperationException($"{label}: expected={expected}, actual={actual}, epsilon={epsilon}");
        }
    }

    private static void AssertSequence(string label, IReadOnlyList<int> actual, params int[] expected)
    {
        AssertEqual($"{label} count", expected.Length, actual.Count);
        for (var i = 0; i < expected.Length; i++)
        {
            AssertEqual($"{label}[{i}]", expected[i], actual[i]);
        }
    }

    private sealed class TimerStressArtifact
    {
        public string Status { get; set; } = string.Empty;
        public string ExpectedInputs { get; set; } = string.Empty;
        public string ExpectedObservations { get; set; } = string.Empty;
        public string PassCriteria { get; set; } = string.Empty;
        public string FailCriteria { get; set; } = string.Empty;
        public string ArtifactPath { get; set; } = string.Empty;
        public string GeneratedAt { get; set; } = string.Empty;
        public List<TimerStressCheck> Checks { get; set; } = new();
    }

    private sealed class TimerStressCheck
    {
        public string Name { get; set; } = string.Empty;
        public bool Passed { get; set; }
        public string Message { get; set; } = string.Empty;
        public Dictionary<string, string> Observations { get; } = new(StringComparer.Ordinal);
    }
}
