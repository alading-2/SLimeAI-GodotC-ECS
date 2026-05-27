using Godot;
using System;
using System.Collections.Generic;

/// <summary>
/// EntityManager 的迁移扩展。
/// <para>职责：把“源实体”替换为“目标实体”，并在两者之间做受控 Data 迁移。</para>
/// <para>v1 只迁移安全 Data，不迁移局部 EventBus 订阅、组件私有状态、视觉节点树或整张关系图。</para>
/// </summary>
public static partial class EntityManager
{
    /// <summary>
    /// 迁移执行期间使用的只读快照。
    /// <para>用于隔离“读源实体状态”和“生成目标实体”两个阶段，避免中途读取被销毁或被改写的运行时数据。</para>
    /// </summary>
    private readonly record struct EntityMigrationSnapshot(
        string SourceEntityId, //源实体ID
        Dictionary<string, object> BaseData, //安全可迁移的Data快照
        IEntity? DirectParent, //直接父实体
        ParentDestroyPolicy ParentDestroyPolicy, //父实体销毁策略
        Vector2? Position, //源实体位置
        float? Rotation //源实体旋转角度（度）
    );

    /// <summary>
    /// 将源实体迁移为新的目标实体。
    /// <para>固定流程：拍快照 → 生成目标实体 → 复制受控 Data → 记录来源 → 销毁源实体。</para>
    /// </summary>
    /// <typeparam name="TTarget">目标实体类型</typeparam>
    /// <param name="sourceEntity">源实体</param>
    /// <param name="config">迁移配置</param>
    /// <returns>迁移成功返回目标实体；失败返回 null，且尽量保持源实体原状</returns>
    public static TTarget? Migrate<TTarget>(
        Node sourceEntity, // 源实体
        EntityMigrationConfig config // 迁移配置
    ) where TTarget : Node, IEntity
    {
        if (sourceEntity is not IEntity sourceIEntity)
        {
            _log.Error($"迁移失败：源节点 {sourceEntity.Name} 未实现 IEntity");
            return null;
        }

        string sourceEntityId = EntityRelationshipTraversal.ResolveEntityId(sourceEntity); // 源实体 Id
        if (string.IsNullOrEmpty(sourceEntityId) || !NodeLifecycleManager.IsRegistered(sourceEntityId))
        {
            _log.Error($"迁移失败：源实体未注册或无法解析 Id，node={sourceEntity.Name}");
            return null;
        }

        EntityMigrationProfile profile = config.Profile ?? EntityMigrationProfile.Default; // 迁移 Profile
        EntityMigrationSnapshot snapshot = BuildMigrationSnapshot(
            sourceEntity, // 源实体
            sourceIEntity, // 源 IEntity
            config // 迁移配置
        );

        GlobalEventBus.Global.Emit(
            new GameEventType.Global.EntityMigrating(
                sourceIEntity, // 源实体
                typeof(TTarget).Name, // 目标实体类型名
                profile.Name // Profile 名称
            )
        );

        EntitySpawnConfig targetSpawn = BuildTargetSpawnConfig(
            sourceEntity, // 源实体
            snapshot, // 迁移快照
            config // 迁移配置
        );

        TTarget? targetEntity = Spawn<TTarget>(targetSpawn);
        if (targetEntity == null)
        {
            _log.Error($"迁移失败：目标实体生成失败，source={sourceEntity.Name}, targetType={typeof(TTarget).Name}");
            return null;
        }

        try
        {
            ApplyMigratedData(
                targetEntity, // 目标实体
                snapshot.BaseData, // 源基础数据快照
                profile // 迁移 Profile
            );
            ApplyDataOverrides(
                targetEntity, // 目标实体
                config.DataOverrides // 额外覆写数据
            );
            StampMigrationLineage(
                sourceIEntity, // 源实体
                targetEntity, // 目标实体
                snapshot.SourceEntityId // 源实体 Id
            );

            GlobalEventBus.Global.Emit(
                new GameEventType.Global.EntityMigrated(
                    sourceIEntity, // 源实体
                    targetEntity, // 目标实体
                    profile.Name // Profile 名称
                )
            );

            Destroy(sourceEntity);
            return targetEntity;
        }
        catch (Exception ex)
        {
            _log.Error($"迁移失败：目标实体回滚，source={sourceEntity.Name}, targetType={typeof(TTarget).Name}, error={ex}");
            Destroy(targetEntity);
            return null;
        }
    }

    /// <summary>
    /// 从源实体拍摄迁移快照。
    /// </summary>
    private static EntityMigrationSnapshot BuildMigrationSnapshot(
        Node sourceEntity, // 源实体
        IEntity sourceIEntity, // 源 IEntity
        EntityMigrationConfig config // 迁移配置
    )
    {
        IEntity? directParent = null;
        ParentDestroyPolicy parentDestroyPolicy = ParentDestroyPolicy.DestroyRecursively;

        if (config.InheritDirectParent)
        {
            // 仅继承直接父实体，避免把整条关系链在迁移阶段一并重建。
            directParent = EntityRelationshipTraversal.GetDirectParentOfType<IEntity>(sourceEntity);
            if (directParent is Node directParentNode)
            {
                parentDestroyPolicy = EntityRelationshipLifecycle.ReadParentDestroyPolicy(
                    EntityRelationshipTraversal.ResolveEntityId(directParentNode), // 父实体 Id
                    EntityRelationshipTraversal.ResolveEntityId(sourceEntity) // 源实体 Id
                );
            }
        }

        Vector2? position = null;
        float? rotation = null;
        if (sourceEntity is Node2D source2D)
        {
            // 迁移对外统一保留 2D 世界坐标与角度（度），供目标生成配置按需复用。
            position = source2D.GlobalPosition;
            rotation = source2D.GlobalRotationDegrees;
        }

        return new EntityMigrationSnapshot(
            EntityRelationshipTraversal.ResolveEntityId(sourceEntity), // 源实体 Id
            sourceIEntity.Data.GetAll(), // 基础数据快照；不包含修改器与局部事件
            directParent, // 直接父实体
            parentDestroyPolicy, // 父销毁策略
            position, // 位置快照
            rotation // 旋转快照
        );
    }

    /// <summary>
    /// 组装目标实体的最终生成配置。
    /// </summary>
    private static EntitySpawnConfig BuildTargetSpawnConfig(
        Node sourceEntity, // 源实体
        EntityMigrationSnapshot snapshot, // 迁移快照
        EntityMigrationConfig config // 迁移配置
    )
    {
        // 以调用方显式提供的 TargetSpawn 为第一优先级，仅在未填写时回退到源实体快照。
        EntitySpawnConfig targetSpawn = config.TargetSpawn;

        if (sourceEntity is Node2D)
        {
            if (!targetSpawn.Position.HasValue && snapshot.Position.HasValue)
            {
                targetSpawn = targetSpawn with { Position = snapshot.Position.Value };
            }

            if (!targetSpawn.Rotation.HasValue && snapshot.Rotation.HasValue)
            {
                targetSpawn = targetSpawn with { Rotation = snapshot.Rotation.Value };
            }
        }

        if (config.InheritDirectParent && targetSpawn.ParentEntity == null && snapshot.DirectParent != null)
        {
            // 仅在调用方未显式指定父实体时继承，避免覆盖外部组装的目标关系。
            targetSpawn = targetSpawn with
            {
                ParentEntity = snapshot.DirectParent,
                AutoAddParentRelation = true,
                ParentDestroyPolicy = snapshot.ParentDestroyPolicy
            };
        }

        return targetSpawn;
    }

    /// <summary>
    /// 把符合规则的基础 Data 复制到目标实体。
    /// </summary>
    private static void ApplyMigratedData(
        IEntity targetEntity, // 目标实体
        Dictionary<string, object> sourceData, // 源基础数据快照
        EntityMigrationProfile profile // 迁移 Profile
    )
    {
        foreach ((string key, object value) in sourceData)
        {
            // 迁移阶段只复制“安全且被允许”的基础值，避免把旧实例引用带到新实体。
            if (!ShouldMigrateDataEntry(
                    key, // DataKey
                    value, // 数据值
                    profile // 迁移 Profile
                ))
            {
                continue;
            }

            targetEntity.Data.Set(
                key, // DataKey
                value // 数据值
            );
        }
    }

    /// <summary>
    /// 应用迁移完成后的显式 Data 覆写。
    /// </summary>
    private static void ApplyDataOverrides(
        IEntity targetEntity, // 目标实体
        Dictionary<string, object>? dataOverrides // 迁移后覆写
    )
    {
        if (dataOverrides == null)
        {
            return;
        }

        foreach ((string key, object value) in dataOverrides)
        {
            targetEntity.Data.Set(
                key, // DataKey
                value // 覆写值
            );
        }
    }

    /// <summary>
    /// 写入迁移链路标记。
    /// <para>`SourceEntityId` 总是记录最近一次源实体；`OriginEntityId` 固定记录首个来源。</para>
    /// </summary>
    private static void StampMigrationLineage(
        IEntity sourceEntity, // 源实体
        IEntity targetEntity, // 目标实体
        string sourceEntityId // 源实体 Id
    )
    {
        string originEntityId = sourceEntity.Data.Get<string>(DataKey.OriginEntityId); // 历史第一来源
        if (string.IsNullOrEmpty(originEntityId))
        {
            originEntityId = sourceEntityId;
        }

        targetEntity.Data.Set(DataKey.SourceEntityId, sourceEntityId);
        targetEntity.Data.Set(DataKey.OriginEntityId, originEntityId);
    }

    /// <summary>
    /// 判断指定 Data 项是否允许迁移。
    /// </summary>
    private static bool ShouldMigrateDataEntry(
        string key, // DataKey
        object value, // 数据值
        EntityMigrationProfile profile // 迁移 Profile
    )
    {
        // Profile 显式排除拥有最高优先级，用于快速禁止某些 DataKey 迁移。
        if (profile.Excludes(key))
        {
            return false;
        }

        DataMeta? meta = DataRegistry.GetMeta(key);
        // DataMeta.CanMigrate 是默认规则；Profile.Includes 可用于对白名单项做显式放行。
        if (meta != null && !meta.CanMigrate && !profile.Includes(key))
        {
            return false;
        }

        return IsSafeMigrationValue(value);
    }

    /// <summary>
    /// 判断运行时值是否属于 v1 允许迁移的安全类型。
    /// <para>规则：允许值类型、字符串、Resource；拒绝 Node / IEntity / IComponent / 委托 / EventBus 等绑定旧实例生命周期的引用。</para>
    /// </summary>
    private static bool IsSafeMigrationValue(object value)
    {
        Type valueType = value.GetType();

        // 这些类型通常绑定旧实体生命周期、场景树或事件订阅，迁移后继续复用会制造悬挂引用。
        if (typeof(Node).IsAssignableFrom(valueType) ||
            typeof(IEntity).IsAssignableFrom(valueType) ||
            typeof(IComponent).IsAssignableFrom(valueType) ||
            typeof(Delegate).IsAssignableFrom(valueType) ||
            typeof(EventBus).IsAssignableFrom(valueType))
        {
            return false;
        }

        if (value is Resource)
        {
            return true;
        }

        if (valueType == typeof(string))
        {
            return true;
        }

        return valueType.IsValueType;
    }
}
