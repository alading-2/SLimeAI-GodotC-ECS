using Godot;

/// <summary>
/// 条件节点：检查目标是否在指定范围内
/// <para>
/// 通用范围检测，通过构造函数传入 DataKey 来读取不同的范围值。
/// 可用于攻击范围、技能范围等多种场景。
/// </para>
/// </summary>
public class IsInRangeCondition : BehaviorNode
{
    private readonly DataKey<float> _rangeDataKey;

    /// <summary>
    /// 创建范围检测条件节点
    /// </summary>
    /// <param name="rangeDataKey">存储范围值的 DataKey（如 GeneratedDataKey.AttackRange）</param>
    public IsInRangeCondition(DataKey<float> rangeDataKey)
        : base($"在范围内({rangeDataKey.StableKey})")
    {
        _rangeDataKey = rangeDataKey;
    }

    /// <inheritdoc/>
    public override NodeState Evaluate(AIContext ctx)
    {
        var target = ctx.Entity.Data.Get(GeneratedDataKey.TargetNode);
        if (target == null) return NodeState.Failure;

        var selfNode = ctx.Entity as Node2D;
        if (selfNode == null) return NodeState.Failure;

        float range = ctx.Entity.Data.Get(_rangeDataKey);
        float distance = selfNode.GlobalPosition.DistanceTo(target.GlobalPosition);

        return distance <= range ? NodeState.Success : NodeState.Failure;
    }
}
