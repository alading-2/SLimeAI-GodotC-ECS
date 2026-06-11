# 测试统一与 Observation 接入

> 更新：2026-06-11
> 状态：historical design note；Validation/artifact 裁决仍可参考，analyzer 默认目录以项目级第二部分 `03-最终设计与完成清单.md` 为准
> 提醒：本文是 SDD-0040 初始快照。旧 `by-owner` / `by-phase` raw 分桶和 pretty `flows.json` 不再是默认 analyzer 产物。

## 1. 当前问题

现在测试里常见几种结果表达混在一起：

- `GD.Print("PASS")`
- `GD.PushError("FAIL")`
- `_log.Success("[PASS]")`
- `_log.Error("[FAIL]")`
- `throw new Exception(...)`

这会让 runner 只能靠 pattern 猜结果。

Godot 的 `GD.PushError` / `GD.PushWarning` 是输出到 debugger 和终端的错误/警告 API，不是测试框架里的断言结果模型。当前把 `Fail()` 直接绑定到 `GD.PushError`，会让受控负向测试、真实运行错误和 artifact 写入失败混在一起。

## 2. 统一裁决

测试结果必须统一进入同一套观测语义：

- `Check`
- `Pass`
- `Fail`
- `Artifact`
- `ExpectedFailure`
- `FailureReason`

console 只是显示层，不能作为唯一事实源。

AI-first 默认显示层也不再走 Godot rich/editor API：

- 详细测试事实写入 `jsonl-buffered-file` 和 Validation artifact。
- runner 快速摘要写入 C# `stdout-summary`。
- `GD.Print` / `GD.PushError` / `GD.PushWarning` 只允许作为 `GodotEditorSink` 或过渡 stdout marker fallback，不再是 pass/fail 主事实源。

`Pass` / `Fail` 不再是通用 `LogLevel`。它们属于 `ValidationStatus`：

```text
ValidationStatus = Pass | Fail | Skip | ExpectedFailure
```

## 3. 统一形态

建议每个 test scene / validation session 采用：

```text
Test start
  -> emit structured info
  -> run checks
  -> log pass/fail entries
  -> write artifact
  -> exit code reflects final verdict
```

更完整的目标形态：

```text
ValidationSession.Start(scene, owner, phase=Validation)
  -> Check(name, category, expected, actual, reasonCode)
  -> LogEntry(channel=Validation, validationStatus=Pass/Fail)
  -> OperationTrace(summary)
  -> WriteArtifact(status, checks, failures, logs, expectedInputs, expectedObservations, passCriteria, failCriteria)
  -> SceneTree.Quit(status)
```

## 4. Validation 形态

建议保留一个轻量 `ValidationSession`：

- 内部使用新的 Log 结构化 API。
- 记录 `checks`、`failureReasons`、`logs`。
- 最终写出 artifact JSON。
- artifact 中要有 `expectedInputs / expectedObservations / passCriteria / failCriteria`。
- 支持 `ExpectedFailure`，避免负向测试污染 runner 失败判断。
- 支持 `ControlledError`，明确某个 Godot error 是测试输入还是测试失败。
- 支持 `owner` / `operation` / `phase` / `correlationId`，把测试和业务 flow 关联。

旧 AiFirst Observation 原型可采纳：

| 原型 | 可采纳 | 需要升级 |
| --- | --- | --- |
| `GameOSObservationSession` | 从 runner 环境读取 run/artifact/log 目录，并挂载 JSONL sink | 增加 run elapsed、frame、phase、profile metadata。 |
| `GameOSLogEntry` | `context + message + values` 的结构化雏形 | 增加 owner、channel、operation、correlation、budget、severity/outcome 拆分。 |
| `SceneValidationSession` | check、failureReasons、artifact、memory sink | 把 `Pass/Fail` 从 log level 拆到 validation status，补 expected/actual/reasonCode。 |

## 5. Runner 与 Log CLI 接入

scene runner 的职责应保持很薄：

- 注入环境变量。
- 收集 JSONL / artifact / stdout。
- 判断 exit code。
- 生成最小 run metadata / gate report。
- 调用 `logctl analyze` 生成分析目录。

Log CLI 的职责是：

- 根据 artifact 和 structured logs 判断 pass/fail 的事实来源。
- 调用 `logctl analyze` 把 raw log 提炼成 `summary.md`、`ai-context.md`、`flows/flows.jsonl`、`noise/templates.jsonl`、`failures`、`noise`、`missing-fields` 和 `raw/entries.jsonl`；不默认维护 `by-owner` / `by-phase` raw 复制分桶。
- 生成 AI 分析入口 `ai-context.md`。
- 支持 `logctl query` 对已整理 run 做二次筛选。

runner 不应该再依赖散乱的 `PASS` / `FAIL` 文本作为主判断。

过渡期可以保留字符串 pattern 作为 fallback，但必须在 gate report 标记：

```text
resultSource = artifact | structured-log | stdout-pattern-fallback
```

只要 `stdout-pattern-fallback` 出现，就说明对应测试还没完成 Log/Validation 迁移。

sink 规则：

- runner 默认读取 artifact / JSONL 判定结果。
- stdout 只承载 `[VALIDATION:<status>]` 摘要、关键 warn/error 和 flow summary。
- Godot editor sink 默认关闭；只有人工 editor 调试 profile 能打开，且必须写入 run metadata。

边界裁决：**日志整理和分析不属于 `godot-scene-test` skill 的长期职责；测试 skill 只负责运行场景、保存 run dir、调用 `logctl analyze/query`，不自己维护业务日志拆分规则。**

## 6. 测试 helper 迁移

优先迁移这些地方：

- `Src/ECS/Tools/Logger/Tests/LogTest.cs`
- `Src/ECS/Runtime/Data/Tests/DataOS/*`
- `Src/ECS/Runtime/System/Tests/SystemCore/*`
- `Src/ECS/Test/SingleTest/ECS/System/AbilitySystemTest/*`
- `Src/ECS/Test/SingleTest/ECS/System/DamageSystemTest/*`
- `Src/ECS/Test/SingleTest/ECS/System/Movement/*`
- `Src/ECS/Test/GlobalTest/MainTest/MainTest.cs`

这些地方最容易把日志和测试结果混成一团。

### 6.1 当前已发现的分裂点

| 文件 | 当前问题 | 目标 |
| --- | --- | --- |
| `Src/ECS/Runtime/Data/Tests/DataOS/DataSceneTestBase.cs` | `GD.Print("PASS ...")` / `GD.PushError("FAIL ...")` | 迁到 `ValidationSession.Check`，artifact 记录 expected/actual。 |
| `Src/ECS/Runtime/System/Tests/SystemCore/SystemCoreRuntimeTest.cs` | `_log.Info("[PASS]")` / `_log.Error("[FAIL]")` | PASS/FAIL 改 `ValidationStatus`，日志只承载事实。 |
| `Src/ECS/Tools/Timer/Tests/TimerStressValidation.cs` | 已写 artifact，但仍用 `GD.Print("PASS ...")` / `GD.PushError("FAIL ...")` | artifact 作为主事实源，stdout marker 降为 fallback。 |
| `Src/ECS/Tools/ObjectPool/Tests/Validation/CollisionIsolation/ObjectPoolCollisionIsolationValidation.cs` | 已有 checks/artifact，但 PASS/FAIL marker 仍是 stdout 主线 | 迁入统一 Validation helper，保留 collision flow summaries。 |
| `.ai-config/skills/godot/godot-scene-test/scripts/godot-scene-runner.mjs` | `FAILURE_PATTERNS` 字符串扫描 | runner 只保留 gate fallback；结构化判断和分析交给 `logctl analyze`。 |

## 7. 结果约束

统一后应该达到：

- `PASS` / `FAIL` 是结构化事实。
- console 只显示摘要和关键错误。
- JSONL 可以回放。
- artifact 可以给 AI 做后续分析。
- `Fail()` 不再等于 `GD.PushError()`；是否输出 Godot error 由 profile/sink 决定。
- 负向测试可以表达“运行中发生受控错误，但测试通过”。
- runner 报告每个结果来自 artifact、structured log 还是 stdout fallback。

## 8. 与 Test/TDD 的边界

Log、Debug、Test/TDD 是一组系统，但职责不能混：

| 概念 | 职责 |
| --- | --- |
| Log | 记录结构化事实和过程。 |
| Debug | 临时打开更细粒度事实、定位异常、生成下一轮 profile 建议。 |
| Test/TDD | 定义预期行为、断言 pass/fail、决定退出码。 |
| Validation artifact | 保存测试事实、检查项、失败原因和可恢复证据。 |
| AI analysis | 读取 analyzer digest 和 owner `Log.md`，判断下一步修复/降噪。 |

TODO：后续执行型 SDD 需要把 `TestSystem`、`Src/ECS/**/Tests`、scene-gate 和 runner 的关系统一成一个测试/观察面规范。本设计阶段先把 TODO 写入 Log 包，不直接重构全部测试系统。
