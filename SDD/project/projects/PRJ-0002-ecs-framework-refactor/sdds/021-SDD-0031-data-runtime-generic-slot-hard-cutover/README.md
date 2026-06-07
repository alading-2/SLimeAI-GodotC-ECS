# SDD-0031 Data Runtime Generic Slot Hard Cutover

## Index Card

- **Status**: done
- **Created**: 2026-06-06
- **Updated**: 2026-06-06
- **Type**: refactor
- **Scope**: SlimeAI
- **Git Boundary**: /home/slime/Code/SlimeAI/SlimeAI
- **Affected Areas**:
  - ecs/runtime/data
- **Tags**: data, gc, boxing, hard-cutover

## What This SDD Is About

本 SDD 执行 `design/01-Data运行时object去除设计.md` 的 Data-only 切片：把 Runtime Data typed `DataKey<T>` 读写、modifier 和 computed 主链路从 `DataSlot.Value object?` 切到 `DataSlot<T> + IDataSlot`。

本轮只改 Data runtime、Data tests、DocsAI Runtime/Data 和 `ecs-data` skill 源。EventBus dynamic object、Feature / Ability typed execution context、ObjectPool、TargetSelector、Logger 仍按项目 roadmap 后续 SDD 处理，本轮不改。

## Reading Order

1. `design/INDEX.md` — 设计文档列表和主设计入口
2. `design/main.md` — 本轮 DeepThink、裁决、范围和实现方案
3. `design/01-Data运行时object去除设计.md` — Data 方案来源副本
4. `tasks.md` — 当前任务拆分
5. `bdd.md` — Data runtime 行为验收
6. `Core/progress.md` — 最近结论和恢复点
7. `Core/notes.md` — 参考与开放问题

## Current Resume

- **Current Task**: done
- **Last Conclusion**: Data runtime generic slot hard cutover 已完成：typed `DataKey<T>` 主链路、modifier effective value 和 computed cache 使用 `DataSlot<T> + IDataSlot`；untyped API 仅保留 loader/debug/TestSystem 边界。
- **Next Action**: 若继续同一 GC/装箱优化设计包，下一步创建 Event Dynamic Object Removal SDD；不要在本 SDD 继续改 Event / Feature / Ability / ObjectPool / TargetSelector / Logger。
- **Open Blockers**: none
