/// <summary>
/// Global 游戏状态相关事件定义
/// </summary>
public static partial class GameEventType
{
    public static partial class Global
    {
        /// <summary>游戏开始</summary>
        public readonly record struct GameStart();

        /// <summary>游戏暂停</summary>
        public readonly record struct GamePause();

        /// <summary>游戏恢复</summary>
        public readonly record struct GameResume();

        /// <summary>游戏结束</summary>
        public readonly record struct GameOver(bool IsVictory);
    }
}
