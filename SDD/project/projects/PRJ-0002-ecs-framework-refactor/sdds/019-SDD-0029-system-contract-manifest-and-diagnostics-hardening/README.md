# SDD-0029 System Contract Manifest And Diagnostics Hardening

## Index Card

- **Status**: done
- **Created**: 2026-06-03
- **Updated**: 2026-06-03
- **Type**: refactor
- **Scope**: SlimeAI
- **Git Boundary**: /home/slime/Code/SlimeAI/SlimeAI
- **Affected Areas**:
  - ecs/runtime/system
  - DocsAI/ECS/Runtime/System
  - Src/ECS/Runtime/System
  - Src/ECS/Capabilities/TestSystem/System/System
  - Data/DataOS
- **Tags**: system, runtime, ai-first, diagnostics, docsai

## What This SDD Is About

本 SDD 承接项目级 `design/Runtime/8.System优化/` 裁决：保留现有 Runtime System Core，不重写 `SystemManager` 生命周期；首切片只做 AI-first 合同硬化，把系统清单、配置/注册/运行态 preflight、diagnostics snapshot、生命周期 trace 和 DocsAI 文档同步纳入可验证闭环。

本任务必须同步更新 `DocsAI/ECS/Runtime/System/`，后续实现不得只改源码和测试。

## Reading Order

1. `../../design/Runtime/8.System优化/README.md` — 项目共享设计包入口
2. `../../design/Runtime/8.System优化/01-现状证据与AI-first裁决.md` — 裁决、风险和确认点
3. `../../design/Runtime/8.System优化/02-目标架构与优化路线.md` — 目标 Contract Layer
4. `../../design/Runtime/8.System优化/03-调用点迁移与验证计划.md` — 调用点、BDD 和验证矩阵
5. `design/main.md` — 本 SDD 执行级设计
6. `tasks.md` — 当前任务拆分
7. `bdd.md` — 行为验收场景
8. `execution-prompt.md` — 新会话执行提示词

## Current Resume

- **Current Task**: done
- **Last Conclusion**: Runtime System AI-first contract hardening 已完成：SystemManifest、SystemPreflight、SystemDiagnosticsSnapshot、稳定 blocked reason code、lifecycle trace、TestSystem diagnostics adapter、SystemCore diagnostics artifact 支持、DocsAI 同步和 ecs-system skill 同步均已落地；未重写 SystemManager 生命周期，未做 typed SystemId hard cutover。
- **Next Action**: 本 SDD 无剩余框架代码任务；承载游戏 runner 可用后，运行 SystemCoreRuntimeTest 并检查 `.ai-temp/scene-tests/artifacts/system-core-diagnostics.json` 作为 scene-gate 证据。
- **Open Blockers**: none
