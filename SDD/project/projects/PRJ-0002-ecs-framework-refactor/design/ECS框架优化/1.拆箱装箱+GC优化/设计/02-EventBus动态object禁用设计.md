# EventBus 动态 object 禁用设计

## 当前结论

EventBus 的泛型 payload 设计应保留；动态 object 入口应删除或至少从框架主链路禁用。

用户判断“Event 完全是 AI 写的，不需要 object 兼任”成立。Event 是协议，不是人工脚本快捷入口。AI-first 下事件类型本身就是事件名，payload 就是契约，继续保留 `EmitDynamic(object)` 会让事件重新退回“运行时猜类型”。

## Data 完成后的重新裁决

`SDD-0031` 已完成 Data runtime 主链路 hard cutover，因此 Event 不再需要和 Data 一起排队。当前下一步应把 Event dynamic object 与 Feature / Ability object bridge 作为同一个协议边界处理：

- 只缓存 `EmitDynamic` 的反射方法不推荐；这只能减少反射和数组分配，不能修复事件 payload 契约漂移。
- 只删除 EventBus dynamic API 也不够；`EmitEventAction.EventData object?`、`FeatureContext.ActivationData/ExecuteResult object?` 和 `TriggerComponent.OnEventTriggered(object)` 会继续形成 object 绕路。
- 推荐执行 SDD 名称：`Event + Feature/Ability Typed Execution Boundary`。Event 侧只负责提供 typed bus 和禁用 dynamic object；Feature / Ability 侧负责 typed event action、typed Execute adapter 和 typed trigger binding。
- `TriggerComponent` 当前 OnEvent 订阅仍是 TODO 日志，不是稳定依赖。应趁这个切片把事件类型字符串迁成 typed trigger binding id，而不是为了未完成路径保留 `Action<object>`。

## 当初为什么这么设计

Event 的现行文档已经把旧字符串事件名迁到 `readonly record struct` payload 类型，这是正确方向。`EmitDynamic` / `OnDynamic` 的存在主要是为了 Feature 等数据驱动场景：配置里可以塞一个事件 payload object，然后运行时动态发出。

这个目的可以理解，但它有两个问题：

- “数据驱动发任意事件”没有静态契约，AI 无法知道 payload 的字段和归属。
- 动态发事件必须把 struct payload 装成 object，再通过反射调回泛型 `Trigger<T>`。

目前 `TriggerComponent` 的 OnEvent 订阅也还处于 TODO 状态，说明 dynamic event 并不是稳定、不可替代的主链路。

## 源码证据

| 文件 | 证据 | 风险 |
| --- | --- | --- |
| `EventBus.cs` | `OnDynamic(Type eventType, Action<object> handler)` | 动态订阅用 object handler |
| `EventBus.cs` | `EmitDynamic(object eventData)` | payload struct 先装箱为 object |
| `EventBus.cs` | `GetMethod` / `MakeGenericMethod` / `Invoke(this, new[] { eventData })` | 每次动态 emit 有反射和数组分配 |
| `EventBus.cs` | `sub.Handler is Action<object> objectHandler` | 泛型派发中保留 object 分支 |
| `EmitEventAction.cs` | `public object? EventData` + `EmitDynamic(EventData)` | Feature action 绕过 typed event |
| `TriggerComponent.cs` | 注释“使用 On<object> 配合 EventBus”但实际 TODO | 设计意图仍是动态 object |

## 目标架构

### 1. EventBus 只保留 typed API

保留：

```csharp
public void On<T>(Action<T> handler, int priority = 0) where T : struct;
public void Once<T>(Action<T> handler, int priority = 0) where T : struct;
public void Off<T>(Action<T> handler) where T : struct;
public void Emit<T>(in T data) where T : struct;
```

删除或禁用：

```csharp
OnDynamic(Type, Action<object>)
OffDynamic(Type, Action<object>)
EmitDynamic(object)
Action<object> dispatch branch
```

如果短期必须保留，必须标记：

```csharp
[Obsolete("仅限迁移期调试。框架事件必须使用 typed payload；object 会导致装箱、反射和契约漂移。")]
```

并且不得被 `Src/ECS/Capabilities` 业务调用。

### 2. Feature event action 改 typed registry

当前 `EmitEventAction.EventData object?` 不应继续存在。替代方案：

```csharp
public interface IFeatureEventAction
{
    string ActionId { get; }
    void Execute(FeatureContext ctx);
}

public abstract class FeatureEventAction<TEvent> : IFeatureEventAction
    where TEvent : struct
{
    public void Execute(FeatureContext ctx)
    {
        var evt = BuildEvent(ctx);
        ResolveBus(ctx).Emit(evt);
    }

    protected abstract TEvent BuildEvent(FeatureContext ctx);
}
```

数据驱动配置只保存 `ActionId` 和参数，不保存任意 object payload。AI 新增事件动作时，必须写一个 typed action 类或由 generator 生成。

### 3. OnEvent trigger 改 typed trigger binding

不要让 `AbilityTriggerEvent` 用字符串指向任意事件类型并通过 `Action<object>` 订阅。

推荐：

- DataOS 保存 `TriggerBindingId`，例如 `trigger.on_unit_damaged`。
- C# 注册 typed binding：

```csharp
public interface IAbilityTriggerBinding
{
    string BindingId { get; }
    void Subscribe(AbilityEntity ability, IEntity owner);
    void Unsubscribe(AbilityEntity ability, IEntity owner);
}
```

每个 binding 内部使用 `owner.Events.On<GameEventType.Unit.Damaged>(...)`，并构造 typed `CastContext`。

## 迁移步骤

1. 扫描 `EmitDynamic|OnDynamic|Action<object>` 调用点，确认 Feature/Trigger/TestSystem/diagnostics 边界。
2. 在同一 SDD 中新增 typed Feature event action registry；`EmitEventAction.EventData object?` 不再作为业务动作入口。
3. 新增 Ability trigger binding registry，替代 `AbilityTriggerEvent` 事件类型字符串 + `Action<object>` 订阅意图。
4. Feature / Ability Execute 改 typed adapter 后，再删除或禁用 EventBus dynamic API 和 object branch，避免中间状态无法编译。
5. 更新 `DocsAI/ECS/Runtime/Event/Event系统说明.md`、Feature/Ability 文档和 `ecs-event` / `feature-system` / `ability-system` skill。

## 验证门禁

```bash
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
rg -n "EmitDynamic|OnDynamic|OffDynamic|Action<object>|object EventData" Src/ECS/Runtime/Event Src/ECS/Capabilities
```

新增测试：

- EventBus typed emit 不装箱路径的行为测试。
- Feature typed event action 发出预期 payload。
- Ability trigger binding 能订阅/取消 typed event。
- 事件 handler 内异常隔离、priority、once、重入 guard 保持现有语义。

## 不推荐

- 不推荐只缓存 `MakeGenericMethod`。这只能降低反射开销，不能解决 Event 协议 object 化。
- 不推荐保留 `Action<object>` 给“通用监听”。通用监听应是 diagnostics 层 snapshot，不是业务协议。

## Must Confirm

- 是否接受 Event 与 Feature / Ability 合并成一个协议边界 SDD，而不是单独给 EventBus 做反射缓存。
- 是否接受删除或 `[Obsolete]` 禁用 EventBus dynamic API。
- 是否接受 `AbilityTriggerEvent` 从“事件类型字符串”改为 typed trigger binding id。
