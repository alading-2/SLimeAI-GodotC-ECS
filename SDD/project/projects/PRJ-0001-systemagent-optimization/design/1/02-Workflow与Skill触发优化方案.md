# Workflow 与 Skill 触发优化方案

> 日期：2026-05-24  
> 目标：让 SystemAgent 更像可选择的工作流路由器，而不是每次都启动完整 ritual

---

## 1. 当前问题

SystemAgent 现在的问题不是没有触发入口，而是入口链太长、语义太绕。

实际链路大致是：

```text
用户请求
-> 全局 rules
-> skill 描述匹配
-> wrapper skill
-> Workspace/SystemAgent/README.md
-> INDEX.md
-> workflow
-> role
-> gate/policy/protocol
-> OpenSpec 或具体实现
```

这个链路的优点是边界清楚。

缺点是：

- 每次都像在读制度，不像在干活。
- wrapper skill、rule、workflow、protocol 的边界对 AI 不够直观。
- selected workflow 和 must-read 状态没有稳定结构化输出。
- 新增 role/gate/protocol 容易继续增加上下文开销。

---

## 2. 触发机制重新分层

建议把 SystemAgent 触发分成 5 层，详细概念边界见 `09-WorkflowSkillRole分层模型.md`。

### 2.1 全局规则层

来源：`.ai-config/rules/rules.md` 及同步副本。

职责：硬边界。

只写：

- Git 安全。
- AI 配置事实源边界。
- SDD/OpenSpec 默认策略。
- 修改前读文件、修改后验证。
- 不覆盖用户改动。

不写：

- 完整 workflow。
- 完整 role rubric。
- SDD 模板正文。

### 2.2 Workflow Entry Skill 层

来源：`.ai-config/skills/systemagent/*/SKILL.md`。

职责：短入口，把用户请求路由进 workflow。

每个 skill 只回答：

- 什么时候触发。
- 进入哪个 workflow。
- 最少读哪些 SystemAgent 文件。
- 是否需要 SDD。
- 禁止事项。

不复制 workflow 正文。

### 2.3 Workflow 编排层

来源：`Workspace/SystemAgent/Workflows/*.md`。

职责：编排完整流程。

Workflow 不是单个 skill 的同义词。它应声明：

- 当前任务从哪里开始。
- 分几个 phase。
- 每个 phase 调用哪些 capability skill。
- 每个 phase 使用哪些 role。
- 读写哪些 artifact。
- 需要哪些 gate。
- 是否允许调用只读 subagent。
- 如何暂停、恢复和完成。

每个 workflow 应以“直接执行顺序”开头，而不是先介绍大量概念。

推荐开头格式：

```text
When triggered:
1. Classify task size: small / medium / large.
2. Decide workflow mode: lean / full.
3. Call required capability skills.
4. Report selected workflow and must-read status.
5. Execute phase list.
6. Update SDD or final report.
```

### 2.4 Capability Skill 层

来源：`.ai-config/skills/*` 或未来 `Workspace/SystemAgent/Skills/`。

职责：提供可复用能力，可独立运行，也可被 workflow 调用。

典型 capability skill：

- `design-discovery`
- `sdd-management`
- `validation-release`
- `git-worktree-management`
- owner capability skill，例如 `damage-system`、`ability-system`

能力是否适合独立成 skill，判断标准是：

- 用户可能单独请求它。
- 多个 workflow 会复用它。
- 有明确输入和输出。
- 能产生、读取或更新稳定 artifact。

按这个标准，`DesignDiscovery` 和 `SDD Management` 应作为 capability skill，而不是只写成 workflow 内部小节。

### 2.5 Subagent 执行基座

来源：`.claude/agents/`、`.codex/agents/` 或工具层只读搜索子代理。

职责：在必要时提供独立上下文。

Subagent 不是新的 workflow 层，也不是 skill 或 role 的同义词。

当前建议：

- 可用于只读资料搜索。
- 可用于独立评审。
- 可用于测试设计检查。
- 不默认用于并行写代码。
- 写操作由主对话统一执行。

### 2.6 SDD Artifact 层

来源：具体 SDD。

职责：任务事实和恢复上下文。

大中型任务开始后，AI 不应每次重新读大量 workflow/protocol，而应优先读取：

```text
README.md
progress.md Latest Resume
tasks.md 当前任务
bdd.md 相关场景
```

---

## 3. 任务规模分级

### 3.1 Small

特点：

- 单文件或少量文档修正。
- 不跨模块。
- 无长期恢复需求。

执行：

- 不创建 SDD。
- 不跑完整 workflow。
- 遵守基本 git status、读文件、验证、总结。

### 3.2 Medium

特点：

- 2-5 个文件。
- 有明确设计，但影响面有限。
- 可能需要一次恢复。

执行：

- SDD 可选。
- 如果用户要求“深度分析、生成文档、后续继续”，建议创建 SDD。
- workflow 走 lean 模式。

### 3.3 Large

特点：

- 跨模块、跨 git 边界、影响 SystemAgent 或 GameOS contract。
- 需要多轮执行。
- 需要设计、任务、验证、复盘。

执行：

- 必须创建 SDD。
- workflow 读取 SDD 作为任务事实源。
- 需要明确 progress resume。

---

## 4. selected workflow 输出标准化

每次进入 SystemAgent workflow，都应输出一个极短的结构化声明。

推荐格式：

```text
SystemAgent route:
- workflow: NewFeature | DebugFix | WorkflowIteration | ConfigMaintenance | ResearchAdoption | ValidationRelease
- task_size: small | medium | large
- sdd: none | optional | required | SDD-0001
- must_read: read=<n>, pending=<n>
- mode: lean | full
- subagent: none | read-only | review-only
```

这个输出比长篇“我会先...”更有价值，因为它给后续 ReviewGate 提供了稳定输入。

---

## 5. Skill 精简与拆分方向

现有 8 个 systemagent wrapper skill 可以保留，但要区分 workflow entry skill 和 capability skill。

### 5.1 保留 Workflow Entry Skill

建议保留这些 workflow 入口：

| Skill | 用途 |
| --- | --- |
| `systemagent-new-feature` | 新功能、重构、迁移、OpenSpec/SDD 实施 |
| `systemagent-debug-fix` | bug、测试失败、验证失败 |
| `systemagent-workflow-iteration` | 优化 SystemAgent 自身流程 |
| `systemagent-config-maintenance` | skill/rule/hook/subagent/sync 维护 |
| `systemagent-research-adoption` | 外部参考研究 |
| `systemagent-validation-release` | 发布前完整验证 |
| `systemagent-retrospective` | 完成前复盘 |
| `systemagent-skill-test` | wrapper skill lint |

### 5.2 新增 Capability Skill

`DesignDiscovery` 和 `SDD Management` 应作为 capability skill 设计。

原因：

- 用户可能单独请求“深度思考/设计发现/创建 SDD/恢复 SDD”。
- 多个 workflow 会调用它们。
- 它们有明确输入输出。
- 它们会读写稳定 artifact。

Capability skill 不等于 workflow entry skill。

例如：

| Skill | Standalone 用途 | Workflow 内用途 |
| --- | --- | --- |
| `design-discovery` | 用户要求先深度分析方案 | NewFeature/WorkflowIteration 的 Discover phase |
| `sdd-management` | 创建、检查、恢复、迁移 SDD | NewFeature/Validation/Retrospective 的 artifact phase |
| `git-worktree-management` | 创建/检查/清理 worktree | 大型实施 workflow 的隔离 phase |

### 5.3 不建议新增 Checklist Skill

不是所有 discipline 都应该做成 skill。

不建议把这些简单 checklist 独立成 skill：

- “记得跑测试”
- “记得 git status”
- “记得不要改同步副本”
- “记得写总结”

这些应留在 rule、workflow checklist 或 gate 中。

---

## 6. Workflow 文档重写原则

当前 workflow 文档偏像容器，核心内容散到 roles/protocols/gates。

建议重写时遵守：

### 6.1 第一屏必须说明怎么干活

每个 workflow 文件前 30 行应包含：

- 何时触发。
- 任务规模判断。
- 是否需要 SDD。
- 是否需要 Design Discovery。
- 直接执行步骤。
- 完成条件。

### 6.2 NewFeature 必须包含 Design Discovery phase

新功能、重构和行为改动不应直接进入任务拆分。

推荐 phase：

```text
1. Route and classify task size.
2. Read minimal context.
3. Call skill: design-discovery.
4. Present risks, options, recommendation, and user decisions.
5. Call skill: sdd-management to select/create SDD when needed.
6. Record accepted assumptions into SDD progress.md.
7. Write or update SDD design/.
8. Split tasks.
```

`design-discovery` 可以单独运行，也可以作为 workflow phase 被调用。它不是逐问逐答，而是 AI 深度分析后一次性给出“确认包”。

### 6.3 role 是视角，不是流程阶段

角色可以作为检查视角，但不要让 AI 每次都模拟完整角色链。

例如：

- Planner 视角：任务拆分是否合理。
- DesignCritic 视角：方案是否有缺陷、遗漏和用户决策点。
- Reviewer 视角：证据是否足够。
- Retrospective 视角：流程缺口是否要回写。

但输出时不必长篇声明“现在我作为 X”。

### 6.4 gate 是检查点，不是阅读负担

Gate 应尽量变成 checklist。

如果 gate 需要大量上下文才能执行，说明需要 SDD 提供稳定输入，而不是让 AI 重读全部历史。

---

## 7. SDD 接入后的新工作流示例

### 7.1 用户请求大型 SystemAgent 优化

```text
1. route: workflow_iteration, task_size=large, sdd=required
2. 读取当前 SDD README/progress/tasks
3. 执行 Design Discovery，输出确认包和推荐方案
4. 若没有 SDD，创建或请求创建正式 SDD
5. 写入 SDD design/tasks/bdd/progress
6. 用户确认后实施
7. 每完成任务组更新 progress
8. 验证后 done
```

### 7.2 用户请求单文件 hook bug 修复

```text
1. route: debug_fix, task_size=small, sdd=none
2. 读取 hook 文件和配置
3. 写最小 smoke
4. 修复
5. 跑 smoke
6. 汇报 git status 和验证
```

### 7.3 用户请求参考外部项目

```text
1. route: research_adoption, task_size=medium, sdd=optional
2. 限定 Resources 范围
3. 输出 Evidence/Inference/Unknown
4. 如果会转成长期改动，再创建 SDD
```

---

## 8. 最终建议

SystemAgent workflow 的优化方向不是“补更多流程”，而是“把入口变短，把任务上下文交给 SDD”。

推荐优先改这些事：

1. 固化 selected workflow 输出格式。
2. workflow 开头改成直接执行步骤。
3. NewFeature 调用 `design-discovery` 和 `sdd-management` capability skill。
4. 中大型任务默认读取/维护 SDD，而不是默认进入 OpenSpec。
5. subagent 只作为可选执行基座，不做默认并行实现。
6. 避免新增只有 checklist 作用的 skill。

这样可以保留 SystemAgent 的治理能力，同时降低每次任务启动的上下文成本。
