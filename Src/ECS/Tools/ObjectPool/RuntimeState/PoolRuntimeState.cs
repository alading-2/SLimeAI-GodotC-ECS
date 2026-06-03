using Godot;

/// <summary>
/// 池化节点运行时状态。
/// <para>该状态属于对象池基础设施，不写入 Entity.Data / DataOS。</para>
/// </summary>
public readonly record struct PoolRuntimeState(
    string PoolName,
    string NodeName,
    string NodeType,
    bool IsInPool,
    bool CollisionLogicActive,
    long CollisionReadyPhysicsFrame,
    long LastAcquirePhysicsFrame,
    long LastReleasePhysicsFrame,
    Vector2 ParkingPosition,
    Vector2 LastAcquirePosition,
    Vector2 LastReleasePosition,
    bool DetachFallbackEnabled
);

/// <summary>
/// 池化节点只读观测快照。
/// </summary>
public readonly record struct PoolNodeStateSnapshot(
    ulong InstanceId,
    string PoolName,
    string NodeName,
    string NodeType,
    bool IsInsideTree,
    bool IsInPoolMeta,
    bool IsInPool,
    bool CollisionLogicActive,
    long CollisionReadyPhysicsFrame,
    long LastAcquirePhysicsFrame,
    long LastReleasePhysicsFrame,
    Vector2 ParkingPosition,
    Vector2 LastAcquirePosition,
    Vector2 LastReleasePosition,
    bool DetachFallbackEnabled
);
