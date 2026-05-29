/// <summary>
/// AI 运行时状态枚举。
/// </summary>
public enum AIState
{
    /// <summary>待机。</summary>
    Idle = 0,

    /// <summary>追逐。</summary>
    Chasing = 1,

    /// <summary>攻击。</summary>
    Attacking = 2,

    /// <summary>巡逻。</summary>
    Patrolling = 3,

    /// <summary>逃跑。</summary>
    Fleeing = 4
}
