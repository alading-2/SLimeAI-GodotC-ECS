# Cross-agent Session Adapter

## Goal

实现 SystemAgent 的只读会话记录适配器，把 Claude Code / Codex / OpenCode 的本地会话从工具私有记录变成 SlimeAI 可恢复、可复盘的 ChatHistory sidecar。

第一版目标：

- 手动列出当前仓最近会话。
- 手动为指定 session 生成 Markdown sidecar。
- 维护 `Workspace/DocsAI/ChatHistory/index.json`。
- 对 Codex 支持 `codlogs` 高保真补充路径。
- 对 OpenCode 保留支持路径，但不要求当前本机有真实 OpenCode session。

非目标：

- 不自动抓取。
- 不接 hook/watch/MCP/dashboard。
- 不改 Claude Code / Codex / OpenCode 原始 session 文件名或索引。
- 默认 `index/summarize` 不复制完整 transcript；按需 `export-codex-month` 可导出 Codex 可见 transcript。
- 不复制原始 JSONL，不还原或伪造隐藏推理。
- 不还原或推断私有 chain-of-thought。
- 不 fork `codbash` / `codlogs` / `tracebase`。

## Context

设计来源已复制到本 SDD：

- `2026-06-09-参考项目驱动的Cross-agent-Session-Adapter设计.md`

项目级原始位置：

- `SDD/project/projects/PRJ-0001-systemagent-optimization/design/会话记录适配器参考设计/`
- `SDD/project/projects/PRJ-0001-systemagent-optimization/design/优化/`

用户确认：

- OpenCode 只是支持，暂时没有使用 OpenCode。
- 采用推荐方案并执行。

本机参考项目验证：

- `codbash stats` 能扫描当前本机 Claude / Codex sessions。
- `codbash list 5` 能列出当前仓最近会话。
- `codlogs-sessions.cjs --help` 支持 `--json`、`--md`、`--html`、`--include-tool-results`。
- `tracebase` 本地 clone 缺依赖时无法零安装运行，因此仅参考复盘维度。

边界：

- Git Boundary：`/home/slime/Code/SlimeAI/SlimeAI`。
- Worktree：none。原因：当前是小型工具 + SDD 文档实现，用户要求直接执行；工作区已有用户/其它任务改动，不能自动搬移或清理。
- Submodule Boundary：none。本任务不进入 `Games/*/SlimeAI`。

## Design

### Tool Location

```text
Workspace/SystemAgent/Tools/session-adapter/
  session_adapter.py
  README.md
```

### CLI Surface

```bash
python3 Workspace/SystemAgent/Tools/session-adapter/session_adapter.py list --repo . --limit 20
python3 Workspace/SystemAgent/Tools/session-adapter/session_adapter.py index --session <id>
python3 Workspace/SystemAgent/Tools/session-adapter/session_adapter.py summarize --session <id>
python3 Workspace/SystemAgent/Tools/session-adapter/session_adapter.py export-codex-month --source-root /home/slime/.codex/sessions/2026/06
```

`summarize` 是 `index` 的可读摘要入口，默认生成或刷新 sidecar 和 index entry。
`export-codex-month` 是 Codex 专项高保真入口，按 `Workspace/DocsAI/ChatHistory/YYYY/MM/DD/` 输出可见 transcript Markdown。

### Source Strategy

Discovery：

- 优先调用 `codbash list <limit>`。
- 解析输出中的 agent、session id、时间、标题、项目路径。
- 工具缺失时给出明确错误，不自动安装依赖。

Codex high-fidelity：

- `export-codex-month` 流式读取 Codex JSONL，导出可见 message、tool call、tool output、event payload 和 turn context。
- 隐藏推理以 `encrypted_content` 存储时不可读；导出只记录 bytes 和 sha256，不把它当成完整思考过程。
- `index/summarize` 仍生成 summary-level sidecar，标记 `Evidence Level: summary`。

OpenCode：

- 第一版保留 `source_tool=opencode` 和 `opencode export <sessionID>` 作为后续 fallback 文档，不要求当前本机样例。

### Output

```text
Workspace/DocsAI/ChatHistory/
  index.json
  2026-06-09-1249-codex-游戏开发流程agent-019eaab6.md
  2026/06/09/2026-06-09-1249-codex-游戏开发流程agent-019eaab6bfe77.md
```

Index entry 最小字段：

- `id`
- `source_tool`
- `source_adapter`
- `session_id`
- `title`
- `slug`
- `cwd`
- `started_at`
- `updated_at`
- `chat_history_path`
- `source_path`
- `evidence_level`
- `tags`

`evidence_level` 枚举：

- `summary`：摘要级恢复入口，会截取长内容。
- `visible-transcript`：Codex 可见 transcript Markdown，不对可见 message / tool output 做摘要截断。

Markdown sidecar 最小段落：

- Metadata
- First Prompt
- User Goal
- Decisions
- Tool Evidence
- Key Command Outputs
- Files Touched
- Validation Evidence
- Open Questions
- Final State
- Resume Prompt

### Defaults

- 中文 slug 允许，但限制长度。
- 临时导出默认放 `.ai-temp/session-adapter/`。
- 第一版只处理指定 session 或最近 N 个，不全量导入历史。
- ChatHistory 是派生证据，不是 SystemAgent 正文规则事实源。

## Verification

Owner scope：SystemAgent tool + SDD artifact。

必须验证：

```bash
python3 Workspace/SystemAgent/Tools/session-adapter/session_adapter.py list --repo . --limit 5
python3 Workspace/SystemAgent/Tools/session-adapter/session_adapter.py index --session <latest-session-id>
python3 Workspace/SystemAgent/Tools/session-adapter/session_adapter.py summarize --session <latest-session-id>
python3 Workspace/SDD/sdd.py validate SDD-0039
python3 Workspace/SDD/sdd.py validate --root SDD/project/projects/PRJ-0001-systemagent-optimization --all
```

全量 `validate --all` 可运行，但当前 PRJ-0002 存在已知非本轮失败时只记录边界，不把它作为 SDD-0039 阻塞。

通过标准：

- `list` 能列出当前仓至少一个 Claude 或 Codex 会话，或在无会话时输出明确诊断。
- `index/summarize` 能创建/更新 `Workspace/DocsAI/ChatHistory/index.json` 和一个 Markdown sidecar。
- sidecar 不包含完整原始 transcript，只包含摘要和 source locator。
- `export-codex-month` 导出的 Markdown 包含 `Evidence Level: visible-transcript`、`Source SHA256`、事件统计、tool output 和隐藏推理占位。
- `SDD-0039` 专项校验通过。
