using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// TargetSelector 查询参数载体。
/// 统一描述几何范围、过滤、排序、随机源和数量限制，供 TargetQueryEngine 执行查询。
/// </summary>
public readonly record struct TargetSelectorQuery
{
    /// <summary>
    /// 初始化 TargetSelectorQuery，设置默认不限目标数。
    /// </summary>
    public TargetSelectorQuery()
    {
    }

    // ==================== 几何参数 ====================

    /// <summary>
    /// 几何形状类型（必填）。
    /// 常见值：Circle / Ring / Box / Line / Cone / Chain / Global / Single。
    /// </summary>
    public required GeometryType Geometry { get; init; }

    /// <summary>
    /// 查询原点（必填）。
    /// 所有几何计算都基于该点。
    /// </summary>
    public required Vector2 Origin { get; init; }

    /// <summary>
    /// 查询原点提供器（可选）。
    /// 非 null 时优先使用，用于光环/持续范围等需要实时跟随的场景。
    /// </summary>
    public Func<Vector2>? OriginProvider { get; init; }

    /// <summary>
    /// 方向向量（可选）。
    /// Box / Line / Cone 需要朝向，不传时通常默认使用 Vector2.Right。
    /// </summary>
    public Vector2? Forward { get; init; }

    /// <summary>
    /// 查询方向提供器（可选）。
    /// 非 null 时优先使用，用于跟随施法者朝向的持续查询。
    /// </summary>
    public Func<Vector2>? ForwardProvider { get; init; }

    /// <summary>
    /// 半径或最大距离（可选）。
    /// Circle / Ring / Cone 等几何会使用该值。
    /// </summary>
    public float Range { get; init; }

    /// <summary>
    /// 圆环内半径（可选，默认 0）。
    /// 仅 Ring 使用。
    /// </summary>
    public float InnerRange { get; init; }

    /// <summary>
    /// 宽度（可选）。
    /// Box / Line 使用。
    /// </summary>
    public float Width { get; init; }

    /// <summary>
    /// 长度（可选）。
    /// Box / Line 使用。
    /// </summary>
    public float Length { get; init; }

    /// <summary>
    /// 扇形角度（可选，单位：度）。
    /// 仅 Cone 使用。
    /// </summary>
    public float Angle { get; init; }

// ==================== 过滤参数 ====================

    /// <summary>
    /// 阵营过滤器（可选）。
    /// 支持 Friendly / Enemy / Neutral / Self 组合。
    /// </summary>
    public TeamFilter TeamFilter { get; init; }

    /// <summary>
    /// 类型过滤器（可选）。
    /// 通过 EntityType 位掩码筛选目标类型。
    /// </summary>
    public EntityType TypeFilter { get; init; }

    /// <summary>
    /// 阵营过滤参照实体（可选）。
    /// 判断 Self / Friendly / Enemy 时需要。
    /// </summary>
    public IEntity? CenterEntity { get; init; }

// ==================== 排序与限制 ====================

    /// <summary>
    /// 排序规则（可选）。
    /// 如 Nearest / Farthest / LowestHealth / Random。
    /// </summary>
    public TargetSorting Sorting { get; init; }

    /// <summary>
    /// 最大目标数量（可选）。
    /// -1 表示不限。
    /// </summary>
    public int MaxTargets { get; init; } = -1;

    /// <summary>
    /// 显式候选来源。设置后不扫描默认 EntityManager source。
    /// </summary>
    public IReadOnlyList<IEntity>? ExplicitCandidates { get; init; }

    /// <summary>
    /// 显式单体目标，用于 Single 查询。
    /// </summary>
    public IEntity? ExplicitTarget { get; init; }

    /// <summary>
    /// 可复现随机 seed。Random sorting 和位置采样必须优先使用该 seed。
    /// </summary>
    public int? RandomSeed { get; init; }

    /// <summary>
    /// 可注入随机源。优先级高于 RandomSeed；调用方负责其生命周期。
    /// </summary>
    public Random? RandomSource { get; init; }

    /// <summary>
    /// 解析本次查询应使用的原点。
    /// </summary>
    public Vector2 ResolveOrigin() => OriginProvider?.Invoke() ?? Origin;

    /// <summary>
    /// 解析本次查询应使用的方向。
    /// </summary>
    public Vector2 ResolveForward()
    {
        var forward = ForwardProvider?.Invoke() ?? Forward ?? Vector2.Right;
        return forward.LengthSquared() > 0f ? forward.Normalized() : Vector2.Right;
    }

    /// <summary>
    /// 使用已解析 origin/forward 生成快照 query，避免后续阶段重复读取 provider。
    /// </summary>
    public TargetSelectorQuery Resolve()
    {
        return this with
        {
            Origin = ResolveOrigin(),
            OriginProvider = null,
            Forward = ResolveForward(),
            ForwardProvider = null
        };
    }
}
