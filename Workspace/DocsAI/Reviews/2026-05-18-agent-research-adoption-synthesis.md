# Agent 研究采纳综合分析

**分析日期**：2026-05-18  
**覆盖项目**：10 个外部 AI agent 项目（`Resources/Agent/`）  
**对照对象**：`Workspace/SystemAgent/`（v0.2.x；`manifest.yaml` schema_version: 2，`systemagent-catalog.yaml` schema_version: 3）
**分析方向**：从外部项目提炼可落地改进，增强 SlimeAI SystemAgent 的鲁棒性、可观测性和工程纪律

**治理边界**：本文是历史研究分析，不是当前 SystemAgent workflow、role、gate 或 policy 入口。长期生效规则必须进入 `Workspace/SystemAgent/` 正文事实源或 `openspec/specs/` 基线；本轮采纳以 `2026-05-18-systemagent-research-adoption-governance-review.md` 的 Adopt Now / Adopt Later / Reject 矩阵为准。

---

## 1. 研究覆盖范围

| 项目 | 技术栈 | 分析深度 | 最高价值 |
| --- | --- | --- | --- |
| **Maestro** | Go FSM | 🟢 深度 | 状态机规格、三态决议、迭代预算、错误分类 |
| **Claude-Code-Agent-Farm** | Python | 🟢 深度 | 多 AI 协调锁、Doctor preflight、状态 JSON |
| **Agents-Council** | Python/TS | 🟢 深度 | 跨工具上下文共享、状态标注、离线文档 |
| **Unagnt** | Python/YAML | 🟢 深度 | DAG workflow、Policy 测试、Replay artifact |
| **Claude-Code-Game-Studios** | TypeScript | 🔵 首轮 | Verdict 词表、Review gates、Skill testing |
| **Claude-Corps** | TS/Claude Code | 🟡 浅扫 | review.json 配置、Verify Iron Law |
| **SwarmClaw** | TS/Next.js | 🟡 浅扫 | Agent 回归测试、Conversation-to-skill |
| **Swarms** | Python | 🟡 浅扫 | 多 Agent 架构模式目录 |
| **Pantheon-Studio** | TS/Node.js | 🟡 浅扫 | LiveOps 流水线（与 SystemAgent 关联低）|
| **BDD** | C#/Ruby | 🟢 深度 | Gherkin 行为契约（已由 BDDSceneFormat.md 采纳）|

---

## 2. SystemAgent 现状盘点

> 以下是 `Workspace/SystemAgent/` v0.2.0 **已完成**的能力，供后续缺口分析对照。

### 2.1 已完成的高价值迁移

| 来源项目 | 已迁移机制 | 实现位置 |
| --- | --- | --- |
| CCGS | 共享 gate 库（6 ID） | `Gates/ReviewGates.md` |
| CCGS | 标准化 Verdict 词表（APPROVE/CONCERNS/REJECT） | `Gates/VerdictVocabulary.md` |
| CCGS | workflow-catalog.yaml 流程数据化 | `Catalog/workflow-catalog.yaml` |
| CCGS | manifest.yaml + README 单一入口 | `Catalog/manifest.yaml`、`README.md` |
| CCGS | review-mode 三档（full/lean/solo） | `Config/review-mode.txt`（默认 lean） |
| CCGS | Skill testing framework (R001–R006) | `Tools/skill-test/` |
| BDD | BDD 行为契约格式 | `BDDSceneFormat.md` |
| 多项目 | 统一事实源根 + 路由索引 | `INDEX.md` |
| 多项目 | 6 Workflow 正文 | `Workflows/` |
| 多项目 | 9 Role 正文 | `Roles/` |
| 多项目 | 4 Policy 文件 | `Policies/` |
| 多项目 | 6 Protocol 文件 | `Protocols/` |

### 2.2 SlimeAI 相对外部项目的强项（不要丢）

| 强项 | 证据 | 说明 |
| --- | --- | --- |
| **OpenSpec 一等公民** | 30+ archived specs | 其他项目无规格基线层 |
| **多 IDE 同源同步** | `.ai-config/` + sync-ai-config.sh | 三 IDE 副本自动生成 |
| **运行时验证闭环** | run-godot-scene.sh + 5字段 artifact | 真实编译/测试而非 LLM 自评 |
| **跨 git 边界纪律** | AGENTS.md + 4 边界约定 | 其他项目单仓无此约束 |
| **AI 自动 commit** | rules.md 明确授权 + 步骤 | 其他项目多要求用户授权 |
| **Capability owner skill** | gameos/<system>/SKILL.md 直接绑定 | 直接映射游戏运行时 capability |
| **AI retrospective 强制** | ai-process-retrospective skill 末尾调用 | 其他项目为用户主动触发 |

---

## 3. 发现的缺口（按优先级）

### 3.1 P0：立即可做（改文档，不改代码）

#### 3.1.1 Reviewer 角色三态协议（来源：Maestro）

**现状**：`Roles/Reviewer.md` 有 gate 引用，且 `VerdictVocabulary.md` 已要求末尾聚合 verdict 可被 `grep -E '^(APPROVE|CONCERNS|REJECT)'` 解析。
**缺口**：CONCERNS / REJECT 应能附带回归相位（plan/implement/test/docs），但附加元数据不能替代聚合 verdict 行。
**做法**：更新 `Workspace/SystemAgent/Roles/Reviewer.md` 或相关 review 文档时，只允许把回归相位作为附加字段；末尾仍保留 `APPROVE / CONCERNS / REJECT` 开头的聚合 verdict 行：
```
remediation_phase: plan|implement|test|docs  （CONCERNS/REJECT时填）
reason: ...
CONCERNS: <summary>
```
REJECT + `remediation_phase: plan` → 必须重走 Planner；不允许"不通过但不指明回归点"。
**验证**：一次完整任务评审中，reviewer 输出可被 `grep -E '^(APPROVE|CONCERNS|REJECT)'` 解析。

#### 3.1.2 Rules action 语义分类（来源：Unagnt）

**现状**：SystemAgent 已区分 `.ai-config/` 源、同步副本、hook/subagent 运行配置和 `Workspace/SystemAgent/` 正文事实源，但规则动作的语义仍散在 rule、policy 与 workflow 文档中。
**缺口**：AI 容易把“读取参考”“修改维护源”“运行同步”“advisory hook 提醒”混为同一种动作，导致直接改副本或把历史分析当事实源。
**做法**：先用文档级语义分类约束，不新增全量 frontmatter 或 lint 规则：
- `read_reference`：只读参考资料或历史分析，不把结论视为当前入口。
- `edit_source`：只修改明确维护源，例如 `Workspace/SystemAgent/` 或 `.ai-config/`。
- `sync_generated`：由同步脚本生成副本，不手写副本。
- `advisory_check`：hook 或 review 只提示风险，不自动改写文件。

**验证**：文档治理 review 能指出每个长期规则的事实源位置；发现只存在于 `Workspace/DocsAI/Reviews/` 的长期规则时，必须迁入 `Workspace/SystemAgent/` 或 OpenSpec baseline。

#### 3.1.3 Capability / Skill 状态标注（来源：Agents-Council）

**现状**：各 capability spec 和 SKILL.md 无稳定性状态标注。  
**缺口**：AI 无法判断"这个 capability 是稳定的还是实验性的"。  
**做法**：给所有 OpenSpec `specs/<cap>/spec.md` 和 `.ai-config/skills/<cat>/<name>/SKILL.md` 加 frontmatter：
```yaml
status: experimental | beta | stable | deprecated
```
写一个 lint：`grep -rL '^status:' openspec/specs/ .ai-config/skills/` 报告缺状态的文件。  
**验证**：`find openspec/specs -name spec.md | xargs grep -L '^status:'` 输出为空。

#### 3.1.4 Reviews 模板加 risk profile（来源：Unagnt）

**现状**：`Workspace/DocsAI/Reviews/<date>-<topic>.md` 无风险维度记录。  
**缺口**：回顾文档缺少"本次操作的风险维度"清单，导致高风险操作无法事后审计。  
**做法**：在所有新建 review 文档末尾加 risk profile 段：
```markdown
## Risk Profile
- [ ] Git 边界跨越
- [ ] 用户已有改动可能被影响
- [ ] 跨仓库提交
- [ ] 不可逆操作（rm / push --force）
- [ ] 外部 API 调用
```
**验证**：最近 3 份 Reviews/ 文档包含此段。

---

### 3.2 P1：高价值，1–2天可完成

#### 3.2.1 错误恢复 mini-template 目录（来源：Maestro）

**现状**：错误处理散在各 skill 的"故障排查"段落，无统一分类。  
**缺口**：常见错误（git commit 失败 / submodule 更新失败 / build 失败 / Godot scene 超时 / openspec archive 失败）无专门 prompt。  
**做法**：创建 `Workspace/SystemAgent/Recovery/` 目录，每类错误一个 markdown：
- `git-commit-failure.md`：症状 + 立即停止 + 诊断命令 + 修复行为 + 不要做什么
- `submodule-update-failure.md`
- `build-failure.md`
- `godot-scene-timeout.md`
- `openspec-archive-failure.md`

Advisory hook 在检测到对应错误关键词时引导 AI 读对应文件。  
**验证**：故意触发一次 submodule update 冲突，AI 是否引用 `submodule-update-failure.md` 的步骤。

#### 3.2.2 Replay artifact 协议（来源：Unagnt）

**现状**：OpenSpec change 执行中无执行 trace；只有 git log 和 Reviews 文档。  
**缺口**：Retrospective 阶段无法重放执行过程，无法 diff "计划 vs 实际"。  
**做法**：在 `Workspace/SystemAgent/Protocols/LongRunningPlanProtocol.md` 加"执行 trace 录制"段，约定：
```
openspec/changes/<change>/runs/<timestamp>/tool-calls.jsonl
```
AI 执行时每次工具调用写一行 `{"tool": ..., "args": ..., "result_summary": ..., "ts": ...}`。  
Retrospective 阶段引用这个 artifact 作为"过程证据"。  
**验证**：一次 in-progress change 执行后，`runs/` 目录下有可读的 `tool-calls.jsonl`。

#### 3.2.3 Skill contextSources 显式声明（来源：Unagnt）

**现状**：每个 skill 的"前置必读"散在 SKILL.md 正文中，格式不统一。  
**缺口**：AI 进入 skill 时不能批量检查"该读的文件是否都读了"。  
**做法**：给每个 SKILL.md 加 frontmatter：
```yaml
contextSources:
  - SlimeAI/DocsAI/INDEX.md
  - openspec/specs/<capability>/spec.md
  - Workspace/SystemAgent/Gates/ReviewGates.md
```
与 `openspec instructions apply --json` 返回的 `contextFiles` 对齐；skill-test R00x 规则加一条：`contextSources` 字段存在且路径有效。  
**验证**：`skill-test static all` 的 R00x 通过率 ≥ 95%。

#### 3.2.4 Skill permissions 显式声明（来源：Unagnt）

**现状**：skill 文档默认"AI 想做什么就做什么"，没有权限约束声明。  
**缺口**：`openspec-archive-change` skill 执行时不知道"我不应该有 git push 权限"。  
**做法**：给 SKILL.md 加：
```yaml
permissions:
  allow: [archive-spec, commit, sync-ai-config]
  require_approval: []
  deny: [git-push, delete-user-changes]
```
AI 进入 skill 时检查当前操作是否在 permissions 范围内。  
skill-test 加一条 lint 规则：所有 skill 至少有 `permissions` 字段。  
**验证**：5 个高风险 skill 的 permissions 写完后，skill-test lint 通过。

#### 3.2.5 4-layer Memory 显式制图（来源：Unagnt）

**现状**：SlimeAI 的"记忆"分散在 chat history / DocsAI / OpenSpec specs / git log，没有显式分层。  
**缺口**：AI 不知道"我现在写的这个文档应该进哪一层"，容易写错位置。  
**做法**：在 `Workspace/SystemAgent/README.md` 或 `INDEX.md` 加一张"记忆分层图"：

| 层 | 类型 | SlimeAI 实体 | 生命周期 |
| -- | -- | -- | -- |
| Working | 当前会话 | chat session 上下文 | 会话结束丢弃 |
| Persistent | 跨会话记录 | `Workspace/DocsAI/Reviews/<date>-<topic>.md` | 显式存档 |
| Semantic | 基线知识 | `openspec/specs/` + `SlimeAI/DocsAI/` | 长期稳定 |
| Event Log | 历史事件 | `git log` + OpenSpec changes 历史 | 追加不可变 |

AI 写文档前先判断"这属于哪一层，应该写到哪里"。  
**验证**：下一次 Review 文档放到 `Workspace/DocsAI/Reviews/` 而非工作区根。

---

### 3.3 P2：中优先级，设计后推进

#### 3.3.1 BUDGET_REVIEW / 迭代预算检查点（来源：Maestro）

**现状**：`Protocols/LongRunningPlanProtocol.md` 存在，但无迭代预算硬天花板概念。  
**缺口**：AI 实现者改文件超 N 次、reviewer 反复要求修改超 M 次，没有强制"budget checkpoint"触发。  
**做法**：在 `LongRunningPlanProtocol.md` 加"迭代预算"节：
- Implementer 改同一文件超 5 次：触发 budget checkpoint
- Reviewer 连续 REJECT 超 3 次：触发 budget checkpoint  
- Checkpoint 由 Retrospective role 评估：CONTINUE / PIVOT / ESCALATE-TO-USER / ABANDON
- 结果写到 `openspec/changes/<change>/budget-checkpoints/`

**风险**：无法自动检测次数，靠 AI 自觉。先做 advisory 版，不强制。

#### 3.3.2 SUSPEND 协议（外部服务不可用）（来源：Maestro）

**现状**：网络抖动 / API 限流靠用户重试，无协议化。  
**做法**：在 `LongRunningPlanProtocol.md` 加"外部服务暂停协议"节，规定：
- 检测到反复 API 错误时，AI 必须输出"挂起卡片"
- 挂起卡片包含：当前 change、当前 task、in-flight 文件列表、需要哪些 API 健康
- 恢复时读挂起卡片续跑，不重头
- 卡片存于 `openspec/changes/<change>/suspend-<timestamp>.md`

#### 3.3.3 Durable Incidents（跨会话未解事项）（来源：Maestro）

**现状**：未解事项靠 OpenSpec tasks.md checkbox，没有"AI 主动 open incident"协议。  
**做法**：Retrospective 阶段必须列出 open incidents（未解问题/未实现 task/未达验收能力）。写到 `openspec/changes/<change>/incidents.md`。下次同 change 启动时先读 incidents。  
**风险**：增加文档碎片；建议 change archive 时清理 inactive incidents。

#### 3.3.4 Multi-Agent Coordination Protocol（来源：Claude-Code-Agent-Farm）

**现状**：单 AI 顺序操作；无"多 AI 工具同时改"的协调协议。  
**做法**：新建 `Workspace/SystemAgent/Protocols/MultiAgentCoordinationProtocol.md`，约定：
1. 任何 AI 工具开始 OpenSpec change 前：写 lock 声明（capability + 文件范围 + 估时）至 `.coordination/agent_locks/`
2. 冲突时检查 lock 是否 stale（>1h）
3. Stale → 接管；否则进入 `planned_work_queue`
4. Retrospective 阶段读 `completed_log` 验证

**风险**：AI 偶尔忘记写 lock；可用 retrospective 校验。

#### 3.3.5 BestPractices 目录（来源：Claude-Code-Agent-Farm）

**现状**：`SlimeAI/DocsAI/` 偏架构总览，缺"日常怎么写"的清单文。  
**做法**：创建 `SlimeAI/DocsAI/BestPractices/` 目录，按 capability 写：
- `OpenSpecChangeAuthoring.md`（应当 / 不应当 + 反例 + 关联 ADR）
- `GodotSceneValidation.md`
- `DataOSAuthoring.md`

风格参考 Claude-Code-Agent-Farm 的 38 篇 `<STACK>_BEST_PRACTICES.md`，但只做和 SlimeAI 相关的。

#### 3.3.6 ADR 编号 + 6-section 模板（来源：Unagnt）

**现状**：`SlimeAI/DocsAI/ArchitectureDecisionRecords/` 存在但格式不统一。  
**做法**：给现有 ADR 编号（0001、0002...），强制 6-section：
`Status / Context / Decision / Alternatives Considered / Consequences / Related`  
在 INDEX.md 加 ADR 索引段。将 ADR 目录作为显式"pattern 库"，AI 入手新任务时按 capability 检索。

#### 3.3.7 /verify 证据核查 skill（来源：Claude-Corps）

**现状**：没有统一的"证据核查"入口；结论可能无证据支撑。  
**做法**：新建 `.ai-config/skills/core/verify/SKILL.md`，要求：
- 核心 Iron Law：无证据不声称
- 输出 anti-rationalization 表格：`claim | evidence | source`
- Red flags list：无 git commit 引用、无日志引用、无测试输出

这是"Reviewer + Retrospective 的前置 sanity check"。

---

### 3.4 P3：可选，低成本设计借鉴

#### 3.4.1 Multi-Agent 架构模式目录（来源：Swarms）

在 `Workspace/SystemAgent/` 加一篇 `AgentCollaborationPatterns.md`，描述 SlimeAI 实际用到的模式：
- **Sequential**：planner → implementer → reviewer → retrospective（标准 workflow）
- **Hierarchical**：planner 制定 plan → 多个 AI 工具分工实现
- **MoA（Mixture of Agents）**：多 reviewer 并行 + aggregator 汇总（用于大型 change 评审）

不引入 Python 库，纯概念文档化。

#### 3.4.2 Task 依赖标记（来源：Unagnt）

OpenSpec `tasks.md` 每个 task 后加注释 `depends_on: 1.1`。Retrospective 检查依赖图，发现可并行执行的 task 组。不引入 YAML / CEL。

#### 3.4.3 Doctor Preflight（来源：Claude-Code-Agent-Farm）

给重型 skill（`ai-feature-development`、`openspec-apply-change`、Godot validation）加 doctor 子流程，检查：
- 相关文件存在
- git 状态干净
- `openspec validate` 通过
- 目标 capability 是否存在

写 `Workspace/Tools/skill-doctor.sh` 试点。

#### 3.4.4 Skill 功能分类 tag（来源：Claude-Corps）

给 SKILL.md frontmatter 加 `function_category: [plan|execute|review|quality|tool]`，让 AI agent 按任务阶段自动检索。注意这是叠加标签，不替换现有模块分类。

#### 3.4.5 Markdown FSM 试点（来源：Maestro）

对 Reviewer 角色画一个 mermaid 状态机（不超过 8 个状态），写到 `Roles/ReviewerFSM.md`。验证是否比现有散文描述更清晰。**先做 1 个角色**，不强行给所有角色画。

---

## 4. 推荐行动计划（分批次）

| 批次 | 内容 | 成本 | 影响 |
| --- | --- | --- | --- |
| **批次 1**（立刻，改文档）| 3.1.1 三态 Reviewer 协议 + 3.1.2 Rules action 语义分类 + 3.1.3 Status frontmatter + 3.1.4 Reviews risk profile | 1 天 | 立刻改善 review 质量和规则清晰度 |
| **批次 2**（协议化）| 3.2.1 错误恢复 mini-template（5个）+ 3.2.2 Replay artifact 协议 + 3.2.5 Memory 分层图 | 1 天 | AI 行为可观测、错误处理结构化 |
| **批次 3**（Skill 增强）| 3.2.3 contextSources frontmatter + 3.2.4 permissions frontmatter + 3.3.6 ADR 编号规范 | 1.5 天 | Skill 自文档化、权限显式化 |
| **批次 4**（协议扩充）| 3.3.1 BUDGET_REVIEW + 3.3.2 SUSPEND + 3.3.3 Durable Incidents + 3.3.4 Multi-Agent 协调锁 | 2 天 | 长任务稳定性、多工具协作 |
| **批次 5**（内容增补）| 3.3.5 BestPractices 目录（先 1 篇）+ 3.3.7 /verify skill + 3.4.1 架构模式目录 | 1.5 天 | 日常开发质量提升 |
| **可选** | 3.4.2–3.4.5 各项 | 按需 | 收益递减但无副作用 |

---

## 5. 不应照搬清单（明确拒绝）

| 来源 | 机制 | 原因 |
| --- | --- | --- |
| Maestro | Docker per agent 沙箱 | SlimeAI 不是多并发软件工厂；引入 Docker 成本远超收益 |
| Maestro | SQLite 全量持久化 | 双源同步问题；git + markdown 已足够 |
| Maestro | agentsh shell shim | AI 工具已有自己的命令审批机制 |
| Maestro | 单二进制 APT/Brew 分发 | SlimeAI 不是端用户 CLI |
| Maestro | Hotfix mode 专属角色 | OpenSpec 小修复豁免条款已覆盖 |
| Maestro | 强等级 PM/Architect/Coder 三层 | SlimeAI 用户对象是 AI + 框架维护者，不是"软件团队代理" |
| Unagnt | CEL DSL 引擎 | SlimeAI AI 工具读 markdown，不能执行 CEL |
| Unagnt | K8s CRD + Operator + Helm | SlimeAI 不是部署型软件 |
| Unagnt | 多租户 / SSO / Enterprise auth | 单用户工作区 |
| Claude-Code-Agent-Farm | Tmux 多 pane 跑 N 个 LLM | 不是横向扩 LLM 的场景 |
| Claude-Code-Agent-Farm | 38 个 Tech Stack 配置 | SlimeAI 单栈：Godot + .NET |
| CCGS | 49 个 subagent 扩展 | 扩数量只会稀释 prompt 质量 |
| CCGS | 41 个 GDD/艺术/经济模板 | 内容创作模板与游戏框架开发不重叠 |
| CCGS | GitHub template 独立包路线 | N=1 下游项目，独立包无复用对象 |
| SwarmClaw | 通用 agent 平台架构 | 1793 文件 Next.js + Electron，与 Godot 框架方向完全不同 |
| SwarmClaw | 社交平台 connector（Discord/Slack） | SlimeAI 专注游戏内 runtime |
| Swarms | Python LiteLLM wrapper | SlimeAI 不需要统一 LLM 接入层 |
| Claude-Corps | Plugin marketplace 路线 | 深度绑定 Claude Code 生态 |
| Claude-Corps | 14+ 专业 reviewer agent | 对游戏框架过重；保留框架相关 reviewer 即可 |
| Agents-Council | Electrobun 桌面 app | SlimeAI 不是端用户产品 |

---

## 6. 关键洞察总结

### 6.1 工程纪律 vs 提示词工程

Maestro 的核心主张是：**"没有任何单一 gate 是万能的，必须叠合"**。它的质量保证来自 7 层独立验证（模板类型、approval gate、TESTING、coverage、lint、sandbox、持久化），而 SlimeAI 当前主要靠"Runtime tests + Godot scene + reviewer 自然语言"。

可落地的方向不是引入 Docker/SQLite，而是：
- **错误分类化**（mini-template，见 3.2.1）
- **迭代预算显式化**（budget checkpoint，见 3.3.1）
- **证据要求规范化**（/verify skill，见 3.3.7）

### 6.2 声明式 vs 散文式

Unagnt + CCGS 共同揭示的一个规律：**声明式的东西（YAML/frontmatter/表格）比散文更容易被 AI 正确消费和 lint 验证**。

SlimeAI 可以在保留 markdown 的前提下，把关键结构化信息声明到 frontmatter：
- `status:`（稳定性，见 3.1.3）
- `contextSources:`（前置必读，见 3.2.3）
- `permissions:`（操作权限，见 3.2.4）

这不改变 markdown 的人类可读性，但让机器检查成为可能。

### 6.3 可观测性赤字

SlimeAI 当前的"可观测性"主要是：`git log` + `DocsAI/Reviews/` + `openspec list`。相比 Maestro（SQLite + WebUI）、SwarmClaw（OpenTelemetry + 仪表板），差距明显。

但不需要引入数据库或可视化平台。最小可行补丁是：
- **Replay artifact**（tool-calls.jsonl，见 3.2.2）—— AI 执行 trace 可审计
- **Risk profile**（Reviews 模板段，见 3.1.4）—— 操作风险可分类
- **Memory 分层图**（见 3.2.5）—— 知识分层可追溯

这三件事加起来，把"AI 在做什么 + 做到哪里了 + 风险是什么"都显式化了。

### 6.4 SlimeAI 的真正护城河

研究后反而更清楚：SlimeAI 的护城河不是 prompt 数量或 agent 角色数，而是：
1. **运行时验证闭环**（Godot scene + dotnet test，不是 LLM 自评）
2. **OpenSpec 规格基线**（capability 有正式 spec，不只是 prompt 描述）
3. **多 IDE 同源**（三工具共用一套 skill，不被单 IDE 锁死）
4. **跨 git 边界纪律**（多仓库场景下的原子性保障）

这四点是外部项目都没有的，也是任何"加 skill / 加 agent"改进方案的基础。改进的方向是**在这四点基础上叠加可观测性、声明式约束和错误恢复协议**，而不是模仿外部项目的架构。

---

## 7. 证据引用

| 结论 | 主要来源 |
| --- | --- |
| 三态决议协议 | `Maestro/Docs/06-MigrationToSlimeAI.md#1.6` + `04-QualityAndValidation.md#2.2` |
| BUDGET_REVIEW | `Maestro/Docs/03-AgentAndPromptLifecycle.md#5` |
| 错误恢复 mini-template | `Maestro/Docs/06-MigrationToSlimeAI.md#1.5` |
| Multi-Agent 协调锁 | `Claude-Code-Agent-Farm/Docs/05-MigrationToSlimeAI.md#1.1` |
| Replay artifact | `Unagnt/Docs/06-MigrationToSlimeAI.md#1.2` |
| contextSources + permissions | `Unagnt/Docs/06-MigrationToSlimeAI.md#1.3, 1.5` |
| Action 语义分类 | `Unagnt/Docs/06-MigrationToSlimeAI.md#1.6` |
| Status frontmatter | `Agents-Council/Docs/04-MigrationToSlimeAI.md#1.3` |
| 4-layer memory | `Unagnt/Docs/06-MigrationToSlimeAI.md#2.1` |
| Risk profile | `Unagnt/Docs/06-MigrationToSlimeAI.md#2.3` |
| BestPractices 目录 | `Claude-Code-Agent-Farm/Docs/05-MigrationToSlimeAI.md#1.4` |
| ADR 编号 + 6-section | `Unagnt/Docs/06-MigrationToSlimeAI.md#1.1` |
| /verify Iron Law skill | `Claude-Corps/Docs/DiscoveryMap.md#6.3` |
| Review gates + Verdict | `Claude-Code-Game-Studios/Docs/02-AIFrameworkSystemAgentAnalysis/01-MappingToSlimeAI.md#3.1` |
| Multi-agent 架构模式 | `Swarms/Docs/DiscoveryMap.md#6.1` |
| Agent regression suite | `SwarmClaw/Docs/DiscoveryMap.md#6.1` |
| Durable Incidents | `Maestro/Docs/06-MigrationToSlimeAI.md#2.5` |
| SUSPEND 协议 | `Maestro/Docs/06-MigrationToSlimeAI.md#1.4` |
