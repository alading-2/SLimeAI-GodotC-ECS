using Godot;

/// <summary>
/// 敌人实体类（Scene 即 Entity）。
/// <para>
/// 职责：AI 驱动、对象池管理（IPoolable）、掉落逻辑。
/// 架构：与 Player 逻辑分离，通过组件（Component）复用共享行为。
/// </para>
/// </summary>
public partial class EnemyEntity : CharacterBody2D, IPoolable, IUnit
{
    private static readonly Log _log = new("Enemy", LogLevel.Info);

    // ================= IEntity 实现 =================

    /// <summary>
    /// 动态数据容器
    /// </summary>
    public Data Data { get; private set; }

    public EnemyEntity()
    {
        Data = new Data(this);
        // 敌人的默认移动模式是组件注册期数据；这里给对象池/场景直放路径提供保底值。
        Data.Set(GeneratedDataKey.DefaultMoveMode, MoveMode.AIControlled);
    }

    /// <summary>
    /// 实体局部事件总线
    /// </summary>
    public EventBus Events { get; } = new EventBus();


    // ================= Godot 生命周期 =================

    public override void _Ready()
    {
        base._Ready();
        _log.Debug($"敌人 {Name} 初始化完成。");
    }

    public override void _ExitTree()
    {
        base._ExitTree();
    }


    // ================= IPoolable 接口实现 =================

    /// <summary>
    /// [IPoolable] 当从池中取出时调用 (Active)。
    /// 统一在此处订阅事件，确保对象池复用时事件正确绑定。
    /// </summary>
    public void OnPoolAcquire()
    {
        // DefaultMoveMode 是组件注册期字段，已在构造时提供保底值；snapshot record 可在 Spawn 时覆盖。
    }

    /// <summary>
    /// [IPoolable] 当归还池时调用 (Deactive)。
    /// 核心职责：清理状态、重置数据。
    /// </summary>
    public void OnPoolRelease()
    {
        // 注意：Events.Clear(), Data.Clear(), Component.OnComponentUnregistered()
        // 均由 EntityManager.Destroy() -> UnregisterEntity() 统一处理
    }

    /// <summary>
    /// [IPoolable] 当归还池时重置
    /// </summary>
    public void OnPoolReset()
    {
        // 可以在这里移除所有动态添加的组件，如果需要的话
        // 但通常为了复用，我们保留组件结构，只重置数据
    }
}
