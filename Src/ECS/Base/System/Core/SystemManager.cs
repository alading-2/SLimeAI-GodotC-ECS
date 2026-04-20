using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// 系统运行时管理器。
/// <para>作为项目唯一 autoload 入口，负责初始化 ParentManager、实例化系统、管理启停并按项目状态重算运行资格。</para>
/// </summary>
public partial class SystemManager : Node
{
    private static readonly Log _log = new(nameof(SystemManager));

    // systemId -> 运行时条目，统一维护系统实例和当前运行状态。
    private readonly Dictionary<string, ManagedSystemEntry> _entries = new(StringComparer.Ordinal);
    // 生命周期域 -> Host 节点。避免业务系统直接散挂在 SystemManager 根下。
    private readonly Dictionary<SystemLifetime, Node> _hosts = new();
    private bool _isInitialized;
    private bool _isBootstrapped;

    /// <summary>全局访问入口。</summary>
    public static SystemManager? Instance { get; private set; }

    /// <summary>项目状态服务。</summary>
    public ProjectStateService ProjectState { get; } = new();

    /// <summary>系统启动是否已完成。</summary>
    public bool IsBootstrapped => _isBootstrapped;

    /// <summary>启动完成信号，供测试场景或延迟依赖链等待。</summary>
    [Signal]
    public delegate void BootstrapCompletedEventHandler();

    public override void _EnterTree()
    {
        // 约定同一时刻只存在一个活动 SystemManager。
        Instance = this;
        ParentManager.Init(GetTree().Root); // 系统树与对象池/UI/Entity 父节点统一共用 Root
    }

    public override void _Ready()
    {
        EnsureBootstrapped();
    }

    public override void _ExitTree()
    {
        ProjectState.StateChanged -= OnProjectStateChanged;

        // 在移除管理器前，先关闭正在运行的系统，再给所有系统一次统一反注册机会。
        foreach (var entry in _entries.Values)
        {
            if (entry.IsRunning)
            {
                entry.Runtime?.OnSystemDisabled(ProjectState.Snapshot);
            }

            entry.Runtime?.OnSystemUnregistered();
        }

        _entries.Clear();
        _hosts.Clear();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// 初始化 Host 容器并接入项目状态监听。
    /// </summary>
    public void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        EnsureHosts();
        ProjectState.StateChanged -= OnProjectStateChanged;
        ProjectState.StateChanged += OnProjectStateChanged;
        _isInitialized = true;
    }

    /// <summary>
    /// 启动当前已注册的全部系统描述符。
    /// </summary>
    public void BootstrapRegisteredSystems()
    {
        if (_isBootstrapped)
        {
            return;
        }

        // 按注册表快照依次接管。依赖由 EnsureSystem 内部递归展开。
        foreach (var descriptor in SystemRegistry.GetDescriptorValues())
        {
            EnsureSystem(descriptor);
        }

        _isBootstrapped = true;
        EmitSignal(SignalName.BootstrapCompleted);
        _log.Info("SystemManager 启动完成");
    }

    /// <summary>
    /// 确保指定系统已经被创建并纳入管理。
    /// </summary>
    /// <param name="descriptor">系统描述符。</param>
    public void EnsureSystem(SystemDescriptor descriptor)
    {
        // 已托管则直接复用，避免重复创建。
        if (_entries.ContainsKey(descriptor.SystemId))
        {
            return;
        }

        if (descriptor.Factory == null)
        {
            _log.Error($"系统 {descriptor.SystemId} 缺少 Factory，无法创建实例");
            return;
        }

        // 先确保依赖系统可用，再创建当前系统。
        foreach (var dependency in descriptor.Dependencies)
        {
            if (_entries.ContainsKey(dependency))
            {
                continue;
            }

            var dependencyDescriptor = SystemRegistry.GetDescriptor(dependency);
            if (dependencyDescriptor == null)
            {
                _log.Error($"系统 {descriptor.SystemId} 缺少依赖描述符: {dependency}");
                return;
            }

            EnsureSystem(dependencyDescriptor);
        }

        var instance = descriptor.Factory.Invoke();
        if (instance == null)
        {
            _log.Error($"系统 {descriptor.SystemId} Factory 返回 null");
            return;
        }

        var nodeInstance = instance as Node;
        if (nodeInstance != null)
        {
            // Node 类型系统统一挂到生命周期 Host 下，ParentPath 相对 Host 生效。
            nodeInstance.Name = descriptor.SystemId;
            var parent = ParentManager.EnsurePath(GetHost(descriptor.Lifetime), descriptor.ParentPath);
            parent.AddChild(nodeInstance);
        }

        var runtime = instance as ISystemRuntime;
        runtime?.OnSystemRegistered(new SystemRegistrationContext(descriptor, ProjectState));

        var entry = new ManagedSystemEntry
        {
            Descriptor = descriptor,
            Instance = instance,
            NodeInstance = nodeInstance,
            Runtime = runtime,
            IsEnabled = descriptor.DefaultEnabled,
            IsStateAllowed = descriptor.RunCondition.Evaluate(ProjectState.Snapshot),
            IsRunning = false
        };

        _entries.Add(descriptor.SystemId, entry);
        ApplyEntryState(entry, notifyTransition: true); // 避免创建后短暂误运行
    }

    /// <summary>
    /// 启用指定系统。
    /// </summary>
    /// <param name="systemId">系统 Id。</param>
    public void EnableSystem(string systemId)
    {
        if (!_entries.TryGetValue(systemId, out var entry))
        {
            return;
        }

        if (entry.IsEnabled)
        {
            return;
        }

        entry.IsEnabled = true;
        ApplyEntryState(entry, notifyTransition: true);
    }

    /// <summary>
    /// 禁用指定系统。
    /// </summary>
    /// <param name="systemId">系统 Id。</param>
    public void DisableSystem(string systemId)
    {
        if (!_entries.TryGetValue(systemId, out var entry))
        {
            return;
        }

        if (!entry.IsEnabled)
        {
            return;
        }

        entry.IsEnabled = false;
        ApplyEntryState(entry, notifyTransition: true);
    }

    /// <summary>
    /// 获取一个已托管实例。
    /// </summary>
    public T? Resolve<T>() where T : class
    {
        foreach (var entry in _entries.Values)
        {
            if (entry.Instance is T typed)
            {
                return typed;
            }
        }

        return null;
    }

    private void OnProjectStateChanged(object? sender, ProjectStateChangedEventArgs args)
    {
        foreach (var entry in _entries.Values)
        {
            // 先重算状态门禁，再通知系统，再统一应用启停。
            entry.IsStateAllowed = entry.Descriptor.RunCondition.Evaluate(args.Current);
            entry.Runtime?.OnProjectStateChanged(args);
            ApplyEntryState(entry, notifyTransition: true);
        }
    }

    private void ApplyEntryState(ManagedSystemEntry entry, bool notifyTransition)
    {
        var shouldRun = entry.IsEnabled && entry.IsStateAllowed; // 人工开关 && 状态条件

        if (entry.NodeInstance != null)
        {
            entry.NodeInstance.ProcessMode = shouldRun
                ? ProcessModeEnum.Inherit
                : ProcessModeEnum.Disabled;
        }

        if (entry.Runtime == null)
        {
            entry.IsRunning = shouldRun;
            return;
        }

        if (!notifyTransition)
        {
            entry.IsRunning = shouldRun;
            return;
        }

        if (shouldRun == entry.IsRunning)
        {
            return;
        }

        if (shouldRun)
        {
            entry.Runtime.OnSystemEnabled(ProjectState.Snapshot);
            entry.IsRunning = true;
            return;
        }

        entry.Runtime.OnSystemDisabled(ProjectState.Snapshot);
        entry.IsRunning = false;
    }

    private void EnsureHosts()
    {
        _hosts[SystemLifetime.Persistent] = EnsureHost(SystemLifetime.Persistent);
        _hosts[SystemLifetime.Gameplay] = EnsureHost(SystemLifetime.Gameplay);
        _hosts[SystemLifetime.Overlay] = EnsureHost(SystemLifetime.Overlay);
        _hosts[SystemLifetime.Debug] = EnsureHost(SystemLifetime.Debug);
        _hosts[SystemLifetime.Test] = EnsureHost(SystemLifetime.Test);
    }

    private Node GetHost(SystemLifetime lifetime)
    {
        if (_hosts.TryGetValue(lifetime, out var host))
        {
            return host;
        }

        host = EnsureHost(lifetime);
        _hosts[lifetime] = host;
        return host;
    }

    private Node EnsureHost(SystemLifetime lifetime)
    {
        var name = $"{lifetime}Host";
        var node = GetNodeOrNull<Node>(name);
        if (node != null)
        {
            return node;
        }

        node = new Node { Name = name }; // Host 本身不承载业务逻辑，仅作系统容器
        AddChild(node);
        return node;
    }

    private void EnsureBootstrapped()
    {
        Initialize();
        BootstrapRegisteredSystems();
    }
}
