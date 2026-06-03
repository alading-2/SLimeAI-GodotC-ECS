using System.Collections.Generic;

/// <summary>
/// Timer 显式诊断快照。普通 tick 不创建该对象，只有调用 diagnostics API 时才分配。
/// </summary>
public sealed record TimerDiagnosticsSnapshot(
    int ActiveCount,
    int PausedCount,
    int DispatchQueueCount,
    int PerFrameUpdateCount,
    IReadOnlyDictionary<TimerClock, int> HeapCountByClock,
    IReadOnlyDictionary<TimerOwnerType, int> ActiveCountByOwnerType,
    IReadOnlyDictionary<TimerPurpose, int> ActiveCountByPurpose,
    IReadOnlyDictionary<TimerClock, int> ActiveCountByClock,
    int CancelledLazyHeapItems,
    double LastTickCostMs,
    double MaxTickCostMs,
    double LastDispatchCostMs,
    double MaxDispatchCostMs,
    int MaxCallbacksDispatchedInFrame,
    IReadOnlyList<TimerOwnerCount> TopOwners,
    IReadOnlyList<string> LeakHints,
    IReadOnlyList<TimerObservation> Entries);
