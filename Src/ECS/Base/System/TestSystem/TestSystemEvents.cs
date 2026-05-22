/// <summary>
/// TestSystem 自身的实体级状态事件。
/// </summary>
public static class TestSystemEvents
{
    public readonly record struct SelectionChanged(IEntity? Entity) : IEntityEvent;
}
