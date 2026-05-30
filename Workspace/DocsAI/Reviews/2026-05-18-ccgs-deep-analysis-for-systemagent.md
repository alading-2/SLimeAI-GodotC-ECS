# CCGS 对 SystemAgent 的深度影响分析

**分析日期**：2026-05-18  
**来源**：`Resources/Agent/Claude-Code-Game-Studios/`（v1.0.0，全部 Docs + 源文件）  
**对照对象**：`Workspace/SystemAgent/`（v0.2.0）  
**聚焦**：角色结构、Skill Testing Framework、Hook suite、协调规则  
**目标**：精准提炼 CCGS 中对 SystemAgent **真正有帮助**的机制并落地

---

## 1. CCGS 对 SystemAgent 的核心启示（按价值排序）

### 1.1 最重要发现：质量 Rubric 的分类思维

**CCGS 的做法**：`quality-rubric.md` 把 73 个 skill 分成 9 类（gate / review / authoring / readiness / pipeline / analysis / team / sprint / utility），每类定义 4-5 个**二元 PASS/FAIL 指标**。Agent 同理按 6 类分（director / lead / specialist / engine / qa / operations）。

**这意味着**：不同职责的 skill/agent 有本质不同的质量标准。一个 `review` 类 skill 的核心指标是"R1 read-only + R3 verdict 词表合规"，而一个 `pipeline` 类 skill 的核心指标是"P1 output schema + P5 reads-before-writes"。用同一套 lint 规则检查所有 skill 是不够的。

**SlimeAI 现状**：`Tools/skill-test/` 的 R001-R006 都是**结构 lint**（frontmatter、路径引用、副本一致），没有"这类 role 的专属质量标准"。

**可落地**：给每个 Role 和 skill 增加 `function_category` + `rubric` 字段，从 review/analysis/authoring/pipeline/utility 五个功能视角定义 2-3 个专属检查点。

---

### 1.2 `validate-skill-change.sh` 的精准触发思维

**CCGS 的做法**：hook `validate-skill-change.sh` 仅在 PostToolUse(Write/Edit) 且路径匹配 `.claude/skills/**` 时触发，输出 advisory "建议运行 `/skill-test`"。

**精华**：路径过滤 + 精确事件 = 极低误报率。12 个 hook 每个都有明确的 path/command 过滤条件，**不匹配直接 `exit 0`**。

**SlimeAI 现状**：`systemagent_hook.py` 的 PostToolUse 事件处理已有，但对"修改 `.ai-config/skills/` 后提示跑 lint"没有专门触发。

**可立即落地**：在 PostToolUse 分支加一行路径检测：
```python
if any(p.startswith('.ai-config/skills/') for p in changed_paths):
    print("Advisory: skill 文件已修改，建议运行 skill-test lint")
    print("  bash Workspace/SystemAgent/Tools/skill-test/lint.sh static changed --no-fail")
```

---

### 1.3 SubagentStart/Stop Audit Trail

**CCGS 的做法**：`log-agent.sh` (SubagentStart) + `log-agent-stop.sh` (SubagentStop) 记录每次 subagent 调用的完整 audit trail（agent name、prompt、duration、result）。

**价值**：让"AI 工作流的中间过程"可审计，不只是最终 output。

**SlimeAI 现状**：`systemagent_hook.py` 有 `SubagentStop` 分支但处理轻量（主要是提醒 retrospective）。

**可落地**：`SubagentStart` 时写一行 JSON 到 `.ai-temp/subagent-audit.jsonl`：
```json
{"ts": "...", "event": "start", "agent": "systemagent-reviewer", "trigger": "..."}
```
`SubagentStop` 时追加 result 摘要。成本：3-4 行代码。

---

### 1.4 No Unilateral Cross-Domain Changes（跨域禁改原则）

**CCGS 的做法**：`coordination-rules.md` 第 5 条明确：**agent 不得单方面改变不属于自己域的文件**。这条规则写进每个 agent 的 system prompt Forbidden behavior，而不只是 gate 的事后检查。

**SlimeAI 现状**：`RV-IMPL-BOUNDARY` gate 是事后检查。`Reviewer.md` 的 Forbidden behavior 里只写"不写实现代码"，没有"我不能改哪些目录"。

**可立即落地**：在每个 Role 的 Forbidden behavior 中加**领域约束**：
- Planner：不写实现代码；不修改 `.ai-config/` 配置；不改 `SlimeAI/` 框架源码。
- Reviewer：不写/修改任何实现文件；不修改副本路径；不自创 verdict 变体。
- Retrospective：不修改 OpenSpec change 的 proposal/design；不改工具脚本。
- TestDesigner：不实现功能；不修改现有测试逻辑（只补缺口）。

---

### 1.5 Skill Spec Test Template（5 个 Test Case 的结构）

**CCGS 的做法**：`skill-test-spec.md` 模板定义每个 skill 的测试规格，包含：
1. Static Assertions（5 项清单）
2. Director Gate Checks（full / lean / solo）
3. 5 个 Test Case（Happy Path / Failure+Blocked / Mode Variant / Edge Case / Director Gate）
4. Protocol Compliance

**精华**：Test Case 的 `Fixture → Expected behavior → Assertions → Case Verdict` 四段结构，让 LLM 可以"模拟场景 + 按格式验证 skill 指令是否满足期望"。

**SlimeAI 可落地**：为 SlimeAI 的 skill 建立类似模板，对应 ReviewGates 和 review-mode：

```markdown
## Test Cases

### Case 1: Happy Path — [brief name]
**Fixture**: [assumed context]
**Expected behavior**: ...
**Assertions**: - [ ] APPROVE 出现在末尾 - [ ] gate 已引用 RV-*
**Case Verdict**: PASS / FAIL / PARTIAL

### Case 2: REJECT path — [brief name]
**Fixture**: [missing/invalid condition]
**Expected behavior**: [skill reports REJECT, stops]
**Assertions**: - [ ] 输出包含 REJECT - [ ] 未继续执行
**Case Verdict**: PASS / FAIL / PARTIAL

### Case 3: Mode Variant — lean vs solo
**Fixture**: review-mode.txt = lean
**Expected behavior**: 只跑 phase gate，不全量 gate
**Assertions**: - [ ] 只触发 phase_gates 列表中的 gate
**Case Verdict**: PASS / FAIL / PARTIAL
```

---

### 1.6 Role Category Rubric（角色专属质量指标）

这是把 CCGS 的 quality-rubric 思想直接应用到 SlimeAI Role 文件的最高价值迁移。

**CCGS 的 review category rubric（R1-R5）对比 SlimeAI Reviewer：**

| CCGS 指标 | 内容 | SlimeAI Reviewer 现状 |
| --- | --- | --- |
| R1 Read-only enforcement | 不改被评审文件 | ✅ Forbidden: 不写实现代码 |
| R2 N-section check | 逐项检查所有 gate | ✅ Output: 每个gate证据+verdict |
| R3 Correct verdict vocabulary | 只输出 APPROVE/CONCERNS/REJECT | ✅ 引用 VerdictVocabulary |
| R4 No gates during analysis | 分析阶段不产生新任务 | ❌ 未明确 |
| R5 Structured findings | 表格/清单而非散文 | ❌ 只说"末尾聚合verdict"，无格式规定 |

**缺口**：R4（分析阶段不产生新任务）和 R5（结构化表格）在 Reviewer.md 未明确。

**CCGS 的 analysis category rubric（AN1-AN4）对比 SlimeAI Debugger/Verifier：**

| CCGS 指标 | 内容 | SlimeAI 现状 |
| --- | --- | --- |
| AN1 Read-only scan | 分析阶段只用 Read/Grep，不 Write | ❌ 未明确 |
| AN2 Structured findings | findings 表格带 severity/priority | ❌ 未明确 |
| AN3 No auto-write | 建议 fix 前必须用户确认 | ✅ Shared constraints |
| AN4 No director gates | 不产生下游 gate | N/A |

---

### 1.7 Tier × Model 成本设计 → review-mode 复用

**CCGS 的做法**：
- Haiku：`/help`、`/sprint-status`、`/project-stage-detect`、`/changelog` 等只读状态查询
- Opus：`/gate-check`、`/review-all-gdds`、`/architecture-review` 等高风险评审
- Sonnet：主力实现

这是把"模型选择"和"任务风险等级"显式绑定。

**SlimeAI 可落地**：在 Role 文件加 `recommended_model` 字段（建议，不强制）：
- Planner：sonnet（需要上下文综合）
- Reviewer：opus（高风险评审，避免漏检）
- Retrospective：sonnet（结构化分析）
- Debugger/Verifier：sonnet
- Documentarian：haiku（只写文档，成本敏感）

---

### 1.8 Session-State active.md → OpenSpec checkpoint 协议

**CCGS 的做法**：`production/session-state/active.md` + pre/post-compact hook 让会话崩溃后能从文件恢复。

**SlimeAI 现状**：用 `openspec/changes/<change>/tasks.md` 承担了类似功能，但没有明确的"新会话时先读哪里恢复"的协议。

**可落地**：在 `LongRunningPlanProtocol.md` 加"会话恢复协议"段，规定：
- 开启新会话时，先运行 `openspec list` 查 in-progress change
- 读最近 change 的 `tasks.md` 和最后一次 `Reviews/<date>-<topic>.md`
- 从最后已完成 task checkpoint 继续，不重跑

---

## 2. 结构对照表（CCGS vs 当前 SystemAgent）

| 维度 | CCGS v1.0.0 | SlimeAI v0.2.0 | 差距 |
| --- | --- | --- | --- |
| **Agent 层次** | 4 Tier（Director/Lead/Specialist/Engine） | 1 层（4 subagent，平级） | 有意为之的差异（SlimeAI 不需要工作室层次） |
| **Role Rubric** | 9 类 skill × 4-5 指标；6 类 agent × 3-4 指标 | R001-R006 结构 lint，无功能分类 rubric | **缺失** |
| **Skill category** | 9 类（gate/review/authoring/readiness/pipeline/analysis/team/sprint/utility） | 无功能分类（仅按系统模块分：gameos/ecs/openspec/...） | **缺失** |
| **Skill spec test** | 每 skill 一份 5 test case spec 文件 | 无 behavioral spec，只有 static lint | **缺失** |
| **Catalog last_test** | `last_static / last_spec / last_category` 各 result | `systemagent-catalog.yaml` 只有 `id / source / status` | **缺失 last_test 跟踪** |
| **Hook 精确触发** | 每 hook 有明确 path/command 过滤，不匹配 exit 0 | PostToolUse 无 skill 修改专项触发 | **缺失** |
| **Hook audit trail** | SubagentStart/Stop → 每次 audit log | SubagentStop → 轻量提醒 | **缺失完整 audit** |
| **Cross-domain 禁改** | 每 agent prompt 里写"不改域外文件" | Gate 是事后检查，Role 里无域约束 | **缺失预防层** |
| **Reviewer 结构化输出** | R5：structured findings table 要求 | 只说"末尾聚合 verdict"，格式未规定 | **不够具体** |
| **Review mode × Role** | Haiku/Sonnet/Opus 按 skill 风险绑定 | review-mode.txt 存在，但未绑定 role 建议模型 | **部分缺失** |
| **Session recovery** | active.md + compact hook 显式协议 | OpenSpec tasks.md 承担，但无明确恢复协议 | **未文档化** |

---

## 3. 不应照搬清单

| CCGS 机制 | 不照搬原因 |
| --- | --- |
| 49 个 Agent + Studio 层次 | SlimeAI 用户对象是 AI + 框架维护者，不需要"工作室角色扮演"。4 subagent 按工作流阶段命名已够用 |
| 41 个 GDD/UX/艺术 Bible 模板 | 这些是给游戏内容创作者的，SlimeAI 用 OpenSpec spec 替代，不重叠 |
| path-scoped rules 11 个文件 | SlimeAI 通过 owner skill + `contextSources` 间接实现，多 IDE 场景不适合独立 rule 文件 |
| statusline.sh 自动 stage 推断 | Claude Code 专属 UI；Codex/Windsurf 无等价能力，三 IDE 统一不适合引入 |
| session-state/active.md + compact hook | Claude Code 专属 compact event；Codex 无等价；用 OpenSpec tasks.md 更通用 |
| GitHub template + UPGRADING.md 路线 | SlimeAI N=1 下游，独立包无复用对象 |
| 单 IDE（仅 Claude Code）深度 | SlimeAI 必须保持 Claude + Codex + Windsurf 等效，CCGS 的 IDE 专属功能无法照搬 |
| Windows 优先的 bash hook | SlimeAI 主要 Linux 环境 |

---

## 4. 可执行改动列表

下面的改动已可立即执行，按实施顺序排列：

### 4.1 Role 文件：加 `category` + 功能专属 rubric【已执行 → 见下方改动】

每个 Role 文件加 `## Role Category` 节，写明：
- `function_category`：对应 CCGS 的 gate/review/analysis/pipeline/authoring/utility 之一
- `rubric`：2-3 个该类别专属的 PASS/FAIL 指标

### 4.2 systemagent-catalog.yaml：加 `last_spec / last_spec_result / last_category / last_category_result / function_category`【已执行 → 见下方改动】

### 4.3 skill-test/README：加 behavioral spec + category rubric 说明层【已执行 → 见下方改动】

### 4.4 Tools/skill-test/templates/skill-test-spec.md：新建 spec 模板【已执行 → 见下方改动】

### 4.5 systemagent_hook.py：PostToolUse 加 skill-change 检测【已执行 → 见下方改动】

---

## 5. 证据引用

| 结论 | 来源 |
| --- | --- |
| 9 类 skill rubric (G/R/A/RD/P/AN/T/SP/U) | `CCGS Skill Testing Framework/quality-rubric.md:1-250` |
| 6 类 agent rubric (D/L/S/E/Q/O) | `CCGS Skill Testing Framework/quality-rubric.md:177-250` |
| Catalog schema (last_static/spec/category) | `CCGS Skill Testing Framework/catalog.yaml:1-100` |
| Skill spec template (5 test cases) | `CCGS Skill Testing Framework/templates/skill-test-spec.md:1-143` |
| Hook 12 个精确触发设计 | `Docs/DiscoveryMap.md#1.7` |
| `validate-skill-change.sh` 路径过滤 | `Docs/DiscoveryMap.md#1.7` |
| Coordination rules 5 条（含 No Unilateral Cross-Domain）| `Docs/01-ProjectAnalysis/02-StudioModelAndQualityFramework.md#2.3` |
| Tier × Model 成本设计 | `Docs/01-ProjectAnalysis/02-StudioModelAndQualityFramework.md#2.2` |
| 三大缺失（gate库/workflow yaml/skill testing） | `Docs/02-AIFrameworkSystemAgentAnalysis/01-MappingToSlimeAI.md#3` |
| SlimeAI 不是分散是缺路由器 | `Docs/02-AIFrameworkSystemAgentAnalysis/02-DispersionAndCohesionDiagnosis.md#6` |
| 方案 B 轻量集中（已实施） | `Docs/02-AIFrameworkSystemAgentAnalysis/03-PackagingDecisionAndAdoptionPlan.md#1` |
