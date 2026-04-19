/// <summary>
/// 父实体销毁时，直接归属子实体的处理策略。
/// <para>该策略只挂在 PARENT 关系上，用来描述“归属链”的生命周期语义。</para>
/// </summary>
public enum ParentDestroyPolicy
{
    /// <summary>
    /// 父实体销毁时，递归销毁子实体。
    /// <para>适用于投射物、附着特效、技能实体等没有独立生存意义的派生实体。</para>
    /// </summary>
    DestroyRecursively = 0,

    /// <summary>
    /// 父实体销毁时，仅断开归属关系，子实体继续存活。
    /// <para>适用于掉落物、脱手后继续存在的世界实体等场景。</para>
    /// </summary>
    Detach = 1
}
