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
    public readonly record struct StopResolution(
        bool EmitCompletedEvent,
        bool DestroyEntity,
        MoveMode NextMode
    );

    /// <summary>
    /// 解析一次停止请求的最终行为。
    /// </summary>
    public static StopResolution Resolve(
        MoveMode currentMode,
        MoveMode defaultMode,
        in MovementParams @params,
        MovementStopReason reason,
        bool emitCompletedEvent = true,
        MoveMode requestedNextMode = MoveMode.None,
        bool destroyEntity = false)
    {
        bool willDestroy = destroyEntity || ShouldDestroyByReason(@params, reason);
        MoveMode nextMode = MoveMode.None;

        if (!willDestroy)
        {
            if (requestedNextMode != MoveMode.None)
            {
                nextMode = requestedNextMode;
            }
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

    private static bool ShouldDestroyByReason(in MovementParams @params, MovementStopReason reason)
    {
        if (reason == MovementStopReason.Collision)
        {
            return @params.Collision.HasValue && @params.Collision.Value.DestroyOnStop;
        }

        return reason == MovementStopReason.Completed && @params.DestroyOnComplete;
    }
}
