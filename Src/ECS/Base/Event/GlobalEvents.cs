using Godot;
using System.Collections.Generic;

/// <summary>
/// World 级全局事件。
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
