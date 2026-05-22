using System;
using System.Collections.Generic;

/// <summary>
/// 统一收纳事件订阅 token，便于在生命周期结束时一次性释放。
/// </summary>
public sealed class EventSubscriptionCollector
{
    private readonly List<IDisposable> _subscriptions = new();

    /// <summary>
    /// 收纳一个已经创建好的订阅 token。
    /// </summary>
    public void Add(IDisposable? subscription)
    {
        if (subscription != null)
        {
            _subscriptions.Add(subscription);
        }
    }

    /// <summary>
    /// 订阅事件并自动收纳返回的 token。
    /// </summary>
    public void Subscribe<T>(IEventBus bus, Action<T> handler) where T : struct, IEvent
    {
        ArgumentNullException.ThrowIfNull(bus);
        ArgumentNullException.ThrowIfNull(handler);
        _subscriptions.Add(bus.Subscribe(handler));
    }

    /// <summary>
    /// 释放当前收纳的全部 token，但保留 collector 以便后续复用。
    /// </summary>
    public void Clear()
    {
        for (var i = _subscriptions.Count - 1; i >= 0; i--)
        {
            _subscriptions[i].Dispose();
        }

        _subscriptions.Clear();
    }
}
