# SDD-0027 Execution Prompt

把本文件整体交给新的执行会话。目标是一次性完成 `SDD-0027 Timer Scheduler Full Rewrite`，不是做局部补丁。

## 角色定位

你是 SDD-0027 的主执行者和集成者。默认中文回答；命令、代码、错误信息保留原文。大任务先计划，再执行。改文件前先读相关文件，改完总结改动和验证结果。不要 push。不要随意加依赖、大重构或跨 git 边界混提交。

执行时必须使用相关 skill：

- `sdd-workflow` / `sdd-management`：恢复和更新 SDD。
- `tools`：Timer 属于 ECS Tools owner。
- `test-system`：新增 Timer tests、benchmark、diagnostics artifact。
- `godot-scene-test` 和 `scene-gate`：新增或运行 TimerStressValidation 场景。
- `ai-config-management` / `skill-test`：仅当 `.ai-config` skill 源需要同步时使用。

## 工作区

- **Framework Git Boundary**: `/home/slime/Code/SlimeAI/SlimeAI`
- **Game Validation Git Boundary**: `/home/slime/Code/SlimeAI/Games/BrotatoLike`
- **Project**: `SDD/project/projects/PRJ-0002-ecs-framework-refactor/`
- **Current SDD**: `sdds/017-SDD-0027-timer-scheduler-full-rewrite/`
- **Shared Design Package**: `design/Tool/Timer/`

每次执行 git 操作前先确认边界：

```bash
cd /home/slime/Code/SlimeAI/SlimeAI
git status --short
```

当前工作区可能已有 unrelated `.uid`、AI 配置、DocsAI、ObjectPool、Timer 设计文档或 `__pycache__` 改动。不要清理、回滚、覆盖或混入无关改动。

## 必读顺序

先读规则和项目入口：

1. `AGENTS.md`
2. `DocsAI/README.md`
3. `DocsAI/ECS/README.md`
4. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/README.md`
5. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/roadmap.md`
6. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/progress.md`

再读 Timer 共享设计包：

1. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Tool/Timer/README.md`
2. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Tool/Timer/01-现状证据与AI-first裁决.md`
3. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Tool/Timer/02-目标架构与优化路线.md`
4. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Tool/Timer/03-调用点迁移与验证计划.md`

再读本 SDD：

1. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/sdds/017-SDD-0027-timer-scheduler-full-rewrite/README.md`
2. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/sdds/017-SDD-0027-timer-scheduler-full-rewrite/design/main.md`
3. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/sdds/017-SDD-0027-timer-scheduler-full-rewrite/tasks.md`
4. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/sdds/017-SDD-0027-timer-scheduler-full-rewrite/bdd.md`
5. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/sdds/017-SDD-0027-timer-scheduler-full-rewrite/progress.md`
6. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/sdds/017-SDD-0027-timer-scheduler-full-rewrite/notes.md`

最后读源码和当前文档：

1. `Src/ECS/Tools/Timer/TimerManager.cs`
2. `Src/ECS/Tools/Timer/GameTimer.cs`
3. `Src/ECS/Tools/ObjectPool/ObjectPool.cs`
4. `DocsAI/ECS/Tools/Timer/Concept.md`
5. `DocsAI/ECS/Tools/Timer/Usage.md`
6. 高风险调用点：Cooldown、Charge、Trigger、Attack、ContactDamage、DamageTool、SpawnSystem、WaitIdleAction、DecoratorNode、ChainLightning。

## 核心裁决

- 保留 `TimerManager` 作为统一入口和 Godot 生命周期 adapter。
- 内部重做为纯 C# `TimerScheduler`。
- 第一调度结构使用 min-heap by due time。
- timing wheel 只有 benchmark 证明 heap 成为瓶颈后才引入。
- gameplay callback 必须主线程派发。
- 不用 Godot `Timer` / `SceneTreeTimer` 作为 gameplay 默认。
- 不用 `.NET System.Threading.Timer`、`System.Timers.Timer`、`PeriodicTimer`、`Task.Delay` 直接执行 gameplay callback。
- 不保留“每帧扫描所有 active timer + 每帧 ToList 快照”作为最终架构。
- 新 gameplay timer 必须有 owner、purpose、clock 和 cancel point。
- Debug diagnostics 和 TimerStressValidation 是完成定义，不是可选增强。

## 禁止结果

- 不只修 `ObjectPool.GetActiveSnapshot().ToList()`。
- 不只加 `TimerHandle`，但 Timer 主循环仍扫所有 active timer。
- 不把所有旧调用点继续靠 `.WithTag("...")` 表达语义。
- 不在 `TimerScheduler` core 内调用 Godot API、`GD.Print`、`SceneTree` 或 `Time.GetTicksMsec`。
- 不在 ThreadPool callback 中调用 `EntityManager.Spawn/Destroy`、`Data.Set`、`EventBus.Emit`、`Node.AddChild/QueueFree`。
- 不新增 gameplay `GetTree().CreateTimer`、`new Timer()`、`.NET Timer` 或 `Task.Delay`。
- 不让 debug dump 每帧自动打印或无上限输出。
- 不声称场景验证通过，除非有 README 五字段、scene-gate 结果和 PASS JSON artifact。

## T1.1 Readiness Baseline

先只读，不改实现。记录摘要到 `progress.md`，不要复制完整 dirty 列表。

```bash
cd /home/slime/Code/SlimeAI/SlimeAI
git status --short
sed -n '1,260p' Src/ECS/Tools/Timer/TimerManager.cs
sed -n '1,260p' Src/ECS/Tools/Timer/GameTimer.cs
sed -n '1,220p' Src/ECS/Tools/ObjectPool/ObjectPool.cs
rg -n "TimerManager\\.Instance\\.(Delay|Loop|Repeat|Countdown)|CreateTimer|new Timer\\(|Task\\.Delay|WithTag\\(" Src Data DocsAI
rg -n "_timerPool\\.ForEachActive|GetActiveSnapshot\\(\\)|ToList\\(\\)" Src/ECS/Tools/Timer Src/ECS/Tools/ObjectPool
rg -n "TimerManager\\.Create|WithTimeScale|PauseByTag|ResumeByTag" DocsAI SDD Src Data
rg -n "GetTree\\(\\)\\.CreateTimer|new Timer\\(|System\\.Threading\\.Timer|System\\.Timers\\.Timer|PeriodicTimer|Task\\.Delay" Src Data DocsAI
python3 Workspace/SDD/sdd.py validate SDD-0027
```

完成后勾选 `T1.1`，追加 progress：Context / Conclusion / Evidence / Impact / Resume。

## 实现顺序

严格按 `tasks.md` 推进：

1. T1.2 先补 Pure C# TimerScheduler tests。
2. T1.3 实现 `TimerScheduler` core。
3. T1.4 改 `TimerManager` adapter 和旧 API compatibility。
4. T1.5 迁移高风险 owner/purpose 调用点。
5. T1.6 补 debug diagnostics API。
6. T1.7 补 benchmark 和性能证据。
7. T1.8 新增 TimerStressValidation 场景、README 五字段和 PASS JSON artifact。
8. T1.9 做 gameplay regression validation。
9. T1.10 同步 DocsAI Timer 文档。
10. T1.11 若改 API/流程，同步 `.ai-config` tools skill 并运行 sync/skill-test。
11. T1.12 跑最终 gates，回填 SDD 和项目进度。

每完成一项任务就更新 `tasks.md` 和 `progress.md`。不要等到最后一次性补状态。

## 目标代码形态

推荐新增：

```text
Src/ECS/Tools/Timer/Core/TimerScheduler.cs
Src/ECS/Tools/Timer/Core/TimerDueQueue.cs
Src/ECS/Tools/Timer/Core/TimerEntry.cs
Src/ECS/Tools/Timer/Core/TimerHandle.cs
Src/ECS/Tools/Timer/Core/TimerOptions.cs
Src/ECS/Tools/Timer/Core/TimerClock.cs
Src/ECS/Tools/Timer/Core/TimerOwner.cs
Src/ECS/Tools/Timer/Core/TimerPurpose.cs
Src/ECS/Tools/Timer/Core/TimerCancelReason.cs
Src/ECS/Tools/Timer/Core/TimerPauseMask.cs
Src/ECS/Tools/Timer/Core/TimerObservation.cs
Src/ECS/Tools/Timer/Core/TimerDiagnosticsSnapshot.cs
Src/ECS/Tools/Timer/Core/TimerDiagnosticsFilter.cs
Src/ECS/Tools/Timer/Core/TimerDiagnosticsFormatter.cs
```

推荐 API：

```csharp
public TimerHandle Delay(float duration, TimerOptions options, Action onComplete);
public TimerHandle Loop(float interval, TimerOptions options, Action onLoop);
public TimerHandle Repeat(float interval, int count, TimerOptions options, Action<int> onRepeat, Action? onComplete = null);
public TimerHandle Countdown(float duration, float interval, TimerOptions options, Action<float, float> onTick, Action? onComplete = null);
public bool Cancel(TimerHandle handle, TimerCancelReason reason = TimerCancelReason.Manual);
public int CancelByOwner(TimerOwner owner, TimerCancelReason reason);
public int CancelByOwnerAndPurpose(TimerOwner owner, TimerPurpose purpose, TimerCancelReason reason);
public bool TryGetRemaining(TimerHandle handle, out float remaining);
public bool TryGetProgress(TimerHandle handle, out float progress);
```

旧 `Delay/Loop/Repeat/Countdown` 可保留 compatibility wrapper，但内部必须转发 scheduler，不能继续由 `GameTimer.Update` 每帧扫 active pool。

## 必迁调用点

- `CooldownComponent`: Component/Ability owner, `Cooldown`
- `ChargeComponent`: Component/Ability owner, `Charge`
- `TriggerComponent`: Component/Ability owner, `PeriodicTrigger`
- `AttackComponent`: `AttackWindup` / `AttackRecovery` / `AttackValidation`
- `ContactDamageComponent`: source-target relation owner, `ContactDamage`
- `DamageTool.ScheduleDoT`: caller/effect owner, `DoT`
- `SpawnSystem`: System owner, `SpawnWave` / `SpawnCheck`
- `WaitIdleAction`: AI node/entity owner, `AIWait`
- `DecoratorNode`: AI node/entity owner, `DecoratorCooldown`
- `ChainLightning`: Ability/Feature owner, `ChainDelay`

规则：

- loop timer 必须有 cancel point。
- 多 timer 组件不能共用泛 purpose。
- 多目标 timer owner 必须带 relation id 或 target id。
- tag 只能辅助分类，不能替代 owner/purpose。

## Diagnostics 要求

必须提供：

```csharp
public TimerDiagnosticsSnapshot GetTimerDiagnostics(TimerDiagnosticsFilter? filter = null);
public string FormatTimerSummary(TimerDiagnosticsSnapshot snapshot, int topN = 10);
public string FormatTimerDump(TimerDiagnosticsSnapshot snapshot);
public void PrintTimerSummary(int topN = 10);
public void PrintTimerDump(TimerDiagnosticsFilter? filter = null);
public Error ExportTimerDiagnosticsJson(string path, TimerDiagnosticsFilter? filter = null);
```

snapshot 至少包含：

- activeCount
- pausedCount
- dispatchQueueCount
- perFrameUpdateCount
- heapCountByClock
- activeCountByOwnerType
- topOwners
- activeCountByPurpose
- activeCountByClock
- cancelledLazyHeapItems
- lastTickCostMs / maxTickCostMs
- lastDispatchCostMs / maxDispatchCostMs
- maxCallbacksDispatchedInFrame
- leakHints
- bounded entries

## TimerStressValidation 场景

建议新增：

```text
Src/ECS/Tools/Timer/Tests/TimerStressValidation.cs
Src/ECS/Tools/Timer/Tests/TimerStressValidation.tscn
Src/ECS/Tools/Timer/Tests/README.md
```

README 必须包含：

- `expectedInputs`
- `expectedObservations`
- `passCriteria`
- `failCriteria`
- `artifactPath`

场景必须自动执行并输出 JSON artifact，例如：

```text
.ai-temp/scene-tests/artifacts/timer-stress-validation.json
```

checks 至少覆盖：

- `LongDelayNoDue`
- `StaggeredDue`
- `CancelByOwner`
- `LoopRepeatCountdown`
- `GamePauseRealClock`
- `PerFrameUpdateIsolation`
- `OwnerPurposeLeakHints`
- `MainThreadDispatch`

运行命令：

```bash
cd /home/slime/Code/SlimeAI/Games/BrotatoLike
Tools/run-godot-scene.sh run res://SlimeAI/Src/ECS/Tools/Timer/Tests/TimerStressValidation.tscn --timeout 20 --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh
```

如果实际资源路径不同，按游戏仓真实挂载路径调整，并把真实命令记录到 progress。

## Grep Gates

禁止新增 gameplay Godot/.NET timer：

```bash
rg -n "GetTree\\(\\)\\.CreateTimer|new Timer\\(|System\\.Threading\\.Timer|System\\.Timers\\.Timer|PeriodicTimer|Task\\.Delay" Src Data DocsAI
```

禁止 current docs 教不存在 API：

```bash
rg -n "TimerManager\\.Create|WithTimeScale|PauseByTag|ResumeByTag" DocsAI SDD Src Data
```

检查 owner/purpose 迁移：

```bash
rg -n "TimerManager\\.Instance\\.(Delay|Loop|Repeat|Countdown)|WithTag\\(\"" Src Data
```

检查旧热路径：

```bash
rg -n "_timerPool\\.ForEachActive|GetActiveSnapshot\\(\\)|ToList\\(\\)" Src/ECS/Tools/Timer Src/ECS/Tools/ObjectPool
```

允许 `ObjectPool` 保留通用 snapshot API；Timer 主循环不得调用它。

## 最低验证命令

框架仓：

```bash
cd /home/slime/Code/SlimeAI/SlimeAI
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
python3 Workspace/SDD/sdd.py validate SDD-0027
python3 Workspace/SDD/sdd.py validate --all
git diff --check -- Src/ECS/Tools/Timer DocsAI/ECS/Tools/Timer SDD/project/projects/PRJ-0002-ecs-framework-refactor
```

如果改到 Data authoring 或 Feature 数据：

```bash
bash Data/DataOS/Tools/validate-dataos.sh Data/DataOS/Authoring/slimeainew.authoring.db
```

如果改 `.ai-config`：

```bash
bash Workspace/Tools/ai-config-sync/sync-ai-config.sh
bash Workspace/SystemAgent/Tools/skill-test/lint.sh static all --no-fail --summary-only
```

游戏仓 smoke：

```bash
cd /home/slime/Code/SlimeAI/Games/BrotatoLike
Tools/run-godot-scene.sh run res://Scenes/Main.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/analyze-godot-scene-logs.sh
```

## 完成标准

- `tasks.md` T1.1 到 T1.12 全部完成或有明确 blocker。
- `progress.md` 有 baseline、核心实现结论、diagnostics、压力场景、验证摘要和 Latest Resume。
- Timer core 是纯 C#，可脱离 Godot 测试。
- TimerManager 不再用 `_timerPool.ForEachActive` 更新 timer。
- 普通 tick no due 不分配，不随 active total 线性增长。
- `TimerHandle` generation 生效。
- 高风险 gameplay 调用点有 owner/purpose/cancel point。
- Debug summary/dump/JSON export 可用。
- TimerStressValidation 有 README 五字段、scene-gate 结果和 PASS artifact。
- DocsAI Timer current 文档与真实 API 一致。
- build、SDD validate、grep gates、Timer stress scene、BrotatoLike smoke 有新鲜证据。
