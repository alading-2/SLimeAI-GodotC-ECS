# SystemAgent 工作流内化与会话记录优化

> 状态：current
> 日期：2026-06-08
> 来源：用户对 `Workspace/DocsAI/思考/2026-06-07-游戏开发SystemAgent流程Agent深度分析.md` 的复盘和方向校准
> 定位：PRJ-0001 补充设计裁决；不是实现任务，不直接修改 `Workspace/SystemAgent/` 正文。

## Goal

本设计只记录当前最关键的 SystemAgent 优化方向：

- 不额外写一个外层 agent 去管理 AI CLI。
- 不魔改 Warp 或从零做 IDE / terminal 替代品。
- 在当前 AI CLI 内通过项目里的 SystemAgent、SDD、DocsAI、hook、subagent 把工作流跑通。
- 把 Claude Code / Codex / OpenCode 对话记录整理成可查、可复盘的结构化证据，而不是追求完整私有思考过程。
- 用只读 subagent 承担资料搜索和大上下文阅读，主对话只吸收结论和证据。
- 优先复用现成 session / transcript / trace 工具；SystemAgent 只补项目语义摘要、SDD 复盘字段和落盘协议。

非目标：

- 不设计写入型 multi-agent dispatcher。
- 不让 AI 自动拆所有任务；大任务仍由设计交流和 SDD 拆分，实际执行一个个推进。
- 不自动创建、清理或合并 worktree。
- 不复制完整 AI CLI 原始 session 文件、SQLite 数据库或 JSONL 到仓库作为长期事实源。
- 不从零重写 Claude Code / Codex / OpenCode / Cursor / Gemini transcript parser。

## Context Read

已读取：

- `Workspace/SystemAgent/Actors/DeepThink.md`
- `Workspace/SystemAgent/Actors/DesignCritic.md`
- `Workspace/SystemAgent/Rules/Subagent.md`
- `Workspace/SystemAgent/Routes/ResearchAdoption.md`
- `Workspace/SystemAgent/Routes/WorkflowIteration.md`
- `SDD/project/projects/PRJ-0001-systemagent-optimization/README.md`
- `SDD/project/projects/PRJ-0001-systemagent-optimization/Core/roadmap.md`
- `SDD/project/projects/PRJ-0001-systemagent-optimization/Core/progress.md`
- `SDD/project/projects/PRJ-0001-systemagent-optimization/design/1/03-Hook与Gate重写方案.md`
- `SDD/project/projects/PRJ-0001-systemagent-optimization/design/1/04-Git与Worktree策略.md`
- `SDD/project/projects/PRJ-0001-systemagent-optimization/design/1/10-Subagent使用场景与采纳策略.md`
- `Workspace/DocsAI/思考/2026-06-07-游戏开发SystemAgent流程Agent深度分析.md`
- Codex manual 本地缓存：`/tmp/openai-docs-cache/codex-manual.md`
- `Workspace/SystemAgent/Routes/ResearchAdoption.md`
- `Workspace/SystemAgent/Actors/ResearchAnalyst.md`
- `Workspace/SystemAgent/Rules/Boundary.md`

本轮外部资源记录：

```yaml
externalResources:
  enabled:
    - web
    - github
    - context7
  scope:
    - Claude Code / Codex / OpenCode / Cursor / Gemini session browser, transcript export, replay, observability
    - LLM tracing / observability platforms
  reason: 用户明确要求搜索现成工具，避免重复造轮子
  expires: current-task
copiedCodeOrAssets: none
```

当前仓事实：

- `codex --version`：`codex-cli 0.137.0`
- 当前仓存在 `.codex/skills/`，但没有 `.codex/agents/` 和 `.codex/hooks.json`。
- `Workspace/DocsAI/ChatHistory/` 当前为空目录。

## Evidence / Search Coverage

### Evidence

- Codex manual 说明 CLI 支持 `resume` / `fork`，本地 transcript 位于 `~/.codex/sessions/`，`codex exec --json` 可输出 JSONL 事件。
- Codex manual 说明 subagent 默认启用，但只在显式请求时 spawn；它适合并行探索、测试、日志分析和摘要，且会消耗额外 tokens。
- Codex manual 说明 hooks 可在 `UserPromptSubmit`、`PostToolUse`、`Stop` 等事件运行命令，但非托管 hook 需要 review / trust，且 command hook 不支持真正 async。
- Codex manual 说明公开可见审查内容是 retained conversation items 和 tool evidence，不包含 private chain-of-thought。
- PRJ-0001 既有设计已经裁决：subagent 默认只读，主对话负责写入和合并；hook 只做低频安全栏，不承担 workflow 执行；worktree 是隔离工具，不是所有任务的仪式。
- GitHub / web 搜索显示，session 管理和 replay 已有现成工具，不应从零写 parser：
  - `vakovalskii/codbash`：跨 Claude Code / Codex / OpenCode / Cursor 等本地 session，支持 Linux、搜索、查看、replay、tag、handoff、launch；README 和源码都确认 OpenCode SQLite 数据源 `~/.local/share/opencode/opencode.db`。
  - `tobitege/codlogs`：Codex 专用，只读扫描 `~/.codex`，支持按仓库查找 session、JSON 输出、Markdown / HTML 导出、包含 tool result、token usage、错误 tool call 汇总、session title 和 sanitize。
  - `es617/claude-replay`：跨 Claude Code / Cursor / Codex CLI / Gemini CLI / OpenCode 的 transcript replay，支持自动识别 `~/.codex/sessions`、HTML 回放、secret redaction、tool call / file activity 展示。
  - `jazzyalex/agent-sessions`：macOS local-first session browser，覆盖 Codex CLI/Desktop/VS Code、Claude、OpenCode、Cursor、Gemini 等，支持搜索、查看、保存、resume command 和本地索引。
  - `pugliatechs/polpo`：移动端远程控制和 session browser，支持 Codex session spawning、auto-discovery、`codex exec --json`、`~/.codex/sessions` watch 和 resume；Codex 支持仍在稳定中。
  - `graykode/abtop`：只读监控 Claude Code / Codex CLI / OpenCode session、token、上下文、rate limit、进程和端口，适合作运行状态看板，不是复盘摘要系统。
  - `entireio/cli`：把 AI session 与 git commit / checkpoint 关联，支持 Claude Code、Codex、Gemini 等；能力强但会安装 hooks 并把 metadata 放到独立 checkpoint branch，采纳成本高。
- Context7 查到 OpenCode 官方 CLI 文档：`opencode session list` 可列出 session，`opencode export [sessionID]` 可导出 session JSON；`Langfuse` / `Arize Phoenix` 是成熟 LLM tracing / observability 平台，但更适合自建 agent 应用 trace。
- 本机实测 `codlogs`：在当前仓只读扫描到 76 个 Codex sessions，能按 repo root 匹配 `~/.codex/sessions` 并输出 `id/file/cwd/startedAt/threadName`，包括用户提到的历史主题。

### Inference

- 用户现在的方向比 2026-06-07 文档更准确：重点不是做一个新 orchestrator，而是完善项目内 SystemAgent。
- 对话记录问题值得解决，但第一阶段应做“现成工具评估 + SystemAgent 摘要适配”，不是从零写 JSONL parser 或 hook 自动抓取全量日志。
- 搜索资料用 subagent 有必要，但只适合只读研究和总结，不适合默认进入所有任务。
- LLM observability 平台适合自建 agent 应用的运行 trace，不适合直接替代 AI CLI 本地 transcript 管理。

### Unknown

- Claude Code / Codex / OpenCode 原始会话格式是否会在后续版本变化。
- 当前账号 / 当前界面是否稳定支持交互式 `/agent` 管理体验；本文只基于 manual 和本地 CLI 能力判断。
- `codbash` 的 CLI 文本输出是否足够稳定用于机器解析仍需验证；默认先消费 `handoff` Markdown 或后续验证 localhost API。
- `codlogs` 是否适合作为 Codex 专项长期依赖仍需确认安装方式和版本锁定；当前仓根目录有 MIT `LICENSE`，但 `package.json` 标记为 `UNLICENSED`，正式 vendor 或发布前需要再次确认许可证口径。

## External Sources

本轮只采纳能力形状和边界，不复制外部代码、prompt 或资产。

| Source | Type | Decision | Notes |
| --- | --- | --- | --- |
| <https://github.com/vakovalskii/codbash> | Cross-agent local session dashboard / CLI | Adopt Now as first-phase candidate | Linux 可用，覆盖 Claude Code / Codex / OpenCode，提供 search/show/handoff/replay/tag/launch；SystemAgent 先只使用只读查看和 handoff。 |
| <https://opencode.ai/docs/cli> | OpenCode official CLI docs | Evidence | 官方文档确认 `opencode session list` 和 `opencode export [sessionID]`，可作为 OpenCode fallback 导出路径。 |
| <https://github.com/tobitege/codlogs> | Codex session CLI / desktop browser | Adopt as Codex specialist | Codex 专用，只读扫描、JSON 输出、Markdown / HTML 导出、tool result、token usage、错误 tool call、sanitize；仓库有 MIT `LICENSE` 但 `package.json` 写 `UNLICENSED`，正式 vendor 前需确认。 |
| <https://github.com/es617/claude-replay> | Multi-agent transcript HTML replay | Adopt Later as optional review artifact | 适合生成 HTML replay 和人工审查，不作为 SystemAgent 默认事实源。 |
| <https://github.com/jazzyalex/agent-sessions> | macOS local-first session browser | Adopt Later / Reference | 能解决 macOS 上搜索、查看、保存、resume；当前 Linux 环境不作为默认依赖。 |
| <https://github.com/pugliatechs/polpo> | Mobile remote control + session browser | Reference | 能 watch `~/.codex/sessions`、session browser、resume；包含远程控制和网关，范围大于当前需求。 |
| <https://github.com/graykode/abtop> | Read-only agent monitor | Reference | 适合看运行态、token、rate limit、进程端口；不负责复盘摘要。 |
| <https://github.com/entireio/cli> | Git checkpoint + agent session capture | Reject for first phase | 能力强但会接入 git hooks 和 checkpoint branch，采纳成本和隐私边界高。 |
| <https://github.com/langfuse/langfuse> | LLM observability platform | Reference | 适合自建 LLM app / agent trace、eval 和 session_id 分析，不直接替代本地 CLI transcript。 |
| <https://github.com/Arize-ai/phoenix> | LLM observability / evaluation platform | Reference | 与 Langfuse 类似，适合应用 trace 和评估，不是 Codex session browser。 |

## Problem Reality Check

用户提出的问题真实存在，但最小修正方向已经清楚。

当前真正的问题不是“缺一个外层 agent 管理 AI CLI”，而是：

- SystemAgent 的工作流需要更明确地告诉 AI 什么时候用 hook、什么时候用 subagent、什么时候写 SDD artifact。
- Claude Code / Codex / OpenCode 原始会话记录难找、难读、文件名或 session id 不可记忆。
- 大资料搜索会污染主对话上下文。
- 旧文档把 Warp / app-server / full orchestrator 讨论得太多，容易把后续实现带偏。
- 现成工具已经能解决大部分“找 session / 看 transcript / 导出 / replay”问题；SlimeAI 缺的是把这些证据转成项目可用的 `Evidence / Inference / Unknown / Decisions / Validation / Follow-up` 摘要。

## Idea Check

### 成立

- “不应该额外写一个 agent 去管理 AI CLI”成立。外层 agent 会变成新的管理对象，并且与当前项目内 SystemAgent 形成双重事实源。
- “在一个 AI CLI 里把工作流跑通”成立。SlimeAI 已经有 SystemAgent / SDD / DocsAI / skill / hook / subagent 基础，继续完善这一层更稳。
- “可审查的是 conversation items、tool evidence、assistant 消息、命令返回、web/tool 事件，不是私有 chain-of-thought”成立。
- “搜索资料用只读 subagent”成立，尤其适合外部资料、本地 Resources、大量 DocsAI 读取和历史会话总结。

### 需要校正

- “思考过程、命令返回没用”不完全成立。私有思考不可用；命令返回是否有用取决于任务。复盘不需要完整命令返回，但需要验证命令、关键错误、关键输出摘要和工具失败原因。
- “直接找 `.codex` / `.claude` / OpenCode DB 对话记录即可”短期可行，长期不够。原始记录是证据源，不是人和 AI 的恢复入口。
- “自动抓取对话并保存”不应第一阶段做。Codex 已保存原始 session；SystemAgent 只需要生成索引和摘要。hook 自动抓取应等脚本稳定后再评估。
- “自己写一个 session-ledger parser”需要校正。外部工具已经覆盖跨工具搜索、handoff、Codex 专项导出、sanitize 和 HTML replay；SystemAgent 应优先复用或包一层 CLI adapter。

## Main Decisions

### D1：不做外层 AI CLI 管理 agent

裁决：Reject。

原因：

- 新增外层 agent 会带来“谁管理管理者”的问题。
- SlimeAI 项目已经由 AI CLI + SystemAgent 管理，再套一层会形成重复入口。
- 当前更缺的是项目内 workflow、artifact 和复盘规则，不是新运行时。

### D2：不魔改 Warp，不做 Warp 替代品

裁决：Reject。

原因：

- Warp 是工具外壳和工作台方向，不是 SlimeAI 的项目事实源。
- 当前 Codex 已有 `resume`、`fork`、subagent、hook、`exec --json` 等基础能力；OpenCode 也有 `session list` / `export` 作为 session fallback；Claude Code 有本地 JSONL 可被现成工具读取。
- SlimeAI 的差异化在 SystemAgent + SDD + DocsAI + Godot 验证，不在 terminal UI。

### D3：会话记录先复用现成工具，不从零写 parser

裁决：Adopt Now，范围限定为只读 adapter / wrapper 设计。

第一选择：

```text
codbash
```

原因：

- 同时覆盖 Claude Code / Codex / OpenCode，满足用户修正后的第一阶段边界。
- Linux 可用，安装成本低，提供 `list/search/show/handoff`，能直接减少“会话难找、session id 难记”的问题。
- OpenCode 数据源和 `handoff` 命令已从 README、源码和 Context7 官方 OpenCode CLI 文档交叉确认。
- 它不是外层 orchestrator；第一阶段只把它当本地 session 获取 / 搜索 / handoff 工具使用。

Codex 专项补充：

```text
codlogs
```

适合 Codex-only 精细导出、机器可读 session 列表、tool result / token usage / failed tool call 汇总；但不能覆盖 Claude Code / OpenCode，因此不再作为整体 adapter 默认值。

人工 replay 辅助：

```text
claude-replay
```

适合生成可交互 HTML replay，用于分享、审查或教学，不作为 SystemAgent 默认索引源。

可选 GUI：

```text
agent-sessions
```

适合 macOS 本地搜索和 resume 工作台；由于当前环境是 Linux，不能作为默认实现依赖。

SystemAgent 自己只做：

- 调用现有 CLI 或读取其 JSON / Markdown / HTML 输出。
- 不改上游工具源码；只在 wrapper 层统一字段、命名和落盘。
- 生成项目级摘要：
  - `source_tool`
  - `session_id`
  - `source_path`
  - `started_at`
  - `cwd`
  - `first_prompt`
  - `title_slug`
  - `message_summary`
  - `tool_calls_summary`
  - `key_command_outputs`
  - `files_touched_in_session`
  - `validation_evidence`
  - `decisions`
  - `open_questions`
  - `final_summary`
- 将摘要落入 `Workspace/DocsAI/ChatHistory/` 或当前 SDD `progress.md` / `notes.md` 的引用。

不做：

- 不重写 Claude Code / Codex / OpenCode 原始格式解析器。
- 不复制完整原始 session 文件、数据库或导出记录。
- 不保存或推断私有 chain-of-thought。
- 不把 ChatHistory 当 SystemAgent 正文事实源，只作为 Persistent Review。
- 不默认启用写回 Codex `session_index.jsonl`、OpenCode DB、Claude session 文件、sanitize re-add 或 hook 安装。

### D4：hook 只用于轻量索引触发，不做重活

裁决：Adopt Later。

第一阶段不配置 hook 自动抓取。

原因：

- Codex hook 需要 review / trust；Claude Code / OpenCode 侧也不应在未验证前接入自动抓取。
- command hook 不支持真正 async。
- Stop hook 做重活会重复 PRJ-0001 已经解决过的 hook 可靠性风险。

可接受的后续方向：

- `UserPromptSubmit` 只记录一个极小事件到 `.ai-temp/session-tool-adapter/queue.jsonl`。
- `Stop` 只提醒“可运行 session tool adapter index”，不直接解析全量 sessions。
- hook smoke 覆盖后才允许进入项目配置。

### D5：搜索资料用只读 subagent

裁决：Adopt Now，作为 workflow 可选能力，不设为默认步骤。

适用场景：

- 外部资料搜索。
- 本地 Resources / DocsAI 大范围读取。
- 历史会话摘要。
- 独立评审或测试设计。

输出必须固定：

```text
Scope
Evidence
Inference
Unknown
Risks
Recommended Main-Thread Action
Files Touched: none
```

主对话职责：

- 合并结论。
- 决定是否写入 SDD / DocsAI / SystemAgent。
- 执行任何写入和验证。

### D6：任务拆分继续交给 SDD，不做额外拆分系统

裁决：Adopt Now。

原则：

- 设计阶段充分交流，形成设计文档。
- 生成 SDD 时自然拆成可执行任务。
- 过大的任务不追求一次跑通；一个个任务完成更容易控制游戏进度。
- 只有并行收益明显且文件 owner 清晰时，才考虑 worktree / subagent 辅助。

## Options

### 方案 A：只保留人工查找原始 session

优点：零实现成本。

缺点：恢复体验差，session id 难记，复盘依赖人工。

裁决：短期可用，但不足以作为 SystemAgent 优化方向。

### 方案 B：复用现成 transcript/session 工具 + SystemAgent 摘要 adapter

优点：避免重写 parser；`codbash` 覆盖跨工具查找和 handoff，`codlogs` 覆盖 Codex 精细导出，`claude-replay` 覆盖 replay；SystemAgent 只补项目语义摘要。

缺点：需要确认工具版本、许可证、输出稳定性和安全边界；不能替代人工设计判断。

裁决：推荐第一阶段。

### 方案 C：自写只读 session-ledger 脚本

优点：完全可控，输出格式可直接服务 SlimeAI。

缺点：重复造轮子；需要追 Claude Code / Codex / OpenCode 多套格式变化；大 session、sanitize、tool output 边界容易踩坑。

裁决：Reject as default；只有现成工具不能满足关键字段时，才写很薄的补充解析。

### 方案 D：hook 自动抓取 + 自动摘要

优点：自动化程度高。

缺点：hook 稳定性、信任、性能和隐私风险更高。

裁决：后置，必须等方案 B 稳定并有 smoke 后再评估。

## Recommendation

推荐后续只开一个小 SDD：

```text
Cross-agent Session Adapter and Read-only Research Subagent Pilot
```

范围：

1. 先评估并包装 `codbash`：只读列出、搜索、查看 Claude Code / Codex / OpenCode sessions，生成 handoff Markdown。
2. 写 SlimeAI 薄层 `session-adapter`：消费 `codbash handoff` 或工具导出，生成统一 schema、命名和 `Workspace/DocsAI/ChatHistory/` sidecar。
3. 对 Codex 精细导出时再调用 `codlogs`，不把 `codlogs` 当跨工具总入口。
4. 对 OpenCode fallback 使用官方 `opencode session list` / `opencode export [sessionID]`；对人工审查使用 `claude-replay` 生成 HTML artifact。
5. 只在 wrapper 层生成 `Workspace/DocsAI/ChatHistory/index.json` 和 `<time>-<tool>-<prompt-slug>-<session-id>.md`，不复制完整原始 session。
6. 增加一个只读资料 subagent 使用协议或 launcher 试验，优先用 Codex 内置 subagent 能力，不急着自定义 `.codex/agents/`。
7. 验证 `codex exec --json` 输出能否稳定作为非交互式 research runner 的 artifact。
8. 暂不接 hook；只在文档中记录 hook 后续接入条件。

## Must Confirm

### 思路问题

- 是否确认不做外层 AI CLI 管理 agent？为什么问：这是后续路线的最大分叉。默认值：确认不做，继续完善项目内 SystemAgent。

### 信息缺口

- 第一阶段是否确认只强制支持 Claude Code / Codex / OpenCode？为什么问：如果还要 Cursor / Gemini / Copilot，需要扩大验收样例。默认值：先支持这三个，其它工具作为可选。
- ChatHistory 是否放在 `Workspace/DocsAI/ChatHistory/`？为什么问：这是索引 artifact 的长期位置。默认值：放这里，原始 session 只记录路径和来源。
- 是否接受“`codbash` 获取 / 搜索 / handoff，SlimeAI 薄脚本整理 / 命名 / 落盘，`codlogs` 只做 Codex 补充”？为什么问：这决定是否改上游源码。默认值：接受，不改上游源码。
- 是否接受不改原始 session 文件名，只生成 sidecar 摘要文件？为什么问：直接改原始文件名可能破坏各 CLI 的 resume / index / archived sessions。默认值：只生成 sidecar。

### 决策未定

- 是否创建独立 SDD 实现 session tool adapter + read-only research pilot？为什么问：本文只是设计裁决，不应直接改工具。默认值：用户确认后创建独立 SDD。

## Should Confirm

- 是否需要把昨天的 `Workspace/DocsAI/思考/2026-06-07-游戏开发SystemAgent流程Agent深度分析.md` 压缩成短版或归档？默认值：保留历史文档，但在开头标注本文为当前裁决。
- 是否要试一次 Codex subagent 读取外部资料 / 本地文档？默认值：后续小 SDD 中试。
- 是否需要 hook 自动触发索引？默认值：第一阶段不做 hook。

## Defaults I Will Use

如果用户说“按这个执行”，默认采用：

- 不做 Warp / 外层 orchestrator / LangGraph / app-server 编排。
- 第一阶段强制支持 Claude Code / Codex / OpenCode，其它工具可选。
- session tool adapter 只读，不复制完整原始 session，不抓取私有 chain-of-thought。
- 不自写跨工具 parser；优先调用或适配 `codbash`，`codlogs` 只作 Codex 专项补充，`claude-replay` 只作 replay/审查辅助。
- ChatHistory 输出到 `Workspace/DocsAI/ChatHistory/`。
- 不改 Claude Code / Codex / OpenCode 原始 session 文件名。
- 搜索资料 subagent 只读，输出固定六段，`Files Touched: none`。
- hook 不自动跑重任务；后续只作为低频提示或轻量 queue。

## Artifact Updates

本轮新增 / 补充：

- `SDD/project/projects/PRJ-0001-systemagent-optimization/design/优化/INDEX.md`
- `SDD/project/projects/PRJ-0001-systemagent-optimization/design/优化/2026-06-08-SystemAgent工作流内化与会话记录优化.md`
- 本轮补充外部研究裁决：优先复用 `codbash` / `codlogs` / `claude-replay` 等现成工具；SystemAgent 不从零重写跨工具 transcript parser，只做只读 adapter、摘要和落盘协议。

建议同步：

- `Core/roadmap.md` 登记本设计为后续 SDD 候选。
- `Core/progress.md` 记录本轮裁决。
- `Workspace/DocsAI/思考/2026-06-07-游戏开发SystemAgent流程Agent深度分析.md` 标注为历史长分析，避免后续误用。
