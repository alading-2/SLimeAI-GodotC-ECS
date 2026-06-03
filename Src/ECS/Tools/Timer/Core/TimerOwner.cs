/// <summary>
/// Timer 生命周期归属。业务 cleanup 应优先按 owner 取消，而不是依赖自由字符串 tag。
/// </summary>
public readonly record struct TimerOwner(TimerOwnerType Type, string Id)
{
    public static TimerOwner None => new(TimerOwnerType.None, string.Empty);

    public bool IsNone => Type == TimerOwnerType.None || string.IsNullOrWhiteSpace(Id);

    public override string ToString() => IsNone ? "none" : $"{Type}:{Id}";
}
