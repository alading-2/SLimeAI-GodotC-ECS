using Godot;

/// <summary>
/// 伤害飘字系统 (SystemManager Runtime Bridge)
///
/// 职责：
/// 1. 监听全局 Damaged / HealApplied / Dodged 事件
/// 2. 从对象池取出 DamageNumberUI，定位到受击单位，播放飘字动画
/// 3. 飘字动画结束后自动归还对象池（由 DamageNumberUI 自身处理）
///
/// 设计原则：
/// - DamageNumberSystem 只负责"触发"与"定位"，不持有任何 UI 引用
/// - DamageNumberUI 是纯视觉节点，负责自身动画与归还池
/// - 所有战斗结果事件（伤害/治疗/闪避）统一通过 GlobalEventBus 监听，无需逐个 Entity 订阅
/// </summary>
public static class DamageNumberSystem
{
    private static readonly Log _log = new("DamageNumberSystem");
    private static bool _isSubscribed;

    /// <summary>
    /// 启用运行时事件监听。
    /// </summary>
    public static void EnableRuntime()
    {
        if (_isSubscribed) return;

        GlobalEventBus.Global.On<GameEventType.Unit.DamagedEventData>(
            GameEventType.Unit.Damaged, OnDamaged);

        GlobalEventBus.Global.On<GameEventType.Unit.HealAppliedEventData>(
            GameEventType.Unit.HealApplied, OnHealApplied);

        GlobalEventBus.Global.On<GameEventType.Unit.DodgedEventData>(
            GameEventType.Unit.Dodged, OnDodged);

        _isSubscribed = true;
        _log.Success("DamageNumberSystem 已启用（全局事件模式）");
    }

    /// <summary>
    /// 禁用运行时事件监听。
    /// </summary>
    public static void DisableRuntime()
    {
        if (!_isSubscribed) return;

        GlobalEventBus.Global.Off<GameEventType.Unit.DamagedEventData>(
            GameEventType.Unit.Damaged, OnDamaged);

        GlobalEventBus.Global.Off<GameEventType.Unit.HealAppliedEventData>(
            GameEventType.Unit.HealApplied, OnHealApplied);

        GlobalEventBus.Global.Off<GameEventType.Unit.DodgedEventData>(
            GameEventType.Unit.Dodged, OnDodged);

        _isSubscribed = false;
        _log.Info("DamageNumberSystem 已禁用");
    }

    // ============================================================
    // 飘字触发
    // ============================================================

    private static void OnDamaged(GameEventType.Unit.DamagedEventData data)
    {
        var worldPos = GetEntityPosition(data.Victim);
        if (worldPos == null) return;

        var ui = GetFromPool();
        if (ui == null) return;

        ui.Show(data.Amount, worldPos.Value, data.IsCritical, data.Type);
    }

    private static void OnHealApplied(GameEventType.Unit.HealAppliedEventData data)
    {
        var worldPos = GetEntityPosition(data.Victim);
        if (worldPos == null) return;

        var ui = GetFromPool();
        if (ui == null) return;

        ui.ShowHeal(data.ActualAmount, worldPos.Value);
    }

    private static void OnDodged(GameEventType.Unit.DodgedEventData data)
    {
        var worldPos = GetEntityPosition(data.Victim);
        if (worldPos == null) return;

        var ui = GetFromPool();
        if (ui == null) return;

        ui.ShowMiss(worldPos.Value);
    }

    // ============================================================
    // 工具方法
    // ============================================================

    private static Vector2? GetEntityPosition(IEntity? entity)
    {
        if (entity is Node2D node2D && Node.IsInstanceValid(node2D))
            return node2D.GlobalPosition;
        return null;
    }

    private static DamageNumberUI? GetFromPool()
    {
        var pool = ObjectPoolManager.GetPool<DamageNumberUI>(ObjectPoolNames.DamageNumberUIPool);
        if (pool == null)
        {
            _log.Error("DamageNumberUIPool 不存在，请检查 ObjectPoolInit");
            return null;
        }
        return pool.Get();
    }
}
