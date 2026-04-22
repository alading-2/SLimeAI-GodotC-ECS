using System;

/// <summary>
/// 系统标签（逻辑分类）。
/// <para>多选 Flags 枚举，用于游戏类型预设的批量开启和运行时筛选。</para>
/// </summary>
[Flags]
public enum SystemTag : ulong
{
    None         = 0,
    Core         = 1 << 0,  // 核心系统
    Roguelike    = 1 << 1,  // Roguelike 玩法
    Survival     = 1 << 2,  // 生存玩法
    Tower        = 1 << 3,  // 塔防玩法
    Multiplayer  = 1 << 4,  // 多人模式
    Singleplayer = 1 << 5,  // 单人模式
    Editor       = 1 << 6,  // 编辑器模式
    Runtime      = 1 << 7,  // 运行时模式
}
