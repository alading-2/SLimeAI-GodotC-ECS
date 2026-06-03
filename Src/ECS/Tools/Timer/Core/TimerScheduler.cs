using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

/// <summary>
/// 纯 C# Timer 调度核心。它不依赖 Godot API，由外层 TimerManager 输入 delta 并在主线程派发 callback。
/// </summary>
public sealed class TimerScheduler
{
    private readonly Dictionary<int, TimerEntry> _entries = new();
    private readonly Dictionary<int, int> _generations = new();
    private readonly Stack<int> _freeIds = new();
    private readonly Dictionary<TimerClock, PriorityQueue<TimerQueueItem, TimerDuePriority>> _dueQueues = new()
    {
        [TimerClock.Game] = new PriorityQueue<TimerQueueItem, TimerDuePriority>(),
        [TimerClock.Real] = new PriorityQueue<TimerQueueItem, TimerDuePriority>(),
        [TimerClock.Fixed] = new PriorityQueue<TimerQueueItem, TimerDuePriority>()
    };
    private readonly Dictionary<TimerClock, double> _clockNow = new()
    {
        [TimerClock.Game] = 0,
        [TimerClock.Real] = 0,
        [TimerClock.Fixed] = 0
    };
    private readonly HashSet<TimerClock> _pausedClocks = new();
    private readonly Dictionary<TimerOwner, HashSet<int>> _ownerIndex = new();
    private readonly Dictionary<TimerPurpose, HashSet<int>> _purposeIndex = new();
    private readonly Dictionary<string, HashSet<int>> _tagIndex = new(StringComparer.Ordinal);
    private readonly List<int> _perFrameUpdateIds = new();
    private readonly Queue<TimerDispatchItem> _dispatchQueue = new();
    private int _nextId = 1;
    private long _sequence;
    private int _cancelledLazyHeapItems;
    private double _lastTickCostMs;
    private double _maxTickCostMs;
    private double _lastDispatchCostMs;
    private double _maxDispatchCostMs;
    private int _maxCallbacksDispatchedInFrame;

    public TimerHandle Delay(float duration, TimerOptions options, Action onComplete)
    {
        if (onComplete == null) throw new ArgumentNullException(nameof(onComplete));
        return Schedule(TimerMode.Delay, duration, duration, 0, options, null, null, null, onComplete);
    }

    public TimerHandle Loop(float interval, TimerOptions options, Action onLoop)
    {
        if (onLoop == null) throw new ArgumentNullException(nameof(onLoop));
        return Schedule(TimerMode.Loop, interval, interval, -1, options, onLoop, null, null, null);
    }

    public TimerHandle Repeat(float interval, int count, TimerOptions options, Action<int> onRepeat, Action? onComplete = null)
    {
        if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count), "Repeat count must be positive.");
        if (onRepeat == null) throw new ArgumentNullException(nameof(onRepeat));
        return Schedule(TimerMode.Repeat, interval, interval, count, options, null, onRepeat, null, onComplete);
    }

    public TimerHandle Countdown(float duration, float interval, TimerOptions options, Action<float, float> onTick, Action? onComplete = null)
    {
        if (duration <= 0) throw new ArgumentOutOfRangeException(nameof(duration), "Countdown duration must be positive.");
        if (interval <= 0) throw new ArgumentOutOfRangeException(nameof(interval), "Countdown interval must be positive.");
        if (onTick == null) throw new ArgumentNullException(nameof(onTick));
        return Schedule(TimerMode.Countdown, duration, interval, -1, options, null, null, onTick, onComplete);
    }

    public void Tick(TimerClock clock, float delta)
    {
        if (delta < 0) throw new ArgumentOutOfRangeException(nameof(delta), "Timer delta cannot be negative.");
        var startTimestamp = Stopwatch.GetTimestamp();

        if (!_pausedClocks.Contains(clock))
        {
            _clockNow[clock] += delta;
            UpdatePerFrameTimers(clock, delta);
            ProcessDue(clock);
        }

        _lastTickCostMs = Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;
        if (_lastTickCostMs > _maxTickCostMs)
        {
            _maxTickCostMs = _lastTickCostMs;
        }
    }

    public void DispatchDueCallbacks()
    {
        var startTimestamp = Stopwatch.GetTimestamp();
        var dispatched = 0;

        while (_dispatchQueue.Count > 0)
        {
            var item = _dispatchQueue.Dequeue();
            item.Callback();
            dispatched++;
        }

        if (dispatched > _maxCallbacksDispatchedInFrame)
        {
            _maxCallbacksDispatchedInFrame = dispatched;
        }

        _lastDispatchCostMs = Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;
        if (_lastDispatchCostMs > _maxDispatchCostMs)
        {
            _maxDispatchCostMs = _lastDispatchCostMs;
        }
    }

    public bool Cancel(TimerHandle handle, TimerCancelReason reason = TimerCancelReason.Manual)
    {
        if (!TryGetActiveEntry(handle, out var entry))
        {
            return false;
        }

        RemoveActiveEntry(entry, reason);
        return true;
    }

    public int CancelByOwner(TimerOwner owner, TimerCancelReason reason)
    {
        if (!_ownerIndex.TryGetValue(owner, out var ids) || ids.Count == 0)
        {
            return 0;
        }

        var targets = new List<TimerHandle>(ids.Count);
        foreach (var id in ids)
        {
            if (_entries.TryGetValue(id, out var entry))
            {
                targets.Add(entry.Handle);
            }
        }

        var count = 0;
        foreach (var handle in targets)
        {
            if (Cancel(handle, reason))
            {
                count++;
            }
        }

        return count;
    }

    public int CancelByOwnerAndPurpose(TimerOwner owner, TimerPurpose purpose, TimerCancelReason reason)
    {
        if (!_ownerIndex.TryGetValue(owner, out var ids) || ids.Count == 0)
        {
            return 0;
        }

        var targets = new List<TimerHandle>();
        foreach (var id in ids)
        {
            if (_entries.TryGetValue(id, out var entry) && entry.Purpose == purpose)
            {
                targets.Add(entry.Handle);
            }
        }

        var count = 0;
        foreach (var handle in targets)
        {
            if (Cancel(handle, reason))
            {
                count++;
            }
        }

        return count;
    }

    public int CancelByTag(string tag, TimerCancelReason reason)
    {
        if (!_tagIndex.TryGetValue(tag, out var ids) || ids.Count == 0)
        {
            return 0;
        }

        var targets = new List<TimerHandle>(ids.Count);
        foreach (var id in ids)
        {
            if (_entries.TryGetValue(id, out var entry))
            {
                targets.Add(entry.Handle);
            }
        }

        var count = 0;
        foreach (var handle in targets)
        {
            if (Cancel(handle, reason))
            {
                count++;
            }
        }

        return count;
    }

    public void SetAllPaused(bool paused, TimerPauseMask mask = TimerPauseMask.Manual)
    {
        var handles = new List<TimerHandle>(_entries.Count);
        foreach (var entry in _entries.Values)
        {
            handles.Add(entry.Handle);
        }

        foreach (var handle in handles)
        {
            if (paused)
            {
                Pause(handle, mask);
            }
            else
            {
                Resume(handle, mask);
            }
        }
    }

    public int SetPausedByTag(string tag, bool paused, TimerPauseMask mask = TimerPauseMask.Manual)
    {
        if (!_tagIndex.TryGetValue(tag, out var ids) || ids.Count == 0)
        {
            return 0;
        }

        var targets = new List<TimerHandle>(ids.Count);
        foreach (var id in ids)
        {
            if (_entries.TryGetValue(id, out var entry))
            {
                targets.Add(entry.Handle);
            }
        }

        foreach (var handle in targets)
        {
            if (paused)
            {
                Pause(handle, mask);
            }
            else
            {
                Resume(handle, mask);
            }
        }

        return targets.Count;
    }

    public bool SetOnUpdate(TimerHandle handle, Action<float>? onUpdate)
    {
        if (!TryGetActiveEntry(handle, out var entry))
        {
            return false;
        }

        entry.OnUpdate = onUpdate;
        if (onUpdate == null)
        {
            _perFrameUpdateIds.Remove(entry.Handle.Id);
        }
        else if (!_perFrameUpdateIds.Contains(entry.Handle.Id))
        {
            _perFrameUpdateIds.Add(entry.Handle.Id);
        }

        return true;
    }

    public void Clear(TimerCancelReason reason = TimerCancelReason.SceneExit)
    {
        var handles = new List<TimerHandle>(_entries.Count);
        foreach (var entry in _entries.Values)
        {
            handles.Add(entry.Handle);
        }

        foreach (var handle in handles)
        {
            Cancel(handle, reason);
        }
        _dispatchQueue.Clear();
    }

    public bool Pause(TimerHandle handle, TimerPauseMask mask)
    {
        if (!TryGetActiveEntry(handle, out var entry))
        {
            return false;
        }

        entry.PausedRemaining = entry.GetRemaining(_clockNow[entry.Clock]);
        entry.PauseMask |= mask;
        return true;
    }

    public bool Resume(TimerHandle handle, TimerPauseMask mask)
    {
        if (!TryGetActiveEntry(handle, out var entry))
        {
            return false;
        }

        entry.PauseMask &= ~mask;
        if (entry.PauseMask == TimerPauseMask.None)
        {
            entry.DueTime = _clockNow[entry.Clock] + entry.PausedRemaining;
            Enqueue(entry);
        }
        return true;
    }

    public void SetClockPaused(TimerClock clock, bool paused)
    {
        if (paused)
        {
            _pausedClocks.Add(clock);
        }
        else
        {
            _pausedClocks.Remove(clock);
        }
    }

    public bool TryGetRemaining(TimerHandle handle, out float remaining)
    {
        if (!TryGetActiveEntry(handle, out var entry))
        {
            remaining = 0;
            return false;
        }

        remaining = entry.GetRemaining(_clockNow[entry.Clock]);
        return true;
    }

    public bool TryGetProgress(TimerHandle handle, out float progress)
    {
        if (!TryGetActiveEntry(handle, out var entry))
        {
            progress = 1;
            return false;
        }

        progress = entry.GetProgress(_clockNow[entry.Clock]);
        return true;
    }

    public TimerDiagnosticsSnapshot GetTimerDiagnostics(TimerDiagnosticsFilter? filter = null)
    {
        var heapCounts = new Dictionary<TimerClock, int>();
        foreach (var pair in _dueQueues)
        {
            heapCounts[pair.Key] = pair.Value.Count;
        }

        var ownerTypeCounts = new Dictionary<TimerOwnerType, int>();
        var purposeCounts = new Dictionary<TimerPurpose, int>();
        var clockCounts = new Dictionary<TimerClock, int>();
        var entries = new List<TimerObservation>(_entries.Count);
        var leakHints = new List<string>();
        var ownerCounts = new Dictionary<TimerOwner, int>();
        var pausedCount = 0;
        var maxEntries = Math.Max(0, filter?.MaxEntries ?? 100);

        foreach (var entry in _entries.Values)
        {
            if (entry.PauseMask != TimerPauseMask.None)
            {
                pausedCount++;
            }

            Increment(ownerTypeCounts, entry.Owner.Type);
            Increment(purposeCounts, entry.Purpose);
            Increment(clockCounts, entry.Clock);
            Increment(ownerCounts, entry.Owner);

            if (entry.Owner.IsNone)
            {
                leakHints.Add($"Timer {entry.Handle.Id}:{entry.Handle.Generation} has no owner.");
            }
            if (entry.Purpose == TimerPurpose.None)
            {
                leakHints.Add($"Timer {entry.Handle.Id}:{entry.Handle.Generation} has no purpose.");
            }

            if (MatchesFilter(entry, filter) && entries.Count < maxEntries)
            {
                entries.Add(entry.ToObservation(_clockNow[entry.Clock]));
            }
        }

        var topOwners = ownerCounts
            .OrderByDescending(item => item.Value)
            .Take(10)
            .Select(item => new TimerOwnerCount(item.Key, item.Value))
            .ToArray();

        return new TimerDiagnosticsSnapshot(
            _entries.Count,
            pausedCount,
            _dispatchQueue.Count,
            _perFrameUpdateIds.Count,
            heapCounts,
            ownerTypeCounts,
            purposeCounts,
            clockCounts,
            _cancelledLazyHeapItems,
            _lastTickCostMs,
            _maxTickCostMs,
            _lastDispatchCostMs,
            _maxDispatchCostMs,
            _maxCallbacksDispatchedInFrame,
            topOwners,
            leakHints,
            entries);
    }

    private static bool MatchesFilter(TimerEntry entry, TimerDiagnosticsFilter? filter)
    {
        if (filter == null)
        {
            return true;
        }

        if (filter.Owner.HasValue && entry.Owner != filter.Owner.Value)
        {
            return false;
        }

        if (filter.Purpose.HasValue && entry.Purpose != filter.Purpose.Value)
        {
            return false;
        }

        if (filter.Clock.HasValue && entry.Clock != filter.Clock.Value)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(filter.Tag) && !string.Equals(entry.Tag, filter.Tag, StringComparison.Ordinal))
        {
            return false;
        }

        return true;
    }

    private TimerHandle Schedule(
        TimerMode mode,
        float duration,
        float interval,
        int repeatRemaining,
        TimerOptions options,
        Action? onLoop,
        Action<int>? onRepeat,
        Action<float, float>? onCountdown,
        Action? onComplete)
    {
        if (duration <= 0) throw new ArgumentOutOfRangeException(nameof(duration), "Timer duration must be positive.");
        if (interval <= 0) throw new ArgumentOutOfRangeException(nameof(interval), "Timer interval must be positive.");
        if (options == null) throw new ArgumentNullException(nameof(options));

        var handle = AllocateHandle();
        var now = _clockNow[options.Clock];
        var entry = new TimerEntry
        {
            Handle = handle,
            Mode = mode,
            Clock = options.Clock,
            Owner = options.Owner,
            Purpose = options.Purpose,
            Tag = options.Tag,
            Duration = duration,
            Interval = interval,
            DueTime = now + interval,
            StartedAt = now,
            LastProgressNow = now,
            RepeatRemaining = repeatRemaining,
            OnLoop = onLoop,
            OnRepeat = onRepeat,
            OnCountdown = onCountdown,
            OnComplete = onComplete,
            OnUpdate = options.OnUpdate,
            Source = options.Source
        };

        _entries[handle.Id] = entry;
        AddIndex(_ownerIndex, entry.Owner, handle.Id);
        AddIndex(_purposeIndex, entry.Purpose, handle.Id);
        if (!string.IsNullOrWhiteSpace(entry.Tag))
        {
            AddIndex(_tagIndex, entry.Tag, handle.Id);
        }
        if (entry.OnUpdate != null)
        {
            _perFrameUpdateIds.Add(handle.Id);
        }
        Enqueue(entry);
        return handle;
    }

    private TimerHandle AllocateHandle()
    {
        var id = _freeIds.Count > 0 ? _freeIds.Pop() : _nextId++;
        _generations.TryGetValue(id, out var generation);
        generation++;
        _generations[id] = generation;
        return new TimerHandle(id, generation);
    }

    private void Enqueue(TimerEntry entry)
    {
        _dueQueues[entry.Clock].Enqueue(
            new TimerQueueItem(entry.Handle, entry.DueTime),
            new TimerDuePriority(entry.DueTime, ++_sequence));
    }

    private void ProcessDue(TimerClock clock)
    {
        var now = _clockNow[clock];
        var queue = _dueQueues[clock];

        while (queue.TryPeek(out var item, out var priority) && priority.DueTime <= now)
        {
            queue.Dequeue();

            if (!TryGetActiveEntry(item.Handle, out var entry))
            {
                _cancelledLazyHeapItems++;
                continue;
            }

            if (Math.Abs(item.DueTime - entry.DueTime) > 0.000001d)
            {
                _cancelledLazyHeapItems++;
                continue;
            }

            if (entry.PauseMask != TimerPauseMask.None)
            {
                continue;
            }

            ProcessDueEntry(entry, now);
        }
    }

    private void ProcessDueEntry(TimerEntry entry, double now)
    {
        switch (entry.Mode)
        {
            case TimerMode.Delay:
                var delayComplete = entry.OnComplete;
                CompleteEntry(entry, () => delayComplete?.Invoke());
                break;
            case TimerMode.Loop:
                var loopCallback = entry.OnLoop;
                _dispatchQueue.Enqueue(new TimerDispatchItem(entry.Handle, () => loopCallback?.Invoke()));
                RescheduleRepeating(entry, now);
                break;
            case TimerMode.Repeat:
                entry.RepeatRemaining--;
                var remaining = entry.RepeatRemaining;
                var repeatCallback = entry.OnRepeat;
                _dispatchQueue.Enqueue(new TimerDispatchItem(entry.Handle, () => repeatCallback?.Invoke(remaining)));
                if (entry.RepeatRemaining <= 0)
                {
                    var repeatComplete = entry.OnComplete;
                    CompleteEntry(entry, () => repeatComplete?.Invoke());
                }
                else
                {
                    RescheduleRepeating(entry, now);
                }
                break;
            case TimerMode.Countdown:
                entry.TotalElapsed = Math.Min(entry.Duration, entry.TotalElapsed + entry.Interval);
                var elapsed = (float)entry.TotalElapsed;
                var progress = entry.Duration > 0 ? Math.Clamp(elapsed / entry.Duration, 0f, 1f) : 1f;
                var countdownCallback = entry.OnCountdown;
                _dispatchQueue.Enqueue(new TimerDispatchItem(entry.Handle, () => countdownCallback?.Invoke(elapsed, progress)));
                if (entry.TotalElapsed >= entry.Duration)
                {
                    var countdownComplete = entry.OnComplete;
                    CompleteEntry(entry, () => countdownComplete?.Invoke());
                }
                else
                {
                    RescheduleRepeating(entry, now);
                }
                break;
            default:
                throw new InvalidOperationException($"Unknown timer mode: {entry.Mode}");
        }
    }

    private void RescheduleRepeating(TimerEntry entry, double now)
    {
        // catch-up 使用 dueTime 累加，避免帧率波动时持续漂移。
        entry.DueTime += entry.Interval;
        Enqueue(entry);
    }

    private void CompleteEntry(TimerEntry entry, Action callback)
    {
        var handle = entry.Handle;
        RemoveActiveEntry(entry, TimerCancelReason.Completed);
        _dispatchQueue.Enqueue(new TimerDispatchItem(handle, callback));
    }

    private void UpdatePerFrameTimers(TimerClock clock, float delta)
    {
        if (_perFrameUpdateIds.Count == 0)
        {
            return;
        }

        for (var i = _perFrameUpdateIds.Count - 1; i >= 0; i--)
        {
            var id = _perFrameUpdateIds[i];
            if (!_entries.TryGetValue(id, out var entry))
            {
                _perFrameUpdateIds.RemoveAt(i);
                continue;
            }

            if (entry.Clock != clock || entry.PauseMask != TimerPauseMask.None)
            {
                continue;
            }

            entry.OnUpdate?.Invoke(entry.GetProgress(_clockNow[clock]));
            entry.LastProgressNow += delta;
        }
    }

    private bool TryGetActiveEntry(TimerHandle handle, out TimerEntry entry)
    {
        if (handle.IsValid &&
            _entries.TryGetValue(handle.Id, out entry!) &&
            entry.Handle.Generation == handle.Generation)
        {
            return true;
        }

        entry = null!;
        return false;
    }

    private bool IsHandleActive(TimerHandle handle) => TryGetActiveEntry(handle, out _);

    private void RemoveActiveEntry(TimerEntry entry, TimerCancelReason reason)
    {
        _entries.Remove(entry.Handle.Id);
        RemoveIndex(_ownerIndex, entry.Owner, entry.Handle.Id);
        RemoveIndex(_purposeIndex, entry.Purpose, entry.Handle.Id);
        if (!string.IsNullOrWhiteSpace(entry.Tag))
        {
            RemoveIndex(_tagIndex, entry.Tag, entry.Handle.Id);
        }
        _perFrameUpdateIds.Remove(entry.Handle.Id);
        entry.CancelReason = reason;
        entry.ClearCallbacks();
        _freeIds.Push(entry.Handle.Id);
    }

    private static void AddIndex<TKey>(Dictionary<TKey, HashSet<int>> index, TKey key, int id) where TKey : notnull
    {
        if (!index.TryGetValue(key, out var ids))
        {
            ids = new HashSet<int>();
            index[key] = ids;
        }

        ids.Add(id);
    }

    private static void RemoveIndex<TKey>(Dictionary<TKey, HashSet<int>> index, TKey key, int id) where TKey : notnull
    {
        if (!index.TryGetValue(key, out var ids))
        {
            return;
        }

        ids.Remove(id);
        if (ids.Count == 0)
        {
            index.Remove(key);
        }
    }

    private static void Increment<TKey>(Dictionary<TKey, int> counts, TKey key) where TKey : notnull
    {
        counts.TryGetValue(key, out var count);
        counts[key] = count + 1;
    }

    private sealed class TimerEntry
    {
        public TimerHandle Handle;
        public TimerMode Mode;
        public TimerClock Clock;
        public TimerOwner Owner;
        public TimerPurpose Purpose;
        public string? Tag;
        public float Duration;
        public float Interval;
        public double StartedAt;
        public double DueTime;
        public double PausedRemaining;
        public double LastProgressNow;
        public double TotalElapsed;
        public int RepeatRemaining;
        public TimerPauseMask PauseMask;
        public TimerCancelReason CancelReason;
        public Action? OnLoop;
        public Action<int>? OnRepeat;
        public Action<float, float>? OnCountdown;
        public Action? OnComplete;
        public Action<float>? OnUpdate;
        public string? Source;

        public float GetRemaining(double now)
        {
            if (PauseMask != TimerPauseMask.None)
            {
                return (float)Math.Max(0, PausedRemaining);
            }

            return (float)Math.Max(0, DueTime - now);
        }

        public float GetProgress(double now)
        {
            if (Duration <= 0)
            {
                return 1f;
            }

            return Math.Clamp((float)((now - StartedAt) / Duration), 0f, 1f);
        }

        public TimerObservation ToObservation(double now)
        {
            return new TimerObservation(
                Handle,
                Mode,
                Clock,
                Owner,
                Purpose,
                Tag,
                Duration,
                Interval,
                GetRemaining(now),
                GetProgress(now),
                RepeatRemaining,
                PauseMask,
                CancelReason,
                Source);
        }

        public void ClearCallbacks()
        {
            OnLoop = null;
            OnRepeat = null;
            OnCountdown = null;
            OnComplete = null;
            OnUpdate = null;
        }
    }

    private readonly record struct TimerQueueItem(TimerHandle Handle, double DueTime);

    private readonly record struct TimerDuePriority(double DueTime, long Sequence) : IComparable<TimerDuePriority>
    {
        public int CompareTo(TimerDuePriority other)
        {
            var dueCompare = DueTime.CompareTo(other.DueTime);
            return dueCompare != 0 ? dueCompare : Sequence.CompareTo(other.Sequence);
        }
    }

    private readonly record struct TimerDispatchItem(TimerHandle Handle, Action Callback);
}
