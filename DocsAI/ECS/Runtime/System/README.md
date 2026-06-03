# Runtime System 文档入口

> 状态：current
> 更新：2026-06-03
> 范围：`Src/ECS/Runtime/System/`

## 入口

| 文档 | 说明 |
| --- | --- |
| [Concept.md](Concept.md) | System Core 概念、职责边界和依赖 |
| [Usage.md](Usage.md) | SystemRegistry / SystemManager / DataOS system config 使用说明 |
| [Concepts/系统与状态分层总览.md](Concepts/系统与状态分层总览.md) | 系统治理、项目状态、实体状态和 AI 决策分层 |
| `SystemManifest.md` | 待 SDD-0029 生成：AI 可读系统清单，覆盖 owner、源码、config、run condition、command handler、测试和风险 |

## 当前执行设计

Runtime System 的 AI-first 优化不重写 `SystemManager` 生命周期，当前执行入口为：

- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/8.System优化/README.md`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/sdds/019-SDD-0029-system-contract-manifest-and-diagnostics-hardening/README.md`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/sdds/019-SDD-0029-system-contract-manifest-and-diagnostics-hardening/execution-prompt.md`

SDD-0029 的首切片只做 SystemManifest、SystemPreflight、SystemDiagnosticsSnapshot、SystemLifecycleTrace、SystemCore artifact 和 DocsAI 同步；不做 typed `SystemId` hard cutover。

## 源码

```text
Src/ECS/Runtime/System/
```

Runtime System 只管理注册、生命周期、状态门禁和系统配置。具体 Ability、Damage、Movement、StatusSystem、TestSystem 等行为系统归 `Src/ECS/Capabilities/<owner>/System/`。

## 文档同步要求

修改 Runtime System 实现、接口、生命周期、配置、preflight、diagnostics 或验证方式时，必须同步：

- 本 `README.md`
- [Usage.md](Usage.md)
- `SystemManifest.md`（SDD-0029 生成后）
- 必要时同步 [Concept.md](Concept.md) 和 `Concepts/` 下 current / history 标注

不要只改源码和测试；DocsAI 是 AI-first 入口事实源。
