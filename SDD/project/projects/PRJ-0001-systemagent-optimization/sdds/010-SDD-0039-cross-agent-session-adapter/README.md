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

边界：不改原始 session 文件名，不复制完整 transcript，不接自动 hook，不 fork 上游工具。OpenCode 第一版只保留支持路径，不要求本机真实样例。

## Reading Order

1. `design/INDEX.md` — 设计文档列表和主设计入口
2. `design/main.md` — 本 SDD 的执行级设计
3. `tasks.md` — 当前任务拆分
4. `bdd.md` — 行为场景和验收标准
5. `progress.md` — 最近结论和恢复点
6. `notes.md` — 参考与开放问题

## Current Resume

- **Current Task**: done
- **Last Conclusion**: Cross-agent Session Adapter 第一版完成：只读 `list/index/summarize` 可用，默认通过本地 `codbash` 读取 Claude/Codex 会话并生成 ChatHistory sidecar；OpenCode 保留支持路径但不要求本机真实样例。
- **Next Action**: 后续增强另建 SDD：Claude/OpenCode 高保真导出、`codlogs --include-tool-results` 自动化、retrospective 可选接入，或只读资料 subagent pilot。
- **Open Blockers**: none
