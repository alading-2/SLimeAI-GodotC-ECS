/// <summary>
/// 移动碰撞目标匹配模式。
/// </summary>
public enum MovementCollisionTargetMatchMode
{
    /// <summary>任意目标都可参与碰撞策略。</summary>
    Any = 0,

    /// <summary>仅当前运动追踪的目标可参与碰撞策略。</summary>
    TrackedTargetOnly = 1,

    /// <summary>仅指定节点可参与碰撞策略。</summary>
    SpecificNode = 2,
}
