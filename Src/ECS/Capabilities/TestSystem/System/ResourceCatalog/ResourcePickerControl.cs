using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// TestSystem 通用资源选择控件。
/// <para>
/// 负责展示 ResourceCatalog 条目，并提供目录前缀过滤、分组过滤和文本搜索。
/// </para>
/// </summary>
public partial class ResourcePickerControl : VBoxContainer
{
    /// <summary>资源选择变化事件，参数为 ResourcePaths 资源键。</summary>
    [Signal] public delegate void SelectionChangedEventHandler(string resourceKey);

    private LineEdit _searchBox = null!;
    private OptionButton _groupOption = null!;
    private ItemList _itemList = null!;
    private Label _summaryLabel = null!;

    private readonly List<ResourceCatalogEntry> _allEntries = new();
    private readonly List<ResourceCatalogEntry> _visibleEntries = new();
    private string[] _catalogPathPrefixes = [];
    private string _selectedResourceKey = string.Empty;
    private bool _suppressSelectionEvent;
    private bool _isConfigured;

    /// <summary>当前选中的资源键。</summary>
    public string SelectedResourceKey => _selectedResourceKey;

    public override void _Ready()
    {
        CacheUiNodes();
        BindUiEvents();
        if (_isConfigured)
        {
            Reload();
        }
    }

    /// <summary>
    /// 配置控件允许展示的资源目录前缀。
    /// </summary>
    /// <param name="catalogPathPrefixes">允许显示的目录前缀；为空时显示全部目录。</param>
    public void Configure(params string[] catalogPathPrefixes)
    {
        _catalogPathPrefixes = catalogPathPrefixes.Length == 0 ? [] : catalogPathPrefixes;
        _isConfigured = true;
        Reload();
    }

    /// <summary>
    /// 重新加载资源目录。
    /// </summary>
    public void Reload()
    {
        if (_itemList == null)
        {
            return;
        }

        _allEntries.Clear();
        _allEntries.AddRange(ResourceCatalog.GetEntries(_catalogPathPrefixes)); // 从通用目录服务读取资源条目
        RebuildGroupOptions();
        ApplyFilter();
    }

    /// <summary>
    /// 获取当前选中的资源目录条目。
    /// </summary>
    /// <param name="entry">输出当前选中的目录条目。</param>
    public bool TryGetSelectedEntry(out ResourceCatalogEntry entry)
    {
        if (string.IsNullOrWhiteSpace(_selectedResourceKey))
        {
            entry = default;
            return false;
        }

        foreach (var candidate in _allEntries)
        {
            if (string.Equals(candidate.ResourceKey, _selectedResourceKey, StringComparison.Ordinal))
            {
                entry = candidate;
                return true;
            }
        }

        entry = default;
        return false;
    }

    private void CacheUiNodes()
    {
        _searchBox = GetNode<LineEdit>("SearchBox");
        _groupOption = GetNode<OptionButton>("GroupOption");
        _itemList = GetNode<ItemList>("ItemList");
        _summaryLabel = GetNode<Label>("SummaryLabel");
    }

    private void BindUiEvents()
    {
        _searchBox.TextChanged += OnSearchTextChanged;
        _groupOption.ItemSelected += OnGroupSelected;
        _itemList.ItemSelected += OnItemSelected;
    }

    private void RebuildGroupOptions()
    {
        var selectedGroup = GetSelectedGroup();
        _groupOption.Clear();
        _groupOption.AddItem("全部分组");
        _groupOption.SetItemMetadata(0, string.Empty); // 空字符串表示不过滤分组

        var groups = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var entry in _allEntries)
        {
            groups.Add(entry.CatalogPath);
        }

        foreach (var groupId in groups)
        {
            var index = _groupOption.ItemCount;
            _groupOption.AddItem(groupId);
            _groupOption.SetItemMetadata(index, groupId); // 记录完整分组路径
            if (string.Equals(groupId, selectedGroup, StringComparison.Ordinal))
            {
                _groupOption.Select(index);
            }
        }

        if (_groupOption.Selected < 0)
        {
            _groupOption.Select(0);
        }
    }

    private void ApplyFilter()
    {
        var previousSelection = _selectedResourceKey;
        var selectedGroup = GetSelectedGroup();
        var searchText = _searchBox.Text?.Trim() ?? string.Empty;

        _visibleEntries.Clear();
        _itemList.Clear();

        foreach (var entry in _allEntries)
        {
            if (!string.IsNullOrWhiteSpace(selectedGroup)
                && !string.Equals(entry.CatalogPath, selectedGroup, StringComparison.Ordinal))
            {
                continue;
            }

            if (!MatchesSearch(entry, searchText))
            {
                continue;
            }

            var itemIndex = _itemList.ItemCount;
            _visibleEntries.Add(entry);
            _itemList.AddItem($"{entry.DisplayName}  [{entry.ResourceKey}]");
            _itemList.SetItemMetadata(itemIndex, entry.ResourceKey); // 绑定 ResourcePaths 资源键
            _itemList.SetItemTooltip(itemIndex, $"{entry.CatalogPath}\n{entry.Path}");
        }

        RestoreSelection(previousSelection);
        UpdateSummary();
    }

    private void RestoreSelection(string previousSelection)
    {
        _selectedResourceKey = string.Empty;
        for (int i = 0; i < _visibleEntries.Count; i++)
        {
            if (!string.Equals(_visibleEntries[i].ResourceKey, previousSelection, StringComparison.Ordinal))
            {
                continue;
            }

            _suppressSelectionEvent = true;
            _itemList.Select(i);
            _suppressSelectionEvent = false;
            _selectedResourceKey = previousSelection;
            return;
        }

        if (_visibleEntries.Count == 0)
        {
            return;
        }

        _suppressSelectionEvent = true;
        _itemList.Select(0);
        _suppressSelectionEvent = false;
        _selectedResourceKey = _visibleEntries[0].ResourceKey;
        EmitSignal(SignalName.SelectionChanged, _selectedResourceKey); // 通知默认选择变化
    }

    private string GetSelectedGroup()
    {
        var selected = _groupOption.Selected;
        if (selected < 0)
        {
            return string.Empty;
        }

        return _groupOption.GetItemMetadata(selected).AsString();
    }

    private static bool MatchesSearch(ResourceCatalogEntry entry, string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return true;
        }

        return entry.DisplayName.Contains(searchText, StringComparison.OrdinalIgnoreCase)
            || entry.ResourceKey.Contains(searchText, StringComparison.OrdinalIgnoreCase)
            || entry.CatalogPath.Contains(searchText, StringComparison.OrdinalIgnoreCase)
            || entry.Path.Contains(searchText, StringComparison.OrdinalIgnoreCase);
    }

    private void UpdateSummary()
    {
        _summaryLabel.Text = $"显示 {_visibleEntries.Count} / {_allEntries.Count} 个资源";
    }

    private void OnSearchTextChanged(string newText)
    {
        ApplyFilter();
    }

    private void OnGroupSelected(long index)
    {
        ApplyFilter();
    }

    private void OnItemSelected(long index)
    {
        var itemIndex = (int)index;
        if (itemIndex < 0 || itemIndex >= _visibleEntries.Count)
        {
            return;
        }

        _selectedResourceKey = _visibleEntries[itemIndex].ResourceKey;
        if (!_suppressSelectionEvent)
        {
            EmitSignal(SignalName.SelectionChanged, _selectedResourceKey); // 通知用户选择变化
        }
    }
}
