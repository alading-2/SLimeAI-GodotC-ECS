# Timer 工具入口

> 状态：current
> 定位：TimerManager / TimerScheduler owner 文档入口。
> 更新：2026-06-09

## 入口

| 文档 | 说明 |
| --- | --- |
| [Concept.md](Concept.md) | TimerManager、TimerScheduler、TimerHandle、TimerOptions 和 diagnostics 边界 |
| [Usage.md](Usage.md) | gameplay timer、取消点、clock 选择和 diagnostics 使用 |

## 源码

```text
Src/ECS/Tools/Timer/
```

## Log

Timer owner 使用 `owner=Timer`。当前不在每次 `_Process` / `TimerScheduler.Tick` 写逐帧 trace；Timer 是高频基础工具，默认只在显式 diagnostics 和导出动作写 summary。

| operation | phase | 关键字段 |
| --- | --- | --- |
| `TimerDiagnosticsSummary` | `Diagnostics` | `activeCount`、`dispatchQueueCount`、`perFrameUpdateCount`、`entriesCount` |
| `TimerDiagnosticsDump` | `Diagnostics` | 同 summary，message 包含格式化 dump |
| `ExportTimerDiagnosticsJson` | `Diagnostics` | `path`、`activeCount`、`dispatchQueueCount`、`perFrameUpdateCount`、`entriesCount`、失败时 `exception` |

预算规则：普通 tick 不打日志；需要排查 timer 泄漏、owner/purpose 分布或派发队列时，先调用 `GetTimerDiagnostics(...)` 或 `ExportTimerDiagnosticsJson(...)`，再用 `logctl` 查询 `owner=Timer`。

```bash
Workspace/Tools/logctl/logctl query --analysis-dir <run>/analysis owner=Timer operation=ExportTimerDiagnosticsJson
```
