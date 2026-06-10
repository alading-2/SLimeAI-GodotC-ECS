# SDD-0041 Session Adapter Digest Accuracy and Retrospective Handoff

## Index Card

- **Status**: done
- **Created**: 2026-06-10
- **Updated**: 2026-06-10
- **Type**: workflow
- **Scope**: Workspace/SystemAgent
- **Git Boundary**: /home/slime/Code/SlimeAI/SlimeAI
- **Affected Areas**:
  - Workspace/SystemAgent/Tools/session-adapter
  - Workspace/SystemAgent/Actors
  - Workspace/SystemAgent/Rules
  - .ai-config/skills/systemagent-skill
  - .ai-config/rules
  - SDD/project/projects/PRJ-0001-systemagent-optimization
- **Tags**: systemagent

## What This SDD Is About

本 SDD 执行 PRJ-0001 的 session-adapter 二次审查结论：按新契约完整重构 Codex digest 分类、摘要提取、失败分析、stale 检查和 Retrospective/DeepThink 会话证据交接。

核心裁决：session-adapter 是内部 AI 工具，后续重构不维护旧 digest / index schema fallback。旧 ChatHistory 产物只作为一次性迁移输入或只读证据，完成后默认入口必须指向新契约。

## Reading Order

1. `execution-prompt.md` — 交给执行会话的完整提示词
2. `design/main.md` — 本 SDD 的任务级设计
3. `tasks.md` — 执行批次、边界和验证
4. `bdd.md` — 行为验收场景
5. `progress.md` — 当前恢复点
6. `notes.md` — 参考、默认假设和开放问题

## Current Resume

- **Current Task**: done
- **Last Conclusion**: SDD-0041 已冻结目标：完整重构 session-adapter digest 准确性和 Retrospective handoff，不维护旧格式 fallback。
- **Next Action**: 从 `execution-prompt.md` 开始执行 T1.1，先补 baseline tests，再改 `session_adapter.py` 命令分类与 schema。
- **Open Blockers**: none
