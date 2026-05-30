# 旧 ECS 框架 Event 系统问题分析与优化方向

> 更新：2026-05-26
> 范围：`Resources/Else/brotato-my/Src/ECS/Base/Event/`、`SlimeAI/Src/ECS/Base/Event/`、`SlimeAI/Data/EventType/`、事件调用点。
> 新方向：删掉 `const string` 事件名，只保留 `readonly record struct` payload 作为事件标识。砍掉一半冗余，方向简单清晰。

## 0. 核心结论

旧 ECS 的 Event 系统不应该被整体重构，也不应该先收缩能力。

需要完整保留的能力：

- `EventBus.On/Off/Once/Emit`。
- `EventContext`，包括流程状态、失败原因、传播控制和结果承载。
- `EventPriority`。
- 派发中延迟移除。
- 同事件重入保护。
- handler 异常隔离。
- `GlobalEventBus` 作为全局事件入口。

本轮优化方向：**删掉 `const string` 事件名，只保留 `readonly record struct` payload。**

- 每个事件当前被拆成 `const string EventName` 与 `readonly record struct EventData` 两部分，删掉字符串这一半，冗余直接减半。
- `readonly record struct` 本身就是事件标识，不需要额外的字符串主键。
- `On<T>` / `Emit<T>` 的 payload 类型天然与事件绑定，不再需要手动传事件名。
- `SlimeAI/Data/EventType` 目录保留，同类事件放同一个文件。

EventContext 不改、不收缩；Once 保留；Priority 保留；GlobalEventBus 保留。

## 1. EventContext 的定位：必须保留

`EventContext` 不是冗余能力。它解决的是普通通知事件之外的另一类需求：**事件执行过程中的上下文与结果承载**。

典型用途：

- 多个订阅者参与一次检查或拦截。
- 发布者发出请求后，需要订阅者写入结果。
- 事件处理过程中需要记录是否成功、失败原因和是否继续传播。
- Ability、Damage、Feature 等流程中，局部模块需要通过事件链参与决策，但不希望直接互相调用。

旧实现中的核心能力：

```csharp
public class EventContext
{
    public bool IsHandled { get; protected set; }
    public bool IsPropagationStopped { get; private set; }
    public bool Success { get; protected set; } = true;
    public string? FailReason { get; protected set; }

    private object? _result;

    public void StopPropagation() => IsPropagationStopped = true;
    public void SetFailed(string reason) { ... }
    public void SetResult<T>(T result) { ... }
    public T? GetResult<T>() { ... }
}
```

这个能力不应该丢。它与普通事件通知不是同一类语义：

- 普通事件：告诉订阅者“某事发生了”。
- Context 事件：让订阅者参与“某事如何执行 / 是否成功 / 返回什么结果”。

因此后续文档与实现不再把 `EventContext` 作为简化对象。

## 2. EventContext 目前只需要补规则，不需要改结构

当前不建议把 `EventContext` 改成泛型，也不建议删除 `SetResult<T>` / `GetResult<T>`。

原因：

- 用户已经明确要求 `EventContext` 不改、不收缩。
- 旧 ECS 的核心目标是先回到稳定可理解的基线。
- `object? _result` 虽然不是最强类型安全，但它足够灵活，适合旧框架当前阶段。
- 过早改成 `EventContext<TResult>` 会扩大调用点修改面，偏离“只改事件调用方式”的主线。

需要补充的是使用规则：

- `EventContext` 只用于需要执行结果、失败原因、处理状态或传播控制的事件。
- 纯通知事件不强制使用 `EventContext`。
- `SetResult<T>` 表示某个订阅者写入了结果，并将 context 标记为 handled。
- `SetFailed` 表示流程失败或被阻止，并记录第一个失败原因。
- `StopPropagation` 的语义应被 EventBus 明确支持：当 context 已停止传播时，不再调用后续订阅者。
- 如果多个订阅者写入结果，暂时沿用旧行为，不在本轮改变覆盖规则。

## 3. Once 应该保留

`Once` 是 EventBus 的基础实用能力，不是冗余。

它适合：

- 等一次动画结束。
- 等一次技能执行完成。
- 等一次碰撞进入。
- 等一次对象生命周期事件。
- 测试中监听一次事件。

旧实现已经通过延迟移除避免派发中修改集合的问题，因此 `Once` 保留是合理的。

后续如果优化调用方式，只应从：

```csharp
Once<T>(string eventName, Action<T> handler)
```

演进到某种更安全的调用形式，而不是删除 `Once`。

## 4. 核心问题：const string 与 payload 双写

当前 `SlimeAI/Data/EventType` 中的事件定义类似：

```csharp
public static partial class GameEventType
{
    public static partial class Global
    {
        public const string WaveStarted = "global:wave_started";          // ← 这一半是冗余的
        public readonly record struct WaveStartedEventData(int WaveIndex);  // ← 这一半就够了

        public const string WaveCompleted = "global:wave_completed";
        public readonly record struct WaveCompletedEventData(int WaveIndex);
    }
}
```

每个事件被拆成两样东西：`const string` 事件名 + `readonly record struct` payload。二者一一对应，但需要手动维护同步。

**删掉 `const string` 这一半，冗余直接减半。** `readonly record struct` 本身就是事件标识，不需要额外的字符串主键。

## 5. 事件定义目录：继续放 `SlimeAI/Data/EventType`，不放 DataOS

### 5.1 不建议放 `SlimeAI/DataOS`

`DataOS` 更适合承载：

- 数据 schema。
- 配置、表格、快照。
- migration。
- authoring 数据。
- 数据验证和生成。

Event 类型定义是 C# runtime contract，不是可配置数据资产。把事件定义放进 `DataOS` 会制造新的边界混乱：

- Event 会被误解为数据资产。
- DataOS 会被迫依赖 ECS runtime 类型，如 `IEntity`、`DamageType`、`AbilityExecutedResult`。
- DataOS 的 schema/migration 语义和 EventBus 的运行时语义不同。

因此不建议把事件定义迁到 `SlimeAI/DataOS`。

### 5.2 短期建议保留 `SlimeAI/Data/EventType`

在回退阶段，建议继续使用：

```text
SlimeAI/Data/EventType/
```

理由：

- 这是旧 ECS 的既有事实源位置，回退成本最低。
- 事件定义已经按域拆分：`Ability`、`Global`、`Unit`、`Feature`、`UI` 等。
- 同类事件放同一个文件可读性较好，例如技能事件放 `GameEventType_Ability_*.cs`。
- 用户当前认可“同一类事件放同一个文件”。
- 先保持目录稳定，避免把“事件调用方式优化”和“目录重构”混在一起。

### 5.3 目录命名的长期问题

`SlimeAI/Data/EventType` 的问题是名字容易让人误会它属于 Data 系统。

长期可以考虑两个方向，但不是当前优先级：

| 方案 | 说明 | 当前建议 |
| --- | --- | --- |
| 保留 `SlimeAI/Data/EventType` | 最小变更，延续旧 ECS 事实源 | 推荐短期采用 |
| 改到 `SlimeAI/Src/ECS/Base/Event/Types` | 更贴近 Event runtime，语义更清楚 | 可作为后续目录整理议题 |
| 放到 `SlimeAI/DataOS/EventType` | 与 DataOS 语义冲突 | 不推荐 |

当前结论：**事件定义继续放 `SlimeAI/Data/EventType`，不要放 DataOS。**

## 6. 同类事件放同一个文件是合理的

AI-first 框架的“一事件一文件”有一个优点：每个事件的 owner 清晰，重命名和搜索直接。

但它也有明显缺点：

- 文件数量快速膨胀。
- 简单事件被拆得太碎。
- 查看一个系统的事件全貌需要打开多个文件。
- 对小型 Godot C# 框架不一定划算。

旧 ECS 的“同类事件一个文件”更适合当前项目：

```text
GameEventType_Ability_Execution.cs
GameEventType_Ability_Cooldown.cs
GameEventType_Unit_Health.cs
GameEventType_Global_Wave.cs
```

这种组织方式的优点：

- 同一领域事件集中，可读性高。
- 文件数量可控。
- 与模块边界基本一致。
- AI 和人类能一次看到某个领域的事件协议。

需要补强的是每个文件内部的规范，而不是改成一事件一文件。

建议每个事件块固定包含：

- payload `readonly record struct`（即事件本身）。
- 发布者说明。
- 订阅者/用途说明。
- 事件语义：通知 / 请求 / 命令 / 上下文事件。
- 作用域：Entity / Global / UI / Test。

## 7. 确定方向：删掉 const string，payload 做主键

### 7.1 改动内容

**Data/EventType 定义变化：**

```csharp
// 旧
public static partial class GameEventType
{
    public static partial class Global
    {
        public const string WaveStarted = "global:wave_started";
        public readonly record struct WaveStartedEventData(int WaveIndex);
    }
}

// 新
public static partial class GameEventType
{
    public static partial class Global
    {
        /// <summary>波次开始。GlobalEventBus 是唯一 producer。</summary>
        public readonly record struct WaveStarted(int WaveIndex);
    }
}
```

变化要点：

- 删掉所有 `const string`。
- `XxxEventData` 改名为 `Xxx`——payload 就是事件本身，不需要 "Data" 后缀。
- 目录和文件结构不变，同类事件仍放同一个文件。

**EventBus 内部变化：**

```csharp
// 旧
private readonly Dictionary<string, List<Subscription>> _subscriptions = new();

// 新
private readonly Dictionary<Type, List<Subscription>> _subscriptions = new();
```

**EventBus API 变化：**

```csharp
// 旧
public void On<T>(string eventName, Action<T> handler, int priority = (int)EventPriority.Normal)
public void Once<T>(string eventName, Action<T> handler, int priority = (int)EventPriority.Normal)
public void Off<T>(string eventName, Action<T> handler)
public void Emit<T>(string eventName, T data)

// 新
public void On<T>(Action<T> handler, int priority = (int)EventPriority.Normal) where T : struct
public void Once<T>(Action<T> handler, int priority = (int)EventPriority.Normal) where T : struct
public void Off<T>(Action<T> handler) where T : struct
public void Emit<T>(in T data) where T : struct
```

只改签名，不改行为。重入保护、延迟移除、异常隔离、priority 排序全部保留。

**调用代码变化：**

```csharp
// 旧
entity.Events.Emit(GameEventType.Unit.Damaged, new GameEventType.Unit.DamagedEventData(victim, amount));
entity.Events.On<GameEventType.Unit.DamagedEventData>(GameEventType.Unit.Damaged, OnDamaged);

// 新
entity.Events.Emit(new GameEventType.Unit.Damaged(victim, amount));
entity.Events.On<GameEventType.Unit.Damaged>(OnDamaged);
```

**EventContext 不受影响：**

```csharp
// 旧
public const string AbilityCastRequested = "ability:cast_requested";
public readonly record struct AbilityCastRequestedEventData(AbilityCastContext Context, EventContext Ctx);

// 新
public readonly record struct AbilityCastRequested(AbilityCastContext Context, EventContext Ctx);

// 调用
var ctx = new EventContext();
entity.Events.Emit(new AbilityCastRequested(castContext, ctx));
// 订阅者照常 ctx.SetResult(...) / ctx.SetFailed(...) / ctx.StopPropagation()
// 发布者照常 ctx.GetResult<T>()
```

**日志/调试：**

```csharp
// 旧
Log.Info($"Event emitted: {eventName}");
// 输出：Event emitted: global:wave_started

// 新
Log.Info($"Event emitted: {typeof(T).Name}");
// 输出：Event emitted: WaveStarted
```

`typeof(T).Name` 足够定位事件。如果后续需要域前缀，可按需加 attribute，但当前不急。

### 7.2 不改的部分

| 保留项 | 说明 |
| --- | --- |
| `EventContext` | 不改、不收缩，上下文和结果承载完整保留 |
| `Once` | 保留，签名从 `Once<T>(string, Action<T>)` 变为 `Once<T>(Action<T>)` |
| `EventPriority` | 保留 |
| `GlobalEventBus` | 保留，便捷方法签名同步调整 |
| `Data/EventType` 目录 | 保留，同类事件放同一个文件 |
| `GameEventType` partial class 树 | 保留命名空间组织，只删 `const string` + 改 payload 名 |
| 重入保护 / 延迟移除 / 异常隔离 | 完全保留 |

### 7.3 与 AI-first 框架的对比

AI-first 框架的做法：

- 事件定义：每个事件一个 `readonly record struct` 文件，放在 owner Capability 的 `Events/` 子目录。
- 调用：`Publish(new Activated(context))` / `Subscribe<Activated>(handler)`。
- 作用域：marker interface（`IEntityEvent` / `IGlobalEvent` / `IBroadcastEvent`）决定路由。
- 退订：`IDisposable` token。
- 无 `EventContext`，上下文直接放 payload。

本方案与 AI-first 的相同点：

- 删掉 `const string`，payload 做主键。
- 调用签名简化为 `Emit(data)` / `On<T>(handler)`。

本方案与 AI-first 的不同点：

- 保留 `EventContext`（AI-first 把上下文放 payload，我们保留独立的 context 对象）。
- 保留 `Once` / `EventPriority`（AI-first 没有）。
- 保留 `GameEventType` partial class 树和 `Data/EventType` 目录（AI-first 放 owner 目录）。
- 不引入 marker interface scope 约束（AI-first 用 `IEntityEvent` / `IGlobalEvent`）。
- 不引入 `IDisposable` 退订 token（保留 `Off` 方式，可后续按需加）。
- 不引入 `EntityEventBus` / `WorldEventBus` 拆分（保留旧 `EventBus` + `GlobalEventBus`）。

本方案只取 AI-first 的核心改进（删 `const string`），不引入它的额外复杂度。

## 8. 当前结论：删 const string，payload 做主键

回退到旧 ECS 基线后，Event 系统的唯一优化：**删掉 `const string` 事件名，用 `readonly record struct` payload 作为事件标识。**

- `EventBus.On/Off/Once/Emit` 签名去掉 `string eventName` 参数，内部改 `Dictionary<Type, ...>`。
- `EventContext` 不改、不收缩。
- `EventPriority` 保留。
- `GlobalEventBus` 保留，便捷方法签名同步调整。
- `Data/EventType` 目录保留，同类事件放同一个文件。
- `GameEventType` partial class 树保留命名空间组织，只删 `const string` + `XxxEventData` 改名 `Xxx`。
- 重入保护 / 延迟移除 / 异常隔离完全保留。

这是 AI-first 框架的核心改进，但只取这一项，不引入它的额外复杂度（marker interface scope、EntityEventBus/WorldEventBus 拆分、Observation、一事件一文件）。

## 9. 后续 SDD 建议

建议后续 SDD 名称：**Event 调用方式类型安全优化**

任务边界：

1. 保留旧 `EventBus/EventContext/Once/Priority/GlobalEventBus` 行为。
2. `EventBus` 内部 `Dictionary<string, ...>` 改为 `Dictionary<Type, ...>`。
3. `On/Off/Once/Emit` 签名去掉 `string eventName` 参数。
4. `Data/EventType` 中删掉所有 `const string`，`XxxEventData` 改名 `Xxx`。
5. 所有调用点迁移。
6. `GlobalEventBus` 便捷方法签名同步调整。
7. 不引入 marker interface scope、EntityEventBus/WorldEventBus 拆分、Observation。
8. 不把同类事件拆成一事件一文件。
9. 不迁移到 DataOS。

## 10. 明确不做

| 项目 | 原因 |
| --- | --- |
| 改 `EventContext` 结构 | 用户明确要求不改、不收缩，且它承载事件执行结果 |
| 删除 `Once` | `Once` 是有价值的基础能力 |
| 删除 `EventPriority` | 当前不是主要矛盾 |
| 当前执行 Event 调用方式优化 | 已确定方向：删 const string，payload 做主键 |
| 把事件定义移到 DataOS | DataOS 是数据资产/Schema 边界，不适合 runtime event contract |
| 一事件一文件 | 当前更适合同一类事件集中在一个文件 |
| 照搬当前 SlimeAI typed Event 系统 | 当前实现文件多、重复多，不符合回退后优化目标 |
