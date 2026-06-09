# 游戏开发 SystemAgent 流程 Agent 深度分析

> 状态：analysis
> 日期：2026-06-07
> 位置：Workspace 级思考文档，不是 SystemAgent 正文规则。
> 结论摘要：SystemAgent 可以做成，但不应该从零做一个 Warp/IDE 替代品。更稳的路线是把 Codex 作为执行基座，把 SlimeAI 的 SDD、DocsAI、git boundary、验证、复盘和会话索引做成项目内 SystemAgent 控制层。

> 2026-06-08 更新：本文是历史长分析，保留证据和上下文。当前精简裁决见 `SDD/project/projects/PRJ-0001-systemagent-optimization/design/优化/2026-06-08-SystemAgent工作流内化与会话记录优化.md`。后续不要把本文中的 Warp / 外层 orchestrator / LangGraph 扩展讨论当作当前执行建议。

## Goal

本轮要判断的问题：

- 游戏开发是否需要一个“系统级 agent”，而不是单个 IDE / AI CLI 会话。
- Codex / Claude Code / Warp / OpenHands / SWE-agent / Aider / LangGraph 等现有工具是否已经覆盖这个需求。
- SlimeAI 当前 `Workspace/SystemAgent/` 能否演进到满足该需求。
- 如果能做，第一阶段应该做什么，避免从零写一个过大的工具。

非目标：

- 本文不修改 `Workspace/SystemAgent/` 正文 workflow、rule 或 hook。
- 本文不实现会话抓取、worktree 调度、自动合并器。
- 本文不把外部项目 API、prompt 或代码复制到 SlimeAI。

## Context Read

本地事实源：

- `Workspace/SystemAgent/README.md`
- `Workspace/SystemAgent/Actors/DeepThink.md`
- `Workspace/SystemAgent/Actors/DesignCritic.md`
- `Workspace/SystemAgent/Routes/ResearchAdoption.md`
- `Workspace/SystemAgent/Routes/WorkflowIteration.md`
- `Workspace/SystemAgent/Rules/Subagent.md`
- `Workspace/SystemAgent/Rules/AIConfig.md`
- `Workspace/SystemAgent/Rules/Boundary.md`
- `Workspace/SystemAgent/Rules/Documentation.md`
- `Workspace/SystemAgent/Registry/workflow-catalog.yaml`
- `Workspace/DocsAI/INDEX.md`
- `DocsAI/README.md`
- `DocsAI/思考/README.md`

样本会话：

- `/home/slime/.codex/sessions/2026/06/07/rollout-2026-06-07T14-12-29-019ea0b6-1953-7892-9ebf-c4e953700771.jsonl`
- `/home/slime/.codex/history.jsonl`

Git boundary：

- 当前仓库：`/home/slime/Code/SlimeAI/SlimeAI`
- 改文档前 `git status --short` 已有 3 个既有 `Workspace/SDD/Src/__pycache__/*.pyc` 改动，本文不触碰。

## Evidence / Search Coverage

### 本地证据

Codex session 样本结构统计：

- 总行数：`1918`
- 顶层类型：`session_meta` 1、`turn_context` 7、`response_item` 1389、`event_msg` 518、`compacted` 3。
- `response_item.payload.type`：`function_call` 430、`function_call_output` 430、`custom_tool_call` 59、`custom_tool_call_output` 59、`message` 186、`reasoning` 169、`web_search_call` 56。
- `function_call_output.payload.output` 存在命令返回文本；这说明“只记录命令不记录返回”不是事实。
- `reasoning` 主要是 `encrypted_content`，`summary` 多数为空；这说明“完整思考过程可直接抓取”也不是事实。
- `~/.codex/history.jsonl` 保存 `session_id`、时间戳和用户输入文本，可用于按首个提示词建立可读索引。

现有 SystemAgent 约束：

- `Workspace/SystemAgent/Rules/Subagent.md` 明确 subagent 是可选执行基座，不是 workflow / skill / role 同义词。
- 当前只允许只读 subagent：资料搜索、多源汇总、独立评审、测试设计、日志分析。
- 写入型 subagent / dispatcher 需要先稳定 SDD work package、worktree、文件 owner、validation artifact、cleanup、timeout、retry 和冲突处理策略。

### 外部资料

使用来源：

- Codex 官方 manual：`https://developers.openai.com/codex/codex-manual.md`
- Codex worktrees：`https://developers.openai.com/codex/app/worktrees.md`
- Codex subagents：`https://developers.openai.com/codex/subagents.md`
- Codex app-server：`https://developers.openai.com/codex/app-server.md`
- Codex SDK：`https://developers.openai.com/codex/sdk.md`
- Codex hooks：`https://developers.openai.com/codex/hooks.md`
- LangGraph Context7 / GitHub docs：`https://github.com/langchain-ai/langgraph`
- Warp docs / repository：`https://docs.warp.dev/`、`https://github.com/warpdotdev/Warp`
- Claude Code docs：`https://docs.anthropic.com/en/docs/claude-code`
- OpenHands：`https://github.com/All-Hands-AI/OpenHands`
- SWE-agent：`https://github.com/SWE-agent/SWE-agent`
- Aider：`https://aider.chat/docs/`
- Microsoft AutoGen：`https://microsoft.github.io/autogen/stable/`
- CrewAI：`https://docs.crewai.com/`

未覆盖：

- 没有 clone 并逐行审计 Warp / OpenHands / SWE-agent / AutoGen / CrewAI 源码。
- 没有验证这些工具在本机的安装、登录、权限和实际可用性。
- 没有读取非公开产品 roadmap；所有判断只基于本地事实源和公开资料。

## Problem Reality Check

问题真实存在，但需要拆成三个层级。

第一层是真实痛点：大型游戏开发任务天然跨多轮、跨资料、跨验证、跨 git 边界。把所有搜索、日志、设计、实现、验证都塞进一个对话，会导致上下文污染、恢复困难、历史难检索、复盘证据不足。

第二层是工具能力缺口：**现代 Codex 已经有 `resume`、`fork`、subagent、worktree、automation、app-server、SDK、`codex exec --json`、hooks 等能力**。用户早期判断“AI CLI 不能开新对话，不能做长任务”对旧体验成立，但对 2026-06-07 的 Codex 已经不完全成立。

第三层是项目级流程缺口：现有 IDE / AI CLI 通常提供通用 agent 能力，但不会天然理解 SlimeAI 的 SDD、DocsAI、ECS 红线、Godot 验证、游戏仓 / 框架仓 git boundary、DataOS 生成、场景测试 artifact 和复盘规则。这一层仍需要 SlimeAI 自己做 SystemAgent。

## Idea Check

用户思路基本成立，但有两个要校正的点。

成立的部分：

- 搜索资料、读本地文档、读外部框架资料应该隔离到独立工作单元，主对话只吸收结论。
- 每个写入任务用独立 worktree 更安全，最后再合并。
- 会话记录、命令输出、文件变更、决策和验证证据应该落地，供 Retrospective 改进 SystemAgent。
- 用用户输入提示词为 session 建立可读索引是必要的，因为原始 UUID 难以恢复上下文。

需要校正的部分：

- 不应该追求抓取“完整思考过程”。Codex 样本和官方资料都表明，公开可审查的是 conversation items、tool evidence、assistant 消息、命令返回、web/tool 事件等，不是私有 chain-of-thought。SystemAgent 应该要求 agent 输出结构化设计理由、假设、证据和复盘摘要，而不是试图还原不可读推理。
- 不应该先做 Warp 替代品。Warp / Codex app / Claude Code / OpenHands 是上层执行环境或工作台，SlimeAI 需要的是项目级 workflow control plane。先做可复用的会话索引、work package、research branch 和验证闭环，比做完整 UI 更有价值。

## Tool Capability Map

| 工具 / 框架 | 能力 | 适合采纳 | 不足 |
| --- | --- | --- | --- |
| Codex CLI / App | `resume`、`fork`、worktree、subagent、automation、hooks、JSONL session、`exec --json` | 作为默认执行基座 | 不自动理解 SlimeAI SDD / DocsAI / 游戏验证语义 |
| Codex app-server / SDK | 程序化 thread start/resume/fork、turn events、streamed items | 适合做 SystemAgent orchestrator 原型 | 需要自己定义任务状态机、artifact schema 和合并策略 |
| LangGraph | durable execution、human-in-the-loop、memory、graph workflow | 适合后续做长期 orchestrator 或状态机 | 会引入新依赖和复杂度；第一阶段未必需要 |
| Warp / Oz | 更强的终端 / agent 工作台和编排理念 | 适合参考交互体验和多 agent 工作台方向 | 不能替代 SlimeAI 项目事实源；是否适合深度集成需原型验证 |
| Claude Code | subagents、hooks、headless / CLI 工作流 | 适合对照 hook/subagent 设计 | 与 Codex 并存会增加配置同步和行为差异成本 |
| OpenHands | 开源软件开发 agent 平台 | 适合参考 sandbox、runtime、event 和 UI 架构 | 引入成本高，可能替换而非增强现有 Codex 工作流 |
| SWE-agent | 面向软件工程任务的 agent harness | 适合参考 trajectory、任务运行和评测思想 | 偏 benchmark / issue fixing，不是游戏开发流程事实源 |
| Aider | Git-first pair programming、repo map | 适合参考 repo map 和 diff/commit 工作流 | 多会话 / 多 worktree 编排不是核心目标 |
| AutoGen / CrewAI | 多 agent conversation / crew orchestration | 适合参考 agent 角色与 handoff | 对代码仓、Godot 验证、git boundary 仍需自建规则 |

结论：没有一个现成工具完整覆盖“SlimeAI 游戏开发 SystemAgent”。最合理路线是组合：Codex 执行能力 + SlimeAI SystemAgent 规则 + SDD artifact + git worktree + 会话索引 / 复盘脚本。LangGraph / Agents SDK / Warp 可以作为后续增强，而不是第一阶段必选依赖。

## Problem Shape

### 思路问题

- “完整记录才有价值”需要改成“可审查记录才有价值”。完整隐式推理不可得，也不应该作为系统依赖。
- “AI 自己合并解决冲突，反正 AI 应该知道要合并什么”风险过高。**AI 可以执行合并，但必须有 work package、owner 文件边界、验证命令和合并 gate**。
- “subagent 不稳定”需要区分 read-only subagent 和 write-heavy subagent。读资料 / 总结 / 审计适合 subagent；并行写代码需要 worktree 和合并协议。

### 信息缺口

- 需要确定第一阶段面向 Codex CLI、Codex app-server / SDK，还是 Warp / OpenHands 这种外部工作台。
- 需要确定会话索引是只处理 Codex，还是同时处理 Claude Code。
- 需要确定长期 artifact 放在 `Workspace/DocsAI/ChatHistory`、`.ai-temp`、还是 SDD 项目目录。
- 需要确定是否允许新增依赖。若使用 LangGraph / Agents SDK，需要 SDD 记录依赖和运行方式。

### 决策未定

- 是否先做一个只读“会话索引 + 复盘分析”工具。
- 是否把 research branch 固化为 SystemAgent workflow。
- 是否设计写入型 worktree dispatcher。
- 是否引入新的 UI / wrapper，还是只做 CLI 脚本和文档协议。

## Recommended Architecture

推荐分三层，而不是做一个巨型 agent。

### Layer 1：Session Ledger

目标：把已有 Codex session 从“难找的 UUID JSONL”变成可检索的本地证据。

输入：

- `~/.codex/history.jsonl`
- `~/.codex/sessions/**/*.jsonl`
- 可选：`codex exec --json` 输出
- 可选：Codex hooks `UserPromptSubmit`、`PostToolUse`、`Stop`

输出：

- `Workspace/DocsAI/ChatHistory/index.json`
- `Workspace/DocsAI/ChatHistory/<date>-<slug>-<session-id>.md`
- 每个 session 的 source JSONL 路径、首个用户提示词、主要命令、工具输出摘要、文件改动、验证命令、最终结论、开放问题。

关键边界：

- 保存“工具返回和可见消息”，不保存不可读推理。
- 工具输出如果在执行时已经被截断，只能记录 Codex 实际收到的文本，不能恢复 OS 原始完整输出。
- session ledger 是 Persistent Review，不是 SystemAgent 正文规则。

### Layer 2：Research Branch Workflow

目标：把外部资料搜索、本地 Resources 研究、本地 DocsAI 读取从主对话移出去。

流程：

1. 主对话创建 research task：范围、问题、输出 schema。
2. 启动只读 worker：例如 `LocalDocsResearch`、`WebFrameworkResearch`、`ExistingSessionAnalysis`。
3. Worker 输出固定格式：Scope、Evidence、Inference、Unknown、Risks、Recommended Main-Thread Action、Files Touched: none。
4. 主对话只导入结论和 citation，不导入大段日志 / 全文资料。
5. Research 结果若长期有效，再由主对话落入 `Workspace/SystemAgent/`、`DocsAI/` 或 SDD。

这正好匹配当前 `Workspace/SystemAgent/Rules/Subagent.md`，属于可以先做的低风险增强。

### Layer 3：Worktree Work Package

目标：解决“一个大任务拆成多个可并行写入任务”的问题。

每个写入任务必须有：

- `work_package_id`
- base branch / commit
- worktree path
- owner paths
- forbidden paths
- prompt
- expected artifacts
- validation commands
- merge criteria
- rollback / cleanup strategy

合并策略：

- worker 不直接合并主分支。
- worker 完成后提交或生成 patch。
- merger agent 在主线程读取 diff、验证 owner 边界、解决冲突、运行验证。
- 失败时保留 patch、log 和 validation artifact，不自动覆盖主工作区。

这层不是当前 SystemAgent 已完成能力。它需要 SDD 设计后再做。

## Options

### 方案 A：只做文档和人工流程

内容：

- 写 Research Branch / Session Ledger / Worktree Work Package 规范。
- 继续人工启动 Codex / fork / resume / worktree。

优点：

- 最快，几乎无技术风险。
- 不引入依赖。

缺点：

- 仍需要手动复制提示词和整理 session。
- 不能解决“多开对话自动串联”的核心痛点。

适合：立即修正流程认知，但不够。

### 方案 B：先做 Codex Session Ledger + Research Runner

内容：

- 编写只读脚本解析 `~/.codex/history.jsonl` 和 `~/.codex/sessions/**/*.jsonl`。
- 生成按提示词命名的本地索引和 Markdown 摘要。
- 用 `codex exec --json` 或 Codex app-server 启动只读 research worker，输出固定 schema。
- 把结果落到 `Workspace/DocsAI/ChatHistory` 或 SDD `notes.md`。

优点：

- 直接解决“找不到对话、复盘证据不足、搜索挤占上下文”的痛点。
- 不需要一开始做复杂自动合并。
- 可验证：给定一个 session JSONL，输出稳定摘要和索引。

缺点：

- 只能解决研究和复盘，不能完整自动跑大任务。
- 对 interactive session 的事件粒度依赖 Codex 当前 JSONL 格式，格式变化需要适配。

适合：推荐第一阶段。

### 方案 C：完整 SystemAgent Orchestrator

内容：

- 基于 Codex app-server / SDK 或 LangGraph 做状态机。
- 支持新建 / fork / resume 多线程。
- 支持 research worker、write worker、worktree dispatcher、merger、retrospective。
- 支持 artifacts、UI 或 dashboard。

优点：

- 最接近用户愿景。
- 长期可以变成真正的游戏开发系统级 agent。

缺点：

- 范围大，风险高。
- 需要先稳定 artifact schema、权限模型、失败恢复、合并策略。
- 如果没有足够高频使用，很容易做成维护成本很高的工具。

适合：第二阶段或第三阶段，不适合直接开做。

## Recommendation

推荐采用方案 B 作为第一阶段，并把方案 C 作为 SDD 后续方向。

具体顺序：

1. 先做 `Session Ledger`：解析 Codex `history.jsonl` + `sessions/**/*.jsonl`，按首个用户 prompt 生成可读索引。
2. 再做 `Research Branch`：只读 worker 搜索外部 web / ctx7 / Resources / DocsAI，返回结构化结论，不污染主对话。
3. 然后设计 `Worktree Work Package`：每个写入任务一个 worktree，明确 owner paths、验证命令、merge gate。
4. 最后才考虑 app-server / SDK / LangGraph / Warp UI 级集成。

原因：

- 方案 B 命中当前最大痛点，但不会过早承诺自动写入 / 自动合并。
- 它和现有 SystemAgent 兼容：只读 subagent 已被允许，ResearchAdoption 和 WorkflowIteration 已有 route。
- 它能产出可验证 artifact，为后续改进 SystemAgent 提供真实样本。

## Can SystemAgent Meet The Need?

可以，但必须重新定义边界。

SystemAgent 不应该是：

- 一个新的 IDE。
- 一个完整 Warp 替代品。
- 一个试图截获模型隐式推理的监控器。
- 一个让多个 agent 在同一工作区随意写文件的 dispatcher。

SystemAgent 应该是：

- 项目语义控制层：知道 SlimeAI 的 DocsAI、SDD、ECS 红线、Godot 验证、git boundary。
- 任务状态机：把大任务拆成可恢复 work package。
- 证据系统：把 prompt、命令、返回、diff、验证、决策、未知项落盘。
- 研究隔离层：让资料搜索在独立 worker 中完成，主对话只接收结论。
- 合并门禁：让 AI 可以合并，但必须经过 owner boundary、diff review、验证和 retrospective。

现有 SystemAgent 已有基础：

- Route / Actor / Rule / Tool / Registry 五目录结构。
- DeepThink / DesignCritic / ResearchAdoption / WorkflowIteration。
- Subagent read-only policy。
- SDD 作为中大型任务恢复事实源。
- AI config 同步与 skill-test。

当前缺口：

- 缺 Session Ledger schema 和解析工具。
- 缺按 prompt 命名的 session index。
- 缺 research worker 启动协议和结果导入协议。
- 缺写入型 work package / worktree / merge gate。
- 缺对 Codex app-server / SDK / `codex exec --json` 的项目级封装。

## Not Recommended

- 不建议先写一个完整 Warp clone。UI 不是当前最大风险，任务协议和证据闭环才是。
- 不建议依赖完整 chain-of-thought。不可读、不可稳定获取，也不是复盘必须条件。
- 不建议把 `~/.codex/sessions` 直接作为长期事实源。它是原始事件日志，应派生为 `ChatHistory` / SDD notes。
- 不建议让多个写入 agent 直接在同一个 checkout 并行改文件。
- 不建议一开始引入 LangGraph / AutoGen / CrewAI 做重编排，除非先有 SDD 证明 Codex app-server / `exec --json` 不够用。

## Must Confirm

### 思路问题

- 你是否接受“记录可见证据 + 结构化决策”，不追求抓取完整隐式思考过程？如果不接受，当前 Codex / Claude Code 公开能力大概率无法满足。
- 你是否接受第一阶段只做只读 research / session ledger，不先做写入型多 agent dispatcher？这会显著降低失败和冲突风险。

### 信息缺口

- 第一阶段是否只支持 Codex，还是必须同时支持 Claude Code？默认建议只支持 Codex，因为当前样本和本仓工作流都在 Codex 上。
- Session 索引长期放哪里？默认建议 `Workspace/DocsAI/ChatHistory/`，原始 JSONL 只保留路径引用。

### 决策未定

- 后续是否创建 SDD 来正式设计 `Session Ledger + Research Branch + Worktree Work Package`？如果要实现，不建议只靠一篇思考文档推进。
- 是否允许后续使用 Codex app-server / SDK？默认建议先用 `codex exec --json` 和本地 JSONL 解析，等需求稳定再接 app-server / SDK。

## Should Confirm

- 是否需要把会话标题同步到 terminal title / statusline，还是只做本地索引文件即可。
- 是否需要处理历史 Claude Code session。
- 是否需要把 research worker 输出自动挂到 SDD `notes.md`。
- 是否需要为不同研究类型定义固定 worker：`web-research`、`local-docs`、`session-review`、`engine-reference`。

## Defaults I Will Use

如果后续用户说“按建议执行”，默认假设：

- 第一阶段只支持 Codex。
- 不抓取或推断私有 chain-of-thought。
- 会话索引输出到 `Workspace/DocsAI/ChatHistory/`。
- 原始 session JSONL 不复制全文进仓库，只记录路径、摘要和必要证据。
- Research worker 只读，`Files Touched: none`。
- 写入型多 agent / worktree dispatcher 先进入 SDD，不直接实现。

## Artifact Updates

本轮只新增本文：

- `Workspace/DocsAI/思考/2026-06-07-游戏开发SystemAgent流程Agent深度分析.md`

本文属于思考文档，不作为 SystemAgent 正文规则。若要进入实现，应创建或更新 SDD，并把长期规则落到：

- `Workspace/SystemAgent/Rules/Subagent.md`
- `Workspace/SystemAgent/Routes/ResearchAdoption.md`
- `Workspace/SystemAgent/Routes/WorkflowIteration.md`
- 新增 `Workspace/SystemAgent/Tools/session-ledger/` 或等价工具目录
- 必要时新增 `.ai-config/skills/systemagent/...` wrapper skill 源
