using Godot;
using System;
using System.Runtime.CompilerServices;

/// <summary>
/// 【模式】贝塞尔曲线移动。
/// <para>
/// 静态模式按完整控制点数组沿曲线推进；追踪模式优先使用贝塞尔模板，
/// 以“当前实体位置 → 当前目标位置”的剩余段重建曲线，维持整体风格稳定。
/// </para>
/// <para>
/// 模板语义：
/// - <c>BezierPoints</c>：兼容旧用法，传完整世界坐标控制点
/// - <c>BezierTemplate</c>：推荐新用法，传相对模板，支持 3~5 阶与稳定追踪
/// </para>
/// </summary>
public class BezierCurveStrategy : IMovementStrategy
{
    private static readonly Log _log = new("BezierCurveStrategy");

    /// <summary>
    /// 静态模式下使用的完整控制点数组。
    /// </summary>
    private Vector2[] _finalPoints = Array.Empty<Vector2>();

    /// <summary>
    /// 追踪模式下使用的工作缓冲区，避免在 Update 中重复分配数组。
    /// </summary>
    private Vector2[] _trackingPoints = Array.Empty<Vector2>();

    /// <summary>
    /// 当前运动的贝塞尔模板；静态模式和追踪模式都可复用它解析控制点。
    /// </summary>
    private BezierCurveTemplate? _template;

    /// <summary>
    /// 追踪模式下锁定的最新目标位置；目标失效后冻结在最后一次有效值。
    /// </summary>
    private Vector2 _trackedTargetPosition;

    [ModuleInitializer]
    public static void Register()
    {
        MovementStrategyRegistry.Register(MoveMode.BezierCurve, () => new BezierCurveStrategy());
    }

    /// <summary>
    /// 策略进入时准备控制点缓存。
    /// </summary>
    public void OnEnter(IEntity entity, Data data, in MovementParams @params)
    {
        ResetState();
        if (entity is not Node2D node)
        {
            return;
        }

        if (@params.MaxDuration <= 0f)
        {
            _log.Warn($"MaxDuration={@params.MaxDuration} 无效，必须 > 0，曲线将不会移动");
        }

        if (@params.isTrackTarget && @params.TargetNode == null)
        {
            _log.Warn("isTrackTarget=true 但 TargetNode 未设置，追踪将退化为固定终点。");
        }

        Vector2 startPos = node.GlobalPosition;

        if (@params.BezierTemplate.HasValue && @params.BezierTemplate.Value.IsValid)
        {
            _template = @params.BezierTemplate.Value.Copy();
            Vector2 initialEnd = ResolveInitialEndPoint(startPos, @params, startPos + Vector2.Right);
            _trackedTargetPosition = initialEnd;
            PrepareTemplatePoints(startPos, initialEnd);
            return;
        }

        if (@params.BezierPoints != null && @params.BezierPoints.Length >= 2)
        {
            _finalPoints = (Vector2[])@params.BezierPoints.Clone();
            _finalPoints[0] = startPos; // 起点统一由真实当前位置覆盖

            Vector2 fallbackEnd = _finalPoints[_finalPoints.Length - 1];
            Vector2 initialEnd = ResolveInitialEndPoint(startPos, @params, fallbackEnd);
            _trackedTargetPosition = initialEnd;
            _finalPoints[_finalPoints.Length - 1] = initialEnd;

            // 旧 BezierPoints 在 3~5 阶时自动转成模板，供追踪模式稳定重建剩余曲线。
            if (BezierTemplateBuilder.TryCreateTemplateFromPoints(_finalPoints, startPos, initialEnd, out BezierCurveTemplate template))
            {
                _template = template.Copy();
                _trackingPoints = new Vector2[_template.Value.PointCount];
            }
            return;
        }

        if (@params.TargetPoint != Vector2.Zero)
        {
            _trackedTargetPosition = @params.TargetPoint;
            _finalPoints = new[] { startPos, @params.TargetPoint };
        }
    }

    /// <summary>
    /// 每帧推进当前位置。
    /// </summary>
    public MovementUpdateResult Update(IEntity entity, Data data, float delta, in MovementParams @params)
    {
        if (entity is not Node2D node)
        {
            return MovementUpdateResult.Continue();
        }

        float duration = @params.MaxDuration;
        if (duration <= 0f)
        {
            return MovementUpdateResult.Continue();
        }

        if (@params.isTrackTarget && _template.HasValue && _template.Value.IsValid)
        {
            return UpdateTrackingCurve(node, data, delta, duration, @params);
        }

        if (_finalPoints.Length < 2)
        {
            return MovementUpdateResult.Continue();
        }

        UpdateTrackedEndPoint(node, @params);

        float t = Mathf.Clamp((@params.ElapsedTime + delta) / duration, 0f, 1f);
        return EvaluateCurveStep(node, data, delta, t, _finalPoints);
    }

    private MovementUpdateResult UpdateTrackingCurve(
        Node2D node,
        Data data,
        float delta,
        float duration,
        in MovementParams @params)
    {
        UpdateTrackedEndPoint(node, @params);

        if (MovementHelper.HasReachedTarget(node.GlobalPosition, _trackedTargetPosition, @params.ReachDistance))
        {
            data.Set(DataKey.Velocity, Vector2.Zero);
            return MovementUpdateResult.Complete();
        }

        float remainingDuration = Mathf.Max(duration - @params.ElapsedTime, delta);
        if (_trackingPoints.Length < _template!.Value.PointCount)
        {
            _trackingPoints = new Vector2[_template.Value.PointCount];
        }

        float remainingRatio = Mathf.Clamp((duration - @params.ElapsedTime) / Mathf.Max(duration, 0.001f), 0f, 1f);
        BezierTemplateBuilder.ResolvePoints(
            _template.Value,
            node.GlobalPosition, // 剩余曲线的当前起点
            _trackedTargetPosition, // 剩余曲线的当前终点
            _trackingPoints,
            remainingRatio);

        float localT = Mathf.Clamp(delta / remainingDuration, 0f, 1f);
        return EvaluateCurveStep(node, data, delta, localT, _trackingPoints);
    }

    private MovementUpdateResult EvaluateCurveStep(
        Node2D node,
        Data data,
        float delta,
        float t,
        ReadOnlySpan<Vector2> points)
    {
        Vector2 newPos = BezierCurve.Evaluate(points, t);
        Vector2 facingDirection = BezierCurve.EvaluateTangent(points, t);
        Vector2 displacement = newPos - node.GlobalPosition;
        float displacementLength = displacement.Length();

        data.Set(
            DataKey.Velocity,
            displacementLength > 0.001f
                ? displacement / Mathf.Max(delta, 0.001f)
                : Vector2.Zero);

        if (t >= 1f)
        {
            return MovementUpdateResult.Complete();
        }

        return MovementUpdateResult.Continue(displacementLength, facingDirection);
    }

    private void PrepareTemplatePoints(Vector2 startPos, Vector2 endPos)
    {
        if (!_template.HasValue || !_template.Value.IsValid)
        {
            return;
        }

        _finalPoints = new Vector2[_template.Value.PointCount];
        _trackingPoints = new Vector2[_template.Value.PointCount];
        BezierTemplateBuilder.ResolvePoints(_template.Value, startPos, endPos, _finalPoints);
    }

    private void UpdateTrackedEndPoint(Node2D node, in MovementParams @params)
    {
        if (@params.isTrackTarget && @params.TargetNode != null && GodotObject.IsInstanceValid(@params.TargetNode))
        {
            _trackedTargetPosition = @params.TargetNode.GlobalPosition;
        }
        else if (_trackedTargetPosition == Vector2.Zero)
        {
            _trackedTargetPosition = ResolveInitialEndPoint(
                node.GlobalPosition,
                @params,
                _finalPoints.Length > 0 ? _finalPoints[_finalPoints.Length - 1] : node.GlobalPosition + Vector2.Right);
        }

        if (!_template.HasValue && _finalPoints.Length >= 2)
        {
            _finalPoints[_finalPoints.Length - 1] = _trackedTargetPosition;
        }
    }

    private static Vector2 ResolveInitialEndPoint(
        Vector2 startPos,
        in MovementParams @params,
        Vector2 fallbackEnd)
    {
        if (@params.TargetNode != null && GodotObject.IsInstanceValid(@params.TargetNode))
        {
            return @params.TargetNode.GlobalPosition;
        }

        if (@params.TargetPoint != Vector2.Zero)
        {
            return @params.TargetPoint;
        }

        if (fallbackEnd != Vector2.Zero)
        {
            return fallbackEnd;
        }

        return startPos + Vector2.Right; // 避免退化成零长度曲线
    }

    private void ResetState()
    {
        _finalPoints = Array.Empty<Vector2>();
        _trackingPoints = Array.Empty<Vector2>();
        _template = null;
        _trackedTargetPosition = Vector2.Zero;
    }
}
