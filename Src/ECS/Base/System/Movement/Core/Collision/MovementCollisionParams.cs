using System;
using Godot;

/// <summary>
/// 单次移动的碰撞策略配置。
/// <para>
/// 该对象只描述“哪些碰撞算有效、是否通知、是否停止、停止后是否销毁”，
/// 不负责存储运行时计数状态。
/// </para>
/// </summary>
public readonly record struct MovementCollisionParams
{
    /// <summary>
    /// 带默认值的 struct 需要显式无参构造函数。
    /// </summary>
    public MovementCollisionParams()
    {
    }

    /// <summary>阵营过滤；`All` 表示不限制。</summary>
    public TeamFilter TeamFilter { get; init; } = TeamFilter.All;

    /// <summary>实体类型过滤；`None` 表示不限制。</summary>
    public EntityType EntityTypeFilter { get; init; } = EntityType.None;

    /// <summary>目标匹配模式。</summary>
    public MovementCollisionTargetMatchMode TargetMatchMode { get; init; } = MovementCollisionTargetMatchMode.Any;

    /// <summary>当 <see cref="TargetMatchMode"/> 为 <c>SpecificNode</c> 指定节点时使用的目标节点。</summary>
    public Node2D? SpecificTargetNode { get; init; } = null;

    /// <summary>
    /// 有效碰撞累计到多少次后自动停止。
    /// <para>`-1` = 永不因碰撞自动停止。</para>
    /// </summary>
    public int StopAfterCollisionCount { get; init; } = -1;

    /// <summary>
    /// 因碰撞触发停止后是否销毁实体。
    /// <para>
    /// 只有当 <see cref="StopAfterCollisionCount"/> 达到阈值并触发“碰撞停止”（WillStop=true）时才会生效；
    /// 若 <see cref="StopAfterCollisionCount"/> = -1（只通知不停止），该值不会被检查。
    /// </para>
    /// </summary>
    public bool DestroyOnStop { get; init; } = false;

    /// <summary>是否对有效碰撞发出 <c>MovementCollision</c> 事件。</summary>
    public bool EmitCollisionEvent { get; init; } = true;

    /// <summary>有效碰撞时的本地回调。</summary>
    public Action<MovementCollisionContext>? OnCollision { get; init; } = null;
}
