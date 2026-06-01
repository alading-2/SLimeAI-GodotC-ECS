using Godot;

/// <summary>
/// 通用实体朝向组件。
/// <para>
/// 该组件是最终朝向输出层：Movement 只负责解算并发布 <c>MovementFacingDirection</c>，
/// 朝向组件再根据 <see cref="Sink"/> 决定最终写 root <c>RotationDegrees</c> 还是 <c>VisualRoot.FlipH</c>。
/// </para>
/// <para>
/// 当前支持三种逻辑模式：跟随移动朝向、纯自转、跟随并自转；
/// 当前支持两种输出目标：<c>RootRotation</c> 与 <c>VisualFlipX</c>。
/// </para>
/// </summary>
public partial class EntityOrientationComponent : Node, IComponent
{
    private static readonly Log _log = new(nameof(EntityOrientationComponent));

    [Export]
    public OrientationSink Sink { get; set; } = OrientationSink.RootRotation;

    private IEntity? _entity;
    private Data? _data;
    private Node2D? _node;
    private AnimatedSprite2D? _visualSprite;

    // ================= 组件内部运行态 =================

    private bool _isActive; //朝向控制是否激活
    private OrientationSource _source = OrientationSource.Standalone; //朝向来源（Movement/Standalone）
    private OrientationMode _mode = OrientationMode.FollowMovement; //朝向模式（跟随移动/纯自转/跟随并自转）
    private bool _stopWithMovement; //是否跟随运动生命周期自动停止
    private float _baseAngle; //基础朝向角（度）
    private float _currentAngularSpeed; //当前角速度（度/秒）
    private float _angularAcceleration; //角加速度（度/秒²）
    private float _totalAngle = -1f; //总旋转角度（度，-1=无限制）
    private float _initialAngle; //初始偏移角（度）
    private float _accumulatedAngle; //已累积自转角度（度）
    private bool _isClockwise = true; //旋转方向（true=顺时针）
    private bool _isSuspendedByMovement; // 是否因当前 movement 请求暂停朝向输出

    public void OnComponentRegistered(Node entity)
    {
        if (entity is not Node2D node || entity is not IEntity iEntity) return;

        _entity = iEntity;
        _data = iEntity.Data;
        _node = node;
        _visualSprite = ResolveVisualSprite(node);

        // 订阅运动生命周期事件，用于跟随运动朝向的启停联动
        _entity.Events.On<GameEventType.Unit.MovementStarted>(OnMovementStarted);
        _entity.Events.On<GameEventType.Unit.MovementCompleted>(OnMovementCompleted);
        _entity.Events.On<GameEventType.Unit.MovementStopRequested>(OnMovementStopRequested);
        // 订阅朝向控制事件，用于外部主动启停
        _entity.Events.On<GameEventType.Unit.OrientationStarted>(OnOrientationStarted);
        _entity.Events.On<GameEventType.Unit.OrientationStopped>(OnOrientationStopped);

        ResetOrientationState(GetCurrentPresentedAngle()); // 初始化默认跟随输出状态
    }

    public void OnComponentUnregistered()
    {
        // 释放引用，防止悬挂
        _visualSprite = null;
        _node = null;
        _data = null;
        _entity = null;
    }

    public override void _Process(double delta)
    {
        if (_data == null || _node == null) return;
        // 朝向未激活时跳过
        if (!_isActive) return;

        float baseAngle = ResolveBaseAngle(_mode); // 基础朝向角（跟随移动或固定）
        float spinOffset = ResolveSpinOffset((float)delta, _mode); // 自转偏移角

        ApplyOrientationOutput(baseAngle, spinOffset);
    }

    private void OnMovementStarted(GameEventType.Unit.MovementStarted evt)
    {
        if (_data == null) return;

        // 新移动携带朝向参数时，立即应用为 Movement 来源
        if (evt.Params.Orientation.HasValue)
        {
            ApplyOrientation(evt.Params.Orientation.Value, OrientationSource.Movement, true);
            return;
        }

        // 当前 movement 显式要求不改朝向时，暂停输出但保留当前视觉结果
        if (!evt.Params.RotateToVelocity)
        {
            SuspendOrientation();
            return;
        }

        // 没有显式 Orientation 参数时，恢复默认跟随输出
        if (_isSuspendedByMovement || _source == OrientationSource.Movement)
        {
            ResumeDefaultOrientation();
        }
    }

    private void OnMovementCompleted(GameEventType.Unit.MovementCompleted evt)
    {
        // 运动完成后，如果当前 movement 曾暂停朝向，恢复默认跟随
        if (_isSuspendedByMovement)
        {
            ResumeDefaultOrientation();
            return;
        }

        // 运动完成且配置了跟随运动生命周期时，同步结束显式 movement 朝向
        if (ShouldStopWithMovement(OrientationSource.Movement))
        {
            StopOrientation(evt.Reason);
        }
    }

    private void OnMovementStopRequested(GameEventType.Unit.MovementStopRequested evt)
    {
        // 外部请求打断运动时，若当前 movement 曾暂停朝向，同步恢复默认跟随
        if (_isSuspendedByMovement)
        {
            ResumeDefaultOrientation();
            return;
        }

        // 外部请求打断运动时，同步结束显式 movement 朝向
        if (ShouldStopWithMovement(OrientationSource.Movement))
        {
            StopOrientation(evt.Reason);
        }
    }

    private void OnOrientationStarted(GameEventType.Unit.OrientationStarted evt)
    {
        // 外部主动启动朝向控制
        ApplyOrientation(evt.Params, evt.Source, evt.StopWithMovement);
    }

    private void OnOrientationStopped(GameEventType.Unit.OrientationStopped evt)
    {
        if (_data == null) return;
        if (!_isActive) return;
        // 仅处理与事件来源匹配的朝向控制，避免误停其他来源
        if (_source != evt.Source) return;

        StopOrientation(evt.Reason);
    }

    /// <summary>
    /// 判断当前朝向是否应跟随运动生命周期停止。
    /// </summary>
    private bool ShouldStopWithMovement(OrientationSource source)
    {
        if (_data == null) return false;
        if (!_isActive) return false;
        if (_source != source) return false;
        return _stopWithMovement;
    }

    /// <summary>
    /// 应用朝向控制参数，并初始化内部运行态。
    /// </summary>
    /// <param name="params">朝向控制参数</param>
    /// <param name="source">参数来源（Movement/Standalone）</param>
    /// <param name="stopWithMovement">是否跟随 movement 生命周期自动停止</param>
    private void ApplyOrientation(
        OrientationParams @params,
        OrientationSource source,
        bool stopWithMovement)
    {
        if (_data == null || _node == null) return;

        _isActive = true; // 激活朝向控制
        _isSuspendedByMovement = false; // 新显式朝向接管后取消暂停标记
        _source = source; // 记录来源
        _mode = @params.Mode; // 朝向模式
        _stopWithMovement = stopWithMovement; // 是否跟随运动停止
        _currentAngularSpeed = Mathf.Max(@params.AngularSpeed, 0f); // 当前角速度
        _angularAcceleration = @params.AngularAcceleration; // 角加速度
        _totalAngle = @params.TotalAngle; // 总旋转角度（-1=无限制）
        _initialAngle = @params.InitialAngle; // 初始偏移角
        _accumulatedAngle = 0f; // 已累积角度归零
        _isClockwise = @params.IsClockwise; // 旋转方向
        _baseAngle = ResolveInitialBaseAngle(@params.Mode); // 初始基础角
    }

    /// <summary>
    /// 根据配置的输出目标应用最终朝向。
    /// </summary>
    private void ApplyOrientationOutput(float baseAngle, float spinOffset)
    {
        if (_node == null) return;

        if (Sink == OrientationSink.VisualFlipX)
        {
            ApplyVisualFlip(baseAngle);
            return;
        }

        // RootRotation：最终旋转 = 基础朝向 + 自转偏移
        _node.RotationDegrees = baseAngle + spinOffset;
    }

    /// <summary>
    /// 角色类单位用 FlipH 表示左右朝向，不旋转 root。
    /// </summary>
    private void ApplyVisualFlip(float baseAngle)
    {
        if (_visualSprite == null)
        {
            if (_node != null)
            {
                _log.Warn($"[{_node.Name}] OrientationSink=VisualFlipX 但未找到 AnimatedSprite2D/VisualRoot");
            }
            return;
        }

        Vector2 facingDirection = Vector2.FromAngle(Mathf.DegToRad(baseAngle));
        if (Mathf.Abs(facingDirection.X) < 0.1f) return; // 接近竖直时不翻面，避免抖动

        _visualSprite.FlipH = facingDirection.X < 0f;
    }

    private static AnimatedSprite2D? ResolveVisualSprite(Node2D node)
    {
        var visualRoot = node.GetNodeOrNull("VisualRoot");
        if (visualRoot is AnimatedSprite2D sprite)
        {
            return sprite;
        }

        return visualRoot?.GetNodeOrNull<AnimatedSprite2D>(".");
    }

    /// <summary>
    /// 解析初始基础角。
    /// <para>SpinOnly 模式取当前旋转角；其他模式优先取移动朝向方向，兜底取当前旋转角。</para>
    /// </summary>
    private float ResolveInitialBaseAngle(OrientationMode mode)
    {
        if (_data == null || _node == null) return 0f;

        // 纯自转模式：基础角固定为当前旋转角
        if (mode == OrientationMode.SpinOnly)
        {
            return _node.RotationDegrees;
        }

        // 跟随移动模式：优先取移动朝向方向角
        Vector2 facing = _data.Get<Vector2>(GeneratedDataKey.MovementFacingDirection);
        return facing.LengthSquared() >= 0.001f
            ? Mathf.RadToDeg(facing.Angle())
            : GetCurrentPresentedAngle(); // 无朝向输入时兜底取当前真实朝向
    }

    /// <summary>
    /// 每帧解析基础朝向角。
    /// <para>SpinOnly 模式返回缓存的固定角；FollowMovement 模式实时追踪移动朝向。</para>
    /// </summary>
    private float ResolveBaseAngle(OrientationMode mode)
    {
        if (_data == null) return 0f;

        float baseAngle = _baseAngle;
        // 纯自转模式：基础角不变
        if (mode == OrientationMode.SpinOnly)
        {
            return baseAngle;
        }

        // 跟随移动模式：实时读取移动朝向方向
        Vector2 facing = _data.Get<Vector2>(GeneratedDataKey.MovementFacingDirection);
        if (facing.LengthSquared() < 0.001f)
        {
            return baseAngle; // 停顿或当前帧无朝向输入时，沿用上一帧基础角
        }

        baseAngle = Mathf.RadToDeg(facing.Angle());
        _baseAngle = baseAngle; // 更新缓存
        return baseAngle;
    }

    /// <summary>
    /// 每帧解析自转偏移角。
    /// <para>FollowMovement 模式仅返回初始偏移；含自转的模式累加角速度并检查总角度限制。</para>
    /// </summary>
    private float ResolveSpinOffset(float delta, OrientationMode mode)
    {
        if (_data == null) return 0f;

        float initialAngle = _initialAngle;
        // 纯跟随移动模式：无自转，仅保留初始偏移
        if (mode == OrientationMode.FollowMovement || Sink == OrientationSink.VisualFlipX)
        {
            return initialAngle;
        }

        // 含自转模式：累加角速度
        float currentAngularSpeed = Mathf.Max(0f, _currentAngularSpeed);
        float angularAcceleration = _angularAcceleration;
        // 角加速度驱动角速度变化
        if (!Mathf.IsZeroApprox(angularAcceleration))
        {
            currentAngularSpeed = Mathf.Max(0f, currentAngularSpeed + angularAcceleration * delta);
            _currentAngularSpeed = currentAngularSpeed;
        }

        float accumulatedAngle = _accumulatedAngle;
        float totalAngle = _totalAngle;
        float deltaAngle = currentAngularSpeed * delta;
        // 总角度限制：本帧增量不超过剩余角度
        if (totalAngle >= 0f)
        {
            float remainingAngle = Mathf.Max(totalAngle - accumulatedAngle, 0f);
            deltaAngle = Mathf.Min(deltaAngle, remainingAngle);
        }

        accumulatedAngle += deltaAngle;
        _accumulatedAngle = accumulatedAngle;

        // 达到总角度后自动停机
        if (totalAngle >= 0f && accumulatedAngle >= totalAngle - 0.001f)
        {
            StopOrientation(MovementStopReason.Completed);
        }

        float directionSign = _isClockwise ? 1f : -1f;
        return initialAngle + directionSign * accumulatedAngle;
    }

    /// <summary>
    /// 停止朝向控制，记录最终角度并重置状态。
    /// </summary>
    private void StopOrientation(MovementStopReason reason)
    {
        if (_data == null || _node == null) return;

        float currentRotation = GetCurrentPresentedAngle(); // 记录停机时的真实朝向，兼容 FlipH / RootRotation 两种 sink
        ResetOrientationState(currentRotation);
        _log.Debug($"[{_node.Name}] 停止朝向控制 Reason={reason}");
    }

    /// <summary>
    /// 仅暂停当前 movement 对朝向的驱动，保留当前显示结果。
    /// 典型场景：Dash 等显式要求“保持原有朝向”的运动。
    /// </summary>
    private void SuspendOrientation()
    {
        if (_node == null) return;

        _isActive = false;
        _isSuspendedByMovement = true;
        _baseAngle = GetCurrentPresentedAngle(); // 保留冻结前的真实朝向
    }

    /// <summary>
    /// 恢复到默认的“跟随移动朝向”输出模式。
    /// </summary>
    private void ResumeDefaultOrientation()
    {
        float currentAngle = GetCurrentPresentedAngle();
        ResetOrientationState(currentAngle);
    }

    /// <summary>
    /// 读取当前实际呈现给玩家的朝向角。
    /// <para>
    /// RootRotation 直接读节点旋转；
    /// VisualFlipX 则把 FlipH 映射成 0° / 180°，避免在 root 始终为 0° 时丢失左朝向。
    /// </para>
    /// </summary>
    private float GetCurrentPresentedAngle()
    {
        if (_node == null) return _baseAngle;

        if (Sink == OrientationSink.VisualFlipX)
        {
            return _visualSprite?.FlipH == true ? 180f : 0f;
        }

        return _node.RotationDegrees;
    }

    /// <summary>
    /// 重置所有朝向内部运行态为默认值，保留指定基础角。
    /// </summary>
    private void ResetOrientationState(float baseAngle)
    {
        _isActive = false; // 关闭朝向控制
        _isSuspendedByMovement = false; // 清除暂停标记
        _source = OrientationSource.Standalone; // 来源归为独立
        _mode = OrientationMode.FollowMovement; // 模式归为跟随移动
        _stopWithMovement = false; // 不跟随运动停止
        _baseAngle = baseAngle; // 保留当前基础角
        _currentAngularSpeed = 0f; // 角速度归零
        _angularAcceleration = 0f; // 角加速度归零
        _totalAngle = -1f; // 总角度归为无限制
        _initialAngle = 0f; // 初始偏移归零
        _accumulatedAngle = 0f; // 累积角度归零
        _isClockwise = true; // 方向归为顺时针

        // 重置完成后，默认重新进入“跟随移动朝向”的输出状态
        _isActive = true;
    }
}
