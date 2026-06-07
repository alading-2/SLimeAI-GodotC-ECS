# SDD-0033 Non-Data GC Boundary Completion

## Index Card

- **Status**: done
- **Created**: 2026-06-07
- **Updated**: 2026-06-07
- **Type**: refactor
- **Scope**: SlimeAI
- **Git Boundary**: /home/slime/Code/SlimeAI/SlimeAI
- **Affected Areas**:
  - Src/ECS/Runtime/Event
  - Src/ECS/Capabilities/Feature
  - Src/ECS/Capabilities/Ability
  - Src/ECS/Tools/ObjectPool
  - Src/ECS/Tools/TargetSelector
- **Tags**: gc, event, feature, ability, objectpool, target-selector

## What This SDD Is About

Data runtime 的 generic slot hard cutover 已完成，本 SDD 收口剩余非 Data 热路径里已经有明确证据的问题：Event 动态 object 入口、Feature / Ability execute 边界的 raw object 传递、ObjectPool manager 反射管理，以及 TargetSelector 查询结果 ownership。目标不是追求全仓零 GC，而是把明显错误的主协议宽口改成 typed / 可验证边界。

## Reading Order

1. `design/INDEX.md` — 设计文档列表和主设计入口
2. `tasks.md` — 当前任务拆分
3. `progress.md` — 最近结论和恢复点
4. `bdd.md` — 行为场景或不适用说明
5. `notes.md` — 参考与开放问题

## Current Resume

- **Current Task**: done
- **Last Conclusion**: Event/Feature/Ability typed boundary、ObjectPool runtime interface、TargetQueryEngine facade、DocsAI/skill sync 和 PRJ-0002 current sources 已完成；dotnet build、DataOS validator、skill lint 和 grep gates 已通过。
- **Next Action**: PRJ-0002 当前无 active 子 SDD；后续只从 Logger 热路径、TargetQuery pooled lease/deterministic RNG 或 profiler 证据指向的局部 cleanup 创建新 SDD。
- **Open Blockers**: none
