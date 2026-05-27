using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// 事件优先级
/// 数值越大越优先执行
/// </summary>
public enum EventPriority
{
    Monitor = -100,  // 最后执行，用于监控或后期处理
    Lowest = -50,
    Low = -10,
    Normal = 0,
    High = 10,
    Highest = 50,
    Critical = 100   // 最先执行，用于关键逻辑
}

/// <summary>
/// 通用游戏事件总线
/// 以 payload 类型（readonly record struct）作为事件标识，替代旧字符串主键。
/// 支持泛型数据传递、优先级排序、一次性订阅。
/// <para>typeof(T) 在泛型方法中是 JIT 常量，无反射开销。</para>
/// </summary>
public class EventBus
{
    private static readonly Log _log = new Log("EventBus");

    /// <summary>
    /// 事件订阅封装
    /// </summary>
    private class Subscription
    {
        /// <summary>
        /// 事件类型（payload 的 Type）
        /// </summary>
        public Type EventType { get; set; }

        /// <summary>
        /// 事件处理器委托
        /// </summary>
        public Delegate Handler { get; set; }

        /// <summary>
        /// 优先级，数值越大越优先
        /// </summary>
        public int Priority { get; set; }

        /// <summary>
        /// 是否一次性订阅
        /// </summary>
        public bool Once { get; set; }

        // 标记是否已被移除（处理迭代期间的安全删除）
        public bool IsPendingRemoval { get; set; } = false;

        public Subscription(Type eventType, Delegate handler, int priority, bool once)
        {
            EventType = eventType;
            Handler = handler;
            Priority = priority;
            Once = once;
        }
    }

    // 事件存储字典：payload Type -> 订阅列表
    private readonly Dictionary<Type, List<Subscription>> _subscriptions = new();

    // 防止在 Emit 过程中修改列表导致的集合修改异常
    private int _emittingCount = 0;
    private readonly List<Subscription> _pendingRemovals = new();

    // 重入保护：追踪正在执行的事件类型，防止同类型事件递归触发
    private readonly HashSet<Type> _emittingEvents = new();

    // ==================== 订阅 (Subscribe) ====================

    /// <summary>
    /// 订阅带参数的事件，payload 类型 T 即事件标识
    /// </summary>
    public void On<T>(Action<T> handler, int priority = (int)EventPriority.Normal) where T : struct
    {
        Subscribe<T>(handler, priority, false);
    }

    /// <summary>
    /// 订阅一次性事件（带参数）
    /// </summary>
    public void Once<T>(Action<T> handler, int priority = (int)EventPriority.Normal) where T : struct
    {
        Subscribe<T>(handler, priority, true);
    }

    private void Subscribe<T>(Delegate handler, int priority, bool once) where T : struct
    {
        if (handler == null)
        {
            _log.Error($"尝试订阅空处理器: {typeof(T).Name}");
            return;
        }

        var key = typeof(T);
        if (!_subscriptions.TryGetValue(key, out var list))
        {
            list = new List<Subscription>();
            _subscriptions[key] = list;
        }

        var sub = new Subscription(key, handler, priority, once);
        list.Add(sub);
        list.Sort((a, b) => b.Priority.CompareTo(a.Priority));
    }

    /// <summary>
    /// 动态订阅事件（用于运行时通过反射/配置订阅的场景）
    /// </summary>
    public void OnDynamic(Type eventType, Action<object> handler, int priority = (int)EventPriority.Normal)
    {
        if (handler == null) return;
        if (!_subscriptions.TryGetValue(eventType, out var list))
        {
            list = new List<Subscription>();
            _subscriptions[eventType] = list;
        }
        var sub = new Subscription(eventType, handler, priority, false);
        list.Add(sub);
        list.Sort((a, b) => b.Priority.CompareTo(a.Priority));
    }

    // ==================== 取消订阅 (Unsubscribe) ====================

    /// <summary>
    /// 取消订阅带参数的事件
    /// </summary>
    public void Off<T>(Action<T> handler) where T : struct
    {
        Unsubscribe<T>(handler);
    }

    private void Unsubscribe<T>(Delegate handler) where T : struct
    {
        var key = typeof(T);
        if (!_subscriptions.TryGetValue(key, out var list)) return;

        var target = list.FirstOrDefault(s => s.Handler == handler);
        if (target != null)
        {
            RemoveSubscription(target, list);
        }
    }

    /// <summary>
    /// 动态取消订阅事件
    /// </summary>
    public void OffDynamic(Type eventType, Action<object> handler)
    {
        if (!_subscriptions.TryGetValue(eventType, out var list)) return;
        var target = list.FirstOrDefault(s => s.Handler == handler);
        if (target != null)
        {
            RemoveSubscription(target, list);
        }
    }

    private void RemoveSubscription(Subscription sub, List<Subscription> list)
    {
        if (_emittingCount > 0)
        {
            sub.IsPendingRemoval = true;
            _pendingRemovals.Add(sub);
        }
        else
        {
            list.Remove(sub);
            if (list.Count == 0)
            {
                _subscriptions.Remove(sub.EventType);
            }
        }
    }

    // ==================== 触发 (Emit) ====================

    /// <summary>
    /// 触发事件，payload 类型 T 即事件标识
    /// </summary>
    public void Emit<T>(in T data) where T : struct
    {
        Trigger(in data);
    }

    /// <summary>
    /// 动态触发事件（用于 Feature 系统等运行时场景，通过反射调用泛型 Trigger）。
    /// 性能低于泛型 Emit，仅用于数据驱动场景。
    /// </summary>
    public void EmitDynamic(object eventData)
    {
        if (eventData == null) return;
        var eventType = eventData.GetType();
        var triggerMethod = typeof(EventBus).GetMethod(
            nameof(TriggerDynamicInner),
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (triggerMethod == null) return;
        var generic = triggerMethod.MakeGenericMethod(eventType);
        generic.Invoke(this, new[] { eventData });
    }

    private void TriggerDynamicInner<T>(object data) where T : struct
    {
        Trigger((T)data);
    }

    private void Trigger<T>(in T data) where T : struct
    {
        var key = typeof(T);

        // 重入检测：阻止同类型事件递归触发
        if (_emittingEvents.Contains(key))
        {
            _log.Warn($"检测到事件重入，已阻止: [{key.Name}]");
            return;
        }

        if (!_subscriptions.TryGetValue(key, out var list) || list.Count == 0)
        {
            return;
        }

        _emittingEvents.Add(key);
        _emittingCount++;
        try
        {
            int count = list.Count;
            for (int i = 0; i < count; i++)
            {
                var sub = list[i];
                if (sub.IsPendingRemoval) continue;

                try
                {
                    // 带参处理器：直接强转 Action<T>，避免反射
                    if (sub.Handler is Action<T> typedHandler)
                    {
                        typedHandler(data);
                    }
                    else if (sub.Handler is Action<object> objectHandler)
                    {
                        objectHandler(data);
                    }
                    else
                    {
                        _log.Warn($"事件 [{key.Name}] 类型不匹配: " +
                                 $"订阅者需要 {sub.Handler.GetType()}, " +
                                 $"但事件数据是 Action<{typeof(T)}>");
                    }

                    if (sub.Once)
                    {
                        RemoveSubscription(sub, list);
                    }
                }
                catch (Exception ex)
                {
                    _log.Error($"事件处理异常 [{key.Name}]: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }
        finally
        {
            _emittingCount--;
            _emittingEvents.Remove(key);
            if (_emittingCount <= 0)
            {
                ProcessPendingRemovals();
            }
        }
    }

    /// <summary>
    /// 处理延迟移除的订阅
    /// </summary>
    private void ProcessPendingRemovals()
    {
        if (_pendingRemovals.Count == 0) return;

        foreach (var sub in _pendingRemovals)
        {
            if (_subscriptions.TryGetValue(sub.EventType, out var list))
            {
                list.Remove(sub);
                if (list.Count == 0)
                {
                    _subscriptions.Remove(sub.EventType);
                }
            }
        }
        _pendingRemovals.Clear();
    }

    // ==================== 管理 ====================

    /// <summary>
    /// 清除指定事件类型的所有订阅
    /// </summary>
    public void ClearEvent<T>() where T : struct
    {
        _subscriptions.Remove(typeof(T));
    }

    /// <summary>
    /// 清空所有订阅
    /// </summary>
    public void Clear()
    {
        _subscriptions.Clear();
        _pendingRemovals.Clear();
        _emittingCount = 0;
        _log.Debug("EventBus 已清空");
    }
}
