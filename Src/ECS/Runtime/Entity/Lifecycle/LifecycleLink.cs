/// <summary>
/// LifecycleTree 中的一条 typed 生命周期链接。
/// </summary>
public readonly record struct LifecycleLink(
    EntityId ParentId,
    EntityId ChildId,
    ParentDestroyPolicy DestroyPolicy,
    int Priority = 0);
