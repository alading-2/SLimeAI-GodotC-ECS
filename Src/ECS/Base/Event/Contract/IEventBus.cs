using System;

// ==================================================================================
// 事件总线核心契约 — 同步通知层的唯一公开 API
// ==================================================================================
//
// IEventBus 是 EntityEventBus 和 WorldEventBus 的共同契约。
// 只提供三个能力：Publish、Subscribe、ExportObservation。
//
// 设计边界（EventBus 不是什么）：
//   - 不是请求响应 bus → 请求响应走 owner service / pipeline
//   - 不是优先级 pipeline → handler 按注册顺序执行，无 priority
//   - 不是 command bus  → 结构变更走 RuntimeCommandBuffer / lifecycle API
//   - 不是状态存储      → 状态由 Data / Entity 管理
//   - 不是 Godot Signal 替代 → UI 内部交互仍可用 Signal，但核心业务必须走 typed event
//
// 退订方式：Subscribe 返回 IDisposable token，持有方在生命周期结束时 Dispose()。
// 禁止使用 handler 身份退订（旧 Off 模式已删除）。
// ==================================================================================

/// <summary>
/// 事件总线唯一公开契约。EntityEventBus 和 WorldEventBus 均实现此接口。
/// </summary>
public interface IEventBus
{
    /// <summary>
    /// 总线名称，用于 observation dump 和调试输出。
    /// EntityEventBus 格式: "entity:{InstanceId}:{Name}"
    /// WorldEventBus 固定为: "world"
    /// </summary>
    string BusName { get; }

    /// <summary>
    /// 发布 typed event。事件作用域由 payload marker 决定：
    ///   - IEntityEvent    → EntityEventBus 接受，WorldEventBus 拒绝
    ///   - IGlobalEvent    → WorldEventBus 接受，EntityEventBus 拒绝
    ///   - IBroadcastEvent → EntityEventBus 接受并自动路由到 WorldEventBus
    /// </summary>
    void Publish<T>(in T @event) where T : struct, IEvent;

    /// <summary>
    /// 订阅 typed event。返回的 IDisposable token 是唯一退订入口。
    /// handler 按注册顺序执行，无 priority。同一 token Dispose 后不再收到事件。
    /// 推荐使用 EventSubscriptionCollector 统一收纳 token。
    /// </summary>
    IDisposable Subscribe<T>(Action<T> handler) where T : struct, IEvent;

    /// <summary>
    /// 导出事件总线 observation JSON 到指定路径。
    /// 包含：订阅列表、发布计数、重入阻断计数、handler 异常、注册顺序。
    /// 路径支持 "user://" 前缀，会自动转换为绝对路径。
    /// </summary>
    void ExportObservation(string path);
}
