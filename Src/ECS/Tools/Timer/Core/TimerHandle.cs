/// <summary>
/// Timer 稳定身份。Id 表示槽位，Generation 用于拒绝复用槽位后的旧 handle。
/// </summary>
public readonly record struct TimerHandle(int Id, int Generation)
{
    public bool IsValid => Id > 0 && Generation > 0;
}
