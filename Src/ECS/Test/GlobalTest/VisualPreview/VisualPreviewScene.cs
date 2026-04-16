using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// 独立视觉资源预览测试场景。
/// </summary>
public partial class VisualPreviewScene : Node2D
{
    private readonly VisualPreviewCatalogService _catalogService = new();
    private readonly VisualPreviewWorldController _world = new();
    private IReadOnlyList<VisualPreviewEntry> _entries = Array.Empty<VisualPreviewEntry>();

    private PanelContainer _panel = null!;
    private Button _togglePanelButton = null!;
    private OptionButton _catalogOption = null!;
    private OptionButton _animationOption = null!;
    private Button _refreshButton = null!;
    private Label _summaryLabel = null!;
    private Label _selectionLabel = null!;

    public override void _Ready()
    {
        CacheUi();
        BindUi();
        BindSelectionEvents();
        Reload();
    }

    public override void _ExitTree()
    {
        GlobalEventBus.Global.Off<GameEventType.Global.MouseSelectionCompletedEventData>(
            GameEventType.Global.MouseSelectionCompleted,
            OnMouseSelectionCompleted
        );
        GlobalEventBus.Global.Off<GameEventType.Global.MouseSelectionMissedEventData>(
            GameEventType.Global.MouseSelectionMissed,
            OnMouseSelectionMissed
        );
        _world.Clear();
    }

    /// <summary>
    /// 缓存固定 UI 节点。
    /// </summary>
    private void CacheUi()
    {
        _panel = GetNode<PanelContainer>("UILayer/Panel");
        _togglePanelButton = GetNode<Button>("UILayer/TogglePanelButton");
        _catalogOption = GetNode<OptionButton>("UILayer/Panel/Margin/Layout/CatalogOption");
        _animationOption = GetNode<OptionButton>("UILayer/Panel/Margin/Layout/AnimationOption");
        _refreshButton = GetNode<Button>("UILayer/Panel/Margin/Layout/RefreshButton");
        _summaryLabel = GetNode<Label>("UILayer/Panel/Margin/Layout/SummaryLabel");
        _selectionLabel = GetNode<Label>("UILayer/Panel/Margin/Layout/SelectionLabel");
    }

    /// <summary>
    /// 绑定 UI 事件。
    /// </summary>
    private void BindUi()
    {
        _togglePanelButton.Pressed += () => _panel.Visible = !_panel.Visible;
        _refreshButton.Pressed += Reload;
        _catalogOption.ItemSelected += _ => ApplyCatalogSelection();
        _animationOption.ItemSelected += _ => ApplyAnimationSelection();
    }

    /// <summary>
    /// 绑定鼠标选择事件。
    /// </summary>
    private void BindSelectionEvents()
    {
        GlobalEventBus.Global.On<GameEventType.Global.MouseSelectionCompletedEventData>(
            GameEventType.Global.MouseSelectionCompleted,
            OnMouseSelectionCompleted
        );
        GlobalEventBus.Global.On<GameEventType.Global.MouseSelectionMissedEventData>(
            GameEventType.Global.MouseSelectionMissed,
            OnMouseSelectionMissed
        );
    }

    /// <summary>
    /// 重新加载全部资源并重建预览。
    /// </summary>
    private void Reload()
    {
        _entries = _catalogService.GetEntries();
        _world.Rebuild(_entries);
        RebuildCatalogOptions();
        ApplyCatalogSelection();
        _selectionLabel.Text = "未选择";
    }

    /// <summary>
    /// 重建分类下拉。
    /// </summary>
    private void RebuildCatalogOptions()
    {
        _catalogOption.Clear();
        foreach (var catalogPath in _catalogService.GetCatalogPaths(_entries))
        {
            var index = _catalogOption.ItemCount;
            _catalogOption.AddItem(catalogPath);
            _catalogOption.SetItemMetadata(index, catalogPath); // 保存分类路径
        }

        _catalogOption.Disabled = _catalogOption.ItemCount == 0;
        if (_catalogOption.ItemCount > 0)
        {
            _catalogOption.Select(0);
        }
    }

    /// <summary>
    /// 重建动作下拉。
    /// </summary>
    /// <param name="catalogPath">当前分类路径。</param>
    private void RebuildAnimationOptions(string catalogPath)
    {
        _animationOption.Clear();
        foreach (var animationName in _world.GetAnimationUnion(catalogPath))
        {
            var index = _animationOption.ItemCount;
            _animationOption.AddItem(animationName);
            _animationOption.SetItemMetadata(index, animationName); // 保存动作名
        }

        _animationOption.Disabled = _animationOption.ItemCount == 0;
        if (_animationOption.ItemCount > 0)
        {
            _animationOption.Select(0);
        }
    }

    /// <summary>
    /// 应用分类选择。
    /// </summary>
    private void ApplyCatalogSelection()
    {
        var catalogPath = GetSelectedCatalogPath();
        _world.ShowCatalog(catalogPath);
        RebuildAnimationOptions(catalogPath);
        ApplyAnimationSelection();
        _selectionLabel.Text = "未选择";
        _summaryLabel.Text =
            $"分类：{(string.IsNullOrWhiteSpace(catalogPath) ? "无" : catalogPath)}\n" +
            $"当前分类资源：{_world.CountByCatalog(catalogPath)}\n" +
            $"全部 Asset 资源：{_entries.Count}";
    }

    /// <summary>
    /// 应用动作选择。
    /// </summary>
    private void ApplyAnimationSelection()
    {
        _world.ApplyAnimation(
            GetSelectedCatalogPath(), // 当前分类
            GetSelectedAnimationName() // 当前动作
        );
    }

    /// <summary>
    /// 获取当前分类路径。
    /// </summary>
    private string GetSelectedCatalogPath()
    {
        var selected = _catalogOption.Selected;
        return selected < 0 ? string.Empty : _catalogOption.GetItemMetadata(selected).AsString();
    }

    /// <summary>
    /// 获取当前动作名。
    /// </summary>
    private string GetSelectedAnimationName()
    {
        var selected = _animationOption.Selected;
        return selected < 0 ? string.Empty : _animationOption.GetItemMetadata(selected).AsString();
    }

    /// <summary>
    /// 鼠标选择完成。
    /// </summary>
    /// <param name="evt">选择事件数据。</param>
    private void OnMouseSelectionCompleted(GameEventType.Global.MouseSelectionCompletedEventData evt)
    {
        if (!_world.TryGetEntry(evt.PrimaryEntity, out var entry)
            || !string.Equals(entry.CatalogPath, GetSelectedCatalogPath(), StringComparison.Ordinal))
        {
            return;
        }

        var entity = (VisualPreviewEntity)evt.PrimaryEntity!;
        _selectionLabel.Text =
            $"名称：{entity.Data.Get<string>(DataKey.Name)}\n" +
            $"资源键：{entry.ResourceKey}\n" +
            $"路径：{entry.ResourcePath}\n" +
            $"分类：{entry.Category}\n" +
            $"默认动作：{FormatEmpty(entity.Data.Get<string>(DataKey.PreviewDefaultAnimation))}\n" +
            $"当前动作：{FormatEmpty(entity.Data.Get<string>(DataKey.PreviewCurrentAnimation))}";
    }

    /// <summary>
    /// 鼠标选择未命中。
    /// </summary>
    /// <param name="evt">选择事件数据。</param>
    private void OnMouseSelectionMissed(GameEventType.Global.MouseSelectionMissedEventData evt)
    {
        _selectionLabel.Text = "未选择";
    }

    /// <summary>
    /// 格式化空文本。
    /// </summary>
    /// <param name="value">原始文本。</param>
    private static string FormatEmpty(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "无" : value;
    }
}
