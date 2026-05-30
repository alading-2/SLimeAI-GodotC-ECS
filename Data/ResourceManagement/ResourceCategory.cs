
/// <summary>
/// 资源分类枚举 - 用于对资源进行分组管理
/// </summary>
public enum ResourceCategory
{
    /// <summary>Entity</summary>
    Entity,
    /// <summary>Component</summary>
    Component,
    /// <summary>UI</summary>
    UI,
    /// <summary>Asset 资源（未匹配子分类时的兜底）</summary>
    Asset,
    /// <summary>AssetEffect 特效动画资源（assets/Effect/）</summary>
    AssetEffect,
    /// <summary>AssetUnit 单位动画资源（assets/Unit/）</summary>
    AssetUnit,
    /// <summary>AssetUnitEnemy 敌人动画资源（assets/Unit/Enemy/）</summary>
    AssetUnitEnemy,
    /// <summary>AssetUnitPlayer 玩家动画资源（assets/Unit/Player/）</summary>
    AssetUnitPlayer,
    /// <summary>AssetProjectile 弹道动画资源（assets/Projectile/）</summary>
    AssetProjectile,

    /// <summary>System 系统 (如 SpawnSystem)</summary>
    System,
    /// <summary>Manager 管理器 (如 TimerManager)</summary>
    Tools,

    /// <summary>DataOS snapshot 数据目录兜底</summary>
    Data,
    /// <summary>DataOS snapshot 技能数据目录</summary>
    DataAbility,
    /// <summary>DataOS snapshot 单位数据目录</summary>
    DataUnit,
    /// <summary>Test 测试资源</summary>
    Test,

    /// <summary>Other 其他</summary>
    Other,
}
