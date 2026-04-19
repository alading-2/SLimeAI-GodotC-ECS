using System;

/// <summary>
/// 贝塞尔模板中的单个相对控制点。
/// <para>
/// <c>ForwardRatio</c> 表示沿起点→终点主轴推进比例；
/// <c>LateralRatio</c> 表示沿主轴垂线偏移比例。
/// </para>
/// </summary>
public readonly struct BezierRelativeControlPoint
{
    public BezierRelativeControlPoint(float forwardRatio, float lateralRatio)
    {
        ForwardRatio = forwardRatio;
        LateralRatio = lateralRatio;
    }

    public float ForwardRatio { get; }

    public float LateralRatio { get; }
}

/// <summary>
/// 贝塞尔曲线模板。
/// <para>
/// 模板不存世界坐标，而存相对描述，供技能生成初始控制点，
/// 或供追踪模式按“当前剩余段”稳定重建曲线。
/// </para>
/// </summary>
public readonly struct BezierCurveTemplate
{
    public BezierCurveTemplate(
        int degree,
        BezierPatternType pattern,
        BezierRelativeControlPoint[] controlPoints,
        int variantIndex = 0)
    {
        Degree = degree;
        Pattern = pattern;
        ControlPoints = controlPoints ?? Array.Empty<BezierRelativeControlPoint>();
        VariantIndex = variantIndex;
    }

    /// <summary>
    /// 贝塞尔阶数。3 = 三阶，4 = 四阶，5 = 五阶。
    /// </summary>
    public int Degree { get; }

    /// <summary>
    /// 模板样式。
    /// </summary>
    public BezierPatternType Pattern { get; }

    /// <summary>
    /// 相对控制点数组，不含起点和终点。
    /// </summary>
    public BezierRelativeControlPoint[] ControlPoints { get; }

    /// <summary>
    /// 多发模式中的当前序号，仅用于调试与复现。
    /// </summary>
    public int VariantIndex { get; }

    /// <summary>
    /// 模板是否有效。
    /// </summary>
    public bool IsValid => Degree >= 3 && Degree <= 5 && ControlPoints.Length == Degree - 1;

    /// <summary>
    /// 完整点数量 = 阶数 + 1。
    /// </summary>
    public int PointCount => Degree + 1;

    /// <summary>
    /// 克隆模板，避免共享数组被外部误修改。
    /// </summary>
    public BezierCurveTemplate Copy()
    {
        return new BezierCurveTemplate
        (
            Degree,
            Pattern,
            (BezierRelativeControlPoint[])ControlPoints.Clone(),
            VariantIndex
        );
    }
}
