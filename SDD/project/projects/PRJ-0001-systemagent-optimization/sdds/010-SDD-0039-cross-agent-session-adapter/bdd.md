# BDD

## Applicability

- **Required**: true
- **Reason**: 本 SDD 新增 SystemAgent CLI 工具行为，需要可观察的输入、输出和失败标准。

## Scenarios

### Scenario: List recent local AI sessions

Given 当前仓存在 Codex 或 Claude Code 历史会话，且 `codbash` 可运行
When 用户运行 `python3 Workspace/SystemAgent/Tools/session-adapter/session_adapter.py list --repo . --limit 5`
Then 输出包含最多 5 个候选会话，每项包含 source tool、session id、title、time 和 cwd/project

### Scenario: Generate ChatHistory sidecar for one session

Given `list` 输出中存在一个 session id
When 用户运行 `python3 Workspace/SystemAgent/Tools/session-adapter/session_adapter.py index --session <id>`
Then 工具创建或更新 `Workspace/DocsAI/ChatHistory/index.json`
And 工具创建或更新一个形如 `2026-06-09-1249-codex-游戏开发流程agent-019eaab6.md` 的 sidecar
And sidecar 包含 Metadata、First Prompt、Tool Evidence、Validation Evidence、Final State 和 Resume Prompt 标题

### Scenario: OpenCode support is non-blocking without local sample

Given 当前本机没有 OpenCode session
When 用户运行 `list` 或 `index`
Then 工具不因为 OpenCode 样例缺失而失败
And 文档和 metadata 保留 `opencode` 作为支持的 source tool

### Scenario: Export Codex month into date-partitioned visible transcripts

Given `/home/slime/.codex/sessions/2026/06` 存在 Codex `rollout-*.jsonl`
When 用户运行 `python3 Workspace/SystemAgent/Tools/session-adapter/session_adapter.py export-codex-month --source-root /home/slime/.codex/sessions/2026/06`
Then 工具按 `Workspace/DocsAI/ChatHistory/2026/06/DD/` 创建 Markdown transcript
And `Workspace/DocsAI/ChatHistory/index.json` 为每个导出的 Codex session 记录 `evidence_level=visible-transcript`
And 导出的 Markdown 包含 `Source SHA256`、`Event Counts`、`function_call_output` tool output 和隐藏推理占位
And 工具不复制原始 JSONL 到仓库
