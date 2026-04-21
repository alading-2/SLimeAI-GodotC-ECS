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
            AllowedAppPhases = [AppPhase.InSession],
            AllowedSessionPhases = [SessionPhase.Playing],
            AllowedExecutionPhases = [ExecutionPhase.Running]
        };
    }

    /// <summary>
    /// 覆盖层系统运行条件。
    /// <para>默认允许 PauseMenu / ModalUi / Cutscene 三类覆盖层。</para>
    /// </summary>
    /// <param name="allowedPhases">允许的覆盖层阶段；为空时使用默认覆盖层集合。</param>
    public static SystemRunCondition OverlayActive(params OverlayPhase[] allowedPhases)
    {
        var overlayPhases = allowedPhases.Length == 0
            ?[OverlayPhase.PauseMenu, OverlayPhase.ModalUi, OverlayPhase.Cutscene]
            : allowedPhases;

        return new SystemRunCondition
        {
            AllowedOverlayPhases = overlayPhases
        };
    }

    /// <summary>允许的应用主阶段；为空表示不限制。</summary>
    public AppPhase[] AllowedAppPhases { get; set; } = Array.Empty<AppPhase>();

    /// <summary>允许的会话阶段；为空表示不限制。</summary>
    public SessionPhase[] AllowedSessionPhases { get; set; } = Array.Empty<SessionPhase>();

    /// <summary>允许的覆盖层阶段；为空表示不限制。</summary>
    public OverlayPhase[] AllowedOverlayPhases { get; set; } = Array.Empty<OverlayPhase>();

    /// <summary>禁止的覆盖层阶段；为空表示不限制。</summary>
    public OverlayPhase[] BlockedOverlayPhases { get; set; } = Array.Empty<OverlayPhase>();

    /// <summary>允许的执行阶段；为空表示不限制。</summary>
    public ExecutionPhase[] AllowedExecutionPhases { get; set; } = Array.Empty<ExecutionPhase>();

    /// <summary>
    /// 判断指定快照是否满足当前运行条件。
    /// </summary>
    /// <param name="snapshot">当前项目状态快照。</param>
    /// <returns>满足条件返回 true。</returns>
    public bool Evaluate(ProjectStateSnapshot snapshot)
    {
        // 规则：任一“允许集合”不匹配即失败；任一“禁止集合”命中即失败。
        // 允许集合为空表示“该维度不限制”。
        if (!Matches(AllowedAppPhases, snapshot.AppPhase))
        {
            return false;
        }

        if (!Matches(AllowedSessionPhases, snapshot.SessionPhase))
        {
            return false;
        }

        if (!Matches(AllowedOverlayPhases, snapshot.OverlayPhase))
        {
            return false;
        }

        if (Contains(BlockedOverlayPhases, snapshot.OverlayPhase))
        {
            return false;
        }

        if (!Matches(AllowedExecutionPhases, snapshot.ExecutionPhase))
        {
            return false;
        }

        return true;
    }

    private static bool Matches<T>(T[] allowedValues, T currentValue) where T : struct, Enum
    {
        // allowed 为空时代表 wildcard（任意值均通过）。
        return allowedValues.Length == 0 || Contains(allowedValues, currentValue);
    }

    private static bool Contains<T>(T[] values, T currentValue) where T : struct, Enum
    {
        for (var i = 0; i < values.Length; i++)
        {
            if (EqualityComparer<T>.Default.Equals(values[i], currentValue))
            {
                return true;
            }
        }

        return false;
    }
}
