# SDD-0005 SDD CLI Source Modularization

## Index Card

- **Status**: done
- **Created**: 2026-05-25
- **Updated**: 2026-05-25
- **Type**: workflow
- **Scope**: Workspace/SDD
- **Git Boundary**: /home/slime/Code/SlimeAI
- **Affected Areas**:
  - Workspace/SDD
- **Tags**: sdd, refactor

## What This SDD Is About

本 SDD 负责把 `Workspace/SDD/sdd.py` 的 CLI 实现拆分到 `Workspace/SDD/Src/` 职责模块中，同时保持公开命令入口、输出格式、metadata schema 和验证方式不变。当前拆分已经完成，剩余工作是运行完整验证并记录完成结论。

## Reading Order

1. `design/INDEX.md` — 设计文档列表和主设计入口
2. `tasks.md` — 当前任务拆分
3. `progress.md` — 最近结论和恢复点
4. `bdd.md` — 行为场景或不适用说明
5. `notes.md` — 参考与开放问题

## Current Resume

- **Current Task**: done
- **Last Conclusion**: `sdd.py` 已收敛为 CLI 参数和命令绑定入口，模板、仓储、项目、任务、进度、索引和校验逻辑已拆入 `Workspace/SDD/Src/`；本轮又补强了项目 roadmap 模板与文档契约。
- **Next Action**: 运行完整 SDD 验证、确认 `validate --all` 无错误，并将 T1.4 的验证摘要写入完成结论。
- **Open Blockers**: none
