using Godot;
using System.Collections.Generic;

// ==================================================================================
// GlobalEvents — 框架级全局事件 payload 定义
// ==================================================================================
//
// 此文件集中定义框架级 IGlobalEvent payload。所有事件均为 readonly record struct。
//
// 事件归属规则：
//   - 框架级全局事件（跨实体、跨系统）→ 本文件 GlobalEvents
//   - 模块专属事件 → 跟随 owner 模块目录（如 AbilityEvents.cs、UnitEvents.cs）
//   - 游戏专属事件 → 游戏侧事件目录，不放框架核心
//
// payload 设计约束：
//   - 必须是 readonly record struct（值类型、不可变、自动实现相等性）
//   - 框架级 payload 不包含 Godot engine type（Vector2 等 Godot 基础类型除外）
//   - 事件名使用过去式或完成式（EntitySpawned 而非 EntitySpawn）
// ==================================================================================

/// <summary>
/// 框架级全局事件 payload。所有事件实现 IGlobalEvent，在 WorldEventBus 发布和订阅。
/// </summary>
public static class GlobalEvents
{
    public readonly record struct EntitySpawned(IEntity Entity) : IGlobalEvent;

    public readonly record struct EntityDestroyed(IEntity Entity) : IGlobalEvent;

    public readonly record struct EntityMigrating(
        IEntity SourceEntity,
        string TargetEntityType,
        string ProfileName) : IGlobalEvent;

    public readonly record struct EntityMigrated(
        IEntity SourceEntity,
        IEntity TargetEntity,
        string ProfileName) : IGlobalEvent;

    public readonly record struct RelationshipAdded(
        string ParentEntityId,
        string ChildEntityId,
        string RelationType) : IGlobalEvent;

    public readonly record struct RelationshipRemoved(
        string ParentEntityId,
        string ChildEntityId,
        string RelationType) : IGlobalEvent;

    public readonly record struct GameStart() : IGlobalEvent;

    public readonly record struct GamePause() : IGlobalEvent;

    public readonly record struct GameResume() : IGlobalEvent;

    public readonly record struct GameOver(bool IsVictory) : IGlobalEvent;

    public readonly record struct WaveStarted(int WaveIndex) : IGlobalEvent;

    public readonly record struct WaveCompleted(int WaveIndex) : IGlobalEvent;

    public enum MouseSelectionInteractionKind
    {
        Click = 0,
        Box = 1
    }

    public enum MouseSelectionHitKind
    {
        None = 0,
        PhysicsPoint = 1,
        DistanceFallback = 2,
        BoxRect = 3
    }

    public readonly record struct MouseSelectionCompleted(
        IReadOnlyList<IEntity> Entities,
        IEntity? PrimaryEntity,
        Vector2 ScreenPosition,
        Vector2 WorldPosition,
        Rect2 ScreenRect,
        MouseSelectionHitKind HitKind,
        MouseSelectionInteractionKind InteractionKind) : IGlobalEvent;

    public readonly record struct MouseSelectionPreviewUpdated(
        Vector2 StartScreenPosition,
        Vector2 CurrentScreenPosition,
        Rect2 ScreenRect) : IGlobalEvent;

    public readonly record struct MouseSelectionMissed(
        Vector2 ScreenPosition,
        Vector2 WorldPosition,
        Rect2 ScreenRect,
        MouseSelectionInteractionKind InteractionKind) : IGlobalEvent;
}
