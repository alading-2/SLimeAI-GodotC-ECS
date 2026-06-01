using Godot;
using System;
using System.Text.Json;

/// <summary>
/// Entity 生成请求。
/// <para>只表达通用创建事实；owner/source/target 等业务语义留在领域 facade。</para>
/// </summary>
public sealed class EntitySpawnRequest<T> where T : Node, IEntity
{
    public required Func<T?> CreateNode { get; init; }
    public required object Config { get; init; }
    public DataRuntimeBootstrap? RuntimeDataBootstrap { get; init; }
    public RuntimeDataRecordDto? RuntimeDataRecord { get; init; }
    public string? RuntimeDataRecordTable { get; init; }
    public string? RuntimeDataRecordId { get; init; }
    public EntityId EntityId { get; init; } = EntityId.Empty;
    public EntityId LifecycleParentId { get; init; } = EntityId.Empty;
    public ParentDestroyPolicy ParentDestroyPolicy { get; init; } = ParentDestroyPolicy.DestroyRecursively;
    public Vector2? Position { get; init; }
    public float? Rotation { get; init; }
    public PackedScene? VisualSceneOverride { get; init; }
    public Action<T>? AddToSceneTree { get; init; }
    public Action<T>? ActivateNode { get; init; }
    public Action<T>? RollbackNode { get; init; }
}

/// <summary>
/// Entity 生成结果。
/// </summary>
public sealed class EntitySpawnResult<T> where T : Node, IEntity
{
    private EntitySpawnResult(bool success, T? node, EntityId entityId, string error)
    {
        Success = success;
        Node = node;
        EntityId = entityId;
        Error = error;
    }

    public bool Success { get; }
    public T? Node { get; }
    public EntityId EntityId { get; }
    public string Error { get; }

    public static EntitySpawnResult<T> Succeeded(T node, EntityId entityId)
        => new(true, node, entityId, string.Empty);

    public static EntitySpawnResult<T> Failed(string error)
        => new(false, null, EntityId.Empty, error);
}

/// <summary>
/// Entity 生成管线。
/// <para>统一编排 create、data、visual、transform、registry、component、lifecycle 和 activate 阶段。</para>
/// </summary>
public sealed class EntitySpawnPipeline
{
    private static readonly Log _log = new(nameof(EntitySpawnPipeline), LogLevel.Debug);

    private readonly EntityRegistry _registry;
    private readonly LifecycleTree _lifecycleTree;
    private readonly ComponentRegistrar _componentRegistrar;

    public EntitySpawnPipeline(
        EntityRegistry registry,
        LifecycleTree lifecycleTree,
        ComponentRegistrar componentRegistrar)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _lifecycleTree = lifecycleTree ?? throw new ArgumentNullException(nameof(lifecycleTree));
        _componentRegistrar = componentRegistrar ?? throw new ArgumentNullException(nameof(componentRegistrar));
    }

    public EntitySpawnResult<T> Spawn<T>(EntitySpawnRequest<T> request) where T : Node, IEntity
    {
        T? entity = null;
        var registered = false;
        var componentsRegistered = false;
        var lifecycleAttached = false;

        try
        {
            entity = request.CreateNode();
            if (entity == null)
                return EntitySpawnResult<T>.Failed("create node returned null");

            request.AddToSceneTree?.Invoke(entity);

            var entityId = request.EntityId.IsEmpty
                ? EntityId.From(entity.GetInstanceId().ToString())
                : request.EntityId;

            if (entityId.IsEmpty)
            {
                Rollback(entity, request, registered, componentsRegistered, lifecycleAttached);
                return EntitySpawnResult<T>.Failed("entity id is empty");
            }

            if (!ApplySpawnData(entity, request, entityId, out var record))
            {
                Rollback(entity, request, registered, componentsRegistered, lifecycleAttached);
                return EntitySpawnResult<T>.Failed("data apply failed");
            }

            InjectVisualScene(entity, record, request.VisualSceneOverride);
            ApplyTransform(entity, request.Position, request.Rotation);

            if (!_registry.Register(entityId, entity))
            {
                Rollback(entity, request, registered, componentsRegistered, lifecycleAttached);
                return EntitySpawnResult<T>.Failed($"registry register failed: {entityId.Value}");
            }

            registered = true;
            NodeLifecycleManager.Register(entity);
            _componentRegistrar.RegisterComponents(entity);
            componentsRegistered = true;

            if (!request.LifecycleParentId.IsEmpty)
            {
                if (!_lifecycleTree.Attach(request.LifecycleParentId, entityId, request.ParentDestroyPolicy))
                {
                    Rollback(entity, request, registered, componentsRegistered, lifecycleAttached);
                    return EntitySpawnResult<T>.Failed($"lifecycle attach failed: parent={request.LifecycleParentId.Value}, child={entityId.Value}");
                }

                lifecycleAttached = true;
            }

            request.ActivateNode?.Invoke(entity);
            GlobalEventBus.Global.Emit(new GameEventType.Global.EntitySpawned(entity));
            return EntitySpawnResult<T>.Succeeded(entity, entityId);
        }
        catch (Exception ex)
        {
            if (entity != null)
                Rollback(entity, request, registered, componentsRegistered, lifecycleAttached);

            _log.Error($"Entity spawn failed: {ex}");
            return EntitySpawnResult<T>.Failed(ex.Message);
        }
    }

    private static bool ApplySpawnData<T>(
        T entity,
        EntitySpawnRequest<T> request,
        EntityId entityId,
        out RuntimeDataRecordDto record) where T : Node, IEntity
    {
        record = null!;
        var bootstrap = request.RuntimeDataBootstrap ?? DataRuntimeBootstrap.Default;

        if (request.RuntimeDataRecord != null)
        {
            record = request.RuntimeDataRecord;
        }
        else if (!string.IsNullOrWhiteSpace(request.RuntimeDataRecordTable) && !string.IsNullOrWhiteSpace(request.RuntimeDataRecordId))
        {
            try
            {
                record = bootstrap.FindRecord(request.RuntimeDataRecordTable, request.RuntimeDataRecordId);
            }
            catch (Exception ex)
            {
                _log.Error($"runtime snapshot record 查找失败: {typeof(T).Name}, {request.RuntimeDataRecordTable}/{request.RuntimeDataRecordId}, error={ex.Message}");
                return false;
            }
        }
        else
        {
            _log.Error($"runtime snapshot record 未显式指定: {typeof(T).Name}");
            return false;
        }

        var report = bootstrap.ApplyToData(entity.Data, record);
        if (report.HasErrors)
        {
            _log.Error(report.ToSummary());
            return false;
        }

        entity.Data.Set(GeneratedDataKey.Id, entityId.Value);
        return true;
    }

    private static void InjectVisualScene(Node entity, RuntimeDataRecordDto record, PackedScene? visualSceneOverride = null)
    {
        PackedScene? scene = visualSceneOverride;

        if (scene == null && TryReadRecordString(record, GeneratedDataKey.VisualScenePath.StableKey, out var recordPath)
            && !string.IsNullOrWhiteSpace(recordPath))
        {
            scene = CommonTool.LoadPackedScene(recordPath, $"{entity.Name} 视觉");
        }

        var existingVisual = entity.GetNodeOrNull("VisualRoot");
        existingVisual?.Free();

        if (scene == null)
        {
            _log.Debug($"[{entity.Name}] 未配置 VisualScene，跳过视觉注入");
            return;
        }

        var visual = scene.Instantiate();
        visual.Name = "VisualRoot";
        entity.AddChild(visual);

        if (visual is Node2D visual2D)
            visual2D.ZIndex = 10;

        EntityVisualCollisionTemplate.SyncAndRemove(entity, visual);
        _log.Debug($"已加载 VisualScene: {scene.ResourcePath}");
    }

    private static void ApplyTransform(Node entity, Vector2? position, float? rotation)
    {
        if (entity is not Node2D entity2D)
            return;

        if (position.HasValue)
            entity2D.GlobalPosition = position.Value;
        if (rotation.HasValue)
            entity2D.GlobalRotationDegrees = rotation.Value;
        if (entity2D.IsInsideTree())
            entity2D.ForceUpdateTransform();
    }

    private void Rollback<T>(
        T entity,
        EntitySpawnRequest<T> request,
        bool registered,
        bool componentsRegistered,
        bool lifecycleAttached) where T : Node, IEntity
    {
        var entityId = _registry.GetEntityId(entity);

        if (lifecycleAttached && !entityId.IsEmpty)
            _lifecycleTree.Detach(entityId);

        if (componentsRegistered)
            _componentRegistrar.UnregisterComponents(entity);

        if (registered)
        {
            _registry.Unregister(entity);
            NodeLifecycleManager.Unregister(entity);
        }

        request.RollbackNode?.Invoke(entity);
        if (entity.HasMeta("InPool") && entity.GetMeta("InPool").AsBool())
            return;

        if (GodotObject.IsInstanceValid(entity) && !entity.IsQueuedForDeletion())
        {
            if (entity.GetParent() != null)
                entity.GetParent()!.RemoveChild(entity);

            entity.QueueFree();
        }
    }

    private static bool TryReadRecordString(RuntimeDataRecordDto record, string fieldKey, out string value)
    {
        value = string.Empty;
        if (!record.Fields.TryGetValue(fieldKey, out var field))
            return false;

        value = field.Value switch
        {
            string text => text,
            JsonElement element when element.ValueKind == JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonElement element => element.GetRawText(),
            null => string.Empty,
            _ => Convert.ToString(field.Value) ?? string.Empty
        };
        return true;
    }
}

/// <summary>
/// Entity 视觉碰撞模板同步入口。
/// </summary>
internal static class EntityVisualCollisionTemplate
{
    public static void SyncAndRemove(Node entity, Node visualRoot)
    {
        // 当前碰撞模板同步实现仍保留在 EntityManager_Collision partial 中。
        EntityManager.SyncVisualCollisionTemplate(entity, visualRoot);
    }
}
