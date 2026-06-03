# 现状证据与 AI-first 裁决

> 更新：2026-06-02
> 状态：current research decision
> 裁决：当前 Timer 的问题不是“不能用”，而是达不到通用框架级底层工具质量。必须从 `Node._Process + active snapshot scan` 升级为纯 C# `TimerScheduler`，并保留主线程一致性。

## 1. Goal

本设计要解决：

- Timer 作为 SlimeAI 通用框架工具的最终方向。
- 当前每帧更新所有 timer 的真实性能风险。
- Godot API Timer、纯 C# scheduler、.NET Timer、多线程 Timer 的取舍。
- 成熟引擎和框架的证据对照。
- 后续实现必须一次性补齐的架构契约。

非目标：

- 不在本文直接写实现代码。
- 不把 Cooldown、DoT、SpawnWave、AIWait 等业务规则塞进 Timer。
- 不把 Timer 重构成某个游戏专属玩法工具。
- 不复制外部框架源码或 API。

## 2. Context Read

### 2.1 SlimeAI 当前事实源

- `Src/ECS/Tools/Timer/TimerManager.cs`
- `Src/ECS/Tools/Timer/GameTimer.cs`
- `Src/ECS/Tools/ObjectPool/ObjectPool.cs`
- `DocsAI/ECS/Tools/Timer/Concept.md`
- `DocsAI/ECS/Tools/Timer/Usage.md`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Tool/Timer/*`

### 2.2 外部和本地 Resources

- Godot 4.6.2 source：`Resources/Engine/Engine/godot-4.6.2-stable/scene/main/scene_tree.cpp`
- Godot docs via Context7：`/godotengine/godot-docs`
- Bevy source：`Resources/Engine/Engine/bevy/crates/bevy_time/src/timer.rs`、`time.rs`
- Bevy docs via Context7：`/websites/rs_bevy`
- Unreal docs via Context7：`/websites/dev_epicgames_en-us_unreal-engine`
- Microsoft Learn via Context7：`/websites/learn_microsoft_en-us`
- UnityCsReference：`Resources/Engine/Engine/UnityCsReference/Modules/UIElements/Core/Scheduler.cs`
- Unity docs via Context7：`/websites/unity_en-us`
- ET Framework book：`Resources/Engine/Engine/ET-Framework/Book/2.3单线程异步.md`
- IFramework source：`Resources/Engine/Engine/IFramework/.../TimerModule.cs`
- QFramework source：`Resources/Engine/Engine/QFramework/.../DeprecateActionKit.cs`
- Netty docs via Context7：`/netty/netty`

### 2.3 Git boundary

当前仓库边界：`/home/slime/Code/SlimeAI/SlimeAI`。

本轮只应修改 Timer SDD 设计文档。工作区已有大量与 Timer 无关的未提交改动，不能回滚、覆盖或混入。

## 3. 当前实现证据

### 3.1 TimerManager 当前形态

当前 `TimerManager` 是 Godot `Node`：

```text
TimerManager : Node, ISystem
  _EnterTree()
    Instance = this
    _timerPool = ObjectPoolManager.GetPool<GameTimer>(TimerPool)
    GetTree().Connect(ProcessFrame, OnProcessFrame)

  _Process(double delta)
    ProcessTimers(delta)

  ProcessTimers(delta)
    _timerPool.ForEachActive(timer => timer.Update(dt))
```

当前工厂 API：

- `Delay(float duration, bool useUnscaledTime = false)`
- `Loop(float interval, bool useUnscaledTime = false)`
- `Repeat(float interval, int count, bool immediate = false, bool useUnscaledTime = false)`
- `Countdown(float duration, float interval, bool immediate = false, bool useUnscaledTime = false)`

当前批量 API：

- `Cancel(string id)`
- `CancelByTag(string tag)`
- `SetAllTimerPaused(bool paused)`
- `SetAllTimerPausedByTag(string tag, bool paused)`

当前 observation：

- `GetActiveTimerCount()`
- `GetStats()`
- `GetSystemRuntimeInfo()` 只输出活跃数、池容量、unscaled delta。

### 3.2 GameTimer 当前形态

`GameTimer` 是池化纯 C# 对象，包含：

- `Id` string。
- `Duration/Elapsed/Remaining/Progress`。
- `IsLoop/RepeatCount/TotalDuration/TotalElapsed`。
- `UseUnscaledTime`。
- 手动暂停 `_manualPaused` 与系统暂停 `SystemPaused`。
- `Tag` string。
- `OnComplete/OnLoop/OnRepeat/OnCountdown/OnUpdate` 回调。
- `Cancel/Complete/Update`。

优点：

- 单个 timer 状态基本清楚。
- 回调 API 对人方便。
- 对象池减少 timer 对象分配。
- scaled/unscaled 和 project suspended 语义已有雏形。

缺口：

- 没有 owner。
- 没有 purpose。
- 没有 handle generation。
- `Id` 是 string GUID，创建成本高，不适合高频底层身份。
- 池化对象本身被调用方长期持有，存在 stale 引用风险。
- `OnUpdate` 每帧执行，与普通 due timer 混在同一调度路径。

### 3.3 ObjectPool 热路径证据

`ObjectPool<T>` 当前维护 `_activeItems = new HashSet<T>()`。

关键问题：

```text
GetActiveSnapshot()
  -> return _activeItems.ToList()

ForEachActive(action)
  -> var snapshot = GetActiveSnapshot()
  -> foreach snapshot action(item)
```

这意味着 Timer 每帧调用 `ForEachActive` 时，都会分配一个 `List<GameTimer>` 快照。

所以当前“对象池集成、零 GC”只覆盖 `GameTimer` 对象本身，没有覆盖每帧 active 遍历。Timer 是典型热路径，这个分配不能保留为最终架构。

### 3.4 Debug 和压力验证缺口

当前 Timer 只有 `GetActiveTimerCount()`、`GetStats()`、`GetSystemRuntimeInfo()` 的轻量摘要，无法打印或导出完整资源数据：

- 无法按 owner/purpose/clock/state 过滤活跃 timer。
- 无法看到 heap/dispatch/per-frame update 的结构化指标。
- 无法看到 dueTime、remaining、pauseMask、cancelReason、source。
- 无法输出 JSON artifact 给 Godot scene runner 或 CI 判断。
- 无法自动识别无 owner、无 purpose、invalid owner、长生命周期 loop timer。

当前也没有 `Src/ECS/Tools/Timer/Tests/` 下的 Timer 专属压力验证场景。纯 C# scheduler 单测和 benchmark 必须有，但它们不能替代 Godot 场景，因为它们覆盖不了 `_Process` driver、project pause、Real/Game clock、主线程 callback 和 scene-gate artifact。

结论：debug diagnostics 和 Timer stress scene 不是附加功能，而是 Timer 重构完成定义的一部分。

## 4. 当前性能问题重新定义

### 4.1 不是 Godot API 本身慢

当前每帧用到的 Godot API 主要是：

- `_Process(double delta)` 作为帧入口。
- `Time.GetTicksMsec()` 计算 unscaled delta。
- `SceneTree.ProcessFrame` 信号辅助更新真实时间戳。

这些不是当前主要问题。真正的问题在 C# 层：

- 每帧 `ToList()` 分配。
- 每帧 O(N active timers) 扫描。
- 委托回调混在遍历中执行。
- 完成 timer 延迟回收依赖下一帧扫描。
- 批量操作按 tag 全量扫描。
- 无 owner/purpose 索引导致 observation 和 cleanup 只能扫全量。

### 4.2 每帧扫描所有 timer 的上限

线性扫描在小项目里可以接受，但通用框架不能以“小规模暂时够用”为最终设计。

问题规模示例：

| 场景 | 当前线性扫描影响 |
| --- | --- |
| 100 个 timer | 通常没问题，但每帧 `ToList()` 仍违背热路径零分配。 |
| 1000 个 timer | 每帧扫描和分配开始可见，尤其在移动端和 GC 敏感场景。 |
| 10000 个 timer | 即使每帧只到期很少，仍全量扫描；普通 delay 也持续占帧成本。 |
| 大量远期 timer | 最糟糕：几乎没有 timer 到期，但每帧仍扫全部。 |
| 大量 per-frame progress timer | 必须 O(M) 更新，但应显式分类，不拖累普通 timer。 |

结论：当前结构可以保留为旧实现证据，但不能作为“一次性做到最好”的目标。

### 4.3 正确优化目标

优化目标不是“把回调丢给底层线程”，而是：

```text
Schedule: O(log N) 插入到 dueTime heap
Tick: O(K log N) 处理本帧到期 K 个 timer
Cancel: O(1) 标记失效，heap pop 时 lazy skip
Owner cancel: O(M owner timers) 通过 owner index 定位
Observation: 通过 active index / owner index / purpose index 输出
Frame update: 只更新显式 RequiresFrameUpdate 的 timer
Allocation: 普通 tick 不分配
Dispatch: 到期回调在主线程稳定 phase 执行
```

## 5. 成熟框架 Evidence

### 5.1 Godot

**Evidence**

本地 Godot source `scene_tree.cpp`：

- `SceneTree::process_timers(double p_delta, bool p_physics_frame)` 遍历 `timers` list。
- 每个 `SceneTreeTimer` 根据 `ignore_time_scale` 选择 `unscaled_delta` 或 `p_delta`。
- 到期后 emit `timeout`，然后从 list erase。
- `SceneTree::create_timer(...)` 创建 `SceneTreeTimer`，设置 `process_always/process_in_physics/ignore_time_scale`，push 到 `timers`。

Context7 Godot docs：

- `SceneTree.create_timer` 返回 one-shot `SceneTreeTimer`。
- timeout 后自动释放。
- 可配置 `process_always`、`process_in_physics`、`ignore_time_scale`。
- Thread-safe APIs 文档说明 active scene tree 交互不是线程安全，跨线程应 defer 到主线程。

**Inference**

Godot 自己也不是为每个 timer 开线程，而是在 SceneTree 中统一处理。Godot 的 Timer 适合 scene-local one-shot 或简单等待，但它没有 SlimeAI 需要的 owner/purpose、批量 owner cancel、AI observation、RuntimeCommandBuffer phase 和跨宿主纯 C# 单测能力。

**Decision**

- Adopt Now：保留 `ignore_time_scale` 等价语义。
- Adopt Now：保留主线程 SceneTree 安全边界。
- Reject：把 Godot `Timer` / `SceneTreeTimer` 作为 SlimeAI gameplay 默认。

### 5.2 Bevy

**Evidence**

本地 Bevy `bevy_time/src/timer.rs`：

- `Timer` 文档写明必须调用 `Timer::tick` 才会推进。
- `TimerMode` 区分 once/repeating。
- paused timer 不增加 elapsed。
- repeating timer 可在单 tick 内完成多次，通过 `times_finished_this_tick` 查询。

本地 Bevy `bevy_time/src/time.rs`：

- `Time<Real>` 代表 wall-clock real time。
- `Time<Virtual>` 代表可暂停、可缩放的 game time。
- `Time<Fixed>` 代表 fixed timestep。

Context7 Bevy docs 同样说明 `Virtual` clock 可 pause/speed/max delta，`Real` 不受 pause/time scaling 影响。

**Inference**

成熟 ECS 框架会把 clock 语义显式化，不把“真实时间、游戏时间、固定步长”混成一个 bool。Timer 状态由 schedule/system 显式 tick，而不是后台线程任意触发业务。

**Decision**

- Adopt Now：`TimerClock.Game/Real/Fixed`。
- Adopt Now：显式 tick 和 pause 语义。
- Adopt Later：单 tick 多次补偿策略和 max catch-up 策略。
- Reject：复制 Bevy schedule 架构。

### 5.3 Unreal

**Evidence**

Context7 Unreal docs：

- Gameplay timer 通过 `GetWorldTimerManager().SetTimer(...)` 创建。
- 返回 `FTimerHandle`。
- 使用 `ClearTimer` 停止。
- 可 query elapsed / remaining。
- 也提供 next tick timer。

**Inference**

Unreal 的 gameplay Timer 是 world-level manager + handle 模型，不要求业务自己管理底层线程。Handle 是一等公民，适合取消、查询和生命周期管理。

**Decision**

- Adopt Now：`TimerHandle` 是必须项。
- Adopt Now：统一 manager/facade 是正确 API 形态。
- Adopt Now：elapsed/remaining query 应通过 handle，而不是池化对象引用。

### 5.4 Unity

**Evidence**

本地 UnityCsReference UIElements：

- `TimerEventScheduler` 持有 scheduled items list。
- `UpdateScheduledEvents()` 在 panel update 中检查时间并执行 `PerformTimerUpdate`。
- `VisualElement.schedule` 文档说明 scheduler 用于 delay/repeated action，元素 detach 时 scheduler pause。

Context7 Unity docs 补充：

- Unity 常见延迟/周期工作通过 coroutine、`WaitForSecondsRealtime` 等模型表达。

**Inference**

Unity 生态同样区分主循环调度、UI attachment lifecycle 和真实时间/游戏时间。即使用 coroutine，业务 callback 仍回到 Unity 主循环上下文，不是 ThreadPool 任意执行 gameplay。

**Decision**

- Adopt Now：owner attachment / detachment 对 timer 生命周期有意义。
- Adopt Now：UI/unscaled 与 gameplay/scaled 分离。
- Reject：把 coroutine 体系或 Unity schedule API 复制到 SlimeAI。

### 5.5 .NET Timers

**Evidence**

Context7 Microsoft Learn：

- `System.Threading.Timer` 常用于 timed hosted service。
- `DoWork` 由 timer 触发。
- Microsoft 文档说明该 Timer 不等待上一次 `DoWork` 完成。
- `PeriodicTimer` 常用于 async loop 中等待下一个 tick。

**Inference**

`.NET Timer` 是后台定时任务工具，不是游戏主线程 schedule。它的优点是底层等待和线程池唤醒；它的缺点正是 gameplay Timer 不能接受的点：线程上下文、顺序、重入、暂停、timeScale、owner 和主线程 API 安全都不在它的职责内。

**Decision**

- Reject：`.NET Timer` 直接执行 SlimeAI gameplay 回调。
- Adopt Later：工具层、后台 IO、编辑器服务可用 `.NET Timer` 作为 wake-up source，但只能投递 main-thread command。
- Reject：把 `Task.Delay` 当作 gameplay Timer 默认。

### 5.6 ET Framework

**Evidence**

ET `Book/2.3单线程异步.md` 明确说明：

- 用 Sleep 实现等待时，每个计时器需要一个线程，会导致频繁线程切换，效率低。
- 一般游戏逻辑会设计单线程 timer。
- 示例中主线程每帧调用 `CheckTimerOut`，过期则调用回调。
- 文中明确：异步并非多线程，单线程同样可以异步。

**Inference**

这直接反驳“底层 Timer 已经写好多线程，所以游戏框架应该直接用”的直觉。游戏逻辑需要的是单线程异步和可控回调，而不是每个等待一个线程或 ThreadPool callback。

**Decision**

- Adopt Now：gameplay Timer 主线程派发。
- Adopt Now：异步等待语义可以由 scheduler 提供，不需要多线程执行 gameplay。

### 5.7 IFramework

**Evidence**

本地 IFramework `TimerModule : UpdateModule`：

- `Awake` 初始化 `_entities` 和 `_lastTime`。
- `OnUpdate` 计算 `deltaTime`。
- 倒序遍历 timer entity，done 后 remove/reset，否则 `entity.Update(deltaTime)`。

**Inference**

IFramework 也是统一 update module 处理 timer，不是每个业务点直接开底层 timer。它仍是线性扫描，适合参考状态机和统一入口，但 SlimeAI 的最终目标应比它更强：零分配、handle、owner/purpose、heap 或 timing wheel。

**Decision**

- Adopt Now：统一 timer module 思想。
- Adopt Later：TimerEntity 状态机可参考。
- Reject：照搬线性 List 扫描作为 SlimeAI 最终方案。

### 5.8 QFramework

**Evidence**

本地 QFramework ActionKit `DelayAction`：

- `DelayAction.Allocate` 从 `SafeObjectPool` 分配。
- `OnExecute(float dt)` 累加 `CurrentSeconds`。
- `CurrentSeconds >= DelayTime` 时完成并回调。
- `OnDispose` 回收到对象池。

**Inference**

QFramework 证明“dt 驱动 + 对象池 + action lifecycle”是成熟 Unity 工具常见做法。但 SlimeAI 需要框架级 owner、purpose、observation 和更强数据结构。

**Decision**

- Adopt Now：对象池/复用可保留。
- Reject：仅靠池化对象和 per-frame Execute 即认为性能完成。

### 5.9 Netty HashedWheelTimer

**Evidence**

Context7 Netty docs：

- Hashed wheel timer 是 `java.util.Timer` 和 `ScheduledThreadPoolExecutor` 的 scalable alternative。
- 适合大量 scheduled tasks 和 cancellations。
- 大多数 timer 操作借助内部 hash table 达到接近常数复杂度。

**Inference**

Timing wheel 是大规模 timeout 的成熟数据结构，特别适合 IO timeout、大量取消和粗粒度延迟。但它带来 tick granularity、bucket cascade、调试复杂度。SlimeAI 当前第一目标是修掉 per-frame allocation 和 full scan，并建立 owner/handle/clock 契约；min-heap 更简单、精确、容易验证。

**Decision**

- Adopt Later：当 benchmark 显示 10k/50k+ timer 压力下 heap 成为瓶颈，再引入 hierarchical timing wheel。
- Reject：第一版直接上 timing wheel 导致复杂度过高。

## 6. Problem Shape

### 6.1 用户直觉中正确的部分

你认为“手写 Timer、每帧更新所有 Timer、便利做法不够底层”是对的。当前 SlimeAI 不能把 `ObjectPool.ForEachActive().ToList()` 写在 Timer 热路径里，还宣称高性能/零 GC。

### 6.2 需要纠正的部分

“底层 C# Timer 已经写好多线程，所以更高效”这个推断不适合 gameplay Timer。

底层 Timer 解决的是：

- 什么时候唤醒线程。
- 什么时候触发后台任务。
- 如何让服务周期执行。

SlimeAI gameplay Timer 解决的是：

- 按游戏时间还是现实时间推进。
- 暂停时是否停住。
- 回调在哪个 schedule phase 执行。
- owner 销毁时是否取消。
- 如何避免 stale handle。
- 如何观测泄漏。
- 如何保证 Godot/Entity/Data/Event 主线程安全。

这两个问题域不同。

### 6.3 隐藏约束

- Godot active SceneTree 不线程安全。
- SlimeAI Data/Event/Entity 当前没有锁模型。
- gameplay callback 顺序影响行为结果。
- object pool reuse 需要 generation 防线。
- AI-first 框架需要可观测，不只是快。
- 通用框架要能被测试 runner/headless 使用。

## 7. Main Risks

| 风险 | 说明 | 解决方向 |
| --- | --- | --- |
| 热路径 GC | 当前 `ForEachActive` 每帧 `ToList()` | Timer 不再依赖 ObjectPool active snapshot；scheduler 自有数组/heap/index。 |
| 大量远期 timer 浪费 | 当前每帧扫全部 | min-heap 只 pop due timer。 |
| ThreadPool 回调破坏主线程 | .NET Timer callback 不在 Godot 主线程 | gameplay callback 只进入 main-thread dispatch。 |
| Timer 身份 stale | `GameTimer` 池化对象被长期持有 | `TimerHandle(id, generation)`。 |
| tag 误伤 | `CancelByTag("Buff")` 无 owner 边界 | `CancelByOwner`、`CancelByPurpose`，tag 只做辅助筛选。 |
| pause 语义混乱 | bool `useUnscaledTime` 不足以描述 clock | `TimerClock.Game/Real/Fixed`。 |
| 过度设计 | 直接上 timing wheel 复杂度高 | 第一版 min-heap；benchmark 触发 timing wheel。 |
| 兼容破坏 | 大量调用点当前持有 `GameTimer?` | facade 保留旧 API，内部转换 handle，调用点一次性迁移高风险 owner。 |

## 8. Options

### Option A：继续当前实现，只修 ToList 分配

做法：

- 修改 ObjectPool 或 TimerManager 遍历，避免每帧 `ToList()`。
- 继续所有 timer 每帧扫描。
- 继续返回 `GameTimer`。

优点：

- 改动小。
- 回归风险低。

缺点：

- 仍是 O(N) full scan。
- 仍无 handle/owner/purpose。
- 仍无法做到通用框架级 Timer。

裁决：Reject。它只是局部补丁，不符合“一次性做到最好”。

### Option B：纯 C# min-heap TimerScheduler

做法：

- 新建纯 C# `TimerScheduler`。
- `TimerManager` 保留为 facade/driver。
- 使用 `PriorityQueue` 或自实现二叉/四叉 min-heap 按 dueTime 调度。
- handle + generation。
- owner/purpose/clock/cancel reason/observation。
- 到期回调主线程派发。
- `OnUpdate` timer 进入单独 per-frame list。

优点：

- 解决当前 full scan 和 per-frame allocation。
- 精确、简单、可测试。
- 能完整承载 SlimeAI owner/purpose/observation。
- 与成熟框架 manager/handle/tick 模式一致。

缺点：

- 需要重构 Timer 内部结构和调用点。
- heap 对超大规模同质 timeout 不一定最优。

裁决：Adopt Now。作为第一版最终架构。

### Option C：直接 hierarchical timing wheel

做法：

- 用多层时间轮按 bucket 存 timer。
- 每 tick 推进 wheel，处理 bucket 中到期 timer。

优点：

- 大量 timer 和大量取消时性能好。
- 接近 O(1) schedule/cancel。

缺点：

- 粒度、层级、cascade、长延迟、debug 难度明显增加。
- 当前 SlimeAI Timer 还缺 handle/owner/purpose/observation，直接上 wheel 会把核心契约和复杂数据结构耦合。

裁决：Adopt Later。作为 benchmark 触发的第二调度后端。

## 9. Recommendation

推荐 Option B：

```text
纯 C# TimerScheduler
  + min-heap by dueTime
  + TimerHandle generation
  + TimerOwner / TimerPurpose / TimerClock
  + main-thread dispatch
  + per-frame update list
  + owner/purpose observation
  + TimerManager compatibility facade
```

这是当前最平衡的“做到最好”：

- 比当前实现更底层、更可控。
- 比 Godot Timer 更适合框架 owner/observation。
- 比 .NET Timer 更符合 gameplay 主线程和 timeScale。
- 比 timing wheel 更容易一次性验证正确。

## 10. Must Confirm

本文作为设计文档写入，不需要额外确认才能完成。

进入实现前唯一必须冻结的实现口径：

```text
是否允许 Timer 内部 hard cutover，同时保留旧 TimerManager API 作为兼容 facade？
```

默认值见下一节。

## 11. Defaults I Will Use

如果后续用户说“按这个设计实现”，默认采用：

- 内部 hard cutover：当前 `_timerPool.ForEachActive()` 不再作为 Timer 调度核心。
- 外部 source compatibility：`Delay/Loop/Repeat/Countdown` 旧 API 先保留。
- 旧 API 返回对象可短期兼容，但底层身份以 `TimerHandle` 为准。
- 新 gameplay 调用点必须传 owner/purpose。
- 高风险调用点一次性迁移，不只迁移一个组件。
- 不使用 .NET Timer 做 gameplay callback。
- 不使用 Godot Timer 做 gameplay 默认。
- benchmark 不达标才引入 timing wheel，不提前复杂化。

## 12. Not Recommended

- 不推荐只修 `ToList()`，因为这仍保留 O(N) full scan 和生命周期缺口。
- 不推荐把每个 gameplay timer 改成 Godot `Timer` node，因为会丢 owner/purpose/observation，也增加 SceneTree 分散管理。
- 不推荐用 `GetTree().CreateTimer()` 作为框架默认，因为它是 one-shot convenience，不是 SlimeAI gameplay timer contract。
- 不推荐用 `System.Threading.Timer` 直接执行 gameplay callback，因为线程和顺序不可控。
- 不推荐为了“底层高效”马上多线程化 Timer，因为游戏状态一致性成本更高。
- 不推荐第一版直接 timing wheel，因为会掩盖 owner/handle/clock 这些更重要的契约。

## 13. Artifact Updates

本轮将结论写入：

- `README.md`：总裁决和阅读入口。
- `01-现状证据与AI-first裁决.md`：当前文件，记录证据和采纳/拒绝。
- `02-目标架构与优化路线.md`：纯 C# scheduler 目标架构。
- `03-调用点迁移与验证计划.md`：迁移和验证计划。
