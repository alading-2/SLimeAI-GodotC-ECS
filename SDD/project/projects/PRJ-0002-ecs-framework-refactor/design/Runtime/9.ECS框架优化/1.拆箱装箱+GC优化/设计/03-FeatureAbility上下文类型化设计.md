# Feature / Ability 上下文类型化设计

## 当前结论

Event 禁 object 后，Feature / Ability 也必须同步类型化。否则 object 会从 `EventBus` 迁移到 `FeatureContext.ActivationData` 和 `ExecuteResult`，问题没有消失，只是换了入口。

当前 FeatureSystem 设计强调“Feature 不依赖 Ability 专有类型”，这个边界是对的；错误在于用 `object?` 实现解耦。AI-first 更适合用 typed adapter / typed execution contract 实现解耦。

## Data 完成后的重新裁决

Data 主链路已经由 `SDD-0031` 完成 hard cutover，因此 Feature / Ability 不再是“跟随 Data”的次级任务，而是 Event dynamic object 删除后的必需配套。重新分析后裁决如下：

- Feature / Ability 必须和 Event dynamic object removal 同批设计。只删 EventBus dynamic API，不改 `ActivationData/ExecuteResult object?`，object 会从 Feature 执行链路绕回去。
- 不建议把 Feature 的 Granted / Removed / Activated / Ended 全生命周期都泛型化。生命周期阶段主要传 owner、feature、instance、reason，普通 `FeatureLifecycleContext` 足够。
- 真正需要 typed 的是 Execute 阶段：Ability 传入 `CastContext`，handler 返回 `AbilityExecutedResult`，这两个类型不应再靠 `object?` 和 `is` 判断连接。
- `ExtraData Dictionary<string, object>` 不应作为长期 action 间通信入口；如果确实需要跨 action scratchpad，应使用 typed scratch key 或 Capability-owned context 字段。
- `CastContext.SourceEventData object?` 应由 TriggerBinding 显式投影，而不是把原始事件 payload 塞进 Ability。

## 当初为什么这么设计

Feature 是通用能力，Ability 是其中一个调用方。为了避免 Feature 反向依赖 Ability，当前设计把调用方专有数据放进：

```csharp
FeatureContext.ActivationData object?
FeatureContext.ExecuteResult object?
IFeatureHandler.OnExecute(FeatureContext): object?
```

Ability 再通过：

```csharp
featureContext.TryGetActivationData<CastContext>(out var context)
featureCtx.ExecuteResult is AbilityExecutedResult
```

完成转型。这是典型的“宽口桥接”：短期减少接口数量，长期牺牲契约。

## 源码证据

| 文件 | 证据 | 风险 |
| --- | --- | --- |
| `FeatureContext.cs` | `ActivationData object?`、`ExecuteResult object?`、`Source object?`、`ExtraData Dictionary<string, object>` | 通用上下文全靠运行时转型 |
| `IFeatureHandler.cs` | `object? OnExecute(FeatureContext context)` | handler 返回值无静态契约 |
| `FeatureSystem.cs` | `ctx.ExecuteResult = handler.OnExecute(ctx)` | Feature core 接收任意结果 |
| `AbilityFeatureHandler.cs` | `ActivationData 不是 CastContext` 日志错误 | Ability 契约运行时才发现错误 |
| `AbilitySystem.cs` | `ActivationData = abilityContext`、`featureCtx.ExecuteResult is AbilityExecutedResult` | Ability 结果依赖 object 转型 |
| `CastContext.cs` | `SourceEventData object?`、`ResponseContext EventContext?` | 触发源事件仍是 object |

## 目标原则

- Feature core 保持不依赖 Ability namespace。
- 每个 Capability 显式声明自己的 execution context 和 result 类型。
- Feature handler registry 知道 handler 的输入/输出类型，注册时校验。
- object 只允许存在于 diagnostics / debug 扩展点，不作为执行协议。

## 推荐架构

### 1. Execute-only typed context

```csharp
public sealed class FeatureExecutionContext<TInput, TResult>
{
    public IEntity Owner { get; }
    public IEntity Feature { get; }
    public FeatureInstance Instance { get; }
    public TInput Input { get; }
    public TResult? Result { get; private set; }

    public void SetResult(TResult result) => Result = result;
}
```

Ability 使用：

```csharp
FeatureExecutionContext<CastContext, AbilityExecutedResult>
```

Feature core 不需要知道 Ability 细节，只处理泛型接口。Granted / Removed / Activated / Ended 不使用这个泛型上下文。

### 2. Typed handler 接口

```csharp
public interface IFeatureHandler<TInput, TResult> : IFeatureHandlerContract
{
    string FeatureId { get; }
    TResult Execute(FeatureExecutionContext<TInput, TResult> context);
}
```

`IFeatureHandlerContract` 只暴露 metadata：

```csharp
public interface IFeatureHandlerContract
{
    string FeatureId { get; }
    Type InputType { get; }
    Type ResultType { get; }
}
```

registry 注册时记录类型，调用方按 expected type 获取：

```csharp
FeatureHandlerRegistry.Get<CastContext, AbilityExecutedResult>(handlerId)
```

如果类型不匹配，在触发前 fail fast，而不是进入 handler 后日志报错。

### 3. Feature lifecycle 与 Execute 分离

Granted/Removed/Enabled/Disabled/Ended 可以继续用非泛型 lifecycle context，因为它们主要传 owner/feature/instance。

Execute 是唯一需要 typed input/result 的阶段：

```text
OnGranted(FeatureLifecycleContext)
OnActivated(FeatureLifecycleContext)
Execute<TInput,TResult>(FeatureExecutionContext<TInput,TResult>)
OnEnded(FeatureLifecycleContext, reason)
```

### 4. `SourceEventData object?` 替换

OnEvent trigger 不应传原始 object 事件。改为 typed source：

```csharp
public readonly record struct AbilityTriggerSource<TEvent>(TEvent Event)
    where TEvent : struct;
```

或者由 trigger binding 把事件字段投影到 `CastContext` 的明确字段，不再把整个事件 object 塞进去。

## 迁移步骤

1. 新增 `FeatureLifecycleContext`，承载 owner / feature / instance / source metadata；不要泛型化生命周期。
2. 新增 `FeatureExecutionContext<TInput, TResult>` 与 typed handler registry。
3. 改 `AbilityFeatureHandler` 为 typed Execute adapter，例如 `IFeatureHandler<CastContext, AbilityExecutedResult>`。
4. 改 `FeatureSystem`：lifecycle 继续走普通 context，Execute 阶段通过 typed registry 调用。
5. 改 `AbilitySystem.EmitAbilityExecutedEvent`：不再读 `object ExecuteResult`。
6. 在 `Event + Feature/Ability Typed Execution Boundary` 联合 SDD 中同步改 `EmitEventAction` 和 `TriggerComponent`：typed trigger binding 将事件字段投影到 `CastContext`，不再设置 `SourceEventData object?`。
7. 清理或限制 `FeatureContext.ExtraData Dictionary<string, object>`；如确需 action 间共享，改为 typed scratchpad key。

## 验证门禁

```bash
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
rg -n "ActivationData|ExecuteResult|OnExecute\\(FeatureContext|object\\? OnExecute|SourceEventData|Dictionary<string, object>" Src/ECS/Capabilities/Feature Src/ECS/Capabilities/Ability
```

新增测试：

- handler 注册时输入/输出类型不匹配会 fail fast。
- Ability trigger 能拿到 `AbilityExecutedResult`，不经 object 转型。
- 没有 handler / handler 未注册时仍能结构化失败。
- Feature lifecycle 事件不依赖 Ability 类型。

## 不推荐

- 不推荐把 `FeatureContext` 继续做成“什么都能塞”的万能上下文。
- 不推荐为了 Feature 不依赖 Ability 而牺牲类型系统；typed generic contract 可以同时满足解耦和类型安全。

## Must Confirm

- 是否接受只类型化 Feature Execute 阶段，lifecycle context 暂不泛型化。
- 是否接受每个 Capability 自己注册 typed handler adapter，而不是复用 `object? OnExecute`。
- 是否接受 `CastContext.SourceEventData` 由 typed trigger binding 投影替代。
