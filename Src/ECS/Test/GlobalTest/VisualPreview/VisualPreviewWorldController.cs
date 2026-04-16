using Godot;
using Slime.Config.Test;
using System;
using System.Collections.Generic;

/// <summary>
/// 独立视觉预览世界控制器。
/// </summary>
internal sealed class VisualPreviewWorldController
{
    private sealed class PreviewRuntimeState
    {
        public required VisualPreviewEntity Entity { get; init; }
        public AnimatedSprite2D? Sprite { get; init; }
        public required List<string> AvailableAnimations { get; init; }
        public Action? AnimationFinishedHandler { get; set; }
        public string CurrentPreviewAnimation { get; set; } = string.Empty;
    }

    private const int ColumnCount = 5;
    private const float ColumnSpacing = 220f;
    private const float RowSpacing = 220f;

    private static readonly Log _log = new(nameof(VisualPreviewWorldController));

    private readonly List<VisualPreviewEntity> _entities = new();
    private readonly Dictionary<VisualPreviewEntity, VisualPreviewEntry> _entriesByEntity = new();
    private readonly Dictionary<VisualPreviewEntity, PreviewRuntimeState> _runtimeByEntity = new();

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
            _runtimeByEntity[entity] = BuildRuntimeState(
                entity, // 预览实体
                entries[i] // 资源条目
            );
        }
    }

    /// <summary>
    /// 清理全部预览实体。
    /// </summary>
    public void Clear()
    {
        foreach (var runtime in _runtimeByEntity.Values)
        {
            UnbindAnimationFinished(runtime);
        }

        foreach (var entity in _entities)
        {
            if (GodotObject.IsInstanceValid(entity))
            {
                EntityManager.Destroy(entity);
            }
        }

        _entities.Clear();
        _entriesByEntity.Clear();
        _runtimeByEntity.Clear();
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

            if (!_runtimeByEntity.TryGetValue(entity, out var runtime))
            {
                continue;
            }

            foreach (var animationName in runtime.AvailableAnimations)
            {
                result.Add(animationName);
            }
        }

        var animations = new List<string>(result.Count);
        foreach (var animationName in result)
        {
            animations.Add(animationName);
        }

        return animations;
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

            if (!_runtimeByEntity.TryGetValue(entity, out var runtime))
            {
                continue;
            }

            var resolvedAnimation = ResolveAnimation(
                runtime, // 预览缓存
                entry, // 资源条目
                animationName // 用户选择动作
            );
            PlayResolvedAnimation(
                runtime, // 预览缓存
                resolvedAnimation // 最终动作
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
            PreviewDefaultAnimation = entry.DefaultAnimation
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
    /// <param name="runtime">预览运行时状态。</param>
    /// <param name="entry">资源条目。</param>
    /// <param name="requestedAnimation">用户选择动作。</param>
    private static string ResolveAnimation(PreviewRuntimeState runtime, VisualPreviewEntry entry, string requestedAnimation)
    {
        var available = runtime.AvailableAnimations;
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
    /// 构建预览实体运行时状态。
    /// </summary>
    /// <param name="entity">预览实体。</param>
    /// <param name="entry">资源条目。</param>
    private PreviewRuntimeState BuildRuntimeState(VisualPreviewEntity entity, VisualPreviewEntry entry)
    {
        var sprite = FindPreviewSprite(entity);
        var availableAnimations = GetSpriteAnimationNames(sprite);
        var runtime = new PreviewRuntimeState
        {
            Entity = entity,
            Sprite = sprite,
            AvailableAnimations = availableAnimations
        };

        BindAnimationFinished(runtime);
        entity.Data.Set(DataKey.PreviewCurrentAnimation, string.Empty);
        return runtime;
    }

    /// <summary>
    /// 在预览实体上查找唯一 AnimatedSprite2D。
    /// </summary>
    /// <param name="entity">预览实体。</param>
    private static AnimatedSprite2D? FindPreviewSprite(VisualPreviewEntity entity)
    {
        var visualRoot = entity.GetNodeOrNull("VisualRoot");
        if (visualRoot == null)
        {
            return null;
        }

        var foundSprites = new List<AnimatedSprite2D>();
        CollectAnimatedSprites(
            visualRoot, // 视觉根节点
            foundSprites // 收集结果
        );

        if (foundSprites.Count > 1)
        {
            _log.Error($"[{entity.Name}] VisualRoot 下找到 {foundSprites.Count} 个 AnimatedSprite2D，只会控制第一个，这属于异常资源结构");
        }

        return foundSprites.Count > 0 ? foundSprites[0] : null;
    }

    /// <summary>
    /// 递归收集视觉节点树中的 AnimatedSprite2D。
    /// </summary>
    /// <param name="node">起始节点。</param>
    /// <param name="result">收集列表。</param>
    private static void CollectAnimatedSprites(Node node, List<AnimatedSprite2D> result)
    {
        if (node is AnimatedSprite2D sprite)
        {
            result.Add(sprite);
        }

        foreach (Node child in node.GetChildren())
        {
            CollectAnimatedSprites(
                child, // 子节点
                result // 收集列表
            );
        }
    }

    /// <summary>
    /// 读取 SpriteFrames 里的全部动画名称。
    /// </summary>
    /// <param name="sprite">目标动画节点。</param>
    private static List<string> GetSpriteAnimationNames(AnimatedSprite2D? sprite)
    {
        var result = new List<string>();
        var spriteFrames = sprite?.SpriteFrames;
        if (spriteFrames == null)
        {
            return result;
        }

        foreach (var animationName in spriteFrames.GetAnimationNames())
        {
            result.Add(animationName);
        }

        return result;
    }

    /// <summary>
    /// 绑定预览动画结束回调，用于持续回放当前预览动作。
    /// </summary>
    /// <param name="runtime">预览运行时状态。</param>
    private void BindAnimationFinished(PreviewRuntimeState runtime)
    {
        if (runtime.Sprite == null)
        {
            return;
        }

        Action handler = () => OnPreviewAnimationFinished(
            runtime.Entity // 当前预览实体
        );
        runtime.AnimationFinishedHandler = handler;
        runtime.Sprite.AnimationFinished += handler;
    }

    /// <summary>
    /// 解绑预览动画结束回调。
    /// </summary>
    /// <param name="runtime">预览运行时状态。</param>
    private static void UnbindAnimationFinished(PreviewRuntimeState runtime)
    {
        if (runtime.Sprite == null || runtime.AnimationFinishedHandler == null)
        {
            return;
        }

        runtime.Sprite.AnimationFinished -= runtime.AnimationFinishedHandler;
        runtime.AnimationFinishedHandler = null;
    }

    /// <summary>
    /// 预览动画结束后重新播放当前预览动作。
    /// </summary>
    /// <param name="entity">对应的预览实体。</param>
    private void OnPreviewAnimationFinished(VisualPreviewEntity entity)
    {
        if (!_runtimeByEntity.TryGetValue(entity, out var runtime)
            || runtime.Sprite == null
            || string.IsNullOrWhiteSpace(runtime.CurrentPreviewAnimation)
            || !runtime.AvailableAnimations.Contains(runtime.CurrentPreviewAnimation))
        {
            return;
        }

        runtime.Sprite.Play(runtime.CurrentPreviewAnimation); // 预览场景下持续回放当前动作
    }

    /// <summary>
    /// 播放已解析出的目标动作。
    /// </summary>
    /// <param name="runtime">预览运行时状态。</param>
    /// <param name="animationName">最终动作名。</param>
    private static void PlayResolvedAnimation(PreviewRuntimeState runtime, string animationName)
    {
        runtime.CurrentPreviewAnimation = animationName;
        runtime.Entity.Data.Set(
            DataKey.PreviewCurrentAnimation,
            animationName // 当前预览动作
        );

        if (runtime.Sprite == null || string.IsNullOrWhiteSpace(animationName))
        {
            runtime.Sprite?.Stop();
            return;
        }

        runtime.Sprite.Play(animationName);
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
