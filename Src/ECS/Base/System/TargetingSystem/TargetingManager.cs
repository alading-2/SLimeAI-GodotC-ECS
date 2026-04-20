using Godot;
using Slime.Config.Units;
/// <summary>
/// 瞄准状态管理器 - 管理由输入层发起的异步点选会话
/// 
/// 职责：
/// - 维护当前瞄准状态（是否正在瞄准、挂起的技能请求）
/// - 生成/隐藏瞄准指示器
/// - 响应确认/取消事件，完成技能释放或取消流程
/// 
/// 设计理念：
/// - 使用全局事件解耦输入层、瞄准指示器和 AbilitySystem
/// - 支持单一激活瞄准（同时只能有一个技能在瞄准状态）
/// </summary>
public static class TargetingManager
{
    private static readonly Log _log = new(nameof(TargetingManager));
    private static bool _isSubscribed;

    // ================= 状态 =================

    /// <summary>是否正在瞄准中</summary>
    public static bool IsTargeting { get; private set; }

    /// <summary>当前瞄准的施法者</summary>
    public static IEntity? CurrentCaster { get; private set; }

    /// <summary>当前瞄准的技能</summary>
    public static AbilityEntity? CurrentAbility { get; private set; }

    /// <summary>当前施法上下文</summary>
    public static CastContext? CurrentContext { get; private set; }


    /// <summary>技能射程（指示器移动范围）</summary>
    public static float CurrentRange { get; private set; }

    // ================= 初始化 =================

    /// <summary>
    /// 初始化瞄准管理器 - 订阅全局事件
    /// <para>由 SystemManager 驱动的运行态生命周期调用。</para>
    /// </summary>
    public static void EnableRuntime()
    {
        if (_isSubscribed) return;

        // 订阅瞄准开始事件
        GlobalEventBus.Global.On<GameEventType.Targeting.StartTargetingEventData>(
            GameEventType.Targeting.StartTargeting,
            OnStartTargeting
        );

        // 订阅瞄准确认事件
        GlobalEventBus.Global.On<GameEventType.Targeting.TargetConfirmedEventData>(
            GameEventType.Targeting.TargetConfirmed,
            OnTargetConfirmed
        );

        // 订阅瞄准取消事件
        GlobalEventBus.Global.On<GameEventType.Targeting.TargetCancelledEventData>(
            GameEventType.Targeting.TargetCancelled,
            OnTargetCancelled
        );

        // 订阅单位死亡事件（玩家死亡时取消瞄准）
        GlobalEventBus.Global.On<GameEventType.Unit.KilledEventData>(
            GameEventType.Unit.Killed,
            OnUnitKilled
        );

        _isSubscribed = true;
        _log.Info("TargetingManager 已启用");
    }

    /// <summary>
    /// 停止瞄准运行时，解除事件订阅并清理悬挂会话。
    /// </summary>
    public static void DisableRuntime()
    {
        if (!_isSubscribed) return;

        GlobalEventBus.Global.Off<GameEventType.Targeting.StartTargetingEventData>(
            GameEventType.Targeting.StartTargeting,
            OnStartTargeting
        );

        GlobalEventBus.Global.Off<GameEventType.Targeting.TargetConfirmedEventData>(
            GameEventType.Targeting.TargetConfirmed,
            OnTargetConfirmed
        );

        GlobalEventBus.Global.Off<GameEventType.Targeting.TargetCancelledEventData>(
            GameEventType.Targeting.TargetCancelled,
            OnTargetCancelled
        );

        GlobalEventBus.Global.Off<GameEventType.Unit.KilledEventData>(
            GameEventType.Unit.Killed,
            OnUnitKilled
        );

        _isSubscribed = false;
        ForceCancelTargeting();
        _log.Info("TargetingManager 已禁用");
    }

    // ================= 事件处理 =================

    /// <summary>当前激活的瞄准指示器实例</summary>
    private static TargetingIndicatorEntity? _currentIndicator;

    /// <summary>
    /// 处理开始瞄准事件
    /// </summary>
    private static void OnStartTargeting(GameEventType.Targeting.StartTargetingEventData evt)
    {
        // 如果已经在瞄准中，先取消之前的
        if (IsTargeting)
        {
            CancelTargeting();
        }

        // 从 Context 提取所有信息
        var context = evt.Context;
        IsTargeting = true;
        CurrentCaster = context.Caster;
        CurrentAbility = context.Ability;
        CurrentContext = context;
        CurrentRange = CurrentAbility!.Data.Get<float>(DataKey.AbilityCastRange);

        // 获取施法者位置
        Vector2 casterPos = Vector2.Zero;
        if (CurrentCaster is Node2D node2D)
        {
            casterPos = node2D.GlobalPosition;
        }

        // 生成瞄准指示器（每次重新创建）
        _currentIndicator = SpawnIndicator(casterPos);

        var abilityName = CurrentAbility?.Data.Get<string>(DataKey.Name);
        _log.Info($"开始瞄准: {abilityName}, 射程: {CurrentRange}");
    }

    /// <summary>
    /// 处理瞄准确认事件
    /// </summary>
    private static void OnTargetConfirmed(GameEventType.Targeting.TargetConfirmedEventData evt)
    {
        if (!IsTargeting || CurrentContext == null || CurrentAbility == null)
        {
            _log.Warn("收到瞄准确认但当前不在瞄准状态");
            return;
        }

        // 1. 填充目标位置到上下文
        // 注意：HasPreselectedPosition 是计算属性，基于 TargetPosition.HasValue
        CurrentContext.TargetPosition = evt.TargetPosition;

        var abilityName = CurrentAbility.Data.Get<string>(DataKey.Name);
        _log.Info($"瞄准确认: {abilityName} -> {evt.TargetPosition}");

        // 2. 确认后才正式提交 TryTrigger；AbilitySystem 会再次检查可用性并负责消耗/冷却。
        var responseContext = new EventContext();
        CurrentContext.ResponseContext = responseContext;
        CurrentAbility.Events.Emit(
            GameEventType.Ability.TryTrigger,
            new GameEventType.Ability.TryTriggerEventData(CurrentContext) //施法上下文
        );

        var result = responseContext.HasResult
            ? responseContext.GetResult<TriggerResult>()
            : TriggerResult.Failed;
        if (result == TriggerResult.Failed)
        {
            _log.Debug($"点选确认后技能触发失败: {abilityName}");
        }

        // 3. 清理状态并销毁指示器
        EndTargeting(wasConfirmed: true, _currentIndicator);
        _currentIndicator = null;
    }

    /// <summary>
    /// 处理瞄准取消事件
    /// </summary>
    private static void OnTargetCancelled(GameEventType.Targeting.TargetCancelledEventData evt)
    {
        if (!IsTargeting) return;

        var abilityName = CurrentAbility?.Data.Get<string>(DataKey.Name);
        _log.Info($"瞄准取消: {abilityName}");

        EndTargeting(wasConfirmed: false, _currentIndicator);
        _currentIndicator = null;
    }

    /// <summary>
    /// 处理单位死亡事件（玩家死亡时强制取消瞄准）
    /// </summary>
    private static void OnUnitKilled(GameEventType.Unit.KilledEventData evt)
    {
        // 只处理玩家死亡
        if (evt.Victim is not PlayerEntity) return;

        // 如果正在瞄准，强制取消
        if (IsTargeting)
        {
            _log.Info("玩家死亡，强制取消瞄准");
            ForceCancelTargeting();
        }
    }

    // ================= 内部方法 =================

    /// <summary>
    /// 生成瞄准指示器（每次重新创建，避免 Component._Process 持续运行）
    /// </summary>
    private static TargetingIndicatorEntity? SpawnIndicator(Vector2 position)
    {
        // 加载 TargetingIndicatorConfig 资源
        var config = ResourceManagement.Load<TargetingIndicatorConfig>(
            ResourcePaths.DataUnit_TargetingIndicatorConfig,
            ResourceCategory.DataUnit
        );

        if (config == null)
        {
            _log.Error("无法加载 TargetingIndicatorConfig 资源");
            return null;
        }

        var indicator = EntityManager.Spawn<TargetingIndicatorEntity>(new EntitySpawnConfig
        {
            Config = config,
            UsingObjectPool = false,
            Position = position
        });

        if (indicator == null)
        {
            _log.Error("生成瞄准指示器 TargetingIndicatorEntity 失败");
            return null;
        }

        // 获取 TargetingIndicatorControlComponent 并设置参数
        var controlComponent = EntityManager.GetComponent<TargetingIndicatorControlComponent>(indicator);
        if (controlComponent != null)
        {
            controlComponent.SetTargetingParams(CurrentCaster, CurrentRange);
        }

        return indicator;
    }

    /// <summary>
    /// 销毁指示器（彻底销毁，停止 Component._Process）
    /// </summary>
    private static void DestroyIndicator(TargetingIndicatorEntity? indicator)
    {
        if (indicator != null && GodotObject.IsInstanceValid(indicator))
        {
            EntityManager.Destroy(indicator);
        }
    }

    /// <summary>
    /// 结束瞄准流程
    /// </summary>
    private static void EndTargeting(bool wasConfirmed, TargetingIndicatorEntity? indicator)
    {
        // 销毁指示器（彻底销毁，停止 Component._Process）
        DestroyIndicator(indicator);

        // 发送瞄准结束事件
        GlobalEventBus.Global.Emit(
            GameEventType.Targeting.TargetingEnded,
            new GameEventType.Targeting.TargetingEndedEventData(wasConfirmed)
        );

        // 清理状态
        IsTargeting = false;
        CurrentCaster = null;
        CurrentAbility = null;
        CurrentContext = null;
        CurrentRange = 0;
    }

    /// <summary>
    /// 取消当前瞄准（内部调用）
    /// </summary>
    private static void CancelTargeting()
    {
        EndTargeting(wasConfirmed: false, _currentIndicator);
        _currentIndicator = null;
    }

    // ================= 公共 API =================

    /// <summary>
    /// 强制取消瞄准（外部调用，如玩家受到控制效果）
    /// </summary>
    public static void ForceCancelTargeting()
    {
        if (IsTargeting)
        {
            _log.Info("瞄准被强制取消");
            CancelTargeting();
        }
    }
}
