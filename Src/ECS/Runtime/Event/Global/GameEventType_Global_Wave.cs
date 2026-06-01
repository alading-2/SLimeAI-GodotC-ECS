/// <summary>
/// Global 波次相关事件定义
/// </summary>
public static partial class GameEventType
{
    public static partial class Global
    {
        /// <summary>波次开始</summary>
        public readonly record struct WaveStarted(int WaveIndex);

        /// <summary>波次完成</summary>
        public readonly record struct WaveCompleted(int WaveIndex);
    }
}
