# Runtime System 文档入口

> 状态：current
> 更新：2026-06-03
> 范围：`Src/ECS/Runtime/System/`

## 入口

| 文档 | 说明 |
| --- | --- |
| [Concept.md](Concept.md) | System Core 概念、职责边界和依赖 |
| [Usage.md](Usage.md) | SystemRegistry / SystemManager / DataOS system config 使用说明 |
| [SystemManifest.md](SystemManifest.md) | AI 可读系统清单，覆盖 owner、源码、config、run condition、command handler、测试和风险 |
| [Concepts/系统与状态分层总览.md](Concepts/系统与状态分层总览.md) | 系统治理、项目状态、实体状态和 AI 决策分层 |

## 当前执行设计

Runtime System 的 AI-first 优化不重写 `SystemManager` 生命周期，当前执行入口为：

- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/8.System优化/README.md`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/sdds/019-SDD-0029-system-contract-manifest-and-diagnostics-hardening/README.md`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/sdds/019-SDD-0029-system-contract-manifest-and-diagnostics-hardening/execution-prompt.md`

SDD-0029 的首切片只做 SystemManifest、SystemPreflight、SystemDiagnosticsSnapshot、SystemLifecycleTrace、SystemCore artifact 和 DocsAI 同步；不做 typed `SystemId` hard cutover。

## AI-first Contract

| 合同 | 源码 / Artifact | 职责 |
| --- | --- | --- |
| `SystemPreflight` | `Src/ECS/Runtime/System/Preflight/` | 启动/验证前检查 config、registry、preset、dependencies、cycle 和 descriptor-only allow-list。 |
| `SystemDiagnosticsSnapshot` | `Src/ECS/Runtime/System/Diagnostics/` | 合并 DataOS config、SystemRegistry descriptor、SystemManager runtime 和 ProjectState。 |
| `SystemLifecycleTrace` | `Src/ECS/Runtime/System/Diagnostics/SystemLifecycleTrace.cs` | 记录 bootstrap、add/remove、enable/disable、state gate 和 command events 的轻量 ring buffer。 |
| `blockedReasonCode` | `SystemBlockedReasonCode` | 稳定机器原因码；中文 `blockedReason` 只作 UI / 日志说明。 |
| SystemCore artifact | `.ai-temp/scene-tests/artifacts/system-core-diagnostics.json` | Godot 场景验证输出，包含标准答案字段和 diagnostics snapshot。 |

## Log

System owner 使用 `owner=System`。当前第一批 flow 已覆盖：

| operation | phase | outcome | 关键字段 |
| --- | --- | --- | --- |
| `SystemPreflight` | `Preflight` | `Completed` / `Failed` | `configCount`、`registeredDescriptorCount`、`activePresetName`、`errorCount`、`warningCount` |
| `SystemDiagnosticsSnapshot` | `Diagnostics` | `Completed` | `configCount`、`registeredDescriptorCount`、`loadedCount`、`runningCount`、`blockedCount`、`disabledCount`、`preflightErrorCount`、`preflightWarningCount` |

规则：

- 机器判断用 `SystemBlockedReasonCode`、`SystemPreflightIssue.RuleId` 和 `Severity`；中文 `blockedReason` 只作 UI / 日志说明。
- `SystemLifecycleTrace` 仍是 ring buffer；不默认长期写文件。需要场景证据时 dump 到 validation artifact。
- `SystemPreflight` 只读 config / registry / preset；日志也只记录检查结果，不写回配置。

```bash
Workspace/Tools/logctl/logctl query --analysis-dir <run>/analysis owner=System operation=SystemPreflight
```

## 源码

```text
Src/ECS/Runtime/System/
  Config/
  Diagnostics/
  Lifecycle/
  Preflight/
  State/
  Tests/SystemCore/
```

Runtime System 只管理注册、生命周期、状态门禁和系统配置。具体 Ability、Damage、Movement、StatusSystem、TestSystem 等行为系统归 `Src/ECS/Capabilities/<owner>/System/`。

## 文档同步要求

修改 Runtime System 实现、接口、生命周期、配置、preflight、diagnostics 或验证方式时，必须同步：

- 本 `README.md`
- [Usage.md](Usage.md)
- `SystemManifest.md`（SDD-0029 生成后）
- 必要时同步 [Concept.md](Concept.md) 和 `Concepts/` 下 current / history 标注

不要只改源码和测试；DocsAI 是 AI-first 入口事实源。
