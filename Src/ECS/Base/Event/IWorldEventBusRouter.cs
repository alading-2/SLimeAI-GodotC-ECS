/// <summary>
/// 接收实体 bus 转发的广播事件。
/// </summary>
public interface IWorldEventBusRouter
{
    /// <summary>把广播事件路由到 world bus。</summary>
    void RouteBroadcast<T>(in T @event) where T : struct, IEvent;
}
