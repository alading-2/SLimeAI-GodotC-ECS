# SDD-0018 Data Descriptor Migration and Generated Handles

## Index Card

- **Status**: done
- **Created**: 2026-05-28
- **Updated**: 2026-05-29
- **Type**: migration
- **Scope**: SlimeAI
- **Git Boundary**: /home/slime/Code/SlimeAI/SlimeAI
- **Affected Areas**:
  - SlimeAI/Data/DataKey
  - SlimeAI/Data
  - SlimeAI/Src/ECS
- **Tags**: data, migration, codegen

## What This SDD Is About

按 Base/Unit、Attribute、Movement、Ability、Feature、AI/Test 顺序迁移旧 DataKey_*.cs 字段能力到 descriptor，并生成只含 stable key 和泛型类型的薄 DataKey<T> handle；业务调用点收口到 generated handle。

## Reading Order

1. `../../design/Runtime/2.Data系统优化/README.md` — Data 完整重构总裁决
2. `../../design/Runtime/2.Data系统优化/01-代码实现说明.md` — 目标代码形状
3. `../../design/Runtime/2.Data系统优化/02-DataMeta属性审计与Feature计算边界.md` — DataMeta 属性和 Feature/Compute 边界
4. `../../design/Runtime/2.Data系统优化/03-完全重构范围与TDD测试计划.md` — 删除范围、TDD 矩阵和验收门禁
5. `design/main.md` — 本 SDD 切片设计
6. `tasks.md` — 可执行任务
7. `Core/progress.md` — 最新恢复点与验证摘要
8. `execution-prompt.md` — 可直接复制给执行会话/子代理的任务提示词

## Current Resume

- **Current Task**: done
- **Last Conclusion**: 旧 DataKey/DataMeta 字段能力已迁移到 DataOS descriptor authoring 事实源，snapshot descriptors 改为由 `data_key_descriptor` 表驱动，并生成 typed thin handle。
- **Next Action**: 进入 SDD-0019，删除旧 Data/Data、DataNew、旧 Data 测试场景并重建 Godot smoke / Docs sync。
- **Open Blockers**: none
