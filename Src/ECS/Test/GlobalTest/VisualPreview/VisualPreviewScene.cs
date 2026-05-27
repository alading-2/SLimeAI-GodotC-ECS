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
        GlobalEventBus.Global.Off<GameEventType.Global.MouseSelectionCompleted>(OnMouseSelectionCompleted
        );
        GlobalEventBus.Global.Off<GameEventType.Global.MouseSelectionMissed>(OnMouseSelectionMissed
        );
        _world.Clear();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventMouseButton mouseButton
            || !mouseButton.Pressed
            || _animationOption.Disabled
            || !_animationOption.GetGlobalRect().HasPoint(GetViewport().GetMousePosition()))
        {
            return;
        }

        if (mouseButton.ButtonIndex == MouseButton.WheelUp)
        {
            StepAnimationSelection(-1);
            GetViewport().SetInputAsHandled();
        }
        else if (mouseButton.ButtonIndex == MouseButton.WheelDown)
        {
            StepAnimationSelection(1);
            GetViewport().SetInputAsHandled();
        }
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
        GlobalEventBus.Global.On<GameEventType.Global.MouseSelectionCompleted>(OnMouseSelectionCompleted
        );
        GlobalEventBus.Global.On<GameEventType.Global.MouseSelectionMissed>(OnMouseSelectionMissed
        );
    }

    /// <summary>
    /// 重新加载全部资源并重建预览。
    /// </summary>
    private void Reload()
    {
        var selectedCatalogPath = GetSelectedCatalogPath();
        var selectedAnimationName = GetSelectedAnimationName();
        _entries = _catalogService.GetEntries();
        _world.Rebuild(_entries);
        RebuildCatalogOptions(selectedCatalogPath); // 刷新后优先保留分类选择
        ApplyCatalogSelection(selectedAnimationName); // 刷新后优先保留动作选择
        _selectionLabel.Text = "未选择";
    }

    /// <summary>
    /// 重建分类下拉。
    /// </summary>
    private void RebuildCatalogOptions(string preferredCatalogPath)
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
            SelectOptionByMetadata(_catalogOption, preferredCatalogPath); // 尽量保留用户当前分类
        }
    }

    /// <summary>
    /// 重建动作下拉。
    /// </summary>
    /// <param name="catalogPath">当前分类路径。</param>
    private void RebuildAnimationOptions(string catalogPath, string preferredAnimationName)
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
            SelectOptionByMetadata(_animationOption, preferredAnimationName); // 尽量保留用户当前动作
        }
    }

    /// <summary>
    /// 应用分类选择。
    /// </summary>
    private void ApplyCatalogSelection(string preferredAnimationName = "")
    {
        var catalogPath = GetSelectedCatalogPath();
        _world.ShowCatalog(catalogPath);
        RebuildAnimationOptions(catalogPath, preferredAnimationName);
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
    private void OnMouseSelectionCompleted(GameEventType.Global.MouseSelectionCompleted evt)
    {
        if (!_world.TryGetEntry(evt.PrimaryEntity, out var entry)
            || !string.Equals(entry.CatalogPath, GetSelectedCatalogPath(), StringComparison.Ordinal))
        {
            return;
        }

        var entity = (VisualPreviewEntity)evt.PrimaryEntity!;
        _selectionLabel.Text =
            $"名称：{entity.Data.Get<string>(DataKey.Name)}\n" +
            "\n" +
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
    private void OnMouseSelectionMissed(GameEventType.Global.MouseSelectionMissed evt)
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

    /// <summary>
    /// 按偏移量切换动作下拉选项，便于滚轮快速预览。
    /// </summary>
    /// <param name="step">切换步进，正数向后，负数向前。</param>
    private void StepAnimationSelection(int step)
    {
        if (_animationOption.ItemCount <= 0)
        {
            return;
        }

        var current = _animationOption.Selected;
        if (current < 0)
        {
            current = 0;
        }

        var next = Mathf.Wrap(current + step, 0, _animationOption.ItemCount);
        _animationOption.Select(next);
        ApplyAnimationSelection();
    }

    /// <summary>
    /// 按 metadata 选择下拉项，不存在时回退到第一个。
    /// </summary>
    /// <param name="optionButton">目标下拉框。</param>
    /// <param name="metadataValue">期望保留的 metadata。</param>
    private static void SelectOptionByMetadata(OptionButton optionButton, string metadataValue)
    {
        if (optionButton.ItemCount <= 0)
        {
            return;
        }

        for (var i = 0; i < optionButton.ItemCount; i++)
        {
            if (string.Equals(optionButton.GetItemMetadata(i).AsString(), metadataValue, StringComparison.Ordinal))
            {
                optionButton.Select(i);
                return;
            }
        }

        optionButton.Select(0);
    }
}
