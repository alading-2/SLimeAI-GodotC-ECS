# SDD-0007 Hook and Gate P0 Stability

## Index Card

- **Status**: done
- **Created**: 2026-05-25
- **Updated**: 2026-05-25
- **Type**: workflow
- **Scope**: Workspace/SystemAgent
- **Git Boundary**: /home/slime/Code/SlimeAI
- **Affected Areas**:
  - Workspace/SystemAgent
  - Workspace/SystemAgent/Tools/systemagent-hooks
  - .claude/settings.json
  - .codex/hooks.json
- **Tags**: systemagent, hook, gate

## What This SDD Is About

本 SDD 将 Hook 从流程执行器降级为低频安全栏，并先解决 P0 稳定性：Stop hook 永远输出合法 JSON、异常 fallback 可验证、Stop 阶段不运行耗时命令，同时为后续 Gate 提供 SDD-aware 输入契约。

## Reading Order

1. `design/INDEX.md` — 设计文档列表和主设计入口
2. `tasks.md` — 当前任务拆分
3. `progress.md` — 最近结论和恢复点
4. `bdd.md` — 行为验收场景
5. `notes.md` — 共享设计引用和开放问题

## Current Resume

- **Current Task**: done
- **Last Conclusion**: SDD-0007 已完成：新增 hook smoke，Stop 输出统一 JSON/fallback，Stop 阶段不再运行长命令，PostToolUse 支持 SDD validate 与 cooldown 去重，Gate 文档改为 SDD-aware evidence checklist。
- **Next Action**: 继续 PRJ-0001 的 SDD-0008 Workflow / Skill / Role 分层执行。
- **Open Blockers**: none
