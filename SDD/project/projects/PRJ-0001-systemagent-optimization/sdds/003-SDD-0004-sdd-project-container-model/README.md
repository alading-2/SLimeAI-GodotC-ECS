# SDD-0004 SDD Project Container Model

## Index Card

- **Status**: done
- **Created**: 2026-05-25
- **Updated**: 2026-05-25
- **Type**: workflow
- **Scope**: Workspace/SDD
- **Git Boundary**: /home/slime/Code/SlimeAI
- **Affected Areas**:
  - Workspace/SDD
- **Tags**: sdd, project-container

## What This SDD Is About

本 SDD 实施项目级 SDD 容器：新增 `SDD/project/projects/` 与 `SDD/project/archived/`，让相关任务共享项目级设计，并把真实状态改为由 `project.json.status` / `sdd.json.status` 决定。

## Reading Order

1. `../../design/INDEX.md` — 项目共享设计资料
2. `design/INDEX.md` — 本 SDD 任务级设计入口
3. `tasks.md` — 当前任务拆分
4. `progress.md` — 最近结论和恢复点
5. `bdd.md` — 行为场景

## Current Resume

- **Current Task**: done
- **Last Conclusion**: 项目 CLI、metadata 状态模型和基础项目校验已实现，正在补全文档、项目实例与验证闭环。
- **Next Action**: 更新文档和 skill 后运行 sync、unit tests、validate 与 lint。
- **Open Blockers**: none
