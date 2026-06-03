# SDD-0029 System Contract Manifest And Diagnostics Hardening

## Index Card

- **Status**: pending
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

本 SDD 承接项目级 `design/8.System优化/` 裁决：保留现有 Runtime System Core，不重写 `SystemManager` 生命周期；首切片只做 AI-first 合同硬化，把系统清单、配置/注册/运行态 preflight、diagnostics snapshot、生命周期 trace 和 DocsAI 文档同步纳入可验证闭环。

本任务必须同步更新 `DocsAI/ECS/Runtime/System/`，后续实现不得只改源码和测试。

## Reading Order

1. `../../design/8.System优化/README.md` — 项目共享设计包入口
2. `../../design/8.System优化/01-现状证据与AI-first裁决.md` — 裁决、风险和确认点
3. `../../design/8.System优化/02-目标架构与优化路线.md` — 目标 Contract Layer
4. `../../design/8.System优化/03-调用点迁移与验证计划.md` — 调用点、BDD 和验证矩阵
5. `design/main.md` — 本 SDD 执行级设计
6. `tasks.md` — 当前任务拆分
7. `bdd.md` — 行为验收场景
8. `execution-prompt.md` — 新会话执行提示词

## Current Resume

- **Current Task**: T1.1
- **Last Conclusion**: SDD-0029 已创建并补齐为 pending 执行胶囊；目标是 System AI-first contract hardening，不进入 typed SystemId hard cutover。
- **Next Action**: 从 `execution-prompt.md` 执行 T1.1 readiness baseline；先记录 dirty workspace、System config/registry/execute 调用点、DocsAI 当前状态和现有验证基线。
- **Open Blockers**: none
