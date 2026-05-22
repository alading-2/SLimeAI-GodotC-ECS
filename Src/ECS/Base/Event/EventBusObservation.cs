using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

/// <summary>
/// 单个事件总线实例的可观测数据。记录订阅、发布、作用域拒绝、同类型重入阻断和 handler 异常。
/// </summary>
public sealed class EventBusObservation
{
    private const int MaxHandlerExceptions = 64;
    private const string SchemaVersion = "1.0";

    private readonly object _sync = new();
    private readonly Dictionary<Type, SubscriptionStats> _subscriptions = new();
    private readonly Dictionary<Type, long> _publishCounts = new();
    private readonly Dictionary<Type, long> _reentryBlockedCounts = new();
    private readonly LinkedList<HandlerExceptionRecord> _exceptions = new();

    internal void RecordSubscribe(Type eventType, string handlerLabel, int registrationOrder)
    {
        lock (_sync)
        {
            if (!_subscriptions.TryGetValue(eventType, out var stats))
            {
                stats = new SubscriptionStats();
                _subscriptions[eventType] = stats;
            }

            stats.Handlers.Add(new SubscriptionEntry(handlerLabel, registrationOrder));
        }
    }

    internal void RecordUnsubscribe(Type eventType, int registrationOrder)
    {
        lock (_sync)
        {
            if (!_subscriptions.TryGetValue(eventType, out var stats))
            {
                return;
            }

            stats.Handlers.RemoveAll(entry => entry.RegistrationOrder == registrationOrder);
            if (stats.Handlers.Count == 0)
            {
                _subscriptions.Remove(eventType);
            }
        }
    }

    internal void RecordPublish(Type eventType)
    {
        lock (_sync)
        {
            _publishCounts.TryGetValue(eventType, out var count);
            _publishCounts[eventType] = count + 1;
        }
    }

    internal void RecordSameTypeReentry(Type eventType, string callChain)
    {
        lock (_sync)
        {
            _reentryBlockedCounts.TryGetValue(eventType, out var count);
            _reentryBlockedCounts[eventType] = count + 1;
            AppendException(new HandlerExceptionRecord(
                EventType: eventType.FullName ?? eventType.Name,
                HandlerLabel: "(reentry guard)",
                ExceptionType: "SameTypeReentryBlocked",
                Message: callChain));
        }
    }

    internal void RecordRejectedPublish(Type eventType, string message)
    {
        lock (_sync)
        {
            AppendException(new HandlerExceptionRecord(
                EventType: eventType.FullName ?? eventType.Name,
                HandlerLabel: "(scope guard)",
                ExceptionType: "RejectedPublish",
                Message: message));
        }
    }

    internal void RecordHandlerException(Type eventType, string handlerLabel, Exception ex)
    {
        lock (_sync)
        {
            AppendException(new HandlerExceptionRecord(
                EventType: eventType.FullName ?? eventType.Name,
                HandlerLabel: handlerLabel,
                ExceptionType: ex.GetType().FullName ?? ex.GetType().Name,
                Message: ex.Message));
        }
    }

    /// <summary>
    /// 按当前状态生成 eventbus-dump.json 文件。
    /// </summary>
    public void ExportTo(string busName, string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var dump = Snapshot(busName);
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };
        File.WriteAllText(path, JsonSerializer.Serialize(dump, options), Encoding.UTF8);
    }

    private void AppendException(HandlerExceptionRecord record)
    {
        _exceptions.AddLast(record);
        while (_exceptions.Count > MaxHandlerExceptions)
        {
            _exceptions.RemoveFirst();
        }
    }

    private EventBusObservationDump Snapshot(string busName)
    {
        lock (_sync)
        {
            var subscriptionDump = new Dictionary<string, SubscriptionDumpEntry>();
            foreach (var (eventType, stats) in _subscriptions)
            {
                var handlers = new List<SubscriptionDumpHandler>(stats.Handlers.Count);
                foreach (var entry in stats.Handlers)
                {
                    handlers.Add(new SubscriptionDumpHandler(entry.HandlerLabel, entry.RegistrationOrder));
                }

                subscriptionDump[eventType.FullName ?? eventType.Name] = new SubscriptionDumpEntry(handlers.Count, handlers);
            }

            var emittedCounts = new Dictionary<string, long>();
            foreach (var (eventType, count) in _publishCounts)
            {
                emittedCounts[eventType.FullName ?? eventType.Name] = count;
            }

            var sameTypeReentry = new Dictionary<string, long>();
            foreach (var (eventType, count) in _reentryBlockedCounts)
            {
                sameTypeReentry[eventType.FullName ?? eventType.Name] = count;
            }

            var registrationOrder = new List<string>();
            foreach (var (_, stats) in _subscriptions)
            {
                foreach (var entry in stats.Handlers)
                {
                    registrationOrder.Add($"{entry.RegistrationOrder}:{entry.HandlerLabel}");
                }
            }

            return new EventBusObservationDump(
                SchemaVersion,
                busName,
                DateTime.UtcNow,
                subscriptionDump,
                emittedCounts,
                sameTypeReentry,
                new List<HandlerExceptionRecord>(_exceptions),
                registrationOrder);
        }
    }

    private sealed class SubscriptionStats
    {
        public List<SubscriptionEntry> Handlers { get; } = new();
    }

    private readonly record struct SubscriptionEntry(string HandlerLabel, int RegistrationOrder);

    public readonly record struct HandlerExceptionRecord(
        string EventType,
        string HandlerLabel,
        string ExceptionType,
        string Message);

    private readonly record struct SubscriptionDumpEntry(
        int HandlerCount,
        List<SubscriptionDumpHandler> Handlers);

    private readonly record struct SubscriptionDumpHandler(
        string HandlerLabel,
        int RegistrationOrder);

    private readonly record struct EventBusObservationDump(
        string SchemaVersion,
        string BusName,
        DateTime GeneratedAtUtc,
        Dictionary<string, SubscriptionDumpEntry> Subscriptions,
        Dictionary<string, long> EmittedCounts,
        Dictionary<string, long> SameTypeReentryBlockedCounts,
        List<HandlerExceptionRecord> HandlerExceptions,
        List<string> HandlerRegistrationOrder);
}
