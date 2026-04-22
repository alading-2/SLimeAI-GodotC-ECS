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
    Base        = 1 << 0,  // 基础系统（优先级最高，强制加载）
    Combat      = 1 << 1,  // 战斗系统
    Movement    = 1 << 2,  // 移动系统
    AI          = 1 << 3,  // AI 系统
    UI          = 1 << 4,  // UI 系统
    Audio       = 1 << 5,  // 音频系统
    VFX         = 1 << 6,  // 特效系统
    Loot        = 1 << 7,  // 掉落系统
    Progression = 1 << 8,  // 进度系统
    Debug       = 1 << 9,  // 调试系统
    Test        = 1 << 10, // 测试系统
    Else        = 1 << 63, // 未分组系统（兜底）
}
