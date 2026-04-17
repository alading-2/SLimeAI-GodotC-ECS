using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// 系统运行时管理器。
/// <para>职责：实例化系统、管理启停、按项目状态重算运行资格。</para>
/// </summary>
public partial class SystemManager : Node
{
    private static readonly Log _log = new(nameof(SystemManager));
    // systemId -> 运行时条目，统一维护系统实例和当前运行状态。
    private readonly Dictionary<string, ManagedSystemEntry> _entries = new(StringComparer.Ordinal);
    // 生命周期域 -> Host 节点。避免业务系统直接散挂在 SystemManager 根下。
    private readonly Dictionary<SystemLifetime, Node> _hosts = new();

    /// <summary>全局访问入口。</summary>
    public static SystemManager? Instance { get; private set; }

    /// <summary>项目状态服务。</summary>
    public ProjectStateService ProjectState { get; } = new();

    public override void _EnterTree()
    {
        // 约定同一时刻只存在一个活动 SystemManager。
        Instance = this;
    }

    public override void _ExitTree()
    {
        ProjectState.StateChanged -= OnProjectStateChanged;

        // 在移除管理器前，给所有系统一次统一反注册机会。
        foreach (var entry in _entries.Values)
        {
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
        EnsureHosts();
        ProjectState.StateChanged -= OnProjectStateChanged;
        ProjectState.StateChanged += OnProjectStateChanged;
    }

    /// <summary>
    /// 启动当前已注册的全部系统描述符。
    /// </summary>
    public void BootstrapRegisteredSystems()
    {
        // 按注册表快照依次接管。依赖由 EnsureSystem 内部递归展开。
        foreach (var descriptor in SystemRegistry.GetDescriptors())
        {
            EnsureSystem(descriptor);
        }
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
            // Node 类型系统统一挂到生命周期 Host 下，便于层级隔离和批量观测。
            nodeInstance.Name = descriptor.SystemId;
            GetHost(descriptor.Lifetime).AddChild(nodeInstance);
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
        };

        _entries.Add(descriptor.SystemId, entry);
        // 新系统接管后立即按当前状态应用一次运行资格，避免“创建后短暂误运行”。
        ApplyEntryState(entry, notifyTransition: true);
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
    /// <typeparam name="T">目标类型。</typeparam>
    /// <returns>找到则返回实例。</returns>
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
            // 顺序保证：系统在 OnProjectStateChanged 内读到的是“新的状态快照”。
            entry.IsStateAllowed = entry.Descriptor.RunCondition.Evaluate(args.Current);
            entry.Runtime?.OnProjectStateChanged(args);
            ApplyEntryState(entry, notifyTransition: true);
        }
    }

    private void ApplyEntryState(ManagedSystemEntry entry, bool notifyTransition)
    {
        // 系统是否真正运行 = 人工启用开关 && 状态条件允许。
        var shouldRun = entry.IsEnabled && entry.IsStateAllowed;

        if (entry.NodeInstance != null)
        {
            // Node 系统通过 ProcessMode 控制处理循环。
            // 注意：这不替代事件订阅治理，事件类系统仍应在 Runtime 回调里做订阅/退订。
            entry.NodeInstance.ProcessMode = shouldRun
                ? ProcessModeEnum.Inherit
                : ProcessModeEnum.Disabled;
        }

        if (entry.Runtime == null)
        {
            return;
        }

        if (shouldRun)
        {
            entry.Runtime.OnSystemEnabled(ProjectState.Snapshot);
            return;
        }

        if (notifyTransition)
        {
            // 只在需要通知转换时触发 Disable，避免重复禁用产生副作用。
            entry.Runtime.OnSystemDisabled(ProjectState.Snapshot);
        }
    }

    private void EnsureHosts()
    {
        // 预建标准生命周期域 Host，保持层级结构稳定。
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

        // Host 本身不承载业务逻辑，仅作为系统容器节点。
        node = new Node { Name = name };
        AddChild(node);
        return node;
    }
}
