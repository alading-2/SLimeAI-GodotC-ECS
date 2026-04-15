using Godot;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

/// <summary>
/// 运行时测试系统入口。
/// <para>
/// 负责在 Debug 环境下挂载一个可交互的测试面板，统一承接“选中实体”“切换测试模块”“刷新测试数据”等操作。
/// </para>
/// <para>
/// 这个系统本身不承载具体测试逻辑，具体行为由各个 <see cref="TestModuleBase"/> 子模块实现。
/// </para>
/// </summary>
public partial class TestSystem : CanvasLayer
{
    /// <summary>测试系统日志器，用于记录初始化与关键调试操作。</summary>
    private static readonly Log _log = new(nameof(TestSystem));

    /// <summary>测试系统单例，方便其它调试工具直接访问当前测试面板。</summary>
    public static TestSystem? Instance { get; private set; }

    /// <summary>当前被测试面板选中的实体；属性面板、技能面板等都会围绕它刷新。</summary>
    public IEntity? SelectedEntity => _selectionContext?.SelectedEntity;

    /// <summary>TestSystem 局部事件总线，仅用于测试面板内部通信。</summary>
    public EventBus Events { get; } = new();

    /// <summary>当前已注册的测试模块列表，顺序就是场景挂载顺序。</summary>
    private readonly List<ITestModule> _modules = new();

    /// <summary>模块 Id 到模块实例的快速映射。</summary>
    private readonly Dictionary<string, ITestModule> _modulesById = new(StringComparer.Ordinal);

    /// <summary>所有测试模块 Id 的列表，用于默认模块与顺序定位。</summary>
    private readonly List<string> _moduleIds = new();

    /// <summary>当前真正处于前台的测试模块。</summary>
    private ITestModule? _currentModule;

    /// <summary>测试面板根节点，作为所有 UI 的父容器。</summary>
    private Control _root = null!;

    /// <summary>总开关按钮，用来显示或隐藏测试面板。</summary>
    private Button _toggleButton = null!;

    /// <summary>模块导航显示开关。</summary>
    private Button _moduleNavToggleButton = null!;

    /// <summary>当前模块路径显示。</summary>
    private Label _currentModulePathLabel = null!;

    /// <summary>承载整个调试界面的面板容器。</summary>
    private PanelContainer _panel = null!;

    /// <summary>提示用户如何进入选实体模式的说明文本。</summary>
    private Label _selectionHintLabel = null!;

    /// <summary>是否允许在场景中通过鼠标点击选择实体。</summary>
    private CheckButton _selectionToggle = null!;

    /// <summary>手动清空当前选中实体的按钮。</summary>
    private Button _clearSelectionButton = null!;

    /// <summary>强制刷新当前模块内容的按钮。</summary>
    private Button _refreshButton = null!;

    /// <summary>显示当前选中实体名称、类型和 ID 的标签。</summary>
    private Label _selectedEntityLabel = null!;

    /// <summary>模块导航面板。</summary>
    private Control _moduleNavPanel = null!;

    /// <summary>模块导航树，按模块路径自动分组。</summary>
    private Tree _moduleTree = null!;

    /// <summary>真正放置各个测试模块实例的宿主容器。</summary>
    private VBoxContainer _moduleHost = null!;

    /// <summary>面板当前可见性缓存，用于避免重复处理隐藏/显示逻辑。</summary>
    private bool _panelVisible = true;

    /// <summary>模块导航当前是否可见。</summary>
    private bool _moduleNavVisible = true;

    /// <summary>当前是否正在由代码同步模块树选中态。</summary>
    private bool _syncingModuleTreeSelection;

    /// <summary>统一选中实体上下文。</summary>
    private TestSelectionContext _selectionContext = null!;

    /// <summary>模块共享上下文。</summary>
    private TestModuleContext _moduleContext = null!;

    /// <summary>
    /// 模块初始化入口。
    /// <para>
    /// 把 TestSystem 注册到 AutoLoad，确保仅在调试优先级链路中自动挂载。
    /// </para>
    /// </summary>
    [ModuleInitializer]
    internal static void Initialize()
    {
        AutoLoad.Register(new AutoLoad.AutoLoadConfig
        {
            Name = nameof(TestSystem),
            Scene = ResourceManagement.Load<PackedScene>(nameof(TestSystem), ResourceCategory.System),
            Priority = AutoLoad.Priority.Debug,
            ParentPath = "Debug"
        });
    }

    /// <summary>
    /// 进入场景树时做单例校验，并把测试系统设置为始终处理输入。
    /// </summary>
    public override void _EnterTree()
    {
        if (Instance != null && Instance != this)
        {
            QueueFree();
            return;
        }

        Instance = this;
        Layer = 100;
        ProcessMode = ProcessModeEnum.Always;
    }

    /// <summary>
    /// 场景准备完成后构建 UI、注册子模块，并设置默认模块。
    /// </summary>
    public override void _Ready()
    {
        // 缓存 UI 节点
        CacheUiNodes();
        // 绑定 UI 事件
        BindUiEvents();
        // 初始化上下文
        InitializeContexts();
        // 绑定选中实体变化事件
        BindSelectionContextEvents();
        // 从ModuleHost节点中扫描并注册所有测试模块。
        RegisterModulesFromScene();
        // 设置默认模块
        if (_modules.Count > 0)
        {
            TrySwitchModule(_moduleIds[0]);
        }

        BindMouseSelectionEvents();
        // 更新选中实体显示
        UpdateSelectedEntityDisplay();
        _log.Info("TestSystem 初始化完成");
    }

    /// <summary>
    /// 离开场景树时清理单例引用，避免调试系统指向失效对象。
    /// </summary>
    public override void _ExitTree()
    {
        _currentModule?.DeactivateModule();
        if (_selectionContext != null)
        {
            UnbindSelectionContextEvents();
        }

        UnbindMouseSelectionEvents();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    //====================================================

    /// <summary>
    /// 缓存所有 UI 节点引用。
    /// </summary>
    private void CacheUiNodes()
    {
        // 缓存 UI 节点
        _root = GetNode<Control>("Root");
        _toggleButton = GetNode<Button>("Root/TopLeft/Layout/Toolbar/ToggleButton");
        _moduleNavToggleButton = GetNode<Button>("Root/TopLeft/Layout/Toolbar/ModuleNavToggleButton");
        _currentModulePathLabel = GetNode<Label>("Root/TopLeft/Layout/Toolbar/CurrentModulePathLabel");
        _panel = GetNode<PanelContainer>("Root/TopLeft/Layout/Panel");
        _selectionHintLabel = GetNode<Label>("Root/TopLeft/Layout/Panel/PanelMargin/PanelLayout/SelectionHintLabel");
        _selectionToggle =
            GetNode<CheckButton>("Root/TopLeft/Layout/Panel/PanelMargin/PanelLayout/InfoRow/SelectionToggle");
        _refreshButton = GetNode<Button>("Root/TopLeft/Layout/Panel/PanelMargin/PanelLayout/InfoRow/RefreshButton");
        _clearSelectionButton =
            GetNode<Button>("Root/TopLeft/Layout/Panel/PanelMargin/PanelLayout/InfoRow/ClearSelectionButton");
        _selectedEntityLabel =
            GetNode<Label>("Root/TopLeft/Layout/Panel/PanelMargin/PanelLayout/InfoRow/SelectedEntityLabel");
        _moduleNavPanel = GetNode<Control>("Root/TopLeft/Layout/Panel/PanelMargin/PanelLayout/ModuleSplit/ModuleNavPanel");
        _moduleTree = GetNode<Tree>("Root/TopLeft/Layout/Panel/PanelMargin/PanelLayout/ModuleSplit/ModuleNavPanel/ModuleNavMargin/ModuleNavLayout/ModuleTree");
        _moduleHost = GetNode<VBoxContainer>("Root/TopLeft/Layout/Panel/PanelMargin/PanelLayout/ModuleSplit/ModuleHost");

        // 设置鼠标过滤：
        // 1. 大部分容器和展示节点使用 Ignore，避免拦截鼠标，保证底层测试选取逻辑仍可正常响应。
        // 2. 仅面板本体使用 Stop，作为调试 UI 的交互边界，防止点击穿透到游戏画面。
        _root.MouseFilter = Control.MouseFilterEnum.Ignore;
        GetNode<Control>("Root/TopLeft").MouseFilter = Control.MouseFilterEnum.Ignore;
        GetNode<Control>("Root/TopLeft/Layout").MouseFilter = Control.MouseFilterEnum.Ignore;
        GetNode<Control>("Root/TopLeft/Layout/Toolbar").MouseFilter = Control.MouseFilterEnum.Ignore;
        _panel.MouseFilter = Control.MouseFilterEnum.Stop;
        GetNode<Control>("Root/TopLeft/Layout/Panel/PanelMargin/PanelLayout").MouseFilter =
            Control.MouseFilterEnum.Ignore;
        _selectionHintLabel.MouseFilter = Control.MouseFilterEnum.Ignore;
        GetNode<Control>("Root/TopLeft/Layout/Panel/PanelMargin/PanelLayout/InfoRow").MouseFilter =
            Control.MouseFilterEnum.Ignore;
        _moduleNavPanel.MouseFilter = Control.MouseFilterEnum.Stop;
        _moduleTree.MouseFilter = Control.MouseFilterEnum.Stop;
        _moduleHost.MouseFilter = Control.MouseFilterEnum.Ignore;
        _moduleTree.HideRoot = true;
    }

    /// <summary>
    /// 绑定所有 UI 事件。
    /// </summary>
    private void BindUiEvents()
    {
        _toggleButton.Pressed += OnTogglePressed; // 测试按钮事件：切换测试面板显示/隐藏
        _moduleNavToggleButton.Pressed += OnModuleNavTogglePressed; // 模块列表显示/隐藏
        _moduleTree.ItemSelected += OnModuleTreeItemSelected; // 模块树选择事件：切换测试模块
        _refreshButton.Pressed += RefreshCurrentModule; // 刷新按钮事件：刷新当前测试模块
        _clearSelectionButton.Pressed += () => SetSelectedEntity(null); // 清除选择按钮事件：清除选中实体
    }

    // 初始化上下文
    private void InitializeContexts()
    {
        // TestSystem 的统一选中上下文
        _selectionContext = new TestSelectionContext();
        // TestSystem 模块上下文
        _moduleContext = new TestModuleContext(this, _selectionContext);
    }


    //====================================================

    /// <summary>
    /// 设置当前测试实体。
    /// <para>
    /// 实体切换后会先通知所有模块更新选中目标，再刷新当前激活模块的界面。
    /// </para>
    /// </summary>
    public void SetSelectedEntity(IEntity? entity)
    {
        if (_selectionContext == null)
        {
            return;
        }

        // 设置选中的实体
        if (_selectionContext.SetSelectedEntity(entity))
        {
            Events.Emit(
                GameEventType.TestSystem.SelectionChanged,
                new GameEventType.TestSystem.SelectionChangedEventData(
                    entity // 当前选中实体
                )
            );
        }
        else
        {
            RefreshCurrentModule();
        }
    }

    /// <summary>
    /// 刷新当前显示的测试模块。
    /// <para>
    /// 直接触发当前模块 Refresh。
    /// </para>
    /// </summary>
    public void RefreshCurrentModule()
    {
        if (!_panelVisible)
        {
            return;
        }

        _currentModule?.Refresh();
    }

    /// <summary>当前激活模块 Id。</summary>
    public string CurrentModuleId => _currentModule?.Definition.Id ?? string.Empty;

    //=========================测试模块注册===========================
    /// <summary>
    /// 从ModuleHost节点中扫描并注册所有测试模块。
    /// </summary>
    private void RegisterModulesFromScene()
    {
        _modules.Clear();
        _modulesById.Clear();
        _moduleIds.Clear();
        _moduleTree.Clear();

        foreach (Node child in _moduleHost.GetChildren()) // 获取ModuleHost节点下所有测试模块
        {
            if (child is not TestModuleBase module)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(module.Definition.Id))
            {
                _log.Error($"TestSystem 模块缺少稳定 Id: module={module.Name}");
                continue;
            }

            var modulePath = TestModulePath.Normalize(module.Definition.ModulePath);
            if (string.IsNullOrWhiteSpace(modulePath))
            {
                _log.Error($"TestSystem 模块缺少有效路径: id={module.Definition.Id} module={module.Name}");
                continue;
            }

            if (_modulesById.ContainsKey(module.Definition.Id))
            {
                _log.Error($"TestSystem 模块 Id 重复: id={module.Definition.Id} module={module.Name}");
                continue;
            }

            RegisterModule(module);
        }

        RebuildModuleTree();

        if (_modules.Count == 0)
        {
            _log.Warn("TestSystem 未在 ModuleHost 下找到任何测试模块，请检查 TestSystem.tscn 挂载配置");
        }
    }

    /// <summary>
    /// 注册一个测试模块，并把它挂到统一的 UI 宿主中。
    /// </summary>
    private void RegisterModule(ITestModule module)
    {
        // 1. 初始化模块
        module.Initialize(_moduleContext);
        // 2. 添加到模块列表
        _modules.Add(module);
        // 3. 添加到模块字典
        _modulesById[module.Definition.Id] = module;
        // 4. 添加到模块索引列表
        _moduleIds.Add(module.Definition.Id);
        // 5. 通知模块当前选中实体（如果有的话）
        module.OnSelectedEntityChanged(SelectedEntity);
    }

    /// <summary>
    /// 根据模块路径重建左侧导航树。
    /// </summary>
    private void RebuildModuleTree()
    {
        _moduleTree.Clear();
        var root = _moduleTree.CreateItem();
        var groupItems = new Dictionary<string, TreeItem>(StringComparer.Ordinal);

        foreach (var module in _modules)
        {
            var parts = TestModulePath.Split(module.Definition.ModulePath);
            if (parts.Length == 0)
            {
                continue;
            }

            var parent = root;
            var groupPath = string.Empty;
            for (int i = 0; i < parts.Length - 1; i++)
            {
                groupPath = string.IsNullOrEmpty(groupPath) ? parts[i] : $"{groupPath}.{parts[i]}";
                if (!groupItems.TryGetValue(groupPath, out var groupItem))
                {
                    groupItem = _moduleTree.CreateItem(parent);
                    groupItem.SetText(0, parts[i]);
                    groupItem.SetSelectable(0, false);
                    groupItems[groupPath] = groupItem;
                }

                parent = groupItem;
            }

            var moduleItem = _moduleTree.CreateItem(parent);
            moduleItem.SetText(0, parts[^1]);
            moduleItem.SetMetadata(0, module.Definition.Id); // 模块稳定 Id
            moduleItem.SetTooltipText(0, module.Definition.ModulePath);
        }
    }

    //=========================切换测试模块===========================

    /// <summary>
    /// 按稳定模块 Id 切换当前模块。
    /// </summary>
    public bool TrySwitchModule(string moduleId)
    {
        if (string.IsNullOrWhiteSpace(moduleId) || !_modulesById.ContainsKey(moduleId))
        {
            return false;
        }

        SelectModuleTreeItem(moduleId); // 同步模块树选中态
        if (string.Equals(CurrentModuleId, moduleId, StringComparison.Ordinal))
        {
            return true;
        }

        SwitchModule(moduleId);
        return true;
    }

    /// <summary>
    /// 激活指定模块，并让其它模块进入失活状态。
    /// <para>
    /// 切换时会先通知旧模块失活，再显示新模块并刷新一次界面，避免模块残留旧数据。
    /// </para>
    /// </summary>
    private void SwitchModule(string moduleId)
    {
        if (_currentModule != null)
        {
            _currentModule.DeactivateModule();
            _currentModule.ModuleRoot.Visible = false;
        }

        _currentModule = null;
        if (_modulesById.TryGetValue(moduleId, out var currentModule))
        {
            _currentModule = currentModule;
            currentModule.ModuleRoot.Visible = true;
            if (_panelVisible)
            {
                currentModule.ActivateModule();
            }

            UpdateCurrentModulePathDisplay();
        }
    }

    /// <summary>
    /// 测试面板显示与隐藏。
    /// </summary>
    private void OnTogglePressed()
    {
        _panelVisible = !_panelVisible;
        _panel.Visible = _panelVisible;
        _toggleButton.Text = _panelVisible ? "测试" : "测试(隐藏)";

        if (_currentModule == null)
        {
            return;
        }

        if (_panelVisible)
        {
            _currentModule.ActivateModule();
            return;
        }

        _currentModule.DeactivateModule();
    }

    /// <summary>
    /// 模块导航显隐按钮回调。
    /// </summary>
    private void OnModuleNavTogglePressed()
    {
        _moduleNavVisible = !_moduleNavVisible;
        _moduleNavPanel.Visible = _moduleNavVisible;
        _moduleNavToggleButton.Text = _moduleNavVisible ? "隐藏模块" : "显示模块";
    }

    /// <summary>
    /// 模块树选择回调。
    /// </summary>
    private void OnModuleTreeItemSelected()
    {
        if (_syncingModuleTreeSelection)
        {
            return;
        }

        var item = _moduleTree.GetSelected();
        if (item == null)
        {
            return;
        }

        var moduleId = item.GetMetadata(0).AsString();
        if (string.IsNullOrWhiteSpace(moduleId))
        {
            return;
        }

        TrySwitchModule(moduleId);
    }

    /// <summary>
    /// 同步模块树选中态。
    /// </summary>
    /// <param name="moduleId">模块稳定 Id。</param>
    private void SelectModuleTreeItem(string moduleId)
    {
        var root = _moduleTree.GetRoot();
        if (root == null)
        {
            return;
        }

        var item = FindModuleTreeItem(root, moduleId);
        if (item == null)
        {
            return;
        }

        _syncingModuleTreeSelection = true;
        item.Select(0);
        _syncingModuleTreeSelection = false;
    }

    /// <summary>
    /// 递归查找模块树叶子节点。
    /// </summary>
    private static TreeItem? FindModuleTreeItem(TreeItem item, string moduleId)
    {
        if (item.GetMetadata(0).AsString() == moduleId)
        {
            return item;
        }

        var child = item.GetFirstChild();
        while (child != null)
        {
            var matched = FindModuleTreeItem(child, moduleId);
            if (matched != null)
            {
                return matched;
            }

            child = child.GetNext();
        }

        return null;
    }

    /// <summary>
    /// 更新当前模块路径显示。
    /// </summary>
    private void UpdateCurrentModulePathDisplay()
    {
        _currentModulePathLabel.Text = _currentModule == null
            ? "未选择模块"
            : _currentModule.Definition.ModulePath;
    }
}
