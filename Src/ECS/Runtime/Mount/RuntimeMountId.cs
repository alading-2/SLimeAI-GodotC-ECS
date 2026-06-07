using System;

/// <summary>
/// Runtime 挂载点稳定标识。
/// </summary>
public readonly record struct RuntimeMountId(string Value)
{
    public bool IsEmpty => string.IsNullOrWhiteSpace(Value);

    public override string ToString() => Value;

    public static RuntimeMountId From(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Runtime mount id 不能为空。", nameof(value));

        return new RuntimeMountId(value);
    }
}
