using Godot;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

/// <summary>
/// 暂停菜单系统。
/// <para>统一处理局内暂停输入、暂停菜单显隐和恢复操作。</para>
/// </summary>
public partial class PauseMenuSystem : CanvasLayer, ISystem
{
    private static readonly Log _log = new(nameof(PauseMenuSystem));

    private ProjectStateService? _projectState;
    private Control _root = null!;
    private Button _resumeButton = null!;
    private Label _hintLabel = null!;

    [ModuleInitializer]
    internal static void Initialize()
    {
        SystemRegistry.Register(nameof(PauseMenuSystem),
            static () => ResourceManagement.Load<PackedScene>(nameof(PauseMenuSystem), ResourceCategory.System)
                .Instantiate());
    }

    public override void _EnterTree()
    {
        Layer = 80;
    }

    public override void _Ready()
    {
        _root = GetNode<Control>("Root");
        _resumeButton = GetNode<Button>("Root/Panel/Body/Margin/Content/ResumeButton");
        _hintLabel = GetNode<Label>("Root/Panel/Body/Margin/Content/HintLabel");

        _resumeButton.Pressed += OnResumePressed;
        _hintLabel.Text = "按 Esc / Start 继续";
        SetMenuVisible(false);
        _log.Info("PauseMenuSystem 初始化完成");
    }

    public override void _ExitTree()
    {
        if (_resumeButton != null)
        {
            _resumeButton.Pressed -= OnResumePressed;
        }
    }

    public override void _Process(double delta)
    {
        if (!InputManager.IsPause())
        {
            return;
        }

        TogglePauseMenu();
    }

    /// <inheritdoc />
    public void OnRegistered(SystemRegistrationContext context)
    {
        _projectState = context.ProjectState;
    }

    /// <inheritdoc />
    public void OnStarted(ProjectStateSnapshot snapshot)
    {
        UpdateMenuState(snapshot);
    }

    /// <inheritdoc />
    public void OnStopped(ProjectStateSnapshot snapshot)
    {
        SetMenuVisible(false);
    }

    /// <inheritdoc />
    public void OnProjectStateChanged(GameEventType.Global.ProjectStateTransitionEventData data)
    {
        UpdateMenuState(data.Current);
    }

    private void TogglePauseMenu()
    {
        var projectState = ResolveProjectState();
        if (projectState == null)
        {
            return;
        }

        if ((projectState.Overlays & OverlayFlags.PauseMenu) != 0)
        {
            projectState.ClosePauseMenu();
            GlobalEventBus.TriggerGameResume();
            return;
        }

        projectState.OpenPauseMenu();
        GlobalEventBus.TriggerGamePause();
    }

    private void OnResumePressed()
    {
        var projectState = ResolveProjectState();
        if (projectState == null)
        {
            return;
        }

        projectState.ClosePauseMenu();
        GlobalEventBus.TriggerGameResume();
    }

    private void UpdateMenuState(ProjectStateSnapshot snapshot)
    {
        SetMenuVisible((snapshot.Overlays & OverlayFlags.PauseMenu) != 0);
    }

    private void SetMenuVisible(bool visible)
    {
        if (_root == null)
        {
            return;
        }

        _root.Visible = visible;
        if (visible)
        {
            _resumeButton.GrabFocus();
        }
    }

    private ProjectStateService? ResolveProjectState()
    {
        return _projectState ?? SystemManager.Instance?.ProjectState;
    }

    public SystemRuntimeInfo GetSystemRuntimeInfo()
    {
        return new SystemRuntimeInfo
        {
            SystemId = nameof(PauseMenuSystem),
            CustomStats = new List<SystemStat>()
        };
    }
}
