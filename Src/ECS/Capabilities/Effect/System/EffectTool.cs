using Godot;
using System.Linq;

/// <summary>
/// 特效生成参数（统一独立特效和附着特效）
/// </summary>
/// <param name="VisualScenePath">特效视觉场景路径</param>
/// <param name="Name">特效名称（用于调试）</param>
/// <param name="MaxLifeTime">特效持续时间，-1 表示由动画结束控制</param>
/// <param name="Host">宿主 Entity 节点（非 null 时为附着模式，跟随宿主位置）</param>
/// <param name="Owner">归属 Entity（独立特效可填写；Host 非 null 时忽略此字段）</param>
/// <param name="Scale">特效缩放</param>
/// <param name="Rotation">旋转角度（度，2D 下 0=右、90=下、180=左，正值顺时针；内部按需转弧度）</param>
/// <param name="PlayRate">播放倍率</param>
/// <param name="Offset">生成或附着偏移</param>
/// <param name="IsLooping">是否循环播放</param>
/// <param name="EffectPosition">特效位置；独立特效建议显式填写，附着特效可不填</param>
public readonly record struct EffectSpawnOptions(
    string VisualScenePath,
    string Name = "Effect",
    float MaxLifeTime = -1f,
    Node? Host = null,
    IEntity? Owner = null,
    Vector2? Scale = null,
    float Rotation = 0f,
    float PlayRate = 1f,
    Vector2? Offset = null,
    bool IsLooping = false,
    Vector2? EffectPosition = null
);

/// <summary>
/// 特效工具 - 统一的特效生成和销毁入口
///
/// 设计理念：
/// - 独立静态工具类，消费 EntityManager 的基础生命周期 API
/// - 类似 DamageService 的领域服务模式
/// - Effect 当前仍自行编排对象池取出和视觉注入，owner/host 引用通过 EffectOwnershipService 收口
/// - 视觉加载由 EffectTool 完成
/// - 动画播放命令由 EffectComponent 通过事件驱动
/// - 附着跟随、生命周期、动画结束销毁由 EffectComponent 负责
///
/// 使用示例：
/// <code>
/// // 独立特效（在指定位置播放，播完自动销毁）
/// EffectTool.Spawn(new EffectSpawnOptions(hitEffectScenePath, EffectPosition: position, Owner: caster));
///
/// // 附着特效（跟随宿主，宿主销毁时自动销毁）
/// EffectTool.Spawn(new EffectSpawnOptions(buffEffectScenePath, Host: hostEntity));
///
/// // 销毁宿主身上所有特效
/// EffectTool.DestroyByHost(hostEntity);
/// </code>
/// </summary>
public static partial class EffectTool
{
    private static readonly Log _log = new("EffectTool");

    // ==================== 生成 ====================

    /// <summary>
    /// 生成特效（统一入口）
    /// - Host 为 null：独立特效，在 EffectPosition 指定的位置播放
    /// - Host 非 null：附着特效，跟随宿主位置，自动建立 lifecycle 和 owner projection
    /// </summary>
    /// <param name="options">特效参数</param>
    /// <returns>生成的 EffectEntity，失败返回 null</returns>
    public static EffectEntity? Spawn(EffectSpawnOptions options)
    {
        bool isAttached = options.Host != null;
        Vector2 position = ResolveEffectPosition(options, isAttached);

        // 附着模式：使用宿主位置
        if (isAttached)
        {
            if (options.Host is not IEntity)
            {
                _log.Error("宿主不是 IEntity");
                return null;
            }
            if (options.Host is Node2D host2D)
            {
                position = host2D.GlobalPosition;
            }
        }

        var entity = AcquireEffectEntity();
        if (entity == null)
        {
            _log.Error("特效生成失败");
            return null;
        }

        string effectId = entity.GetInstanceId().ToString();
        entity.Data.Set(GeneratedDataKey.Id, effectId);

        // 写入 Data
        FillEffectData(entity, options, isAttached);

        // 加载视觉场景到 VisualRoot；EffectTool 是特效场景的最终实例化点。
        if (!InjectVisualScene(entity, options.VisualScenePath))
        {
            EntityManager.Destroy(entity);
            return null;
        }

        // 应用初始变换
        ApplyInitialTransform(entity, position, options, isAttached);

        // 注册 Entity / Component（对象池复用后需要重新注册）
        if (EntityManager.ResolveEntityNode(EntityId.From(effectId)) == null)
        {
            EntityManager.Register(entity);
        }

        // 必须先建立 lifecycle/owner projection，再注册组件，避免 EffectComponent 取不到宿主。
        var hostOrOwner = ResolveHostOrOwner(options, isAttached);
        if (hostOrOwner != null)
        {
            AttachLifecycle(hostOrOwner, entity);
            EffectOwnershipService.Runtime.Attach(hostOrOwner, entity);
        }

        EntityManager.RegisterComponents(entity);

        // GlobalEventBus.Global.Emit(
        //     GameEventType.Global.EntitySpawned,
        //     new GameEventType.Global.EntitySpawned(entity));

        // 日志
        if (isAttached)
        {
            _log.Debug($"生成附着特效: {options.Name} -> 宿主: {options.Host!.Name}");
        }
        else
        {
            _log.Debug($"生成独立特效: {options.Name} 位置: {position}");
        }

        return entity;
    }

    /// <summary>
    /// 解析特效生成位置。
    /// </summary>
    private static Vector2 ResolveEffectPosition(EffectSpawnOptions options, bool isAttached)
    {
        if (isAttached)
        {
            if (options.Host is Node2D host2D)
            {
                return host2D.GlobalPosition;
            }
            return Vector2.Zero;
        }

        if (options.EffectPosition.HasValue)
        {
            return options.EffectPosition.Value;
        }

        _log.Warn($"独立特效 {options.Name} 未提供 EffectPosition，回退到 Vector2.Zero");
        return Vector2.Zero;
    }

    // ==================== 销毁 ====================

    /// <summary>
    /// 销毁指定宿主身上的所有附着特效
    /// </summary>
    /// <param name="host">宿主 Entity 节点</param>
    public static void DestroyByHost(Node host)
    {
        if (host is not IEntity hostEntity)
            return;

        var effects = EffectOwnershipService.Runtime.GetEffects(hostEntity).ToList();
        if (effects.Count == 0) return;
        int count = 0;

        foreach (var effect in effects)
        {
            if (effect != null)
            {
                EntityManager.Destroy(effect);
                count++;
            }
        }

        _log.Debug($"销毁宿主 {host.Name} 的 {count} 个附着特效");
    }

    /// <summary>
    /// 销毁单个特效实体
    /// </summary>
    /// <param name="effect">要销毁的特效实体</param>
    public static void Destroy(EffectEntity? effect)
    {
        if (effect == null) return;
        EntityManager.Destroy(effect);
    }

    // ==================== 内部方法 ====================

    /// <summary>
    /// 写入特效运行时数据到 Data 容器
    /// </summary>
    private static void FillEffectData(EffectEntity entity, EffectSpawnOptions options, bool isAttached)
    {
        entity.Data.Set(GeneratedDataKey.Name, options.Name);
        entity.Data.Set(GeneratedDataKey.EntityType, EntityType.Effect);
        entity.Data.Set(GeneratedDataKey.MaxLifeTime, options.MaxLifeTime);
        entity.Data.Set(GeneratedDataKey.EffectPlayRate, options.PlayRate);
        entity.Data.Set(GeneratedDataKey.EffectScale, options.Scale ?? Vector2.One);
        entity.Data.Set(GeneratedDataKey.EffectOffset, options.Offset ?? Vector2.Zero);
        entity.Data.Set(GeneratedDataKey.EffectIsLooping, options.IsLooping);
        entity.Data.Set(GeneratedDataKey.EffectIsAttached, isAttached);
    }

    private static IEntity? ResolveHostOrOwner(EffectSpawnOptions options, bool isAttached)
    {
        if (isAttached && options.Host is IEntity hostEntity)
            return hostEntity;

        return options.Owner;
    }

    private static void AttachLifecycle(IEntity hostOrOwner, EffectEntity effect)
    {
        if (hostOrOwner is not Node)
            return;

        // EffectTool 仍在对象池路径中手工构造 EffectEntity；生命周期只接入 typed LifecycleTree。
        EntityManager.AttachLifecycleParent(hostOrOwner, effect, ParentDestroyPolicy.DestroyRecursively);
    }

    /// <summary>
    /// 从对象池获取特效实体
    /// </summary>
    private static EffectEntity? AcquireEffectEntity()
    {
        var pool = ObjectPoolManager.GetPool<EffectEntity>(ObjectPoolNames.EffectPool);
        if (pool == null)
        {
            _log.Error($"对象池不存在: {ObjectPoolNames.EffectPool}");
            return null;
        }

        return pool.Get();
    }

    /// <summary>
    /// 应用特效初始变换
    /// </summary>
    private static void ApplyInitialTransform(EffectEntity entity, Vector2 position, EffectSpawnOptions options, bool isAttached)
    {
        var finalPosition = position;
        if (!isAttached)
        {
            finalPosition += options.Offset ?? Vector2.Zero;
        }

        entity.GlobalPosition = finalPosition;
        entity.GlobalRotationDegrees = options.Rotation;
        entity.Scale = options.Scale ?? Vector2.One;
        if (entity.IsInsideTree())
            entity.ForceUpdateTransform();
    }

    /// <summary>
    /// 加载视觉场景到 VisualRoot 子节点
    /// </summary>
    private static bool InjectVisualScene(
        EffectEntity entity, // 特效实体
        string visualScenePath) // 特效视觉场景路径
    {
        var visualScene = ResourceLoading.LoadPackedScenePath(
            visualScenePath, // res:// 场景路径
            ResourceLoadSource.DataOS("Effect.VisualScene", "特效视觉")); // 来源诊断
        if (visualScene == null)
        {
            return false;
        }

        // 清理旧的 VisualRoot
        var existingVisual = entity.GetNodeOrNull("VisualRoot");
        if (existingVisual != null)
        {
            entity.RemoveChild(existingVisual);
            existingVisual.QueueFree();
        }

        // 实例化新的视觉场景
        var visual = visualScene.Instantiate();
        visual.Name = "VisualRoot";
        entity.AddChild(visual);
        return true;
    }
}
