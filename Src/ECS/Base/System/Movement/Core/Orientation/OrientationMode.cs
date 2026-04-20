/// <summary>
/// 最终朝向控制模式。
/// </summary>
public enum OrientationMode
{
    /// <summary>
    /// 仅跟随移动系统解析出的朝向。
    /// </summary>
    FollowMovement = 0,

    /// <summary>
    /// 忽略移动朝向，仅执行固定方向自转。
    /// </summary>
    SpinOnly = 1,

    /// <summary>
    /// 在移动朝向基础上叠加持续自转。
    /// </summary>
    FollowMovementAndSpin = 2,
}
