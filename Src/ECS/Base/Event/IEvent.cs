/// <summary>
/// ECS 事件 payload 的基础 marker。所有事件必须实现 IEntityEvent、IGlobalEvent 或 IBroadcastEvent。
/// </summary>
public interface IEvent
{
}

/// <summary>
/// 只允许在实体 bus 发布的事件。
/// </summary>
public interface IEntityEvent : IEvent
{
}

/// <summary>
/// 只允许在 world bus 发布的事件。
/// </summary>
public interface IGlobalEvent : IEvent
{
}

/// <summary>
/// 同时发布到实体 bus 和 world bus 的广播事件。
/// </summary>
public interface IBroadcastEvent : IEntityEvent, IGlobalEvent
{
}
