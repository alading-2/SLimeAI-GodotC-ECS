/// <summary>
/// Feature 上下文 - 贯穿一次生命周期操作与 Action 执行的统一上下文对象
///
/// 在 Granted/Removed/Activated/Execute/Ended 各阶段传递，也可直接作为 IFeatureAction.Execute 的数据载体。
/// 刻意不包含任何子系统专有类型（如 AbilityEntity / CastContext），
/// 保持 FeatureSystem 对上层系统的零依赖。
/// 调用方（如 AbilitySystem）通过 typed helper 写入专有上下文和执行结果。
/// </summary>
public class FeatureContext
{
    private object? _activationPayload;
    private object? _executionResult;

    /// <summary>拥有该 Feature 的实体</summary>
    public IEntity? Owner { get; set; }

    /// <summary>Feature 实体（承载 Data 与 Events 的任意 IEntity）</summary>
    public IEntity? Feature { get; set; }

    /// <summary>运行时实例（含 Owner/FeatureEntity 及状态快捷访问）</summary>
    public FeatureInstance? Instance { get; set; }

    /// <summary>单次运行阶段的 typed payload，优先通过 SetActivationPayload / TryGetActivation 访问。</summary>
    public object? ActivationPayload
    {
        get => _activationPayload;
        set => _activationPayload = value;
    }

    /// <summary>OnExecute 阶段的 typed result，优先通过 SetExecutionResult / TryGetExecutionResult 访问。</summary>
    public object? ExecutionResult
    {
        get => _executionResult;
        set => _executionResult = value;
    }

    /// <summary>触发源事件数据（OnEvent 触发时携带，其余为 null）</summary>
    [System.Obsolete("Source 是旧 raw object 入口；事件触发应改为 typed trigger payload。")]
    public object? Source { get; set; }

    //=======================Action的参数=====================
    /// <summary>Action 侧对 Source 的语义别名</summary>
    [System.Obsolete("Trigger 是旧 raw object 入口；FeatureAction 应使用 typed payload。")]
    public object? Trigger
    {
        get => Source;
        set => Source = value;
    }

    /// <summary>Action 间共享的临时数据（跨 Action 传值用）</summary>
    [System.Obsolete("ExtraData 是旧 raw object 临时字典；新 Action 应定义 typed context/result。")]
    public System.Collections.Generic.Dictionary<string, object> ExtraData { get; } = new();

    /// <summary>旧 ActivationData 兼容入口；新代码使用 ActivationPayload / SetActivationPayload。</summary>
    [System.Obsolete("使用 ActivationPayload / SetActivationPayload<T>() / TryGetActivation<T>()。")]
    public object? ActivationData
    {
        get => _activationPayload;
        set => _activationPayload = value;
    }

    /// <summary>旧 ExecuteResult 兼容入口；新代码使用 ExecutionResult / SetExecutionResult。</summary>
    [System.Obsolete("使用 ExecutionResult / SetExecutionResult<T>() / TryGetExecutionResult<T>()。")]
    public object? ExecuteResult
    {
        get => _executionResult;
        set => _executionResult = value;
    }

    /// <summary>写入本次执行的 typed activation payload。</summary>
    public void SetActivationPayload<T>(T payload)
    {
        _activationPayload = payload;
    }

    /// <summary>按类型读取本次执行的 activation payload。</summary>
    public T GetActivation<T>()
    {
        if (TryGetActivation<T>(out var payload))
        {
            return payload;
        }

        var actualType = _activationPayload?.GetType().Name ?? "null";
        throw new System.InvalidOperationException(
            $"FeatureContext.ActivationPayload 类型错误，期望 {typeof(T).Name}，实际 {actualType}");
    }

    /// <summary>尝试按类型读取本次执行的 activation payload。</summary>
    public bool TryGetActivation<T>(out T payload)
    {
        if (_activationPayload is T typedPayload)
        {
            payload = typedPayload;
            return true;
        }

        payload = default!;
        return false;
    }

    /// <summary>写入本次执行的 typed result。</summary>
    public void SetExecutionResult<T>(T result)
    {
        _executionResult = result;
    }

    /// <summary>按类型读取本次执行的 result。</summary>
    public T GetExecutionResult<T>()
    {
        if (TryGetExecutionResult<T>(out var result))
        {
            return result;
        }

        var actualType = _executionResult?.GetType().Name ?? "null";
        throw new System.InvalidOperationException(
            $"FeatureContext.ExecutionResult 类型错误，期望 {typeof(T).Name}，实际 {actualType}");
    }

    /// <summary>尝试按类型读取本次执行的 result。</summary>
    public bool TryGetExecutionResult<T>(out T result)
    {
        if (_executionResult is T typedResult)
        {
            result = typedResult;
            return true;
        }

        result = default!;
        return false;
    }
}
