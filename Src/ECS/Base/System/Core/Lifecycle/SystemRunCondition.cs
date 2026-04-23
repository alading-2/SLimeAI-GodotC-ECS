using System;
using System.Collections.Generic;

/// <summary>
/// 系统运行条件。
/// <para>用于把业务状态判断前置到调度层，避免系统内部散落条件分支。</para>
/// </summary>
public sealed class SystemRunCondition
{
    /// <summary>无条件运行。</summary>
    public static SystemRunCondition Always { get; } = new();

    /// <summary>
    /// 局内主玩法运行条件。
    /// <para>仅在会话进行中且执行态为 Running 时通过。</para>
    /// </summary>
    public static SystemRunCondition GameplayRunning()
    {
        return new SystemRunCondition
        {
            AllowedAppPhases = AppPhase.InSession,
            AllowedSessionPhases = SessionPhase.Playing,
            AllowedExecutionPhases = ExecutionPhase.Running
        };
    }

    /// <summary>
    /// 覆盖层系统运行条件。
    /// <para>默认允许 PauseMenu / ModalUi / Cutscene 三类覆盖层。</para>
    /// </summary>
    /// <param name="allowedPhases">允许的覆盖层阶段；为 None 时使用默认覆盖层集合。</param>
    public static SystemRunCondition OverlayActive(OverlayPhase allowedPhases = OverlayPhase.None)
    {
        var overlayPhases = allowedPhases == OverlayPhase.None
            ? OverlayPhase.PauseMenu | OverlayPhase.ModalUi | OverlayPhase.Cutscene
            : allowedPhases;

        return new SystemRunCondition
        {
            AllowedOverlayPhases = overlayPhases
        };
    }

    /// <summary>允许的应用主阶段；为 None 表示不限制。</summary>
    public AppPhase AllowedAppPhases { get; set; } = AppPhase.None;

    /// <summary>允许的会话阶段；为 None 表示不限制。</summary>
    public SessionPhase AllowedSessionPhases { get; set; } = SessionPhase.None;

    /// <summary>允许的覆盖层阶段；为 None 表示不限制。</summary>
    public OverlayPhase AllowedOverlayPhases { get; set; } = OverlayPhase.None;

    /// <summary>禁止的覆盖层阶段；为 None 表示不限制。</summary>
    public OverlayPhase BlockedOverlayPhases { get; set; } = OverlayPhase.None;

    /// <summary>允许的执行阶段；为 None 表示不限制。</summary>
    public ExecutionPhase AllowedExecutionPhases { get; set; } = ExecutionPhase.None;

    /// <summary>
    /// 判断指定快照是否满足当前运行条件。
    /// </summary>
    /// <param name=”snapshot”>当前项目状态快照。</param>
    /// <returns>满足条件返回 true。</returns>
    public bool Evaluate(ProjectStateSnapshot snapshot)
    {
        // 规则：任一”允许集合”不匹配即失败；任一”禁止集合”命中即失败。
        // 允许集合为 None 表示”该维度不限制”。
        if (!MatchesFlags(AllowedAppPhases, snapshot.AppPhase))
        {
            return false;
        }

        if (!MatchesFlags(AllowedSessionPhases, snapshot.SessionPhase))
        {
            return false;
        }

        if (!MatchesFlags(AllowedOverlayPhases, snapshot.OverlayPhase))
        {
            return false;
        }

        if (ContainsFlags(BlockedOverlayPhases, snapshot.OverlayPhase))
        {
            return false;
        }

        if (!MatchesFlags(AllowedExecutionPhases, snapshot.ExecutionPhase))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// 判断当前值是否匹配允许的 Flags 组合。
    /// </summary>
    /// <param name=”allowedFlags”>允许的 Flags 组合（为 None 表示不限制）。</param>
    /// <param name=”currentValue”>当前值。</param>
    private static bool MatchesFlags<T>(T allowedFlags, T currentValue) where T : struct, Enum
    {
        var allowedInt = Convert.ToInt32(allowedFlags);
        var currentInt = Convert.ToInt32(currentValue);

        // allowedFlags 为 0 (None) 时代表 wildcard（任意值均通过）。
        if (allowedInt == 0)
        {
            return true;
        }

        // 按位与判断：当前值必须在允许的 Flags 范围内。
        return (currentInt & allowedInt) != 0;
    }

    /// <summary>
    /// 判断当前值是否包含在禁止的 Flags 组合中。
    /// </summary>
    /// <param name=”blockedFlags”>禁止的 Flags 组合（为 None 表示不限制）。</param>
    /// <param name=”currentValue”>当前值。</param>
    private static bool ContainsFlags<T>(T blockedFlags, T currentValue) where T : struct, Enum
    {
        var blockedInt = Convert.ToInt32(blockedFlags);
        var currentInt = Convert.ToInt32(currentValue);

        // blockedFlags 为 0 (None) 时代表不禁止任何值。
        if (blockedInt == 0)
        {
            return false;
        }

        // 按位与判断：当前值是否在禁止的 Flags 范围内。
        return (currentInt & blockedInt) != 0;
    }
}
