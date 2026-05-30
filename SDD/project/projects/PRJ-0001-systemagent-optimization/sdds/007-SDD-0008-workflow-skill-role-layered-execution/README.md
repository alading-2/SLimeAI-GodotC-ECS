# SDD-0008 Workflow Skill Role Layered Execution

## Index Card

- **Status**: done
- **Created**: 2026-05-25
- **Updated**: 2026-05-25
- **Type**: workflow
- **Scope**: Workspace/SystemAgent
- **Git Boundary**: /home/slime/Code/SlimeAI
- **Affected Areas**:
  - Workspace/SystemAgent
  - .ai-config/skills/systemagent
  - .ai-config/skills/ai
- **Tags**: systemagent, workflow, skill, role

## What This SDD Is About

本 SDD 将 SDD-0006 已确认的信息架构进一步落到执行流程：标准化 selected workflow 输出、任务规模分级、workflow phase、workflow entry skill 与 capability skill 边界，并减少每次任务启动的上下文仪式感。

## Reading Order

1. `design/INDEX.md` — 设计文档列表和主设计入口
2. `tasks.md` — 当前任务拆分
3. `progress.md` — 最近结论和恢复点
4. `bdd.md` — 行为验收场景
5. `notes.md` — 共享设计引用和开放问题

## Current Resume

- **Current Task**: done
- **Last Conclusion**: Workflow / Skill / Role 分层执行已落地：SystemAgent 默认入口收敛为 README → INDEX → selected workflow → current SDD，6 个 workflow 第一屏声明 route、task_size、SDD strategy 和 phases，workflow catalog 区分 must_read 与 conditional_read。
- **Next Action**: PRJ-0001 的 SystemAgent 优化子 SDD 队列已完成；后续新增 dispatcher、写入型 subagent 或更大目录迁移需新建独立 SDD。
- **Open Blockers**: none
