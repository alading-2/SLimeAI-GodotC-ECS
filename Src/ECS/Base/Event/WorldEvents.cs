/// <summary>
/// World 事件总线静态入口。替换旧 GlobalEventBus.Global。
/// </summary>
public static class WorldEvents
{
    public static IWorldEventBus World { get; } = new WorldEventBus();
}
