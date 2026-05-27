using Godot;

public partial class EntityMovementComponent
{
    // ================= 碰撞处理 =================

    /// <summary>
    /// 碰撞进入回调（由 CollisionComponent 通过 Entity.Events 转发，仅 Area2D Entity 根节点触发）
    /// <para>仅在非默认运动模式下响应，避免常驻 AI/Player 模式产生噪声事件。</para>
    /// </summary>
    private void OnCollisionDetected(GameEventType.Collision.CollisionEntered evt)
    {
        if (_entity == null || _data == null) return;
        if (_moveCompleted) return;

        var mode = _data.Get<MoveMode>(DataKey.MoveMode);
        var defaultMode = _data.Get<MoveMode>(DataKey.DefaultMoveMode);
        // 移动模式为 defaultMode 或 None 时不处理碰撞通知。
        if (mode == defaultMode || mode == MoveMode.None) return;

        TryHandleRawCollision(evt.Target);
    }

    /// <summary>
    /// 处理一次原始碰撞候选。
    /// <para>流程：1.前置过滤 → 2.碰撞策略裁决 → 3.回调/事件/停止请求</para>
    /// </summary>
    /// <param name="target">碰撞候选节点（约定直接为 IEntity 根节点）</param>
    private void TryHandleRawCollision(Node2D? target)
    {
        // 运动已结束，忽略后续碰撞。
        if (_moveCompleted) return;
        if (_entity == null || _data == null) return;
        // 碰撞策略未启用则跳过。
        if (!_collisionPolicy.IsEnabled) return;

        var moveMode = _data.Get<MoveMode>(DataKey.MoveMode);
        var defaultMode = _data.Get<MoveMode>(DataKey.DefaultMoveMode);
        // 默认模式（如 AI 巡逻/玩家移动）不触发碰撞处理，避免噪声。
        if (moveMode == MoveMode.None || moveMode == defaultMode) return;

        // 交由碰撞策略裁决：过滤掩码/重复/目标匹配，输出碰撞上下文。
        if (!_collisionPolicy.TryAccept(_entity, moveMode, _params, target, out var context))
        {
            return;
        }

        if (!_params.CollisionParams.HasValue)
        {
            return;
        }

        var collision = _params.CollisionParams.Value;

        // 调用外部回调（如炮弹命中扣血）。
        collision.OnCollision?.Invoke(context);

        // 发出 MovementCollision 事件，供外部订阅。
        if (collision.EmitCollisionEvent)
        {
            _entity.Events.Emit(
                GameEventType.Unit.MovementCollision,
                new GameEventType.Unit.MovementCollision(
                    context.Mode,
                    context.TargetNode,
                    context.TargetEntity,
                    context.CollisionCount,
                    context.WillStop));
        }

        _log.Debug(
            $"[{(_entity as Node)?.Name}] 运动碰撞 Mode={moveMode}, Target={context.TargetNode.Name}, Count={context.CollisionCount}, WillStop={context.WillStop}");

        // 只有达到 StopAfterCollisionCount 阈值的那次有效碰撞，才会发出停止请求；
        // DestroyOnStop 也只会在这里生效，不会出现“配置碰撞 5 次却首碰即销毁”。
        if (!context.WillStop)
        {
            return;
        }

        _entity.Events.Emit(
            GameEventType.Unit.MovementStopRequested,
            new GameEventType.Unit.MovementStopRequested
            {
                Reason = MovementStopReason.Collision,
                EmitCompletedEvent = true,
                CollisionTarget = context.TargetNode, //碰撞目标
                DestroyEntity = collision.DestroyOnStop //仅在达到 StopAfterCollisionCount 并触发停止时才销毁
            });
    }
}