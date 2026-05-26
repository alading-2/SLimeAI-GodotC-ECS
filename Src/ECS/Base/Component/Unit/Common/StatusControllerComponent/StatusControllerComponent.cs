using Godot;
using System.Collections.Generic;

/// <summary>
/// 状态控制组件。
/// <para>负责维护状态实例集合，并把聚合结果同步到 Data 供其他系统消费。</para>
/// </summary>
public partial class StatusControllerComponent : Node, IComponent
{
    private IEntity? _entity;
    private Data? _data;
    private readonly StatusCollection _statuses = new();
    private readonly Dictionary<string, GameTimer> _statusTimers = new();
    private StatusSnapshot _currentSnapshot = new(StatusEffectFlags.None);

    /// <summary>当前状态快照。</summary>
    public StatusSnapshot CurrentSnapshot => _currentSnapshot;

    /// <inheritdoc />
    public void OnComponentRegistered(Node entity)
    {
        if (entity is not IEntity iEntity) return;

        _entity = iEntity;
        _data = iEntity.Data;
        ApplySnapshot(_statuses.BuildSnapshot());
    }

    /// <inheritdoc />
    public void OnComponentUnregistered()
    {
        foreach (var timer in _statusTimers.Values)
        {
            timer.Cancel();
        }

        _statusTimers.Clear();
        _entity = null;
        _data = null;
    }

    /// <summary>
    /// 应用一个状态实例。
    /// </summary>
    /// <param name="instance">状态实例。</param>
    public void ApplyStatus(StatusInstance instance)
    {
        _statuses.Apply(instance);
        RefreshTimer(instance);
        ApplySnapshot(_statuses.BuildSnapshot());
    }

    /// <summary>
    /// 用简化参数应用状态。
    /// </summary>
    /// <param name="definition">状态定义。</param>
    /// <param name="sourceId">来源 Id。</param>
    /// <param name="durationSeconds">持续时间；-1 表示不限时。</param>
    public void ApplyStatus(StatusDefinition definition, string sourceId, float durationSeconds = -1f)
    {
        ApplyStatus(new StatusInstance(sourceId, definition.StatusId, definition, durationSeconds));
    }

    /// <summary>
    /// 移除一个状态来源。
    /// </summary>
    /// <param name="sourceId">来源 Id。</param>
    /// <param name="statusId">状态 Id。</param>
    public void RemoveStatus(string sourceId, string statusId)
    {
        _statuses.Remove(sourceId, statusId);
        CancelTimer(sourceId, statusId);
        ApplySnapshot(_statuses.BuildSnapshot());
    }

    private void RefreshTimer(StatusInstance instance)
    {
        CancelTimer(instance.SourceId, instance.StatusId);

        if (instance.DurationSeconds <= 0f)
        {
            return;
        }

        var timerManager = TimerManager.Instance;
        if (timerManager == null)
        {
            return;
        }

        var key = BuildTimerKey(instance.SourceId, instance.StatusId);
        _statusTimers[key] = timerManager.Delay(instance.DurationSeconds)
            .OnComplete(() => RemoveStatus(instance.SourceId, instance.StatusId));
    }

    private void CancelTimer(string sourceId, string statusId)
    {
        var key = BuildTimerKey(sourceId, statusId);
        if (!_statusTimers.Remove(key, out var timer))
        {
            return;
        }

        timer.Cancel();
    }

    private void ApplySnapshot(StatusSnapshot snapshot)
    {
        _currentSnapshot = snapshot;
        if (_data == null) return;

        _data.Set(DataKey.StatusCanThink, snapshot.CanThink);
        _data.Set(DataKey.StatusCanMoveInput, snapshot.CanMoveInput);
        _data.Set(DataKey.StatusCanAttack, snapshot.CanAttack);
        _data.Set(DataKey.StatusCanCast, snapshot.CanCast);
        _data.Set(DataKey.StatusIsInvulnerable, snapshot.IsInvulnerable);
        _data.Set(DataKey.StatusIsControlImmune, snapshot.IsControlImmune);

        // 兼容旧链路：先同步到已有的运行时布尔键。
        _data.Set(DataKey.IsInvulnerable, snapshot.IsInvulnerable);
        _data.Set(DataKey.IsMovementLocked, snapshot.IsMovementLocked);
        _data.Set(DataKey.IsStunned, !snapshot.CanThink && !snapshot.CanMoveInput && !snapshot.CanAttack && !snapshot.CanCast);

        _entity?.Events.Emit(new GameEventType.Unit.StateChanged("StatusSnapshot", string.Empty, snapshot.Flags.ToString()));
    }

    private static string BuildTimerKey(string sourceId, string statusId)
    {
        return $"{sourceId}::{statusId}";
    }
}
