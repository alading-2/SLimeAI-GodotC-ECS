using Godot;

/// <summary>
/// UI 相关事件定义
/// </summary>
public static partial class GameEventType
{
    public static class UI
    {
        // === 技能 UI ===
        /// <summary>主动技能选中切换，PlayerEntity事件，不要在AbilityEntity里面Emit</summary>
        public readonly record struct ActiveSkillSelected(int SlotIndex, string AbilityName);
    }
}
