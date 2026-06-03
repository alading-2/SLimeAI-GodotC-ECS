# Timer 工具设计包

> 更新：2026-06-02
> 状态：current architecture decision
> 入口：`README.md`
> 裁决：Timer 要做到通用框架级质量，不能停留在当前 `Node._Process + ObjectPool.ForEachActive()` 的便利实现。最终方案是 **纯 C# TimerScheduler 核心 + TimerManager 兼容 facade/driver + 主线程派发 + handle/owner/purpose/observation 完整契约**。

## 0. 本设计包回答什么

这份设计包一次性回答 Timer 的关键架构问题：

- 当前每帧更新所有 timer 性能会不会有问题。
- Godot `Timer` / `SceneTreeTimer` 是否应该作为框架 gameplay Timer 默认实现。
- `.NET System.Threading.Timer` / `System.Timers.Timer` / `PeriodicTimer` 是否更适合作为底层 Timer。
- 计时器是否应该多线程。
- 成熟引擎和框架的 Timer 是否也是统一管理。
- SlimeAI 要做到通用框架级质量，Timer 应该改到什么最终形态。

结论先写清楚：

```text
保留 TimerManager 作为统一入口和 Godot 生命周期适配层；
重做内部 Timer 调度核心为纯 C# TimerScheduler；
默认 gameplay 回调只在主线程执行；
第一实现采用 min-heap by dueTime；
大规模压力出现后再引入 hierarchical timing wheel；
不使用 Godot Timer / SceneTreeTimer 作为 gameplay 默认；
不使用 .NET ThreadPool Timer 直接执行 gameplay 回调；
不把“每帧扫描所有 timer + 每帧 ToList 快照”作为最终架构。
```

## 1. 总裁决

采用 **AI-first Main-thread Timer Scheduler**：

```text
TimerManager
  -> 仍是外部统一 API、Godot Node 生命周期和 delta 输入 adapter

TimerScheduler
  -> 纯 C# 调度核心；不继承 Node；不调用 Godot API；可单测、可 benchmark

TimerHandle
  -> id + generation；调用方持有 handle，不长期依赖池化对象身份

TimerOwner / TimerPurpose
  -> 每个 gameplay timer 必须说明归属和用途

TimerClock
  -> Game/Scaled、Real/Unscaled、Fixed 明确分离

TimerObservation
  -> 可按 owner、purpose、clock、pause、dueTime、remaining 输出结构化状态

Main-thread Dispatch
  -> 到期回调进入主线程稳定派发点；不在 ThreadPool 直接改 Entity/Data/Event/Godot
```

旧 `GameTimer` 可以短期保留为兼容对象或 builder，但不能继续作为长期身份和调度核心。

## 2. 对用户问题的直接回答

### 2.1 当前性能会不会有问题

会有问题，而且问题不是“用了 Godot API 所以一定慢”这么简单。

当前真正的热路径问题是：

- `TimerManager._Process(delta)` 每帧调用 `ProcessTimers(delta)`。
- `ProcessTimers` 调 `_timerPool.ForEachActive(...)`。
- `ObjectPool.ForEachActive()` 内部调用 `GetActiveSnapshot()`。
- `GetActiveSnapshot()` 用 `_activeItems.ToList()`，这会每帧分配一个活跃 timer 快照列表。
- 所有 active timer 每帧都被扫描，即使它们距离到期还有很久。
- `Cancel/CancelByTag/SetAllTimerPausedByTag/ApplyProjectPauseState` 也依赖 active 全量遍历。

所以当前 Timer 与“零 GC”目标矛盾：Timer 对象本身池化了，但每帧遍历产生快照分配。

### 2.2 `_Process` 每帧驱动是不是错

不是。游戏 Timer 必须跟随游戏主循环、暂停、timeScale、fixed/update schedule。成熟框架普遍有统一的 tick / schedule / timer manager。

真正不够好的是：

```text
每帧入口存在：合理
每帧扫描所有 timer：只适合小规模简单实现
每帧分配 ToList 快照：不应出现在框架热路径
Timer 状态依附 Godot Node 管理：不利于纯逻辑测试和跨宿主复用
```

因此最终设计不是取消帧驱动，而是让帧驱动只喂 delta 给纯 C# 调度器，由调度器只处理 due timer 和显式要求 per-frame update 的 timer。

### 2.3 用纯 C# 会不会更好

会。Timer 核心应该是纯 C#，原因是：

- 可单元测试，不需要 Godot SceneTree。
- 可 benchmark，不依赖场景运行。
- 可复用于 Godot、headless server、测试 runner。
- 数据结构可控，可以使用 min-heap / timing wheel / handle generation / owner index。
- 不把 Timer 的正确性绑到 Godot signal、Node、SceneTree 生命周期上。

但纯 C# 不等于用 `.NET Timer` 直接跑 gameplay 回调。SlimeAI 要的是纯 C# **调度数据结构**，不是 ThreadPool callback。

### 2.4 要不要用 C# / .NET 底层 Timer

不要把 `.NET Timer` 作为 gameplay Timer 默认实现。

`.NET System.Threading.Timer`、`System.Timers.Timer`、`PeriodicTimer` 解决的是后台定时唤醒、服务心跳、异步等待循环等问题；它们不解决游戏框架最关键的问题：

- 回调默认不在 Godot 主线程。
- 不知道 SlimeAI `SimulationState.Suspended`。
- 不知道 scaled/unscaled/fixed 游戏时间。
- 不知道 Entity/Component/System owner。
- 不知道 RuntimeCommandBuffer phase。
- 不知道 Data/Event/Godot API 的主线程约束。
- 周期任务可能与上一轮执行重叠或跨帧乱序。

`.NET Timer` 可作为工具层或后台 IO 的唤醒源，但到期后必须投递回主线程队列，不能直接执行 gameplay callback。

### 2.5 要不要多线程

gameplay Timer 默认不要多线程。

多线程 Timer 的直觉来自“底层线程睡眠更省 CPU”。但游戏逻辑的瓶颈和正确性不在这里。成熟游戏框架通常要求 gameplay 状态在可控主线程或明确 schedule 中更新，否则需要大量锁、同步上下文、主线程回投和生命周期校验。

SlimeAI 当前 Entity/Data/Event/Godot 调用都按主线程模型设计。让 Timer 回调在线程池直接执行，会把简单计时问题变成数据竞争、顺序不确定、Godot API 非线程安全和调试困难的问题。

可采纳的多线程边界：

- 后台 IO、联网、工具、编辑器服务可以使用后台 timer。
- 后台 timer 到期后只投递 `RuntimeCommand` / main-thread queue。
- gameplay callback 仍在主线程 schedule phase 执行。

## 3. 成熟框架对照结论

| 来源 | 证据摘要 | SlimeAI 采纳 |
| --- | --- | --- |
| Godot `SceneTreeTimer` | `SceneTree::process_timers` 在 scene tree 中遍历 timer list，按 delta/ignore_time_scale 扣时间；`create_timer` 创建 one-shot 并 push 到统一 list。 | 证明引擎也是统一处理 timer；但 Godot one-shot 缺 owner/purpose/handle observation，不作为 gameplay 默认。 |
| Godot thread-safe docs | 活动 SceneTree 交互不是线程安全，跨线程要 defer 到主线程。 | Timer 回调必须主线程派发，不能从 ThreadPool 直接改 Godot/Entity。 |
| Bevy | `Timer::tick(delta)` 必须显式调用；`Time<Real>`、`Time<Virtual>`、`Time<Fixed>` 分离。 | 采纳显式 clock 和 tick 语义；不复制 Bevy ECS schedule。 |
| Unreal Gameplay Timers | `GetWorldTimerManager().SetTimer(...)` 返回 `FTimerHandle`，通过 world timer manager 统一 set/clear/query。 | 采纳 unified manager + handle + elapsed/remaining query。 |
| Unity UIElements Scheduler | 调度项挂在 scheduler，由 panel update 调用 `UpdateScheduledEvents`，不是任意线程直接改 UI。 | 采纳主循环调度和 owner attachment 思想。 |
| .NET Timers | `System.Threading.Timer` 适合 timed background service；文档明确存在不等待上一次执行完成等限制。 | 拒绝作为 gameplay 默认；可用于后台服务唤醒。 |
| ET Framework | 明确指出每个 timer 一个线程/Sleep 会频繁线程切换、效率低；游戏逻辑通常设计单线程 timer。 | 强采纳：异步不是多线程，Timer 回调应回到主逻辑线程。 |
| IFramework | `TimerModule : UpdateModule`，`OnUpdate` 计算 delta 并更新 timer entity。 | 证明成熟工具框架也有统一 update timer module。 |
| QFramework ActionKit | `DelayAction.OnExecute(float dt)` 累加 dt，完成后回调，并使用对象池。 | 采纳对象池和显式执行；但 SlimeAI 要补 owner/handle/observation。 |
| Netty HashedWheelTimer | time wheel 用于大量 scheduled timeout/cancel，适合 IO timeout 场景。 | Timing wheel 作为后续高规模优化，不作为第一实现复杂化入口。 |

## 4. 文件结构

| File | Role | 说明 |
| --- | --- | --- |
| `README.md` | design-index | 本文件。给出总裁决、边界、成熟框架结论和完成定义。 |
| `01-现状证据与AI-first裁决.md` | research-decision | 当前代码热路径、性能问题、外部框架证据、Evidence/Inference/Adopt/Reject。 |
| `02-目标架构与优化路线.md` | architecture-roadmap | 纯 C# `TimerScheduler`、min-heap、handle、owner/purpose、clock、threading、debug diagnostics、observation 的目标设计。 |
| `03-调用点迁移与验证计划.md` | migration-test-plan | 调用点迁移、兼容策略、benchmark、Timer 压力场景、grep gate、构建和 Godot 场景验证。 |

## 5. 目标边界

| 模块 | 目标职责 | 禁止职责 |
| --- | --- | --- |
| `TimerManager` | 对外 API、Godot 生命周期、delta 输入、兼容 facade、系统注册 | 不承载调度数据结构核心；不直接遍历对象池快照作为最终方案。 |
| `TimerScheduler` | 纯 C# 创建、取消、暂停、dueTime 调度、主线程派发、索引和 observation | 不调用 Godot API；不直接写业务状态。 |
| `TimerHandle` | 稳定身份、generation 校验、取消/暂停/查询入口 | 不暴露池化对象引用作为长期身份。 |
| `TimerOwner` | Entity/Component/System/Ability/Feature/Tool 归属 | 不替代 Entity lifecycle，只提供取消和观测索引。 |
| `TimerPurpose` | Cooldown、DoT、SpawnWave、AIWait 等用途 | 不让自由字符串 tag 承担唯一语义。 |
| `TimerClock` | Game/Scaled、Real/Unscaled、Fixed clock | 不混用 Godot `Engine.TimeScale`、project pause 和真实时间语义。 |
| `TimerObservation` | 结构化 dump、统计、泄漏线索、source | 不参与业务决策，不在 release 热路径收集重 source。 |
| `TimerDiagnostics` | debug snapshot、summary、dump、JSON artifact、压力场景指标 | 不在每帧自动打印；不把人工日志当作唯一验证证据。 |

## 6. 完成定义

Timer 优化完成不是“还能延迟回调”，而是同时满足：

- Timer core 为纯 C#，可以脱离 Godot 单测和 benchmark。
- `TimerManager` 仍是统一入口，但只做 facade/driver，不是调度核心。
- gameplay 默认不用 Godot `Timer` / `SceneTreeTimer`。
- gameplay 默认不用 `.NET ThreadPool Timer` 直接执行回调。
- 到期回调在主线程稳定 phase 派发。
- 调度结构不再每帧扫描所有 timer，也不再每帧分配 active snapshot。
- 支持 `TimerHandle(id, generation)` 防池化 stale 引用。
- 支持 `TimerOwner`、`TimerPurpose`、`TimerClock`、cancel reason。
- 支持按 owner/purpose 批量取消和 observation。
- 支持 `TimerManager.PrintTimerSummary()`、`PrintTimerDump(filter)`、`ExportTimerDiagnosticsJson(path)` 等 debug 入口，且 debug 输出只在显式调用时分配。
- `OnUpdate` 这类每帧进度回调被显式标记为 per-frame timer，不拖累普通 delay/loop。
- 新增 `TimerStressValidation` 场景或等价 headless 验证场景，能生成 PASS artifact，覆盖大量 timer、取消风暴、暂停、Real/Game clock 和 owner/purpose 泄漏检查。
- DocsAI Timer 文档与源码 API 一致，不再出现不存在的 `Create/WithTimeScale/PauseByTag`。
- 构建、Timer 单测、性能 benchmark、Timer stress scene、BrotatoLike 主场景 smoke 通过。

## 7. 阅读顺序

1. 先读 `01-现状证据与AI-first裁决.md`，确认为什么当前便利实现不能作为最终架构。
2. 再读 `02-目标架构与优化路线.md`，确认纯 C# scheduler、heap、handle、owner 和线程边界。
3. 最后读 `03-调用点迁移与验证计划.md`，确认怎么一次性改到完整形态并验证。
