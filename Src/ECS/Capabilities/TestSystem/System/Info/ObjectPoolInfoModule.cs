using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// 对象池信息测试模块。
/// <para>
/// 用于在 TestSystem 中查看全部对象池的运行时统计与容量配置。
/// </para>
/// </summary>
public partial class ObjectPoolInfoModule : TestModuleBase
{
    private const float AutoRefreshIntervalSeconds = 1f;

    private readonly ObjectPoolInfoService _service = new();
    private readonly List<ObjectPoolInfoSnapshot> _snapshots = new();

    private Label _summaryLabel = null!;
    private ItemList _poolList = null!;
    private Label _overviewLabel = null!;
    private Label _detailsLabel = null!;
    private Label _statusLabel = null!;
    private float _autoRefreshElapsed;

    internal override TestModuleDefinition Definition => new(
        "object-pool-info", // 模块稳定 Id
        $"{TestModuleGroupId.Info}.对象池" // 模块导航路径
    );

    internal override void Initialize(ITestModuleContext context)
    {
        base.Initialize(context);
        CacheUiNodes();
        BindUiEvents();
        SetProcess(false);
        ReloadSnapshots();
    }

    internal override void OnActivated()
    {
        base.OnActivated();
        _autoRefreshElapsed = 0f;
        SetProcess(true);
        ReloadSnapshots();
    }

    internal override void OnDeactivated()
    {
        base.OnDeactivated();
        SetProcess(false);
        _autoRefreshElapsed = 0f;
    }

    internal override void Refresh()
    {
        _autoRefreshElapsed = 0f;
        ReloadSnapshots();
    }

    public override void _Process(double delta)
    {
        if (!CanRefresh)
        {
            return;
        }

        _autoRefreshElapsed += (float)delta; // 累积模块激活后的运行时间
        if (_autoRefreshElapsed < AutoRefreshIntervalSeconds)
        {
            return;
        }

        _autoRefreshElapsed = 0f;
        ReloadSnapshots();
    }

    /// <summary>
    /// 缓存固定 UI 节点。
    /// </summary>
    private void CacheUiNodes()
    {
        MouseFilter = Control.MouseFilterEnum.Stop;
        _summaryLabel = GetNode<Label>("SummaryLabel");
        _poolList = GetNode<ItemList>("Content/PoolListPanel/PoolListMargin/PoolList");
        _overviewLabel = GetNode<Label>("Content/DetailsLayout/OverviewPanel/OverviewMargin/OverviewLabel");
        _detailsLabel = GetNode<Label>("Content/DetailsLayout/DetailsPanel/DetailsMargin/DetailsScroll/DetailsLabel");
        _statusLabel = GetNode<Label>("StatusLabel");
    }

    /// <summary>
    /// 绑定 UI 事件。
    /// </summary>
    private void BindUiEvents()
    {
        _poolList.ItemSelected += OnPoolSelected;
    }

    /// <summary>
    /// 重新读取对象池快照并刷新显示。
    /// </summary>
    private void ReloadSnapshots()
    {
        var selectedPoolName = GetSelectedPoolName();
        _snapshots.Clear();
        _snapshots.AddRange(_service.GetSnapshots());
        RebuildPoolList(selectedPoolName);
        UpdateSummary();

        if (_snapshots.Count == 0)
        {
            _overviewLabel.Text = "当前没有可观测对象池";
            _detailsLabel.Text = "暂无对象池运行时统计";
            _statusLabel.Text = "对象池列表为空";
            return;
        }

        if (_poolList.GetSelectedItems().Length == 0)
        {
            _poolList.Select(0);
        }

        UpdateSelectedSnapshotDetails();
    }

    /// <summary>
    /// 重建对象池总览列表。
    /// </summary>
    /// <param name="selectedPoolName">刷新前选中的对象池名称。</param>
    private void RebuildPoolList(string selectedPoolName)
    {
        _poolList.Clear();
        var selectedIndex = -1;
        for (var i = 0; i < _snapshots.Count; i++)
        {
            var snapshot = _snapshots[i];
            _poolList.AddItem(snapshot.PoolName);
            if (selectedIndex >= 0)
            {
                continue;
            }

            if (string.Equals(snapshot.PoolName, selectedPoolName, StringComparison.Ordinal))
            {
                selectedIndex = i;
            }
        }

        if (selectedIndex >= 0)
        {
            _poolList.Select(selectedIndex);
        }
    }

    /// <summary>
    /// 更新汇总文案。
    /// </summary>
    private void UpdateSummary()
    {
        var poolCount = _snapshots.Count;
        var riskCount = 0;
        foreach (var snapshot in _snapshots)
        {
            if (!string.Equals(snapshot.RiskHint, "正常", StringComparison.Ordinal))
            {
                riskCount++;
            }
        }

        _summaryLabel.Text = $"对象池 {poolCount} 个，存在提示 {riskCount} 个";
    }

    /// <summary>
    /// 刷新当前选中池的详情文案。
    /// </summary>
    private void UpdateSelectedSnapshotDetails()
    {
        var selectedItems = _poolList.GetSelectedItems();
        if (selectedItems.Length == 0)
        {
            _overviewLabel.Text = "请选择一个对象池";
            _detailsLabel.Text = "请选择一个对象池";
            _statusLabel.Text = "未选择对象池";
            return;
        }

        var itemIndex = selectedItems[0];
        if (itemIndex < 0 || itemIndex >= _snapshots.Count)
        {
            _overviewLabel.Text = "当前选择无效";
            _detailsLabel.Text = "当前选择无效";
            _statusLabel.Text = "对象池索引超出范围";
            return;
        }

        var snapshot = _snapshots[itemIndex];
        _overviewLabel.Text =
            $"对象池：{snapshot.PoolName}\n" +
            $"风险提示：{snapshot.RiskHint}\n" +
            $"闲置数量：{snapshot.Stats.Count}\n" +
            $"使用中数量：{snapshot.Stats.ActiveCount}\n" +
            $"累计创建：{snapshot.Stats.TotalCreated}\n" +
            $"复用率：{snapshot.Stats.ReuseRate:P2}";

        _detailsLabel.Text =
            $"累计获取：{snapshot.Stats.TotalAcquired}\n" +
            $"累计池内复用：{snapshot.Stats.TotalReused}\n" +
            $"累计扩容新建：{snapshot.Stats.TotalCreatedOnAcquire}\n" +
            $"累计归还：{snapshot.Stats.TotalReleased}\n" +
            $"累计丢弃：{snapshot.Stats.TotalDiscarded}\n" +
            $"\n" +
            $"初始容量：{FormatCapacity(snapshot.HasMetadata, snapshot.InitialSize)}\n" +
            $"最大容量：{FormatCapacity(snapshot.HasMetadata, snapshot.MaxSize)}";
        _statusLabel.Text = $"已选择 {snapshot.PoolName}";
    }

    /// <summary>
    /// 对象池列表选中回调。
    /// </summary>
    /// <param name="index">选中索引。</param>
    private void OnPoolSelected(long index)
    {
        UpdateSelectedSnapshotDetails();
    }

    /// <summary>
    /// 获取当前选中的对象池名称，供刷新后恢复选择。
    /// </summary>
    private string GetSelectedPoolName()
    {
        var selectedItems = _poolList.GetSelectedItems();
        if (selectedItems.Length == 0)
        {
            return string.Empty;
        }

        var itemIndex = selectedItems[0];
        return itemIndex >= 0 && itemIndex < _snapshots.Count
            ? _snapshots[itemIndex].PoolName
            : string.Empty;
    }

    /// <summary>
    /// 统一格式化容量字段，未登记时直接显示“未登记”。
    /// </summary>
    /// <param name="hasMetadata">是否存在容量元数据。</param>
    /// <param name="value">原始容量值。</param>
    private static string FormatCapacity(bool hasMetadata, int value)
    {
        return hasMetadata ? value.ToString() : "未登记";
    }
}
