# 2026-05-16 SlimeAI SystemAgent 质量评估

> 范围：评估"AI 框架 agent 统一管理"在当前仓库的落地完整度。不动代码、不动 skill、不动 OpenSpec change，仅产出评估结论，作为后续 Change 1（紧急修复 + 收尾）与 Change 2（流程闭环增强）的设计输入。

## 总体评分

- 设计完整度（unify-ai-agent-development-workflow 设计层）：**8.5 / 10**
- 落地完整度（实际生效程度）：**4 / 10**
- AI 可执行性（路径、入口、命令是否能跑通）：**3 / 10**
- 流程闭环度（计划 → 实现 → 验证 → 回顾 → 反向更新）：**5 / 10**
- 自动化覆盖（hook/subagent 实际效果）：**1 / 10**
- 文档一致性：**5 / 10**
- 用户原始诉求覆盖：**6 / 10**
- 综合：**4.6 / 10**

结论：**设计已经基本到位，但实施时引入了一致性 typo 把整套自动化变成空跑**，加上未 archive 让 baseline 没合入。修复成本极低（路径替换），但已经造成"用户认为流程没建起来"的错觉。建议立即按 Change 1 修复并 archive，再用 Change 2 补齐用户真正关心的"闭环 + 反思"。

## 证据范围

读取的关键事实源：

- `Workspace/DocsAI/AgentWorkflow/` 全部 5 份长期文档 + 6 份 Protocols。
- `Workspace/DocsAI/INDEX.md`、`Workspace/DocsAI/GitSubmoduleWorkflow.md`。
- `.ai-config/skills/ai/ai-feature-development/SKILL.md` + 12 个 references。
- `.ai-config/rules/rules.md`、`AGENTS.md`、`SlimeAI/AGENTS.md`、`CLAUDE.md`、`.windsurf/rules/windsurfrules.md`。
- `.claude/settings.json`、`.codex/hooks.json`、`.codex/config.toml`、3 个 systemagent subagent 配置。
- `Workspace/Tools/systemagent-hooks/systemagent_hook.py`。
- `openspec/Plan/2026-05-15-ai-first-runtime-refactor-plan.md`。
- 4 个 active OpenSpec changes 的 proposal/design/tasks。
- `SlimeAI/DocsAI/Tests/{GodotSceneValidation, GodotSceneTesting, RuntimeTests}.md`。
- `Games/BrotatoLike/.ai-temp/scene-tests/runs/2026-05-16/18-40-54/` 一次真实 run artifact。

实际执行的检查命令：

- `openspec list --json`：当前 4 个 change，其中 3 个 status=complete 未 archive。
- `git status --short`：根仓 60+ 改动 + 大量未追踪；`SlimeAI/` 仓 22 改动（含 7 个 `D DocsAI/Agent/*`）；`Games/BrotatoLike/` 6 改动。
- `grep -rE "SlimeAISlimeAI|SlimeAIGames|SlimeAIWorkspace|SkilmeAI|SlimeAIResources"`：**276 处 typo 命中，分布在 101 个文件**。
- `test -f`：`/home/slime/Code/SlimeAIWorkspace/.../systemagent_hook.py` missing；`/home/slime/Code/SlimeAI/Workspace/.../systemagent_hook.py` found。
- `wc -l` 最新 combined.log：21 行（日志体量合规）。

## 主要优点

- **顶层架构方向正确**：把 systemagent 定义为"rule + skill + OpenSpec + DocsAI + 测试 + 角色 + artifact"组成的工作流系统，而非单个大 prompt。
- **`Workspace/DocsAI/AgentWorkflow/` 是名副其实的入口**：INDEX、总流程、协议、角色、调研笔记都集中在一个目录。RolePrompts.md 7 个角色齐全。
- **TDD 标准答案字段强约束已落地**：`GodotSceneValidation.md` 明确 `expectedInputs/expectedObservations/passCriteria/failCriteria/artifactPath` 非空、`status=pass`、`failureReasons=[]`、PASS marker 同时必须满足。
- **验证场景实际覆盖了 P1-P4**：8 个 headless 场景到位，artifact 设计统一，最新 run 实测 passed。`verify-ai-first-runtime-refactor-scenes` 14/14 done。
- **日志体量已经 AI-friendly**：最新 Entity scene combined.log 仅 21 行，stdoutLineCount=10，没有每帧噪声。
- **`.ai-config` vs `.claude/.codex` 分层清晰**：skill/rule/command 走 `.ai-config` 同步；hook/subagent 直接维护。
- **git 三档策略文档化**：manual / checkpoint / release 语义明确，默认 manual 保守。

## 主要问题

### 🔴 严重 / 阻塞性

#### 1. 路径 typo 让整套自动化空跑（最关键问题）

整个仓库存在 **5 个一致性 typo** 模式，共 **276 处分布在 101 个文件**：

| 错误模式 | 应改为 | 影响 |
| --- | --- | --- |
| `/home/slime/Code/SkilmeAI` | `/home/slime/Code/SlimeAI` | rules、AGENTS、hook session 消息 |
| `/home/slime/Code/SlimeAISlimeAI` | `/home/slime/Code/SlimeAI/SlimeAI` | 所有"框架验证入口"`cd` 命令 |
| `/home/slime/Code/SlimeAIGames/<game>` | `/home/slime/Code/SlimeAI/Games/<game>` | 所有"Godot 场景验证入口"`cd` 命令 |
| `/home/slime/Code/SlimeAIWorkspace/Tools/...` | `/home/slime/Code/SlimeAI/Workspace/Tools/...` | **hook 脚本路径全部失效** |
| `/home/slime/Code/SlimeAIResources/Else/...` | `/home/slime/Code/SlimeAI/Resources/Else/...` | 旧输入仓验证入口 |

最严重的是 **hook 路径**。`.claude/settings.json` 和 `.codex/hooks.json` 共 8 处指向 `/home/slime/Code/SlimeAIWorkspace/Tools/systemagent-hooks/systemagent_hook.py`，但该文件实际位于 `/home/slime/Code/SlimeAI/Workspace/Tools/systemagent-hooks/systemagent_hook.py`。**这意味着 UserPromptSubmit / PostToolUse / Stop / SubagentStop / SessionStart 全部从未成功执行过**。

进一步，hook 脚本 `systemagent_hook.py` L88 自己的 SessionStart 消息文本也写错了：

```@/home/slime/Code/SlimeAI/Workspace/Tools/systemagent-hooks/systemagent_hook.py:88
            "SlimeAI session gate: 当前工作区是 /home/slime/Code/SkilmeAI；注意根仓、SlimeAI 仓、Games 仓和 submodule 是不同 Git 边界。"
```

修复方式：全局 sed 替换 5 个 pattern。因为 typo 分布在 `.ai-config/` 源 + 已生成的 `.claude/.codex/.windsurf` 副本中，需要：

1. 修 `.ai-config/` 源（rule + skill + reference）。
2. 修不走 `.ai-config` 同步的事实源：`AGENTS.md`、`SlimeAI/AGENTS.md`、`.claude/settings.json`、`.codex/hooks.json`、`Workspace/DocsAI/**`、`Workspace/Tools/systemagent-hooks/systemagent_hook.py`、`openspec/Plan/*.md`、`openspec/changes/*/`、`SlimeAI/DocsAI/**`、`Games/BrotatoLike/DocsAI/**`。
3. 跑 `Workspace/Tools/ai-config-sync/sync-ai-config.sh` 让副本与源对齐。
4. 重新人工跑 hook 验证：`echo '{}' | python3 <hook> --event UserPromptSubmit` 能输出 advisory 消息。

#### 2. 三个 OpenSpec change 已完成但未 archive

`openspec list --json` 输出：

| change | tasks | status | baseline 合入？ |
| --- | --- | --- | --- |
| `unify-ai-agent-development-workflow` | 42/42 | complete | **否** |
| `verify-ai-first-runtime-refactor-scenes` | 14/14 | complete | **否** |
| `clarify-gameos-terminology` | 23/23 | complete | **否** |
| `expand-engine-reference-analysis-for-ai-gameos` | 27/29 | in-progress | N/A |

按工作区规则（`AGENTS.md` 第 83 行）："收尾使用 openspec-archive-change；将 delta spec 合入 openspec/specs/，并清理已完成 change 的执行历史，避免归档目录成为长期 AI 入口。"

未 archive 直接导致：

- `openspec/specs/` baseline 缺少 `ai-agent-workflow-governance`（新）、`agent-protocols`（修订）、`ai-feature-development-skill`（修订）、`gameos-godot-scene-validation`（修订）、`godot-scene-test-tooling`（修订）、`runtime-refactor-validation-scenes`（新）和 GameOS 术语清理 6 处。
- AI 下次路由可能把 `openspec/changes/<change>/` 误当作长期入口，导致已完成 change 持续占 context 配额。
- `openspec/changes/archive/` 不全（P4 archive 缺失问题在 `verify-ai-first-runtime-refactor-scenes` design.md L11-12 已记录但未修复）。

修复方式：对 3 个 complete change 分别跑 `openspec archive <change>`，逐一确认 baseline 合并无冲突。

#### 3. 反馈机制完全缺失（用户最痛的痛点）

用户原话："写得慢是因为我来检查代码，实际上检查没什么问题，问题就在于我检查的时候发现 AI 写得不好，方向不对，然后我要反馈给 AI 让 AI 继续去改，时间都浪费在这里。"

当前系统中**没有任何一个机制**能在 AI 完成功能后自动给出方向判定：

- Hook 路径错 → 不执行。
- subagent 必须被 AI 主动调用 → AI 不会主动调用 review 自己。
- skill 是被动入口 → AI 读不读靠运气。
- `Workspace/DocsAI/AgentWorkflow/SystemAgentWorkflow.md` 在 retrospective 段写了 7 个检查点，但只是"写下来"，不是"必须经过"。

即使 typo 修复后，advisory hook 只能 print 文本，无法阻断或标记失败。这是"流程闭环度 5/10"的根本原因。

需要 Change 2 解决（讨论：是否上 blocking hook？是否拆独立 retrospective skill？是否端到端 wrapper？）。

### 🟠 高优 / 设计层

#### 4. AgentWorkflow 与未 archive change 形成"逻辑源 vs 副本"漂移

`Workspace/DocsAI/AgentWorkflow/` 5 份长期文档 + 6 份 Protocols 中，相当部分应该作为 baseline spec 进入 `openspec/specs/ai-agent-workflow-governance/` 等目录。当前 baseline 还没有这些 spec（因为没 archive），所以：

- 同一段"角色 + TDD + retrospective"规则同时出现在 SKILL.md、`workflow-governance.md`、`SystemAgentWorkflow.md`、`design.md`、`tasks.md`。
- 修改时 AI 不知道哪个是源。
- 用户感觉"分散"是有道理的——不是物理分散，而是**逻辑源 vs 副本未对齐**。

修复方式：archive `unify-ai-agent-development-workflow` 后，baseline spec 自动合入，DocsAI / SKILL 中的内容降为"路由 + 摘要"，长期事实源回归 `openspec/specs/`。

#### 5. "AI 自己生成测试场景"实际未自动触发

`SystemAgentWorkflow.md` 和 `workflow-governance.md` 明确写"AI 必须主动设计测试或验证场景"，但**没有触发机制**：

- 不读 skill 的 AI 看不到这条规则。
- 读了 skill 但"极小修复模式"的 AI 会跳过 TDD 段。
- 没有 PostEdit hook 检查"代码改了但 `Tests/` 和 `Src/Validation/` 没改"。
- 没有 OpenSpec validate gate 检查"change 写了 capability 但 tasks 里没有 scene 任务"。

用户已经发现这个问题（原文 §1："实际上 AI 应该自己生成测试场景验证而不是我来提醒"）。

可选方案（Change 2 讨论）：

- **A. Blocking PostToolUse hook**：检测到 `Capabilities/` / `GameOS/` 写操作后，必须看到 `Tests/` 或 `Src/Validation/` 同步写入，否则 Stop 阶段返回非零阻断完成。代价：误报需要旁路。
- **B. 新独立 skill `test-scene-design-gate`**：作为强制阶段被 `ai-feature-development` 引用，AI 进入时被迫输出"本任务 TDD plan"。代价：AI 仍可能跳过。
- **C. 强化 `systemagent-test-designer` subagent 调用纪律**：在 SKILL.md 写明"必须 invoke systemagent-test-designer 输出 Verdict=pass 才能继续"。代价：Claude/Codex 不强制 subagent 调用。
- **D. OpenSpec validator 增加 capability change scene 任务检查**：扩展 validator，capability 类 change 缺少 scene 任务时 fail。代价：需要改 OpenSpec 工具或写自定义 lint。

#### 6. 标准答案分散在场景 README（未集中索引）

用户原话："测试场景需要把标准答案写出来，SlimeAI/DocsAI/Tests"——意思是希望有一个**索引位置**列出所有标准答案。

现状：

- 8 个场景的 README 各自写 `expected inputs / observations / pass criteria / fail criteria`。
- `SlimeAI/DocsAI/Tests/GodotSceneValidation.md` 在 L227-287 集中描述了每个场景必须验证什么，但和 README 是**并行写两份**，不是索引。
- 修改时容易只改一边，造成漂移。

可选方案（Change 2 讨论）：

- **A. 集中到 catalog 文档**：`SlimeAI/DocsAI/Tests/ValidationCatalog.md` 列 scene → `expected{Inputs,Observations,PassCriteria,FailCriteria,ArtifactPath}` 表格，README 只保留"运行命令、依赖、排查"。
- **B. 集中到 OpenSpec spec**：每个 capability 的 spec.md 包含场景 acceptance，README 只引用 spec id。
- **C. 保留双份但用一致性 lint**：写脚本验证 README 内容 = `GodotSceneValidation.md` 内容。

#### 7. Engine/Docs 研究产出未形成"反向更新提案"

用户原话："增强框架功能自我更新迭代闭环的能力 ... 这肯定要做对其他框架引擎详细调研后深度分析看看对这个 AI 框架有什么用 ... 实际上 Resources/Engine/Docs 已经有了"。

现状：

- `Resources/Engine/Docs/FrameworkAnalysis/Reports/` 有 22 份引擎源码分析报告（含综合报告 99-）。
- `ResearchAndAdoptionNotes.md` 引用了 99-综合报告，给出"已采纳方向"5 条。
- **没有"AI 自己读研究 → 反向提 OpenSpec proposal"的固定入口**。`research-reference-framework` skill 是研究入口，但产出落在 chat 里，不落 `openspec/changes/`。
- `expand-engine-reference-analysis-for-ai-gameos` change 27/29 在做这件事但卡住未完。

可选方案：等 `expand-engine-reference-analysis-for-ai-gameos` 完成后评估是否需要新 change；本 review 暂不展开。

### 🟡 中等 / 一致性

#### 8. 角色提示词只落了 3/7

`RolePrompts.md` 定义 7 个角色：Planner / Implementer / Test Designer / Reviewer / Verifier / Research Analyst / Retrospective。

`.claude/agents/` 和 `.codex/agents/` 只配了 3 个：test-designer / reviewer / retrospective。

`design.md` Decision 8 称"第一批只落地 read-only / review 型角色"是有意取舍。但用户原话："每个流程实际上角色不一样，需要有一个地方专门存放角色提示词才行"——可能希望全 7 个都有 subagent，或明确"剩下 4 个由默认 agent 承担"。

建议 Change 2 决策：保留 3/7 还是补齐 7/7。

#### 9. `ai-feature-development` reference 数量与 P5 archive 记录不一致

P5 archive 描述（`openspec/Plan/2026-05-15-ai-first-runtime-refactor-plan.md` 第 160 行）：

> `.ai-config/skills/ai/ai-feature-development/SKILL.md` 固定为 9 段，**references 固定为 11 个文件**

实际 `ls references/` 显示 **12 个文件**（多了 `workflow-governance.md`）。SKILL.md L13-25 已列 12 项。

这是 `unify-ai-agent-development-workflow` 加进来的，ProjectState 已记录"12 reference"，但 Plan 文件还停在"11 reference"。

修复：Change 1 顺便修 Plan 文件。

#### 10. 总流程图与子 protocol 之间的入口重复

`SystemAgentWorkflow.md` 给出 0-9 总流程；6 份 Protocols 又各自覆盖其中一部分（OpenSpec / Capability / Boundary / LongRunning / Completion / AIFeatureDev）。

读多份会"看到同一规则被三个文件表述"。`Workspace/DocsAI/AgentWorkflow/INDEX.md` 用入口顺序缓解，但 Protocols 之间还是有重叠（例如 `AIFeatureDevelopmentProtocol.md` 与 `OpenSpecChangeProtocol.md` 都写了"何时进 OpenSpec"）。

建议 Change 2 评估：6 份 Protocols 能否压缩为 3 份（FeatureLifecycle / Boundary / Completion）。本次评估**不建议**急于合并，因为 archive 后这些会成为 baseline，合并代价 < 收益时再做。

#### 11. `.codex/agents/*.toml` 的 model 名是占位符

```@/home/slime/Code/SlimeAI/.codex/agents/systemagent-test-designer.toml:3
model = "gpt-5.5"
```

`gpt-5.5` 不是真实模型名。Codex agent 配置在 fallback 时可能直接报错。需要确认这是设计阶段占位还是 bug。

### 🟢 低 / 待观察

#### 12. systemagent_hook.py 只 print 文本，未利用 Claude/Codex 的 inject 接口

Claude Code 的 `UserPromptSubmit` hook 支持返回 JSON 决定是否注入 system prompt 增强；`PostToolUse` 支持返回 `decision="block"` 阻断。当前脚本只 print stdout，等于把所有事件降级为"在 transcript 里写一行字"。

这是 advisory 设计的一部分（design.md Decision 7 明确"第一轮不实现不可旁路阻断"），但用户原文 §1 倾向于上更强的 gate。Change 2 可讨论。

#### 13. `expand-engine-reference-analysis-for-ai-gameos` 27/29 卡住

剩 2 个任务未完成，`lastModified` 是 2026-05-13，可能已被搁置。不属于本 review 主线，提一句作为提醒。

#### 14. 工作区根 60+ 文件未提交

`unify-ai-agent-development-workflow` 引入的产物（AgentWorkflow/、hooks、subagents、AGENTS.md 重写）目前**全部未提交**。这是用户全局规则"默认不自动 commit"的体现，符合规范。但意味着用户切换机器或重置 worktree 时会丢失。建议 Change 1 完成时提示用户做一次明确的 work-in-progress checkpoint。

## 用户原始诉求逐条对照

按用户原文 14 个要点（合并相近项）：

| # | 原文摘要 | 当前覆盖 | 缺口 |
| --- | --- | --- | --- |
| 0 | 总纲文档统一说明流程 | ✅ `SystemAgentWorkflow.md` | 路径 typo + 未 archive 让"事实源"信号弱 |
| 1 | AI 自己生成测试场景验证、独立"反思 skill" | 🟡 写在 reference 但未拆独立 skill | Change 2 决策 |
| TDD | 标准答案集中 + 统一标准 | 🟡 字段统一已落 | 集中索引未做 |
| TDD | 日志面向 AI、无冗余 | ✅ 实际 21 行 combined.log | 规范执行但未审计验证 |
| 2 | 提需求 → plan → 执行 → 验收 → 回顾迭代闭环 | 🟡 文档化 | 触发机制缺失 |
| 角色 | 多角色提示词集中 | ✅ `RolePrompts.md` 7 角色 | subagent 只落 3 个 |
| 3 | 参考其他游戏 skill | ❓ 语义不明 | 需澄清 |
| 4 | git 自动提交方案 | 🟡 三档策略文档化 | 默认 manual，待你决策是否升级 |
| 测试 | 端到端：编框架 → 更 submodule → 编游戏 → 跑场景 → 分析 → 修改 → 更新 plan | 🟡 各步骤有脚本 | 没有 wrapper 一键跑 |
| 5 | 评估已做 agent 质量 | 本文档 | — |
| 6 | 完整整合统一管理 | ✅ AgentWorkflow + ai-feature-development | typo + archive 让用户感知不到 |
| Plan | 验证 P1-P5 的新场景任务 | ✅ `verify-ai-first-runtime-refactor-scenes` 14/14 | 未 archive |
| 思考 | systemagent 是 skill + hook + subagent + rule 组合 | ✅ 已是这个抽象 | — |
| 其他 | 在 Workspace/DocsAI 新开目录写总体文档 | 🟡 AgentWorkflow 已是 | 你可能想要更高层"SystemAgent" 总览 |

## 关于"新开目录"建议

用户原话："在 Workspace/DocsAI 新开目录，生成总体文档"。`Workspace/DocsAI/AgentWorkflow/` 已经是工作流入口。我**不建议**再新开一个 `Workspace/DocsAI/SystemAgent/` 来放总览，原因：

- 会再造一个 AI 路由分叉点（用户最不希望发生的事）。
- AgentWorkflow 的 INDEX 已经是顶层入口。
- 总览内容应该并入 `AgentWorkflow/INDEX.md`（短摘要）+ baseline spec `openspec/specs/ai-agent-workflow-governance/spec.md`（详细，等 archive 后自动产生）。

但我**建议**新增 1 份文档明确"systemagent 是什么、由什么组成、各组件何时使用"——放在 `Workspace/DocsAI/AgentWorkflow/SystemAgentOverview.md`，与 SystemAgentWorkflow.md 分工：

- `SystemAgentOverview.md`（**Change 1 新增**）：架构图 + 组件清单 + 何时用什么。**对人 / 新会话快速 onboarding 友好**。
- `SystemAgentWorkflow.md`（已存在）：完整流程的 step-by-step。**对 AI 执行友好**。

待你决策是否同意。

## 设计开放问题（请你回答，作为 Change 2 输入）

1. **独立 retrospective skill**：是否拆 `ai-process-retrospective` 为独立可触发 skill？
   - 拆：AI 可被 Stop hook / 用户 / 自己显式触发；适合复盘历史会话。
   - 不拆：保留为 `ai-feature-development` 强制阶段，避免入口分散。
   - 我的推荐：**拆**。用户原文 §1 直接说"我认为可以 AI 自己运行这个 skill"。

2. **测试场景自动生成机制**：选 §5 中 A/B/C/D 哪个？
   - 我的推荐：**B + C**。新独立 skill `test-scene-design-gate` + 强化 `systemagent-test-designer` subagent 调用纪律。A 风险大（误报阻断），D 改 OpenSpec 工具成本高。

3. **标准答案集中位置**：选 §6 中 A/B/C 哪个？
   - 我的推荐：**A**。`SlimeAI/DocsAI/Tests/ValidationCatalog.md` 是 AI 友好的单一索引；B 让 OpenSpec spec 文件过大；C 需要写并维护 lint。

4. **角色 subagent 数量**：保留 3/7 还是补齐 7/7？
   - 我的推荐：**补 1 个 planner，共 4/7**。planner 在大型 OpenSpec 提案时有真实需求；implementer / verifier / research-analyst 由默认 agent 承担即可（与当前 systemagent 主轴重合）。

5. **git 默认策略**：保持 manual，还是改为 per-change checkpoint？
   - 我的推荐：**保持 manual**。但同时降低 commit 心智成本：每次任务结束时 AI 给出"建议 commit 范围"（git status + commit message 草稿），用户一行命令即可执行。

6. **Hook 升级**：advisory 保留，还是上 blocking？
   - 我的推荐：**保持 advisory，但脚本利用 inject 接口**（§12）。Claude UserPromptSubmit hook 可注入 system 增强；PostToolUse 仍不阻断但能让 AI 在 transcript 中"看见" advisory，比当前只 stdout 强很多。

7. **端到端验收 wrapper**：是否新建 `Workspace/Tools/run-systemagent-validation.sh` 一键跑"编框架 → 更 submodule → 编游戏 → 跑场景 → 分析"？
   - 我的推荐：**做**。命名建议 `Tools/run-full-validation.sh` 或 `Tools/run-e2e.sh`，分阶段返回失败点，artifact 写到统一目录方便 AI 分析。

8. **`Workspace/DocsAI/AgentWorkflow/SystemAgentOverview.md` 是否新增**？
   - 我的推荐：**新增**。理由见上节。

## 推荐改进方案（蓝图）

### Change 1：紧急修复 + 收尾 + 总览

OpenSpec change 名：`fix-systemagent-paths-and-archive-workflow-changes`

任务（约 5 个一级、20 个二级）：

1. **路径 typo 全局修复**
   - 1.1 grep 全部 5 个 pattern，按文件分组备份替换前后差异。
   - 1.2 修 `.ai-config/` 源（rule + skill + 12 references）。
   - 1.3 修 `AGENTS.md` / `SlimeAI/AGENTS.md` / `.claude/settings.json` / `.codex/hooks.json` / hook 脚本 / `Workspace/DocsAI/**`。
   - 1.4 修 `openspec/Plan/*.md` / `openspec/changes/*/**`。
   - 1.5 修 `SlimeAI/DocsAI/**` / `Games/BrotatoLike/DocsAI/**`。
   - 1.6 跑 `sync-ai-config.sh` 让副本一致。
   - 1.7 验证：手动调用 hook 看到 advisory 输出；`grep` 再次确认 0 处命中。

2. **Archive 三个 complete change**
   - 2.1 `openspec archive unify-ai-agent-development-workflow`（先做，因为它产生最多 baseline）。
   - 2.2 `openspec archive verify-ai-first-runtime-refactor-scenes`。
   - 2.3 `openspec archive clarify-gameos-terminology`。
   - 2.4 验证：`openspec validate --specs --strict` 全通过；`openspec/changes/archive/` 含三新目录；`openspec/specs/` 含新 baseline。

3. **新增 `SystemAgentOverview.md`**
   - 3.1 写架构图（rule + skill + OpenSpec + DocsAI + tests + hooks + subagents + git → systemagent）。
   - 3.2 写组件清单 + 何时使用决策表。
   - 3.3 在 `Workspace/DocsAI/AgentWorkflow/INDEX.md` 添加链接。

4. **修 ProjectState / Plan 中过时数字**
   - 4.1 `SlimeAI/DocsAI/ProjectState.md` 12 reference 已对；检查其他过时表述。
   - 4.2 `openspec/Plan/2026-05-15-ai-first-runtime-refactor-plan.md` "11 reference" → "12 reference"，并在文末标注"第一轮完成，后续大事项见 Change 2 等"。

5. **验证**
   - 5.1 `openspec list --json`：3 个 complete archive，0 个 active complete。
   - 5.2 `grep -rE "SkilmeAI|SlimeAISlimeAI|SlimeAIGames/|SlimeAIWorkspace|SlimeAIResources"`：0 命中。
   - 5.3 hook 手动跑 OK；`sync-ai-config.sh` 干跑无副本漂移。
   - 5.4 框架 `Tools/run-build.sh` / `Tools/run-tests.sh` 通过（这次修了 cd 命令）。
   - 5.5 Godot 主场景 smoke 通过。

预计：1 个会话，纯文本替换 + 命令调用。

### Change 2：流程闭环增强

OpenSpec change 名（暂定）：`close-systemagent-feedback-loop`

任务（按你回答的开放问题确定，蓝图）：

1. **拆独立 `ai-process-retrospective` skill**（开放问题 1）
   - 写 `.ai-config/skills/ai/ai-process-retrospective/SKILL.md`。
   - 输入：本轮 OpenSpec change id 或会话目标；输出：retrospective verdict + process updates + follow-up candidates。
   - 从 `ai-feature-development` 的"功能收尾"段抽出引用入口。

2. **测试场景自动生成机制**（开放问题 2 + B+C）
   - 新 skill `test-scene-design-gate`：作为 capability change 强制阶段。
   - 强化 `systemagent-test-designer` subagent 在 SKILL 中的调用纪律。
   - 在 `ai-feature-development/references/validation-closure.md` 列"capability change → 必须 invoke subagent"。

3. **标准答案 catalog**（开放问题 3 + A）
   - 新文档 `SlimeAI/DocsAI/Tests/ValidationCatalog.md`，scene → expected* 表格。
   - 现有场景 README 精简为"运行命令、依赖、排查"。
   - `GodotSceneValidation.md` 的 L227-287 改为引用 catalog。

4. **planner subagent**（开放问题 4 + 4/7）
   - 新 `.claude/agents/systemagent-planner.md` 和 `.codex/agents/systemagent-planner.toml`。
   - 派生自 `RolePrompts.md` 中的 Planner 段。

5. **hook 升级为 inject 模式**（开放问题 6 + advisory keep）
   - `systemagent_hook.py` 在 UserPromptSubmit 时返回 JSON 注入 systemagent 高层 reminder。
   - PostToolUse 仍不阻断，但 advisory 写入 transcript 让 AI 看见。
   - 文档化"何时升级为 blocking"作为未来 Change 3 候选。

6. **端到端验收 wrapper**（开放问题 7）
   - 新 `Workspace/Tools/run-full-validation.sh`：编框架 → 同步 submodule → 编游戏 → 跑场景列表 → 分析 → 输出统一 artifact。
   - 失败点带 stage 标识 + 跳过条件。

7. **commit 草稿辅助**（开放问题 5）
   - retrospective skill 末端输出"建议 commit 范围 + message 草稿"。
   - 不改全局 git 默认策略。

8. **验证**
   - 全部 capability/test 改动通过新 hook + 新 skill 触发链。
   - 端到端 wrapper 至少一次成功 run。
   - 新 skill / catalog 通过 sync 同步副本无漂移。
   - 新 baseline spec 通过 strict validate。

预计：2-3 个会话，含 hook 脚本写作 + skill 文档 + 端到端 wrapper 实现。

## 风险与权衡

- **路径修复涉及面广**：276 处替换跨 8 个 git 仓边界，必须分批 `git status --short` 防止覆盖用户改动。Change 1 应在开始时和结束时都跑一次 git status diff。
- **archive 可能暴露 baseline 冲突**：`unify-ai-agent-development-workflow` 改了 `agent-protocols` / `ai-feature-development-skill` / `gameos-godot-scene-validation` / `godot-scene-test-tooling` 4 个现有 spec。Archive 时如果旧 baseline 字段与新 delta 不一致，可能需要先修 spec。建议 archive 顺序：先 `clarify-gameos-terminology`（影响最小）→ `verify-ai-first-runtime-refactor-scenes` → `unify-ai-agent-development-workflow`。
- **hook 升级为 inject 有副作用**：每次 UserPromptSubmit 都注入文本会消耗 context tokens。需要控制 reminder 长度（< 200 字）+ 只在"未读 AgentWorkflow"信号时注入。
- **测试场景 gate 误报**：bug fix / docs-only / skill-only 任务不应被强制要求 scene。需要在 gate 中识别任务类型，允许"explicit-skip + reason" 旁路。

## 附录：证据清单（用于复验）

文件计数：

- `.ai-config/skills/` 一级目录 8 个分类；二级 skill 约 25 个。
- `.ai-config/skills/ai/ai-feature-development/references/` 12 个 `.md`。
- `.claude/agents/` 3 个 `.md`；`.codex/agents/` 3 个 `.toml`。
- `Workspace/DocsAI/AgentWorkflow/` 5 份长期 + 6 份 Protocols。
- `openspec/specs/` 28 个 baseline；`openspec/changes/` 4 个 active + 5 个 archived。
- `SlimeAI/Tests/SlimeAI.GameOS.Tests/` 一个 `Program.cs` 测试入口（用户提醒：可以拆 modules）。
- Godot 场景 8 个 `.tscn`（含 BrotatoLike Game/Input 1 个）+ 对应 `.cs` script 8 个。

字段约束：

- `expectedInputs`、`expectedObservations`、`passCriteria`、`failCriteria`、`artifactPath` 非空（README + artifact 同步）。
- PASS marker 模式 `GameOS <Area> <Layer> validation PASS`。
- artifact 必须有 `status / failureReasons / checks / dependencies / notes`。

关键 typo grep 命令：

```bash
cd /home/slime/Code/SlimeAI
grep -rEn "SlimeAISlimeAI|SlimeAIGames|SlimeAIWorkspace|SkilmeAI|SlimeAIResources" \
  --include="*.md" --include="*.json" --include="*.toml" \
  --include="*.yaml" --include="*.py" --include="*.sh" --include="*.mjs" \
  | wc -l
```

修复后该命令应返回 0。
