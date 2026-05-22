using System;
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
    private readonly EventSubscriptionCollector _runtimeSubscriptions = new();

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

        _runtimeSubscriptions.Clear();
        _runtimeSubscriptions.Subscribe<GlobalEvents.GameStart>(WorldEvents.World, OnGameStart);
        _runtimeSubscriptions.Subscribe<GlobalEvents.GamePause>(WorldEvents.World, OnGamePause);
        _runtimeSubscriptions.Subscribe<GlobalEvents.GameResume>(WorldEvents.World, OnGameResume);
        _runtimeSubscriptions.Subscribe<GlobalEvents.GameOver>(WorldEvents.World, OnGameOver);
        _eventsBound = true;
    }

    private void UnbindRuntimeEvents()
    {
        if (!_eventsBound)
        {
            return;
        }

        _runtimeSubscriptions.Clear();
        _eventsBound = false;
    }

    private void OnGameStart(GlobalEvents.GameStart data)
    {
        _projectState?.BeginGameplaySession();
    }

    private void OnGamePause(GlobalEvents.GamePause data)
    {
        _projectState?.OpenPauseMenu();
    }

    private void OnGameResume(GlobalEvents.GameResume data)
    {
        _projectState?.ClosePauseMenu();
    }

    private void OnGameOver(GlobalEvents.GameOver data)
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
