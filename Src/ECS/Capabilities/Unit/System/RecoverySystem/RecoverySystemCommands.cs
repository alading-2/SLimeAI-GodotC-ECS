/// <summary>
/// 注册恢复实体命令。
/// </summary>
/// <param name="Entity">目标实体。</param>
public readonly record struct RecoveryRegisterRequest(IEntity Entity);

/// <summary>
/// 注销恢复实体命令。
/// </summary>
/// <param name="Entity">目标实体。</param>
public readonly record struct RecoveryUnregisterRequest(IEntity Entity);

/// <summary>
/// 恢复系统注册命令结果。
/// </summary>
/// <param name="Handled">是否已处理。</param>
public readonly record struct RecoveryRegistrationResult(bool Handled);
