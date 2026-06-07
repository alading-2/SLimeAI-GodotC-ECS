# SDD-0032 Data Runtime Typed Contract Completion

## Index Card

- **Status**: done
- **Created**: 2026-06-06
- **Updated**: 2026-06-07
- **Type**: refactor
- **Scope**: SlimeAI
- **Git Boundary**: /home/slime/Code/SlimeAI/SlimeAI
- **Affected Areas**:
  - Src/ECS/Runtime/Data
  - Data/DataOS
  - Src/ECS/Capabilities/Ability
  - Src/ECS/Capabilities/Feature
  - Src/ECS/Capabilities/Unit
- **Tags**: data, typed-contract, gc, event, feature

## What This SDD Is About

本 SDD 执行 SDD-0031 之后的 Data typed contract completion：业务 Capability 和 AI 可调用 Data 协议不再使用 string key / untyped write / object payload 作为主链路；loader、snapshot、TestSystem、migration 和 diagnostic dump 仍可保留 object 边界，但必须通过命名、注释和 grep gate 限定。

## Reading Order

1. `design/INDEX.md` — 设计文档列表和主设计入口
2. `design/main.md` — DeepThink、DesignCritic、范围、取舍和验证门禁
3. `tasks.md` — 当前任务拆分
4. `bdd.md` — 行为验收场景
5. `Core/progress.md` — 最近结论和恢复点
6. `Core/notes.md` — 参考与开放问题

## Current Resume

- **Current Task**: done
- **Last Conclusion**: 方向冻结：不追求全仓零 object；本轮收口业务 string/untyped Data 调用、typed system/debug 写入、typed default cache、typed computed resolver、typed Data changed、diagnostic snapshot 命名和 modifier typed source。
- **Next Action**: 按 tasks.md 从 T1.1 进入实现；每个任务组后更新 progress 和验证证据。
- **Open Blockers**: none
