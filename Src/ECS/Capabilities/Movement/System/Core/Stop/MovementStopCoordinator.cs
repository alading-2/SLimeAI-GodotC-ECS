using Godot;

/// <summary>
/// 移动停止协调器。
/// <para>
/// 负责根据停止原因与请求参数计算“是否发完成事件、是否销毁、切到哪个模式”。
/// </para>
/// </summary>
public static class MovementStopCoordinator
{
    /// <summary>
    /// 停止解析结果。
    /// </summary>
    /// <param name="EmitCompletedEvent">是否发射移动完成事件</param>
    /// <param name="DestroyEntity">是否销毁实体</param>
    /// <param name="NextMode">停止后切换到的运动模式（None 表示不切换）</param>
    public readonly record struct StopResolution(
        bool EmitCompletedEvent,
        bool DestroyEntity,
        MoveMode NextMode
    );

    /// <summary>
    /// 解析一次停止请求的最终行为。
    /// <para>优先级：显式请求销毁/按原因销毁 → 显式请求下一模式 → 回退默认模式。</para>
    /// </summary>
    /// <param name="currentMode">当前运动模式</param>
    /// <param name="defaultMode">实体默认运动模式（如 PlayerInput / AIControlled），停止后回退用</param>
    /// <param name="params">运动参数，含碰撞/完成时销毁配置</param>
    /// <param name="reason">停止原因（碰撞/完成/手动等）</param>
    /// <param name="emitCompletedEvent">是否发射完成事件，默认 true</param>
    /// <param name="requestedNextMode">调用方显式请求的下一模式，优先于 defaultMode</param>
    /// <param name="destroyEntity">调用方显式请求销毁实体，优先于按原因判断</param>
    public static StopResolution Resolve(
        MoveMode currentMode,
        MoveMode defaultMode,
        in MovementParams @params,
        MovementStopReason reason,
        bool emitCompletedEvent = true,
        MoveMode requestedNextMode = MoveMode.None,
        bool destroyEntity = false)
    {
        // 合并判断：显式请求销毁 OR 按停止原因+参数判定销毁
        bool willDestroy = destroyEntity || ShouldDestroyByReason(@params, reason);
        MoveMode nextMode = MoveMode.None;

        // 仅在非销毁时决定下一模式
        if (!willDestroy)
        {
            // 优先使用调用方显式请求的模式
            if (requestedNextMode != MoveMode.None)
            {
                nextMode = requestedNextMode;
            }
            // 回退到实体默认模式（需与当前模式不同，避免无意义切换）
            else if (defaultMode != MoveMode.None && defaultMode != currentMode)
            {
                nextMode = defaultMode;
            }
        }

        return new StopResolution(
            emitCompletedEvent,
            willDestroy,
            nextMode);
    }

    /// <summary>
    /// 根据停止原因与运动参数判断是否应销毁实体。
    /// <para>碰撞停止：检查 Collision.DestroyOnStop；运动完成：检查 DestroyOnComplete。</para>
    /// </summary>
    private static bool ShouldDestroyByReason(in MovementParams @params, MovementStopReason reason)
    {
        // 碰撞停止时，按碰撞参数中的 DestroyOnStop 决定（如子弹碰墙销毁）
        if (reason == MovementStopReason.Collision)
        {
            return @params.CollisionParams.HasValue && @params.CollisionParams.Value.DestroyOnStop;
        }

        // 运动自然完成时，按 DestroyOnComplete 决定（如抛物线落地销毁）
        return reason == MovementStopReason.Completed && @params.DestroyOnComplete;
    }
}