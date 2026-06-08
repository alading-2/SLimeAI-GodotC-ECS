# Subagent 使用场景与采纳策略

> 日期：2026-05-24  
> 输入：当前 `.claude/agents/`、`.codex/agents/`、`.windsurf/` 配置，`Resources/Skills/tackle`，`09-WorkflowSkillRole分层模型.md`  
> 结论：SystemAgent 暂不需要引入 tackle 式完整并行执行团队；更适合先把 subagent 定位为可选的只读研究、独立评审和验证设计辅助能力

---

## 1. 当前事实

### 1.1 AICLI 有 subagent

当前工作区已经维护 AICLI 侧 subagent 配置。

Claude：

```text
.claude/agents/systemagent-planner.md
.claude/agents/systemagent-test-designer.md
.claude/agents/systemagent-reviewer.md
.claude/agents/systemagent-retrospective.md
```

Codex：

```text
.codex/agents/systemagent-planner.toml
.codex/agents/systemagent-test-designer.toml
.codex/agents/systemagent-reviewer.toml
.codex/agents/systemagent-retrospective.toml
```

`Workspace/SystemAgent/Catalog/manifest.yaml` 也已经把这些登记为 subagents。

这些 subagent 目前更像“角色短启动器”：

- Planner
- TestDesigner
- Reviewer
- Retrospective

它们不是并行执行团队，也不是自动拆分调度器。

### 1.2 AIIDE 当前没有同类配置事实源

当前 `.windsurf/` 下看到的是：

```text
.windsurf/rules/
.windsurf/skills/
```

没有看到与 `.claude/agents/`、`.codex/agents/` 对等的 `.windsurf/agents/` 配置。

因此，按当前工作区事实源，AIIDE 侧不能假设已有可维护的 subagent 配置体系。

但当前 IDE 工具层有类似 `code_search` 的搜索子代理能力，可以用于只读代码检索。这属于工具能力，不等于项目内可维护的 SystemAgent subagent 配置。

---

## 2. tackle 的模式

`Resources/Skills/tackle` 的核心做法是：

```text
用户需求
-> task-creator / split-work-package
-> human-checkpoint
-> agent-dispatcher
-> checklist / experience-logger
-> completion-report
```

其中 `agent-dispatcher` 的关键点是：

- 将大任务拆成多个工作包。
- 每个工作包绑定一个 Teamee。
- 1:1 执行，不允许一个 Teamee 认领多个任务。
- 主 Agent 维护依赖图和 blockedBy。
- 主 Agent 监控循环动态创建、销毁、重试子代理。
- 子代理完成后需要清理团队资源。
- 有并发上限、超时、守护进程、心跳、清理回退。

这套模式适合“多个独立工作包并行交付”。

但它的成本也很高：

- 需要稳定的团队/任务工具。
- 需要工作包格式稳定。
- 需要依赖图和资源清理。
- 需要失败恢复和超时处理。
- 需要防止多个代理同时改同一文件。
- 需要隔离 Git worktree 或严格 owner 边界。

---

## 3. SystemAgent 是否需要 subagent

### 3.1 现在不需要完整并行执行团队

当前 SystemAgent 的主要问题不是执行速度不够，而是：

- 入口太重。
- 上下文事实源太多。
- OpenSpec/SDD/Workflow/Skill/Role 边界还在重构。
- hook 可靠性还需要修。
- 独立 SDD 还没跑通。
- worktree 策略还没有落地。

如果现在引入 tackle 式 dispatcher，会把问题从“流程复杂”放大为“多代理流程更复杂”。

特别是 SlimeAI 工作区有多个 Git 边界：

```text
/home/slime/Code/SlimeAI
/home/slime/Code/SlimeAI/SlimeAI
/home/slime/Code/SlimeAI/Games/BrotatoLike
```

如果没有 worktree 隔离和文件 owner 协议，多个 subagent 并行改代码很容易造成冲突或跨仓误改。

### 3.2 需要保留 subagent 作为可选能力

虽然不建议现在实现完整 dispatcher，但 subagent 在 SystemAgent 中仍有实际使用场景。

重点不是“多代理并行写代码”，而是“把主对话不适合承担的上下文探索、独立评审和证据收集交给受限子代理”。

---

## 4. 真实使用场景

### 4.1 只读资料搜索

这是最适合的场景。

用户请求：

```text
研究某个外部框架
对比 Resources/Agent 下几个项目
找 GameOS 中某类旧 ECS 痕迹
搜索现有 SystemAgent 规则冲突
```

subagent 可以：

- 在限定目录内搜索。
- 读取相关文件。
- 输出证据摘要。
- 标注路径、行号、置信度。
- 把重要结论返回主对话。

主对话负责：

- 判断哪些证据可信。
- 做最终设计取舍。
- 决定是否写入 SDD。

这个场景收益高，风险低，因为 subagent 不写文件。

### 4.2 多源研究汇总

当资料来源天然分散时，subagent 有价值。

例子：

```text
同时研究 Resources/Skills/tackle、superpowers、现有 SystemAgent、OpenSpec 历史
```

可以拆成多个只读研究子任务：

```text
Subagent A: 阅读 tackle 并总结工作包/dispatcher 模型
Subagent B: 阅读 superpowers 并总结 brainstorming/spec 模型
Subagent C: 阅读现有 SystemAgent 并总结当前冲突
Main: 合并为 DesignDiscovery/SDD/SystemAgent 方案
```

适用条件：

- 每个资料源相互独立。
- 输出是摘要和证据，不是代码改动。
- 主对话仍做综合判断。

### 4.3 独立评审

这比并行实现更适合 SystemAgent。

例子：

- `DesignCritic` 作为 subagent 评审设计确认包。
- `Reviewer` 作为 subagent 检查计划或实现证据。
- `TestDesigner` 作为 subagent 判断测试标准答案是否完整。

但需要注意：

- Role 不等于 subagent。
- Role 可以在主对话内执行。
- 只有当需要“独立视角”或“避免主对话自证正确”时，才值得派 subagent。

### 4.4 大型只读审计

适合 subagent。

例子：

```text
审计所有 SystemAgent 文档中的旧 OpenSpec 默认入口
审计所有 .ai-config skill 是否复制 workflow 正文
审计所有 DocsAI 中的旧路径
```

每个 subagent 负责一个目录或一种 pattern，输出：

```text
Finding:
- path
- line
- issue
- recommendation
- confidence
```

主对话统一决定是否批量修复。

### 4.5 并行实现

当前不推荐。

只有满足以下条件才考虑：

- SDD 已经拆成独立工作包。
- 每个工作包有明确文件 owner。
- 工作包之间没有共享写入文件。
- 每个工作包有独立验证命令。
- worktree 策略已经落地。
- 有清理、超时、失败恢复机制。

否则并行实现容易制造更多协调成本。

---

## 5. 不适合使用 subagent 的场景

### 5.1 小任务

单文件修复、文档措辞、链接修正不需要 subagent。

subagent 启动成本高于收益。

### 5.2 高耦合设计判断

如果任务需要持续权衡用户意图、架构边界、实现方案和验证策略，主对话更适合。

subagent 可以提供材料，但不应替主对话做最终取舍。

### 5.3 多代理同时写同一事实源

不适合。

例如：

- 多个 subagent 同时改 `Workspace/SystemAgent/README.md`。
- 多个 subagent 同时改 `.ai-config/skills/`。
- 多个 subagent 同时改同一个 SDD `tasks.md`。

这些应该由主对话统一写入。

### 5.4 跨 Git 边界写操作

不适合在未隔离时并行执行。

尤其是：

```text
SlimeAI/
Games/BrotatoLike/
Games/BrotatoLike/SlimeAI/
```

---

## 6. 推荐采纳策略

### 6.1 现在只做 Subagent Policy，不做 Dispatcher

当前阶段建议：

```text
不实现 tackle 式 agent-dispatcher。
不新增并行实现 workflow。
不把 subagent 作为 NewFeature 默认步骤。
```

但应该补充一条 SystemAgent subagent policy：

```text
Subagent 只能在明确有收益时使用；默认只读；写操作必须由主对话统一执行。
```

### 6.2 优先支持只读 Research Subagent

最小可用形态：

```text
ResearchSubagent:
- 输入：问题、限定目录、必须返回的证据格式
- 行为：只读搜索和阅读
- 输出：Evidence / Inference / Unknown
- 禁止：改文件、运行高风险命令、做最终决策
```

它可以被这些 workflow 调用：

- `ResearchAdoption`
- `DesignDiscovery`
- `WorkflowIteration`
- `ValidationRelease`

### 6.3 复用现有 Planner/Reviewer/TestDesigner subagent

已有 subagent 不需要废弃。

但定位要调整：

- 不要把它们当成完整流程执行器。
- 把它们当成“独立视角启动器”。
- 输出必须结构化，方便主对话合并。

### 6.4 将来再考虑 Dispatcher

只有在这些基础能力完成后，才考虑类似 tackle 的 dispatcher：

1. 独立 SDD 跑通。
2. SDD work package 格式稳定。
3. worktree 策略落地。
4. validation artifact 标准稳定。
5. subagent 输出格式稳定。
6. 有 cleanup / timeout / conflict 策略。

---

## 7. 与 Workflow / Skill / Role 的关系

Subagent 不是新的顶层概念。

它是某些 skill 或 workflow 的执行基座。

```text
Workflow
  ├── calls skill: design-discovery
  │       └── optional subagent: ResearchSubagent
  ├── calls skill: sdd-management
  ├── uses role: DesignCritic
  │       └── optional subagent: independent critic
  └── calls skill: validation-release
          └── optional subagent: Reviewer/TestDesigner
```

分层关系：

| 概念 | 作用 | 是否等于 subagent |
| --- | --- | --- |
| Workflow | 完整流程编排 | 否 |
| Skill | 可复用能力 | 否 |
| Role | 执行视角 | 否 |
| Subagent | 独立上下文执行者 | 否 |
| SDD | artifact 容器 | 否 |

一个 role 可以由主对话执行，也可以交给 subagent 执行。

一个 skill 可以直接由主对话执行，也可以内部派 subagent 收集材料。

---

## 8. 输出格式建议

如果 SystemAgent 使用 subagent，必须要求它返回结构化结果。

推荐格式：

```text
Subagent Report:
- Scope: 本次搜索/评审范围
- Evidence: 带路径和行号的事实
- Inference: 基于证据的推断
- Unknown: 未确认或未覆盖的部分
- Risks: 如果主对话采用该结论，可能的风险
- Recommended Main-Thread Action: 建议主对话下一步
- Files Touched: 必须为 none，除非用户明确授权写操作
```

只读 subagent 的 `Files Touched` 必须是 `none`。

---

## 9. 最终建议

### 现在不做

- 不做 tackle 式完整 agent-dispatcher。
- 不做并行实现默认流程。
- 不把 subagent 加进所有 workflow。
- 不让多个 subagent 同时写同一个事实源。

### 现在可以做

- 在 SystemAgent 方案中记录 Subagent Policy。
- 明确 AIIDE 当前没有同类可维护 `.windsurf/agents` 配置事实源。
- 复用 AICLI 现有 Planner/Reviewer/TestDesigner/Retrospective subagent。
- 将 ResearchSubagent 作为未来 capability 的候选。
- DesignDiscovery 可以按需调用只读研究 subagent，但不是默认必须。

### 将来再做

当 SDD、worktree、validation artifact 都稳定后，再考虑：

```text
SDD WorkPackage Dispatcher
```

它可以参考 tackle，但必须更保守：

- 默认只读。
- 默认串行写入。
- 并行写代码必须使用 worktree 或明确文件 owner。
- 所有 subagent 结果由主对话合并。
- 主对话对最终改动负责。
