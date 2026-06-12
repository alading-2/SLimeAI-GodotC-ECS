# Logger 概念

> status: current
> sourcePaths: Src/ECS/Tools/Logger/
> relatedDocs: DocsAI/ECS/Tools/Logger/README.md, DocsAI/ECS/Tools/Logger/Usage.md, SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Tool/10.Log/README.md
> lastReviewed: 2026-06-09

## 1. 一句话定位

当前 Log 是 AI-first Observation 入口。它保留 legacy `LogLevel` API 兼容旧调用点，但默认事实链路已经切到 structured `LogEntry`、stdout summary、buffered JSONL、memory 和 artifact sink。

它负责 evidence plane，不负责 SystemAgent 的 workflow / actor / gate / retrospective；Debug workflow 如何消费这些证据，见 `Workspace/SystemAgent/Docs/10-Debug工作流与证据链.md`。

2026-06-09 sink 裁决：Godot rich/editor 输出只保留为人工调试 sink；AI-first 默认详细事实写 C# buffered JSONL file，runner 可见摘要写 C# stdout summary，Validation 事实写 memory/artifact。

用户已确认：C# 输出链路更适合 AI-first 默认主链路，但不是逐条 `Console.WriteLine` 替代 `GD.PrintRich`；正确方案是 C# structured sink，也就是 buffered JSONL + stdout summary + artifact。

默认控制面已经有稳定文件事实源：`Config/Log/log.profile.json`、`Config/Log/log.rules.json`、`Config/Log/log.overrides.json`。运行时会将有效 profile、sink、budget、rule 和 warning 写入 `<runDir>/metadata/log-profile.json`，供 runner / AI 恢复现场。

## 2. 核心概念

### 严重程度、结果和验证状态

`LogSeverity` 只表达运行健康度：`Trace / Debug / Info / Warn / Error / Fatal / None`。

`LogOutcome` 表达流程结果：`Started / Completed / Succeeded / Failed / Skipped / Suppressed`。

`LogValidationStatus` 表达断言结果：`Pass / Fail / Skip / ExpectedFailure`。

legacy `LogLevel.Success` 只映射为 `severity=Info` + `outcome=Succeeded`。

### 推荐模式

每个类一个静态实例：

```csharp
private static readonly Log _log = new(nameof(MyClass));
_log.Info("消息");
_log.Error("错误");

using var trace = Log.BeginTrace("Ability", nameof(AbilitySystem), "AbilityTryTrigger", "Ability");
trace.Complete(LogOutcome.Succeeded, "AbilityTryTrigger completed", new LogFields
{
    ["abilityName"] = abilityName
});
```

### Release 编译

Trace/Debug 级别在 Release 编译时被条件编译排除。

### Budget

Budget 限制日志输出量，不限制业务代码执行。规则按 `budgetKey` 优先聚合；没有 `budgetKey` 时按 `channel/owner/context/operation/reason/entity` 聚合。超出每秒预算后，普通重复日志会被压制，并写 `operation=SuppressedSummary` 摘要，包含 `budgetKey / suppressedCount / budgetPerSecond / windowMs`。

## 3. 职责边界

| Logger 做 | Logger 不做 |
| ---- | ---- |
| 统一 structured envelope | 代替 owner diagnostics 设计 |
| stdout summary / JSONL / artifact sinks | 把全量 stdout 交给 AI |
| `OperationTrace` flow summary | 在高频 tick 中逐帧无预算 trace |
| optional Godot editor sink | 默认依赖 `GD.PrintRich` 判断验证结果 |

## 4. 当前边界

- 本文描述 current structured Log；历史 legacy API 仅作为兼容边界。
- 不建议继续只扩展颜色、墙钟时间或 `GlobalLevel`。
- 测试 PASS/FAIL、runner analyzer、owner `Log.md` 和结构化 Observation 规则以本 README 和 PRJ-0002 Log 设计包为准。
- 不要把 legacy `GD.PrintRich` 方案迁移成“每条日志改用 `Console.WriteLine`”；高密度详细日志应 buffered 写 JSONL，stdout 只输出摘要。
