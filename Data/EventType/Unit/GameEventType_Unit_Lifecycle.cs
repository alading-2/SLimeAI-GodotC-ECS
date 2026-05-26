/// <summary>
/// Unit 生命周期相关事件定义
/// </summary>
public static partial class GameEventType
{
    public static partial class Unit
    {
        /// <summary>单位状态变化</summary>
        public readonly record struct StateChanged(string Key, string OldValue, string NewValue);

        /// <summary>单位开始复活</summary>
        public readonly record struct Reviving(float Duration);

        /// <summary>单位复活完成</summary>
        public readonly record struct Revived();
    }
}
