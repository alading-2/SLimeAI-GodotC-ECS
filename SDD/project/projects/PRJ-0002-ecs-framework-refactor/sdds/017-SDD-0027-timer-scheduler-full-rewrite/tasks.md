# Tasks

## Progress

- **Status**: blocked
- **Completed**: 9/12
- **Current**: T1.8

## Task List

- [x] T1.1 Readiness baseline
  - **Scope**: 确认 git boundary、dirty worktree、Timer 当前热路径、调用点、DocsAI 漂移、Godot/.NET Timer 命中和 SDD 校验基线。
  - **Validation**: `git status --short`；Timer grep gates；`python3 Workspace/SDD/sdd.py validate SDD-0027`。

- [x] T1.2 Pure C# scheduler tests first
  - **Scope**: 为 delay、loop、repeat、countdown、cancel、pause、clock、stale handle、owner cancel、callback mutation、per-frame update 写 RED tests 或等价 headless core tests。
  - **Validation**: Timer core test command 有明确 RED/green 记录。

- [x] T1.3 TimerScheduler core
  - **Scope**: 新增纯 C# `TimerScheduler`、`TimerDueQueue`、`TimerEntry`、`TimerHandle`、`TimerOptions`、`TimerClock`、`TimerOwner`、`TimerPurpose`、`TimerCancelReason`、`TimerPauseMask` 和 dispatch queue。
  - **Validation**: core 不依赖 Godot；普通 tick no due 不分配；heap lazy cancel、generation、pause/resume、owner/purpose index 测试通过。

- [x] T1.4 TimerManager adapter and compatibility facade
  - **Scope**: `TimerManager` 改为 scheduler driver；旧 `Delay/Loop/Repeat/Countdown` API 保留并转发；`GameTimer` 降为 compatibility wrapper 或 builder；停止 Timer 主循环依赖 `_timerPool.ForEachActive`。
  - **Validation**: 旧 API adapter tests 通过；`rg "_timerPool\\.ForEachActive" Src/ECS/Tools/Timer` 无主循环命中。

- [x] T1.5 Owner/purpose callsite migration
  - **Scope**: 迁移 `CooldownComponent`、`ChargeComponent`、`TriggerComponent`、`AttackComponent`、`ContactDamageComponent`、`DamageTool.ScheduleDoT`、`SpawnSystem`、`WaitIdleAction`、`DecoratorNode`、`ChainLightning`。
  - **Validation**: 每个 gameplay timer 可追溯 owner/purpose/cancel point；`.WithTag("...")` 不再作为唯一语义。

- [x] T1.6 Debug diagnostics API
  - **Scope**: 新增 diagnostics snapshot、filter、formatter、`GetTimerDiagnostics`、`PrintTimerSummary`、`PrintTimerDump`、`ExportTimerDiagnosticsJson` 和 leak hints。
  - **Validation**: 10k timer summary/top N/filtered dump/JSON export 可用；普通 tick 不因 diagnostics 分配。

- [x] T1.7 Timer benchmark and performance evidence
  - **Scope**: 覆盖 1k/10k long delay no due、10k staggered due、10k cancel by owner、1k per-frame progress；记录 tick、dispatch、cancel、allocation 和 lazy heap 指标。
  - **Validation**: 普通 tick no due 不随 active total 线性增长；timing wheel 是否需要由结果决定。

- [ ] T1.8 TimerStressValidation scene and scene-gate artifact
  - **Scope**: 新增 `Src/ECS/Tools/Timer/Tests/TimerStressValidation.cs/.tscn/README.md`；README 包含 `expectedInputs`、`expectedObservations`、`passCriteria`、`failCriteria`、`artifactPath`；场景输出 PASS JSON artifact。
  - **Validation**: Godot scene runner、log analyzer、scene-gate 全部通过；artifact 覆盖 LongDelayNoDue、StaggeredDue、CancelByOwner、LoopRepeatCountdown、GamePauseRealClock、PerFrameUpdateIsolation、OwnerPurposeLeakHints、MainThreadDispatch。

- [ ] T1.9 Gameplay regression validation
  - **Scope**: 验证 cooldown、charge、attack windup/recovery/validation、contact damage、DoT、spawn wave/check、AI wait reset、pause Game/Real clock。
  - **Validation**: 目标测试或 Godot smoke 通过；BrotatoLike 主场景无 Timer 相关错误。

- [x] T1.10 DocsAI Timer sync
  - **Scope**: 更新 `DocsAI/ECS/Tools/Timer/Concept.md`、`Usage.md` 或 README/current 文档，删除不存在 API，新增 `TimerHandle/TimerOptions/TimerOwner/TimerPurpose/TimerClock/diagnostics/stress scene` 示例和禁止项。
  - **Validation**: `rg -n "TimerManager\\.Create|WithTimeScale|PauseByTag|ResumeByTag" DocsAI SDD Src Data` 仅允许历史/拒绝说明命中。

- [x] T1.11 Skill and workflow sync
  - **Scope**: 如果 Timer API、验证流程或工具规则变化，更新 `.ai-config` 中 `tools` owner skill，再运行同步和 skill-test。
  - **Validation**: `bash Workspace/Tools/ai-config-sync/sync-ai-config.sh`；`bash Workspace/SystemAgent/Tools/skill-test/lint.sh static all --no-fail --summary-only`。

- [ ] T1.12 Final gates and SDD closeout
  - **Scope**: 跑 build、Timer tests/benchmark、grep gates、TimerStressValidation、scene-gate、BrotatoLike smoke、SDD validate；回填 `tasks.md`、`progress.md`、项目 `roadmap/progress`。
  - **Validation**: `dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly`；`python3 Workspace/SDD/sdd.py validate SDD-0027`；`python3 Workspace/SDD/sdd.py validate --all`；所有 Timer gate 有新鲜证据。
