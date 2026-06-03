/// <summary>
/// Timer 用途。Gameplay timer 不应只靠 tag 表达语义。
/// </summary>
public enum TimerPurpose
{
    None,
    Cooldown,
    Charge,
    PeriodicTrigger,
    AttackWindup,
    AttackRecovery,
    AttackValidation,
    Lifecycle,
    Revive,
    ContactDamage,
    DoT,
    SpawnWave,
    SpawnCheck,
    Recovery,
    AIWait,
    DecoratorCooldown,
    EffectLifetime,
    ChainDelay,
    Debug,
    Test
}
