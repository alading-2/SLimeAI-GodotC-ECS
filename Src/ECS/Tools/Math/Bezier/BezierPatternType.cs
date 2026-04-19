/// <summary>
/// 贝塞尔模板样式。
/// </summary>
public enum BezierPatternType
{
    /// <summary>
    /// 先向目标背后绕，再回打目标。
    /// </summary>
    RearWrap = 0,

    /// <summary>
    /// 单侧切入，再压回目标。
    /// </summary>
    SideSweep = 1,

    /// <summary>
    /// 左右换边，形成单次 S 形。
    /// </summary>
    SWeave = 2,

    /// <summary>
    /// 先散开，再向目标收束。
    /// </summary>
    Converge = 3
}
