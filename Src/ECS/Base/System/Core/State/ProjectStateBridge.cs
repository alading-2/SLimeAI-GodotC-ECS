using System.Collections.Generic;
using System.Runtime.CompilerServices;

/// <summary>
/// 项目状态桥接系统。
/// <para>负责把全局游戏流程事件统一映射到 ProjectStateService。</para>
/// </summary>
public sealed class ProjectStateBridge : ISystem
{
    private ProjectStateService? _projectState;
    private bool _eventsBound;

    [ModuleInitializer]
    internal static void Initialize()
    {
        SystemRegistry.Register(nameof(ProjectStateBridge),
            static () => new ProjectStateBridge());
    }

    /// <inheritdoc />
    public void OnRegistered(SystemRegistrationContext context)
    {
        _projectState = context.ProjectState;
    }

    /// <inheritdoc />
    public void OnStarted(ProjectStateSnapshot snapshot)
    {
        BindRuntimeEvents();
        if (_projectState != null && _projectState.FlowState == GameFlowState.Boot)
        {
            _projectState.EnterFrontEnd();
        }
    }

    /// <inheritdoc />
    public void OnStopped(ProjectStateSnapshot snapshot)
    {
        UnbindRuntimeEvents();
    }

    private void BindRuntimeEvents()
    {
        if (_eventsBound)
        {
            return;
        }

        GlobalEventBus.Global.On(GameEventType.Global.GameStart, OnGameStart);
        GlobalEventBus.Global.On<GameEventType.Global.GamePause>(GameEventType.Global.GamePause, OnGamePause);
        GlobalEventBus.Global.On<GameEventType.Global.GameResume>(GameEventType.Global.GameResume,
            OnGameResume);
        GlobalEventBus.Global.On<GameEventType.Global.GameOver>(GameEventType.Global.GameOver, OnGameOver);
        _eventsBound = true;
    }

    private void UnbindRuntimeEvents()
    {
        if (!_eventsBound)
        {
            return;
        }

        GlobalEventBus.Global.Off(GameEventType.Global.GameStart, OnGameStart);
        GlobalEventBus.Global.Off<GameEventType.Global.GamePause>(GameEventType.Global.GamePause, OnGamePause);
        GlobalEventBus.Global.Off<GameEventType.Global.GameResume>(GameEventType.Global.GameResume,
            OnGameResume);
        GlobalEventBus.Global.Off<GameEventType.Global.GameOver>(GameEventType.Global.GameOver, OnGameOver);
        _eventsBound = false;
    }

    private void OnGameStart()
    {
        _projectState?.BeginGameplaySession();
    }

    private void OnGamePause(GameEventType.Global.GamePause data)
    {
        _projectState?.OpenPauseMenu();
    }

    private void OnGameResume(GameEventType.Global.GameResume data)
    {
        _projectState?.ClosePauseMenu();
    }

    private void OnGameOver(GameEventType.Global.GameOver data)
    {
        _projectState?.EndSession();
    }

    public SystemRuntimeInfo GetSystemRuntimeInfo()
    {
        return new SystemRuntimeInfo
        {
            SystemId = nameof(ProjectStateBridge),
            CustomStats = new List<SystemStat>()
        };
    }
}
