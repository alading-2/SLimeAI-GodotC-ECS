# SDD-0014 Data Runtime Slot and Policy Model

## Index Card

- **Status**: done
- **Created**: 2026-05-28
- **Updated**: 2026-05-28
- **Type**: implementation
- **Scope**: SlimeAI
- **Git Boundary**: /home/slime/Code/SlimeAI/SlimeAI
- **Affected Areas**:
  - SlimeAI/Src/ECS/Base/Data
- **Tags**: data, runtime, policy

## What This SDD Is About

重建 Data 容器核心读写模型：DataSlot、DataValueConverter、默认值、write policy、range policy、allowed values、typed handle 与内部 string API 收口，替代 DataRegistry 驱动的 Get/Set 行为。

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
- **Last Conclusion**: SDD-0014 已完成：新增 descriptor-first `DataRuntimeStorage`、`DataSlot`、`DataValueConverter`、typed `DataKey<T>`、write/range/allowed values policy 和 Data changed 桥接，生产 `Data` 已可在绑定 catalog 时走新 runtime path。
- **Next Action**: 进入 SDD-0015，重建 modifier runtime 与 Feature.Modifiers authoring_blob bridge；完整 computed resolver、records apply、Entity bootstrap 和旧路径删除仍留给 SDD-0016~SDD-0019。
- **Open Blockers**: none
