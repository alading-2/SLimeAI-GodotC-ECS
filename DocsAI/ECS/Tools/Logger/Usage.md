<!-- migrated-from: Src/ECS/Tools/Logger/README.md -->

> 迁移来源：`Src/ECS/Tools/Logger/README.md`
> 迁移说明：本文主体从原 `Src/ECS` 文档迁入 `DocsAI` 统一管理；原 `Src/ECS` Markdown 文件已删除。

# Log 日志系统

> 当前文档描述 structured API。旧 `LogLevel` / `Log.Success(...)` 只作为兼容入口。

## 概述

**路径**: `Src/ECS/Tools/Logger/Log.cs`

**核心价值**:
- 统一的 structured observation envelope
- 默认 stdout summary + buffered JSONL + memory + artifact sinks
- `severity / outcome / validationStatus` 拆分
- `OperationTrace` 和 `ValidationSession` 主事实源

## 快速开始

```csharp
// 推荐用法：每个类声明一个静态实例
private static readonly Log _log = new Log("ClassName");

public void MyMethod()
{
    _log.Info(
        "普通信息",
        outcome: LogOutcome.Completed,
        fields: new LogFields { ["entityId"] = entityId.Value },
        operation: "MyOperation");

    _log.Warn(
        "警告",
        fields: new LogFields { ["reasonCode"] = "MY-WARN-001" },
        operation: "MyOperation");
}
```

长过程使用 `OperationTrace`：

```csharp
using var trace = Log.BeginTrace("Damage", nameof(DamageService), "DamageProcess", "Combat");
trace.Complete(LogOutcome.Completed, "DamageProcess completed", new LogFields
{
    ["appliedDamage"] = result.AppliedDamage,
    ["actualDamage"] = result.ActualDamage
});
```

验证场景使用 `ValidationSession`，不要输出 `[PASS]` / `[FAIL]`：

```csharp
var session = ValidationSession.Start("owner-smoke", "OwnerSmoke", new ValidationSessionOptions
{
    ExpectedInputs = new[] { "fixture" },
    ExpectedObservations = new[] { "structured checks" },
    PassCriteria = new[] { "all checks pass" },
    FailCriteria = new[] { "any required check fails" },
    ArtifactPath = ".ai-temp/scene-tests/artifacts/owner-smoke.json"
});

session.Check("check-id", "检查说明", actual, expected, passed);
session.Complete();
```

## 全局配置

```csharp
// 发布版本建议 Info 或更高
Log.GlobalLevel = LogLevel.Info;

// 针对特定类调试
Log.SetLevel("ClassName", LogLevel.Debug);
```

默认配置文件：

```text
Config/Log/log.profile.json
Config/Log/log.rules.json
Config/Log/log.overrides.json
```

默认运行时会读取这些文件并写入：

```text
<runDir>/metadata/log-profile.json
```

常用环境变量：

```bash
SLIMEAI_LOG_RUN_DIR=.ai-temp/log-runs/manual/20260609
SLIMEAI_LOG_PROFILE=ai-default
SLIMEAI_LOG_OVERRIDES=Config/Log/log.overrides.json
```

代码中需要显式加载其他配置目录时：

```csharp
var options = Log.LoadOptionsFromConfig("Config/Log", ".ai-temp/log-runs/manual");
Log.Configure(options);
```

预算规则只限制日志输出，不限制游戏逻辑执行。重复日志超过 profile 或 rule 的 `budgetPerSecond` 后，会写 `operation=SuppressedSummary`，并保留 `budgetKey / suppressedCount / budgetPerSecond` 字段。

现实时间戳默认不写入每条 JSONL。需要跨进程或跨 artifact 对齐时，才在 profile 或 override 中显式开启：

```json
{
  "includeWallClockUtc": true
}
```

开启后字段名是 `wallClockUtc`；不要恢复旧 `timestampUtc`。

Godot editor sink 默认关闭。只有人工 editor debug 才显式配置：

```csharp
Log.Configure(new LogOptions
{
    RunDirectory = ".ai-temp/log-runs/manual",
    EnableGodotEditorSink = true
});
```

## logctl

```bash
Workspace/Tools/logctl/logctl profile show --config-dir Config/Log
Workspace/Tools/logctl/logctl analyze --run-dir .ai-temp/log-runs/manual --out .ai-temp/log-runs/manual/analysis
Workspace/Tools/logctl/logctl query --analysis-dir .ai-temp/log-runs/manual/analysis owner=Damage operation=DamageProcess
Workspace/Tools/logctl/logctl query --file .ai-temp/log-runs/manual/analysis/raw/entries.jsonl sourceFile=Src/ECS/Capabilities/Damage/Services/DamageService.cs
Workspace/Tools/logctl/logctl ingest --stdin --source legacy-stdout --out .ai-temp/log-ingest/manual
Workspace/Tools/logctl/logctl suggest --run-dir .ai-temp/log-runs/manual --dry-run
```

`query --analysis-dir` 只筛选语义索引，也就是 `flows/flows.jsonl` 和 `noise/templates.jsonl`；语义索引为空时返回空结果，不会自动回退到 raw。只有需要原始 entry 下钻时，才显式 `--file analysis/raw/entries.jsonl`。

## 迁移提醒

- 不要在新测试中继续新增裸 `GD.Print("PASS")` / `GD.PushError("FAIL")` 作为主事实源。
- 不要把 `Success` 当作长期 severity 设计；后续目标是 `severity / outcome / validationStatus` 拆分。
- 高频 owner 新增日志前，应先补 owner `Log.md` 或 README `## Log`。
- AI-first 默认不使用 Godot API 打印详细日志；详细事实写 buffered JSONL，stdout 只写 summary。
- 不要把高密度详细日志简单改成每条 `Console.WriteLine`，否则只是把 Godot Output 噪声换成 stdout 噪声。
- 用户已确认：后续实现以 C# structured sink 为默认主链路，Godot editor sink 默认关闭。
