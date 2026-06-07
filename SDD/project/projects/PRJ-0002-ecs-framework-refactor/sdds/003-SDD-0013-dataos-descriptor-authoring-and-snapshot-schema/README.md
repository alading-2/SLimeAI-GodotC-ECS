# SDD-0013 DataOS Descriptor Authoring and Snapshot Schema

## Index Card

- **Status**: done
- **Created**: 2026-05-28
- **Updated**: 2026-05-28
- **Type**: implementation
- **Scope**: SlimeAI
- **Git Boundary**: /home/slime/Code/SlimeAI/SlimeAI
- **Affected Areas**:
  - SlimeAI/Data
  - SlimeAI/Src/ECS/Base/Data/RuntimeSnapshot
- **Tags**: data, dataos, snapshot

## What This SDD Is About

补齐 DataOS authoring schema、generator 和 validator，让 runtime_snapshot.json.descriptors 成为字段定义唯一事实源，并与 SDD-0012 的 RuntimeDataDescriptorDto 契约一致。

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
- **Last Conclusion**: SDD-0013 已完成：DataOS descriptor-first schema、结构化 validator、runtime snapshot descriptor shape、capability trimming、record/descriptor consistency 和最小 descriptor fixture 均已落地。
- **Next Action**: 进入 SDD-0014，基于 descriptor catalog 实现 DataSlot、DataValueConverter、write/range/allowed values 和 typed handle 读写模型。
- **Open Blockers**: none
