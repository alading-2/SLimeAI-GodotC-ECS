/// <summary>
/// 最终朝向输出目标。
/// </summary>
public enum OrientationSink
{
    /// <summary>
    /// 直接驱动实体 root 的 <c>RotationDegrees</c>。
    /// 适用于投射物、特效、机关等需要整体旋转的实体。
    /// </summary>
    RootRotation = 0,

    /// <summary>
    /// 通过 <c>VisualRoot</c> 的 <c>FlipH</c> 表示左右朝向。
    /// 适用于角色类单位，保持现有动画/技能对 <c>FlipH</c> 的依赖语义。
    /// </summary>
    VisualFlipX = 1,
}
