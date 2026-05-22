using System;

/// <summary>
/// ECS 事件总线唯一公开契约。
/// </summary>
public interface IEventBus
{
    /// <summary>总线名称，用于 observation dump。</summary>
    string BusName { get; }

    /// <summary>发布 typed event。事件作用域由 payload marker 决定。</summary>
    void Publish<T>(in T @event) where T : struct, IEvent;

    /// <summary>订阅 typed event。返回 token 是唯一退订入口。</summary>
    IDisposable Subscribe<T>(Action<T> handler) where T : struct, IEvent;

    /// <summary>导出事件总线 observation JSON。</summary>
    void ExportObservation(string path);
}
