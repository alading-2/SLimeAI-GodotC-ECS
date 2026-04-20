/// <summary>
/// 朝向控制配置来源。
/// </summary>
public enum OrientationSource
{
    /// <summary>
    /// 独立朝向事件触发。
    /// </summary>
    Standalone = 0,

    /// <summary>
    /// 由本次 MovementStarted 一并触发。
    /// </summary>
    Movement = 1,
}
