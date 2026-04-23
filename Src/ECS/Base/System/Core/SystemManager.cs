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
    private readonly Dictionary<SystemGroup, Node> _hosts = new();
    private bool _isInitialized;
    private bool _isBootstrapped; // 启动完成

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

    // ─────────────────────────────────────────────────────────────
    // 生命周期总览：
    //   _EnterTree  → 挂树、设单例、初始化 ParentManager
    //   _Ready      → 触发 Initialize + BootstrapRegisteredSystems
    //   _ExitTree   → 反注册全部系统、清空条目、置空单例
    //
    //   Initialize              → 创建 Host 容器、订阅 ProjectState 变更
    //   BootstrapRegisteredSystems → 遍历 Registry 中的描述符，逐个 EnsureSystem
    //   EnsureSystem             → 递归展开依赖 → Factory 创建实例 → 挂树/注册 → ApplyEntryState
    //   EnableSystem/DisableSystem → 修改 IsEnabled → ApplyEntryState
    //   OnProjectStateChanged    → 重算 IsStateAllowed → 通知系统 → ApplyEntryState
    //   ApplyEntryState          → 统一裁决 shouldRun = IsEnabled && IsStateAllowed
    //                              → 切 ProcessMode / 调用 OnSystemEnabled/OnSystemDisabled
    // ─────────────────────────────────────────────────────────────

    public override void _Ready()
    {
        EnsureBootstrapped();
    }

    public override void _ExitTree()
    {
        // 取消订阅，避免后续状态变更触发回调。
        ProjectState.StateChanged -= OnProjectStateChanged;

        // 在移除管理器前，先关闭正在运行的系统，再给所有系统一次统一反注册机会。
        // 顺序：先 Disable（让系统清理运行态），再 UnRegistered（释放资源）。
        foreach (var entry in _entries.Values)
        {
            if (entry.IsRunning)
            {
                entry.Lifecycle?.OnDisabled(ProjectState.Snapshot);
            }

            entry.Lifecycle?.OnUnRegistered();
        }

        _entries.Clear();
        _hosts.Clear();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// 一次性完成初始化与启动，供 _Ready 调用。
    /// </summary>
    private void EnsureBootstrapped()
    {
        Initialize();
        BootstrapRegisteredSystems();
    }

    /// <summary>
    /// 初始化 Host 容器并接入项目状态监听。
    /// <para>必须在 BootstrapRegisteredSystems 之前调用，通常由 _Ready 自动触发。</para>
    /// </summary>
    public void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        // 为每个生命周期域创建 Host 节点，作为该域系统的统一父容器。
        EnsureHosts();
        // 先减后加，防止重复订阅。
        ProjectState.StateChanged -= OnProjectStateChanged;
        ProjectState.StateChanged += OnProjectStateChanged;
        _isInitialized = true;
    }

    /// <summary>
    /// 启动当前已注册的全部系统描述符。
    /// <para>流程：</para>
    /// <para>1. 初始化 SystemConfigService 和 SystemPresetService</para>
    /// <para>2. 根据 Preset 和 Config 计算应该加载的系统列表</para>
    /// <para>3. 按 Priority 排序后逐个调用 EnsureSystem</para>
    /// <para>4. 完成后发射 BootstrapCompleted 信号</para>
    /// </summary>
    public void BootstrapRegisteredSystems()
    {
        if (_isBootstrapped)
        {
            return;
        }

        // 1. 初始化配置服务
        SystemConfigService.Initialize();
        SystemPresetService.Initialize();

        // 2. 计算应该加载的系统列表
        var enabledSystemIds = SystemPresetService.CalculateEnabledSystems();
        _log.Info($"根据预设计算，应加载 {enabledSystemIds.Count} 个系统");

        // 3. 收集对应的描述符并按 Priority 排序
        var descriptorsToLoad = new List<(SystemDescriptor descriptor, SystemConfig config, int priority)>();
        foreach (var systemId in enabledSystemIds)
        {
            var descriptor = SystemRegistry.GetDescriptor(systemId);
            if (descriptor == null)
            {
                _log.Warn($"系统 {systemId} 未注册，跳过加载");
                continue;
            }

            var config = SystemConfigService.GetConfig(systemId);
            if (config == null)
            {
                _log.Warn($"系统 {systemId} 缺少配置文件，跳过加载");
                continue;
            }

            descriptorsToLoad.Add((descriptor, config, config.Priority));
        }

        // 按 Priority 排序（数值越小越优先）
        descriptorsToLoad.Sort((a, b) => a.priority.CompareTo(b.priority));

        // 4. 逐个加载系统
        foreach (var (descriptor, config, _) in descriptorsToLoad)
        {
            EnsureSystem(descriptor, config);
        }

        _isBootstrapped = true;
        EmitSignal(SignalName.BootstrapCompleted);
        _log.Info("SystemManager 启动完成");
    }

    /// <summary>
    /// 确保指定系统已经被创建并纳入管理。
    /// <para>流程：1) 检查是否已托管 → 2) 递归展开依赖 → 3) Factory 创建实例 →</para>
    /// <para>       4) Node 类型挂到分组 Host 下 → 5) 调用 OnSystemRegistered →</para>
    /// <para>       6) 创建 ManagedSystemEntry → 7) ApplyEntryState 裁决初始运行状态</para>
    /// </summary>
    /// <param name="descriptor">系统描述符。</param>
    /// <param name="config">系统配置。</param>
    public void EnsureSystem(SystemDescriptor descriptor, SystemConfig config)
    {
        // 已托管则直接复用，避免重复创建。
        if (_entries.ContainsKey(descriptor.SystemId))
        {
            return;
        }

        // 先确保依赖系统可用，再创建当前系统。
        foreach (var dependency in config.Dependencies)
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

            var dependencyConfig = SystemConfigService.GetConfig(dependency);
            if (dependencyConfig == null)
            {
                _log.Error($"系统 {descriptor.SystemId} 缺少依赖配置: {dependency}");
                return;
            }

            EnsureSystem(dependencyDescriptor, dependencyConfig);
        }

        // 通过 Factory 创建系统实例。
        var instance = descriptor.Factory.Invoke();
        if (instance == null)
        {
            _log.Error($"系统 {descriptor.SystemId} Factory 返回 null");
            return;
        }

        // Node 类型系统需要挂到场景树：以 SystemId 命名，挂到对应分组 Host 下。
        var nodeInstance = instance as Node;
        if (nodeInstance != null)
        {
            nodeInstance.Name = descriptor.SystemId;
            // 按 Groups 最低位确定挂载路径
            var mountGroup = GetLowestBitGroup(config.Groups);
            var host = GetHost(mountGroup);
            host.AddChild(nodeInstance);
        }

        // 通知系统已被注册，传入描述符和项目状态服务引用。
        var lifecycle = instance as ISystemLifecycle;
        lifecycle?.OnRegistered(new SystemRegistrationContext(descriptor, ProjectState));

        // 创建运行时条目：初始启用状态取配置默认值，状态门禁用当前快照评估。
        var runCondition = CreateRunCondition(config);
        var entry = new ManagedSystemEntry
        {
            Descriptor = descriptor,
            Config = config,
            Instance = instance,
            NodeInstance = nodeInstance,
            Lifecycle = lifecycle,
            IsEnabled = config.DefaultEnabled, // 人工开关：配置默认值
            IsStateAllowed = runCondition.Evaluate(ProjectState.Snapshot), // 状态门禁：Phase 条件裁决
            IsRunning = false
        };

        _entries.Add(descriptor.SystemId, entry);
        ApplyEntryState(entry, notifyTransition: true); // 根据初始状态立即裁决是否运行
    }

    /// <summary>
    /// 启用指定系统（人工开关 = true）。
    /// <para>注意：启用不等于立即运行，还需 IsStateAllowed == true（Phase 条件通过）。</para>
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
    /// 禁用指定系统（人工开关 = false）。
    /// <para>禁用后无论 Phase 条件如何，系统都不会运行。</para>
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
    /// <para>按类型在所有条目中查找第一个匹配的实例，未找到返回 null。</para>
    /// </summary>
    /// <typeparam name="T">要查找的系统类型。</typeparam>
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

    /// <summary>
    /// 项目状态变更回调。
    /// <para>对每个系统：1) 重算 IsStateAllowed（Phase 条件裁决）→ 2) 通知系统 → 3) 统一应用启停。</para>
    /// </summary>
    private void OnProjectStateChanged(object? sender, ProjectStateChangedEventArgs args)
    {
        foreach (var entry in _entries.Values)
        {
            // 先重算状态门禁，再通知系统，再统一应用启停。
            var runCondition = CreateRunCondition(entry.Config);
            entry.IsStateAllowed = runCondition.Evaluate(args.Current);

            // 只通知实现了 IProjectStateAwareSystem 的系统
            if (entry.Instance is IProjectStateAwareSystem stateAware)
            {
                stateAware.OnProjectStateChanged(args);
            }

            ApplyEntryState(entry, notifyTransition: true);
        }
    }

    /// <summary>
    /// 统一裁决并应用系统运行状态。
    /// <para>shouldRun = IsEnabled（人工开关）&& IsStateAllowed（Phase 条件门禁）。</para>
    /// <para>对 Node 系统：切 ProcessMode（Inherit/Disabled）。</para>
    /// <para>对 ISystemLifecycle 系统：在状态切换时调用 OnEnabled/OnDisabled。</para>
    /// <para>notifyTransition=false 时仅更新 IsRunning 标记，不触发钩子（用于初始化静默设置）。</para>
    /// </summary>
    private void ApplyEntryState(ManagedSystemEntry entry, bool notifyTransition)
    {
        var shouldRun = entry.IsEnabled && entry.IsStateAllowed; // 人工开关 && 状态条件

        // Node 类型系统：通过 ProcessMode 控制 _Process/_PhysicsProcess 是否执行。
        if (entry.NodeInstance != null)
        {
            entry.NodeInstance.ProcessMode = shouldRun
                ? ProcessModeEnum.Inherit
                : ProcessModeEnum.Disabled;
        }

        // 非 ISystemLifecycle 系统（纯 Node）：直接更新标记即可。
        if (entry.Lifecycle == null)
        {
            entry.IsRunning = shouldRun;
            return;
        }

        // 不通知时只更新标记，不触发生命周期钩子。
        if (!notifyTransition)
        {
            entry.IsRunning = shouldRun;
            return;
        }

        // 状态未变化时跳过，避免重复调用 Enable/Disable 钩子。
        if (shouldRun == entry.IsRunning)
        {
            return;
        }

        if (shouldRun)
        {
            entry.Lifecycle.OnEnabled(ProjectState.Snapshot);
            entry.IsRunning = true;
            return;
        }

        entry.Lifecycle.OnDisabled(ProjectState.Snapshot);
        entry.IsRunning = false;
    }

    /// <summary>
    /// 为所有分组创建 Host 节点。
    /// <para>Host 是 SystemManager 的直接子节点，按分组分组，避免业务系统散挂在根下。</para>
    /// </summary>
    private void EnsureHosts()
    {
        foreach (SystemGroup group in Enum.GetValues(typeof(SystemGroup)))
        {
            // 跳过 None 和非单一位的组合值
            if (group == SystemGroup.None || !IsSingleBit((ulong)group))
            {
                continue;
            }

            _hosts[group] = EnsureHost(group);
        }
    }

    /// <summary>
    /// 判断一个 ulong 值是否只有一个位被设置。
    /// </summary>
    private static bool IsSingleBit(ulong value)
    {
        return value != 0 && (value & (value - 1)) == 0;
    }

    /// <summary>
    /// 获取指定分组的 Host 节点，不存在则创建。
    /// </summary>
    private Node GetHost(SystemGroup group)
    {
        if (_hosts.TryGetValue(group, out var host))
        {
            return host;
        }

        host = EnsureHost(group);
        _hosts[group] = host;
        return host;
    }

    /// <summary>
    /// 确保指定分组的 Host 节点存在。
    /// <para>Host 节点以 "{Group}Host" 命名，不承载业务逻辑，仅作系统容器。</para>
    /// </summary>
    private Node EnsureHost(SystemGroup group)
    {
        var name = $"{group}Host";
        var node = GetNodeOrNull<Node>(name);
        if (node != null)
        {
            return node;
        }

        node = new Node { Name = name }; // Host 本身不承载业务逻辑，仅作系统容器
        AddChild(node);
        return node;
    }

    /// <summary>
    /// 获取 SystemGroup 的最低位（用于确定挂载路径）。
    /// </summary>
    private static SystemGroup GetLowestBitGroup(SystemGroup groups)
    {
        if (groups == SystemGroup.None)
        {
            return SystemGroup.Else;
        }

        // 按位从低到高查找第一个设置的位
        var value = (ulong)groups;
        for (var i = 0; i < 64; i++)
        {
            var bit = 1UL << i;
            if ((value & bit) != 0)
            {
                return (SystemGroup)bit;
            }
        }

        return SystemGroup.Else;
    }

    /// <summary>
    /// 从 SystemConfig 创建 SystemRunCondition。
    /// </summary>
    private static SystemRunCondition CreateRunCondition(SystemConfig config)
    {
        return new SystemRunCondition
        {
            AllowedAppPhases = config.AllowedAppPhases,
            AllowedSessionPhases = config.AllowedSessionPhases,
            AllowedOverlayPhases = config.AllowedOverlayPhases,
            BlockedOverlayPhases = config.BlockedOverlayPhases,
            AllowedExecutionPhases = config.AllowedExecutionPhases
        };
    }
}