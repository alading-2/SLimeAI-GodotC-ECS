# Logger

> 状态：current
> 定位：ECS Tools/Logger owner 入口。
> 更新：2026-06-09

## 一句话定位

当前运行时代码 `Src/ECS/Tools/Logger/Log.cs` 仍是 legacy 文本日志封装；新的 AI-first Log / Observation 设计事实源在 `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Tool/10.Log/`。

## 当前实现事实

当前 `Log.cs`：

- 使用 `Trace / Debug / Info / Success / Warning / Error / None`。
- 支持 `GlobalLevel` 和 context 级过滤。
- 使用 `GD.PrintRich` 输出 BBCode 文本。
- `Warn` / `Error` 会同步 `GD.PushWarning` / `GD.PushError`。
- 默认墙钟时间显示，不提供 `runElapsedMs / frame / physicsFrame`。

这只是当前实现，不是后续推荐架构。

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

后续 Log hard cutover 应从 PRJ-0002 Log 设计包进入：

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
| 运行控制 | profile、sink、owner/context/operation 开关、override snapshot。 |
| 离线分析 | `analyze --run-dir` 生成 `analysis/raw/by-owner/by-phase/flows/failures/noise/missing-fields/ai-context.md`。 |
| 二次查询 | `query --analysis-dir` 或 `query --file` 按 owner、sourceFile、operation、entityId、severity 过滤。 |

Godot scene runner 只负责运行场景、保存 run dir 和调用 Log CLI；不要在 godot-scene-test skill 中长期维护日志拆分规则。

用户手动运行游戏时，不应复制整段 console 给 AI。应保留 `SLIMEAI_LOG_RUN_DIR` 下的 JSONL / artifact，然后执行 `logctl analyze`；只有旧日志文本才用 `logctl ingest --stdin --source legacy-stdout` 降级处理。

## 阅读顺序

1. 读本 README，确认当前实现和目标设计边界。
2. 读 `Concept.md` / `Usage.md`，只了解 legacy API。
3. 做 Log 重构、测试日志统一或 runner 分析时，转到 PRJ-0002 `design/Tool/10.Log/README.md`。

## 禁止事项

- 不把 `GD.Print("PASS")`、`GD.PushError("FAIL")`、`[PASS]`、`[FAIL]` 当作长期测试事实源。
- 不只靠提高 `GlobalLevel` 降噪。
- 不把全量 stdout 直接交给 AI 作为默认分析流程。
- 不在没有 owner Log 文档或 README `## Log` 的情况下新增高频日志。
