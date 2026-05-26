using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

// ==================================================================================
// EventBusObservation — 事件总线可观测数据采集与导出
// ==================================================================================
//
// 每个 EntityEventBus / WorldEventBus 持有一个 EventBusObservation 实例，
// 在运行时持续采集以下数据：
//   - 订阅/退订事件（含 handler 标签和注册序号）
//   - 发布计数（按事件类型统计）
//   - 作用域拒绝（EntityEventBus 拒绝 IGlobalEvent、WorldEventBus 拒绝 IEntityEvent）
//   - 同类型重入阻断（同一 bus 上正在派发 T 时再次 Publish<T>）
//   - Handler 异常（单个 handler 抛异常不阻断后续，异常被记录）
//
// 通过 ExportTo 导出 JSON 文件，用于：
//   - 运行时调试和事件流审计
//   - Godot 场景 artifact 验证
//   - AI agent 诊断事件系统状态
//
// 导出格式：eventbus-dump.json
//   SchemaVersion | BusName | GeneratedAtUtc | Subscriptions | EmittedCounts |
//   SameTypeReentryBlockedCounts | HandlerExceptions | HandlerRegistrationOrder
//
// 线程安全：所有 Record* 方法通过 _sync 锁保护，ExportTo 在 Snapshot 中加锁。
// HandlerExceptions 保留最近 64 条，超出后丢弃最旧记录。
// ==================================================================================

/// <summary>
/// 单个事件总线实例的可观测数据。记录订阅、发布、作用域拒绝、同类型重入阻断和 handler 异常。
/// 通过 ExportTo 导出 JSON，用于运行时调试、场景 artifact 验证和 AI 诊断。
/// </summary>
public sealed class EventBusObservation
{
    /// <summary>Handler 异常记录上限，超出后丢弃最旧记录</summary>
    private const int MaxHandlerExceptions = 64;

    /// <summary>导出 JSON 的 schema 版本号</summary>
    private const string SchemaVersion = "1.0";

    /// <summary>线程安全锁，保护所有 Record* 方法和 Snapshot</summary>
    private readonly object _sync = new();

    /// <summary>按事件类型索引的订阅统计</summary>
    private readonly Dictionary<Type, SubscriptionStats> _subscriptions = new();

    /// <summary>按事件类型统计的发布计数</summary>
    private readonly Dictionary<Type, long> _publishCounts = new();

    /// <summary>按事件类型统计的同类型重入阻断计数</summary>
    private readonly Dictionary<Type, long> _reentryBlockedCounts = new();

    /// <summary>Handler 异常记录链表，保留最近 MaxHandlerExceptions 条</summary>
    private readonly LinkedList<HandlerExceptionRecord> _exceptions = new();

    /// <summary>记录订阅事件。由 EntityEventBus.Subscribe / WorldEventBus.Subscribe 调用。</summary>
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

    /// <summary>记录退订事件。由 SubscriptionToken.Dispose 触发。</summary>
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

    /// <summary>记录发布事件。每次 Publish 调用时递增对应类型的计数。</summary>
    internal void RecordPublish(Type eventType)
    {
        lock (_sync)
        {
            _publishCounts.TryGetValue(eventType, out var count);
            _publishCounts[eventType] = count + 1;
        }
    }

    /// <summary>记录同类型重入阻断。同一 bus 上正在派发 T 时再次 Publish<T> 被阻断。</summary>
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

    /// <summary>记录作用域拒绝发布。如 EntityEventBus 拒绝 IGlobalEvent-only payload。</summary>
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

    /// <summary>记录 handler 异常。单个 handler 异常不阻断后续 handler 执行。</summary>
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
    /// 输出字段：SchemaVersion, BusName, GeneratedAtUtc, Subscriptions,
    /// EmittedCounts, SameTypeReentryBlockedCounts, HandlerExceptions, HandlerRegistrationOrder。
    /// </summary>
    /// <param name="busName">总线名称</param>
    /// <param name="path">输出路径，支持绝对路径</param>
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
