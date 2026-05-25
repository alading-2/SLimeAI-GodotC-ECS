using Godot;
using System;

/// <summary>
/// TestSystem 对通用鼠标选择结果事件的适配。
/// </summary>
public partial class TestSystem
{
    private IDisposable? _mouseSelectionCompletedSubscription;
    private IDisposable? _selectionChangedSubscription;

    /// <summary>
    /// 绑定通用鼠标选择系统的结果事件。
    /// </summary>
    private void BindMouseSelectionEvents()
    {
        _mouseSelectionCompletedSubscription ??=
            WorldEvents.World.Subscribe<GlobalEvents.MouseSelectionCompleted>(OnMouseSelectionCompleted);
    }

    /// <summary>
    /// 解绑通用鼠标选择系统的结果事件。
    /// </summary>
    private void UnbindMouseSelectionEvents()
    {
        _mouseSelectionCompletedSubscription?.Dispose();
        _mouseSelectionCompletedSubscription = null;
    }

    /// <summary>
    /// 通用鼠标选择完成后，把结果回写到 TestSystem 当前选中实体。
    /// </summary>
    private void OnMouseSelectionCompleted(GlobalEvents.MouseSelectionCompleted evt)
    {
        if (!ShouldAcceptMouseSelection())
        {
            return;
        }

        SetSelectedEntity(evt.PrimaryEntity ?? (evt.Entities.Count > 0 ? evt.Entities[0] : null));
    }

    /// <summary>
    /// 判断 TestSystem 是否应该消费本次全局鼠标选择结果。
    /// </summary>
    private bool ShouldAcceptMouseSelection()
    {
        return _panelVisible && _selectionToggle.ButtonPressed;
    }

    //======================选中实体变化事件逻辑===============================

    /// <summary>
    /// 绑定选中实体变化事件
    /// </summary>
    private void BindSelectionContextEvents()
    {
        _selectionChangedSubscription ??=
            Events.Subscribe<TestSystemEvents.SelectionChanged>(OnSelectionChanged);
    }

    /// <summary>
    /// 解绑选中实体变化事件。
    /// </summary>
    private void UnbindSelectionContextEvents()
    {
        _selectionChangedSubscription?.Dispose();
        _selectionChangedSubscription = null;
    }

    /// <summary>
    /// 选中实体变化后的统一广播入口。
    /// </summary>
    private void OnSelectionChanged(TestSystemEvents.SelectionChanged evt)
    {
        // 显示选中实体名字+ID
        UpdateSelectedEntityDisplay();
        // 当前激活模块触发选中实体变化事件
        if (_currentModule != null)
        {
            _currentModule.OnSelectedEntityChanged(evt.Entity);
        }
    }

    /// <summary>
    /// 将当前选中实体显示到顶部信息栏。
    /// <para>
    /// 若实体没有名称数据，则回退到节点名，确保调试 UI 始终有可读信息。
    /// </para>
    /// </summary>
    private void UpdateSelectedEntityDisplay()
    {
        if (SelectedEntity is not Node node)
        {
            _selectedEntityLabel.Text = "未选择";
            return;
        }

        var name = SelectedEntity.Data.Get(DataKey.Name);
        if (string.IsNullOrWhiteSpace(name))
        {
            name = node.Name.ToString();
        }

        var id = SelectedEntity.Data.Get(DataKey.Id);
        _selectedEntityLabel.Text = $"{name} | {node.GetType().Name} | {id}";
    }
}
