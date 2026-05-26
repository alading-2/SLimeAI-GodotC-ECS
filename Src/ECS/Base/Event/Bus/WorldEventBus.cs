using Godot;
using System;
using System.Collections.Generic;

// ==================================================================================
// WorldEventBus — World 级事件总线实现
// ==================================================================================
//
// 全局唯一实例，通过 WorldEvents.World 静态属性访问。
// 替代旧 GlobalEventBus.Global 入口。
//
// Scope 路由规则：
//   Publish<T> 时检查 T 的 marker interface：
//     - 未实现 IGlobalEvent → 拒绝发布，记录 RejectedPublish
//     - 实现 IGlobalEvent    → 本地派发
//
// RouteBroadcast（IWorldEventBusRouter 实现）：
//   EntityEventBus 在派发 IBroadcastEvent 后自动调用 RouteBroadcast，
//   WorldEventBus 在此方法中再次执行本地派发，使 world 级订阅者也能收到广播事件。
//
// 同类型重入保护、Handler 异常隔离、退订机制与 EntityEventBus 一致。
// ==================================================================================

/// <summary>
/// World 级事件总线。接受 IGlobalEvent 和 IBroadcastEvent；拒绝 IEntityEvent-only。
/// 唯一实例入口: <see cref="WorldEvents.World"/>
/// </summary>
public class WorldEventBus : IWorldEventBus
{
    /// <summary>按事件类型索引的订阅列表</summary>
    private readonly Dictionary<Type, List<Subscription>> _subscriptions = new();

    /// <summary>正在派发的事件类型集合，用于同类型重入保护</summary>
    private readonly HashSet<Type> _dispatchingTypes = new();

    /// <summary>可观测数据采集器</summary>
    private readonly EventBusObservation _observation = new();

    /// <summary>下一个订阅的注册序号，用于 observation 记录注册顺序</summary>
    private int _nextRegistrationOrder;

    public string BusName => "world";

    /// <summary>
    /// 发布 typed event。执行 scope 检查 → 本地派发。
    /// </summary>
    public void Publish<T>(in T @event) where T : struct, IEvent
    {
        var eventType = typeof(T);

        // Scope 检查：WorldEventBus 只接受 IGlobalEvent（含 IBroadcastEvent）
        if (!typeof(IGlobalEvent).IsAssignableFrom(eventType))
        {
            _observation.RecordRejectedPublish(
                eventType,
                $"WorldEventBus rejected {eventType.FullName}: payload must implement IGlobalEvent or IBroadcastEvent.");
            return;
        }

        DispatchLocal(eventType, in @event);
    }

    /// <summary>
    /// 订阅 typed event。返回 IDisposable token 作为唯一退订入口。
    /// </summary>
    public IDisposable Subscribe<T>(Action<T> handler) where T : struct, IEvent
    {
        ArgumentNullException.ThrowIfNull(handler);

        var eventType = typeof(T);
        if (!_subscriptions.TryGetValue(eventType, out var list))
        {
            list = new List<Subscription>();
            _subscriptions[eventType] = list;
        }

        var order = ++_nextRegistrationOrder;
        var subscription = new Subscription(eventType, handler, order);
        list.Add(subscription);
        _observation.RecordSubscribe(eventType, HandlerLabel(handler), order);

        return new SubscriptionToken(this, subscription);
    }

    public void ExportObservation(string path)
    {
        _observation.ExportTo(BusName, NormalizePath(path));
    }

    /// <summary>
    /// IWorldEventBusRouter 实现：接收 EntityEventBus 转发的广播事件。
    /// 仅当 payload 实现 IGlobalEvent 时才执行本地派发。
    /// </summary>
    public void RouteBroadcast<T>(in T @event) where T : struct, IEvent
    {
        if (@event is not IGlobalEvent)
        {
            return;
        }

        DispatchLocal(typeof(T), in @event);
    }

    /// <summary>
    /// 清空订阅和派发状态。供测试 teardown 或 runtime 重启使用。
    /// </summary>
    public void Clear()
    {
        _subscriptions.Clear();
        _dispatchingTypes.Clear();
    }

    /// <summary>
    /// 本地派发：按注册顺序调用 handler，带重入保护和异常隔离。
    /// 与 EntityEventBus.DispatchLocal 逻辑一致。
    /// </summary>
    private void DispatchLocal<T>(Type eventType, in T @event) where T : struct, IEvent
    {
        _observation.RecordPublish(eventType);

        // 同类型重入保护
        if (!_dispatchingTypes.Add(eventType))
        {
            var callChain = $"reentry on {eventType.FullName} within bus {BusName}";
            _observation.RecordSameTypeReentry(eventType, callChain);
            return;
        }

        try
        {
            if (!_subscriptions.TryGetValue(eventType, out var list) || list.Count == 0)
            {
                return;
            }

            // ToArray 防止 handler 中修改订阅列表导致并发异常
            foreach (var subscription in list.ToArray())
            {
                if (subscription.IsDisposed)
                {
                    continue;
                }

                try
                {
                    ((Action<T>)subscription.Handler)(@event);
                }
                catch (Exception ex)
                {
                    // 异常隔离：单个 handler 异常不阻断后续 handler
                    _observation.RecordHandlerException(eventType, HandlerLabel(subscription.Handler), ex);
                }
            }
        }
        finally
        {
            _dispatchingTypes.Remove(eventType);
        }
    }

    /// <summary>
    /// 通过 SubscriptionToken.Dispose 触发的退订逻辑。
    /// </summary>
    private void Unsubscribe(Subscription subscription)
    {
        if (subscription.IsDisposed)
        {
            return;
        }

        subscription.IsDisposed = true;
        if (_subscriptions.TryGetValue(subscription.EventType, out var list))
        {
            list.Remove(subscription);
            if (list.Count == 0)
            {
                _subscriptions.Remove(subscription.EventType);
            }
        }

        _observation.RecordUnsubscribe(subscription.EventType, subscription.RegistrationOrder);
    }

    /// <summary>
    /// 生成 handler 的可读标签，用于 observation dump。格式: "TypeName.MethodName"
    /// </summary>
    private static string HandlerLabel(Delegate handler)
    {
        var target = handler.Target?.GetType().Name ?? "static";
        return $"{target}.{handler.Method.Name}";
    }

    /// <summary>
    /// 将 "user://" 路径转换为绝对路径，其他路径原样返回。
    /// </summary>
    private static string NormalizePath(string path)
    {
        return path.StartsWith("user://", StringComparison.Ordinal)
            ? ProjectSettings.GlobalizePath(path)
            : path;
    }

    /// <summary>
    /// 内部订阅记录，持有事件类型、handler 委托和注册序号。
    /// </summary>
    private sealed class Subscription
    {
        public Subscription(Type eventType, Delegate handler, int registrationOrder)
        {
            EventType = eventType;
            Handler = handler;
            RegistrationOrder = registrationOrder;
        }

        public Type EventType { get; }
        public Delegate Handler { get; }
        public int RegistrationOrder { get; }
        public bool IsDisposed { get; set; }
    }

    /// <summary>
    /// Subscribe 返回的退订 token。Dispose 时从 bus 移除对应订阅。
    /// </summary>
    private sealed class SubscriptionToken : IDisposable
    {
        private readonly WorldEventBus _bus;
        private readonly Subscription _subscription;

        public SubscriptionToken(WorldEventBus bus, Subscription subscription)
        {
            _bus = bus;
            _subscription = subscription;
        }

        public void Dispose()
        {
            _bus.Unsubscribe(_subscription);
        }
    }
}
