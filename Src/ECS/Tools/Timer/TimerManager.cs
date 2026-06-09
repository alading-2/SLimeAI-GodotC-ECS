using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Godot;


/// <summary>
/// 全局定时器管理器 (TimerManager)
/// 
/// 核心职责：
/// 1. 统一驱动游戏内所有的非物理计时逻辑。
/// 2. 使用纯 C# TimerScheduler 作为调度核心，避免每帧扫描所有 active timer。
/// 3. 支持游戏时间 (Game) 与真实时间 (Real) 的双重计时模式。
/// 4. 提供 handle/options 新 API，并保留旧链式 API 作为兼容 facade。
/// 
/// 用法示例：
/// var owner = new TimerOwner(TimerOwnerType.Component, "example-component");
/// var options = new TimerOptions(owner, TimerPurpose.Debug, TimerClock.Game);
/// var handle = TimerManager.Instance.Delay(2.0f, options, () => GD.Print("完成"));
/// TimerManager.Instance.Cancel(handle, TimerCancelReason.Manual);
/// </summary>
public partial class TimerManager : Node, ISystem
{
    /// <summary>
    /// 模块初始化器：利用 C# 属性在模块加载时自动将 TimerManager 注册到 SystemRegistry。
    /// </summary>
    [ModuleInitializer]
    internal static void Initialize()
    {
        SystemRegistry.Register(nameof(TimerManager),
            static () => ResourceLoading.Load<PackedScene>(nameof(TimerManager), ResourceCategory.Tools)
                .Instantiate());
    }

    private static TimerManager? _instance;

    /// <summary> 全局唯一单例访问点 </summary>
    public static TimerManager Instance => _instance!;

    private static readonly Log _log = new(nameof(TimerManager));

    /// <summary>
    /// 纯 C# 调度核心；TimerManager 只负责 Godot 生命周期和 delta 输入。
    /// </summary>
    private readonly TimerScheduler _scheduler = new();

    /// <summary> 用于手动计算 UnscaledDeltaTime 的时间戳（毫秒） </summary>
    private ulong _lastTicksMsec;

    /// <summary> 
    /// 真实时间增量 (秒)。
    /// 即使 Engine.TimeScale 为 0，该值依然能反映真实的物理流逝时间。
    /// </summary>
    private float _unscaledDeltaTime;

    /// <summary>
    /// Node 进入场景树时的初始化逻辑。
    /// </summary>
    public override void _EnterTree()
    {
        if (!NodeSingletonGuard.TryBind(this, ref _instance, _log))
        {
            return;
        }

        _lastTicksMsec = Time.GetTicksMsec();

        // 绑定每一帧的原始处理开始信号，用于更新基础时间戳
        GetTree().Connect(SceneTree.SignalName.ProcessFrame, Callable.From(OnProcessFrame));
    }

    /// <summary>
    /// Node 退出时的清理逻辑。
    /// 确保回收池中资源并断开单例，防止内存泄漏。
    /// </summary>
    public override void _ExitTree()
    {
        _scheduler.Clear(TimerCancelReason.SceneExit);
        NodeSingletonGuard.Release(this, ref _instance);
    }

    /// <summary>
    /// 手动计算真实的 DeltaTime。
    /// Godot 默认的 _Process(delta) 会受 TimeScale 影响，因此需要通过 Time.GetTicksMsec 自行计算。
    /// </summary>
    private void OnProcessFrame()
    {
        ulong currentTicks = Time.GetTicksMsec();
        _unscaledDeltaTime = (currentTicks - _lastTicksMsec) / 1000.0f;
        _lastTicksMsec = currentTicks;
    }

    /// <summary>
    /// 每一帧的计时器驱动。
    /// 将 Process 逻辑转发给 ProcessTimers 处理，统一区分缩放/非缩放时间。
    /// </summary>
    public override void _Process(double delta)
    {
        ProcessTimers(delta);
    }

    /// <summary>
    /// 核心更新逻辑：输入 clock delta，由 TimerScheduler 只处理到期 timer 和显式 per-frame timer。
    /// </summary>
    private void ProcessTimers(double delta)
    {
        float scaledDelta = (float)delta;
        float unscaledDelta = _unscaledDeltaTime;

        _scheduler.Tick(TimerClock.Game, scaledDelta);
        _scheduler.Tick(TimerClock.Real, unscaledDelta);
        _scheduler.DispatchDueCallbacks();
    }

    // ============ 工厂方法 (Factory Methods) ============

    /// <summary>
    /// 创建【延迟定时器】：并在指定时间到达后触发回调。
    /// 适合：爆炸倒计时、一次性 UI 延时。
    /// </summary>
    /// <param name="duration">持续时间 (秒)</param>
    /// <param name="useUnscaledTime">是否无视游戏暂停/缩放 (默认 false)</param>
    /// <returns>GameTimer 实例，建议后续追加 .OnComplete() 配置</returns>
    public GameTimer Delay(float duration, bool useUnscaledTime = false)
    {
        var timer = new GameTimer();
        timer.Configure(duration, false, useUnscaledTime);
        timer.BindLegacy(this, TimerMode.Delay);
        return timer;
    }

    /// <summary>
    /// 创建【无限循环定时器】：每隔一段时间触发一次回调，永不停止直到手动取消。
    /// 适合：周期性伤血、自动回魔、每秒更新界面。
    /// </summary>
    /// <param name="interval">循环间隔 (秒)</param>
    /// <param name="useUnscaledTime">是否无视游戏暂停/缩放 (默认 false)</param>
    /// <returns>GameTimer 实例，建议后续追加 .OnLoop() 配置</returns>
    public GameTimer Loop(float interval, bool useUnscaledTime = false)
    {
        var timer = new GameTimer();
        timer.Configure(interval, true, useUnscaledTime, repeatCount: -1);
        timer.BindLegacy(this, TimerMode.Loop);
        return timer;
    }

    /// <summary>
    /// 创建【有限重复定时器】：每隔一段时间触发一次回调，直到达到执行次数。
    /// 适合：连发技能（如点击三连击）、分段任务提示。
    /// </summary>
    /// <param name="interval">执行间隔 (秒)</param>
    /// <param name="count">总执行次数</param>
    /// <param name="immediate">是否在创建后立即执行第一次回调 (默认 false)</param>
    /// <param name="useUnscaledTime">是否无视游戏暂停/缩放 (默认 false)</param>
    /// <returns>GameTimer 实例，建议后续追加 .OnRepeat() 配置</returns>
    public GameTimer Repeat(float interval, int count, bool immediate = false, bool useUnscaledTime = false)
    {
        var timer = new GameTimer();
        timer.Configure(interval, true, useUnscaledTime, repeatCount: count, immediate: immediate);
        timer.BindLegacy(this, TimerMode.Repeat);
        return timer;
    }

    /// <summary>
    /// 创建【倒计时定时器】：在总时间内以指定频率触发回调，并在最后停止。
    /// 适合：副本结束倒计时、技能持续时间读条。
    /// </summary>
    /// <param name="duration">总持续总时长 (秒)</param>
    /// <param name="interval">每次回调（Tick）的触发频率 (秒)</param>
    /// <param name="immediate">是否在创建后立即执行第一次回调 (默认 false)</param>
    /// <param name="useUnscaledTime">是否无视游戏暂停/缩放 (默认 false)</param>
    /// <returns>GameTimer 实例，建议后续追加 .Countdown() 配置进度回调</returns>
    public GameTimer Countdown(float duration, float interval, bool immediate = false, bool useUnscaledTime = false)
    {
        var timer = new GameTimer();
        // 倒计时本质上是一个带总量限制的循环定时器
        timer.Configure(interval, true, useUnscaledTime, repeatCount: -1, totalDuration: duration,
            immediate: immediate);
        timer.BindLegacy(this, TimerMode.Countdown);
        return timer;
    }

    // ============ 新 handle/options API ============

    public TimerHandle Delay(float duration, TimerOptions options, Action onComplete)
    {
        return _scheduler.Delay(duration, options, onComplete);
    }

    public TimerHandle Loop(float interval, TimerOptions options, Action onLoop)
    {
        return _scheduler.Loop(interval, options, onLoop);
    }

    public TimerHandle Repeat(float interval, int count, TimerOptions options, Action<int> onRepeat, Action? onComplete = null)
    {
        return _scheduler.Repeat(interval, count, options, onRepeat, onComplete);
    }

    public TimerHandle Countdown(float duration, float interval, TimerOptions options, Action<float, float> onTick, Action? onComplete = null)
    {
        return _scheduler.Countdown(duration, interval, options, onTick, onComplete);
    }

    public bool Cancel(TimerHandle handle, TimerCancelReason reason = TimerCancelReason.Manual)
    {
        return _scheduler.Cancel(handle, reason);
    }

    public int CancelByOwner(TimerOwner owner, TimerCancelReason reason = TimerCancelReason.OwnerDestroyed)
    {
        return _scheduler.CancelByOwner(owner, reason);
    }

    public int CancelByOwnerAndPurpose(TimerOwner owner, TimerPurpose purpose, TimerCancelReason reason = TimerCancelReason.Manual)
    {
        return _scheduler.CancelByOwnerAndPurpose(owner, purpose, reason);
    }

    public bool Pause(TimerHandle handle, TimerPauseMask mask = TimerPauseMask.Manual)
    {
        return _scheduler.Pause(handle, mask);
    }

    public bool Resume(TimerHandle handle, TimerPauseMask mask = TimerPauseMask.Manual)
    {
        return _scheduler.Resume(handle, mask);
    }

    public bool TryGetRemaining(TimerHandle handle, out float remaining)
    {
        return _scheduler.TryGetRemaining(handle, out remaining);
    }

    public bool TryGetProgress(TimerHandle handle, out float progress)
    {
        return _scheduler.TryGetProgress(handle, out progress);
    }

    public TimerDiagnosticsSnapshot GetTimerDiagnostics(TimerDiagnosticsFilter? filter = null)
    {
        return _scheduler.GetTimerDiagnostics(filter);
    }

    public string FormatTimerSummary(TimerDiagnosticsSnapshot snapshot, int topN = 10)
    {
        return TimerDiagnosticsFormatter.FormatSummary(snapshot, topN);
    }

    public string FormatTimerDump(TimerDiagnosticsSnapshot snapshot)
    {
        return TimerDiagnosticsFormatter.FormatDump(snapshot);
    }

    public void PrintTimerSummary(int topN = 10)
    {
        var snapshot = GetTimerDiagnostics(new TimerDiagnosticsFilter(MaxEntries: 0));
        _log.Info(
            FormatTimerSummary(snapshot, topN),
            outcome: LogOutcome.Completed,
            fields: CreateTimerDiagnosticsFields(snapshot),
            channel: LogChannel.Diagnostics,
            operation: "TimerDiagnosticsSummary");
    }

    public void PrintTimerDump(TimerDiagnosticsFilter? filter = null)
    {
        var snapshot = GetTimerDiagnostics(filter);
        _log.Info(
            FormatTimerDump(snapshot),
            outcome: LogOutcome.Completed,
            fields: CreateTimerDiagnosticsFields(snapshot),
            channel: LogChannel.Diagnostics,
            operation: "TimerDiagnosticsDump");
    }

    public Error ExportTimerDiagnosticsJson(string path, TimerDiagnosticsFilter? filter = null)
    {
        using var trace = Log.BeginTrace("Timer", nameof(TimerManager), "ExportTimerDiagnosticsJson", "Diagnostics");
        try
        {
            var snapshot = GetTimerDiagnostics(filter);
            var json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            var absolutePath = ProjectSettings.GlobalizePath(path);
            var directory = Path.GetDirectoryName(absolutePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(absolutePath, json);
            var fields = CreateTimerDiagnosticsFields(snapshot);
            fields["path"] = path;
            trace.Complete(LogOutcome.Completed, "Timer diagnostics export completed", fields);
            return Error.Ok;
        }
        catch (Exception ex)
        {
            _log.Error(
                "Timer diagnostics export failed",
                fields: new LogFields
                {
                    ["path"] = path,
                    ["exception"] = ex.Message
                },
                operation: "ExportTimerDiagnosticsJson");
            trace.Complete(LogOutcome.Failed, "Timer diagnostics export failed", new LogFields
            {
                ["path"] = path,
                ["exception"] = ex.Message
            });
            return Error.Failed;
        }
    }

    internal bool SetLegacyOnUpdate(TimerHandle handle, Action<float>? onUpdate)
    {
        return _scheduler.SetOnUpdate(handle, onUpdate);
    }

    // ============ 批量管理 (Batch Management) ============

    /// <summary>
    /// 通过分配的唯一 ID 精确取消某个定时器。
    /// </summary>
    /// <param name="id">Guid 字符串</param>
    public void Cancel(string id)
    {
        if (TryParseHandle(id, out var handle))
        {
            _scheduler.Cancel(handle, TimerCancelReason.Manual);
        }
    }

    /// <summary>
    /// 通过标签 (Tag) 批量取消一组定时器。
    /// 典型应用：单位死亡时，取消该单位身上所有正在计时的 Buff。
    /// </summary>
    /// <param name="tag">标签字符串 (如："Buff", "Skill")</param>
    public void CancelByTag(string tag)
    {
        _scheduler.CancelByTag(tag, TimerCancelReason.Manual);
    }

    /// <summary>
    /// 批量切换所有活跃定时器的暂停状态。
    /// 通常用于全局游戏逻辑暂停（非引擎物理暂停）。
    /// </summary>
    public void SetAllTimerPaused(bool paused)
    {
        _scheduler.SetAllPaused(paused, TimerPauseMask.Manual);
    }

    /// <summary>
    /// 根据标签批量切换暂停状态。
    /// 适合：暂停特定类别的业务（如：暂停所有怪物 AI 计时，但保留玩家计时）。
    /// </summary>
    public void SetAllTimerPausedByTag(string tag, bool paused)
    {
        _scheduler.SetPausedByTag(tag, paused, TimerPauseMask.Manual);
    }

    /// <summary> 
    /// 获取当前正在内存中运行（活跃）的定时器总数。
    /// 通常用于性能分析或调试监控。
    /// </summary>
    public int GetActiveTimerCount() => _scheduler.GetTimerDiagnostics().ActiveCount;

    /// <summary> 
    /// 获取对象池的即时统计状态。
    /// 返回值：Active (活跃数), Pooled (池内闲置数)
    /// </summary>
    public (int Active, int Pooled) GetStats()
    {
        return (_scheduler.GetTimerDiagnostics().ActiveCount, 0);
    }

    /// <inheritdoc />
    public void OnStarted(ProjectStateSnapshot snapshot)
    {
        ApplyProjectPauseState(snapshot);
    }

    /// <inheritdoc />
    public void OnProjectStateChanged(ProjectStateChangedEventArgs args)
    {
        ApplyProjectPauseState(args.Current);
    }

    private void ApplyProjectPauseState(ProjectStateSnapshot snapshot)
    {
        var shouldPauseScaledTimers = ShouldPauseScaledTimers(snapshot);
        _scheduler.SetClockPaused(TimerClock.Game, shouldPauseScaledTimers);
    }

    private static bool ShouldPauseScaledTimers(ProjectStateSnapshot snapshot)
    {
        return snapshot.SimulationState == SimulationState.Suspended;
    }

    public SystemRuntimeInfo GetSystemRuntimeInfo()
    {
        var diagnostics = _scheduler.GetTimerDiagnostics();
        return new SystemRuntimeInfo
        {
            SystemId = nameof(TimerManager),
            CustomStats = new List<SystemStat>
            {
                new SystemStat
                {
                    Name = "活跃定时器",
                    Value = diagnostics.ActiveCount.ToString(),
                    Category = "统计"
                },
                new SystemStat
                {
                    Name = "派发队列",
                    Value = diagnostics.DispatchQueueCount.ToString(),
                    Category = "调度"
                },
                new SystemStat
                {
                    Name = "Unscaled Delta",
                    Value = $"{_unscaledDeltaTime:F4}s",
                    Category = "时间"
                },
                new SystemStat
                {
                    Name = "Per-frame Timer",
                    Value = diagnostics.PerFrameUpdateCount.ToString(),
                    Category = "调度"
                }
            }
        };
    }

    private static LogFields CreateTimerDiagnosticsFields(TimerDiagnosticsSnapshot snapshot)
    {
        return new LogFields
        {
            ["activeCount"] = snapshot.ActiveCount,
            ["dispatchQueueCount"] = snapshot.DispatchQueueCount,
            ["perFrameUpdateCount"] = snapshot.PerFrameUpdateCount,
            ["entriesCount"] = snapshot.Entries.Count
        };
    }

    private static bool TryParseHandle(string id, out TimerHandle handle)
    {
        handle = default;
        var parts = id.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 2 ||
            !int.TryParse(parts[0], out var timerId) ||
            !int.TryParse(parts[1], out var generation))
        {
            return false;
        }

        handle = new TimerHandle(timerId, generation);
        return true;
    }
}
