# 现状证据与 AI-first 裁决

> 更新：2026-06-01
> 状态：current design note

## 1. 问题重新定义

Timer 当前不是“不能用”。更准确的问题是：

```text
TimerManager 已统一大部分 gameplay 计时
  + GameTimer 对象池和链式 API 可用
  + useUnscaledTime / SimulationState 暂停语义已有
  + 但 owner、purpose、取消责任、tag 类型、observation 和文档一致性不足
  = 人能靠经验使用，AI 难以判断生命周期是否正确
```

给人用，当前 `Delay/Loop/Repeat/Countdown` 足够直观。给 AI 用，真正问题是 AI 很难回答：

- 这个 timer 属于哪个实体、组件或系统。
- 这个 timer 是 cooldown、DoT、spawn wave、AI wait 还是 visual lifetime。
- owner 销毁时是否必须取消。
- `CancelByTag("Buff")` 会不会误杀其它 owner 的 timer。
- 暂停来自手动暂停还是项目 `SimulationState.Suspended`。
- 当前活跃 timer 是否泄漏。

## 2. 当前代码证据

当前 Timer 入口集中在：

- `Src/ECS/Tools/Timer/TimerManager.cs`
- `Src/ECS/Tools/Timer/GameTimer.cs`
- `DocsAI/ECS/Tools/Timer/Concept.md`
- `DocsAI/ECS/Tools/Timer/Usage.md`

`TimerManager` 当前提供：

- `Delay(float duration, bool useUnscaledTime = false)`
- `Loop(float interval, bool useUnscaledTime = false)`
- `Repeat(float interval, int count, bool immediate = false, bool useUnscaledTime = false)`
- `Countdown(float duration, float interval, bool immediate = false, bool useUnscaledTime = false)`
- `Cancel(string id)`
- `CancelByTag(string tag)`
- `SetAllTimerPaused(bool paused)`
- `SetAllTimerPausedByTag(string tag, bool paused)`
- `GetActiveTimerCount()`
- `GetStats()`
- `GetSystemRuntimeInfo()`

`GameTimer` 当前提供：

- `Id`
- `Duration/Elapsed/Remaining/Progress`
- `IsLoop/RepeatCount/TotalDuration/TotalElapsed`
- `UseUnscaledTime`
- `IsPaused`，由 `_manualPaused || SystemPaused` 合成
- `IsDone/IsCancelled`
- `Tag`
- `OnComplete/OnLoop/OnRepeat/OnCountdown/OnUpdate`
- `WithTag/Immediate/Pause/Resume/Cancel/Complete`

当前实现已经有几个重要优点：

- 集中驱动，避免 gameplay 到处 `new Timer()`。
- 使用对象池，减少高频分配。
- 支持 scaled/unscaled 时间。
- 项目 `SimulationState.Suspended` 会自动暂停 scaled timer。
- 支持链式 API 和基本统计。

## 3. 调用点证据

Timer 被多个系统广泛使用：

| 调用点 | 用途 | 当前风险 |
| --- | --- | --- |
| `CooldownComponent` | 技能冷却 | owner 是组件字段，但 purpose/tag 只是 `"AbilityCooldown"`。 |
| `ChargeComponent` | 充能恢复 | loop timer 需在组件注销时取消。 |
| `TriggerComponent` | 周期触发 | 周期用途和取消责任应显式。 |
| `AttackComponent` | 前摇、后摇、验证 loop | 一个组件多个 timer，purpose 不清时 AI 难排查。 |
| `LifecycleComponent` | 最大寿命、复活、死亡 linger | 与 Entity 生命周期强相关，owner 销毁必须取消。 |
| `ContactDamageComponent` | 每目标接触伤害间隔 | 多目标 timer 字典，tag 不足以描述 target owner。 |
| `SpawnSystem` | 波次和轮询 | 系统停止时需要清理 wave/check timer。 |
| `RecoverySystem` | 周期恢复 | 系统生命周期 timer。 |
| `DamageTool.ScheduleDoT` | DoT 倒计时 | 返回 `GameTimer?`，调用者必须知道取消责任。 |
| `WaitIdleAction/DecoratorNode` | AI wait/cooldown | 行为树节点状态与 timer 生命周期耦合。 |

这些调用点说明 `TimerManager` 已是关键基础设施。随意替换会扩大影响面。

## 4. 文档漂移证据

`DocsAI/ECS/Tools/Timer/Concept.md` 当前示例包含：

```csharp
TimerManager.Create(2.0f)
    .OnComplete(() => DoSomething())
    .WithTag("cooldown")
    .WithTimeScale(TimeScale.Game);
```

以及：

```csharp
TimerManager.PauseByTag("cooldown");
TimerManager.ResumeByTag("cooldown");
TimerManager.CancelByTag("cooldown");
```

但源码当前真实 API 是：

- `TimerManager.Instance.Delay/Loop/Repeat/Countdown`
- `SetAllTimerPausedByTag`
- `useUnscaledTime`
- 没有 `Create`
- 没有 `WithTimeScale`
- 没有 `PauseByTag/ResumeByTag`

这类漂移对 AI-first 是高风险：AI 会照文档写出不存在的 API。

## 5. 外部资料校准

外部资料只用于校准边界，不用于复制 API。

| 来源 | 对本设计的约束 |
| --- | --- |
| Godot Timer / SceneTreeTimer | Godot 已有节点和 one-shot timer；但它们分散在场景树和调用点，不提供 SlimeAI 的 owner/purpose/observation 统一面。 |
| Godot `ignore_time_scale` | 引擎已有忽略 time scale 的概念，支持 SlimeAI 保留 `useUnscaledTime` 语义。 |
| Bevy Timer | Bevy 的 timer 是显式状态对象，需要 tick 和 mode；这支持 SlimeAI 把 timer 状态可观察化，但不要求复制 Bevy ECS schedule。 |
| Unity Time / Coroutine | Unity 常用 `deltaTime/timeScale/coroutine`；这证明 scaled/unscaled 和 owner 生命周期需要明确，否则 coroutine/timer 泄漏难查。 |
| IFramework TimerModule 本地报告 | IFramework 使用状态机式 timer entity；本地报告认为 SlimeAI `_Process(delta)` 驱动更适合 Godot，但状态管理思想可参考。 |

## 6. AI-first 风险

### 6.1 owner 不显式

当前 owner 只存在于调用方字段名，例如 `_phaseTimer`、`_lifeTimer`、`_chargeTimer`。TimerManager 自身不知道 owner，因此无法按 entity/component/system 观测或批量取消。

### 6.2 tag 太弱

`Tag` 是自由字符串。它适合临时批量操作，但不适合作为长期 contract。`"Buff"`、`"AbilityCooldown"` 这种标签无法区分具体 owner、ability、target 或创建位置。

### 6.3 回调闭包风险

timer 回调常捕获 `_entity`、`_data`、局部 `timer` 或目标节点。如果 owner 销毁但 timer 未取消，AI 只能靠人工阅读判断是否安全。

### 6.4 池化对象身份风险

`GameTimer` 是池化对象。业务如果长期保存并在回收后误用，可能读到被重置或复用后的状态。当前缺少 handle generation 防误用。

### 6.5 Observation 不足

`GetSystemRuntimeInfo()` 只输出活跃数、对象池容量和 unscaled delta。AI 排查 timer 泄漏时还需要 owner、purpose、duration、remaining、paused reason、created stack 或 source。

## 7. AI-first 裁决

### 7.1 TimerManager 继续保留

统一计时入口是正确方向。当前 gameplay 已广泛依赖 TimerManager；重写或回退到 Godot Timer 会破坏框架一致性。

### 7.2 Timer 需要完善

完善重点不是新增更多工厂方法，而是补足：

- owner。
- purpose。
- typed tag。
- handle 语义。
- cancel reason。
- observation。
- 文档一致性。

### 7.3 不把 Timer 做成业务系统

Cooldown、Charge、DoT、Lifecycle、SpawnWave 的业务规则仍归各自 owner。Timer 只负责时间流逝和回调调度。

### 7.4 文档纠偏是第一优先级

Timer 文档当前会误导 AI 调用不存在 API。代码优化前必须先把 DocsAI Timer 文档改成真实 API。

## 8. 结论

推荐策略：

1. 立即修正 DocsAI Timer 文档漂移。
2. 建立 Timer 调用点清单和 owner/purpose 表。
3. 后续新增 `TimerPurpose` / `TimerOwner` / `TimerHandle` 的最小设计。
4. 为 TimerManager observation 补结构化字段。
5. 逐步迁移高风险调用点，不一次性替换所有 timer API。
