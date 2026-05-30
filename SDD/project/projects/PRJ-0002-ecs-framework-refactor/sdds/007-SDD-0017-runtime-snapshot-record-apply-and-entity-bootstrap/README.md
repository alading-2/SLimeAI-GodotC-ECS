# SDD-0017 Runtime Snapshot Record Apply and Entity Bootstrap

## Index Card

- **Status**: done
- **Created**: 2026-05-28
- **Updated**: 2026-05-29
- **Type**: implementation
- **Scope**: SlimeAI
- **Git Boundary**: /home/slime/Code/SlimeAI/SlimeAI
- **Affected Areas**:
  - SlimeAI/Src/ECS/Base/Data
  - SlimeAI/Src/ECS
- **Tags**: data, snapshot, bootstrap

## What This SDD Is About

实现 runtime_snapshot.json records 到 Data 容器的结构化写入，并接入 DataRuntimeBootstrap / Entity spawn，使运行时从 snapshot descriptors/records 初始化 Data，而不是 DataRegistry 或 LoadFromConfig。

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
- **Last Conclusion**: SDD-0017 已完成 runtime snapshot records apply、结构化 DataApplyReport、DataRuntimeBootstrap 和显式 EntityManager.Spawn bootstrap 分支。
- **Next Action**: 进入 SDD-0018，按模块迁移业务 descriptors，生成 typed DataKey thin handle，并迁移调用点。
- **Open Blockers**: none
