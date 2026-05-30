# 2026-05-18 SystemAgent MVP 统一收敛分析

> 范围：基于用户已完成的目录移动重新分析 SystemAgent MVP。当前方向已经固定：`Workspace/SystemAgent/` 是唯一源头；原 `Workspace/DocsAI/AgentWorkflow/` 不再作为入口；原 `Workspace/SystemAgent/` 包内容已临时移动到 `Workspace/SystemAgent/SystemAgent/`，后续需要整理进统一结构。
>
> 本文是分析与 OpenSpec 设计输入，不是新的长期事实源。正式落地应新建 OpenSpec change，修改 `openspec/specs/systemagent-*`、规则入口、skill/subagent/hook 引用和同步脚本。

## 1. 固定决策

这次不再做“保留旧入口 / 多源过渡”的方案。SystemAgent 的目标形态固定为：

- **唯一源头**：`Workspace/SystemAgent/`。
- **其它位置**：只允许保留短引用、生成副本或运行时配置，不保存 SystemAgent 流程正文。
- **命名**：沿用 `SystemAgent` 作为目录和系统名，不再另起其它名字。
- **Context7**：属于 IDE / CLI 外部工具设置，不是 SystemAgent 内容源，也不进入 SystemAgent 文档策略。
- **外部资源**：作为可选参考资源登记给 AI；默认不预加载、不自动扫描。AI 可按任务需要自行判断是否查看，但必须记录理由、范围和结论。
- **OpenSpec**：需要新建 change，正式修改已有 baseline 中“`Workspace/SystemAgent/` 只索引、不接管事实源”的旧约束。

当前物理状态已经接近最终方向：

```text
Workspace/SystemAgent/
  INDEX.md
  BDDSceneFormat.md
  ResearchAndAdoptionNotes.md
  RolePrompts.md
  SystemAgentOverview.md
  SystemAgentWorkflow.md
  Protocols/
  SystemAgent/
    README.md
    manifest.yaml
    workflow-catalog.yaml
    review-gates.md
    verdict-vocabulary.md
    review-mode.txt
    catalog/
    tools/
```

但这个结构仍有一个明显问题：`Workspace/SystemAgent/SystemAgent/` 是“旧包根嵌套在新根里”，AI 会困惑哪个 `SystemAgent` 才是入口。因此后续 OpenSpec MVP 必须整理这个嵌套目录。

## 2. 当前核心问题

### 2.1 旧 baseline 已经过时

当前 `openspec/specs/systemagent-package/spec.md` 曾要求：

- `Workspace/SystemAgent/` 只是包根。
- 包根不能接管 `Workspace/DocsAI/AgentWorkflow/` 等事实源。
- 包根只索引其它事实源位置。

这个要求与现在的目标冲突。新的目标是：

```text
Workspace/SystemAgent/ = SystemAgent 唯一事实源根
```

因此不能只移动文件，需要用 OpenSpec change 正式改掉 baseline，否则后续 AI 会继续被旧规格拉回“索引包”模型。

### 2.2 当前嵌套结构会制造新歧义

临时状态：

```text
Workspace/SystemAgent/
  SystemAgentWorkflow.md
  RolePrompts.md
  Protocols/
  SystemAgent/
    README.md
    manifest.yaml
    workflow-catalog.yaml
```

AI 看到这类路径时会产生两个问题：

- `Workspace/SystemAgent/INDEX.md` 和 `Workspace/SystemAgent/SystemAgent/README.md` 谁是入口？
- `Workspace/SystemAgent/SystemAgent/workflow-catalog.yaml` 是不是比 `Workspace/SystemAgent/SystemAgentWorkflow.md` 更权威？

结论：`Workspace/SystemAgent/SystemAgent/` 不应长期存在。它里面的内容应该拆到 `Catalog/`、`Gates/`、`Tools/`、根目录或 `Config/`，让路径表达职责，而不是再套一层同名目录。

### 2.3 skill / subagent 仍然容易变成正文副本

SystemAgent 收敛后，skill/subagent/hook 的原则应更硬：

```text
Workspace/SystemAgent/ = 正文事实源
.ai-config / .claude / .codex / .windsurf = 启动器、引用器、运行适配层
```

这意味着：

- `.ai-config/skills/systemagent/*/SKILL.md` 不写长流程，只写触发条件和 `Workspace/SystemAgent/...` 路径。
- `.claude/agents/systemagent-*.md` 不写角色正文，只要求启动后读取 `Workspace/SystemAgent/Roles/<role>.md`。
- `.codex/agents/systemagent-*.toml` 同理。
- hook 只输出极短提醒，不复制 checklist。

如果某个工具要求 `.ai-config` 是 skill 同步源，那么 SystemAgent skill 的 `.ai-config` 文件也只能是“短 wrapper 源”，不再是流程正文源。

## 3. 推荐最终目录结构

目标不是“人看起来漂亮”，而是 AI 在最少路径里稳定路由。推荐结构如下：

```text
Workspace/SystemAgent/
  README.md
  INDEX.md

  Workflows/
    INDEX.md
    NewFeature.md
    DebugFix.md
    WorkflowIteration.md
    ConfigMaintenance.md
    ResearchAdoption.md
    ValidationRelease.md

  Roles/
    INDEX.md
    Planner.md
    Implementer.md
    Debugger.md
    TestDesigner.md
    Reviewer.md
    Verifier.md
    ResearchAnalyst.md
    Retrospective.md
    Documentarian.md

  Protocols/
    AIFeatureDevelopmentProtocol.md
    AITaskCompletionContract.md
    CapabilityChangeProtocol.md
    FrameworkVsGameBoundary.md
    LongRunningPlanProtocol.md
    OpenSpecChangeProtocol.md

  Gates/
    ReviewGates.md
    VerdictVocabulary.md

  Policies/
    AIConfigBoundary.md
    GitPolicy.md
    ExternalResources.md
    DocumentationManagement.md

  Catalog/
    manifest.yaml
    workflow-catalog.yaml
    systemagent-catalog.yaml

  Config/
    review-mode.txt

  Tools/
    skill-test/

  Skills/
    README.md
    WrapperSkillPolicy.md

  BDDSceneFormat.md
```

### 3.1 根目录职责

根目录只放 AI 必须第一眼看到的入口：

- `README.md`：一句话定位、入口顺序、目录地图。
- `INDEX.md`：AI 路由总索引，告诉 AI 不同任务读哪个 workflow。
- `BDDSceneFormat.md`：固定保留在根目录。BDD 是横跨 OpenSpec、workflow、测试和验收的行为契约语言，不再迁入 `Protocols/` 或 `Workflows/`。

根目录不放长篇混合说明。当前这些文件应拆分：

- `SystemAgentWorkflow.md` → 拆成 `Workflows/*.md`。
- `RolePrompts.md` → 拆成 `Roles/*.md`。
- `ResearchAndAdoptionNotes.md` → 拆成 `Policies/ExternalResources.md`、`Policies/GitPolicy.md`、`Policies/DocumentationManagement.md`，删除未使用的工具资源策略段。
- `SystemAgentOverview.md` → 合并精简到 `README.md` 和 `INDEX.md`，避免三入口。

### 3.2 `Workspace/SystemAgent/SystemAgent/` 整理方案

当前嵌套目录建议完全拆掉：

| 当前路径 | 目标路径 | 理由 |
| --- | --- | --- |
| `Workspace/SystemAgent/SystemAgent/README.md` | 合并进 `Workspace/SystemAgent/README.md` | 只能有一个入口 README |
| `Workspace/SystemAgent/SystemAgent/manifest.yaml` | `Workspace/SystemAgent/Catalog/manifest.yaml` | manifest 是目录/catalog 数据，不是入口 |
| `Workspace/SystemAgent/SystemAgent/workflow-catalog.yaml` | `Workspace/SystemAgent/Catalog/workflow-catalog.yaml` | workflow 数据源归入 Catalog |
| `Workspace/SystemAgent/SystemAgent/catalog/systemagent-catalog.yaml` | `Workspace/SystemAgent/Catalog/systemagent-catalog.yaml` | skill catalog 归入 Catalog |
| `Workspace/SystemAgent/SystemAgent/review-gates.md` | `Workspace/SystemAgent/Gates/ReviewGates.md` | gate 正文归入 Gates |
| `Workspace/SystemAgent/SystemAgent/verdict-vocabulary.md` | `Workspace/SystemAgent/Gates/VerdictVocabulary.md` | verdict 词表归入 Gates |
| `Workspace/SystemAgent/SystemAgent/review-mode.txt` | `Workspace/SystemAgent/Config/review-mode.txt` | 运行配置归入 Config |
| `Workspace/SystemAgent/SystemAgent/tools/skill-test/` | `Workspace/SystemAgent/Tools/skill-test/` | 工具归入 Tools |
| `Workspace/SystemAgent/SystemAgent/.last-sync` | `Workspace/SystemAgent/Config/.last-sync` 或删除重建 | sync 状态不应放旧根 |

整理后不再保留 `Workspace/SystemAgent/SystemAgent/`。

## 4. Skill 目录策略

SystemAgent skill 统一放在 `.ai-config/skills/systemagent/`。这里仍然是 IDE skill 的唯一维护源；`Workspace/SystemAgent/` 保存 workflow/role/policy 正文，`.ai-config` 保存短 wrapper。

推荐：

```text
.ai-config/skills/systemagent/
  systemagent-new-feature/SKILL.md
  systemagent-debug-fix/SKILL.md
  systemagent-workflow-iteration/SKILL.md
  systemagent-config-maintenance/SKILL.md
  systemagent-research-adoption/SKILL.md
  systemagent-validation-release/SKILL.md
  systemagent-retrospective/SKILL.md
  systemagent-skill-test/SKILL.md
```

每个 wrapper skill 只包含：

```text
# 触发条件
什么时候使用这个 skill。

# 必读
Workspace/SystemAgent/Workflows/<Workflow>.md
Workspace/SystemAgent/Roles/<Role>.md

# 输出要求
当前 workflow 要求的最小输出字段。

# 禁止
不要复制 workflow 正文；不要复制角色 prompt；不要直接改同步副本。
```

旧入口处理：

| 旧入口 | 处理 |
| --- | --- |
| `.ai-config/skills/ai/ai-feature-development` | 保留为短入口，转向 `Workspace/SystemAgent/Workflows/NewFeature.md` |
| `.ai-config/skills/ai/ai-process-retrospective` | 保留为短入口或迁为 `systemagent-retrospective`，但正文在 `Workspace/SystemAgent/Workflows/WorkflowIteration.md` 与 `Roles/Retrospective.md` |
| `.ai-config/skills/core/skill-test` | 保留工具入口，指向 `Workspace/SystemAgent/Tools/skill-test/` 和 `Skills/WrapperSkillPolicy.md` |
| owner capability skills | 不迁入 SystemAgent；它们属于 GameOS/DataOS/能力系统 |

不建议让 `Workspace/SystemAgent/Skills/` 反向生成 `.ai-config`。保持现有同步纪律：SystemAgent skill 源在 `.ai-config/skills/systemagent/`，同步副本仍由 `Workspace/Tools/ai-config-sync/sync-ai-config.sh` 生成。

## 5. Subagent / 角色策略

角色提示词必须分文件，按 workflow 加载：

```text
Workspace/SystemAgent/Roles/Planner.md
Workspace/SystemAgent/Roles/Implementer.md
Workspace/SystemAgent/Roles/Debugger.md
Workspace/SystemAgent/Roles/TestDesigner.md
Workspace/SystemAgent/Roles/Reviewer.md
Workspace/SystemAgent/Roles/Verifier.md
Workspace/SystemAgent/Roles/ResearchAnalyst.md
Workspace/SystemAgent/Roles/Retrospective.md
Workspace/SystemAgent/Roles/Documentarian.md
```

`.claude/agents/systemagent-*.md` 和 `.codex/agents/systemagent-*.toml` 只保留运行适配：

- agent 名称。
- 何时调用。
- 允许工具。
- 启动后必须读取的 `Workspace/SystemAgent/Roles/<role>.md`。
- 输出必须遵守的 workflow/gate/verdict 路径。

推荐优先落地的 subagent：

| 角色 | 优先级 | 理由 |
| --- | --- | --- |
| Planner | P0 | 大任务前必须拆解和判断 OpenSpec |
| Debugger | P0 | Debug 要独立证据链，避免猜修 |
| TestDesigner | P0 | 标准答案必须前置 |
| Reviewer | P0 | gate review 需要独立视角 |
| Retrospective | P0 | 用户最关心“为什么 AI 没提前发现问题” |
| ResearchAnalyst | P1 | 只在任务需要外部资源研究时使用 |
| Verifier | P1 | 验证输出很大时再拆 |
| Implementer | P2 | 默认由主 agent 承担，避免上下文断裂 |
| Documentarian | P2 | MVP 可由主 agent 维护文档 |

## 6. 多工作流设计

`Workspace/SystemAgent/INDEX.md` 应该先让 AI 选 workflow，而不是先读一条大流程。

| 用户意图 | Workflow | 文件 | 必需角色 | 外部资源 |
| --- | --- | --- | --- | --- |
| 写新功能 / 重构 / 迁移 | New Feature | `Workflows/NewFeature.md` | Planner、TestDesigner、Reviewer、Retrospective | 默认不预读；AI 判断需要时可按范围查看 |
| 出错、测试失败、bug | Debug Fix | `Workflows/DebugFix.md` | Debugger、Verifier、Retrospective | 默认不预读；AI 判断需要时可按范围查看 |
| 用户觉得 AI 做得不好，要求分析对话并改流程 | Workflow Iteration | `Workflows/WorkflowIteration.md` | Retrospective、Reviewer、Documentarian | 默认不预读；AI 判断需要时可按范围查看 |
| 修改 skill/rule/hook/subagent | Config Maintenance | `Workflows/ConfigMaintenance.md` | Planner、Reviewer、Verifier | 默认不预读；通常不需要 |
| 用户要求研究外部资料 | Research Adoption | `Workflows/ResearchAdoption.md` | ResearchAnalyst、Reviewer | 按用户指定或 AI 选择的资源查看 |
| 大改后的完整验证 | Validation Release | `Workflows/ValidationRelease.md` | Verifier、Reviewer、Retrospective | 默认不预读；验证失败需要参考时查看 |

### 6.1 New Feature

```text
Intake
-> Route
-> OpenSpec SDD
-> BDD / TDD 标准答案
-> Implement
-> Validate
-> Artifact Analyze
-> Review
-> Retrospective
-> Completion / commit suggestion
```

要点：

- 大任务必须进 OpenSpec。
- 行为变化必须先有标准答案。
- 代码完成不等于完成，必须有验证和 retrospective。
- 不预加载外部资源；如果 AI 判断历史动机、参考架构或完整游戏机制会影响方案，可以按最小范围查看并记录。

### 6.2 Debug Fix

Debug 工作流不能直接套 New Feature。推荐固定为：

```text
Problem Intake
-> Reproduce
-> Evidence Map
-> Hypothesis Tree
-> Minimal Fix
-> Regression Validation
-> Retrospective
```

外部资源不是 Debug 默认步骤。AI 只有在以下情况才应该查看：

- 当前 bug 涉及框架架构或生命周期设计，内部文档不足。
- 同一类 bug 重复出现，需要看旧框架为什么这样设计。
- 用户明确说“参考旧框架 / 参考引擎 / 参考某游戏”。

查看前，AI 必须在计划或回复中写清：

```text
externalResources:
  enabled:
    - <resource-id>
  scope:
    - <path-or-keyword-scope>
  reason: <why this resource is relevant>
  expires: current-task
```

外部资源只在当前任务有效，不变成默认上下文。

### 6.3 Workflow Iteration

这是核心闭环：用户发现 AI 做得不好时，不只是修本次结果，而是找流程缺口。

触发：

- 用户明确说“分析这次对话 / 方向不对 / 完善 systemagent 工作流”。
- retrospective 输出 `CONCERNS` 或 `REJECT`，且问题属于流程缺陷。

输出必须回答：

```text
1. 用户指出的问题是什么。
2. AI 当时为什么没提前发现。
3. 缺口属于 workflow / role / skill / hook / test / docs / external resource policy 哪一类。
4. 应该改 `Workspace/SystemAgent/` 的哪个文件。
5. 是否需要 OpenSpec。
6. 最小修改方案。
7. 需要用户确认的问题。
```

默认策略：

- AI 可以记录 workflow gap candidate。
- AI 可以建议改 SystemAgent。
- AI 不应自动大改长期 workflow，除非用户明确要求或当前 OpenSpec change 已授权。

## 7. 外部资源策略

外部资源是参考资料，不是 SystemAgent 默认上下文，也不是事实源。原则：

```text
默认不预加载、不全量扫描。
AI 可以根据任务自行判断是否查看。
只在当前任务有效。
查看前必须记录 resource-id、scope、reason。
使用后必须汇报读了什么、为什么读、得出什么结论。
不得直接复制外部项目代码或资产到 SlimeAI。
```

### 7.1 资源清单

| resource-id | 路径 | 用途 | 默认行为 | 风险 |
| --- | --- | --- | --- | --- |
| `engine-framework` | `Resources/Engine/Docs/FrameworkAnalysis/Reports/` | 引擎 / 框架设计参考，辅助架构判断 | 不预读；架构判断需要时查看 | 容易过度设计 |
| `agent-reference` | `Resources/Agent/` | agent、BDD、workflow、prompt、subagent 参考 | 不预读；SystemAgent/流程设计需要时查看 | 容易引入不适合本项目的流程 |
| `legacy-godot-csharp-ecs` | `Resources/Else/brotato-my` | 旧 Godot C# ECS 框架，理解历史动机和未迁移逻辑 | 不预读；迁移或历史动机不清时查看 | 已弃用，不能当新架构事实源 |
| `game-reference` | `Resources/Games` | 破解游戏源码和已分析文档，用来参考完整游戏机制 | 不预读；玩法/系统设计需要完整游戏参照时查看 | 只能做设计参考，不复制专有代码/资产 |

### 7.2 使用方式

用户可以显式指定：

```text
使用外部资源: engine-framework
使用外部资源: agent-reference
使用外部资源: legacy-godot-csharp-ecs
使用外部资源: game-reference:<game-or-topic>
```

用户没有指定时，AI 也可以自行判断是否查看，但必须先写明：

```text
使用外部资源: legacy-godot-csharp-ecs
原因: 当前问题涉及旧 ECS 到 GameOS 的迁移意图，内部 DocsAI 没说明历史动机。
范围: 只搜索 Resources/Else/brotato-my 中与 <keyword> 相关的文件。
```

记录格式：

```text
externalResources:
  enabled:
    - legacy-godot-csharp-ecs
  scope:
    - Resources/Else/brotato-my/<specific-area>
  reason: <why>
  expires: current-task
```

### 7.3 使用约束

- **不预加载**：任何 workflow 都不能默认全量读取 `Resources/*`。
- **AI 自主判断**：AI 可以按任务需要查看外部资源，但必须先说明 `resource-id / scope / reason`。
- **不扩大范围**：选择一个 resource，不代表全部 `Resources/` 都可读。
- **不长期记忆为事实源**：外部资源结论必须标成参考、推断或候选，不覆盖 `Workspace/SystemAgent/`、`SlimeAI/DocsAI/`、`openspec/specs/`。
- **不复制专有内容**：`Resources/Games` 只能用于机制理解和设计启发，不复制源码、资产或原文大段内容。
- **不阻塞任务**：外部资源不可用时，AI 应继续基于当前事实源工作，或说明缺少参考会带来的风险。

### 7.4 输出格式

凡使用外部资源，最终汇报必须有一段：

```text
外部资源使用:
- enabled: <resource-id>
- filesRead: <paths>
- reason: <why>
- findings: <Evidence / Inference / Unknown>
- adopted: <what changed in proposal or implementation>
- copiedCodeOrAssets: none
```

## 8. Context7 工具边界

SystemAgent 不维护 Context7 策略，也不把 Context7 当作本地资源层。

当前唯一需要说明的是：

- Context7 是 IDE / CLI 提供的工具能力，用于查询第三方库/框架文档。
- 它不属于 `Workspace/SystemAgent/` 内容管理范围。
- SystemAgent workflow 不应因为 Context7 不可用而失败。
- 如果任务需要库文档，AI 按工具规则使用 Context7；这不等于开启本地外部资源。

因此不创建 Context7 policy，也不在默认 workflow 中出现 Context7 步骤。

## 9. OpenSpec MVP 迭代路线

建议 change 名：

```text
consolidate-systemagent-unified-source
```

### M0：当前分析

目标：更新分析文档，固定用户最新决策。

产物：

- `Workspace/DocsAI/Reviews/2026-05-18-systemagent-mvp-consolidation-analysis.md`

### M1：统一事实源目录

任务：

1. 修改 OpenSpec baseline：`Workspace/SystemAgent/` 从“索引包”改为“唯一 SystemAgent 事实源”。
2. 确认 `Workspace/DocsAI/AgentWorkflow/` 不再作为事实源，不保留兼容正文。
3. 整理 `Workspace/SystemAgent/SystemAgent/`，把内容拆到 `Catalog/`、`Gates/`、`Config/`、`Tools/`。
4. 合并 `SystemAgentOverview.md` 和旧 `README.md`，生成唯一 `Workspace/SystemAgent/README.md`。
5. 将 `SystemAgentWorkflow.md` 拆成 `Workflows/*.md`。
6. 将 `RolePrompts.md` 拆成 `Roles/*.md`。
7. 将 `ResearchAndAdoptionNotes.md` 拆成 `Policies/*.md`，删除未使用的工具资源策略，只保留外部资源、git、文档管理等真实会用的策略。
8. 更新 `Catalog/manifest.yaml`、`Catalog/workflow-catalog.yaml`、`Catalog/systemagent-catalog.yaml`。
9. 更新所有引用路径。

验收：

- `Workspace/SystemAgent/SystemAgent/` 不存在。
- `Workspace/SystemAgent/README.md` 是唯一入口。
- `Workspace/SystemAgent/INDEX.md` 能把用户意图路由到具体 workflow。
- 全仓搜索旧路径 `Workspace/DocsAI/AgentWorkflow` 只剩历史记录或已明确废弃的说明。
- 不存在未使用的工具资源 policy 或默认工具资源 workflow。

### M2：SystemAgent skill 统一

任务：

1. 新建 `.ai-config/skills/systemagent/` 分类。
2. 新建短 wrapper skills。
3. 旧 `ai-feature-development`、`ai-process-retrospective`、`skill-test` 改成短入口或短指针。
4. 同步 `.claude/.codex/.windsurf` skill 副本。
5. 运行 skill-test。

验收：

- SystemAgent skill 不复制 workflow 正文。
- 所有 SystemAgent skill 都指向 `Workspace/SystemAgent/Workflows/` 或 `Workspace/SystemAgent/Roles/`。
- 副本由 sync 生成，不手改。

### M3：Subagent / hook 重接线

任务：

1. `.claude/agents/systemagent-*.md` 改成短启动器。
2. `.codex/agents/systemagent-*.toml` 改成短启动器。
3. hook 输出路径改为 `Workspace/SystemAgent/README.md` / `INDEX.md`。
4. review gate 路径改为 `Workspace/SystemAgent/Gates/ReviewGates.md`。
5. verdict 路径改为 `Workspace/SystemAgent/Gates/VerdictVocabulary.md`。

验收：

- subagent 配置不含长角色正文。
- hook 不输出长 checklist。
- review/retrospective 只引用 gate ID 和 verdict 词表。

### M4：外部资源使用协议

任务：

1. 新建 `Workspace/SystemAgent/Policies/ExternalResources.md`。
2. 在每个 workflow 中声明 `externalResources: not preloaded, AI-decided by task need`。
3. 在 `ResearchAdoption` workflow 中写清 AI 自主判断、范围、过期规则和汇报格式。
4. 在 retrospective 中检查是否全量扫描、范围扩大或把外部资源当事实源。

验收：

- AI 默认不预加载 `Resources/*`。
- AI 查看外部资源前记录 resource-id、scope、reason、expires。
- final report 列出外部资源使用情况。

## 10. 文档管理原则

### 10.1 单事实源规则

| 内容类型 | 唯一事实源 | 其它位置 |
| --- | --- | --- |
| SystemAgent 总入口 | `Workspace/SystemAgent/README.md` | 只引用 |
| 路由索引 | `Workspace/SystemAgent/INDEX.md` | 只引用 |
| 工作流正文 | `Workspace/SystemAgent/Workflows/*.md` | skill/subagent 只引用 |
| 角色 prompt | `Workspace/SystemAgent/Roles/*.md` | subagent 只启动并引用 |
| review gate | `Workspace/SystemAgent/Gates/ReviewGates.md` | reviewer 只引用 gate ID |
| verdict 词表 | `Workspace/SystemAgent/Gates/VerdictVocabulary.md` | retrospective 只引用 |
| SystemAgent catalog | `Workspace/SystemAgent/Catalog/*.yaml` | 工具读取 |
| SystemAgent 工具 | `Workspace/SystemAgent/Tools/` | hook/skill 引用 |
| SystemAgent 策略 | `Workspace/SystemAgent/Policies/*.md` | workflow 引用 |
| IDE skill wrapper | `.ai-config/skills/systemagent/*/SKILL.md` | 只保存短入口 |
| hook/subagent 运行配置 | `.claude/`、`.codex/` | 只保存运行适配 |
| 稳定需求约束 | `openspec/specs/systemagent-*` | OpenSpec baseline |
| 临时分析 | `Workspace/DocsAI/Reviews/*.md` | 不作为长期入口 |

### 10.2 文件长度控制

建议：

- `README.md`：≤ 120 行。
- `INDEX.md`：≤ 160 行。
- 单个 workflow：≤ 220 行。
- 单个 role：≤ 140 行。
- 单个 wrapper skill：≤ 80 行。
- 单个 subagent 配置：≤ 40 行。

超过就拆分；不要把新大文档变成新的“巨型 prompt”。

### 10.3 禁止重复正文

禁止：

- 在 skill 里复制 workflow 全文。
- 在 subagent 里复制 role prompt 全文。
- 在 hook 输出里复制 checklist 全文。
- 在 `README.md` 里复制所有 workflow 细节。
- 在 `INDEX.md` 里复制每个 workflow 的全文。

允许：

- 一句话摘要。
- 路径引用。
- 触发条件。
- 输出格式的最小约束。

## 11. 还需要确认的问题

以下不是阻塞，但建议在 OpenSpec change 开始前确认：

1. **最终目录命名大小写**：用 `Workflows/`、`Roles/`、`Policies/` 这种 PascalCase，还是 `workflows/`、`roles/`、`policies/` 小写？我倾向 PascalCase，因为当前文件多为 Markdown 标题式命名，对 AI 更醒目。
2. **外部资源记录粒度**：AI 自主查看外部资源前，记录到对话/计划即可，还是需要后续落到 `ExternalResources.md` 的固定 YAML 模板？

## 12. 推荐下一步

下一步建议直接创建 OpenSpec change：

```text
consolidate-systemagent-unified-source
```

第一批只做 SystemAgent 目录、文档、skill/subagent 引用和策略收敛，不碰 GameOS 业务代码：

- `Workspace/SystemAgent/` 成为唯一事实源。
- 删除旧入口保留思路。
- 整理 `Workspace/SystemAgent/SystemAgent/`。
- 拆 `Workflows/`、`Roles/`、`Gates/`、`Policies/`、`Catalog/`、`Config/`、`Tools/`。
- 删除未使用的工具资源策略。
- 新增外部资源使用协议。
- 建 `.ai-config/skills/systemagent/` 短 wrapper。
- 让 `.claude/.codex` subagent 只引用 `Workspace/SystemAgent/Roles/`。

完成后，AI 的入口应该变成：

```text
Workspace/SystemAgent/README.md
-> Workspace/SystemAgent/INDEX.md
-> Workspace/SystemAgent/Workflows/<Workflow>.md
-> Workspace/SystemAgent/Roles/<Role>.md
-> Workspace/SystemAgent/Gates / Policies / Catalog 按需加载
```

这才是“只要 AI 好用”的结构：入口少、路径稳定、正文唯一、外部资源不会污染默认上下文。
