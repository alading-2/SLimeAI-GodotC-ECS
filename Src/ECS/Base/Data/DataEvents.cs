/// <summary>
/// Runtime Data 的实体级变更事件。
/// </summary>
public static class DataEvents
{
    public readonly record struct PropertyChanged(string Key, object? OldValue, object? NewValue) : IEntityEvent;

    public readonly record struct Reset() : IEntityEvent;

    public readonly record struct HealthChanged(float OldHp, float NewHp) : IEntityEvent;
}
