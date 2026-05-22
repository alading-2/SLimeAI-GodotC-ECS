using System.Collections.Generic;

/// <summary>
/// 数据键定义 - 单位域
/// </summary>
public static partial class DataKey
{
    // === 基础信息 ===
    // 等级
    public static readonly DataKey<int> Level = DataRegistry.Register<int>(
        new DataMeta { Key = nameof(Level), DisplayName = "等级", Description = "实体的等级", Category = DataCategory_Base.Basic, Type = typeof(int), DefaultValue = 1, MinValue = 1, MaxValue = GlobalConfig.Maxlevel  });

    // 视觉场景路径（玩家角色、特效等）
    public static readonly DataKey<string> VisualScenePath = DataRegistry.Register<string>(
        new DataMeta { Key = nameof(VisualScenePath), DisplayName = "视觉场景路径", Description = "res:// 场景路径", Category = DataCategory_Base.Basic, Type = typeof(string), DefaultValue = ""  });

    // 单位品阶
    public static readonly DataKey<UnitRank> UnitRank = DataRegistry.Register<UnitRank>(
        new DataMeta { Key = nameof(UnitRank), DisplayName = "单位品阶", Description = "单位品阶", Category = DataCategory_Base.Basic, Type = typeof(UnitRank), DefaultValue = global::UnitRank.Normal  });

    // 是否显示血条
    public static readonly DataKey<bool> IsShowHealthBar = DataRegistry.Register<bool>(
        new DataMeta { Key = nameof(IsShowHealthBar), DisplayName = "是否显示血条", Description = "是否显示血条", Category = DataCategory_Base.Basic, Type = typeof(bool), DefaultValue = true  });

    // === 恢复控制 ===
    // 是否禁止生命恢复
    public static readonly DataKey<bool> IsDisableHealthRecovery = DataRegistry.Register<bool>(
        new DataMeta { Key = nameof(IsDisableHealthRecovery), DisplayName = "是否禁止生命恢复", Description = "是否禁止生命恢复", Category = DataCategory_Unit.Recovery, Type = typeof(bool), DefaultValue = false  });

    // 是否禁止魔法恢复
    public static readonly DataKey<bool> IsDisableManaRecovery = DataRegistry.Register<bool>(
        new DataMeta { Key = nameof(IsDisableManaRecovery), DisplayName = "是否禁止魔法恢复", Description = "是否禁止魔法恢复", Category = DataCategory_Unit.Recovery, Type = typeof(bool), DefaultValue = false  });

    // === Spawn ===
    // 是否启用SpawnRule
    public static readonly DataKey<bool> IsEnableSpawnRule = DataRegistry.Register<bool>(
        new DataMeta { Key = nameof(IsEnableSpawnRule), DisplayName = "是否启用SpawnRule", Category = DataCategory_Unit.Spawn, Type = typeof(bool), DefaultValue = true  });

    // 生成策略
    public static readonly DataKey<SpawnPositionStrategy> SpawnStrategy = DataRegistry.Register<SpawnPositionStrategy>(
        new DataMeta { Key = nameof(SpawnStrategy), DisplayName = "生成策略", Category = DataCategory_Unit.Spawn, Type = typeof(SpawnPositionStrategy), DefaultValue = SpawnPositionStrategy.Rectangle  });

    // 最小波次
    public static readonly DataKey<int> SpawnMinWave = DataRegistry.Register<int>(
        new DataMeta { Key = nameof(SpawnMinWave), DisplayName = "最小波次", Category = DataCategory_Unit.Spawn, Type = typeof(int), DefaultValue = 0  });

    // 最大波次
    public static readonly DataKey<int> SpawnMaxWave = DataRegistry.Register<int>(
        new DataMeta { Key = nameof(SpawnMaxWave), DisplayName = "最大波次", Category = DataCategory_Unit.Spawn, Type = typeof(int), DefaultValue = -1  });

    // 生成间隔
    public static readonly DataKey<float> SpawnInterval = DataRegistry.Register<float>(
        new DataMeta { Key = nameof(SpawnInterval), DisplayName = "生成间隔", Category = DataCategory_Unit.Spawn, Type = typeof(float), DefaultValue = 1.0f  });

    // 单波最大数
    public static readonly DataKey<int> SpawnMaxCountPerWave = DataRegistry.Register<int>(
        new DataMeta { Key = nameof(SpawnMaxCountPerWave), DisplayName = "单波最大数", Category = DataCategory_Unit.Spawn, Type = typeof(int), DefaultValue = -1  });

    // 单次数量
    public static readonly DataKey<int> SingleSpawnCount = DataRegistry.Register<int>(
        new DataMeta { Key = nameof(SingleSpawnCount), DisplayName = "单次数量", Category = DataCategory_Unit.Spawn, Type = typeof(int), DefaultValue = 1  });

    // 数量波动
    public static readonly DataKey<int> SingleSpawnVariance = DataRegistry.Register<int>(
        new DataMeta { Key = nameof(SingleSpawnVariance), DisplayName = "数量波动", Category = DataCategory_Unit.Spawn, Type = typeof(int), DefaultValue = 0  });

    // 开始延迟
    public static readonly DataKey<float> SpawnStartDelay = DataRegistry.Register<float>(
        new DataMeta { Key = nameof(SpawnStartDelay), DisplayName = "开始延迟", Category = DataCategory_Unit.Spawn, Type = typeof(float), DefaultValue = 0f  });

    // 生成权重
    public static readonly DataKey<int> SpawnWeight = DataRegistry.Register<int>(
        new DataMeta { Key = nameof(SpawnWeight), DisplayName = "生成权重", Category = DataCategory_Unit.Spawn, Type = typeof(int), DefaultValue = 10  });

    // 血条显示高度
    public static readonly DataKey<float> HealthBarHeight = DataRegistry.Register<float>(
        new DataMeta { Key = nameof(HealthBarHeight), DisplayName = "血条显示高度", Category = DataCategory_Unit.Spawn, Type = typeof(float), DefaultValue = 100f  });

    // === Enemy ===
    // 击杀经验奖励
    public static readonly DataKey<int> ExpReward = DataRegistry.Register<int>(
        new DataMeta { Key = nameof(ExpReward), DisplayName = "击杀经验奖励", Category = DataCategory_Base.Basic, Type = typeof(int), DefaultValue = 1, MinValue = 0  });

    // === 状态标记 ===
    // 是否死亡
    public static readonly DataKey<bool> IsDead = DataRegistry.Register<bool>(
        new DataMeta { Key = nameof(IsDead), DisplayName = "是否死亡", Category = DataCategory_Unit.State, Type = typeof(bool), DefaultValue = false  });

    // 是否无敌
    public static readonly DataKey<bool> IsInvulnerable = DataRegistry.Register<bool>(
        new DataMeta { Key = nameof(IsInvulnerable), DisplayName = "是否无敌", Category = DataCategory_Unit.State, Type = typeof(bool), DefaultValue = false  });

    // 是否免疫
    public static readonly DataKey<bool> IsImmune = DataRegistry.Register<bool>(
        new DataMeta { Key = nameof(IsImmune), DisplayName = "是否免疫", Category = DataCategory_Unit.State, Type = typeof(bool), DefaultValue = false  });

    // 是否眩晕
    public static readonly DataKey<bool> IsStunned = DataRegistry.Register<bool>(
        new DataMeta { Key = nameof(IsStunned), DisplayName = "是否眩晕", Category = DataCategory_Unit.State, Type = typeof(bool), DefaultValue = false  });

    // 是否隐身
    public static readonly DataKey<bool> IsInvisible = DataRegistry.Register<bool>(
        new DataMeta { Key = nameof(IsInvisible), DisplayName = "是否隐身", Category = DataCategory_Unit.State, Type = typeof(bool), DefaultValue = false  });

    // 攻击状态
    public static readonly DataKey<global::AttackState> AttackState = DataRegistry.Register<global::AttackState>(
        new DataMeta { Key = nameof(AttackState), DisplayName = "攻击状态", Category = DataCategory_Unit.State, Type = typeof(global::AttackState), DefaultValue = global::AttackState.Idle  });

    // === 生命周期 ===
    // 生命周期状态
    public static readonly DataKey<LifecycleState> LifecycleState = DataRegistry.Register<LifecycleState>(
        new DataMeta { Key = nameof(LifecycleState), DisplayName = "生命周期状态", Category = DataCategory_Unit.State, Type = typeof(LifecycleState), DefaultValue = global::LifecycleState.Alive  });

    // 死亡类型
    public static readonly DataKey<DeathType> DeathType = DataRegistry.Register<DeathType>(
        new DataMeta { Key = nameof(DeathType), DisplayName = "死亡类型", Category = DataCategory_Unit.State, Type = typeof(DeathType), DefaultValue = global::DeathType.Normal  });

    // 是否可复活
    public static readonly DataKey<bool> CanRevive = DataRegistry.Register<bool>(
        new DataMeta { Key = nameof(CanRevive), DisplayName = "是否可复活", Category = DataCategory_Unit.State, Type = typeof(bool), DefaultValue = false  });

    // 死亡次数
    public static readonly DataKey<int> DeathCount = DataRegistry.Register<int>(
        new DataMeta { Key = nameof(DeathCount), DisplayName = "死亡次数", Category = DataCategory_Unit.State, Type = typeof(int), DefaultValue = 0  });

    // 最大生存时间
    public static readonly DataKey<float> MaxLifeTime = DataRegistry.Register<float>(
        new DataMeta { Key = nameof(MaxLifeTime), DisplayName = "最大生存时间", Category = DataCategory_Unit.State, Type = typeof(float), DefaultValue = -1f  });

    // === FollowComponent ===
    // 跟随速度
    public static readonly DataKey<float> FollowSpeed = DataRegistry.Register<float>(
        new DataMeta { Key = nameof(FollowSpeed), DisplayName = "跟随速度", Category = DataCategory_Unit.Movement, Type = typeof(float), DefaultValue = 100f  });

    // 停止距离
    public static readonly DataKey<float> StopDistance = DataRegistry.Register<float>(
        new DataMeta { Key = nameof(StopDistance), DisplayName = "停止距离", Category = DataCategory_Unit.Movement, Type = typeof(float), DefaultValue = 200f  });

    // === HurtboxComponent ===
    // 无敌计时器
    public static readonly DataKey<float> InvincibilityTimer = DataRegistry.Register<float>(
        new DataMeta { Key = nameof(InvincibilityTimer), DisplayName = "无敌计时器", Category = DataCategory_Unit.State, Type = typeof(float), DefaultValue = 0f  });

    // === 伤害统计（运行时） ===
    public static readonly DataKey<float> TotalDamageTaken = DataRegistry.Register<float>(
        new DataMeta { Key = nameof(TotalDamageTaken), DisplayName = "累计承受伤害", Category = DataCategory_Unit.State, Type = typeof(float), DefaultValue = 0f });

    public static readonly DataKey<float> WaveDamageTaken = DataRegistry.Register<float>(
        new DataMeta { Key = nameof(WaveDamageTaken), DisplayName = "本波承受伤害", Category = DataCategory_Unit.State, Type = typeof(float), DefaultValue = 0f });

    public static readonly DataKey<float> TotalDamageDealt = DataRegistry.Register<float>(
        new DataMeta { Key = nameof(TotalDamageDealt), DisplayName = "累计造成伤害", Category = DataCategory_Unit.State, Type = typeof(float), DefaultValue = 0f });

    public static readonly DataKey<float> WaveDamageDealt = DataRegistry.Register<float>(
        new DataMeta { Key = nameof(WaveDamageDealt), DisplayName = "本波造成伤害", Category = DataCategory_Unit.State, Type = typeof(float), DefaultValue = 0f });

    public static readonly DataKey<float> HighestSingleDamage = DataRegistry.Register<float>(
        new DataMeta { Key = nameof(HighestSingleDamage), DisplayName = "最高单次伤害", Category = DataCategory_Unit.State, Type = typeof(float), DefaultValue = 0f });

    public static readonly DataKey<int> TotalKills = DataRegistry.Register<int>(
        new DataMeta { Key = nameof(TotalKills), DisplayName = "累计击杀", Category = DataCategory_Unit.State, Type = typeof(int), DefaultValue = 0 });

    public static readonly DataKey<int> WaveKills = DataRegistry.Register<int>(
        new DataMeta { Key = nameof(WaveKills), DisplayName = "本波击杀", Category = DataCategory_Unit.State, Type = typeof(int), DefaultValue = 0 });

    public static readonly DataKey<int> TotalHits = DataRegistry.Register<int>(
        new DataMeta { Key = nameof(TotalHits), DisplayName = "累计命中", Category = DataCategory_Unit.State, Type = typeof(int), DefaultValue = 0 });

    public static readonly DataKey<int> WaveHits = DataRegistry.Register<int>(
        new DataMeta { Key = nameof(WaveHits), DisplayName = "本波命中", Category = DataCategory_Unit.State, Type = typeof(int), DefaultValue = 0 });

    public static readonly DataKey<int> TotalCriticalHits = DataRegistry.Register<int>(
        new DataMeta { Key = nameof(TotalCriticalHits), DisplayName = "累计暴击命中", Category = DataCategory_Unit.State, Type = typeof(int), DefaultValue = 0 });

    public static readonly DataKey<int> WaveCriticalHits = DataRegistry.Register<int>(
        new DataMeta { Key = nameof(WaveCriticalHits), DisplayName = "本波暴击命中", Category = DataCategory_Unit.State, Type = typeof(int), DefaultValue = 0 });

    // === 其他运行时 ===
    public static readonly DataKey<bool> IsRecoverySystemRegistered = DataRegistry.Register<bool>(
        new DataMeta { Key = nameof(IsRecoverySystemRegistered), DisplayName = "恢复系统已注册", Category = DataCategory_Unit.Recovery, Type = typeof(bool), DefaultValue = false });

    public static readonly DataKey<List<string>> AvailableAnimations = DataRegistry.Register<List<string>>(
        new DataMeta { Key = nameof(AvailableAnimations), DisplayName = "可用动画列表", Category = DataCategory_Unit.State, Type = typeof(List<string>), DefaultValue = new List<string>() });
}
