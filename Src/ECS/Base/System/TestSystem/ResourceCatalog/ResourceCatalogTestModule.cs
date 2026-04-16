using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// 资源目录测试模块。
/// <para>
/// 用于在 TestSystem 里直接检查 ResourceCatalog 当前能解析出的全部资源分类与资源条目。
/// </para>
/// </summary>
public partial class ResourceCatalogTestModule : TestModuleBase
{
    private readonly Dictionary<string, ResourceCatalogEntry> _entriesByTreeKey = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ResourceCatalogGroup> _groupsByCatalogPath = new(StringComparer.Ordinal);

    private Label _summaryLabel = null!;
    private OptionButton _categoryOption = null!;
    private Button _showCategoryButton = null!;
    private Button _reloadButton = null!;
    private Tree _catalogTree = null!;
    private Label _detailsLabel = null!;
    private Label _statusLabel = null!;

    internal override TestModuleDefinition Definition => new(
        "resource-catalog", // 模块稳定 Id
        $"{TestModuleGroupId.Resource}.资源目录" // 模块导航路径
    );

    internal override void Initialize(ITestModuleContext context)
    {
        base.Initialize(context);
        CacheUiNodes();
        BindUiEvents();
        ReloadCatalog(false); // 初次进入时使用现有缓存，避免重复加载资源
    }

    internal override void OnActivated()
    {
        base.OnActivated();
        ReloadCatalog(false); // 切回模块时刷新视图，保持和 ResourceCatalog 缓存一致
    }

    internal override void Refresh()
    {
        ReloadCatalog(false); // TestSystem 顶部刷新按钮只重建视图，不强制清缓存
    }

    /// <summary>
    /// 缓存场景中的固定 UI 节点。
    /// </summary>
    private void CacheUiNodes()
    {
        MouseFilter = Control.MouseFilterEnum.Stop;
        _summaryLabel = GetNode<Label>("SummaryLabel");
        _categoryOption = GetNode<OptionButton>("Actions/CategoryOption");
        _showCategoryButton = GetNode<Button>("Actions/ShowCategoryButton");
        _reloadButton = GetNode<Button>("Actions/ReloadButton");
        _catalogTree = GetNode<Tree>("CatalogTree");
        _detailsLabel = GetNode<Label>("DetailsLabel");
        _statusLabel = GetNode<Label>("StatusLabel");
        _catalogTree.HideRoot = true;
    }

    /// <summary>
    /// 绑定资源目录测试模块的交互事件。
    /// </summary>
    private void BindUiEvents()
    {
        _categoryOption.ItemSelected += OnCategorySelected; // 分类下拉框：只切换当前待查看分类
        _showCategoryButton.Pressed += OnShowCategoryPressed; // 显示按钮：只展开当前分类资源
        _reloadButton.Pressed += OnReloadPressed; // 刷新按钮：清理 ResourceCatalog 缓存后重建列表
        _catalogTree.ItemSelected += OnCatalogItemSelected; // 树节点选择：显示资源或分类详情
    }

    /// <summary>
    /// 重新加载并渲染 ResourceCatalog 当前的分类和资源。
    /// </summary>
    /// <param name="clearCache">是否先清理 ResourceCatalog 缓存。</param>
    private void ReloadCatalog(bool clearCache)
    {
        if (clearCache)
        {
            ResourceCatalog.ClearCache();
        }

        _entriesByTreeKey.Clear();
        _groupsByCatalogPath.Clear();

        var groups = ResourceCatalog.GetGroups();
        var resourceCount = 0;
        foreach (var group in groups)
        {
            resourceCount += group.Items.Count;
            _groupsByCatalogPath[group.CatalogPath] = group;
        }

        RebuildCategoryOptions();
        ShowCategoryOverview(resourceCount);
        _detailsLabel.Text = "选择分类后点击“显示分类资源”查看该分类资源";
        _statusLabel.Text = clearCache ? "已清理缓存并重新读取资源分类" : "已读取资源分类";
    }

    /// <summary>
    /// 重建分类下拉选项。
    /// </summary>
    private void RebuildCategoryOptions()
    {
        var previousCatalogPath = GetSelectedCatalogPath();
        _categoryOption.Clear();

        foreach (var catalogPath in _groupsByCatalogPath.Keys)
        {
            var index = _categoryOption.ItemCount;
            var group = _groupsByCatalogPath[catalogPath];
            _categoryOption.AddItem($"{catalogPath} ({group.Items.Count})");
            _categoryOption.SetItemMetadata(index, catalogPath); // 保存完整 CatalogPath，按钮点击时按分类查询
            if (string.Equals(catalogPath, previousCatalogPath, StringComparison.Ordinal))
            {
                _categoryOption.Select(index);
            }
        }

        if (_categoryOption.Selected < 0 && _categoryOption.ItemCount > 0)
        {
            _categoryOption.Select(0);
        }
    }

    /// <summary>
    /// 只显示分类总览，不展开具体资源。
    /// </summary>
    /// <param name="resourceCount">当前 ResourceCatalog 中的资源总数。</param>
    private void ShowCategoryOverview(int resourceCount)
    {
        _entriesByTreeKey.Clear();
        _catalogTree.Clear();

        var root = _catalogTree.CreateItem();
        foreach (var group in _groupsByCatalogPath.Values)
        {
            var groupItem = _catalogTree.CreateItem(root);
            groupItem.SetText(0, $"{group.CatalogPath} ({group.Items.Count})");
            groupItem.SetMetadata(0, BuildGroupTreeKey(group.CatalogPath)); // 分类节点用 group: 前缀标识
        }

        _summaryLabel.Text = $"分类 {_groupsByCatalogPath.Count} 个，资源 {resourceCount} 个，当前仅显示分类";
    }

    /// <summary>
    /// 只显示指定分类下的资源。
    /// </summary>
    /// <param name="catalogPath">要显示的 CatalogPath。</param>
    private void ShowCategoryResources(string catalogPath)
    {
        if (!_groupsByCatalogPath.TryGetValue(catalogPath, out var group))
        {
            _statusLabel.Text = "请选择一个有效分类";
            return;
        }

        _entriesByTreeKey.Clear();
        _catalogTree.Clear();

        var root = _catalogTree.CreateItem();
        var groupItem = _catalogTree.CreateItem(root);
        groupItem.SetText(0, $"{group.CatalogPath} ({group.Items.Count})");
        groupItem.SetMetadata(0, BuildGroupTreeKey(group.CatalogPath)); // 分类节点用 group: 前缀标识

        foreach (var entry in group.Items)
        {
            var treeKey = BuildResourceTreeKey(entry);
            _entriesByTreeKey[treeKey] = entry;

            var entryItem = _catalogTree.CreateItem(groupItem);
            entryItem.SetText(0, $"{entry.DisplayName}  [{entry.ResourceKey}]");
            entryItem.SetMetadata(0, treeKey); // 资源节点用 resource: 前缀标识
            entryItem.SetTooltipText(0, entry.Path);
        }

        groupItem.SetCollapsed(false);
        _summaryLabel.Text = $"当前分类 {group.CatalogPath}，资源 {group.Items.Count} 个";
        ShowGroupDetails(group);
    }

    /// <summary>
    /// 生成分类树节点的元数据键。
    /// </summary>
    /// <param name="catalogPath">ResourceCatalog 推导出的点分分类路径。</param>
    private static string BuildGroupTreeKey(string catalogPath)
    {
        return $"group:{catalogPath}";
    }

    /// <summary>
    /// 生成资源树节点的元数据键。
    /// </summary>
    /// <param name="entry">资源目录条目。</param>
    private static string BuildResourceTreeKey(ResourceCatalogEntry entry)
    {
        return $"resource:{entry.Category}:{entry.ResourceKey}";
    }

    /// <summary>
    /// 从分类树节点元数据中还原 CatalogPath。
    /// </summary>
    /// <param name="treeKey">树节点元数据键。</param>
    private static string ExtractGroupCatalogPath(string treeKey)
    {
        const string prefix = "group:";
        return treeKey.StartsWith(prefix, StringComparison.Ordinal)
            ? treeKey[prefix.Length..]
            : string.Empty;
    }

    /// <summary>
    /// 获取下拉框当前选中的 CatalogPath。
    /// </summary>
    private string GetSelectedCatalogPath()
    {
        var selected = _categoryOption.Selected;
        return selected < 0 ? string.Empty : _categoryOption.GetItemMetadata(selected).AsString();
    }

    /// <summary>
    /// 将分类下拉框同步到指定 CatalogPath。
    /// </summary>
    /// <param name="catalogPath">要同步选中的分类路径。</param>
    private void SelectCategoryOption(string catalogPath)
    {
        for (int i = 0; i < _categoryOption.ItemCount; i++)
        {
            if (!string.Equals(_categoryOption.GetItemMetadata(i).AsString(), catalogPath, StringComparison.Ordinal))
            {
                continue;
            }

            _categoryOption.Select(i);
            return;
        }
    }

    /// <summary>
    /// 显示当前选中的分类或资源详情。
    /// </summary>
    private void OnCatalogItemSelected()
    {
        var selected = _catalogTree.GetSelected();
        if (selected == null)
        {
            return;
        }

        var treeKey = selected.GetMetadata(0).AsString();
        if (_entriesByTreeKey.TryGetValue(treeKey, out var entry))
        {
            ShowEntryDetails(entry);
            return;
        }

        var catalogPath = ExtractGroupCatalogPath(treeKey);
        if (_groupsByCatalogPath.TryGetValue(catalogPath, out var group))
        {
            SelectCategoryOption(catalogPath);
            ShowGroupDetails(group);
        }
    }

    /// <summary>
    /// 显示单个资源条目的完整目录信息。
    /// </summary>
    /// <param name="entry">当前选中的资源条目。</param>
    private void ShowEntryDetails(ResourceCatalogEntry entry)
    {
        _detailsLabel.Text =
            $"资源：{entry.DisplayName}\n" +
            $"Key：{entry.ResourceKey}\n" +
            $"CatalogPath：{entry.CatalogPath}\n" +
            $"Category：{entry.Category}\n" +
            $"ResourceType：{entry.ResourceType.Name}\n" +
            $"Path：{entry.Path}";
        _statusLabel.Text = $"已选择资源 {entry.ResourceKey}";
    }

    /// <summary>
    /// 显示单个资源分类的汇总信息。
    /// </summary>
    /// <param name="group">当前选中的资源分类。</param>
    private void ShowGroupDetails(ResourceCatalogGroup group)
    {
        _detailsLabel.Text =
            $"分类：{group.CatalogPath}\n" +
            $"资源数：{group.Items.Count}\n" +
            "点击“显示分类资源”查看该分类下的资源";
        _statusLabel.Text = $"已选择分类 {group.CatalogPath}";
    }

    /// <summary>
    /// 处理分类下拉框选择。
    /// </summary>
    /// <param name="index">当前选项索引。</param>
    private void OnCategorySelected(long index)
    {
        var catalogPath = GetSelectedCatalogPath();
        if (_groupsByCatalogPath.TryGetValue(catalogPath, out var group))
        {
            ShowGroupDetails(group);
        }
    }

    /// <summary>
    /// 处理显示分类资源按钮点击。
    /// </summary>
    private void OnShowCategoryPressed()
    {
        ShowCategoryResources(GetSelectedCatalogPath()); // 按当前分类显示资源，避免一次性展开全部资源
    }

    /// <summary>
    /// 处理刷新按钮点击。
    /// </summary>
    private void OnReloadPressed()
    {
        ReloadCatalog(true); // 手动刷新时强制清理缓存，验证最新 ResourcePaths 结果
    }
}
