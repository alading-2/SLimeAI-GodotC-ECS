# SDD-0039 Cross-agent Session Adapter

## Index Card

- **Status**: done
- **Created**: 2026-06-09
- **Updated**: 2026-06-09
- **Type**: workflow
- **Scope**: Workspace/SystemAgent
- **Git Boundary**: /home/slime/Code/SlimeAI/SlimeAI
- **Affected Areas**:
  - Workspace/SystemAgent/Tools/session-adapter
  - Workspace/DocsAI/ChatHistory
  - SDD/project/projects/PRJ-0001-systemagent-optimization
- **Tags**: systemagent, session-adapter

## What This SDD Is About

实现一个只读的 Cross-agent Session Adapter，作为 SystemAgent 的本地会话记录整理入口。

第一版只做手动触发的小闭环：

- `list`：列出当前仓最近 Claude Code / Codex / OpenCode 候选会话，优先调用 `codbash`。
- `index`：为指定 session 生成 `Workspace/DocsAI/ChatHistory/index.json` entry 和 Markdown sidecar。
- `summarize`：输出或刷新指定 session 的 ChatHistory sidecar。

边界：不改原始 session 文件名，不复制原始 JSONL，不接自动 hook，不 fork 上游工具。`index/summarize` 只生成摘要恢复入口；`export-codex-month` 可导出 Codex 可见 transcript，但不还原隐藏推理。OpenCode 第一版只保留支持路径，不要求本机真实样例。

## Reading Order

1. `design/INDEX.md` — 设计文档列表和主设计入口
2. `design/main.md` — 本 SDD 的执行级设计
3. `tasks.md` — 当前任务拆分
4. `bdd.md` — 行为场景和验收标准
5. `progress.md` — 最近结论和恢复点
6. `notes.md` — 参考与开放问题

## Current Resume

- **Current Task**: done
- **Last Conclusion**: 已补 Codex 月度高保真导出：`export-codex-month` 将 `/home/slime/.codex/sessions/2026/06` 的 63 个 Codex session 导出到 `Workspace/DocsAI/ChatHistory/2026/06/DD/`，逐条 transcript 标题已去掉时间戳；旧 summary sidecar 只适合作恢复入口，不足以完整 AI 复盘。
- **Next Action**: 后续增强另建 SDD：Claude/OpenCode 高保真导出、ChatHistory prune/归档策略、retrospective 可选接入。
- **Open Blockers**: none
