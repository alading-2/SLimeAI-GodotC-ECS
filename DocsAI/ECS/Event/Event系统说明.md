# Event 系统说明

> 状态：当前实现说明；旧字符串事件名已迁移为 payload 类型主键。
> 范围：`Src/ECS/Base/Event/`、`Data/EventType/`、`Entity.Events`、`GlobalEventBus.Global`。
> 设计事实源：`../../../SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/1.Event系统优化/`。
> 更新：2026-05-31

## 1. 一句话定位

Event 是框架内的轻量通信协议：用 `readonly record struct` payload 类型本身作为事件标识，通过 `EventBus.On<T>` / `Emit<T>` 连接发布者和订阅者。

它不是状态存储，也不是系统调度器。Entity 自身状态放 `Data`，跨系统流程优先走明确的 System API 或 Capability 入口；Event 只负责“某事发生了”或“某个局部流程请求协作者参与”。

## 2. 当前架构

```text
Data/EventType/
  定义事件 payload：GameEventType.Unit.Damaged、GameEventType.Ability.CheckCanUse ...

Src/ECS/Base/Event/EventBus.cs
  泛型事件总线：On / Once / Off / Emit / ClearEvent / Clear

Entity.Events
  Entity 实例级事件总线：组件之间围绕同一个实体通信

GlobalEventBus.Global
  全局事件总线：跨实体、跨系统的低频广播

EventContext
  请求-响应式事件的可变上下文：Success / FailReason / Result / Handled
```

### 2.1 payload 类型就是事件名

当前实现不再维护 `const string EventName`。事件定义直接写成 payload 类型：

```csharp
public static partial class GameEventType
{
    public static partial class Unit
    {
        /// <summary>单位受到伤害。</summary>
        public readonly record struct Damaged(
            IEntity Victim,
            float Amount,
            IEntity? Attacker = null,
            DamageType Type = DamageType.True,
            bool IsCritical = false);
    }
}
```

`typeof(GameEventType.Unit.Damaged)` 就是 EventBus 的字典 key。这样避免了旧模式里的双写：

```text
旧：const string Damaged + DamagedEventData
新：GameEventType.Unit.Damaged
```

### 2.2 EventBus 的能力边界

`EventBus` 保留的是最小但足够的运行时能力：

| 能力 | 当前规则 |
| ---- | ---- |
| 订阅 | `On<T>(Action<T> handler, int priority = Normal)` |
| 一次性订阅 | `Once<T>(...)`，首次触发后延迟移除 |
| 取消订阅 | `Off<T>(Action<T> handler)` |
| 发布 | `Emit<T>(in T data)` |
| 优先级 | 数值越大越先执行，默认 `EventPriority.Normal` |
| 派发中移除 | 不直接改列表，先标记，派发结束后统一清理 |
| 同类型重入 | 同一个 payload 类型正在派发时再次 `Emit` 会被阻止并记录 warning |
| 异常隔离 | 单个 handler 异常会记录 error，不中断后续派发 |
| 动态事件 | `EmitDynamic` / `OnDynamic` 仅给 Feature 等低频数据驱动场景使用 |

常规泛型路径不使用 `DynamicInvoke`，handler 类型匹配时直接调用 `Action<T>`。动态路径会有反射和装箱成本，不应放在热路径。

## 3. 怎么用

### 3.1 Entity 局部事件

同一个 Entity 上的 Component 通信优先使用 `entity.Events`：

```csharp
_entity.Events.On<GameEventType.Unit.HealRequest>(ApplyHeal);

_entity.Events.Emit(
    new GameEventType.Unit.HealRequest(20f, HealSource.Regen));
```

适合：

- `HealthComponent` 接收 `HealRequest`。
- `AttackComponent` 接收 `Attack.Requested`。
- `MovementComponent` 接收 `MovementStarted` / `MovementStopRequested`。
- Entity 内部 Component 之间不直接互相调用。

### 3.2 全局低频广播

跨实体、跨系统的结果事件使用 `GlobalEventBus.Global`：

```csharp
GlobalEventBus.Global.On<GameEventType.Unit.Damaged>(OnDamaged);

var data = new GameEventType.Unit.Damaged(victim, amount, attacker, type, isCritical);
victim.Events.Emit(data);
GlobalEventBus.Global.Emit(data);
```

适合：

- UI 监听伤害、治疗、闪避结果。
- 统计系统监听击杀或波次事件。
- EntityManager 广播 `EntitySpawned` / `EntityDestroyed`。

不要把所有事件都上提到全局。全局事件生命周期长，订阅方必须能清理；如果事件只服务单个实体内部协作，留在 `Entity.Events`。

### 3.3 一次性订阅和优先级

```csharp
entity.Events.Once<GameEventType.Unit.Killed>(OnFirstKilled);

entity.Events.On<GameEventType.Ability.CheckCanUse>(
    CheckCooldown,
    priority: (int)EventPriority.High);
```

优先级适合“同一个检查流程内有先后顺序”的场景。不要用优先级隐藏复杂业务编排；流程复杂时应上提到 System 或 Service。

### 3.4 取消订阅和清理

```csharp
entity.Events.Off<GameEventType.Unit.Killed>(OnKilled);
entity.Events.ClearEvent<GameEventType.Unit.Killed>();
entity.Events.Clear();
```

Entity 生命周期结束时由 EntityManager 统一 `Events.Clear()`。长期存在的全局监听者仍应在退出、卸载或解绑时显式 `Off<T>`。

## 4. EventContext

纯通知事件只需要不可变 payload。需要订阅者参与判断、写失败原因或返回结果时，把 `EventContext` 放进 payload：

```csharp
var context = new EventContext();
ability.Events.Emit(new GameEventType.Ability.CheckCanUse(context));

if (!context.Success)
{
    GD.Print($"技能不可用: {context.FailReason}");
}
```

`EventContext` 的语义：

| API | 用途 |
| ---- | ---- |
| `SetFailed(reason)` | 标记流程失败，并记录第一个失败原因 |
| `SetResult<T>(result)` | 写入强类型结果，并标记 `IsHandled` |
| `GetResult<T>()` | 发布者读取订阅者写入的结果 |
| `StopPropagation()` | 设置停止传播标志 |
| `IsHandled` | 是否已有订阅者处理过 |

注意：当前 `EventBus` 不会自动解析 payload 内的 `EventContext`，因此 `StopPropagation()` 只是上下文标志，不会由总线强制跳过后续 handler。若流程需要硬停止，应在该事件的 handler 约定里检查 `context.IsPropagationStopped`，或后续明确改造 `EventBus` 的上下文识别规则。

## 5. 事件定义规则

事件定义放在 `Data/EventType/`，按领域合并到同类文件里，例如：

```text
Data/EventType/Ability/GameEventType_Ability_System.cs
Data/EventType/Unit/GameEventType_Unit_Health.cs
Data/EventType/Global/GameEventType_Global_GameState.cs
```

这批文件不是 DataOS descriptor。它们是 C# runtime contract，只是历史上位于 `Data/EventType/`。

新增事件时遵守：

- 用 `public readonly record struct`，不要新增字符串事件名。
- 类型名就是事件名，不再使用 `XxxEventData` 后缀。
- 空事件写成空 record struct，例如 `public readonly record struct GameStart();`。
- 同一领域事件放同一文件，避免简单事件一事件一文件。
- payload 只携带协议需要的数据，不把可变业务状态藏进事件。
- 框架事件不依赖具体游戏命名空间、具体 UI、输入动作、资产路径或 Godot Node。
- payload 需要 Godot `Node` / `Vector2` / `Rect2` 等引擎类型时，默认先判断是否应放到游戏侧或 GodotBridge 协议。
- 请求型事件如果需要返回值，放 `EventContext` 或领域上下文对象；不要让订阅者反向调用发布者。

## 6. 什么时候不用 Event

| 场景 | 更合适的入口 |
| ---- | ---- |
| 持久状态、属性、运行时数值 | `Entity.Data` + generated `DataKey<T>` |
| 伤害、治疗、暴击、闪避管线 | `DamageService` / `HealService` |
| 目标查询、范围过滤 | `TargetSelector` / TargetingSystem |
| 资源加载 | `ResourceManagement.Load` |
| 高频对象创建回收 | 对象池 / EntityManager |
| 明确命令式流程 | 对应 System / Service 的显式 API |

Event 的强项是解耦通知和局部协作，不是替代所有函数调用。

## 7. 排查入口

- 总线实现：`Src/ECS/Base/Event/EventBus.cs`
- 上下文对象：`Src/ECS/Base/Event/EventContext.cs`
- 全局入口：`Src/ECS/Base/Event/GlobalEventBus.cs`
- 事件协议：`Data/EventType/`
- Entity 清理：`Src/ECS/Base/Entity/Core/Manager/EntityManager.cs`

常用搜索：

```bash
rg "Events\\.(On|Once|Off|Emit)" Src Data/EventType
rg "GlobalEventBus\\.Global\\.(On|Off|Emit)" Src
rg "EventContext|SetFailed|SetResult|GetResult" Src Data/EventType
rg "EmitDynamic|OnDynamic|OffDynamic" Src
```

验证入口：

```bash
Tools/run-build.sh
Tools/run-tests.sh
```

## 8. 历史判断

本轮 Event 优化的关键判断是“删掉字符串事件名，而不是重做一个更大的事件系统”。

保留 `On/Off/Once/Emit`、优先级、一次性订阅、派发中延迟移除、同类型重入保护、handler 异常隔离和 `EventContext`，因为这些能力已经覆盖当前框架的真实使用场景。删除的是旧模式里最容易漂移的部分：`const string` 与 payload 双写。

因此后续维护重点不是扩展 EventBus 能力，而是守住协议边界：事件要少、payload 要清楚、作用域要明确、订阅方要能清理。
