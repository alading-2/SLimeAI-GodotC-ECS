using System;

/// <summary>
/// 系统分组（挂载位置）。
/// <para>多选 Flags 枚举，按位从小到大优先，取最低位作为挂载路径。</para>
/// <para>Base 分组优先级最高，强制加载，禁止移除。</para>
/// </summary>
[Flags]
public enum SystemGroup : ulong
{
    None        = 0,
    Base        = 1UL << 0,  // 基础系统（优先级最高，强制加载）
    Combat      = 1UL << 1,  // 战斗系统
    Movement    = 1UL << 2,  // 移动系统
    AI          = 1UL << 3,  // AI 系统
    UI          = 1UL << 4,  // UI 系统
    Audio       = 1UL << 5,  // 音频系统
    VFX         = 1UL << 6,  // 特效系统
    Loot        = 1UL << 7,  // 掉落系统
    Progression = 1UL << 8,  // 进度系统
    Debug       = 1UL << 9,  // 调试系统
    Test        = 1UL << 10, // 测试系统
    Else        = 1UL << 63, // 未分组系统（兜底）
}
