using System.Runtime.CompilerServices;

/// <summary>
/// 项目状态桥接系统。
/// <para>负责把全局游戏流程事件统一映射到 ProjectStateService。</para>
/// </summary>
public sealed class ProjectStateBridge : ISystemRuntime
{
    private ProjectStateService? _projectState;
    private bool _eventsBound;

    [ModuleInitializer]
    internal static void Initialize()
    {
        SystemRegistry.Register(new SystemDescriptor(nameof(ProjectStateBridge), SystemKind.PureService, SystemLifetime.Persistent)
        {
            Factory = static () => new ProjectStateBridge()
        });
    }

    /// <inheritdoc />
    public void OnSystemRegistered(SystemRegistrationContext context)
    {
        _projectState = context.ProjectState;
    }

    /// <inheritdoc />
    public void OnSystemEnabled(ProjectStateSnapshot snapshot)
    {
        BindRuntimeEvents();
        if (_projectState != null && _projectState.AppPhase == AppPhase.Boot)
        {
            _projectState.EnterFrontEnd();
        }
    }

    /// <inheritdoc />
    public void OnSystemDisabled(ProjectStateSnapshot snapshot)
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
        GlobalEventBus.Global.On<GameEventType.Global.GamePauseEventData>(GameEventType.Global.GamePause, OnGamePause);
        GlobalEventBus.Global.On<GameEventType.Global.GameResumeEventData>(GameEventType.Global.GameResume, OnGameResume);
        GlobalEventBus.Global.On<GameEventType.Global.GameOverEventData>(GameEventType.Global.GameOver, OnGameOver);
        _eventsBound = true;
    }

    private void UnbindRuntimeEvents()
    {
        if (!_eventsBound)
        {
            return;
        }

        GlobalEventBus.Global.Off(GameEventType.Global.GameStart, OnGameStart);
        GlobalEventBus.Global.Off<GameEventType.Global.GamePauseEventData>(GameEventType.Global.GamePause, OnGamePause);
        GlobalEventBus.Global.Off<GameEventType.Global.GameResumeEventData>(GameEventType.Global.GameResume, OnGameResume);
        GlobalEventBus.Global.Off<GameEventType.Global.GameOverEventData>(GameEventType.Global.GameOver, OnGameOver);
        _eventsBound = false;
    }

    private void OnGameStart()
    {
        _projectState?.BeginGameplaySession();
    }

    private void OnGamePause(GameEventType.Global.GamePauseEventData data)
    {
        _projectState?.OpenPauseMenu();
    }

    private void OnGameResume(GameEventType.Global.GameResumeEventData data)
    {
        _projectState?.ClosePauseMenu();
    }

    private void OnGameOver(GameEventType.Global.GameOverEventData data)
    {
        _projectState?.EndSession();
    }
}
