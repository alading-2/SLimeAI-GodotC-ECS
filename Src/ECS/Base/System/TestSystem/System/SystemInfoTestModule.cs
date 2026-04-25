using Godot;
using System;
using System.Collections.Generic;
using System.Text;
using ECS.Base.System.TestSystem.Core;

/// <summary>
/// 系统信息测试模块。
/// <para>用于在 TestSystem 中查看系统配置、运行状态，并执行受保护的运行时管理操作。</para>
/// </summary>
public partial class SystemInfoTestModule : TestModuleBase
{
    private static readonly Log _log = new(nameof(SystemInfoTestModule));

    private readonly SystemInfoService _service = new();
    private readonly List<SystemInfoSnapshot> _snapshots = new();
    private readonly List<SystemInfoSnapshot> _visibleSnapshots = new();
    private readonly Dictionary<string, SystemInfoSnapshot> _snapshotsBySystemId = new(StringComparer.Ordinal);

    private Label _summaryLabel = null!;
    private OptionButton _groupOption = null!;
    private OptionButton _tagOption = null!;
    private OptionButton _statusOption = null!;
    private LineEdit _searchEdit = null!;
    private Button _refreshButton = null!;
    private ItemList _systemList = null!;
    private Label _overviewLabel = null!;
    private Label _detailsLabel = null!;
    private Button _addButton = null!;
    private Button _enableButton = null!;
    private Button _disableButton = null!;
    private Button _removeButton = null!;
    private Label _statusLabel = null!;

    internal override TestModuleDefinition Definition => new(
        "system-info", // 模块稳定 Id
        $"{TestModuleGroupId.System}.系统监控" // 模块导航路径
    );

    internal override void Initialize(ITestModuleContext context)
    {
        base.Initialize(context);
        CacheUiNodes();
        BindUiEvents();
        BuildFilterOptions();
        ReloadSnapshots();
    }

    internal override void OnActivated()
    {
        base.OnActivated();
        ReloadSnapshots();
    }

    internal override void Refresh()
    {
        ReloadSnapshots();
    }

    /// <summary>
    /// 缓存系统监控模块固定 UI 节点。
    /// </summary>
    private void CacheUiNodes()
    {
        MouseFilter = Control.MouseFilterEnum.Stop;
        _summaryLabel = GetNode<Label>("SummaryLabel");
        _groupOption = GetNode<OptionButton>("Filters/GroupOption");
        _tagOption = GetNode<OptionButton>("Filters/TagOption");
        _statusOption = GetNode<OptionButton>("Filters/StatusOption");
        _searchEdit = GetNode<LineEdit>("Filters/SearchEdit");
        _refreshButton = GetNode<Button>("Filters/RefreshButton");
        _systemList = GetNode<ItemList>("Content/SystemListPanel/SystemListMargin/SystemList");
        _overviewLabel = GetNode<Label>("Content/DetailsLayout/OverviewPanel/OverviewMargin/OverviewLabel");
        _detailsLabel = GetNode<Label>("Content/DetailsLayout/DetailsPanel/DetailsMargin/DetailsScroll/DetailsLabel");
        _addButton = GetNode<Button>("Content/DetailsLayout/Actions/AddButton");
        _enableButton = GetNode<Button>("Content/DetailsLayout/Actions/EnableButton");
        _disableButton = GetNode<Button>("Content/DetailsLayout/Actions/DisableButton");
        _removeButton = GetNode<Button>("Content/DetailsLayout/Actions/RemoveButton");
        _statusLabel = GetNode<Label>("StatusLabel");
    }

    /// <summary>
    /// 绑定筛选和操作事件。
    /// </summary>
    private void BindUiEvents()
    {
        _groupOption.ItemSelected += _ => RebuildFilteredList(GetSelectedSystemId()); // 分组筛选
        _tagOption.ItemSelected += _ => RebuildFilteredList(GetSelectedSystemId()); // 标签筛选
        _statusOption.ItemSelected += _ => RebuildFilteredList(GetSelectedSystemId()); // 状态筛选
        _searchEdit.TextChanged += _ => RebuildFilteredList(GetSelectedSystemId()); // 搜索筛选
        _refreshButton.Pressed += ReloadSnapshots; // 重新读取系统快照
        _systemList.ItemSelected += _ => UpdateSelectedSystemDetails(); // 列表选中系统
        _addButton.Pressed += OnAddPressed; // 添加系统
        _enableButton.Pressed += OnEnablePressed; // 启用系统
        _disableButton.Pressed += OnDisablePressed; // 禁用系统
        _removeButton.Pressed += OnRemovePressed; // 移除系统
    }

    /// <summary>
    /// 构建固定筛选选项。
    /// </summary>
    private void BuildFilterOptions()
    {
        _groupOption.Clear();
        _groupOption.AddItem("全部分组");
        _groupOption.SetItemMetadata(0, string.Empty); // 空字符串表示不过滤分组
        foreach (SystemGroup group in Enum.GetValues(typeof(SystemGroup)))
        {
            var index = _groupOption.ItemCount;
            _groupOption.AddItem(group.ToString());
            _groupOption.SetItemMetadata(index, group.ToString()); // 保存分组枚举名
        }

        _tagOption.Clear();
        _tagOption.AddItem("全部标签");
        _tagOption.SetItemMetadata(0, string.Empty); // 空字符串表示不过滤标签
        foreach (SystemTag tag in Enum.GetValues(typeof(SystemTag)))
        {
            if (tag == SystemTag.None)
            {
                continue;
            }

            var index = _tagOption.ItemCount;
            _tagOption.AddItem(tag.ToString());
            _tagOption.SetItemMetadata(index, tag.ToString()); // 保存标签枚举名
        }

        _statusOption.Clear();
        _statusOption.AddItem("全部状态");
        _statusOption.SetItemMetadata(0, string.Empty); // 空字符串表示不过滤状态
        foreach (SystemInfoStatus status in Enum.GetValues(typeof(SystemInfoStatus)))
        {
            var index = _statusOption.ItemCount;
            _statusOption.AddItem(GetStatusDisplayName(status));
            _statusOption.SetItemMetadata(index, status.ToString()); // 保存状态枚举名
        }
    }

    /// <summary>
    /// 重新读取系统快照并刷新列表。
    /// </summary>
    private void ReloadSnapshots()
    {
        var selectedSystemId = GetSelectedSystemId();
        _snapshots.Clear();
        _snapshotsBySystemId.Clear();

        var snapshots = _service.GetSnapshots(SystemManager.Instance);
        foreach (var snapshot in snapshots)
        {
            _snapshots.Add(snapshot);
            _snapshotsBySystemId[snapshot.SystemId] = snapshot;
        }

        RebuildFilteredList(selectedSystemId);
    }

    /// <summary>
    /// 按筛选条件重建系统列表。
    /// </summary>
    /// <param name="preferredSystemId">优先保持选中的系统 Id。</param>
    private void RebuildFilteredList(string preferredSystemId)
    {
        _visibleSnapshots.Clear();
        _systemList.Clear();

        var selectedIndex = -1;
        for (var i = 0; i < _snapshots.Count; i++)
        {
            var snapshot = _snapshots[i];
            if (!MatchesFilters(snapshot))
            {
                continue;
            }

            var visibleIndex = _visibleSnapshots.Count;
            _visibleSnapshots.Add(snapshot);
            _systemList.AddItem($"{GetStatusGlyph(snapshot.Status)} {snapshot.SystemId}");
            _systemList.SetItemMetadata(visibleIndex, snapshot.SystemId); // 列表项绑定完整 SystemId
            _systemList.SetItemTooltip(visibleIndex, snapshot.Description);

            if (string.Equals(snapshot.SystemId, preferredSystemId, StringComparison.Ordinal))
            {
                selectedIndex = visibleIndex;
            }
        }

        if (selectedIndex < 0 && _visibleSnapshots.Count > 0)
        {
            selectedIndex = 0;
        }

        if (selectedIndex >= 0)
        {
            _systemList.Select(selectedIndex);
        }

        UpdateSummary();
        UpdateSelectedSystemDetails();
    }

    /// <summary>
    /// 判断系统是否满足当前筛选条件。
    /// </summary>
    /// <param name="snapshot">系统展示快照。</param>
    private bool MatchesFilters(SystemInfoSnapshot snapshot)
    {
        var groupFilter = GetSelectedMetadata(_groupOption);
        if (!string.IsNullOrEmpty(groupFilter)
            && !string.Equals(snapshot.MountGroup.ToString(), groupFilter, StringComparison.Ordinal))
        {
            return false;
        }

        var tagFilter = GetSelectedMetadata(_tagOption);
        if (!string.IsNullOrEmpty(tagFilter)
            && Enum.TryParse<SystemTag>(tagFilter, out var tag)
            && (snapshot.Tags & tag) == 0)
        {
            return false;
        }

        var statusFilter = GetSelectedMetadata(_statusOption);
        if (!string.IsNullOrEmpty(statusFilter)
            && Enum.TryParse<SystemInfoStatus>(statusFilter, out var status)
            && snapshot.Status != status)
        {
            return false;
        }

        var search = _searchEdit.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(search))
        {
            return true;
        }

        return snapshot.SystemId.Contains(search, StringComparison.OrdinalIgnoreCase)
               || snapshot.Description.Contains(search, StringComparison.OrdinalIgnoreCase)
               || snapshot.Tags.ToString().Contains(search, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 更新汇总文案。
    /// </summary>
    private void UpdateSummary()
    {
        var running = 0;
        var blocked = 0;
        var disabled = 0;
        var notLoaded = 0;
        foreach (var snapshot in _snapshots)
        {
            if (snapshot.Status == SystemInfoStatus.Running)
            {
                running++;
            }
            else if (snapshot.Status == SystemInfoStatus.Blocked)
            {
                blocked++;
            }
            else if (snapshot.Status == SystemInfoStatus.Disabled)
            {
                disabled++;
            }
            else if (snapshot.Status == SystemInfoStatus.NotLoaded)
            {
                notLoaded++;
            }
        }

        _summaryLabel.Text =
            $"系统 {_snapshots.Count} 个，当前显示 {_visibleSnapshots.Count} 个，运行 {running}，阻塞 {blocked}，禁用 {disabled}，未加载 {notLoaded}";
    }

    /// <summary>
    /// 刷新当前选中系统详情。
    /// </summary>
    private void UpdateSelectedSystemDetails()
    {
        var snapshot = GetSelectedSnapshot();
        if (snapshot == null)
        {
            _overviewLabel.Text = "请选择一个系统";
            _detailsLabel.Text = "没有可显示的系统详情";
            SetActionButtons(null);
            _statusLabel.Text = "未选择系统";
            return;
        }

        _overviewLabel.Text =
            $"系统：{snapshot.SystemId}\n" +
            $"状态：{GetStatusDisplayName(snapshot.Status)}\n" +
            $"注册：{FormatBool(snapshot.IsRegistered)}\n" +
            $"加载：{FormatBool(snapshot.IsLoaded)}\n" +
            $"启用：{FormatBool(snapshot.IsEnabled)}\n" +
            $"运行：{FormatBool(snapshot.IsRunning)}\n" +
            $"状态门禁：{FormatBool(snapshot.IsStateAllowed)}\n" +
            $"阻塞原因：{FormatEmpty(snapshot.BlockedReason)}";

        _detailsLabel.Text = BuildDetailsText(snapshot);
        SetActionButtons(snapshot);
        _statusLabel.Text = $"已选择 {snapshot.SystemId}";
    }

    private string BuildDetailsText(SystemInfoSnapshot snapshot)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"挂载分组：{snapshot.MountGroup}");
        builder.AppendLine($"标签：{snapshot.Tags}");
        builder.AppendLine($"必需系统：{FormatBool(snapshot.Required)}");
        builder.AppendLine($"自动加载：{FormatBool(snapshot.AutoLoad)}");
        builder.AppendLine($"初始启用：{FormatBool(snapshot.StartEnabled)}");
        builder.AppendLine($"优先级：{snapshot.Priority}");
        builder.AppendLine($"依赖：{FormatDependencies(snapshot.Dependencies)}");
        builder.AppendLine($"被依赖：{FormatEmpty(snapshot.DependentSystemId)}");
        builder.AppendLine();
        builder.AppendLine("说明：");
        builder.AppendLine(FormatEmpty(snapshot.Description));
        builder.AppendLine();
        builder.AppendLine("自定义统计：");

        if (snapshot.CustomStats.Count == 0)
        {
            builder.AppendLine("无");
            return builder.ToString();
        }

        foreach (var stat in snapshot.CustomStats)
        {
            builder.AppendLine($"{FormatEmpty(stat.Category)} / {stat.Name}：{stat.Value}");
        }

        return builder.ToString();
    }

    private void SetActionButtons(SystemInfoSnapshot? snapshot)
    {
        _addButton.Disabled = snapshot == null || !snapshot.CanAdd;
        _enableButton.Disabled = snapshot == null || !snapshot.CanEnable;
        _disableButton.Disabled = snapshot == null || !snapshot.CanDisable;
        _removeButton.Disabled = snapshot == null || !snapshot.CanRemove;
    }

    private void OnAddPressed()
    {
        ExecuteSelectedSystemAction(snapshot => _service.AddSystem(
            SystemManager.Instance, // 当前 SystemManager
            snapshot.SystemId // 目标系统 Id
        ));
    }

    private void OnEnablePressed()
    {
        ExecuteSelectedSystemAction(snapshot => _service.SetSystemEnabled(
            SystemManager.Instance, // 当前 SystemManager
            snapshot.SystemId, // 目标系统 Id
            true // 目标启用状态
        ));
    }

    private void OnDisablePressed()
    {
        ExecuteSelectedSystemAction(snapshot => _service.SetSystemEnabled(
            SystemManager.Instance, // 当前 SystemManager
            snapshot.SystemId, // 目标系统 Id
            false // 目标启用状态
        ));
    }

    private void OnRemovePressed()
    {
        ExecuteSelectedSystemAction(snapshot => _service.RemoveSystem(
            SystemManager.Instance, // 当前 SystemManager
            snapshot.SystemId // 目标系统 Id
        ));
    }

    private void ExecuteSelectedSystemAction(Func<SystemInfoSnapshot, TestActionResult> action)
    {
        var snapshot = GetSelectedSnapshot();
        if (snapshot == null)
        {
            _statusLabel.Text = "请先选择一个系统";
            return;
        }

        var result = action(snapshot);
        if (result.Success)
        {
            _log.Info(result.Message);
        }
        else
        {
            _log.Warn(result.Message);
        }

        _statusLabel.Text = result.Message;
        ReloadSnapshotsKeeping(snapshot.SystemId);
    }

    private void ReloadSnapshotsKeeping(string systemId)
    {
        _snapshots.Clear();
        _snapshotsBySystemId.Clear();
        var snapshots = _service.GetSnapshots(SystemManager.Instance);
        foreach (var snapshot in snapshots)
        {
            _snapshots.Add(snapshot);
            _snapshotsBySystemId[snapshot.SystemId] = snapshot;
        }

        RebuildFilteredList(systemId);
    }

    private SystemInfoSnapshot? GetSelectedSnapshot()
    {
        var systemId = GetSelectedSystemId();
        return _snapshotsBySystemId.TryGetValue(systemId, out var snapshot) ? snapshot : null;
    }

    private string GetSelectedSystemId()
    {
        var selectedItems = _systemList.GetSelectedItems();
        if (selectedItems.Length == 0)
        {
            return string.Empty;
        }

        var index = selectedItems[0];
        return index < 0 || index >= _systemList.ItemCount
            ? string.Empty
            : _systemList.GetItemMetadata(index).AsString();
    }

    private static string GetSelectedMetadata(OptionButton option)
    {
        return option.Selected < 0 ? string.Empty : option.GetItemMetadata(option.Selected).AsString();
    }

    private static string GetStatusGlyph(SystemInfoStatus status)
    {
        return status switch
        {
            SystemInfoStatus.Running => "[运行]",
            SystemInfoStatus.Blocked => "[阻塞]",
            SystemInfoStatus.Disabled => "[禁用]",
            SystemInfoStatus.Loaded => "[已加载]",
            _ => "[未加载]"
        };
    }

    private static string GetStatusDisplayName(SystemInfoStatus status)
    {
        return status switch
        {
            SystemInfoStatus.Running => "运行中",
            SystemInfoStatus.Blocked => "状态阻塞",
            SystemInfoStatus.Disabled => "已禁用",
            SystemInfoStatus.Loaded => "已加载",
            _ => "未加载"
        };
    }

    private static string FormatBool(bool value)
    {
        return value ? "是" : "否";
    }

    private static string FormatEmpty(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "无" : value;
    }

    private static string FormatDependencies(string[] dependencies)
    {
        return dependencies.Length == 0 ? "无" : string.Join(", ", dependencies);
    }
}
