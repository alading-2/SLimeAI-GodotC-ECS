// ==================================================================================
// WorldEvents — World 事件总线静态入口
// ==================================================================================
//
// 替代旧 GlobalEventBus.Global。所有需要访问 WorldEventBus 的代码
// 统一通过 WorldEvents.World 获取。
//
// Entity 构造 EntityEventBus 时传入 WorldEvents.World 作为广播路由器：
//   new EntityEventBus("entity", WorldEvents.World)
//
// 订阅全局事件：
//   WorldEvents.World.Subscribe<GlobalEvents.GameStart>(OnGameStart)
//
// 发布全局事件：
//   WorldEvents.World.Publish(new GlobalEvents.GameStart())
// ==================================================================================

/// <summary>
/// World 事件总线静态入口。替换旧 GlobalEventBus.Global。
/// 唯一 WorldEventBus 实例在此懒初始化。
/// </summary>
public static class WorldEvents
{
    /// <summary>
    /// 全局唯一 WorldEventBus 实例。用于发布/订阅 IGlobalEvent 和接收 IBroadcastEvent 路由。
    /// </summary>
    public static IWorldEventBus World { get; } = new WorldEventBus();
}
