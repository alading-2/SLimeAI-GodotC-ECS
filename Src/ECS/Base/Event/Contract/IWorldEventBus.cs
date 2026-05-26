// ==================================================================================
// World 事件总线契约 — 全局事件的发布/订阅/路由/清空
// ==================================================================================
//
// IWorldEventBus 组合了三个能力：
//   1. IEventBus        → 标准 Publish/Subscribe/ExportObservation
//   2. IWorldEventBusRouter → 接收 EntityEventBus 转发的广播事件
//   3. Clear()          → 清空订阅和派发状态（测试/runtime 重启用）
//
// 唯一实例通过 WorldEvents.World 静态属性访问，替代旧 GlobalEventBus.Global。
// ==================================================================================

/// <summary>
/// World 级事件总线契约。组合 IEventBus、IWorldEventBusRouter 和 Clear 能力。
/// 唯一实例入口: <see cref="WorldEvents.World"/>
/// </summary>
public interface IWorldEventBus : IEventBus, IWorldEventBusRouter
{
    /// <summary>
    /// 清空订阅和派发状态。供测试 teardown 或 runtime 重启使用。
    /// 常规业务代码不应调用此方法。
    /// </summary>
    void Clear();
}
