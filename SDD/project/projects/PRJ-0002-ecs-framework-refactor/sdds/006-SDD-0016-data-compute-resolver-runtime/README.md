# SDD-0016 Data Compute Resolver Runtime

## Index Card

- **Status**: done
- **Created**: 2026-05-28
- **Updated**: 2026-05-29
- **Type**: implementation
- **Scope**: SlimeAI
- **Git Boundary**: /home/slime/Code/SlimeAI/SlimeAI
- **Affected Areas**:
  - SlimeAI/Src/ECS/Base/Data
- **Tags**: data, compute, runtime

## What This SDD Is About

实现 Data computed 的独立运行时：ComputeId + IDataComputeResolver、依赖图、循环检测、cache、transitive dirty 和 resolver contract。FeatureSystem 不承载 computed。

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

- **Current Task**: complete
- **Last Conclusion**: SDD-0016 已完成 Data computed runtime：`DataRuntimeStorage` 通过 `DataComputeRegistry` / `IDataComputeResolver` 计算 computed 字段，支持 cache、依赖变化递归标脏、computed readonly 和基础 resolver 示例。
- **Next Action**: 进入 SDD-0017，实现 runtime snapshot records ApplyRecord、DataApplyReport 和 Entity/Data bootstrap。
- **Open Blockers**: none
