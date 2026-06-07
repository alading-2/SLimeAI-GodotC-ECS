using Godot;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ECS.Base.System.TestSystem.Core;

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
    private const float PanelMargin = 24f;
    private const float PanelWidthStep = 160f;
    private const float PanelHeightStep = 120f;
    private const float PanelMinWidth = 520f;
    private const float PanelMinHeight = 420f;
    private const float PanelMaxWidthPadding = 24f;
    private const float PanelMaxHeightPadding = 24f;
    private const float PanelToolbarHeight = 72f;

    /// <summary>测试系统日志器，用于记录初始化与关键调试操作。</summary>
    private static readonly Log _log = new(nameof(TestSystem));

    private static TestSystem? _instance;

    /// <summary>测试系统单例，方便其它调试工具直接访问当前测试面板。</summary>
    public static TestSystem? Instance => _instance;

    /// <summary>当前被测试面板选中的实体；属性面板、技能面板等都会围绕它刷新。</summary>
    public IEntity? SelectedEntity => _selectionContext?.SelectedEntity;

    /// <summary>TestSystem 局部事件总线，仅用于测试面板内部通信。</summary>
    public EventBus Events { get; } = new();

    /// <summary>当前已注册的模块场景清单，顺序就是导航展示顺序。</summary>
    private readonly List<TestModuleSceneDefinition> _moduleScenes = new();

    /// <summary>模块 Id 到模块场景定义的快速映射。</summary>
    private readonly Dictionary<string, TestModuleSceneDefinition> _moduleScenesById = new(StringComparer.Ordinal);

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

    /// <summary>缩小面板按钮。</summary>
    private Button _panelShrinkButton = null!;

    /// <summary>放大面板按钮。</summary>
    private Button _panelExpandButton = null!;

    /// <summary>当前模块路径显示。</summary>
    private Label _currentModulePathLabel = null!;

    /// <summary>测试面板左上容器，用于调整整体可视尺寸。</summary>
    private Control _topLeft = null!;

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

    /// <summary>右侧当前模块实例的挂载槽位。</summary>
    private Control _moduleViewport = null!;

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
    /// 把 TestSystem 注册到统一系统注册表，确保仅在调试生命周期链路中自动挂载。
    /// </para>
    /// </summary>
    [ModuleInitializer]
    internal static void Initialize()
    {
        SystemRegistry.Register(nameof(TestSystem),
            static () => ResourceLoading.Load<PackedScene>(nameof(TestSystem), ResourceCategory.System).Instantiate());
    }

    /// <summary>
    /// 进入场景树时做单例校验，并把测试系统设置为始终处理输入。
    /// </summary>
    public override void _EnterTree()
    {
        if (!NodeSingletonGuard.TryBind(this, ref _instance, _log))
        {
            return;
        }

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
        // 初始化面板尺寸
        ApplyPanelFrameSize(_panel.Size);
        // 初始化上下文
        InitializeContexts();
        // 绑定选中实体变化事件
        BindSelectionContextEvents();
        // 注册 TestSystem 支持的模块清单。
        RegisterModulesFromScene();
        // 设置默认模块
        if (_moduleIds.Count > 0)
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
        DisposeCurrentModule();
        if (_selectionContext != null)
        {
            UnbindSelectionContextEvents();
        }

        UnbindMouseSelectionEvents();

        NodeSingletonGuard.Release(this, ref _instance);
    }

    //====================================================

    /// <summary>
    /// 缓存所有 UI 节点引用。
    /// </summary>
    private void CacheUiNodes()
    {
        // 缓存 UI 节点
        _root = GetNode<Control>("Root");
        _topLeft = GetNode<Control>("Root/TopLeft");
        _toggleButton = GetNode<Button>("Root/TopLeft/Layout/Toolbar/ToggleButton");
        _moduleNavToggleButton = GetNode<Button>("Root/TopLeft/Layout/Toolbar/ModuleNavToggleButton");
        _panelShrinkButton = GetNode<Button>("Root/TopLeft/Layout/Toolbar/PanelShrinkButton");
        _panelExpandButton = GetNode<Button>("Root/TopLeft/Layout/Toolbar/PanelExpandButton");
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
        _moduleViewport = GetNode<Control>("Root/TopLeft/Layout/Panel/PanelMargin/PanelLayout/ModuleSplit/ModuleViewport");

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
        _moduleViewport.MouseFilter = Control.MouseFilterEnum.Stop;
        _moduleTree.HideRoot = true;
    }

    /// <summary>
    /// 绑定所有 UI 事件。
    /// </summary>
    private void BindUiEvents()
    {
        _toggleButton.Pressed += OnTogglePressed; // 测试按钮事件：切换测试面板显示/隐藏
        _moduleNavToggleButton.Pressed += OnModuleNavTogglePressed; // 模块列表显示/隐藏
        _panelShrinkButton.Pressed += ShrinkPanelFrame; // 缩小面板
        _panelExpandButton.Pressed += ExpandPanelFrame; // 放大面板
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
                new GameEventType.TestSystem.SelectionChanged(
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
    /// 注册当前 TestSystem 支持的模块清单。
    /// </summary>
    private void RegisterModulesFromScene()
    {
        _moduleScenes.Clear();
        _moduleScenesById.Clear();
        _moduleIds.Clear();
        _moduleTree.Clear();

        foreach (var moduleScene in TestModuleSceneRegistry.Entries)
        {
            if (string.IsNullOrWhiteSpace(moduleScene.Definition.Id))
            {
                _log.Error($"TestSystem 模块缺少稳定 Id: scene={moduleScene.SceneResourceKey}");
                continue;
            }

            var modulePath = TestModulePath.Normalize(moduleScene.Definition.ModulePath);
            if (string.IsNullOrWhiteSpace(modulePath))
            {
                _log.Error($"TestSystem 模块缺少有效路径: id={moduleScene.Definition.Id} scene={moduleScene.SceneResourceKey}");
                continue;
            }

            if (_moduleScenesById.ContainsKey(moduleScene.Definition.Id))
            {
                _log.Error($"TestSystem 模块 Id 重复: id={moduleScene.Definition.Id} scene={moduleScene.SceneResourceKey}");
                continue;
            }

            _moduleScenes.Add(moduleScene);
            _moduleScenesById[moduleScene.Definition.Id] = moduleScene;
            _moduleIds.Add(moduleScene.Definition.Id);
        }

        RebuildModuleTree();

        if (_moduleScenes.Count == 0)
        {
            _log.Warn("TestSystem 模块清单为空，请检查 TestModuleSceneRegistry 配置");
        }
    }

    /// <summary>
    /// 根据模块路径重建左侧导航树。
    /// </summary>
    private void RebuildModuleTree()
    {
        _moduleTree.Clear();
        var root = _moduleTree.CreateItem();
        var groupItems = new Dictionary<string, TreeItem>(StringComparer.Ordinal);

        foreach (var moduleScene in _moduleScenes)
        {
            var parts = TestModulePath.Split(moduleScene.Definition.ModulePath);
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
            moduleItem.SetMetadata(0, moduleScene.Definition.Id); // 模块稳定 Id
            moduleItem.SetTooltipText(0, moduleScene.Definition.ModulePath);
        }
    }

    //=========================切换测试模块===========================

    /// <summary>
    /// 按稳定模块 Id 切换当前模块。
    /// </summary>
    public bool TrySwitchModule(string moduleId)
    {
        if (string.IsNullOrWhiteSpace(moduleId) || !_moduleScenesById.ContainsKey(moduleId))
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
        DisposeCurrentModule();
        if (!_moduleScenesById.TryGetValue(moduleId, out var moduleScene))
        {
            UpdateCurrentModulePathDisplay();
            return;
        }

        try
        {
            var scene = ResourceLoading.Load<PackedScene>(moduleScene.SceneResourceKey, ResourceCategory.System);
            var currentModule = TestSceneHelper.InstantiateScene<TestModuleBase>(scene, moduleScene.SceneResourceKey);
            PrepareModuleRoot(currentModule);
            _moduleViewport.AddChild(currentModule);
            currentModule.Initialize(_moduleContext);
            currentModule.Visible = true;
            currentModule.OnSelectedEntityChanged(SelectedEntity);
            _currentModule = currentModule;
            if (_panelVisible)
            {
                currentModule.ActivateModule();
            }
        }
        catch (Exception ex)
        {
            _log.Error(
                $"TestSystem 模块初始化失败: id={moduleScene.Definition.Id} path={moduleScene.Definition.ModulePath} error={ex.Message}");
        }

        UpdateCurrentModulePathDisplay();
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
    /// 缩小测试面板整体尺寸。
    /// </summary>
    private void ShrinkPanelFrame()
    {
        ApplyPanelFrameSize(new Vector2(
            _panel.Size.X - PanelWidthStep, // 目标宽度
            _panel.Size.Y - PanelHeightStep // 目标高度
        ));
    }

    /// <summary>
    /// 放大测试面板整体尺寸。
    /// </summary>
    private void ExpandPanelFrame()
    {
        ApplyPanelFrameSize(new Vector2(
            _panel.Size.X + PanelWidthStep, // 目标宽度
            _panel.Size.Y + PanelHeightStep // 目标高度
        ));
    }

    /// <summary>
    /// 按当前视口限制应用测试面板尺寸。
    /// </summary>
    /// <param name="targetSize">希望应用的面板尺寸。</param>
    private void ApplyPanelFrameSize(Vector2 targetSize)
    {
        var viewportSize = GetViewport().GetVisibleRect().Size;
        var maxWidth = Mathf.Max(PanelMinWidth, viewportSize.X - PanelMaxWidthPadding);
        var maxHeight = Mathf.Max(PanelMinHeight, viewportSize.Y - PanelMaxHeightPadding);
        var minWidth = Mathf.Min(PanelMinWidth, maxWidth);
        var minHeight = Mathf.Min(PanelMinHeight, maxHeight);
        var width = Mathf.Clamp(targetSize.X, minWidth, maxWidth);
        var height = Mathf.Clamp(targetSize.Y, minHeight, maxHeight);

        _panel.CustomMinimumSize = new Vector2(width, height);
        _topLeft.OffsetRight = _topLeft.OffsetLeft + width + PanelMargin;
        _topLeft.OffsetBottom = _topLeft.OffsetTop + height + PanelMargin + PanelToolbarHeight;
        _panelShrinkButton.Disabled = width <= minWidth && height <= minHeight;
        _panelExpandButton.Disabled =
            width >= viewportSize.X - PanelMaxWidthPadding
            && height >= viewportSize.Y - PanelMaxHeightPadding;
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

    /// <summary>
    /// 释放当前已挂载模块实例。
    /// </summary>
    private void DisposeCurrentModule()
    {
        if (_currentModule == null)
        {
            return;
        }

        _currentModule.DeactivateModule();
        var moduleRoot = _currentModule.ModuleRoot;
        _currentModule = null;
        if (IsInstanceValid(moduleRoot))
        {
            moduleRoot.QueueFree();
        }
    }

    /// <summary>
    /// 把模块根节点铺满右侧挂载槽位。
    /// </summary>
    /// <param name="moduleRoot">当前模块根节点。</param>
    private static void PrepareModuleRoot(Control moduleRoot)
    {
        moduleRoot.Visible = true;
        moduleRoot.AnchorLeft = 0f;
        moduleRoot.AnchorTop = 0f;
        moduleRoot.AnchorRight = 1f;
        moduleRoot.AnchorBottom = 1f;
        moduleRoot.OffsetLeft = 0f;
        moduleRoot.OffsetTop = 0f;
        moduleRoot.OffsetRight = 0f;
        moduleRoot.OffsetBottom = 0f;
        moduleRoot.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        moduleRoot.SizeFlagsVertical = Control.SizeFlags.ExpandFill;
    }
}
