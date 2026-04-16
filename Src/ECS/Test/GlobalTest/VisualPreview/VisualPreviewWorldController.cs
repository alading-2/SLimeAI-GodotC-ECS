using Godot;
using Slime.Config.Test;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 独立视觉预览世界控制器。
/// </summary>
internal sealed class VisualPreviewWorldController
{
    private const int ColumnCount = 5;
    private const float ColumnSpacing = 220f;
    private const float RowSpacing = 220f;

    private readonly List<VisualPreviewEntity> _entities = new();
    private readonly Dictionary<VisualPreviewEntity, VisualPreviewEntry> _entriesByEntity = new();

    public IReadOnlyList<VisualPreviewEntity> Entities => _entities;

    /// <summary>
    /// 重建全部预览实体。
    /// </summary>
    /// <param name="entries">待预览资源条目。</param>
    public void Rebuild(IReadOnlyList<VisualPreviewEntry> entries)
    {
        Clear();
        for (var i = 0; i < entries.Count; i++)
        {
            var entity = SpawnPreviewEntity(
                entries[i], // 当前资源条目
                ResolveGridPosition(i) // 网格位置
            );
            if (entity == null)
            {
                continue;
            }

            _entities.Add(entity);
            _entriesByEntity[entity] = entries[i];
        }
    }

    /// <summary>
    /// 清理全部预览实体。
    /// </summary>
    public void Clear()
    {
        foreach (var entity in _entities)
        {
            if (GodotObject.IsInstanceValid(entity))
            {
                EntityManager.Destroy(entity);
            }
        }

        _entities.Clear();
        _entriesByEntity.Clear();
    }

    /// <summary>
    /// 尝试读取预览实体对应资源条目。
    /// </summary>
    /// <param name="entity">鼠标选择命中的实体。</param>
    /// <param name="entry">对应资源条目。</param>
    public bool TryGetEntry(IEntity? entity, out VisualPreviewEntry entry)
    {
        if (entity is VisualPreviewEntity previewEntity && _entriesByEntity.TryGetValue(previewEntity, out entry))
        {
            return true;
        }

        entry = default;
        return false;
    }

    /// <summary>
    /// 获取指定分类下的实体数量。
    /// </summary>
    /// <param name="catalogPath">分类路径。</param>
    public int CountByCatalog(string catalogPath)
    {
        var count = 0;
        foreach (var entry in _entriesByEntity.Values)
        {
            if (string.Equals(entry.CatalogPath, catalogPath, StringComparison.Ordinal))
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// 只显示指定分类。
    /// </summary>
    /// <param name="catalogPath">分类路径。</param>
    public void ShowCatalog(string catalogPath)
    {
        foreach (var entity in _entities)
        {
            if (!_entriesByEntity.TryGetValue(entity, out var entry))
            {
                continue;
            }

            entity.Visible = string.Equals(entry.CatalogPath, catalogPath, StringComparison.Ordinal);
            entity.ProcessMode = entity.Visible
                ? Node.ProcessModeEnum.Inherit
                : Node.ProcessModeEnum.Disabled;
        }
    }

    /// <summary>
    /// 获取当前分类可用动作并集。
    /// </summary>
    /// <param name="catalogPath">分类路径。</param>
    public IReadOnlyList<string> GetAnimationUnion(string catalogPath)
    {
        var result = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var entity in _entities)
        {
            if (!_entriesByEntity.TryGetValue(entity, out var entry)
                || !string.Equals(entry.CatalogPath, catalogPath, StringComparison.Ordinal))
            {
                continue;
            }

            foreach (var animationName in GetAvailableAnimations(entity))
            {
                result.Add(animationName);
            }
        }

        return result.ToArray();
    }

    /// <summary>
    /// 对当前分类应用统一动作。
    /// </summary>
    /// <param name="catalogPath">分类路径。</param>
    /// <param name="animationName">目标动作名。</param>
    public void ApplyAnimation(string catalogPath, string animationName)
    {
        foreach (var entity in _entities)
        {
            if (!_entriesByEntity.TryGetValue(entity, out var entry)
                || !string.Equals(entry.CatalogPath, catalogPath, StringComparison.Ordinal))
            {
                continue;
            }

            var resolvedAnimation = ResolveAnimation(
                entity, // 目标实体
                entry, // 资源条目
                animationName // 用户选择动作
            );
            entity.Data.Set(DataKey.PreviewCurrentAnimation, resolvedAnimation);
            if (string.IsNullOrWhiteSpace(resolvedAnimation))
            {
                entity.Events.Emit(GameEventType.Unit.StopAnimationRequested, new GameEventType.Unit.StopAnimationRequestedEventData());
                continue;
            }

            entity.Events.Emit(
                GameEventType.Unit.PlayAnimationRequested,
                new GameEventType.Unit.PlayAnimationRequestedEventData(
                    resolvedAnimation, // 动作名
                    true, // 强制重播
                    -1f // 不限制播放时长
                )
            );
        }
    }

    /// <summary>
    /// 生成单个预览实体。
    /// </summary>
    /// <param name="entry">资源条目。</param>
    /// <param name="position">生成位置。</param>
    private static VisualPreviewEntity? SpawnPreviewEntity(VisualPreviewEntry entry, Vector2 position)
    {
        var visualScene = ResourceManagement.Load<PackedScene>(
            entry.ResourceKey, // ResourcePaths 资源键
            entry.Category // 资源分类
        );
        if (visualScene == null)
        {
            return null;
        }

        var config = new VisualPreviewEntityConfig
        {
            Name = entry.SceneName,
            VisualScenePath = visualScene,
            PreviewDefaultAnimation = entry.DefaultAnimation,
            AnimationAutoDriveEnabled = false
        };

        var entity = EntityManager.Spawn<VisualPreviewEntity>(new EntitySpawnConfig
        {
            Config = config,
            UsingObjectPool = false,
            Position = position
        });
        if (entity == null)
        {
            return null;
        }

        entity.Data.Set(DataKey.PreviewResourceKey, entry.ResourceKey);
        entity.Data.Set(DataKey.PreviewResourcePath, entry.ResourcePath);
        entity.Data.Set(DataKey.PreviewResourceCategory, entry.Category.ToString());
        entity.Data.Set(DataKey.PreviewCatalogPath, entry.CatalogPath);
        entity.Data.Set(DataKey.PreviewDefaultAnimation, entry.DefaultAnimation);
        return entity;
    }

    /// <summary>
    /// 解析最终要播放的动作。
    /// </summary>
    /// <param name="entity">目标实体。</param>
    /// <param name="entry">资源条目。</param>
    /// <param name="requestedAnimation">用户选择动作。</param>
    private static string ResolveAnimation(VisualPreviewEntity entity, VisualPreviewEntry entry, string requestedAnimation)
    {
        var available = GetAvailableAnimations(entity);
        if (!string.IsNullOrWhiteSpace(requestedAnimation) && available.Contains(requestedAnimation))
        {
            return requestedAnimation;
        }

        if (!string.IsNullOrWhiteSpace(entry.DefaultAnimation) && available.Contains(entry.DefaultAnimation))
        {
            return entry.DefaultAnimation;
        }

        return available.Count > 0 ? available[0] : string.Empty;
    }

    /// <summary>
    /// 获取实体可用动作列表。
    /// </summary>
    /// <param name="entity">目标实体。</param>
    private static List<string> GetAvailableAnimations(VisualPreviewEntity entity)
    {
        return entity.Data.Get<List<string>>(
            DataKey.AvailableAnimations, // UnitAnimationComponent 缓存的动作列表
            new List<string>() // 非 AnimatedSprite2D 资源兜底为空列表
        );
    }

    /// <summary>
    /// 计算网格位置。
    /// </summary>
    /// <param name="index">资源索引。</param>
    private static Vector2 ResolveGridPosition(int index)
    {
        var column = index % ColumnCount;
        var row = index / ColumnCount;
        var totalWidth = (ColumnCount - 1) * ColumnSpacing;
        return new Vector2(column * ColumnSpacing - totalWidth * 0.5f, row * RowSpacing);
    }
}
