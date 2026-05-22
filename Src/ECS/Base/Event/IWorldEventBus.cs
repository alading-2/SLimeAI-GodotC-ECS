/// <summary>
/// World 级事件总线契约。
/// </summary>
public interface IWorldEventBus : IEventBus, IWorldEventBusRouter
{
    /// <summary>清空订阅和派发状态，供测试或 runtime 重启使用。</summary>
    void Clear();
}
