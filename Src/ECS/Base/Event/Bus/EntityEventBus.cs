using Godot;
using System;
using System.Collections.Generic;

// ==================================================================================
// EntityEventBus — 实体级事件总线实现
// ==================================================================================
//
// 每个 Entity 持有一个 EntityEventBus 实例（通过 IEntity.Events 暴露为 IEventBus）。
//
// Scope 路由规则：
//   Publish<T> 时检查 T 的 marker interface：
//     - 未实现 IEntityEvent → 拒绝发布，记录 RejectedPublish
//     - 实现 IEntityEvent    → 本地派发
//     - 同时实现 IGlobalEvent（即 IBroadcastEvent）→ 本地派发 + 自动路由到 WorldEventBus
//
// 同类型重入保护：
//   同一 bus 上，正在派发 T 类型事件时，若 handler 再次 Publish<T>，第二次发布被阻断。
//   不同类型事件允许级联（Publish<A> 的 handler 可以 Publish<B>）。
//   不同 bus 的同类型事件互不影响。
//
// Handler 异常隔离：
//   单个 handler 抛异常不阻断后续 handler 执行，异常被记录到 observation。
//
// 退订机制：
//   Subscribe 返回 SubscriptionToken（IDisposable），Dispose 后从订阅列表移除。
//   Entity 释放时通过 Clear() 批量清空，避免池化实体保留旧回调。
// ==================================================================================

/// <summary>
/// 实体级事件总线。支持 IEntityEvent 和 IBroadcastEvent；
/// 对 IBroadcastEvent 自动转发到 WorldEventBus。
/// </summary>
public sealed class EntityEventBus : IEventBus
{
    /// <summary>按事件类型索引的订阅列表</summary>
    private readonly Dictionary<Type, List<Subscription>> _subscriptions = new();

    /// <summary>正在派发的事件类型集合，用于同类型重入保护</summary>
    private readonly HashSet<Type> _dispatchingTypes = new();

    /// <summary>可观测数据采集器</summary>
    private readonly EventBusObservation _observation = new();

    /// <summary>World 总线路由器引用，用于广播事件自动转发。null 时无法转发。</summary>
    private readonly IWorldEventBusRouter? _worldRouter;

    /// <summary>下一个订阅的注册序号，用于 observation 记录注册顺序</summary>
    private int _nextRegistrationOrder;

    /// <summary>
    /// 构造实体事件总线。
    /// </summary>
    /// <param name="busName">总线名称，建议使用 EventBusNames.ForEntity(entity) 生成</param>
    /// <param name="worldRouter">World 总线路由器，传入 WorldEvents.World 以启用广播转发</param>
    public EntityEventBus(string busName, IWorldEventBusRouter? worldRouter = null)
    {
        if (string.IsNullOrWhiteSpace(busName))
        {
            throw new ArgumentException("Event bus name cannot be empty.", nameof(busName));
        }

        BusName = busName;
        _worldRouter = worldRouter;
    }

    public string BusName { get; }

    /// <summary>
    /// 发布 typed event。执行 scope 检查 → 本地派发 → 广播路由。
    /// </summary>
    public void Publish<T>(in T @event) where T : struct, IEvent
    {
        var eventType = typeof(T);

        // Scope 检查：EntityEventBus 只接受 IEntityEvent（含 IBroadcastEvent）
        if (!typeof(IEntityEvent).IsAssignableFrom(eventType))
        {
            _observation.RecordRejectedPublish(
                eventType,
                $"EntityEventBus {BusName} rejected {eventType.FullName}: payload must implement IEntityEvent or IBroadcastEvent.");
            return;
        }

        // 本地派发
        var dispatched = DispatchLocal(eventType, in @event);

        // 广播路由：payload 同时实现 IGlobalEvent（即 IBroadcastEvent）时自动转发到 WorldEventBus
        if (dispatched && @event is IGlobalEvent && _worldRouter != null)
        {
            _worldRouter.RouteBroadcast(in @event);
        }
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
    /// 实体释放时清空所有订阅，避免池化实体保留旧回调。
    /// 常规退订仍应优先使用 Subscribe 返回的 IDisposable。
    /// </summary>
    public void Clear()
    {
        _subscriptions.Clear();
        _dispatchingTypes.Clear();
    }

    /// <summary>
    /// 本地派发：按注册顺序调用 handler，带重入保护和异常隔离。
    /// </summary>
    /// <returns>true 表示派发成功（或有订阅但无 handler 异常阻断）；false 表示被重入保护阻断</returns>
    private bool DispatchLocal<T>(Type eventType, in T @event) where T : struct, IEvent
    {
        _observation.RecordPublish(eventType);

        // 同类型重入保护：同一 bus 上正在派发 T 时，再次 Publish<T> 被阻断
        if (!_dispatchingTypes.Add(eventType))
        {
            var callChain = $"reentry on {eventType.FullName} within bus {BusName}";
            _observation.RecordSameTypeReentry(eventType, callChain);
            return false;
        }

        try
        {
            if (!_subscriptions.TryGetValue(eventType, out var list) || list.Count == 0)
            {
                return true;
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

            return true;
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
        private readonly EntityEventBus _bus;
        private readonly Subscription _subscription;

        public SubscriptionToken(EntityEventBus bus, Subscription subscription)
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
