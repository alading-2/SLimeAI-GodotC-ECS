// ==================================================================================
// 广播路由契约 — EntityEventBus → WorldEventBus 的自动转发接口
// ==================================================================================
//
// IBroadcastEvent payload 在 EntityEventBus 发布后，需要自动路由到 WorldEventBus。
// EntityEventBus 持有 IWorldEventBusRouter 引用，在 Publish 时检测 payload 是否
// 实现了 IGlobalEvent（IBroadcastEvent 继承了 IGlobalEvent），若是则调用 RouteBroadcast。
//
// 路由时机：EntityEventBus.DispatchLocal 成功派发本地 handler 之后。
// 路由条件：payload 同时实现 IEntityEvent 和 IGlobalEvent（即 IBroadcastEvent）。
// ==================================================================================

/// <summary>
/// 接收实体 bus 转发的广播事件。WorldEventBus 实现此接口。
/// </summary>
public interface IWorldEventBusRouter
{
    /// <summary>
    /// 把广播事件路由到 world bus。仅当 payload 实现 IGlobalEvent 时才转发。
    /// 由 EntityEventBus.Publish 在本地派发完成后自动调用，调用方不应直接使用。
    /// </summary>
    void RouteBroadcast<T>(in T @event) where T : struct, IEvent;
}
