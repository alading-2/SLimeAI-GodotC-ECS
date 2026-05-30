# SystemAgent 研究采纳治理评估

**日期**：2026-05-18  
**目标**：评估两份 agent 研究采纳文档对 `Workspace/SystemAgent/` 的真实价值，明确哪些机制应保留、试点、降级或拒绝，并将后续动作收敛到 OpenSpec change。  
**输入文档**：
- `Workspace/DocsAI/Reviews/2026-05-18-ccgs-deep-analysis-for-systemagent.md`
- `Workspace/DocsAI/Reviews/2026-05-18-agent-research-adoption-synthesis.md`

## 1. 执行结论

外部 agent 项目的研究对 SystemAgent 有帮助，但真正价值不在于照搬多 agent 平台、session state、锁、replay 或大量 metadata，而在于提炼少量可验证、低漂移、能降低 AI 越界和漏验概率的约束。

本轮建议采用以下原则：

- **保留**：Role `function_category` + rubric、共享 ReviewGates、VerdictVocabulary、skill-test static lint、skill-change advisory hook、ResearchAdoption 的 Evidence/Inference/Unknown 输出形态、Memory / 事实源分层。
- **试点**：少量高风险 skill 的 behavioral spec / permissions、少量高频错误恢复模板。
- **降级**：全量 `status` / `contextSources` / `permissions` frontmatter、全量 behavioral spec、`last_*` 运行字段。
- **拒绝或暂缓**：手工 `tool-calls.jsonl` replay、Multi-Agent lock、Durable incidents、推荐模型字段、ReviewerFSM、AgentCollaborationPatterns。

聚合 verdict：

```text
CONCERNS: 方向有价值，但必须从“批量采纳外部机制”降级为“少数高杠杆约束 + 试点 + 可验证落点”；实施前已有 schema 和文档一致性风险。
```

## 2. Evidence / Inference / Unknown

### Evidence

- `Workspace/SystemAgent/README.md` 与 `INDEX.md` 已明确 `Workspace/SystemAgent/` 是 SystemAgent 唯一正文事实源根，`Workspace/DocsAI/Reviews/` 是历史分析，不作为当前入口。
- `Workspace/SystemAgent/Gates/ReviewGates.md` 已定义 6 个共享 gate，使用 `Trigger / Context to pass / Prompt / Verdicts / Special handling` 五字段。
- `Workspace/SystemAgent/Gates/VerdictVocabulary.md` 已定义 `APPROVE / CONCERNS / REJECT` 三档，并要求 grep 友好的聚合 verdict。
- `Workspace/SystemAgent/Roles/*.md` 已出现 `function_category` 与角色级 PASS/FAIL rubric。
- `Workspace/SystemAgent/Tools/skill-test/` 已有可运行 static lint R001-R006。
- `Workspace/Tools/systemagent-hooks/systemagent_hook.py` 已有 `.ai-config/skills/` / `SKILL.md` 写入后的 skill-test advisory 提醒。
- `Workspace/SystemAgent/Catalog/systemagent-catalog.yaml` 已提升到 `schema_version: 3`，并加入 `function_category`、`spec`、`last_static`、`last_spec`、`last_category` 等字段。
- 实施前 `Workspace/SystemAgent/Catalog/manifest.yaml`、`Workspace/SystemAgent/README.md`、`Workspace/SystemAgent/Policies/DocumentationManagement.md` 仍描述 schema v2 或未记录 schema v3 迁移；本 change 已补充 skill catalog v3 与 package manifest v2 的作用域说明。
- 当前 `.ai-config/skills` 与 `openspec/specs` 未批量引入 `status`、`contextSources`、`permissions` frontmatter。
- 实施前 `2026-05-18-agent-research-adoption-synthesis.md` 的行动计划引用 `3.1.2 Rules action 语义分类`，但正文缺少 `3.1.2` 小节；本 change 已补齐。

### Inference

- Role rubric 和 ReviewGates 属于低成本高收益机制，因为它们直接约束 AI 输出、边界和 verdict，可被 review 和 grep 验证。
- skill-test static lint 是当前最可靠的自动化层；Behavioral Spec 与 Category Rubric 目前更像人工/AI review 流程，不应被描述为完全自动化能力。
- `systemagent-catalog.yaml` 的 schema v3 字段如果不补齐或不标为试点，会形成 metadata debt。
- 全量 frontmatter 迁移会扩大维护面，且与 `workflow-catalog.yaml.must_read`、`systemagent-catalog.yaml.canonical_docs` 存在职责重叠。
- 手工 replay、lock、incident 等机制需要 AI 自觉维护，若没有自动化 hook 支撑，容易增加流程负担而不是提升可靠性。

### Unknown

- CCGS / Maestro / Unagnt 等外部项目机制在 SlimeAI 多 IDE 环境中的长期有效性尚未通过多轮真实任务验证。
- Behavioral Spec 是否能显著提升 skill 质量，需要先对 3-5 个高风险入口做试点。
- skill-change hook 的 payload 在 Claude / Codex / Windsurf 中是否能稳定提供真实文件路径，还需要 hook smoke 或真实编辑场景验证。

## 3. 采纳决策矩阵

该矩阵已对照两份输入文档：CCGS 深度分析中低成本高收益项集中在 Role rubric、ReviewGates、VerdictVocabulary、skill-test static lint 与 advisory hook；综合分析中 Replay、Multi-Agent lock、Durable incidents、全量 frontmatter、推荐模型和 FSM/模式目录等建议均缺少本地验证或维护闭环，因此在本矩阵中降级为试点或拒绝。

| 机制 | 决策 | 理由 | 落点 |
| --- | --- | --- | --- |
| Role `function_category` + rubric | Adopt Now | 直接约束角色行为，已部分落地，低成本 | `Workspace/SystemAgent/Roles/*.md` |
| ReviewGates + VerdictVocabulary | Adopt Now | 已成为 review 和 retrospective 的核心可解析协议 | `Workspace/SystemAgent/Gates/` |
| skill-test static lint R001-R006 | Adopt Now | 可运行、可报告、能发现同步和路径问题 | `Workspace/SystemAgent/Tools/skill-test/` |
| skill-change advisory hook | Adopt Now | 价值明确；本 change 保持 advisory-only，路径检测偏粗作为 follow-up | `Workspace/Tools/systemagent-hooks/systemagent_hook.py` |
| Memory / 事实源分层 | Adopt Now | 低成本减少文档写错位置 | `DocumentationManagement.md` 或 `README.md` |
| Behavioral Spec | Adopt Later | 适合高风险 skill，不适合全量铺开 | 先选 3-5 个 critical/high skill |
| `permissions` frontmatter | Adopt Later | 高风险 skill 有价值，全量会增加 metadata debt | 先选 archive/apply/config 类 skill |
| `contextSources` frontmatter | Reject | 与 catalog/must_read 重叠，容易多事实源 | 优先使用 workflow/catalog |
| 全量 `status` frontmatter | Reject | 40+40 文件迁移成本高，收益不明确 | 暂不进入 lint |
| 手工 `tool-calls.jsonl` replay | Reject | 手工维护不可靠，可能泄漏冗余参数 | Retrospective 记录关键证据即可 |
| Multi-Agent lock | Reject | 当前主要不是并发多 AI，先靠 git boundary | 真实并发后再设计 |
| Durable incidents | Reject | 与 tasks/blocker/retrospective 重叠 | 用现有 tasks.md blocker |
| recommended_model | Reject | 多 IDE 环境不应绑定模型名 | 用 review-mode 表达强度 |
| ReviewerFSM / AgentCollaborationPatterns | Reject | 解释性强、执行约束弱 | 暂不新增文档 |

## 4. 实施前一致性问题与本 change 处理

### 4.1 schema 版本漂移

实施前 `systemagent-catalog.yaml` 已为 schema v3，但 README、manifest 和 DocumentationManagement 仍描述 v2。后续必须二选一：

1. 正式接受 schema v3，并同步更新 `manifest.yaml`、`README.md`、`DocumentationManagement.md`。
2. 将 `systemagent-catalog.yaml` 的 v3 字段降级为试点扩展，并明确不属于全局 schema 迁移。

本 change 采用第 1 种，并只把 v3 定义为“skill catalog schema”，不把它解释为整个 SystemAgent package schema 已全面升级。

### 4.2 三层 skill-test 状态不清

`Tools/skill-test/README.md` 实施前描述三层测试体系，但实际自动化主要是 Static Lint。Behavioral Spec 与 Category Rubric 应标注为 pilot/manual，否则会造成能力夸大；本 change 已将三层状态写入 skill-test README。

### 4.3 catalog metadata 未闭环

`systemagent-catalog.yaml` 中 `last_static`、`last_spec`、`last_category` 多为空，且不是所有 skill 都有 `function_category`。本 change 将这些字段分类为 required、optional 或 pilot/manual：`function_category` 面向已纳入 rubric 覆盖的 SystemAgent-owned 高风险入口、收尾、维护或 wrapper skill，未纳入覆盖时可省略；`spec`、`last_spec`、`last_category` 属于 pilot/manual；`last_static` 属于可选人工快照。

### 4.4 研究综合文档编号缺口

`2026-05-18-agent-research-adoption-synthesis.md` 行动计划引用 `3.1.2 Rules action 语义分类`，实施前正文缺少对应小节；本 change 已补齐该小节，并把长期规则落入 `Workspace/SystemAgent/Policies/DocumentationManagement.md`。

### 4.5 Verdict 格式兼容性

任何 Reviewer 扩展字段都不得替代 `VerdictVocabulary.md` 要求的聚合 verdict 行。若需要回归阶段字段，应保留末尾：

```text
CONCERNS: <issue>
```

并附加：

```text
remediation_phase: plan|implement|test|docs
```

而不是改为 `verdict: CONCERNS`。

### 4.6 skill-change hook 已知 follow-up

本 change 不修改 `Workspace/Tools/systemagent-hooks/systemagent_hook.py`。当前 skill-change hook 仍保持 advisory-only；路径检测启发式偏粗的问题作为后续修复项保留，后续若修改 hook，应优先解析真实 changed paths，而不是扩大文本匹配。

## 5. 推荐 OpenSpec 任务分批

### Batch 1：一致性修正

- 同步 schema 版本说明。
- 修正研究文档 `3.1.2` 缺口。
- 标注 skill-test 三层状态：implemented / pilot / manual。
- 明确 catalog v3 字段适用范围。

### Batch 2：保留高价值机制

- 保留并核对 Role rubric。
- 保留 ReviewGates / VerdictVocabulary 作为唯一 verdict/gate 协议。
- 保留 skill-change advisory hook，并将路径检测从文本启发式逐步收紧为真实路径解析。

### Batch 3：试点而非全量迁移

- 仅为 3-5 个高风险 skill 建 behavioral spec。
- 仅为 archive/apply/config 类高风险 skill 试点 `permissions`。
- 不引入全量 `status`、`contextSources`、Replay、Multi-Agent lock、Durable incidents。

### Batch 4：文档治理增强

- 在 DocumentationManagement 或 README 中加入 Memory / 事实源分层表。
- 仅在高频失败出现后新增 Recovery 模板。

## 6. 验证边界

本次生成的是文档与 OpenSpec 计划，不触及框架 C# 代码、Godot 场景、DataOS schema 或游戏仓实现。验证边界应为：

- `openspec validate govern-systemagent-research-adoption --strict`
- `openspec list --json`
- `find openspec/changes/govern-systemagent-research-adoption -maxdepth 4 -type f | sort`
- 最终 `git status --short` 范围确认

## 7. External Resources 记录

```yaml
externalResources:
  enabled:
    - agent-reference
  scope:
    - Workspace/DocsAI/Reviews/2026-05-18-ccgs-deep-analysis-for-systemagent.md
    - Workspace/DocsAI/Reviews/2026-05-18-agent-research-adoption-synthesis.md
  reason: 用户指定两份外部 agent 项目研究采纳总结，要求判断其对 SystemAgent 的真实价值并生成任务。
  expires: current-task
copiedCodeOrAssets: none
```

## 8. Risk Profile

- [x] Git 边界跨越：只在工作区根仓生成文档与 OpenSpec change；不进入 `SlimeAI/` 或 `Games/BrotatoLike/` 修改。
- [x] 用户已有改动可能被影响：当前已有研究文档未提交修改，后续改动需避免覆盖。
- [ ] 跨仓库提交：本任务不提交。
- [ ] 不可逆操作（rm / push --force）：不涉及。
- [ ] 外部 API 调用：不涉及。
