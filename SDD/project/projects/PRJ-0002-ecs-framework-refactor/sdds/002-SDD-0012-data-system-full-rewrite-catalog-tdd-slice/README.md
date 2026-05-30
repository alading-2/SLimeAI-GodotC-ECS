# SDD-0012 Data System Full Rewrite - Catalog TDD Slice

## Index Card

- **Status**: done
- **Created**: 2026-05-28
- **Updated**: 2026-05-28
- **Type**: implementation
- **Scope**: SlimeAI
- **Git Boundary**: /home/slime/Code/SlimeAI/SlimeAI
- **Affected Areas**:
  - SlimeAI/Src/ECS/Base/Data
  - SlimeAI/Data/DataKey
  - SlimeAI/Src/ECS/Test/SingleTest/ECS/Data
- **Tags**: data, data-rewrite, tdd

## What This SDD Is About

建立 Data 完整重构的第一切片：先写新 Data catalog 红灯测试，再实现 descriptor-first 的 DataDefinition、DataDefinitionCatalog、BuildCatalog 和一次性旧定义审计。该 SDD 不保留 DataMeta/DataRegistry 运行时兼容层，也不做全量业务字段迁移。

## Reading Order

1. `../../design/2.Data系统优化/README.md` — Data 完整重构总裁决
2. `../../design/2.Data系统优化/01-代码实现说明.md` — 目标代码形状
3. `../../design/2.Data系统优化/02-DataMeta属性审计与Feature计算边界.md` — DataMeta 属性和 Feature/Compute 边界
4. `../../design/2.Data系统优化/03-完全重构范围与TDD测试计划.md` — 删除范围、TDD 矩阵和验收门禁
5. `design/main.md` — 本 SDD 切片设计
6. `tasks.md` — 可执行任务
7. `progress.md` — 最新恢复点与验证摘要
8. `execution-prompt.md` — 可直接复制给执行会话/子代理的任务提示词

## Current Resume

- **Current Task**: done
- **Last Conclusion**: SDD-0012 Catalog TDD Slice 完成：descriptor-first catalog、BuildCatalog、DataComputeRegistry 和 LegacyDataAuditReport 已落地，旧 Data 运行时路径未接入新 catalog。
- **Next Action**: 进入 SDD-0013 DataOS Descriptor Authoring and Snapshot Schema。
- **Open Blockers**: none
