/// <summary>
/// 发出事件动作 - 通过 Owner 或 Feature 的局部 EventBus 发出一个 typed 事件
///
/// 适合触发跨系统通信，如通知 UI 更新、触发音效、通知 AI 等。
/// </summary>
public class EmitEventAction<TEvent> : IFeatureAction where TEvent : struct
{
    /// <summary>要发出的 typed 事件数据（类型即事件标识）。</summary>
    public TEvent EventData { get; set; }

    /// <summary>在 Owner 的事件总线上发出（true = Owner，false = Feature）</summary>
    public bool EmitOnOwner { get; set; } = true;

    public void Execute(FeatureContext ctx)
    {
        if (EmitOnOwner)
            ctx.Owner?.Events.Emit(EventData);
        else
            ctx.Feature?.Events.Emit(EventData);
    }
}
