# SDD-0015 Data Modifier Runtime and Feature Bridge

## Index Card

- **Status**: done
- **Created**: 2026-05-28
- **Updated**: 2026-05-28
- **Type**: implementation
- **Scope**: SlimeAI
- **Git Boundary**: /home/slime/Code/SlimeAI/SlimeAI
- **Affected Areas**:
  - SlimeAI/Src/ECS/Base/Data
  - SlimeAI/Src/ECS/Feature
- **Tags**: data, modifier, feature

## What This SDD Is About

重建 Data modifier pipeline，并让 Feature.Modifiers 作为 authoring_blob descriptor 接入新 Data modifier policy；Feature 继续负责生命周期和 modifier 授予/回滚，不接管 computed。

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
- **Last Conclusion**: modifier runtime 已进入 descriptor-first `DataRuntimeStorage`：按 `modifier_policy` 校验，支持 Additive/Multiplicative/FinalAdditive/Override/Cap、source removal、dependent computed dirty；Feature bridge 改为通过 Data modifier API 授予/回滚，不接管 computed。
- **Next Action**: 进入 `SDD-0016 Data Compute Resolver Runtime`，实现 computed resolver/cache/dirty 的实际求值链路。
- **Open Blockers**: none
