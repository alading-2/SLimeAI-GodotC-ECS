# Workflow / Skill / Role 分层模型

> 日期：2026-05-24  
> 输入：`02-Workflow与Skill触发优化方案.md`、`07-DesignDiscovery与DesignCritic方案.md`、`08-SDD独立化与文档迁移方案.md`  
> 结论：Workflow 是完整流程编排；Skill 是可独立运行、也可被 Workflow 调用的能力单元；Role 是执行视角和评审职责，不等同于 Skill

---

## 1. 为什么需要重新定义

前一版方案强调“不要新增独立 skill”，这个判断需要修正。

真正的问题不是“独立 skill 太多”，而是过去容易把三类东西混在一起：

- 把用户任务入口当成 skill。
- 把完整流程当成 skill。
- 把某个角色视角也当成 skill。
- 把可复用能力埋在 workflow 里，导致不能单独运行。

如果后续 SystemAgent workflow 要重构，就必须先定义：

```text
Workflow 编排完整任务流程。
Skill 提供可复用能力。
Role 提供执行视角和责任边界。
Artifact 保存任务事实。
Gate 判断阶段是否可通过。
Subagent 是可选执行基座，不是新的顶层职责分类。
```

这样 `DesignDiscovery`、`SDD` 可以既是独立 skill，又被 `NewFeature`、`WorkflowIteration` 等 workflow 调用。

---

## 2. 核心定义

### 2.1 Workflow

Workflow 是一个端到端任务流程。

它回答：

- 这个任务从哪里开始。
- 需要经历哪些阶段。
- 每个阶段调用哪些 skill。
- 每个阶段使用哪些 role。
- 读写哪些 artifact。
- 哪些 gate 必须通过。
- 什么时候暂停、恢复、完成或回滚。

Workflow 不应该把所有细节写成一篇大文档。

Workflow 应该像编排图：

```text
NewFeature Workflow
├── route
├── call skill: DesignDiscovery
├── call skill: SDD select/create
├── role: Planner
├── role: DesignCritic
├── write artifact: SDD/design, tasks, progress, bdd
├── call owner capability skill
├── gate: validation
└── retrospective
```

### 2.2 Skill

Skill 是一个可复用能力单元。

它回答：

- 什么时候可以单独运行。
- 被 workflow 调用时接收什么输入。
- 输出什么结果或 artifact。
- 需要哪些最小上下文。
- 禁止做什么。

Skill 可以有两种入口：

1. **Standalone**：用户直接要求“做设计发现”“创建/检查 SDD”。
2. **Composed**：某个 workflow 在阶段内调用它。

因此，`DesignDiscovery` 和 `SDD` 都应该是 skill，而不是只能藏在 workflow phase 里。

### 2.3 Role

Role 是执行视角和责任边界。

它回答：

- 这个阶段应该用什么角度判断问题。
- 应重点检查什么。
- 不应该越权做什么。

Role 不一定能单独运行。

例如：

- `Planner`：拆任务、排序、识别依赖。
- `DesignCritic`：找设计缺陷、遗漏、风险、替代方案。
- `Reviewer`：检查证据、质量、实现是否符合计划。
- `Retrospective`：总结流程缺口和后续改进。

`DesignCritic` 更适合作为 role，而不是 skill。

原因：它本身不拥有完整流程，也不直接维护 artifact；它通常嵌入 `DesignDiscovery` skill 或 workflow 的设计阶段。

如果需要独立批判视角，可以把 `DesignCritic` 交给 subagent 执行，但这只是执行方式，不改变它作为 role 的性质。

### 2.4 Artifact

Artifact 是任务事实载体。

典型 artifact：

- SDD `README.md`
- SDD `design/`
- SDD `tasks.md`
- SDD `progress.md`
- SDD `bdd.md`
- 验证日志
- Review report

Artifact 不等于 workflow，也不等于 skill。

SDD 是一组 artifact 的制度化容器。

### 2.5 Gate

Gate 是阶段边界的检查点。

它回答：

- 当前阶段能否进入下一阶段。
- 缺少什么证据。
- 是否需要用户确认。

Gate 不应该生成完整方案。

如果 gate 发现大量设计问题，说明前面应该调用 `DesignDiscovery` 或 `DesignCritic`，而不是让 gate 变成新的 workflow。

### 2.6 Subagent

Subagent 是独立上下文执行者。

它回答：

- 是否需要把某个只读搜索、独立评审或验证设计交给独立上下文。
- 子代理允许读取哪些范围。
- 子代理是否允许写文件。
- 子代理输出如何回到主对话。
- 主对话如何合并和裁决结论。

Subagent 不等于 workflow、skill 或 role。

一个 role 可以由主对话执行，也可以由 subagent 执行。

一个 skill 可以直接执行，也可以在内部调用 subagent 收集证据。

当前建议：subagent 默认只读；写操作必须由主对话统一执行，除非后续有稳定的 SDD 工作包、worktree 隔离和清理协议。

---

## 3. Skill 分类

### 3.1 Workflow Entry Skill

用途：把用户请求路由进某个 workflow。

示例：

- `systemagent-new-feature`
- `systemagent-debug-fix`
- `systemagent-workflow-iteration`
- `systemagent-config-maintenance`

这些 skill 应该很短。

它们不承载完整能力，只负责：

- 判断是否匹配。
- 指向 workflow。
- 列出最少必读文件。
- 提醒关键边界。

### 3.2 Capability Skill

用途：提供一个可单独运行、可被多个 workflow 调用的能力。

示例：

- `design-discovery`
- `sdd-management`
- `validation-release`
- `git-worktree-management`
- `test-driven-development`
- `bdd-scenario-authoring`

不是所有 discipline 都必须马上做成 skill。

但只要满足以下条件，就适合独立成 skill：

1. 用户可能单独请求它。
2. 至少两个 workflow 会复用它。
3. 它有清晰输入和输出。
4. 它能产生或更新稳定 artifact。
5. 它不是单纯 checklist。

按这个标准，`DesignDiscovery` 和 `SDD` 都适合独立成 skill。

### 3.3 Owner Capability Skill

用途：进入具体业务系统或框架模块。

示例：

- `ability-system`
- `damage-system`
- `movement-system`
- `ui-bind`
- `data-authoring`

这些 skill 通常由 workflow 在实现阶段调用。

### 3.4 Role Skill 不推荐

不建议把每个 role 都做成 skill。

原因：

- Role 是视角，不一定有独立输入输出。
- Role 过多会污染 skill 路由。
- Workflow 或 capability skill 可以声明使用哪些 role。

例外：如果某个 role 发展成可单独运行、有明确 artifact 输出的能力，才考虑升格为 capability skill。

---

## 4. Workflow 应包含什么

一个完整 workflow 至少应声明：

| 部分 | 作用 |
| --- | --- |
| Trigger | 什么时候进入这个 workflow |
| Task Size | small / medium / large 判断 |
| Entry Skill | 哪个 wrapper skill 会路由到这里 |
| Required Skills | 必须调用哪些 capability skill |
| Optional Skills | 条件调用哪些 capability skill |
| Roles | 每个阶段使用哪些角色视角 |
| Artifacts | 读写哪些 SDD、DocsAI、测试日志或报告 |
| Gates | 阶段检查点 |
| Validation | 如何证明完成 |
| Resume | 中断后从哪里恢复 |
| Exit | 完成、阻塞、取消的条件 |

推荐 workflow 结构：

```text
# WorkflowName

## Trigger
## Inputs
## Phases
### Phase 1: Route
- skill: workflow-entry
- role: none
- artifact: route summary

### Phase 2: Discover
- skill: design-discovery
- role: DesignCritic
- artifact: SDD/design, SDD/progress

### Phase 3: Plan
- skill: sdd-management
- role: Planner
- artifact: SDD/tasks

### Phase 4: Execute
- skill: owner capability skill
- role: Implementer
- artifact: code/docs/tests

### Phase 5: Validate
- skill: validation-release or test-system
- role: Reviewer
- artifact: validation logs

### Phase 6: Close
- skill: retrospective
- role: Retrospective
- artifact: SDD/progress, final report

## Gates
## Exit Conditions
```

---

## 5. DesignDiscovery 应如何定位

`DesignDiscovery` 应是 capability skill。

### 5.1 Standalone 模式

用户可以直接说：

```text
先做 Design Discovery
深度思考这个功能
分析方案缺陷
不要实现，先给设计确认包
```

输出：

```text
Design Discovery:
- Goal
- Context Read
- Assumptions
- Risks
- Options
- Recommendation
- Must Confirm
- Defaults
- SDD Updates
```

### 5.2 Workflow Composed 模式

Workflow 可以调用它：

```text
NewFeature -> DesignDiscovery -> SDD -> Plan -> Implement -> Validate
WorkflowIteration -> DesignDiscovery -> SDD -> Config/Docs updates -> Validate
ResearchAdoption -> DesignDiscovery -> Adoption plan -> SDD optional
```

### 5.3 内部使用的 role

`DesignDiscovery` 内部默认使用：

- `Planner`：判断范围和拆分方向。
- `DesignCritic`：找缺陷、遗漏、风险和替代方案。
- `Reviewer`：做轻量一致性自检。

这些 role 不需要都变成 skill。

---

## 6. SDD 应如何定位

`SDD` 有两层含义，必须分清。

### 6.1 SDD System

SDD System 是任务管理制度和 artifact 结构。

它包括：

- 根目录结构。
- 单个 SDD 文件结构。
- 状态流转。
- 校验规则。
- 归档规则。

### 6.2 SDD Management Skill

SDD Management Skill 是操作 SDD 的能力。

它可以单独运行：

```text
创建一个 SDD
列出 active SDD
检查这个 SDD 是否完整
恢复 SDD 当前上下文
把 SDD 标记为 blocked/done
```

也可以被 workflow 调用：

```text
NewFeature Phase 2 -> call SDD Management: create/select
ValidationRelease Phase 1 -> call SDD Management: show resume
Retrospective Phase 2 -> call SDD Management: update progress
```

### 6.3 SDD skill 的输出

输出不应只是口头说明，而应是 artifact 操作计划或实际 artifact 更新：

- `README.md`
- `sdd.json`
- `design/INDEX.md`
- `tasks.md`
- `progress.md`
- `bdd.md`
- `notes.md`

---

## 7. 推荐的新分层

```text
User Request
  ↓
Global Rules
  ↓
Workflow Entry Skill
  ↓
Workflow
  ├── calls Capability Skill: DesignDiscovery
  │       ├── uses Role: DesignCritic
  │       └── optional read-only Subagent: ResearchSubagent
  ├── calls Capability Skill: SDD Management
  │       └── writes Artifact: SDD/*
  ├── calls Owner Capability Skill
  │       └── modifies code/docs/data
  ├── calls Validation Skill
  │       └── writes logs/reports
  └── calls Retrospective Skill
          └── updates progress/follow-up
```

关键变化：

- Workflow 不是 skill 的替代品。
- Skill 不是 workflow 的缩写。
- Role 不是 skill 的同义词。
- SDD 不是 workflow，也不是单个文档，而是 artifact system。
- DesignDiscovery 是 skill，DesignCritic 是 role。
- Subagent 是可选执行基座，不是新的事实源层级。

---

## 8. 对前一版方案的修正

需要修正的旧判断：

```text
旧：Design Discovery 不应默认做成独立触发 skill。
新：DesignDiscovery 应作为 capability skill，可独立运行，也可由 workflow 调用。
```

```text
旧：SDD 是任务层，不是 skill。
新：SDD System 是 artifact 制度；SDD Management 应作为 capability skill 管理创建、读取、校验、状态和恢复。
```

```text
旧：不新增 Architect/TDD/BDD/worktree 等一堆独立 skill。
新：不新增没有独立输入输出的 checklist skill；但满足复用、artifact、独立运行条件的能力可以沉淀为 capability skill。
```

---

## 9. 最终建议

SystemAgent 重构后应形成三类事实源：

```text
Workflows/  = 端到端流程编排
Skills/     = 可复用能力说明和运行协议
Roles/      = 执行视角和责任边界
```

其中：

- `NewFeature` 是 workflow。
- `DesignDiscovery` 是 capability skill。
- `SDD Management` 是 capability skill。
- `DesignCritic` 是 role。
- `SDD/active/SDD-xxxx/` 是 artifact 容器。

这比“所有东西都是 workflow”或“所有东西都是 skill”更清楚，也更适合后续 AI 路由和验证。
