// ==================================================================================
// 事件系统 Marker 接口 — 类型驱动的 scope 路由
// ==================================================================================
//
// 事件 payload 通过 marker interface 声明自身作用域，编译期即可确定：
//   - IEntityEvent    → 只能在 EntityEventBus 发布（实体局部通信）
//   - IGlobalEvent    → 只能在 WorldEventBus 发布（全局通知）
//   - IBroadcastEvent → 在 EntityEventBus 发布一次，自动路由到 WorldEventBus
//
// 设计原则：
//   1. 事件主键 = payload 类型本身（T），不再使用 string event name
//   2. scope 由类型决定，不由调用方决定 → 防止误发到错误 bus
//   3. 所有事件 payload 必须是 readonly record struct
//   4. 框架事件 payload 不得包含 Godot engine type
//
// 事件 payload 归属规则：
//   - 框架级全局事件 → Event/Payload/GlobalEvents.cs
//   - 模块专属事件   → 跟随 owner 模块目录（如 AbilityEvents.cs 在 Ability/ 下）
//   - 游戏专属事件   → 游戏侧事件目录，不放框架核心
// ==================================================================================

/// <summary>
/// 事件 payload 基础 marker。所有事件必须实现 IEntityEvent、IGlobalEvent 或 IBroadcastEvent 之一。
/// 不允许直接实现 IEvent 而不实现任一子 marker。
/// </summary>
public interface IEvent
{
}

/// <summary>
/// 实体局部事件 marker。只能在 EntityEventBus 发布和订阅。
/// 适用场景：组件间通信、实体内部状态变化通知（如动画完成、移动停止）。
/// WorldEventBus 会拒绝发布此类型事件。
/// </summary>
public interface IEntityEvent : IEvent
{
}

/// <summary>
/// 全局事件 marker。只能在 WorldEventBus 发布和订阅。
/// 适用场景：跨实体通知（如实体生成/销毁、游戏状态变更、波次开始/结束）。
/// EntityEventBus 会拒绝发布此类型事件。
/// </summary>
public interface IGlobalEvent : IEvent
{
}

/// <summary>
/// 广播事件 marker。同时实现 IEntityEvent 和 IGlobalEvent。
/// 发布时只需在 EntityEventBus.Publish 一次，bus 自动路由到 WorldEventBus。
/// 适用场景：需要实体局部和全局同时感知的事件（如受伤、治疗、击杀）。
/// 禁止调用方手动双发 broadcast payload。
/// </summary>
public interface IBroadcastEvent : IEntityEvent, IGlobalEvent
{
}
