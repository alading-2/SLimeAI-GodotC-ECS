using System.Collections.Generic;

/// <summary>
/// System 生命周期轻量 ring buffer。
/// <para>只保留最近事件，供 diagnostics / validation dump 使用。</para>
/// </summary>
public sealed class SystemLifecycleTrace
{
    private readonly int _capacity;
    private readonly Queue<SystemLifecycleTraceEntry> _entries;
    private long _sequence;

    public SystemLifecycleTrace(int capacity = 256)
    {
        _capacity = capacity < 1 ? 1 : capacity;
        _entries = new Queue<SystemLifecycleTraceEntry>(_capacity);
    }

    public int Count => _entries.Count;

    public void Record(
        string eventName,
        string systemId,
        ProjectStateSnapshot snapshot,
        SystemBlockedReasonCode reasonCode = SystemBlockedReasonCode.None,
        string message = "")
    {
        if (_entries.Count >= _capacity)
        {
            _entries.Dequeue();
        }

        _sequence++;
        _entries.Enqueue(new SystemLifecycleTraceEntry
        {
            Sequence = _sequence,
            EventName = eventName,
            SystemId = systemId,
            ReasonCode = reasonCode.ToString(),
            Message = message,
            FlowState = snapshot.FlowState.ToString(),
            Overlays = snapshot.Overlays.ToString(),
            SimulationState = snapshot.SimulationState.ToString()
        });
    }

    public List<SystemLifecycleTraceEntry> GetEntries()
    {
        return new List<SystemLifecycleTraceEntry>(_entries);
    }
}
