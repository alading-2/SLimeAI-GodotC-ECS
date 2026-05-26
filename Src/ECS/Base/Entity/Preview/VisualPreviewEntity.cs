using Godot;

/// <summary>
/// 视觉预览实体。
/// <para>只作为可被鼠标选择的 Entity 壳，具体视觉由 VisualRoot 注入。</para>
/// </summary>
public partial class VisualPreviewEntity : Node2D, IEntity
{
    public Data Data { get; private set; }
    public EventBus Events { get; } = new EventBus();

    public VisualPreviewEntity()
    {
        Data = new Data(this);
    }
}
