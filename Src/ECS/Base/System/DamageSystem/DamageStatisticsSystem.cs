using Godot;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

/// <summary>
/// 伤害统计系统 - 负责波次统计重置和击杀统计
/// <para>监听波次开始事件重置玩家的波次统计数据。</para>
/// <para>监听全局 Kill 事件记录击杀数。</para>
/// </summary>
public partial class DamageStatisticsSystem : Node, ISystem
{
    /// <summary>日志处理实例</summary>
    private static readonly Log _log = new(nameof(DamageStatisticsSystem));

    private bool _eventsBound;

    /// <summary>
    /// 自动注册到统一系统注册表。
    /// </summary>
    [ModuleInitializer]
    public static void Initialize()
    {
        SystemRegistry.Register(nameof(DamageStatisticsSystem),
            static () => ResourceManagement.Load<PackedScene>(nameof(DamageStatisticsSystem), ResourceCategory.System)
                .Instantiate());
    }

    public override void _EnterTree()
    {
        _log.Debug("伤害统计系统初始化完成");
    }

    public override void _ExitTree()
    {
        UnbindRuntimeEvents();
    }

    /// <summary>
    /// 波次开始时的处理逻辑
    /// </summary>
    /// <param name="data">包含当前波次索引等信息</param>
    private void OnWaveStarted(GameEventType.Global.WaveStarted data)
    {
        _log.Debug($"波次 {data.WaveIndex} 开始，执行统计数据重置");

        // 核心机制：重置玩家实体及其装备的武器/物品的波次数据
        // 敌人实体通常在波次结束或死亡时销毁，因此无需显式重置波次数据
        var players = EntityManager.GetEntitiesByType<PlayerEntity>();
        foreach (var player in players)
        {
            // 1. 重置玩家自身的波次统计
            ResetWaveStats(player.Data);

            // 2. 重置玩家装备的所有武器/物品的波次统计
            var playerId = player.Data.Get<string>(DataKey.Id) ?? string.Empty;
            var itemIds = EntityRelationshipManager.GetChildEntitiesByParentAndType(
                playerId, EntityRelationshipType.ENTITY_TO_ITEM);

            foreach (var itemId in itemIds)
            {
                var item = EntityManager.GetEntityById(itemId);
                if (item is IWeapon weapon)
                {
                    ResetWaveStats(weapon.Data);
                }
            }
        }
    }

    /// <summary>
    /// 执行具体的数据重置操作
    /// </summary>
    /// <param name="data">实体的动态数据容器</param>
    private void ResetWaveStats(Data data)
    {
        data.Set(DataKey.WaveDamageDealt, 0f); // 重置波次造成伤害
        data.Set(DataKey.WaveDamageTaken, 0f); // 重置波次承受伤害
        data.Set(DataKey.WaveHits, 0); // 重置波次命中次数
        data.Set(DataKey.WaveKills, 0); // 重置波次击杀数
        data.Set(DataKey.WaveCriticalHits, 0); // 重置波次暴击次数
    }

    /// <summary>
    /// 处理单位死亡事件，遍历攻击链为 IUnit 和 IWeapon 累加击杀数
    /// </summary>
    /// <param name="data">击杀事件上下文，包含凶手、受害者、伤害类型等</param>
    private void OnUnitKilled(GameEventType.Unit.Killed data)
    {
        if (data.Killer is not Godot.Node killerNode) return;

        // 遍历攻击链，为 IUnit 和 IWeapon 记录击杀
        var ancestorChain = EntityRelationshipTraversal.GetAncestorChain(killerNode);
        bool foundAnyTarget = false;

        foreach (var entity in ancestorChain)
        {
            if (entity is IUnit or IWeapon)
            {
                foundAnyTarget = true;
                entity.Data.Add(DataKey.TotalKills, 1);
                entity.Data.Add(DataKey.WaveKills, 1);

                _log.Debug($"[击杀统计] {entity} 记录击杀 {data.Victim}");
            }
        }

        if (!foundAnyTarget)
        {
            _log.Warn($"击杀统计失败：攻击链上未找到 IUnit 或 IWeapon，Killer={data.Killer}");
        }
    }

    /// <inheritdoc />
    public void OnStarted(ProjectStateSnapshot snapshot)
    {
        BindRuntimeEvents();
    }

    /// <inheritdoc />
    public void OnStopped(ProjectStateSnapshot snapshot)
    {
        UnbindRuntimeEvents();
    }

    private void BindRuntimeEvents()
    {
        if (_eventsBound)
        {
            return;
        }

        // 当新波次开始时，需要清除上一波的临时统计数据（如每波造成的伤害、击杀数等）
        GlobalEventBus.Global.On<GameEventType.Global.WaveStarted>(
            OnWaveStarted);

        // 伤害系统（HealthComponent）在目标死亡时会发送 Kill 事件，本系统负责持久化这些统计
        GlobalEventBus.Global.On<GameEventType.Unit.Killed>(
            OnUnitKilled);

        _eventsBound = true;
    }

    private void UnbindRuntimeEvents()
    {
        if (!_eventsBound)
        {
            return;
        }

        GlobalEventBus.Global.Off<GameEventType.Global.WaveStarted>(
            OnWaveStarted);

        GlobalEventBus.Global.Off<GameEventType.Unit.Killed>(
            OnUnitKilled);

        _eventsBound = false;
    }

    public SystemRuntimeInfo GetSystemRuntimeInfo()
    {
        return new SystemRuntimeInfo
        {
            SystemId = nameof(DamageStatisticsSystem),
            CustomStats = new List<SystemStat>()
        };
    }
}
