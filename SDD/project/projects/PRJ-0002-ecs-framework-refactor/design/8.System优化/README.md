# System 优化设计包

> 状态：current
> 更新：2026-06-03
> 范围：`DocsAI/ECS/Runtime/System/`、`Src/ECS/Runtime/System/`、`Data/DataOS/Snapshots/runtime_snapshot.json` 的 `system.config` / `system.preset`
> 结论：保留现有 System Core 主体，不做整体重写；围绕 AI-first 补齐 manifest、preflight、diagnostics、验证 artifact 和文档门禁。

## 一句话结论

旧 System 系统的解耦方向是对的：代码只注册 `SystemId + Factory`，DataOS snapshot 负责系统配置，`SystemManager` 统一处理生命周期、项目状态门禁和命令执行。

AI-first 的缺口不在“要不要推翻 System”，而在：

- AI 能否一次看清所有系统的 owner、配置、注册、运行条件、依赖、命令和测试入口。
- 系统启动失败、被状态挡住、命令被拒绝时，能否输出稳定、可复盘、可机器检查的原因。
- DataOS `system.config`、代码 `SystemRegistry` 和运行时 `SystemManager` 三者能否在执行前被 preflight 检查对齐。

## 文档入口

| 文档 | 职责 |
| --- | --- |
| [01-现状证据与AI-first裁决.md](01-现状证据与AI-first裁决.md) | 本地事实源、源码证据、外部资料、DeepThink 确认包、主要裁决 |
| [02-目标架构与优化路线.md](02-目标架构与优化路线.md) | 目标 AI-first System Contract、manifest、preflight、diagnostics 和分阶段路线 |
| [03-调用点迁移与验证计划.md](03-调用点迁移与验证计划.md) | 调用点审计、未来实施任务、BDD、验证命令和场景门禁 |

## 当前源码入口

```text
Src/ECS/Runtime/System/
  Config/
  Lifecycle/
  State/
  Internal/
  SystemManager*.cs
  SystemRegistry.cs
  SystemDescriptor.cs
  Tests/SystemCore/
```

能力系统实现不放在 Runtime System：

```text
Src/ECS/Capabilities/<owner>/System/
Src/ECS/Tools/<owner>/
Src/ECS/UI/
```

## 当前正式模型

| 领域 | 当前事实 |
| --- | --- |
| 注册 | `SystemRegistry.Register(systemId, factory)` 只保存 `SystemId + Factory` |
| 配置 | `SystemConfigService` 只从 runtime snapshot 的 `system.config` records 读取 |
| 预设 | `SystemPresetService` 只从 runtime snapshot 的 `system.preset` records 读取 |
| 装载 | `Required / AutoLoad / active preset / Dependencies / Priority` 决定是否创建实例 |
| 运行 | `shouldRun = IsEnabled && IsStateAllowed` |
| 状态 | `ProjectStateService` 维护 `GameFlowState + OverlayFlags + SimulationState` |
| 命令 | 外部命令走 `SystemManager.Execute<TSystem, TRequest, TResult>`，先过同一套运行态门禁 |
| 调试 | `SystemRuntimeInfo` 和 TestSystem 的 `SystemInfoService` 已能合并 config / registry / runtime 信息 |

## 非目标

- 不复制 Bevy / Flecs / Unity DOTS / DefaultEcs 的 scheduler public API。
- 不引入第三方 ECS runtime。
- 不把 Godot Node 系统强行迁到纯数据 ECS。
- 不恢复 `SystemProfile`、旧四维 phase 或代码侧生命周期元数据。
- 不开放全局 query DSL 给 AI 绕过 capability owner。
- 不在本设计包直接修改代码；代码实施必须另建或扩展执行型 SDD。

## 后续执行入口

若进入实现，建议创建新的执行型 SDD：

```text
Title: System Contract Manifest And Diagnostics Hardening
Scope: Src/ECS/Runtime/System, DocsAI/ECS/Runtime/System, Data/DataOS, Src/ECS/Capabilities/TestSystem/System/System
```

默认首切片只做无行为变化的 AI-first 硬化：manifest、preflight、diagnostics dump 和测试门禁。是否引入 generated typed `SystemId` 属于更大 API 变更，需要单独确认。
