# SystemAgent 工作流内化与核心优化裁决

> 状态：current
> 日期：2026-06-08
> 更新：2026-06-09
> 来源：用户对游戏开发 SystemAgent 流程、AI CLI 会话记录、资料搜索和任务拆分方式的连续复盘。
> 定位：PRJ-0001 当前 SystemAgent 优化主设计入口；会话记录工具选型细节已归入 `../会话记录适配器参考设计/`。

## Goal

本设计回答一个核心问题：

```text
SlimeAI 的 SystemAgent 应该怎样在现有 AI CLI 内稳定支撑游戏开发长任务？
```

当前裁决是：

- 不再额外写一个外层 agent 去管理 Claude Code / Codex / OpenCode。
- 不魔改 Warp，不从零做 terminal / IDE / agent workbench。
- 把 SystemAgent 做成项目内工作流控制层：负责 SDD、DocsAI、skill、hook、subagent、会话记录、验证和复盘的协议。
- AI CLI 仍是执行基座；SystemAgent 不替代 AI CLI，只约束 AI CLI 在本项目里的工作方式。
- 会话记录先做只读 adapter 和 ChatHistory 派生证据，不追求完整私有思考过程。
- 资料搜索和大上下文阅读可以交给只读 subagent，但主对话保留写入、裁决和验证责任。
- 任务拆分继续由设计交流和 SDD 自然产生，不另做自动拆分系统。

非目标：

- 不实现写入型 multi-agent dispatcher。
- 不自动创建、清理或合并 worktree。
- 不接管 Claude Code / Codex / OpenCode 的 resume / fork / session 存储协议。
- 不复制完整原始 transcript、JSONL、SQLite 或私有存储到仓库。
- 不还原、猜测或保存私有 chain-of-thought。
- 不把 ChatHistory 当作 SystemAgent 规则事实源；它只是 evidence sidecar 和复盘入口。

## Context Read

已读取的当前事实源：

- `Workspace/SystemAgent/Actors/DeepThink.md`
- `Workspace/SystemAgent/Actors/DesignCritic.md`
- `Workspace/SystemAgent/Routes/ResearchAdoption.md`
- `Workspace/SystemAgent/Routes/WorkflowIteration.md`
- `Workspace/SystemAgent/Rules/Boundary.md`
- `SDD/project/projects/PRJ-0001-systemagent-optimization/README.md`
- `SDD/project/projects/PRJ-0001-systemagent-optimization/Core/roadmap.md`
- `SDD/project/projects/PRJ-0001-systemagent-optimization/Core/progress.md`
- `SDD/project/projects/PRJ-0001-systemagent-optimization/design/1/03-Hook与Gate重写方案.md`
- `SDD/project/projects/PRJ-0001-systemagent-optimization/design/1/04-Git与Worktree策略.md`
- `SDD/project/projects/PRJ-0001-systemagent-optimization/design/1/10-Subagent使用场景与采纳策略.md`
- `SDD/project/projects/PRJ-0001-systemagent-optimization/design/会话记录适配器参考设计/`
- `Workspace/SystemAgent/Tools/session-adapter/`
- `Workspace/DocsAI/ChatHistory/`

已知当前实现事实：

- `SDD-0039 Cross-agent Session Adapter` 已完成第一层只读 adapter。
- `Workspace/SystemAgent/Tools/session-adapter/session_adapter.py` 已提供 `list / index / summarize`。
- `Workspace/DocsAI/ChatHistory/index.json` 和 Markdown sidecar 已能生成。
- 2026 年 6 月 Codex 可见 transcript 已能按日期导出。
- `会话记录适配器参考设计/2026-06-09-ChatHistory-AI-first整理与价值评分设计.md` 已裁决下一步应做 Digest Gate，而不是复杂评分模型。

Git boundary：

- 当前仓：`/home/slime/Code/SlimeAI/SlimeAI`
- SystemAgent 事实源：`Workspace/SystemAgent/`
- SDD 项目事实源：`SDD/project/projects/PRJ-0001-systemagent-optimization/`
- ChatHistory 派生证据：`Workspace/DocsAI/ChatHistory/`
- 外部参考项目：`Workspace/Resources/tool/*`，只作为研究输入，不在本设计中修改。

## Evidence / Search Coverage

本地证据：

- PRJ-0001 已有完整 SDD / workflow / skill / hook / subagent / git policy 基础，不需要再另起外层 orchestrator。
- `SDD-0039` 已证明薄层 session adapter 可行：可以基于本地 `codbash` 结构化读取当前仓 Claude / Codex session，并生成 ChatHistory sidecar。
- Codex 2026 年 6 月会话导出显示：原始 visible transcript 体积大，包含大量系统注入、skill 列表、turn context、工具输出和中断信号；只靠完整 transcript 不适合作 AI 默认恢复入口。
- 当前 ChatHistory 已经解决“能找到 / 能导出”，但还没有完全解决“AI 如何快速判断是否值得读、读什么、失败在哪里、最终结论是什么”。
- `codbash` / `codlogs` / `tracebase` 的参考设计已拆分出三层能力：
  - `codbash`：跨工具发现、搜索、定位和 handoff 思路。
  - `codlogs`：Codex 高保真 tool output / 大 session 导出思路。
  - `tracebase`：失败、context waste、scorecard、redacted export 等复盘维度。

外部研究结论已经沉淀到参考设计目录，不在本文重复展开：

- `../会话记录适配器参考设计/2026-06-08-AI会话管理工具选型分析.md`
- `../会话记录适配器参考设计/2026-06-09-参考项目驱动的Cross-agent-Session-Adapter设计.md`
- `../会话记录适配器参考设计/2026-06-09-ChatHistory-AI-first整理与价值评分设计.md`

未覆盖或仍待验证：

- Claude Code / OpenCode 的高保真 tool output 导出策略尚未实测到 Codex 同等级别。
- OpenCode 当前只是支持路径，用户暂时没有真实 OpenCode 使用样例。
- Codex subagent / hook 的当前版本能力可以继续试，但不应在未验证前变成默认重流程。
- ChatHistory Digest Gate 还未实现，现有 visible transcript 仍需要人工或 AI 二次整理。

## Problem Reality Check

问题真实存在，而且核心问题不止一个。

### P1：SystemAgent 核心内容没有被突出

旧讨论里有很多对 Warp、外层 agent、app-server、LangGraph、多 agent 工作台的发散分析。这些内容在探索期有价值，但现在会稀释当前目标。

当前最重要的不是“做一个更大的工具”，而是让本项目里的 SystemAgent 明确回答：

- 一个任务从需求交流到 SDD、实现、验证、复盘，应该落到哪些 artifact。
- 哪些资料是事实源，哪些只是参考证据。
- AI 在什么时候可以用 subagent，什么时候必须主对话自己做裁决。
- 会话记录怎样成为可恢复的证据，而不是一堆难找的 JSONL。
- 后续流程问题怎样转成新的 SDD，而不是散在聊天里。

### P2：旧方向容易把实现带偏

“新写一个 agent 管理 AI CLI”表面上能解决多开对话、任务串联和资料搜索的问题，但它会引入更大的管理对象：

- 这个外层 agent 自己也要被设计、调试、验证和复盘。
- 它会和当前项目里的 SystemAgent 争夺事实源。
- 它可能重复 Claude Code / Codex / OpenCode 已经提供的 resume、fork、tool、hook、subagent 能力。
- 一旦外层调度出错，排查路径会比当前更复杂。

所以这个方向已经过时，只保留一句历史结论：不做外层 AI CLI manager，不魔改 Warp。

### P3：会话记录难找、难读、难复盘

用户遇到的痛点是真实的：

- Codex resume id 不可读，`.codex/sessions` 路径难找。
- Claude Code / Codex / OpenCode 的存储格式不同。
- 只看最终回答会丢失工具调用、验证、失败原因和文件修改证据。
- 直接读完整 transcript 又会被系统注入、上下文和工具输出淹没。

正确目标不是抓“完整思考过程”，而是把可公开审查的 evidence 整理好：

- 用户请求。
- assistant 用户可见消息。
- tool call 和 tool output 摘要。
- 命令成功 / 失败 / 未知。
- 文件修改信号。
- 验证证据。
- 决策、未决问题、最终状态和恢复提示。

### P4：资料搜索会污染主对话上下文

游戏开发经常需要外部资料、框架文档、本地 Resources、历史会话和竞品机制分析。如果这些内容全部塞进主对话：

- 上下文会快速膨胀。
- 结论和证据容易混在一起。
- 主对话会变成资料仓库，而不是执行控制面。

只读 subagent 或新会话适合承担资料搜集，但必须有固定输出协议，不能把主对话变成“再读一次全部资料”。

### P5：任务拆分不是越自动越好

用户已经校正过：大任务拆得太大并不一定更好。游戏开发需要持续掌控设计方向、验证结果和进度节奏。

SystemAgent 不需要额外发明任务拆分系统。更稳的方式是：

- 先通过对话和 DeepThink 形成设计文档。
- 需要正式执行记忆时创建 SDD。
- SDD 的 tasks 自然拆成可验证的小任务。
- 一次完成一个可控任务；只有 owner 清晰、冲突风险低时才考虑并行。

## Idea Check

### 成立的思路

- “完善项目里的 SystemAgent，而不是套一层 agent 管理 AI CLI”成立。
- “会话记录应该被整理成可读、可搜、可复盘的本地资料”成立。
- “公开可审查的是 transcript、assistant 消息、tool evidence、命令返回和 web/tool 事件，不是私有 chain-of-thought”成立。
- “搜索资料可以用只读 subagent，新会话总结结论后回到主对话”成立。
- “任务通过设计文档和 SDD 拆分，不需要刻意做自动拆分系统”成立。
- “不要一次性处理全部历史会话，优先当前任务、当前 repo、指定 session 和最近 N 个 session”成立。

### 需要校正的思路

- “命令返回没用”不完全成立。完整命令返回不应默认塞进摘要，但关键错误、验证结果、失败工具调用和文件修改证据很有价值。
- “直接找 `.codex` 原始记录就够了”只适合短期人工排查。长期恢复需要 ChatHistory index、可读命名、digest 和 SDD progress 引用。
- “自动抓取和自动保存应该先做”不成立。Codex / Claude / OpenCode 已保存原始 session；第一阶段先做手动只读索引和 digest，hook 后置。
- “魔改 `codbash`”不推荐。参考它的跨工具发现能力即可，SlimeAI 自己做薄层整理和项目语义摘要。

## Current Architecture

SystemAgent 的定位应当是项目内控制层：

```text
AI CLI
  负责：模型对话、工具调用、resume/fork、subagent/hook 基础能力

SystemAgent
  负责：任务入口、workflow、角色、skill、policy、gate、复盘、验证协议

SDD
  负责：中大型任务的设计、任务拆分、BDD、进度、阻塞、验证证据和恢复点

DocsAI
  负责：项目知识、框架事实源、工作区级思考和设计资料

ChatHistory
  负责：会话 evidence sidecar、可读索引、恢复摘要、工具失败、验证证据
```

关键边界：

- AI CLI 是执行环境，不是项目事实源。
- SystemAgent 是工作流规则，不是外部 agent runtime。
- SDD 是正式任务记忆，不是聊天备忘录。
- ChatHistory 是证据 ledger，不是新的设计事实源。
- 外部工具是参考或读取入口，不是 SlimeAI 的控制面。

## Core Decisions

### D1：不做外层 AI CLI 管理 agent

裁决：Reject。

原因：

- 会产生“谁管理管理者”的问题。
- 会与项目内 SystemAgent 形成双重事实源。
- 会重复 AI CLI 已经有的 resume、fork、hook、subagent 和 tool 执行能力。
- 当前真正缺的是项目 workflow、artifact 和复盘协议，不是一个新调度器。

当前替代方案：

- 在现有 AI CLI 会话中运行 SystemAgent workflow。
- 用 SDD 串联长期任务。
- 用 ChatHistory 记录可恢复证据。
- 需要资料搜索时用只读 subagent 或新会话输出结论。

### D2：不魔改 Warp，不做 terminal / IDE 替代品

裁决：Reject。

原因：

- Warp 这类工具解决的是终端体验和工作台体验，不是 SlimeAI 项目事实源。
- SlimeAI 的价值在 SDD、DocsAI、ECS 事实源、验证和复盘闭环。
- UI/外壳投入大，且不直接解决会话证据质量、任务恢复和工作流稳定性。

保留结论：

- Warp / 多 agent 工作台只能作为历史参考，不再作为当前执行建议。

### D3：SystemAgent 先补核心流程，不扩成大平台

裁决：Adopt。

SystemAgent 当前最需要强化的是：

- 入口：明确每类任务从哪个 workflow / skill 进入。
- 决策：DeepThink / DesignCritic 在设计冻结前暴露问题和确认点。
- 执行：SDD tasks 和 progress 记录可恢复的任务状态。
- 验证：每次完成都留下命令和结果，不写弱证据。
- 复盘：ChatHistory / Retrospective 能回答“做了什么、为什么、证据是什么、下次怎么继续”。
- 边界：hook、subagent、worktree、外部工具都要有最小可用范围，不承担重流程。

### D4：会话记录用薄层 adapter + AI-first digest

裁决：Adopt。

已经完成：

- `SDD-0039` 第一版只读 `session-adapter`。
- 可列出当前仓 Claude / Codex sessions。
- 可生成 ChatHistory sidecar 和 `index.json`。
- 可导出 Codex 2026/06 visible transcript。

下一步：

- 在 visible transcript 上增加 Digest Gate。
- 短会话默认 `locator-only`，不生成整理文档。
- 中断会话批量整理时可选跳过，但单个指定 session 默认允许整理。
- 工具调用区分 `success / failed / unknown / not_applicable`。
- 失败工具调用单独写 `derived/tool-failures.md`。
- AI 默认读取 `derived/ai-context.md` 和 `derived/summary.md`，必要时再读 raw transcript。

### D5：资料搜索用只读 subagent，但不默认进入所有任务

裁决：Adopt as optional workflow capability。

适用场景：

- 外部资料搜索。
- 本地 `Workspace/Resources/` 大范围读取。
- 本地 DocsAI / SDD 历史设计归纳。
- 历史 ChatHistory 摘要。
- 独立评审、测试设计、替代方案对比。

输出协议：

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

- 判断结论是否可采纳。
- 把证据写入 SDD / DocsAI / SystemAgent。
- 做最终设计裁决。
- 执行所有写入和验证。

不做：

- 不让 subagent 写项目文件。
- 不让 subagent 合并冲突。
- 不把 subagent 输出直接当事实源，必须经过主对话整理。

### D6：hook 只做轻量提示，不跑重流程

裁决：Adopt Later。

原因：

- hook 有 trust / review / 性能和重复触发风险。
- Stop hook 或 PostToolUse hook 不适合做大 session 解析。
- 现阶段手动触发 adapter 更可控。

后续允许的 hook 方向：

- 提醒运行 `session-adapter`。
- 写轻量 queue marker。
- 做低成本 safety check。

仍不允许：

- hook 自动解析全量历史。
- hook 自动摘要大 transcript。
- hook 自动写 SDD 状态。
- hook 自动修改 git/worktree。

### D7：任务拆分继续交给 SDD

裁决：Adopt。

原则：

- 大任务先设计，设计不清楚不直接实现。
- 设计文档冻结后，需要长期恢复和执行记忆时创建 SDD。
- SDD tasks 按可验证结果拆，而不是按“AI 能一次跑多久”拆。
- 每个任务完成后更新 progress、验证证据和下一步。
- 并行只作为局部手段，不作为默认工作流。

## Main Risks

| Risk | Level | Mitigation |
| --- | --- | --- |
| 主设计继续被工具选型细节淹没 | high | 工具细节归入 `会话记录适配器参考设计/`，`优化/` 只保留核心裁决 |
| ChatHistory 体积越来越大，AI 默认硬读 raw transcript | high | 实现 Digest Gate、`locator-only`、`derived/ai-context.md` 和 index v3 |
| 把外部工具当事实源 | medium | `codbash/codlogs/tracebase` 只作读取或参考，事实源仍是 SDD / DocsAI / SystemAgent |
| subagent 结论污染主线 | medium | 固定只读输出协议，主对话负责采纳和写入 |
| hook 过早自动化导致不稳定 | medium | hook 后置，先手动命令验证 |
| Claude / OpenCode 保真度不如 Codex | medium | 第一阶段 Codex first，Claude/OpenCode 先保留 locator/export 支持路径 |
| 旧文档中的过时方向被误读 | medium | 历史长分析标注过时，当前 design INDEX 指向本裁决 |

## Options

### 方案 A：只保留现状

内容：

- 继续手动找 session。
- 继续在聊天中口头总结。
- 不继续改 ChatHistory digest。

优点：零实现成本。

缺点：恢复体验差，复盘证据弱，旧问题会继续出现。

裁决：不推荐。

### 方案 B：SystemAgent 内化 + ChatHistory digest + 只读 subagent 协议

内容：

- 保留当前 AI CLI。
- SystemAgent 负责 workflow 和 artifact。
- `session-adapter` 继续增强 digest。
- 资料搜索通过只读 subagent 输出结论。
- hook 后置。

优点：范围小、可验证、符合现有项目结构。

缺点：不能一次性解决所有历史会话；Claude/OpenCode 高保真需要后续补。

裁决：推荐。

### 方案 C：引入完整本地 trace / wiki / workbench 平台

内容：

- 直接引入 `tracebase`、`llm-wiki`、agent workbench 或类似系统。

优点：功能完整，长期分析能力强。

缺点：概念重、依赖重、会和 DocsAI / SDD / ChatHistory 重叠。

裁决：后置参考，不作为当前主线。

### 方案 D：重写外层 orchestrator

内容：

- 自己写一个 agent 管 Claude Code / Codex / OpenCode、多开会话、拆任务、合并结果。

优点：理论上最灵活。

缺点：成本最高、风险最大、会产生新的控制面和事实源冲突。

裁决：Reject。

## Recommendation

推荐继续走方案 B。

后续优先级：

1. `ChatHistory AI-first Session Digest`：在现有 visible transcript 上实现 Digest Gate、locator-only、tool failure summary、index v3、per-session folder 和 AI 默认恢复入口。
2. `Read-only Research Subagent Pilot`：试运行只读资料 subagent 协议，输出 `Evidence / Inference / Unknown / Recommended Main-Thread Action`，主对话负责采纳。
3. `Claude/OpenCode High-fidelity Export`：在用户真正使用 OpenCode 或需要 Claude 完整 tool output 时补高保真导出路径。
4. `Retrospective ChatHistory Hook-in`：任务完成前可选提示生成 ChatHistory digest 或把 session locator 写入 progress。
5. `Hook Light Queue`：只有 adapter 和 digest 稳定后，才考虑轻量 hook 提醒或 queue marker。

## Must Confirm

### 思路问题

暂无。用户已确认不做 OpenCode-first、认可推荐方案，并要求按当前路线重构设计文档。

### 信息缺口

- Claude Code 高保真 tool outputs 是否必须下一阶段立即做？为什么问：当前最成熟的是 Codex visible transcript 和 digest。默认值：不立即做，等 ChatHistory digest 完成后再补。
- OpenCode 是否需要准备真实样例？为什么问：当前用户暂时不用 OpenCode。默认值：只保留支持路径，不阻塞当前工作。

### 决策未定

- 是否把 `Read-only Research Subagent Pilot` 排在 ChatHistory Digest 之后立即执行？为什么问：这会决定下一个 PRJ-0001 子 SDD 的顺序。默认值：先做 ChatHistory Digest，再做资料 subagent pilot。

## Should Confirm

- 是否需要把 `Workspace/DocsAI/思考/2026-06-07-游戏开发SystemAgent流程Agent深度分析.md` 进一步压缩？默认值：暂不改正文，保留开头历史标注。
- 是否需要把旧 ChatHistory 扁平 Markdown 迁移到 per-session folder？默认值：不删除不移动旧文件，新 digest 只追加新结构。
- 是否需要把 session digest 结果写回当前 SDD `progress.md`？默认值：只在任务完成或复盘时写 locator 和摘要，不自动写每次对话。

## Defaults I Will Use

若用户继续说“按你的来”，默认采用：

- 不做 Warp / 外层 orchestrator / 自研 workbench。
- SystemAgent 继续作为项目内 workflow control plane。
- SDD 继续作为中大型任务执行记忆。
- ChatHistory 是 evidence sidecar，不是规则事实源。
- 会话记录第一阶段 Codex first，Claude / OpenCode 保留支持路径。
- 不改原始 session 文件名，不复制原始 JSONL / SQLite。
- 不接自动 hook，不跑重任务。
- 资料 subagent 默认只读，输出固定六段。
- 任务拆分由设计文档和 SDD tasks 承担。

## Not Recommended

- 不推荐魔改 `codbash`：上游工具范围大，且 detail/handoff 有截断；SlimeAI 只需要参考和薄层调用。
- 不推荐直接引入 `tracebase` 全套：第一阶段过重，会引入 store、dashboard、MCP、watcher 和依赖管理。
- 不推荐把 transcript 全部复制进仓库：体积和隐私成本高，且会让 AI 默认阅读入口失控。
- 不推荐把 hook 当工作流引擎：稳定性和信任边界不适合。
- 不推荐自动拆所有大任务：游戏开发节奏需要人工和设计文档持续控制。

## Artifact Updates

本轮重构目标：

- 本文件重写为 PRJ-0001 当前 SystemAgent 优化主裁决。
- 原 `AI会话管理工具选型分析` 移到 `../会话记录适配器参考设计/` 并压缩为参考文档。
- `design/INDEX.md`、`design/优化/INDEX.md`、`design/会话记录适配器参考设计/INDEX.md` 同步当前入口。
- `Core/roadmap.md` 和 `Core/progress.md` 记录文档重构裁决。

恢复提示：

- 想确认当前 SystemAgent 优化方向，先读本文件。
- 想查会话记录工具为什么选 `codbash/codlogs/tracebase`，读 `../会话记录适配器参考设计/`。
- 想继续实现，优先创建 `ChatHistory AI-first Session Digest` SDD。
