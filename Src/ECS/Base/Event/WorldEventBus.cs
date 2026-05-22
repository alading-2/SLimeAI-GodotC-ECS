using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// World 级事件总线。接受 IGlobalEvent / IBroadcastEvent；拒绝 IEntityEvent-only。
/// </summary>
public class WorldEventBus : IWorldEventBus
{
    private readonly Dictionary<Type, List<Subscription>> _subscriptions = new();
    private readonly HashSet<Type> _dispatchingTypes = new();
    private readonly EventBusObservation _observation = new();
    private int _nextRegistrationOrder;

    public string BusName => "world";

    public void Publish<T>(in T @event) where T : struct, IEvent
    {
        var eventType = typeof(T);
        if (!typeof(IGlobalEvent).IsAssignableFrom(eventType))
        {
            _observation.RecordRejectedPublish(
                eventType,
                $"WorldEventBus rejected {eventType.FullName}: payload must implement IGlobalEvent or IBroadcastEvent.");
            return;
        }

        DispatchLocal(eventType, in @event);
    }

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

    public void RouteBroadcast<T>(in T @event) where T : struct, IEvent
    {
        if (@event is not IGlobalEvent)
        {
            return;
        }

        DispatchLocal(typeof(T), in @event);
    }

    public void Clear()
    {
        _subscriptions.Clear();
        _dispatchingTypes.Clear();
    }

    private void DispatchLocal<T>(Type eventType, in T @event) where T : struct, IEvent
    {
        _observation.RecordPublish(eventType);

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
                    _observation.RecordHandlerException(eventType, HandlerLabel(subscription.Handler), ex);
                }
            }
        }
        finally
        {
            _dispatchingTypes.Remove(eventType);
        }
    }

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

    private static string HandlerLabel(Delegate handler)
    {
        var target = handler.Target?.GetType().Name ?? "static";
        return $"{target}.{handler.Method.Name}";
    }

    private static string NormalizePath(string path)
    {
        return path.StartsWith("user://", StringComparison.Ordinal)
            ? ProjectSettings.GlobalizePath(path)
            : path;
    }

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
