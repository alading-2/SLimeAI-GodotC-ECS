# SDD-0040 T3 Execution Prompt

你是 SDD-0040 的当前执行者。不要从“T2.6 + Godot runner blocker”直接继续实现；用户已经指出 live 打印仍然分离，根因是 `Src/ECS` 源码调用点语义化未完成。

## 必读

1. `AGENTS.md`
2. `DocsAI/README.md`
3. `DocsAI/ECS/README.md`
4. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/README.md`
5. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Tool/10.Log/README.md`
6. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Tool/10.Log/第三部分-源码调用点语义化/README.md`
7. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Tool/10.Log/第二部分-语义提炼整理/03-最终设计与完成清单.md`
8. `SDD/project/projects/PRJ-0002-ecs-framework-refactor/sdds/029-SDD-0040-log-ai-first-observation-hard-cutover/README.md`
9. 同目录下 `tasks.md`、`bdd.md`、`progress.md`、`notes.md`
10. `DocsAI/ECS/Tools/Logger/README.md`

## 当前裁决

- T1 记录层已落地：`LogEntry`、sink、profile、budget、`OperationTrace`、`ValidationSession`。
- T2 离线整理层已落地：`logctl analyze` 默认输出 flow conclusion、success template、failure-first digest，并避免 raw 默认分桶。
- 这两个完成不等于整个 Log 完成。`Src/ECS` 仍有大量普通 `_log.*` 调用和少量直接打印，新运行的 live stdout 仍可能分离。
- 当前任务是 T3.0 方向冻结，不是马上全仓替换 `_log.Info`。
- 最终 Godot scene smoke 仍 blocked 于缺有效承载游戏 runner；这个 blocker 不阻止 T3 设计和静态盘点，但阻止行为通过声明。

## T3 默认建议

未获用户新裁决时，使用这些默认假设推进设计和盘点：

- 默认 live stdout 严格收口：只显示 warn/error/validation verdict/关键 flow summary/run summary/suppressed summary。
- T3 继续归入 SDD-0040，因为它是 Log hard cutover 未完成的同一目标；若后续规模失控，再拆新 SDD。
- 第一验收链路以 MainTest / 主场景启动 / 释放技能 / 生成怪物为准。
- Debug UI / TestSystem 操作日志默认进 debug profile，不污染 AI live stdout。
- 允许短期保留低频、非默认 stdout 的 `_log.Info`；live 可见路径和第一批 owner 必须语义化。

## 执行顺序

### T3.0 方向冻结

先把第三部分 Must Confirm 回答清楚并写入 `progress.md`。未确认前不做大规模源码迁移。

### T3.1 调用点盘点

盘点时不要只提交 grep 原始列表。按 owner 和事实类型分类：

```text
流程型 -> OperationTrace / flow summary
验证型 -> ValidationSession / artifact
高频成功型 -> window aggregate / success template / sample
启动快照型 -> summary / diagnostics snapshot
Debug UI 型 -> debug profile
真异常型 -> warn/error + reasonCode + key fields
```

建议入口：

```bash
rg -n "(_log\\.(Trace|Debug|Info|Success|Warn|Error|Validation)|GD\\.Print|GD\\.Push|Console\\.WriteLine|PrintRich)" Src/ECS -g "*.cs"
rg -n "BeginTrace|CompleteTrace|OperationTrace" Src/ECS -g "*.cs"
```

### T3.2 Owner flow contract

第一批 owner 至少覆盖：

- Runtime/System：`SystemStartup`、`SystemStatusSnapshot`、`SystemLoadFailure`
- Ability：`AbilityCast`、`AbilityTrigger`、`AbilityInventoryChange`
- Spawn：`WaveSpawn`、`EntitySpawnBatch`、`SpawnFailure`
- TargetSelector：`TargetQuerySummary`、异常 query detail
- ObjectPool：`PoolAcquireSummary`、`PoolReleaseSummary`、异常 detail
- Test / Validation：`ValidationSceneRun`、`CheckResult`
- TestSystem / Debug UI：`DebugAction`，默认 debug profile

### T3.3 第一批源码迁移

优先处理 live 可见和 AI debug 主链路：

1. `Src/ECS/Test/GlobalTest/MainTest/MainTest.cs`
2. `Src/ECS/Runtime/Tests/ECSTest/ECSTest.cs`
3. `Src/ECS/Runtime/System/SystemManager.cs`
4. `Src/ECS/Capabilities/Ability/**`
5. `Src/ECS/Capabilities/Spawn/**`
6. `Src/ECS/Tools/TargetSelector/**`
7. `Src/ECS/Tools/ObjectPool/**`
8. `Src/ECS/Capabilities/TestSystem/**`

原则：

- 不机械全仓替换 `_log.Info`。
- 单次操作内多条成功日志合成一个 flow conclusion。
- 成功路径默认 summary，失败路径保留 detail。
- 字段进入 `fields`，message 只保留短摘要。
- 测试断言进入 `ValidationSession`，不再靠文本 pattern。

### T3.4 新 run 验收

需要用户实际运行或有效 Godot runner 产出新 run。没有新 run 时，不声称 live 行为通过。

验收至少检查：

- live stdout 默认只含 warn/error、validation、flow summary、run summary。
- `logctl analyze` 默认可读入口小于 raw。
- 关键业务 flow 能判断 success / failed / skipped。
- 没有 Validation artifact 时仍不得 `passed`。

## 验证命令

文档和静态盘点阶段：

```bash
python3 Workspace/SDD/sdd.py validate SDD-0040
python3 Workspace/SDD/sdd.py validate --all
git diff --check -- SDD/project/projects/PRJ-0002-ecs-framework-refactor DocsAI/ECS/Tools/Logger
```

实现阶段再加：

```bash
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
node --check Workspace/Tools/logctl/logctl.mjs
node --test Workspace/Tools/logctl/tests/logctl-analyze.test.mjs
Workspace/Tools/logctl/logctl analyze --run-dir <new-run> --out <new-run>/analysis
```

涉及 `.ai-config/skills/` 时必须只改 `.ai-config` 源，并运行：

```bash
bash Workspace/Tools/ai-config-sync/sync-ai-config.sh
bash Workspace/SystemAgent/Tools/skill-test/lint.sh static all --no-fail --summary-only
```

## 禁止

- 不再说“Log 已全部改完”，除非 T3.4 新 run 验收通过。
- 不用 analyzer DONE 证明 live stdout 已经 AI-first。
- 不把旧样本压缩比例当成新源码调用点迁移完成证明。
- 不在方向未冻结时大规模改 `Src/ECS`。
- 不把普通 `_log.Info` 机械替换为 `BeginTrace`。
