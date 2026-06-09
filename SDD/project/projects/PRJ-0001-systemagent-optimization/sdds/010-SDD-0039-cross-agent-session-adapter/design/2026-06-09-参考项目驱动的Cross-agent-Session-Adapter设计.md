# 参考项目驱动的 Cross-agent Session Adapter 设计

> 状态：current
> 日期：2026-06-09
> 任务来源：用户要求基于 `Workspace/Resources/tool` 已 clone 的三个项目继续深度分析，并在 PRJ-0001 design 下新建目录生成详细设计文档。
> 设计裁决：参考项目，不 fork 项目；完善 SystemAgent 内部 adapter，不额外套外层 agent。

## Goal

本设计要解决的问题：

- 把 Claude Code / Codex / OpenCode 的本地会话记录从“难找的 session id、JSONL、SQLite、工具私有路径”变成 SlimeAI 可恢复、可复盘、可审查的 ChatHistory sidecar。
- 基于现成项目提炼实现边界，而不是从零写完整 transcript parser，也不是魔改 `codbash` 成新的工作台。
- 让后续 SDD 可以直接实现一个小而稳的 `session-adapter`：列出、定位、导出、摘要、索引当前仓或指定会话。

非目标：

- 不做外层 AI CLI orchestrator。
- 不替代 Codex / Claude Code / OpenCode 的 resume / fork / session 管理协议。
- 不修改原始 session 文件名、原始 JSONL、OpenCode SQLite、Codex `session_index.jsonl` 或 Claude 会话文件。
- 不复制完整原始 transcript 到仓库。
- 不采集或还原私有 chain-of-thought。
- 不第一阶段接自动 hook、watcher、MCP、dashboard 或 git checkpoint。
- 不把 `codbash` / `codlogs` / `tracebase` 的源码 vendor 到 SlimeAI。

## Context Read

已读项目事实源：

- `SDD/project/projects/PRJ-0001-systemagent-optimization/README.md`
- `SDD/project/projects/PRJ-0001-systemagent-optimization/design/INDEX.md`
- `SDD/project/projects/PRJ-0001-systemagent-optimization/Core/roadmap.md`
- `SDD/project/projects/PRJ-0001-systemagent-optimization/Core/progress.md`
- `SDD/project/projects/PRJ-0001-systemagent-optimization/design/优化/2026-06-08-SystemAgent工作流内化与核心优化裁决.md`
- `SDD/project/projects/PRJ-0001-systemagent-optimization/design/会话记录适配器参考设计/2026-06-08-AI会话管理工具选型分析.md`
- `Workspace/SystemAgent/Actors/DeepThink.md`
- `Workspace/SystemAgent/Actors/DesignCritic.md`

已读参考项目：

- `Workspace/Resources/tool/codbash/README.md`
- `Workspace/Resources/tool/codbash/src/data.js`
- `Workspace/Resources/tool/codbash/src/handoff.js`
- `Workspace/Resources/tool/codlogs/README.md`
- `Workspace/Resources/tool/tracebase/README.md`

Git boundary：

- 当前仓：`/home/slime/Code/SlimeAI/SlimeAI`
- 参考项目位置：`Workspace/Resources/tool/*`
- 参考项目当前作为本地研究输入，不在本设计中修改。

## Evidence / Search Coverage

### 本地运行证据

在当前仓运行：

```bash
node Workspace/Resources/tool/codbash/bin/cli.js stats
```

结果摘要：

- Total sessions: 446
- Claude sessions: 152
- Codex sessions: 294
- OpenCode sessions: 0
- Top projects 包含 `~/Code/SlimeAI/SlimeAI` 97 sessions

在当前仓运行：

```bash
node Workspace/Resources/tool/codbash/bin/cli.js list 5
```

结果摘要：

- 能列出当前仓最近 Codex / Claude 会话。
- session 列表已经包含可读标题、时间、工具类型和项目路径。

在当前仓运行：

```bash
node Workspace/Resources/tool/codlogs/codlogs-sessions.cjs --help
```

结果摘要：

- 支持 `--json` 机器可读列表。
- 支持 `--md FILE.jsonl` 和 `--html FILE.jsonl`。
- 支持 `--include-tool-results` 导出 tool calls 和 tool outputs。
- 支持 `--codex-home PATH`、`--cwd-only`。

在当前仓运行：

```bash
node Workspace/Resources/tool/tracebase/bin/traces.js --help
```

结果：失败，缺少 `jszip` 模块。这个失败不代表项目不可用，只说明本地 clone 不是零安装即可用；第一阶段不应把它作为默认运行依赖。

### `codbash` 证据

`Workspace/Resources/tool/codbash/README.md:20` 到 `README.md:29` 的 Supported Agents 表显示：

- Claude Code: JSONL，支持 Sessions / Preview / Search / Live Status / Convert / Handoff / Launch。
- Codex CLI: JSONL，支持 Sessions / Preview / Search / Live Status / Convert / Handoff / Launch。
- OpenCode: SQLite，支持 Sessions / Preview / Search / Live Status / Launch。

`Workspace/Resources/tool/codbash/README.md:62` 到 `README.md:75` 显示 CLI 包含：

- `codbash run`
- `codbash search <query>`
- `codbash show <session-id>`
- `codbash handoff <id> [target] [--verbosity=full] [--out=file.md]`
- `codbash convert`
- `codbash list`
- `codbash stats`
- `codbash export/import`

`Workspace/Resources/tool/codbash/README.md:80` 到 `README.md:94` 显示数据源包括：

- `~/.claude/`
- `~/.codex/`
- `~/.local/share/opencode/opencode.db`
- Cursor、Pi、Kiro、Copilot Chat 等其它来源。

`Workspace/Resources/tool/codbash/src/data.js:3536` 到 `data.js:3665` 显示 `loadSessionDetail` 会按不同格式加载 detail，但对 Claude / Codex 的消息做截断：

- 单条 content 截到 2000 字符。
- 返回 messages 截到 200 条。

`Workspace/Resources/tool/codbash/src/handoff.js:7` 到 `handoff.js:12` 显示 handoff verbosity 最大 `full: 50`，`handoff.js:26` 到 `handoff.js:28` 只取最近 `msgLimit` 条，`handoff.js:80` 单条内容在 full 下仍截到 3000 字符。

结论：`codbash` 很适合第一阶段的跨工具发现、搜索、定位、人工入口和轻量 handoff；但它的 handoff/detail 不是完整复盘证据，不能作为最终 ChatHistory 的唯一来源。

### `codlogs` 证据

`Workspace/Resources/tool/codlogs/README.md:15` 到 `README.md:18` 显示 `codlogs` 是 read-only Codex session tool，包含 CLI 和桌面浏览器。

`Workspace/Resources/tool/codlogs/README.md:43` 到 `README.md:67` 显示 CLI 支持：

- `--json`
- `--md`
- `--html`
- `--include-images`
- `--include-tool-results`
- `--cwd-only`
- `--codex-home PATH`

`Workspace/Resources/tool/codlogs/README.md:125` 到 `README.md:144` 显示它专门处理大 Codex session：

- 不整体加载大文件。
- bounded JSONL scanning。
- 大 session 自动跳过深分析但允许手动 bounded scan。
- Markdown / HTML export 采用 streaming，避免导出时整文件读入。

`Workspace/Resources/tool/codlogs/README.md:146` 到 `README.md:184` 显示 sanitize 是派生副本，不修改源文件，并明确保留 Codex resume 兼容相关行。

结论：`codlogs` 不适合作跨 Claude / OpenCode 总入口，但适合 Codex 精细导出、大 session、tool outputs、token/error summary 和保留 resume 兼容性的设计参考。

### `tracebase` 证据

`Workspace/Resources/tool/tracebase/README.md:23` 到 `README.md:25` 显示 Tracebase 是 local-first trace capture/inspection，面向本地 coding-agent logs，不抓隐藏推理。

`Workspace/Resources/tool/tracebase/README.md:31` 到 `README.md:37` 显示它支持：

- 导入 Codex / Claude transcript。
- raw events 本地加密。
- localhost dashboard。
- redacted exports。
- failure、resteer、context waste、repeated commands、scorecards。
- CLI、API、MCP、wrappers/hooks。

`Workspace/Resources/tool/tracebase/README.md:43` 到 `README.md:52` 的问题表很贴近 SystemAgent 复盘：

- 是否实际跑测试。
- 哪里卡住。
- 消耗多少 token。
- 哪些文件或工具重要。
- 是否安全分享。
- 一个 run 是否比另一个 run 更好。

`Workspace/Resources/tool/tracebase/README.md:76` 到 `README.md:82` 显示它捕获 Codex JSONL、Claude JSONL、Claude hooks、wrappers、live intake，并明确不捕获 hidden/private chain-of-thought，除非 provider 显式输出。

结论：`tracebase` 的分析模型很有价值，但范围比第一阶段需求大；应该参考其复盘维度，而不是先引入它的存储、dashboard、MCP 和 hook/watch 工作流。

## Problem Reality Check

问题真实存在：

- `codbash stats/list` 已证明本机当前就有数百个 Claude / Codex session，且当前仓有大量可恢复会话。
- 原始会话路径、UUID 和工具私有数据源不适合作为人和 AI 的长期恢复入口。
- `codbash` 可以找和看，但 detail/handoff 有截断；如果直接把它当最终复盘证据，会丢失重要命令返回、tool outputs 和上下文。
- `codlogs` 可以精细处理 Codex，但不能覆盖 Claude Code / OpenCode。
- `tracebase` 可以做复盘分析，但第一阶段引入成本大，而且本地 clone 没安装依赖时无法直接运行 help。

需要校正的旧想法：

- “魔改 `codbash`”不是最优路径。`codbash` 是上游产品，不应让 SlimeAI 的 workflow 绑定到它的 UI、launch、delete、import/export 和内部截断策略。
- “只找 `.codex` 原始文件”也不够。它只能处理 Codex，而且恢复体验差。
- “直接抓完整思考过程”不可作为目标。公开可审查的是 transcript、assistant 消息、tool evidence、命令、验证结果和用户可见摘要。

## Idea Check

用户当前修正后的方向成立：

- 参考现有项目，而不是另起一套 AI CLI manager。
- 优先完善 SystemAgent 内部 workflow 和 artifact。
- 第一阶段跨 Claude Code / Codex / OpenCode。
- 获取和整理拆成两层：现成工具负责获取/发现，SlimeAI 负责结构化摘要、命名和 SDD 复盘字段。

需要继续保持克制：

- 不要把 `codbash` 当要魔改的主产品。
- 不要为了“完整”引入 `tracebase` 的全套 trace 平台。
- 不要为了一次性解决历史问题而全量处理所有 session。
- 不要把 ChatHistory 变成 SystemAgent 的新规则事实源；它只是 evidence ledger / persistent review。

## Reference Adoption Matrix

| Project | Adopt | Do Not Adopt | Why |
| --- | --- | --- | --- |
| `codbash` | 跨工具发现、`list/search/show` 入口、session id 定位、轻量 handoff、数据源覆盖思路 | dashboard 作为事实源、delete、launch、resume 自动化、export/import、截断 handoff 作为最终证据、上游源码 fork | 覆盖 Claude / Codex / OpenCode，最接近第一阶段需求，但 detail/handoff 有截断，且产品范围大于 SystemAgent adapter |
| `codlogs` | Codex `--json`、`--md`、`--html`、`--include-tool-results`、大 session streaming、sanitize 不改源文件的原则 | 作为跨工具总入口、桌面 GUI、写回 Codex title / re-add session 作为默认能力 | Codex 保真和大文件经验强，但 Codex-only |
| `tracebase` | run scorecard、context waste、failure/recovery、redacted export、测试证据、文件/工具重要性等分析维度 | 第一阶段存储、dashboard、MCP、hook/watch、encrypted raw store、wrappers | 复盘模型有价值，但接入面和概念过重 |

## Key Decision

### 2026-06-09 补充裁决：summary 不等于完整复盘证据

用户指出第一版 ChatHistory sidecar 截断过多，这个问题真实存在。修正后的证据分层：

- `summary`：恢复入口，只保留摘要、source locator 和关键结论，不适合作完整 AI 复盘。
- `visible-transcript`：Codex 可见 transcript 导出，保留可见 message、tool call、tool output、event payload 和 turn context，不做摘要截断。

边界仍然成立：

- 不复制原始 JSONL 到仓库。
- 不还原或伪造 `encrypted_content` 中的隐藏推理；只记录 bytes 和 sha256。
- Claude / OpenCode 高保真导出后续另行补充，不用 Codex 专项逻辑冒充跨工具完整方案。

### D1：参考项目，不 fork 项目

裁决：Adopt。

SlimeAI `session-adapter` 是薄层协议和脚本，不是 `codbash` fork。它只做：

- 调用或消费现成工具输出。
- 统一字段。
- 生成可读文件名。
- 生成 ChatHistory sidecar。
- 把摘要引用写回 SDD progress / notes。

### D2：第一阶段获取入口以 `codbash` 为主，但证据保真不能依赖 handoff

裁决：Adopt with guard。

`codbash` 负责：

- `stats`：确认本机覆盖情况。
- `list`：按最近会话、工具、项目路径筛选候选。
- `search`：用关键词找会话。
- `show`：人工快速查看。
- `handoff`：生成轻量恢复文档。

但 `codbash handoff` 有消息条数和单条长度截断，所以：

- 可以作为初始摘要源。
- 不能作为最终复盘证据源。
- 对 Codex 需要高保真时，调用 `codlogs --md --include-tool-results`。
- 对 Claude / OpenCode 需要高保真时，第一阶段记录 source path 和 `codbash` 可见摘要，后续再补官方导出或专用 exporter。

### D3：第一阶段强制支持三类 agent，扩展能力只做可选

强制支持：

- Claude Code
- Codex
- OpenCode

可选支持：

- Cursor
- Copilot Chat
- Qwen
- Pi / Oh My Pi
- Kiro / Kilo
- Gemini

原因：用户明确修正跨 Claude Code / Codex / OpenCode 是第一阶段硬边界。其它工具可以从 `codbash` 获得列表能力，但不进入验收。

### D4：ChatHistory 是派生证据，不是原始日志仓库

裁决：Adopt。

长期路径：

```text
Workspace/DocsAI/ChatHistory/
  index.json
  2026-06-09-1249-codex-游戏开发流程agent-019eaab6.md
  YYYY/MM/DD/YYYY-MM-DD-HHmm-codex-<slug>-<short-session-id>.md
```

不保存：

- 完整原始 JSONL。
- OpenCode SQLite 副本。
- Claude / Codex / OpenCode 私有内部索引。
- 私有 chain-of-thought。

保存：

- 原始来源路径或 source locator。
- 可读标题。
- 首个用户 prompt 摘要。
- 当前任务结论。
- 关键命令和验证证据。
- 关键 tool failure。
- 文件改动摘要。
- 设计决策。
- 未解决问题。
- 后续恢复提示。
- Codex `visible-transcript` 导出中的可见 tool output 原文。

## Adapter Architecture

### Layer 1：Source Discovery

职责：发现本地 session，输出统一 session candidate。

首选输入：

```bash
codbash list <limit>
codbash search <query>
codbash stats
```

Codex 专项输入：

```bash
codlogs-sessions <repo> --json
```

OpenCode fallback：

```bash
opencode session list
opencode export <sessionID>
```

Claude fallback：

```text
~/.claude/projects/**/*.jsonl
```

第一阶段不直接写自研 parser 解析所有格式。只有当某工具无法给出必要字段时，才做最小 source locator 补齐。

### Layer 2：Evidence Export

职责：把候选 session 变成可审查输入。

默认：

```bash
codbash handoff <id> --verbosity=full --out=<tmp.md>
```

Codex 高保真：

```bash
codlogs-sessions --md <session.jsonl> --include-tool-results
```

OpenCode 高保真：

```bash
opencode export <sessionID>
```

导出策略：

- 临时导出放 `.ai-temp/session-adapter/`。
- 生成 ChatHistory 后删除或保留临时导出由实现 SDD 决定。
- ChatHistory 只保留摘要和 source locator，不默认保留完整导出。

### Layer 3：Normalization

职责：统一 schema。

最小字段：

```json
{
  "schema_version": 1,
  "source_tool": "codex",
  "source_adapter": "codbash",
  "session_id": "019ea26e-fb1",
  "source_path": "/home/slime/.codex/sessions/...",
  "cwd": "/home/slime/Code/SlimeAI/SlimeAI",
  "started_at": "2026-06-09T11:39:00+08:00",
  "updated_at": "2026-06-09T12:20:00+08:00",
  "first_prompt": "游戏开发流程Agent...",
  "title_slug": "游戏开发流程agent",
  "chat_history_path": "Workspace/DocsAI/ChatHistory/2026-06-09-1139-codex-游戏开发流程agent-019ea26e.md",
  "source_confidence": "partial",
  "evidence_level": "summary"
}
```

枚举：

```text
source_tool: claude | codex | opencode | cursor | qwen | copilot | unknown
source_adapter: codbash | codlogs | opencode-cli | claude-jsonl | manual
source_confidence: full | partial | locator-only
evidence_level: full-export | tool-results | summary | locator-only
```

### Layer 4：SystemAgent Summary

职责：把 transcript evidence 转成 SlimeAI 可复盘字段。

Markdown sidecar 格式：

```markdown
# <title>

## Metadata

- Source Tool:
- Source Adapter:
- Session ID:
- Source Path:
- CWD:
- Started:
- Updated:
- Evidence Level:

## First Prompt

## User Goal

## Decisions

## Tool Evidence

## Key Command Outputs

## Files Touched

## Validation Evidence

## Open Questions

## Final State

## Resume Prompt
```

字段原则：

- `Tool Evidence` 记录工具名、命令、成功/失败、关键输出摘要。
- `Key Command Outputs` 不塞完整日志，只保留错误、验证摘要、关键数值和路径。
- `Files Touched` 来自 transcript、git diff、工具输出或人工摘要，必须标注来源。
- `Validation Evidence` 必须包含命令和结果；没有验证就写 `not run` 和原因。
- `Resume Prompt` 是给下一次对话使用的短恢复提示，不是完整 transcript。

### Layer 5：Index and Lookup

职责：维护 `index.json`，支持快速找会话。

建议索引结构：

```json
{
  "schema_version": 1,
  "updated_at": "2026-06-09T12:30:00+08:00",
  "entries": [
    {
      "id": "codex:019ea26e-fb1",
      "source_tool": "codex",
      "session_id": "019ea26e-fb1",
      "title": "游戏开发流程Agent",
      "slug": "游戏开发流程agent",
      "cwd": "/home/slime/Code/SlimeAI/SlimeAI",
      "started_at": "2026-06-09T11:39:00+08:00",
      "updated_at": "2026-06-09T12:20:00+08:00",
      "chat_history_path": "Workspace/DocsAI/ChatHistory/2026-06-09-1139-codex-游戏开发流程agent-019ea26e.md",
      "source_path": "/home/slime/.codex/sessions/...",
      "tags": ["systemagent", "session-adapter"]
    }
  ]
}
```

写入规则：

- 同一个 `source_tool + session_id` 重复导入时更新 entry，不重复创建。
- 旧 sidecar 不自动删除，除非实现 SDD 明确加入 rotate / prune。
- 文件名只基于派生 sidecar，不影响原始 session。

## Proposed CLI Surface

工具位置建议：

```text
Workspace/SystemAgent/Tools/session-adapter/
```

命令建议：

```bash
python3 Workspace/SystemAgent/Tools/session-adapter/session_adapter.py list --repo .
python3 Workspace/SystemAgent/Tools/session-adapter/session_adapter.py inspect <session-id>
python3 Workspace/SystemAgent/Tools/session-adapter/session_adapter.py export <session-id>
python3 Workspace/SystemAgent/Tools/session-adapter/session_adapter.py index <session-id>
python3 Workspace/SystemAgent/Tools/session-adapter/session_adapter.py summarize <session-id>
```

最小可交付命令：

```bash
session_adapter.py list --repo . --limit 20
session_adapter.py index --session <id>
session_adapter.py summarize --session <id>
```

实现语言建议：Python。

原因：

- 当前 `Workspace/SDD` 和 SlimeAI 工具链已有 Python 使用基础。
- 便于写 JSON、Markdown、路径处理、subprocess wrapper 和测试。
- 不额外引入 Node 依赖。

外部工具调用策略：

- 优先调用本地 clone 中的 JS CLI 或全局命令。
- 不能假设用户已经 `npm i -g codbash-app`。
- 工具缺失时输出 actionable diagnostic，不失败成空文件。
- 不自动安装依赖。

## Implementation Phases

### Phase 0：Design Freeze

交付：

- 当前设计文档。
- PRJ-0001 `design/INDEX.md`、`roadmap.md`、`progress.md` 更新。

验收：

- SDD validate 通过。
- 用户确认是否进入独立 SDD 实现。

### Phase 1：Read-only Prototype

目标：当前仓最近 session 可列出、可选中、可生成 sidecar。

范围：

- 只支持手动指定 session id 或最近 N 个。
- `codbash list/stats/search` 做发现。
- Codex 可选接 `codlogs --json`。
- 输出 `Workspace/DocsAI/ChatHistory/index.json` 和 Markdown。

不做：

- 全量历史导入。
- 自动 hook。
- OpenCode 深导出保真。
- tracebase scorecard。

验收样例：

```bash
python3 Workspace/SystemAgent/Tools/session-adapter/session_adapter.py list --repo . --limit 10
python3 Workspace/SystemAgent/Tools/session-adapter/session_adapter.py index --session 019ea26e-fb1
python3 Workspace/SystemAgent/Tools/session-adapter/session_adapter.py summarize --session 019ea26e-fb1
```

### Phase 2：Codex High-fidelity Export

目标：对 Codex session 提供 tool result 可审查摘要。

范围：

- 调用 `codlogs-sessions --md <jsonl> --include-tool-results`。
- 提取关键 tool call、失败命令、验证证据。
- 大 session 时标记 `partial`，不强行全文读入。

验收：

- 对一个大 Codex JSONL 不崩溃。
- sidecar 明确写 `Evidence Level`。
- 缺 tool output 时标记 Unknown，而不是假装完整。

### Phase 3：Claude / OpenCode Export Fallback

目标：补齐 Claude Code / OpenCode 的高保真或官方导出路径。

范围：

- Claude：优先消费 `codbash` 可见信息和 source locator；必要时做最小 JSONL reader，但只解析公开 user/assistant/tool fields。
- OpenCode：优先 `opencode export <sessionID>`。
- 不做多工具完整 parser 框架。

验收：

- 每类 agent 至少有一个真实或 fixture 样例。
- 失败时保留 locator-only entry。

### Phase 4：Review Intelligence

目标：参考 `tracebase` 做轻量复盘分析。

范围：

- repeated commands。
- failed tool calls。
- validation missing。
- context waste candidates。
- run scorecard。
- redaction checklist。

仍不做：

- 引入 tracebase store。
- MCP。
- watcher。
- dashboard。

## Risks

| Risk | Severity | Mitigation |
| --- | --- | --- |
| `codbash` CLI 输出偏人读，不稳定 | high | 第一阶段先人工可用和 markdown handoff；机器解析优先找 JSON/API 或在 wrapper 内做最小兼容层，解析失败时降级 locator-only |
| `codbash handoff` 截断导致证据缺失 | high | sidecar 标记 `Evidence Level: summary`；Codex 用 `codlogs --include-tool-results` 补高保真 |
| 原始 session 格式变化 | medium | 不改原始格式；只记录 source locator；parser 最小化；失败时不破坏索引 |
| ChatHistory 变成新垃圾场 | medium | 默认只处理指定 session / 最近 N 个；不全量导入；sidecar 格式限制长度 |
| 自动 hook 引入稳定性问题 | medium | 第一阶段不接 hook；后续只允许轻量 queue 或提醒 |
| 隐私和敏感输出进入仓库 | high | 不保存完整 transcript；增加 redaction checklist；大输出只摘要；敏感内容标记 omitted |
| 多工具支持范围膨胀 | medium | 第一阶段硬验收仅 Claude Code / Codex / OpenCode；其它工具只通过 `codbash` 可选显示 |

## DesignCritic Review

Assumptions：

- 本地 clone 的三个项目可作为参考，但不作为可信稳定 API。
- 当前用户更需要恢复、索引、摘要和复盘，不需要新 dashboard。
- `Workspace/DocsAI/ChatHistory/` 适合作为派生 sidecar 位置。

Missing Context：

- OpenCode 当前本机没有 session 样例，`codbash stats` 显示 OpenCode sessions 为 0。
- Claude Code 高保真 tool result 导出路径尚未实测。
- `codbash` 是否存在稳定 localhost API 尚未验证。

Design Defects to Avoid：

- 把 `codbash handoff` 当完整 transcript。
- 为了跨工具支持自写过大的 parser。
- 把 trace 分析平台提前引入，导致第一阶段无法落地。
- 让 ChatHistory 写满完整日志，造成仓库污染和隐私风险。

Better Option Checked：

- 仅人工使用 `codbash`：成本最低，但不能形成 SlimeAI SDD 复盘证据。
- 直接用 `tracebase`：分析能力强，但第一阶段过重。
- 只用 `codlogs`：Codex 体验好，但不满足跨 Claude / OpenCode。

Recommendation：

采用薄层 `session-adapter`，以 `codbash` 解决发现和跨工具入口，以 `codlogs` 解决 Codex 保真，以 `tracebase` 维度指导后续复盘分析。

## Must Confirm

### 思路问题

- 当前没有必须阻塞实现的思路问题；“参考项目并实现 SlimeAI 薄层 adapter”的方向成立。

### 信息缺口

- 是否需要在第一版就准备 OpenCode 真实样例？为什么问：当前本机 `codbash stats` 显示 OpenCode sessions 为 0。默认值：第一版支持 OpenCode locator/export 代码路径，但真实验收可等用户有 OpenCode session 后补。
- Claude Code 是否必须第一版保留完整 tool outputs？为什么问：当前 `codbash` detail/handoff 会截断。默认值：第一版保留 source locator + summary，完整 tool outputs 后续补。

### 决策未定

- 是否创建独立 SDD 实现 `session-adapter`？为什么问：本文只是设计文档，不直接写工具。默认值：用户确认后创建独立 SDD。

## Should Confirm

- ChatHistory 是否仍放 `Workspace/DocsAI/ChatHistory/`？默认值：是。
- Sidecar 文件是否允许包含中文 slug？默认值：允许，来源是首个提示词；同时限制长度。
- 临时高保真导出是否保留在 `.ai-temp/session-adapter/`？默认值：默认保留到当前任务结束，后续可清理。
- 是否需要把 `session-adapter` 接入 SystemAgent retrospective？默认值：先不接，等工具稳定后在复盘流程中提示可选运行。

## Defaults I Will Use

- 第一阶段强制支持 Claude Code / Codex / OpenCode。
- 不改上游源码。
- 不改原始 session 文件名。
- 不接 hook 自动抓取。
- 不全量处理历史。
- 不保存完整原始 transcript。
- `codbash` 作为跨工具发现入口。
- `codlogs` 作为 Codex 高保真补充。
- `tracebase` 只参考复盘维度。
- 输出位置为 `Workspace/DocsAI/ChatHistory/`。
- 设计实现进入独立 SDD 后再写工具代码。

## Not Recommended

- 不推荐魔改 `codbash`：上游产品范围过大，且 detail/handoff 截断不是 SlimeAI 能靠魔改稳定解决的根本问题。
- 不推荐直接接 `tracebase`：第一阶段过重，会引入 store、dashboard、MCP、watcher、dependency 管理。
- 不推荐自写完整 cross-agent parser：重复造轮子，格式变化风险高。
- 不推荐自动 hook 抓取：会重复 PRJ-0001 已规避的 hook 稳定性问题。
- 不推荐改原始 session 文件名：可能破坏 resume / index / archived sessions。

## Artifact Updates

本轮写入：

- `SDD/project/projects/PRJ-0001-systemagent-optimization/design/会话记录适配器参考设计/INDEX.md`
- `SDD/project/projects/PRJ-0001-systemagent-optimization/design/会话记录适配器参考设计/2026-06-09-参考项目驱动的Cross-agent-Session-Adapter设计.md`

本轮同步：

- `SDD/project/projects/PRJ-0001-systemagent-optimization/design/INDEX.md`
- `SDD/project/projects/PRJ-0001-systemagent-optimization/Core/roadmap.md`
- `SDD/project/projects/PRJ-0001-systemagent-optimization/Core/progress.md`
