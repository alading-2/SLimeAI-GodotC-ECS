using Godot;
using System;

/// <summary>
/// 贝塞尔模板生成器。
/// <para>
/// 负责两类事情：
/// 1. 根据“模式 + 阶数 + 多发序号”生成 3~5 阶模板；
/// 2. 把模板解析为世界坐标控制点，供技能和追踪策略复用。
/// </para>
/// </summary>
internal static class BezierTemplateBuilder
{
    private const float ForwardMinGap = 0.06f;
    private const float MaxLateralBasisDistance = 320f;

    /// <summary>
    /// 创建预设模板。
    /// </summary>
    public static BezierCurveTemplate CreatePattern(
        int degree,
        BezierPatternType pattern,
        int variantIndex = 0,
        int variantCount = 1,
        int randomSeed = 0,
        float randomStrength = 0.12f)
    {
        int clampedDegree = Mathf.Clamp(degree, 3, 5); // 当前只维护 3~5 阶模板
        var controlPoints = CreateBasePattern(clampedDegree, pattern);

        float lane = ResolveLane(variantIndex, variantCount); // 多发时的左右分布
        float sideSign = ResolveSideSign(variantIndex, lane); // 左右手性
        float laneScale = variantCount <= 1 ? 1f : 0.72f + 0.28f * Mathf.Abs(lane); // 越靠边越张开

        ApplyVariantOffsets(
            controlPoints,
            lane,
            sideSign,
            laneScale,
            randomSeed,
            randomStrength,
            pattern,
            variantIndex
        );
        NormalizeForwardRatios(controlPoints);

        return new BezierCurveTemplate
        (
            clampedDegree,
            pattern,
            controlPoints,
            variantIndex
        );
    }

    /// <summary>
    /// 从现有世界坐标控制点反推出相对模板。
    /// <para>
    /// 主要用于兼容旧的 <c>BezierPoints</c> 输入，让追踪模式也能转成模板语义。
    /// </para>
    /// </summary>
    public static bool TryCreateTemplateFromPoints(
        ReadOnlySpan<Vector2> points,
        Vector2 startPos,
        Vector2 endPos,
        out BezierCurveTemplate template)
    {
        int degree = points.Length - 1;
        if (degree < 3 || degree > 5)
        {
            template = default;
            return false;
        }

        float distance = startPos.DistanceTo(endPos);
        if (distance <= 0.001f)
        {
            template = default;
            return false;
        }

        Vector2 forward = (endPos - startPos) / distance;
        Vector2 side = new Vector2(-forward.Y, forward.X);
        var controlPoints = new BezierRelativeControlPoint[degree - 1];

        for (int i = 0; i < controlPoints.Length; i++)
        {
            Vector2 offset = points[i + 1] - startPos;
            float forwardRatio = offset.Dot(forward) / distance;
            float lateralRatio = offset.Dot(side) / distance;
            controlPoints[i] = new BezierRelativeControlPoint(forwardRatio, lateralRatio);
        }

        template = new BezierCurveTemplate(
            degree,
            BezierPatternType.RearWrap, // 兼容模板不强依赖样式枚举，此值仅作调试标签
            controlPoints,
            0);
        return true;
    }

    /// <summary>
    /// 直接创建世界坐标控制点，供技能发射阶段使用。
    /// </summary>
    public static Vector2[] CreatePatternPoints(
        Vector2 startPos,
        Vector2 endPos,
        int degree,
        BezierPatternType pattern,
        int variantIndex = 0,
        int variantCount = 1,
        int randomSeed = 0,
        float randomStrength = 0.12f)
    {
        var template = CreatePattern(degree, pattern, variantIndex, variantCount, randomSeed, randomStrength);
        var points = new Vector2[template.PointCount];
        ResolvePoints(template, startPos, endPos, points);
        return points;
    }

    /// <summary>
    /// 将模板解析为世界坐标控制点。
    /// </summary>
    public static int ResolvePoints(
        in BezierCurveTemplate template,
        Vector2 startPos,
        Vector2 endPos,
        Span<Vector2> destination)
    {
        return ResolvePoints(template, startPos, endPos, destination, 1f);
    }

    /// <summary>
    /// 将模板解析为世界坐标控制点。
    /// <para>
    /// <paramref name="shapeWeight"/> 用于收束模板幅度：
    /// - 1 表示完整保留模板形态；
    /// - 0 表示退化为朝终点收束的直线控制点。
    /// 追踪剩余段会传入递减权重，避免每帧都重新做一次完整绕行。
    /// </para>
    /// </summary>
    public static int ResolvePoints(
        in BezierCurveTemplate template,
        Vector2 startPos,
        Vector2 endPos,
        Span<Vector2> destination,
        float shapeWeight)
    {
        if (!template.IsValid || destination.Length < template.PointCount)
        {
            return 0;
        }

        destination[0] = startPos;
        destination[template.PointCount - 1] = endPos;

        Vector2 chord = endPos - startPos;
        float distance = chord.Length();
        Vector2 forward = distance > 0.001f ? chord / distance : Vector2.Right;
        Vector2 side = new Vector2(-forward.Y, forward.X);
        float clampedShapeWeight = Mathf.Clamp(shapeWeight, 0f, 1f);
        float forwardBasisDistance = Mathf.Max(distance, 1f); // 目标贴脸时仍保持稳定基底，避免除零
        float lateralBasisDistance = Mathf.Min(forwardBasisDistance, MaxLateralBasisDistance); // 长距离不再无限放大侧偏

        for (int i = 0; i < template.ControlPoints.Length; i++)
        {
            var point = template.ControlPoints[i];
            float forwardRatio = point.ForwardRatio;
            if (forwardRatio < 0f)
            {
                forwardRatio *= clampedShapeWeight; // 绕后段随剩余路程收束，避免追踪时反复大回拉
            }

            float lateralRatio = point.LateralRatio * clampedShapeWeight; // 侧偏同样按剩余段收束，避免飞出屏幕
            destination[i + 1] = startPos
                + forward * (forwardBasisDistance * forwardRatio)
                + side * (lateralBasisDistance * lateralRatio);
        }

        return template.PointCount;
    }

    private static BezierRelativeControlPoint[] CreateBasePattern(int degree, BezierPatternType pattern)
    {
        return pattern switch
        {
            BezierPatternType.RearWrap => CreateRearWrap(degree),
            BezierPatternType.SideSweep => CreateSideSweep(degree),
            BezierPatternType.SWeave => CreateSWeave(degree),
            BezierPatternType.Converge => CreateConverge(degree),
            _ => CreateRearWrap(degree)
        };
    }

    private static BezierRelativeControlPoint[] CreateRearWrap(int degree)
    {
        return degree switch
        {
            3 => new[]
            {
                new BezierRelativeControlPoint(-0.18f, 0.60f),
                new BezierRelativeControlPoint(0.36f, 0.24f)
            },
            4 => new[]
            {
                new BezierRelativeControlPoint(-0.22f, 0.72f),
                new BezierRelativeControlPoint(-0.02f, 0.52f),
                new BezierRelativeControlPoint(0.46f, 0.18f)
            },
            _ => new[]
            {
                new BezierRelativeControlPoint(-0.24f, 0.80f),
                new BezierRelativeControlPoint(-0.08f, 0.64f),
                new BezierRelativeControlPoint(0.22f, 0.42f),
                new BezierRelativeControlPoint(0.58f, 0.16f)
            }
        };
    }

    private static BezierRelativeControlPoint[] CreateSideSweep(int degree)
    {
        return degree switch
        {
            3 => new[]
            {
                new BezierRelativeControlPoint(0.12f, 0.48f),
                new BezierRelativeControlPoint(0.56f, 0.26f)
            },
            4 => new[]
            {
                new BezierRelativeControlPoint(0.10f, 0.42f),
                new BezierRelativeControlPoint(0.34f, 0.56f),
                new BezierRelativeControlPoint(0.72f, 0.18f)
            },
            _ => new[]
            {
                new BezierRelativeControlPoint(0.08f, 0.34f),
                new BezierRelativeControlPoint(0.24f, 0.52f),
                new BezierRelativeControlPoint(0.50f, 0.48f),
                new BezierRelativeControlPoint(0.76f, 0.14f)
            }
        };
    }

    private static BezierRelativeControlPoint[] CreateSWeave(int degree)
    {
        return degree switch
        {
            3 => new[]
            {
                new BezierRelativeControlPoint(0.18f, 0.44f),
                new BezierRelativeControlPoint(0.62f, -0.32f)
            },
            4 => new[]
            {
                new BezierRelativeControlPoint(0.12f, 0.40f),
                new BezierRelativeControlPoint(0.34f, 0.54f),
                new BezierRelativeControlPoint(0.62f, -0.28f)
            },
            _ => new[]
            {
                new BezierRelativeControlPoint(0.10f, 0.30f),
                new BezierRelativeControlPoint(0.26f, 0.52f),
                new BezierRelativeControlPoint(0.50f, -0.18f),
                new BezierRelativeControlPoint(0.74f, -0.34f)
            }
        };
    }

    private static BezierRelativeControlPoint[] CreateConverge(int degree)
    {
        return degree switch
        {
            3 => new[]
            {
                new BezierRelativeControlPoint(0.10f, 0.62f),
                new BezierRelativeControlPoint(0.68f, 0.08f)
            },
            4 => new[]
            {
                new BezierRelativeControlPoint(0.08f, 0.70f),
                new BezierRelativeControlPoint(0.32f, 0.46f),
                new BezierRelativeControlPoint(0.72f, 0.06f)
            },
            _ => new[]
            {
                new BezierRelativeControlPoint(0.06f, 0.78f),
                new BezierRelativeControlPoint(0.22f, 0.54f),
                new BezierRelativeControlPoint(0.46f, 0.28f),
                new BezierRelativeControlPoint(0.76f, 0.04f)
            }
        };
    }

    private static void ApplyVariantOffsets(
        BezierRelativeControlPoint[] controlPoints,
        float lane,
        float sideSign,
        float laneScale,
        int randomSeed,
        float randomStrength,
        BezierPatternType pattern,
        int variantIndex)
    {
        for (int i = 0; i < controlPoints.Length; i++)
        {
            var point = controlPoints[i];
            float forwardJitter = (Hash01(randomSeed, i, (int)pattern, variantIndex, 11) - 0.5f) * randomStrength * 0.18f;
            float lateralJitter = (Hash01(randomSeed, i, (int)pattern, variantIndex, 37) - 0.5f) * randomStrength * 0.30f;
            float laneOffset = lane * (0.10f + 0.04f * i);

            controlPoints[i] = new BezierRelativeControlPoint(
                point.ForwardRatio + forwardJitter,
                point.LateralRatio * sideSign * laneScale + laneOffset + lateralJitter
            );
        }
    }

    private static void NormalizeForwardRatios(BezierRelativeControlPoint[] controlPoints)
    {
        float previous = -0.40f;
        for (int i = 0; i < controlPoints.Length; i++)
        {
            float maxAllowed = 0.92f - 0.06f * (controlPoints.Length - i - 1);
            float clamped = Mathf.Clamp(controlPoints[i].ForwardRatio, previous + ForwardMinGap, maxAllowed);
            controlPoints[i] = new BezierRelativeControlPoint(clamped, controlPoints[i].LateralRatio);
            previous = clamped;
        }
    }

    private static float ResolveLane(int variantIndex, int variantCount)
    {
        if (variantCount <= 1)
        {
            return 0f;
        }

        float normalized = (variantIndex % variantCount) / Mathf.Max(variantCount - 1f, 1f);
        return normalized * 2f - 1f;
    }

    private static float ResolveSideSign(int variantIndex, float lane)
    {
        if (Mathf.Abs(lane) > 0.001f)
        {
            return Mathf.Sign(lane);
        }

        return (variantIndex & 1) == 0 ? 1f : -1f;
    }

    private static float Hash01(int seed, int a, int b, int c, int salt)
    {
        uint value = (uint)(seed * 73856093 ^ a * 19349663 ^ b * 83492791 ^ c * 265443576 ^ salt * 97531);
        value ^= value >> 16;
        value *= 2246822519u;
        value ^= value >> 13;
        value *= 3266489917u;
        value ^= value >> 16;
        return (value & 0x00FFFFFFu) / 16777215f;
    }
}
