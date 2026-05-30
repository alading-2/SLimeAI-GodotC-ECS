# SDD-0019 Data Legacy Path Removal and Test Scene Rebuild

## Index Card

- **Status**: done
- **Created**: 2026-05-28
- **Updated**: 2026-05-29
- **Type**: migration
- **Scope**: SlimeAI
- **Git Boundary**: /home/slime/Code/SlimeAI/SlimeAI
- **Affected Areas**:
  - SlimeAI/Data/Data
  - SlimeAI/Data/DataNew
  - SlimeAI/Src/ECS/Test/SingleTest/ECS/Data
  - SlimeAI/DocsAI
- **Tags**: data, cleanup, godot-test

## What This SDD Is About

在新 Data runtime、descriptor 和迁移覆盖完成后删除旧 Data 输入路径，重建 Data 单场景测试，并同步 DocsAI/人类文档/skill，作为 Data 完整重构的收口门禁。

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
- **Last Conclusion**: 旧 Data/Data、DataNew、手写 DataMeta 注册和旧 Data 单场景入口已移除；DataOS descriptor/generated handle/runtime snapshot 路径成为 Data 事实源。
- **Next Action**: Data Full Rewrite 收口完成；后续如继续 PRJ-0002，可进入 Event 或 Entity/Relationship 的新执行型 SDD。
- **Open Blockers**: none
