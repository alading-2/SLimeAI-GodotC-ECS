# 独立 SDD 转向方案

> 日期：2026-05-24  
> 输入：`SystemAgent问题清单.md`、`SDD/SlimeAI-SDD-MVP设计.md`、`08-SDD独立化与文档迁移方案.md`  
> 结论：SystemAgent 应把中大型任务管理职责从 OpenSpec 默认路径迁到 SlimeAI 独立 SDD

---

## 1. 背景

SystemAgent 已经完成一次结构统一：`Workspace/SystemAgent/` 作为 workflow、role、protocol、gate、policy、catalog 的事实源，`.ai-config/skills/systemagent/` 只保留 wrapper skill。

但后续执行暴露了新问题：事实源结构清楚了，执行成本仍然很高。

主要表现是：

- OpenSpec CLI 命令面过宽，恢复任务时反复执行 `status/instructions/validate`。
- OpenSpec artifact 分散在 `proposal/design/specs/tasks/execution-log`，长任务需要多处同步。
- SystemAgent workflow、role、gate、protocol 越补越多，但 AI 实际执行仍靠临场自觉。
- 用户要的是直接干活、可恢复、少命令，而不是更多介绍性入口。

因此，问题不再是“SystemAgent 目录怎么摆”，而是“中大型任务的执行上下文放在哪里，如何低成本恢复”。

---

## 2. 关键判断

### 2.1 SDD 不替代 SystemAgent

独立 SDD 不应该替代 `Workspace/SystemAgent/`。

正确分工是：

| 层级 | 职责 |
| --- | --- |
| `Workspace/SystemAgent/` | 长期流程、角色、gate、policy、触发规则事实源 |
| 独立 `SDD/` 根目录 | 具体中大型任务的设计、任务、进度、行为约束、验证产物和恢复上下文 |
| `SDD/project/projects/PRJ-0001-systemagent-optimization/design/` | 当前过渡分析和用户评审区，不作为正式 SDD 容器 |
| `SlimeAI/DocsAI/` | 框架长期知识与模块契约 |
| `Games/*/DocsAI/` | 游戏侧长期知识 |
| `openspec/` | 既有变更兼容路径，后续可退场 |

SystemAgent 仍回答“怎么工作”。

SDD 回答“这个任务当前做到哪、为什么这么做、下次怎么接”。

### 2.2 SDD 替代的是 OpenSpec 的任务管理职责

OpenSpec 在 SlimeAI 中实际承担了三件事：

1. 保存设计。
2. 保存任务列表。
3. 提供恢复和验证入口。

独立 SDD 应替代第 1-3 点的默认职责，但不需要 fork OpenSpec 的通用能力。

不应继承：

- schema profile。
- workspace sync。
- per-artifact instructions。
- archive merge。
- 复杂 delta spec lifecycle。

应保留：

- artifact 完整性。
- 任务状态。
- validate。
- list/show。
- 可恢复进度。

### 2.3 OpenSpec 不应立刻删除

已有 OpenSpec change 仍按原路径收尾。

新任务优先使用 SDD，等 SDD 跑通 1-2 个真实任务后，再修改 SystemAgent 默认规则。

---

## 3. 推荐目标模型

### 3.1 一个中大型任务 = 一个 SDD 上下文胶囊

每个 SDD 至少包含：

```text
README.md           # 入口卡片
sdd.json            # 机器索引元数据
design/             # 一个或多个完整设计文档，以及 design/INDEX.md
tasks.md            # 任务拆分和状态
progress.md         # 核心结论、决策、验证、恢复点
bdd.md              # 行为场景或不适用说明
notes.md            # 参考资料和开放问题
artifacts/          # 验证日志、截图、临时报告等，可选
```

其中最关键的是 `progress.md`。

它解决 OpenSpec 的核心弱点：`tasks.md` 的 `[x]` 无法恢复“为什么”和“下一步”。

### 3.2 README 是入口卡片

每个 SDD 的 `README.md` 必须存在，但只能做入口卡片。

它不写完整设计，不复制任务，不复制进度。

它只回答：

- 这个 SDD 是什么。
- 当前状态是什么。
- 影响哪些系统和 Git 边界。
- 阅读顺序是什么。
- 当前 resume 是什么。

### 3.3 BDD 是行为约束，不是文档仪式

涉及 workflow、hook、gate、CLI、GameOS capability、UI/HUD 的 SDD 应写 `bdd.md`。

纯研究文档、目录整理、无可观察行为变化的元分析可以标记 BDD 不适用。

---

## 4. SystemAgent 接入方式

### 4.1 Workflow 只判断是否需要 SDD

SystemAgent workflow 不应复制 SDD 模板内容。

它只应判断：

- 当前任务是否需要 SDD。
- 如果需要，当前 SDD ID 是什么。
- 应读 SDD 的哪些文件。
- 完成后应更新 SDD 的哪些文件。
- 如果当前仍在 `Workspace/DocsAI/Idea/...` 分析区，应提示后续迁入正式 SDD 的 `design/`。

### 4.2 Wrapper skill 只做触发器

`.ai-config/skills/systemagent/` 中的 wrapper skill 应保持短入口。

示例职责：

- `systemagent-new-feature`：判断是否需要 SDD，指向 `Workspace/SystemAgent/Workflows/NewFeature.md`。
- `systemagent-debug-fix`：判断 bug 是否跨模块或长期，需要 SDD 时创建/读取 SDD。
- `systemagent-validation-release`：优先读取 SDD 的 `tasks/progress/bdd` 作为验证上下文。

不应把 SDD 制度正文复制到 skill。

### 4.3 Design Discovery 写入 SDD `design/`

新功能、重构、行为改动进入实现前，应优先执行 `Design Discovery`。

其输出不是散落在聊天中，而是：

- 深度设计分析写入当前 SDD 的 `design/`。
- 用户确认、默认假设和关键决策写入 `progress.md`。
- 后续实施项写入 `tasks.md`。
- 可观察行为写入 `bdd.md`。

如果任务尚未创建 SDD，则当前分析文档只能视为过渡材料，不应长期作为 canonical 设计源。

### 4.4 Rule 只放硬边界

`.ai-config/rules/rules.md` 不应写完整 SDD 机制。

只保留硬规则：

- 中大型任务优先使用 SDD。
- 已有 OpenSpec change 按原流程收尾。
- SDD 和 SystemAgent 的事实源边界。
- 禁止把 SDD 当代码事实源。

---

## 5. 与现有问题的对应关系

| 问题 | SDD 方案如何缓解 |
| --- | --- |
| OpenSpec 命令过多 | SDD MVP 只保留 `list/show/validate/index` 等少数命令 |
| contextFiles 过重 | `README + progress Latest Resume` 成为恢复入口 |
| execution-log 额外负担 | `progress.md` 作为 SDD 一等文件，取代外置执行日志 |
| selected workflow 不显式 | SDD README 与 progress 记录 workflow、当前任务、下一步 |
| gate 缺输入 | `tasks/progress/bdd` 提供 review 的稳定输入 |
| 文档太多 | SDD 固定最小文件集，禁止默认生成大量附属文档 |

---

## 6. 不采纳的方向

### 6.1 不继续强化 OpenSpec 默认路径

如果继续优化 `openspec-apply-change`、`instructions apply`、`execution-log`，只能缓解局部成本，不能解决“通用工具过重”的根因。

### 6.2 不把所有任务都 SDD 化

小任务强制 SDD 会把 SDD 变成新的 ritual。

不强制 SDD 的任务：

- 单文件小修。
- 拼写、链接、格式修复。
- 临时排查。
- 不改变长期事实源的局部测试修复。

### 6.3 不让 hook 自动创建或移动 SDD

hook 只做提示和检查，不做写入操作。

否则 hook 会从安全栏变成新的不稳定执行器。

---

## 7. 最终建议

SystemAgent 优化应先承认一个边界：流程正文与任务上下文是两类事实源。

`Workspace/SystemAgent/` 保存流程正文。

独立 SDD 保存任务上下文。

OpenSpec 退到兼容路径。

这样才能同时满足：

- 少命令。
- 少上下文。
- 可恢复。
- 可验证。
- 不把 SystemAgent 再次膨胀成大型流程说明书。
