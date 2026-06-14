# SDD-0044 Data Compute Registry Singleton And Catalog Validation Convergence

## Index Card

- **Status**: pending
- **Created**: 2026-06-14
- **Updated**: 2026-06-14
- **Type**: refactor
- **Scope**: SlimeAI
- **Git Boundary**: /home/slime/Code/SlimeAI/SlimeAI
- **Affected Areas**:
  - ecs/runtime/data
  - ecs/tools/logger
  - docsai/runtime/data
  - ai-config/skills
- **Tags**: data, compute, catalog, validation, diagnostics, ai-first

## What This SDD Is About

本 SDD 执行 `DataComputeRegistry` 默认单例化与 Data catalog 验证边界收敛。

核心方向来自项目级设计：默认 computed resolver 是框架写定能力，应由 `DataComputeRegistry.Default` 作为唯一默认入口；自定义 resolver 仍通过 `new DataComputeRegistry()` 显式传入 bootstrap。`DataComputeRegistry` 收窄为 resolver table，不再理解 `DataDefinition`；computed binding、依赖、输出类型和循环引用校验收敛到 catalog build。Catalog / bootstrap 仍允许 fatal `throw`，但必须先形成 report / structured log，便于 AI 通过 Log 分析问题。

## Reading Order

1. `../../design/Runtime/2.Data系统优化/4.Data验证与Registry简化/01-DataComputeRegistry单例与Catalog验证收敛.md` — 项目级方向设计和用户原始问题
2. `design/main.md` — 本 SDD 的执行边界和局部设计
3. `tasks.md` — 任务拆分与验证门禁
4. `bdd.md` — 本次实现必须满足的行为场景
5. `progress.md` — State / Decisions / Validation 状态面板
6. `notes.md` — 参考与默认假设

## Current Resume

- **Current Task**: T1.1
- **Last Conclusion**: 用户已确认方向无大问题，SDD-0044 已创建为后续执行任务；当前只生成 SDD，尚未开始 runtime 实现。
- **Next Action**: 从 T1.1 readiness 开始，先确认当前源码、DocsAI 和 DataOS 场景基线，再实施 registry 单例与 catalog build report。
- **Open Blockers**: none
