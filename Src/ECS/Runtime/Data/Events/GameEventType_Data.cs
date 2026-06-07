/// <summary>
/// Data 相关事件定义
/// </summary>
public static partial class GameEventType
{
    public static class Data
    {
        /// <summary>typed Data 字段变更，业务代码优先订阅此事件。</summary>
        public readonly record struct Changed<T>(DataKey<T> Key, T OldValue, T NewValue);
        /// <summary>属性变更。仅保留给 diagnostic/TestSystem 兼容，业务代码不要新增 object payload 监听。</summary>
        public readonly record struct PropertyChanged(string Key, object? OldValue, object? NewValue);
        /// <summary>数据重置</summary>
        public readonly record struct Reset();
        /// <summary>生命值变更</summary>
        public readonly record struct HealthChanged(float OldHp, float NewHp);
    }
}
