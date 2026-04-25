using Godot;
using slime.data.Units;
using System;
using ECS.Base.System.TestSystem.Core;

/// <summary>
/// 敌人生成测试模块。
/// <para>
/// 默认通过 DataNew 敌人名称获取 EnemyData，并复用正式 SpawnSystem 执行批量生成。
/// </para>
/// </summary>
public partial class SpawnTestModule : TestModuleBase
{
    private const string EnemyCatalogPath = "Unit.Enemy";

    private static readonly Log _log = new(nameof(SpawnTestModule));

    [Export] private PackedScene? _resourcePickerScene;

    private ResourcePickerControl _enemyPicker = null!;
    private OptionButton _strategyOption = null!;
    private SpinBox _countSpinBox = null!;
    private Button _spawnButton = null!;
    private Button _clearButton = null!;
    private Button _reloadButton = null!;
    private Label _selectedLabel = null!;
    private Label _statusLabel = null!;
    private Label _poolStatsLabel = null!;

    internal override TestModuleDefinition Definition => new(
        "spawn-enemy", // 模块稳定 Id
        $"{TestModuleGroupId.Common}.敌人生成" // 模块导航路径
    );

    internal override void Initialize(ITestModuleContext context)
    {
        base.Initialize(context);
        CacheUiNodes();
        BuildEnemyPicker();
        BuildStrategyOptions();
        BindUiEvents();
        SetProcess(false);
        RefreshSelectionLabel();
        UpdatePoolStats();
    }

    internal override void OnActivated()
    {
        base.OnActivated();
        SetProcess(true);
        _enemyPicker.Reload();
        RefreshSelectionLabel();
        UpdatePoolStats();
    }

    internal override void OnDeactivated()
    {
        base.OnDeactivated();
        SetProcess(false);
    }

    internal override void Refresh()
    {
        _enemyPicker.Reload();
        RefreshSelectionLabel();
        UpdatePoolStats();
    }

    public override void _Process(double delta)
    {
        if (!IsModuleActive)
        {
            return;
        }

        UpdatePoolStats();
    }

    private void CacheUiNodes()
    {
        MouseFilter = Control.MouseFilterEnum.Stop;
        _strategyOption = GetNode<OptionButton>("Controls/StrategyRow/StrategyOption");
        _countSpinBox = GetNode<SpinBox>("Controls/CountRow/CountSpinBox");
        _spawnButton = GetNode<Button>("Actions/SpawnButton");
        _clearButton = GetNode<Button>("Actions/ClearButton");
        _reloadButton = GetNode<Button>("Actions/ReloadButton");
        _selectedLabel = GetNode<Label>("SelectedLabel");
        _statusLabel = GetNode<Label>("StatusLabel");
        _poolStatsLabel = GetNode<Label>("PoolStatsLabel");
    }

    private void BuildEnemyPicker()
    {
        _enemyPicker = TestSceneHelper.InstantiateScene<ResourcePickerControl>(
            _resourcePickerScene, // 通用资源选择控件场景
            nameof(ResourcePickerControl) // 场景名称用于错误提示
        );
        _enemyPicker.Name = "EnemyPicker";
        AddChild(_enemyPicker);
        MoveChild(
            _enemyPicker, // 资源选择控件
            2 // 放在说明与当前选择标签之后
        );
        _enemyPicker.Configure(EnemyCatalogPath); // 生成模块只允许选择 Data/Data/Unit/Enemy 下的配置
    }

    private void BuildStrategyOptions()
    {
        _strategyOption.Clear();
        foreach (var strategy in Enum.GetValues<SpawnPositionStrategy>())
        {
            _strategyOption.AddItem(
                strategy.ToString(), // 策略显示名
                (int)strategy // 策略枚举值
            );
        }

        _strategyOption.Select((int)SpawnPositionStrategy.Rectangle);
    }

    private void BindUiEvents()
    {
        _enemyPicker.SelectionChanged += OnEnemySelectionChanged;
        _strategyOption.ItemSelected += OnStrategySelected;
        _spawnButton.Pressed += SpawnSelectedEnemies;
        _clearButton.Pressed += ClearEnemies;
        _reloadButton.Pressed += ReloadCatalog;
    }

    private void SpawnSelectedEnemies()
    {
        if (!_enemyPicker.TryGetSelectedEntry(out var entry))
        {
            ShowStatus("请先选择一个敌人配置");
            return;
        }

        var config = ResolveEnemyConfig(entry);
        if (config == null)
        {
            _log.Error($"[敌人生成测试] 加载敌人配置失败: key={entry.ResourceKey} name={entry.DisplayName} path={entry.Path}");
            ShowStatus($"加载失败: {entry.ResourceKey}");
            return;
        }

        var manager = SystemManager.Instance;
        if (manager == null)
        {
            ShowStatus("SystemManager 未初始化");
            return;
        }

        var count = Math.Max(1, (int)_countSpinBox.Value);
        var strategy = GetSelectedStrategy();

        _log.Info($"[敌人生成测试] 生成敌人: name={entry.DisplayName} key={entry.ResourceKey} count={count} strategy={strategy}");
        var result = manager.Execute<SpawnSystem, SpawnBatchRequest, SpawnBatchResult>(
            new SpawnBatchRequest(
                count, // 生成数量
                config, // 敌人配置
                strategy // 生成策略
            )
        );
        ShowStatus(result.Success
            ? $"已生成 {count} 个 {entry.DisplayName}，策略 {strategy}"
            : $"SpawnSystem 当前不可执行: {result.Message}");
        UpdatePoolStats();
    }

    /// <summary>
    /// 解析敌人配置：按 DataNew 敌人 Name 获取。
    /// </summary>
    /// <param name="entry">资源选择器条目。</param>
    /// <returns>敌人配置数据。</returns>
    private static EnemyData? ResolveEnemyConfig(ResourceCatalogEntry entry)
    {
        return EnemyData.Get(entry.DisplayName) ?? EnemyData.Get(entry.ResourceKey);
    }

    private void ClearEnemies()
    {
        var manager = SystemManager.Instance;
        if (manager == null)
        {
            ShowStatus("SystemManager 未初始化");
            return;
        }

        var result = manager.Execute<SpawnSystem, KillAllEnemiesRequest, KillAllEnemiesResult>(
            new KillAllEnemiesRequest()
        );
        ShowStatus(result.Success ? "已清空敌人池中的活跃敌人" : $"SpawnSystem 当前不可执行: {result.Message}");
        UpdatePoolStats();
    }

    private void ReloadCatalog()
    {
        ResourceCatalog.ClearCache();
        _enemyPicker.Reload();
        RefreshSelectionLabel();
        ShowStatus("资源目录已刷新");
    }

    private SpawnPositionStrategy GetSelectedStrategy()
    {
        var selectedId = _strategyOption.GetSelectedId();
        return Enum.IsDefined(typeof(SpawnPositionStrategy), selectedId)
            ? (SpawnPositionStrategy)selectedId
            : SpawnPositionStrategy.Rectangle;
    }

    private void RefreshSelectionLabel()
    {
        if (!_enemyPicker.TryGetSelectedEntry(out var entry))
        {
            _selectedLabel.Text = "当前敌人：未选择";
            return;
        }

        _selectedLabel.Text = $"当前敌人：{entry.DisplayName} [{entry.ResourceKey}]";
    }

    private void UpdatePoolStats()
    {
        var pool = ObjectPoolManager.GetPool<EnemyEntity>(ObjectPoolNames.EnemyPool);
        if (pool == null)
        {
            _poolStatsLabel.Text = $"{ObjectPoolNames.EnemyPool} 未初始化";
            return;
        }

        var stats = pool.GetStats();
        _poolStatsLabel.Text = $"敌人池 Active: {stats.ActiveCount} | Pool: {stats.Count} | Created: {stats.TotalCreated}";
    }

    private void ShowStatus(string message)
    {
        _statusLabel.Text = message;
    }

    private void OnEnemySelectionChanged(string resourceKey)
    {
        RefreshSelectionLabel();
    }

    private void OnStrategySelected(long index)
    {
        ShowStatus($"生成策略：{GetSelectedStrategy()}");
    }
}
