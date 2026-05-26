using System;
using System.Collections.Generic;

// ==================================================================================
// EventSubscriptionCollector — 事件订阅 token 批量管理器
// ==================================================================================
//
// 典型用法：
//   1. Entity 持有 _eventSubscriptions 字段
//   2. OnPoolAcquire / _Ready 中通过 Add/Subscribe 收纳 token
//   3. OnPoolRelease / _ExitTree 中通过 Clear() 批量退订
//
// 设计意图：
//   - 避免手动逐个 Dispose Subscribe 返回的 token
//   - 避免遗漏退订导致回调悬挂
//   - 与 EntityEventBus.Clear() 互补：bus.Clear 清空 bus 侧订阅列表，
//     collector.Clear 清空持有方侧 token 引用
//
// Clear 后 collector 可复用（对象池实体 OnPoolRelease → OnPoolAcquire 循环）。
// ==================================================================================

/// <summary>
/// 事件订阅 token 批量管理器。统一收纳 Subscribe 返回的 IDisposable token，
/// 并在 Clear 时批量退订。推荐 Entity 在生命周期结束时调用 Clear()。
/// </summary>
public sealed class EventSubscriptionCollector
{
    /// <summary>已收集的订阅 token 列表</summary>
    private readonly List<IDisposable> _subscriptions = new();

    /// <summary>
    /// 收纳一个已经创建好的订阅 token。用于非 Subscribe 方式获取的 token。
    /// </summary>
    /// <param name="subscription">订阅 token，null 时忽略</param>
    public void Add(IDisposable? subscription)
    {
        if (subscription != null)
        {
            _subscriptions.Add(subscription);
        }
    }

    /// <summary>
    /// 订阅事件并自动收纳返回的 token。等价于 bus.Subscribe(handler) + Add(token)。
    /// </summary>
    /// <param name="bus">目标事件总线</param>
    /// <param name="handler">事件处理回调</param>
    public void Subscribe<T>(IEventBus bus, Action<T> handler) where T : struct, IEvent
    {
        ArgumentNullException.ThrowIfNull(bus);
        ArgumentNullException.ThrowIfNull(handler);
        _subscriptions.Add(bus.Subscribe(handler));
    }

    /// <summary>
    /// 释放当前收纳的全部 token，但保留 collector 以便后续复用。
    /// 逆序 Dispose 以保证后订阅的先退订。Clear 后可再次 Add/Subscribe。
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
