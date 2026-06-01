/// <summary>
/// TestSystem 局部事件定义。
/// </summary>
public static partial class GameEventType
{
    public static class TestSystem
    {
        /// <summary>TestSystem 当前选中实体变化</summary>
        public readonly record struct SelectionChanged(IEntity? Entity);
    }
}
