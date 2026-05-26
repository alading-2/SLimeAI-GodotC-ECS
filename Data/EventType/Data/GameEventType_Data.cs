/// <summary>
/// Data 相关事件定义
/// </summary>
public static partial class GameEventType
{
    public static class Data
    {
        /// <summary>属性变更</summary>
        public readonly record struct PropertyChanged(string Key, object? OldValue, object? NewValue);
        /// <summary>数据重置</summary>
        public readonly record struct Reset();
        /// <summary>生命值变更</summary>
        public readonly record struct HealthChanged(float OldHp, float NewHp);
    }
}
