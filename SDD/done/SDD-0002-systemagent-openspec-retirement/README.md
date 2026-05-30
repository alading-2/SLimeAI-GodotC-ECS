# SDD-0002 SystemAgent OpenSpec Retirement

## Index Card

- **Status**: done
- **Created**: 2026-05-24
- **Updated**: 2026-05-24
- **Type**: workflow
- **Scope**: Workspace/SystemAgent
- **Git Boundary**: /home/slime/Code/SlimeAI
- **Affected Areas**:
  - Workspace/SystemAgent
  - .ai-config/skills/ai
  - .ai-config/skills/systemagent
  - .ai-config/skills/core
  - .ai-config/skills/godot
  - .ai-config/skills/ecs
  - .ai-config/rules
- **Tags**: systemagent, sdd, openspec-retirement

## What This SDD Is About

将 SystemAgent 的默认中大型任务入口从 OpenSpec 切换为 SDD。OpenSpec 目录和兼容 skill 保留为历史资产与显式兼容入口，但不再作为默认 workflow、protocol、rule 或长任务执行记忆。

## Reading Order

1. `design/INDEX.md` — 设计文档列表和主设计入口
2. `tasks.md` — 当前任务拆分
3. `progress.md` — 最近结论和恢复点
4. `bdd.md` — 行为场景或不适用说明
5. `notes.md` — 参考与开放问题

## Current Resume

- **Current Task**: done
- **Last Conclusion**: SystemAgent 默认入口已切换为 SDD-first；OpenSpec 专属协议文件已删除；`.ai-config` 源已更新并同步；SDD validate 与 skill-test 已通过。
- **Next Action**: 无需继续；如有新问题创建新 SDD。
- **Open Blockers**: none
