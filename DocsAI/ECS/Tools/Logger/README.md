# Logger

> 状态：current
> 定位：ECS Tools/Logger owner 入口。
> 更新：2026-06-10

## 一句话定位

`Src/ECS/Tools/Logger/Log.cs` 是 AI-first Observation 入口。默认详细事实写 buffered JSONL，默认可见输出写 stdout summary，Validation 写 artifact；Godot editor 输出只作为可选 sink。

## 当前实现事实

当前 `Log.cs`：

- `LogEntry` envelope 包含 `runElapsedMs / frame / physicsFrame / severity / outcome / validationStatus / channel / owner / context / operation / phase / fields`。
- `LogSeverity` 只表达运行健康度；`LogOutcome` 表达流程结果；`LogValidationStatus` 表达测试断言结果。
- `OperationTrace` 用于长过程，start/step/complete 进入 Flow channel；stdout 只展示完成摘要，完整字段写 JSONL。
- 默认 sinks 是 `StdoutSummarySink`、`JsonlBufferedFileSink`、`MemorySink`、`ArtifactSink`。
- `GodotEditorSink` 默认关闭；只有显式启用时才调用 `GD.PrintRich` / `GD.PushWarning` / `GD.PushError`。
- 默认 profile 事实源是 `Config/Log/log.profile.json`、`Config/Log/log.rules.json`、`Config/Log/log.overrides.json`；运行时会写 `<runDir>/metadata/log-profile.json`。
- budget 会按 `budgetKey` 或 `channel/owner/context/operation/reason/entity` 每秒压制重复日志，并写 `operation=SuppressedSummary` 摘要。
- legacy `LogLevel.Success` 和 `Log.Success(...)` 仍作为兼容入口存在，但映射为 `severity=Info` + `outcome=Succeeded`，不要把 `Success` 当新 severity 使用。

日志默认 run dir 来自 `SLIMEAI_LOG_RUN_DIR`；未设置时写入 `.ai-temp/log-runs/<timestamp>/`。`SLIMEAI_LOG_PROFILE` 可选择 profile 名称，`SLIMEAI_LOG_OVERRIDES` 可指定临时 override 文件。

## 2026-06-10 样本复查

`.ai-temp/log-runs/20260610-013907/raw/scene-log.jsonl` 证明：JSONL 是必要基础，但不是 AI 分析入口本身。

当前问题集中在：

- raw JSONL 约 4914 行，AI 不能默认直接读 raw。
- `TargetSelector/TargetQueryEngine/TargetQueryEntities` 单项约 3041 条，说明 owner/operation 预算和 flow summary 仍要补强。
- 约 1109 条 `fields:{}`，约 1109 条 `operation == context`，说明很多日志还缺业务字段和稳定 operation。
- 当前 `logctl analyze` 已能输出 `by-owner` / `by-phase` / `noise` / `missing-fields`，但 `ai-context.md` 仍太薄，缺 `summary.md`、top noise、missing-fields markdown digest、flow digest 和 owner 文档链接。

后续 Log 分析必须遵循：

```text
raw/scene-log.jsonl
  -> logctl analyze
  -> analysis/summary.md + ai-context.md + noise + missing-fields + flows
  -> AI 按 owner Log.md 分析
  -> 证据不足时才 query/raw
```

完整设计入口：

```text
SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Tool/10.Log/07-当前样本日志问题与整理方案.md
```

## Sink 裁决

AI-first 目标不再默认使用 Godot API 打印：

- 默认详细事实写入 C# buffered JSONL file。
- 默认可见输出使用 C# stdout summary。
- Validation 使用 memory / artifact sink。
- `GD.PrintRich` / `GD.PushWarning` / `GD.PushError` 只作为可选 `GodotEditorSink`，默认关闭。

原因：Godot rich print 的价值是 editor Output 面板和 BBCode 颜色；AI 分析需要结构化 JSONL、artifact 和稳定 stdout 摘要。

2026-06-09 用户已确认该分析方向。后续实现不要把它简化成“全部换成 `Console.WriteLine`”：

- `Console.WriteLine` 只适合 stdout summary。
- 详细事实必须走 C# buffered JSONL / artifact。
- Godot API sink 只作为人工 editor debug 选项。

## AI-first 目标方向

Log hard cutover 设计包：

```text
SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Tool/10.Log/README.md
```

目标方向：

- `LogEntry` 结构化 envelope。
- `severity / outcome / validationStatus` 拆分。
- `runElapsedMs / frame / physicsFrame / phase` 默认时间语义。
- `OperationTrace` / flow summary 聚合长过程。
- `Config/Log` profile + CLI override。
- C# stdout summary + buffered JSONL file 作为默认 sink。
- Validation artifact 作为测试主事实源。
- `logctl analyze` 先拆分日志目录，再给 AI 分析。
- `logctl query` 对已整理 run 做二次筛选，例如 owner / sourceFile / operation / entityId / severity。
- 每个 owner 写 `Log.md` 或 README `## Log`，固定怎么打、怎么分析、哪些默认关闭。

## CLI 边界

`logctl` 不只是运行时开关，也负责已产生日志的整理和查询：

| 类别 | 作用 |
| --- | --- |
| 运行控制 | `profile show` 查看 `Config/Log` 有效 profile；后续扩展 sink、owner/context/operation 开关和 override snapshot。 |
| 离线分析 | `analyze --run-dir` 生成 `analysis/raw/by-owner/by-phase/flows/failures/noise/missing-fields/summary.md/ai-context.md`。 |
| 二次查询 | `query --analysis-dir` 或 `query --file` 按 owner、sourceFile、operation、entityId、severity 过滤。 |
| 建议 | `suggest --run-dir --dry-run` 输出 noisy context 和可审查 `profilePatch`，不直接静默改配置。 |

Godot scene runner 只负责运行场景、保存 run dir 和调用 Log CLI；不要在 godot-scene-test skill 中长期维护日志拆分规则。

用户手动运行游戏时，不应复制整段 console 给 AI。应保留 `SLIMEAI_LOG_RUN_DIR` 下的 JSONL / artifact，然后执行 `logctl analyze`；只有旧日志文本才用 `logctl ingest --stdin --source legacy-stdout` 降级处理。

如果某次分析目录缺 `summary.md` 或 `ai-context.md` 信息不足，应把结论分类为 `Log CLI issue` 或 `Log gap`，不要退回“把整份 raw JSONL 交给 AI 猜”。

## Log

Logger owner 自身使用 `owner=Tools.Logger` 或调用方显式 owner。新增日志调用点必须先判断它属于哪一类事实：

| 事实类型 | 写法 |
| --- | --- |
| 运行流程 | `using var trace = Log.BeginTrace(owner, context, operation, phase);`，完成时写 `outcome` 和关键 `fields`。 |
| 验证断言 | 使用 `ValidationSession.Check/Skip/Complete`，不要写 `[PASS]` / `[FAIL]`。 |
| 可见摘要 | 让 `StdoutSummarySink` 输出 warn/error、validation、flow completion；不要手工 `Console.WriteLine` 长日志。 |
| 人工 editor debug | 显式启用 `EnableGodotEditorSink=true`，默认保持关闭。 |

查询示例：

```bash
Workspace/Tools/logctl/logctl profile show --config-dir Config/Log
Workspace/Tools/logctl/logctl analyze --run-dir .ai-temp/scene-tests/runs/latest --out .ai-temp/scene-tests/runs/latest/analysis
Workspace/Tools/logctl/logctl query --analysis-dir .ai-temp/scene-tests/runs/latest/analysis owner=Ability operation=AbilityTryTrigger
Workspace/Tools/logctl/logctl query --analysis-dir .ai-temp/scene-tests/runs/latest/analysis severity>=Warn
Workspace/Tools/logctl/logctl suggest --run-dir .ai-temp/scene-tests/runs/latest --dry-run
```

预算规则：高频路径不要逐步 trace；只在操作完成、失败、跳过或显式 diagnostics 时写 summary，并用 `fields["budgetKey"]` 标记可聚合预算。

## 阅读顺序

1. 读本 README，确认当前实现和目标设计边界。
2. 读 `Usage.md`，确认 structured API、ValidationSession 和 logctl 使用方式。
3. 做 Log 重构、测试日志统一或 runner 分析时，转到 PRJ-0002 `design/Tool/10.Log/README.md`。

## 禁止事项

- 不把 `GD.Print("PASS")`、`GD.PushError("FAIL")`、`[PASS]`、`[FAIL]` 当作长期测试事实源。
- 不只靠提高 `GlobalLevel` 降噪。
- 不把全量 stdout 直接交给 AI 作为默认分析流程。
- 不在没有 owner Log 文档或 README `## Log` 的情况下新增高频日志。
