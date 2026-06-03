# Timer Scheduler Full Rewrite

## Goal

把 SlimeAI Timer 工具提升到通用框架级质量。最终实现必须保留 `TimerManager` 统一入口，但内部切换为纯 C# `TimerScheduler`，并补齐 handle、owner、purpose、clock、主线程派发、debug diagnostics、压力场景和文档门禁。

本 SDD 的目标不是“Timer 还能 delay 回调”，而是让 Timer 成为可观测、可测试、可 benchmark、可跨宿主复用、可被 AI 正确使用的底层工具。

## Context

当前实现的关键问题来自项目级 Timer 设计包：

- `TimerManager._Process(double delta)` 每帧调用 `ProcessTimers(delta)`。
- `ProcessTimers` 通过 `_timerPool.ForEachActive(timer => timer.Update(dt))` 遍历所有 active timer。
- `ObjectPool.ForEachActive()` 内部调用 `GetActiveSnapshot()`，而 `GetActiveSnapshot()` 使用 `_activeItems.ToList()`，导致 Timer 热路径每帧分配。
- 所有 active timer 每帧被扫描，即使大部分 timer 离到期很远。
- `GameTimer` 作为池化对象被调用方长期持有，没有 `id + generation` 防 stale 引用。
- `Tag` 字符串承载了 owner/purpose 语义，无法可靠处理生命周期 cleanup 和 observation。
- 当前 observation 只有 active count / stats，缺少按 owner、purpose、clock、state、due time、remaining、pause reason、cancel reason 的诊断。
- 当前没有 Timer 专属压力验证场景，无法证明大量 timer、暂停、Real/Game clock、主线程派发和 diagnostics artifact 都正确。

成熟框架对照结论：

- Godot、Unreal、Unity UIElements、Bevy、ET、IFramework、QFramework 都证明 gameplay timer 通常由主循环或统一 manager/scheduler 推进，不是每个 timer 一个线程。
- `.NET System.Threading.Timer`、`System.Timers.Timer`、`PeriodicTimer` 适合后台唤醒/服务心跳，不适合作为 gameplay callback 默认执行器。
- 多线程 timer 到期后也只能投递到 main-thread queue，不能直接操作 Entity/Data/Event/Godot。

## Design

### 分层

```text
TimerManager
  -> 对外 API、Godot Node 生命周期、_Process/_PhysicsProcess driver、project pause adapter、兼容 facade、debug facade

TimerScheduler
  -> 纯 C# core；不继承 Node；不调用 Godot API；维护 heap、clock、handle、owner/purpose index、dispatch queue、diagnostics snapshot

TimerHandle
  -> readonly value identity: id + generation

TimerOptions
  -> owner + purpose + clock + tag + dispatch phase + optional source

TimerDiagnostics
  -> explicit-call snapshot / summary / filtered dump / JSON artifact
```

### 必须采纳

- 第一版调度后端使用 min-heap by due time；timing wheel 只在 benchmark 证明 heap 成为瓶颈后引入。
- 普通 `Tick(no due)` 不随 active timer 总数线性增长。
- 普通 tick 不分配。
- `Delay/Loop/Repeat/Countdown` 旧入口短期保留，但内部不再依赖对象池 active scan。
- 新 gameplay timer 必须有 owner、purpose、clock 和 cancel point。
- `useUnscaledTime=false` 映射 `TimerClock.Game`，`useUnscaledTime=true` 映射 `TimerClock.Real`。
- `SimulationState.Suspended` 暂停 Game clock，不暂停 Real clock。
- 到期 callback 进入主线程 dispatch phase；callback 中结构变更必须遵守 RuntimeCommandBuffer 规则。
- `OnUpdate` / progress timer 进入显式 per-frame list，不拖累普通 due timer。
- `CancelByOwner`、`CancelByOwnerAndPurpose`、pause mask、cancel reason 是框架级能力。
- diagnostics 默认 summary/top N，full dump 需要 filter/MaxEntries；JSON export 用于测试 artifact。

### 必须拒绝

- 不把 Godot `Timer` / `SceneTreeTimer` 作为 SlimeAI gameplay 默认。
- 不把 `.NET Timer` / `Task.Delay` / `PeriodicTimer` 直接用于 gameplay callback。
- 不做“只修 `ToList()`”或“只加 handle 但仍全量扫描”的局部优化。
- 不让 tag 继续作为 owner/purpose 的唯一语义。
- 不在 Timer core 中调用 `GD.Print`、`SceneTree`、`Time.GetTicksMsec` 或 Godot Node API。
- 不把 debug 输出放进每帧自动日志。

### 高风险调用点

执行时必须覆盖：

- `CooldownComponent` -> `TimerPurpose.Cooldown`
- `ChargeComponent` -> `TimerPurpose.Charge`
- `TriggerComponent` -> `TimerPurpose.PeriodicTrigger`
- `AttackComponent` -> `AttackWindup` / `AttackRecovery` / `AttackValidation`
- `ContactDamageComponent` -> per target relation owner + `ContactDamage`
- `DamageTool.ScheduleDoT` -> caller/effect owner + `DoT`
- `SpawnSystem` -> `SpawnWave` / `SpawnCheck`
- `WaitIdleAction` -> `AIWait`
- `DecoratorNode` -> `DecoratorCooldown`
- `ChainLightning` -> ability/feature owner + `ChainDelay`

Test/demo timer 可使用 `TimerOwnerType.Test` 和 `TimerPurpose.Test`，但不能把测试例外扩散到 gameplay。

## Verification

最低验证面：

- Pure C# core tests 覆盖 delay/loop/repeat/countdown/cancel/pause/stale handle/owner cancel/callback mutation/per-frame update。
- TimerManager adapter tests 覆盖旧 API、Game/Real clock 映射、project pause 和 runtime info。
- Benchmark 覆盖 1k/10k long delay no due、10k staggered due、10k cancel by owner、1k per-frame progress。
- Debug diagnostics 覆盖 summary、filtered dump、JSON export、leak hints、MaxEntries。
- 新增 `Src/ECS/Tools/Timer/Tests/TimerStressValidation.cs/.tscn/README.md` 或等价 headless 场景，README 包含 scene-gate 五字段，并输出 PASS artifact JSON。
- DocsAI Timer 文档删除不存在 API：`TimerManager.Create`、`.WithTimeScale`、`PauseByTag`、`ResumeByTag`。
- 修改 `.ai-config/skills/.../tools` 后必须 sync + skill-test。

最终命令至少包括：

```bash
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
python3 Workspace/SDD/sdd.py validate SDD-0027
python3 Workspace/SDD/sdd.py validate --all
rg -n "GetTree\\(\\)\\.CreateTimer|new Timer\\(|System\\.Threading\\.Timer|System\\.Timers\\.Timer|PeriodicTimer|Task\\.Delay" Src Data DocsAI
rg -n "TimerManager\\.Create|WithTimeScale|PauseByTag|ResumeByTag" DocsAI SDD Src Data
rg -n "_timerPool\\.ForEachActive|GetActiveSnapshot\\(\\)|ToList\\(\\)" Src/ECS/Tools/Timer Src/ECS/Tools/ObjectPool
```

涉及 Godot 场景后，还必须在承载游戏仓运行 Timer stress scene、日志分析和 scene-gate 检查；BrotatoLike 主场景 smoke 需要无 Timer 相关错误。
