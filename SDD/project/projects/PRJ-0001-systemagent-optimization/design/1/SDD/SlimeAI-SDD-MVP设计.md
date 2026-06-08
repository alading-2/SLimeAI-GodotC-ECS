# SlimeAI SDD MVP 设计

> 日期：2026-05-24  
> 范围：替代/简化 OpenSpec 的 SlimeAI 专属 SDD 管理系统 MVP  
> 目标：用更少命令、更少上下文、更强恢复能力，管理中大型设计与执行任务

---

## 1. 背景与问题

当前 SystemAgent 已经不是“没有规则”的问题，而是进入了“规则很多，但执行成本过高”的阶段。

现有问题集中在三类：

1. **OpenSpec 命令面过宽**  
   `propose/explore/apply/archive/status/instructions/validate/sync/schema` 等命令适合通用工具，但对 SlimeAI 工作区来说太重。实际聊天记录中 OpenSpec 命令出现次数过多，恢复任务时还要重复读取大量 `contextFiles`。

2. **artifact 分散且有重复维护**  
   OpenSpec 的 `proposal.md/design.md/specs/tasks.md/execution-log.md` 在长任务中会变成多处同步：任务完成要改 `tasks.md`，新结论要改 `execution-log.md`，规格变化还要考虑 delta spec 和 archive merge。

3. **进度记录不足以恢复真实上下文**  
   `[x]` 只能说明任务完成，不能说明“为什么这么做、发现了什么、下次从哪里继续”。这导致恢复任务时仍然需要重新读大量文档和命令输出。

因此，SlimeAI 需要的不是 fork OpenSpec，而是一个**专属、轻量、可验证、以恢复上下文为核心的 SDD 系统**。

---

## 2. 外部参考结论

### 2.1 OpenSpec

OpenSpec 的优势是 artifact-driven 和 CLI 管理，但它的通用能力对当前框架偏重：

- 支持 schema、模板、workspace、sync、archive、instructions。
- 支持 `proposal/design/specs/tasks` 的完整流程。
- 支持 AI 读取 enriched instructions。

对 SlimeAI 的启发：

- 保留 artifact 完整性检查。
- 保留 CLI list/show/validate 的管理能力。
- 不保留复杂 schema、delta spec merge、per-artifact instructions 和 archive sync。

### 2.2 Kiro Specs

Kiro 使用较直观的三件套：

- `requirements.md` / `bugfix.md`
- `design.md`
- `tasks.md`

并强调任务实时状态、任务依赖和并行执行。

对 SlimeAI 的启发：

- SDD 文件数量必须少而稳定。
- `tasks.md` 要能表达依赖和执行状态。
- 状态更新要围绕执行恢复，而不是流程仪式。

### 2.3 GitHub Spec Kit

Spec Kit 的命令链是 `specify -> plan -> tasks -> implement`，并引入 constitution 约束架构原则。

对 SlimeAI 的启发：

- 可以保留“项目宪法/原则”思想，但它应来自现有 `AGENTS.md`、`Workspace/SystemAgent/`、`SlimeAI/DocsAI/`，不应在每个 SDD 中复制。
- `specify/plan/tasks` 这种链条可以压缩为 SlimeAI 专用命令。
- 不要让每个 SDD 生成过多 Markdown，否则会重蹈 OpenSpec 的上下文负担。

### 2.4 SDD 风险

Martin Fowler 与 Thoughtworks 对 SDD 的核心提醒：

- 一个工作流不能适配所有任务大小。
- 过多 Markdown 会让人宁可 review 代码，也不愿 review 文档。
- spec 并不能自动带来控制力，AI 仍可能忽略或过度执行指令。
- BDD 的 Given/When/Then 有价值，但应覆盖关键行为，不应穷举所有边界。

对 SlimeAI 的结论：

- 小任务不强制 SDD。
- 中大型任务使用 SDD，但 SDD 本身必须精简。
- BDD 作为 AI 可读行为约束，而不是每个 SDD 的大篇幅仪式。

---

## 3. MVP 目标

SDD MVP 要解决 5 个问题：

1. **可发现**  
   能通过根索引按状态、系统、Git 边界、标签找到 SDD。

2. **可进入**  
   点进某个 SDD 后，通过本地 `README.md` 立即知道它是什么、什么时候改、影响哪里、该读哪些文件。

3. **可执行**  
   `tasks.md` 能表达任务拆分、依赖、状态和验证要求。

4. **可恢复**  
   `progress.md` 记录运行中的核心结论、决策、验证和 resume notes。

5. **可验证**  
   CLI 能检查必需文件、状态目录一致性、任务完成度、索引有效性和 BDD 是否适用。

---

## 4. 非目标

MVP 不做以下事情：

1. **不 fork OpenSpec**  
   不继承 OpenSpec 的 schema、instructions、sync、archive 复杂度。

2. **不做 spec-as-source**  
   SDD 是任务级事实源，不取代代码。完成后代码仍是实现事实源，SDD 是设计和决策历史。

3. **不自动改分支**  
   MVP 不自动创建 Git branch/worktree，避免跨 Git 边界误操作。

4. **不生成大量附属文档**  
   不默认生成 `research.md/data-model.md/contracts/quickstart.md` 等文件。需要时放入 `notes.md` 或 `artifacts/`。

5. **不强制所有任务使用 SDD**  
   拼写、单文件小修、临时排查不强制创建 SDD。

6. **不让 SystemAgent 归档 SDD 的设计文档**  
   设计文档属于具体 SDD 的生命周期，由 SDD 自己保存、索引和归档。SystemAgent 只引用或消费 SDD，不负责跨目录收编设计文档。

---

## 5. 核心设计原则

### 5.1 一个 SDD 是一个上下文胶囊

一个 SDD 必须能回答：

- 为什么做？
- 改哪里？
- 当前做到哪？
- 关键结论是什么？
- 下一次如何恢复？
- 怎么验证完成？

### 5.2 README 是入口卡片，不是正文

用户补充的“每个 SDD 要有 README”应进入 MVP，但必须限制职责。

`README.md` 只负责：

- 说明这个 SDD 是什么。
- 说明创建/更新时间。
- 说明影响的系统、路径、Git 边界。
- 给出阅读顺序。
- 给出当前 resume 摘要。
- 链接到设计、任务、进度、BDD。

`README.md` 不负责：

- 不写完整设计。
- 不写完整进度日志。
- 不复制 `tasks.md`。
- 不复制 `progress.md` 的所有结论。

这样它解决索引问题，但不会变成新的冗余文档。

### 5.3 SDD 自己管理设计文档生命周期

设计文档不应长期散落在 `Workspace/DocsAI/Idea/...` 这类临时分析目录里，也不应由 SystemAgent 负责后续归档。

正确做法是：**一个 SDD 文件夹就是一个任务的完整归档单元**。

如果任务有一个设计文档，就放在该 SDD 的 `design/` 目录中。

如果任务有多个设计文档，也统一放在该 SDD 的 `design/` 目录中，并用 `design/INDEX.md` 做短索引。

这样任务完成后，只要把整个 SDD 移到 `done/`，设计、任务、进度、BDD、验证产物就一起完成归档，不需要 SystemAgent 再维护跨目录索引。

### 5.4 任务进度与核心结论分离

`tasks.md` 负责“做没做”。

`progress.md` 负责“做的过程中发现了什么、为什么这样做、下次从哪里继续”。

这比把所有内容塞进 `tasks.md` 更清楚，也比 OpenSpec 的独立 `execution-log.md` 更轻，因为 SDD 直接把 `progress.md` 作为任务恢复的主入口。

### 5.5 BDD 是行为约束，不是流程仪式

BDD 应存在，但不是每个 SDD 都必须写大量场景。

需要写 BDD 的情况：

- CLI 行为。
- Workflow / Hook / Gate 行为。
- GameOS Capability 行为。
- UI/HUD 行为。
- 任何会影响用户、AI、运行时系统可观察行为的变更。

可以标记不适用的情况：

- 纯研究文档。
- 纯目录整理。
- 没有可观察行为变化的元分析。

BDD 的目标是让 AI 明确“系统应表现为什么”，而不是穷举测试用例。

---

## 6. 推荐目录结构

```text
SDD/
├── README.md
├── INDEX.md
├── catalog.json
├── pending/
│   └── SDD-0001-example/
├── active/
│   └── SDD-0002-example/
├── blocked/
│   └── SDD-0003-example/
└── done/
    └── SDD-0004-example/
```

单个 SDD：

```text
SDD/active/SDD-0002-example/
├── README.md
├── sdd.json
├── design/
│   ├── INDEX.md
│   └── SlimeAI-SDD-MVP设计.md
├── tasks.md
├── progress.md
├── bdd.md
├── notes.md
└── artifacts/
```

### 6.1 为什么用 `sdd.json` 而不是 `sdd.yaml`

MVP 推荐 `sdd.json` 作为机器元数据文件：

- Python / dotnet / shell 都能无依赖读取 JSON。
- 避免为了 YAML 引入依赖。
- 人类入口由 `README.md` 承担，机器入口由 `sdd.json` 承担。

如果后续认为 YAML 更适合手写，可以在 v2 再迁移。

### 6.2 根目录文件职责

| 文件 | 职责 | 是否手写 |
| --- | --- | --- |
| `SDD/README.md` | 说明 SDD 系统怎么用 | 手写 |
| `SDD/INDEX.md` | 人类可读总索引，按状态/系统/标签列出 SDD | CLI 生成，可手动补充 |
| `SDD/catalog.json` | 机器可读全局索引 | CLI 生成 |

### 6.3 单个 SDD 文件职责

| 文件 | 职责 | 是否必需 |
| --- | --- | --- |
| `README.md` | 当前 SDD 的入口卡片 | 必需 |
| `sdd.json` | CLI 元数据源 | 必需 |
| `design/` | 一个或多个完整设计文档及短索引 | 必需 |
| `tasks.md` | 任务拆分和进度 | 必需 |
| `progress.md` | 运行中核心结论与恢复记录 | 必需 |
| `bdd.md` | 行为场景或不适用说明 | 必需 |
| `notes.md` | 参考资料、开放问题、补充说明 | 必需 |
| `artifacts/` | 验证日志、截图、临时报告等 | 可选 |

---

## 7. 状态设计

MVP 使用 4 个状态目录：

| 状态 | 目录 | 含义 |
| --- | --- | --- |
| `pending` | `SDD/pending/` | 已创建但未开始执行 |
| `active` | `SDD/active/` | 正在执行，是默认工作状态 |
| `blocked` | `SDD/blocked/` | 因缺信息、失败、外部依赖暂停 |
| `done` | `SDD/done/` | 已完成并验证 |

### 7.1 为什么需要 `blocked`

只用“未完成/正在执行/已完成”虽然更少，但会丢失一种重要状态：任务不是没开始，也不是正在做，而是**不能继续**。

`blocked` 能让 CLI 清楚列出：

- 哪些任务需要用户回答。
- 哪些任务被测试失败阻塞。
- 哪些任务等待外部资源。
- 哪些任务不应该被 AI 自动继续。

因此 `blocked` 是必要状态，不是冗余状态。

### 7.2 状态流转

```text
pending -> active -> done
             |
             v
          blocked -> active
```

允许的特殊流转：

- `active -> pending`：任务降级或暂停，需要写入原因。
- `done -> active`：默认不推荐。若发现后续问题，优先创建新 SDD，并在新 SDD 中引用原 SDD。

### 7.3 状态一致性规则

CLI validate 必须检查：

- `sdd.json.status` 与所在目录一致。
- `README.md` 中展示的状态与 `sdd.json.status` 一致。
- `done` 状态必须所有必需任务完成，并记录验证结果。
- `blocked` 状态必须有 blocker 原因和下一步解除条件。

---

## 8. `README.md` 入口卡片设计

每个 SDD 的 `README.md` 是用户补充需求中最重要的索引入口。

推荐模板：

```markdown
# SDD-0002 Example Title

## Index Card

- **Status**: active
- **Created**: 2026-05-24
- **Updated**: 2026-05-24
- **Type**: workflow | feature | fix | research | migration
- **Scope**: Workspace/SystemAgent
- **Git Boundary**: /home/slime/Code/SlimeAI
- **Affected Areas**:
  - Workspace/SystemAgent/Workflows
  - Workspace/SystemAgent/Protocols
- **Tags**: systemagent, sdd, cli

## What This SDD Is About

一句话说明这个 SDD 要解决什么问题。

## Reading Order

1. `design/INDEX.md` — 设计文档列表和主设计入口
2. `tasks.md` — 当前任务拆分
3. `progress.md` — 最近结论和恢复点
4. `bdd.md` — 行为场景或不适用说明
5. `notes.md` — 参考与开放问题

## Current Resume

- **Current Task**: T2.1
- **Last Conclusion**: 最近一条核心结论
- **Next Action**: 下一步做什么
```

### 8.1 README 长度限制

建议硬性约束：

- 目标长度：30-60 行。
- 超过 100 行时 validate 给 warning。
- 不允许写完整分析过程。
- 不允许复制 `progress.md` 时间线。

### 8.2 README 与索引的关系

SDD 采用三层索引：

1. **全局机器索引**：`SDD/catalog.json`  
   给 CLI 查询和验证使用。

2. **全局人类索引**：`SDD/INDEX.md`  
   给人按状态、系统、标签浏览。

3. **局部入口索引**：每个 SDD 的 `README.md`  
   给人进入单个 SDD 时快速判断“这是什么”。

`README.md` 是必要的，因为 `INDEX.md` 只解决“找到它”，不能解决“进入后快速恢复上下文”。

---

## 9. `sdd.json` 元数据设计

示例：

```json
{
  "id": "SDD-0002",
  "slug": "systemagent-sdd-mvp",
  "title": "SystemAgent SDD MVP",
  "status": "active",
  "type": "workflow",
  "created_at": "2026-05-24",
  "updated_at": "2026-05-24",
  "scope": "Workspace/SystemAgent",
  "git_boundaries": [
    "/home/slime/Code/SlimeAI"
  ],
  "affected_areas": [
    "Workspace/SystemAgent",
    "SDD/project/projects/PRJ-0001-systemagent-optimization/design"
  ],
  "tags": [
    "systemagent",
    "sdd",
    "cli"
  ],
  "progress": {
    "current_task": "T2.1",
    "completed_tasks": 3,
    "total_tasks": 12,
    "percent": 25
  },
  "bdd": {
    "required": true,
    "reason": "This SDD defines CLI and workflow behavior."
  },
  "links": {
    "design_index": "design/INDEX.md",
    "main_design": "design/SlimeAI-SDD-MVP设计.md",
    "tasks": "tasks.md",
    "progress": "progress.md",
    "bdd": "bdd.md",
    "notes": "notes.md"
  }
}
```

### 9.1 元数据边界

`sdd.json` 不写长文本，只写可索引字段。

长文本分别进入：

- `design/`
- `progress.md`
- `notes.md`

这样 CLI 不需要解析 Markdown 才能列出和过滤 SDD。

---

## 10. `design/` 设计文档集合

`design/` 是 SDD 的完整设计文档归档区。

这里不强制所有设计文档套同一个格式。原因是：真实设计文档往往就是一次深度分析的自然结果，例如本文档本身 `SlimeAI-SDD-MVP设计.md`。如果强制改写成固定模板，会丢失原始表达结构，也会制造额外整理成本。

因此 MVP 采用“**保留原样 + 短索引**”模型：

```text
design/
├── INDEX.md
├── SlimeAI-SDD-MVP设计.md
├── 方案对比.md
└── 迁移策略.md
```

### 10.1 `design/INDEX.md`

`design/INDEX.md` 是设计文档集合的短索引，不是第二份设计正文。

推荐格式：

```markdown
# Design Index

## Main Design

- `SlimeAI-SDD-MVP设计.md`

## Documents

| File | Role | Status | Updated | Notes |
| --- | --- | --- | --- | --- |
| `SlimeAI-SDD-MVP设计.md` | main | current | 2026-05-24 | SDD MVP 主设计 |
| `方案对比.md` | analysis | superseded | 2026-05-24 | 已被主设计吸收 |
```

### 10.2 设计文档保留原样

设计文档可以是自由结构，只要它能清楚表达：

- 原始问题或提示。
- 分析过程。
- 关键结论。
- 最终设计或取舍。
- 影响范围。
- 后续执行建议。

这些是内容要求，不是标题模板要求。

### 10.3 多设计文档管理

当一个任务有多个设计文档时，不要散落在 `Workspace/DocsAI/Idea/...`、SystemAgent 子目录或临时目录中。

应统一放入当前 SDD 的 `design/`。

如果某个旧设计已经过时，不建议直接删除。应在 `design/INDEX.md` 中标记为 `superseded` 或 `archived`，并说明被哪个设计吸收或替代。

### 10.4 设计文档更新规则

设计文档不是运行日志。

只有当运行中发现的结论改变了最终设计，才更新或新增设计文档。

普通发现、临时验证、任务恢复点写入 `progress.md`。

### 10.5 完成后的归档规则

任务完成时不单独归档设计文档。

整个 SDD 文件夹移动到 `done/`，其中的 `design/` 一起保留。这就是设计文档归档。

这样不会出现“任务完成了，但设计文档还留在临时 Idea 目录，后来被删掉找不到”的问题。

---

## 11. `tasks.md` 任务设计

`tasks.md` 负责可执行任务拆分。

推荐格式：

```markdown
# Tasks

## Progress

- **Status**: active
- **Completed**: 3/12
- **Current**: T2.1

## Task List

- [x] T1.1 梳理现状
  - **Evidence**: progress.md#p001
  - **Validation**: 文档引用完整

- [ ] T2.1 实现 CLI list/show
  - **Depends On**: T1.2
  - **Files**: Workspace/Tools/sdd/
  - **Validation**: `sdd list --state active`

- [ ] T2.2 实现 validate
  - **Depends On**: T2.1
  - **Validation**: `sdd validate --all`
```

### 11.1 任务粒度

任务应满足：

- 一个任务能在一次 AI session 中完成或明确停止。
- 每个任务有验证方式。
- 有依赖时显式写出。
- 任务完成时链接到 `progress.md` 的证据或结论。

### 11.2 任务不写长说明

如果任务执行中出现新分析，不写在任务项里，而是写入 `progress.md`，再在任务项中链接。

---

## 12. `progress.md` 运行记录设计

`progress.md` 是 SDD 相比 OpenSpec 最重要的增强点。

它不是普通日志，而是**恢复上下文的事实账本**。

推荐结构：

```markdown
# Progress

## Latest Resume

- **Updated**: 2026-05-24 16:55
- **Current Task**: T2.1
- **Last Completed**: T1.2
- **Next Action**: 实现 `sdd list` 的状态过滤
- **Open Blockers**: none

## Timeline

### P001 — 2026-05-24 — decision

- **Context**: 是否每个 SDD 需要 README。
- **Conclusion**: 需要，但只作为入口卡片，不承载正文。
- **Evidence**: 单个 SDD 进入后需要快速知道时间、范围、影响系统、阅读顺序。
- **Impact**: README 纳入 MVP 必需文件；validate 限制长度和字段。
- **Resume**: 后续实现时先生成 README 模板，再生成 catalog。
```

### 12.1 记录类型

`progress.md` 只记录关键事实：

- `decision`：设计决策。
- `finding`：调研发现。
- `validation`：验证结果。
- `blocker`：阻塞原因。
- `resume`：恢复说明。
- `change`：设计或范围变化。

### 12.2 更新节奏

必须更新 `progress.md` 的场景：

- 完成一个任务组。
- 发现会影响设计的新事实。
- 验证失败并改变下一步计划。
- 进入 blocked。
- 准备结束当前 session。

不需要更新的场景：

- 简单读文件。
- 临时命令输出且不影响结论。
- 与任务无关的聊天。

---

## 13. `bdd.md` 行为场景设计

`bdd.md` 负责 AI 可读行为约束。

推荐格式：

```markdown
# BDD

## Applicability

- **Required**: true
- **Reason**: This SDD changes CLI behavior.

## Scenarios

### Scenario: List active SDDs

Given there are SDDs in `active/` and `done/`
When the user runs `sdd list --state active`
Then only active SDDs are displayed
And each row includes id, title, updated date, affected areas, and current task

### Scenario: Validate status directory consistency

Given an SDD is stored under `active/`
And its `sdd.json.status` is `done`
When the user runs `sdd validate`
Then validation fails
And the report explains the directory/status mismatch
```

### 13.1 BDD 写作规则

- 使用领域语言，不写实现细节。
- 覆盖关键路径和关键失败路径。
- 不穷举所有边界。
- 每个场景都应能映射到测试、验证命令或人工验收。

### 13.2 不适用模板

```markdown
# BDD

## Applicability

- **Required**: false
- **Reason**: This SDD is a research-only document and does not change observable behavior.
```

---

## 14. `notes.md` 与 `artifacts/`

`notes.md` 放不适合进入正文但仍有价值的信息：

- 参考链接。
- 调研摘要。
- 暂不采纳的想法。
- 开放问题。
- 用户补充说明。

`artifacts/` 放验证产物：

- 日志。
- 截图。
- 生成报告。
- CLI 输出快照。

规则：

- `notes.md` 不作为恢复主入口。
- `artifacts/` 不进入默认 AI 上下文，除非 `progress.md` 或 `tasks.md` 明确引用。

---

## 15. CLI MVP 设计

命令名建议为 `sdd`。

### 15.1 必需命令

| 命令 | 用途 |
| --- | --- |
| `sdd new <title>` | 创建新 SDD，默认进入 `pending/` |
| `sdd list` | 列出 SDD，可按状态/系统/标签过滤 |
| `sdd show <id>` | 显示单个 SDD 的 README 和 resume |
| `sdd start <id>` | `pending/blocked` 转为 `active` |
| `sdd note <id>` | 追加 progress 记录 |
| `sdd task <id>` | 更新任务状态 |
| `sdd block <id>` | 标记 blocked 并写阻塞原因 |
| `sdd done <id>` | 完成并移动到 `done/` |
| `sdd validate [id]` | 验证结构和状态一致性 |
| `sdd index` | 重建 `INDEX.md` 和 `catalog.json` |

### 15.2 命令示例

```bash
sdd new "SystemAgent SDD MVP" --type workflow --scope Workspace/SystemAgent --area Workspace/SystemAgent --tag systemagent --tag sdd
sdd list --state active
sdd show SDD-0002
sdd start SDD-0002
sdd note SDD-0002 --type decision "README is required as an entry card only."
sdd task SDD-0002 done T1.1
sdd block SDD-0002 "Need user decision on README generation policy."
sdd done SDD-0002 --validation "sdd validate SDD-0002 passed"
sdd validate --all
sdd index
```

### 15.3 输出风格

`sdd list` 默认输出应短：

```text
ID        State    Updated     Scope                  Title
SDD-0002  active   2026-05-24  Workspace/SystemAgent  SystemAgent SDD MVP
```

`sdd show` 输出应优先展示 `README.md` 的入口卡片和 `progress.md` 的 `Latest Resume`。

### 15.4 JSON 输出

MVP 可以支持：

```bash
sdd list --json
sdd show SDD-0002 --json
sdd validate --json
```

但 JSON 输出不是第一优先级。第一优先级是让人和 AI 在终端中低成本恢复上下文。

---

## 16. Validate 规则

`sdd validate` 是 MVP 的核心。

### 16.1 单个 SDD 必查

- `README.md` 存在。
- `sdd.json` 存在且是合法 JSON。
- `design/`、`tasks.md`、`progress.md`、`bdd.md`、`notes.md` 存在。
- `design/INDEX.md` 存在，且至少索引一个设计文档。
- `sdd.json.links.main_design` 指向的主设计文档存在。
- `sdd.json.status` 与所在目录一致。
- `README.md` 的 status、updated、scope 与 `sdd.json` 一致。
- `tasks.md` 的完成数量与 `sdd.json.progress` 一致。
- `done` 状态必须有验证记录。
- `blocked` 状态必须有 blocker 记录。
- `bdd.required=true` 时，`bdd.md` 至少有一个 Scenario。
- `bdd.required=false` 时，必须有 reason。

### 16.2 全局必查

- SDD ID 不重复。
- 同一个 SDD 不出现在多个状态目录。
- `catalog.json` 能覆盖所有 SDD。
- `INDEX.md` 不包含已经不存在的 SDD。
- 每个 affected area 至少是可解释路径或明确的系统名。
- 每个 done SDD 都保留自己的 `design/`，不依赖外部临时设计文档路径。

### 16.3 Warning 而非 Error

以下情况只给 warning：

- `README.md` 超过 100 行。
- `progress.md` 超过一定长度但没有 `Latest Resume` 更新。
- `tasks.md` 存在超过 20 个未拆分的大任务。
- `notes.md` 过长且没有索引。
- `design/` 下存在多个文档但 `design/INDEX.md` 没有标记 main/current。

---

## 17. Root Index 设计

`SDD/INDEX.md` 推荐由 CLI 生成。

示例：

```markdown
# SDD Index

## Summary

- **pending**: 2
- **active**: 1
- **blocked**: 1
- **done**: 4

## Active

| ID | Title | Updated | Scope | Current Task |
| --- | --- | --- | --- | --- |
| SDD-0002 | SystemAgent SDD MVP | 2026-05-24 | Workspace/SystemAgent | T2.1 |

## By Scope

### Workspace/SystemAgent

- SDD-0002 — SystemAgent SDD MVP

### SlimeAI/GameOS

- SDD-0005 — Damage Capability Refactor

## By Tag

### sdd

- SDD-0002 — SystemAgent SDD MVP
```

### 17.1 Root README 与 INDEX 的区别

`SDD/README.md` 说明制度。

`SDD/INDEX.md` 展示实例列表。

这两个文件不能合并，否则制度说明和动态索引会互相污染。

---

## 18. 什么时候必须创建 SDD

必须创建 SDD：

- 跨多个模块或 Git 边界的改动。
- 修改 SystemAgent workflow、rule、hook、skill、gate。
- 修改 GameOS Capability 架构或公共 contract。
- 需要长期恢复上下文的任务。
- 用户明确要求设计、计划、跟踪和复盘。

不强制创建 SDD：

- 单文件小修。
- 拼写、链接、格式修正。
- 一次性临时排查。
- 不改变长期事实源的局部测试修复。

### 18.1 小任务为什么不强制

SDD 的价值来自降低长期上下文成本。

如果任务 10 分钟能完成，创建 SDD 反而是负担。

这条规则用于避免 SDD 变成新的 OpenSpec ritual。

---

## 19. 与 SystemAgent 的关系

SDD 不替代 SystemAgent。

SDD 替代的是 OpenSpec change 在 SlimeAI 中承担的任务管理职责。

建议关系：

- `Workspace/SystemAgent/`：长期 workflow、role、gate、policy 事实源。
- `SDD/`：中大型任务的设计文档集合、任务、进度、行为场景和恢复事实源。
- `SlimeAI/DocsAI/`：框架长期知识。
- `Games/*/DocsAI/`：游戏侧长期知识。

### 19.1 SDD 与 SystemAgent 的归档边界

SDD 应独立管理自己的生命周期。

SystemAgent 不负责把散落的设计文档收编、归档或建立跨目录索引。

原因是：

- SDD 未来可以做成插件，应保持与 SystemAgent 解耦。
- SystemAgent 负责“流程如何运行”，不应再承担“每个任务的资料怎么归档”。
- 设计文档直接放入 SDD 后，归档只需要移动整个 SDD 文件夹。
- 如果归档由 SystemAgent 完成，会重新制造跨目录索引和长期维护负担。

SystemAgent 只需要在 workflow 中识别当前是否有 SDD，并按 SDD README / catalog 读取它。

### 19.2 未来 OpenSpec 退场路径

MVP 阶段不立刻删除 OpenSpec。

建议先并行：

1. 新任务优先使用 SDD。
2. 已存在 OpenSpec change 继续按原流程收尾。
3. SDD CLI 稳定后，再写迁移方案。
4. 最后把 SystemAgent 规则里的 OpenSpec 默认入口改为 SDD。

---

## 20. MVP 实施阶段

### Phase 0：文档模板

目标：不用 CLI 也能手动创建规范 SDD。

产物：

- `SDD/README.md`
- `SDD/templates/README.md`
- `SDD/templates/sdd.json`
- `SDD/templates/design/INDEX.md`
- `SDD/templates/tasks.md`
- `SDD/templates/progress.md`
- `SDD/templates/bdd.md`
- `SDD/templates/notes.md`

### Phase 1：只读 CLI

目标：先解决索引和验证。

命令：

- `sdd list`
- `sdd show`
- `sdd validate`
- `sdd index`

### Phase 2：写入 CLI

目标：支持创建和状态流转。

命令：

- `sdd new`
- `sdd start`
- `sdd note`
- `sdd task`
- `sdd block`
- `sdd done`

### Phase 3：SystemAgent 接入

目标：让 SystemAgent workflow 使用 SDD 作为默认中大型任务管理入口。

改动：

- 更新相关 workflow。
- 更新 skill 路由。
- 替换 OpenSpec 默认入口。
- 保留 OpenSpec 作为旧任务兼容路径。

---

## 21. README 是否必须进入 MVP 的最终结论

必须进入。

原因：

1. **全局索引只能解决“找到 SDD”**  
   它不能解决“打开这个 SDD 后立刻理解它”。

2. **`design/` 下的主设计文档太重，不适合作为入口**  
   设计文档必须完整，会包含提示词、分析、方案、风险。每次恢复都先读它会增加上下文负担。

3. **`sdd.json` 适合机器，不适合人类快速阅读**  
   JSON 能保证 CLI 稳定，但不适合作为阅读入口。

4. **README 可以承接用户最关心的索引字段**  
   什么时候改、改什么系统、影响哪里、怎么读、当前从哪里继续。

但 README 必须被限制为入口卡片。

如果 README 开始承载设计正文、完整进度或长分析，它就会变成新的冗余源。

---

## 22. 推荐 MVP 最小文件集

最终推荐：

```text
SDD/
├── README.md          # SDD 制度说明
├── INDEX.md           # 生成的人类索引
├── catalog.json       # 生成的机器索引
├── templates/         # 模板
├── pending/
├── active/
├── blocked/
└── done/
```

单个 SDD：

```text
SDD/<state>/SDD-0001-slug/
├── README.md          # 入口卡片，必须精简
├── sdd.json           # CLI 元数据
├── design/            # 一个或多个完整设计文档
│   ├── INDEX.md       # 设计文档短索引
│   └── main.md        # 主设计文档，可保留原始标题文件名
├── tasks.md           # 任务拆分和进度
├── progress.md        # 核心结论与恢复
├── bdd.md             # 行为场景或不适用说明
├── notes.md           # 参考与补充
└── artifacts/         # 可选验证产物
```

这是 MVP 的最小完整闭环。

---

## 23. 后续待确认问题

实施前建议确认 4 个问题：

1. **SDD 根目录位置**  
   推荐工作区根：`/home/slime/Code/SlimeAI/SDD/`。

2. **CLI 实现语言**  
   推荐 Python 标准库或 .NET tool。若追求无依赖与简单脚本，Python 标准库更快。

3. **是否允许 CLI 自动重写 README**  
   推荐 MVP 中 `sdd index` 可以重建 `INDEX.md/catalog.json`，但单个 SDD 的 `README.md` 先只 validate，不强制自动覆盖。

4. **OpenSpec 退场时机**  
   推荐 SDD MVP 跑通 1-2 个真实任务后，再修改 SystemAgent 默认流程。

---

## 24. 最终建议

先完成 SDD MVP，不 fork OpenSpec。

MVP 必须包含每个 SDD 的 `README.md`，但它的定位是**入口卡片**，不是新文档层级。

最关键的设计是：

- `catalog.json` 负责机器索引。
- `INDEX.md` 负责全局人类索引。
- `README.md` 负责单个 SDD 的局部入口。
- `design/` 负责保存和归档一个或多个完整设计文档。
- `tasks.md` 负责任务状态。
- `progress.md` 负责运行中核心结论和恢复上下文。
- `bdd.md` 负责关键行为约束。

这样可以同时满足：

- 比 OpenSpec 更贴合 SlimeAI。
- 比普通文档更可管理。
- 比 execution-log 更适合恢复任务。
- 比完整 SDD/Spec Kit 更轻，不会把每个任务变成 Markdown 审核负担。
