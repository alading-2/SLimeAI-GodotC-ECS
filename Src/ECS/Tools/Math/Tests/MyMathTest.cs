using Godot;
using System;

public partial class MyMathTest : Node
{
    private static readonly Log _log = new Log("MyMathTest");

    public override void _Ready()
    {
        Run();
        GetTree().Quit();
    }

    public void Run()
    {
        _log.Info("开始测试 Math 工具...");

        TestCheckProbability();
        TestEllipseArc2D();
        TestParabola2D();
        TestCircularArc2D();
        TestCircularArcWorldUp();
        TestBezierTemplate_LongDistanceOffsetIsCapped();
        TestBezierTemplate_TrackingWeightReducesCurveSwing();

        _log.Info("Math 工具测试完成");
    }

    private void TestCheckProbability()
    {
        bool result0 = MyMath.CheckProbability(0);
        AssertFalse(result0, "0% 概率不应触发");

        bool result100 = MyMath.CheckProbability(100);
        AssertTrue(result100, "100% 概率应触发");

        bool result150 = MyMath.CheckProbability(150);
        AssertTrue(result150, "超过 100% 概率应触发");

        bool resultNeg = MyMath.CheckProbability(-10);
        AssertFalse(resultNeg, "负概率不应触发");

        int successCount = 0;
        int totalTests = 10000;
        for (int i = 0; i < totalTests; i++)
        {
            if (MyMath.CheckProbability(50))
            {
                successCount++;
            }
        }

        float rate = (float)successCount / totalTests;
        bool isReasonable = rate > 0.45f && rate < 0.55f;
        AssertTrue(isReasonable, $"50% 概率统计测试结果: {rate:P2}");
    }

    private void TestEllipseArc2D()
    {
        var curve = EllipseArc2D.Create(Vector2.Zero, new Vector2(100f, 0f), 40f, true);

        Vector2 start = curve.Evaluate(0f);
        Vector2 end = curve.Evaluate(1f);
        Vector2 mid = curve.Evaluate(0.5f);
        Vector2 tangent = curve.EvaluateTangent(0.5f);

        AssertNear(start, Vector2.Zero, 0.01f, "EllipseArc2D t=0 返回起点");
        AssertNear(end, new Vector2(100f, 0f), 0.01f, "EllipseArc2D t=1 返回终点");
        AssertTrue(mid.Y > 0f, "EllipseArc2D 顺时针侧偏时中点应落在弦线下方");
        AssertTrue(tangent.LengthSquared() > 0.1f, "EllipseArc2D 中点切线应有效");

        var mirroredCurve = EllipseArc2D.Create(Vector2.Zero, new Vector2(100f, 0f), 40f, false);
        Vector2 mirroredMid = mirroredCurve.Evaluate(0.5f);
        AssertTrue(mid.Y > 0f && mirroredMid.Y < 0f, "EllipseArc2D 顺逆时针侧偏结果应相反");
    }

    private void TestParabola2D()
    {
        var curve = Parabola2D.Create(Vector2.Zero, new Vector2(100f, 0f), 30f);

        Vector2 start = curve.Evaluate(0f);
        Vector2 end = curve.Evaluate(1f);
        Vector2 mid = curve.Evaluate(0.5f);

        AssertNear(start, Vector2.Zero, 0.01f, "Parabola2D t=0 返回起点");
        AssertNear(end, new Vector2(100f, 0f), 0.01f, "Parabola2D t=1 返回终点");
        AssertTrue(mid.Y > 25f, "Parabola2D 中段高度应接近顶高");

        var linearCurve = Parabola2D.Create(Vector2.Zero, new Vector2(100f, 0f), 0f);
        Vector2 linearMid = linearCurve.Evaluate(0.5f);
        AssertNear(linearMid, new Vector2(50f, 0f), 0.01f, "Parabola2D 顶高为 0 时应退化为直线");
    }

    private void TestCircularArc2D()
    {
        var curve = CircularArc2D.Create(Vector2.Zero, new Vector2(100f, 0f), 80f, true);

        Vector2 start = curve.Evaluate(0f);
        Vector2 end = curve.Evaluate(1f);
        Vector2 mid = curve.Evaluate(0.5f);

        AssertNear(start, Vector2.Zero, 0.01f, "CircularArc2D t=0 返回起点");
        AssertNear(end, new Vector2(100f, 0f), 0.01f, "CircularArc2D t=1 返回终点");
        AssertTrue(mid.Y > 0f, "CircularArc2D 顺时针侧偏时中点应落在弦线下方");

        var invalidCurve = CircularArc2D.Create(Vector2.Zero, new Vector2(100f, 0f), 40f, true);
        AssertFalse(invalidCurve.IsValid, "CircularArc2D 半径不足时应构建失败");
    }

    private void TestCircularArcWorldUp()
    {
        Vector2 downwardTarget = new Vector2(100f, 100f);
        var downwardCurve = CircularArc2D.CreateWorldUp(Vector2.Zero, downwardTarget, 180f, true);
        Vector2 downwardMid = downwardCurve.Evaluate(0.5f);
        AssertTrue(downwardCurve.IsValid, "CircularArc2D.CreateWorldUp 向下目标应构建成功");
        AssertTrue(downwardMid.Y < 50f, "CircularArc2D.CreateWorldUp 向下目标时中点也应朝屏幕上方弯曲");

        Vector2 downwardLeftTarget = new Vector2(-100f, 100f);
        var downwardLeftCurve = CircularArc2D.CreateWorldUp(Vector2.Zero, downwardLeftTarget, 180f, false);
        Vector2 downwardLeftMid = downwardLeftCurve.Evaluate(0.5f);
        AssertTrue(downwardLeftCurve.IsValid, "CircularArc2D.CreateWorldUp 向左下目标应构建成功");
        AssertTrue(downwardLeftMid.Y < 50f, "CircularArc2D.CreateWorldUp 向左下目标时中点也应朝屏幕上方弯曲");
    }

    private void TestBezierTemplate_LongDistanceOffsetIsCapped()
    {
        var template = BezierTemplateBuilder.CreatePattern(
            5,
            BezierPatternType.RearWrap,
            variantIndex: 2,
            variantCount: 5,
            randomSeed: 20260419);
        Vector2[] points = new Vector2[template.PointCount];
        BezierTemplateBuilder.ResolvePoints(template, Vector2.Zero, new Vector2(1200f, 0f), points);

        float maxLateralOffset = 0f;
        for (int i = 1; i < points.Length - 1; i++)
        {
            maxLateralOffset = Mathf.Max(maxLateralOffset, Mathf.Abs(points[i].Y));
        }

        AssertTrue(maxLateralOffset <= 320f, $"BezierTemplate 长距离侧偏应受控，actual={maxLateralOffset:F1}");
    }

    private void TestBezierTemplate_TrackingWeightReducesCurveSwing()
    {
        var template = BezierTemplateBuilder.CreatePattern(
            5,
            BezierPatternType.RearWrap,
            variantIndex: 0,
            variantCount: 1,
            randomSeed: 20260419);
        Vector2[] fullWeightPoints = new Vector2[template.PointCount];
        Vector2[] lowWeightPoints = new Vector2[template.PointCount];

        BezierTemplateBuilder.ResolvePoints(template, Vector2.Zero, new Vector2(500f, 0f), fullWeightPoints, 1f);
        BezierTemplateBuilder.ResolvePoints(template, Vector2.Zero, new Vector2(500f, 0f), lowWeightPoints, 0.25f);

        float fullWeightOffset = 0f;
        float lowWeightOffset = 0f;
        for (int i = 1; i < template.PointCount - 1; i++)
        {
            fullWeightOffset = Mathf.Max(fullWeightOffset, Mathf.Abs(fullWeightPoints[i].Y));
            lowWeightOffset = Mathf.Max(lowWeightOffset, Mathf.Abs(lowWeightPoints[i].Y));
        }

        AssertTrue(lowWeightOffset < fullWeightOffset, $"BezierTemplate 剩余段权重降低后应减少飘移，full={fullWeightOffset:F1}, low={lowWeightOffset:F1}");
    }

    private void AssertNear(Vector2 actual, Vector2 expected, float tolerance, string message)
    {
        bool isNear = actual.DistanceTo(expected) <= tolerance;
        AssertTrue(isNear, $"{message}，actual={actual} expected={expected}");
    }

    private void AssertTrue(bool condition, string message)
    {
        if (condition)
        {
            _log.Info($"[通过] {message}");
        }
        else
        {
            _log.Error($"[失败] {message}");
        }
    }

    private void AssertFalse(bool condition, string message)
    {
        AssertTrue(!condition, message);
    }
}
