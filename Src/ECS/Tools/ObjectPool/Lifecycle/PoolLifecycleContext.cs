using Godot;

/// <summary>
/// 对象池节点生命周期策略上下文。
/// </summary>
public readonly record struct PoolLifecycleContext(
    string PoolName,
    string? ParentPath,
    Vector2 ParkingPosition,
    bool IsDeferredActivation,
    long PhysicsFrame,
    string Reason
);
