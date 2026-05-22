/// <summary>
/// UI 绑定层事件。
/// </summary>
public static class UIEvents
{
    public readonly record struct ActiveSkillSelected(int SlotIndex, string AbilityName) : IEntityEvent;
}
