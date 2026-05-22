/// <summary>
/// 攻击取消原因。
/// </summary>
public enum AttackCancelReason
{
    TargetDead,
    TargetOutOfRange,
    TargetInvalid,
    SelfDead,
    SelfDisabled,
    ExternalCancel,
    ComponentCleanup
}
