/// <summary>
/// 系统 Id 枚举。
/// <para>用于 Dependencies、SystemPreset 等需要引用系统的地方。</para>
/// <para>手动维护，与实际系统类名保持一致。</para>
/// </summary>
public enum SystemId
{
    // 基础设施
    ObjectPoolInit,
    TimerManager,
    ProjectStateBridge,

    // 战斗系统
    DamageService,
    RecoverySystem,
    DamageStatisticsSystem,

    // 生成系统
    SpawnSystem,

    // 目标系统
    TargetingManager,

    // UI 系统
    UIManager,
    DamageNumberSystem,

    // 暂停菜单
    PauseMenuSystem,

    // 调试系统
    TestSystem,
    MouseSelectionSystem,
}
