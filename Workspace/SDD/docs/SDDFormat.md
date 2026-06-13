# SDD Format

## Root

```text
SDD/
├── README.md
├── INDEX.md
├── catalog.json
├── project/
│   ├── projects/
│   └── archived/
├── templates/
├── pending/
├── active/
├── blocked/
└── done/
```

`project/` 是项目级 SDD 容器命名空间。`project/projects/` 存放当前项目，`project/archived/` 存放已归档项目。真实状态来自 `project.json.status`，不是目录名。

`pending/active/blocked/done/` 是 legacy 独立 SDD 目录。新状态流转不再依赖移动目录。

## Project

```text
SDD/project/projects/PRJ-0001-project-slug/
├── README.md
├── project.json
├── Core/
│   ├── roadmap.md
│   ├── progress.md
│   └── notes.md
├── design/
│   ├── INDEX.md
│   └── main.md
└── sdds/
    └── 001-SDD-0001-task-slug/
```

## Instance

```text
SDD/<legacy-state>/SDD-0001-slug/
├── README.md
├── sdd.json
├── design/
│   ├── INDEX.md
│   └── main.md
├── tasks.md
├── progress.md
├── bdd.md
├── notes.md
└── artifacts/
```

项目子 SDD 使用：

```text
SDD/project/projects/PRJ-0001-project-slug/sdds/001-SDD-0001-task-slug/
```

`001` 是项目内顺序，`SDD-0001` 是全局 SDD ID。

## Project Roadmap

项目级 `roadmap.md` 是文档为中心的执行路线图，不是 `project.json.sdds` 的 Markdown 副本。

必须包含：

1. `Design Progress`：按 `design/` 文档列出每份文档的完成情况（done / pending / —）和对应 SDD。多份文档可以合并为一个 SDD；纯上下文 / 索引 / backlog 文档标记为 `—`。
2. `Next SDDs`：列出下一步要创建的 SDD、对应设计文档和目标。不预分配 SDD ID，创建后再回填。

`project.json.sdds` 只保存已创建子 SDD 的 metadata 顺序。`roadmap.md` 保存文档完成追踪和下一步计划；创建或完成子 SDD 后应回填路线图。

## File Roles

| File | Role |
| --- | --- |
| `project.json` | 项目级 CLI 元数据源，保存项目 id、slug、title、status、scope、current_sdd 和 sdds。 |
| `roadmap.md` | 项目执行路线图（位于 `Core/roadmap.md`），以文档为中心追踪每份设计文档的完成情况和对应 SDD，并列出下一步要创建的 SDD。 |
| `Core/progress.md` | 项目级状态面板（位于 `Core/progress.md`），只记录当前项目状态、跨 SDD blocker、少量阶段裁决和验证入口。 |
| `README.md` | 单个 SDD 的入口卡片，展示状态、范围、影响区域、阅读顺序和恢复点。 |
| `sdd.json` | CLI 元数据源，保存 id、slug、title、status、scope、tags、progress 和 links。 |
| `design/` | 任务特定设计或项目级共享设计引用；不默认复制项目级设计快照。 |
| `design/INDEX.md` | 设计索引，标注 main/current 和 shared design references。 |
| `tasks.md` | 可执行任务清单和任务进度。 |
| 子 SDD `progress.md` | 单个任务的状态面板：current / next / blocker / 少量 decision / 最终 validation summary。 |
| `bdd.md` | 本任务 FeatureSpec 行为场景摘录、Source 引用或不适用说明。 |
| `notes.md` | 参考、开放问题和补充说明。 |
| `artifacts/` | 验证日志、报告、截图或 CLI 输出快照。 |

## Status

| Status | Meaning |
| --- | --- |
| `pending` | 已创建但未开始执行。 |
| `active` | 正在执行。 |
| `blocked` | 因缺信息、失败或外部条件暂停。 |
| `done` | 已完成并记录验证结果。 |

真实状态必须来自 `project.json.status` 或 `sdd.json.status`。目录只表达组织或归档位置。

## State Flow

```text
pending -> active -> done
             |
             v
          blocked -> active
```

`start`、`block`、`done` 只更新 metadata、README、tasks 和 progress，不移动 SDD 目录。项目归档使用显式 `project-archive`，将项目从 `SDD/project/projects/` 移到 `SDD/project/archived/`。

## README Boundary

单个 SDD 的 `README.md` 是入口卡片，不写完整设计、不复制任务列表、不复制完整 progress 时间线。

`new` 可以创建初始 README；后续 CLI 写操作只更新明确由 CLI 管理的字段，例如 `Status`、`Updated`、`Current Task` 和 `Open Blockers`。`What This SDD Is About`、阅读顺序说明和人工维护的恢复摘要不应被状态流转命令整体覆盖。

## Progress Boundary

`progress.md` 是状态面板，不是 timeline 全量日志。它默认只保存：

- **State**：当前任务、下一步、阻塞。
- **Decisions**：少量改变方向、边界、验证策略或事实源的裁决。
- **Validation**：最终或关键验证命令、结果摘要、artifact / commit / report 入口。

不推荐写入：

- 每个 `task command`、每次 checkbox 勾选、每个临时文件读取。
- `已完成任务 Tx.x / 继续处理下一个未完成任务` 这类可由 `tasks.md` 推导的信息。
- Conclusion 与 Resume 重复的长段文字。
- 完整命令输出、全量 diff、所有修改文件列表。
- 为了“恢复上下文”保存的过程叙述；真正重要的设计结论应进入 `project/design` 或 DocsAI。

连续完成多个任务时，只更新 `tasks.md` 和 State；除非出现方向变化、阻塞或最终验证，不追加 progress 条目。

## Evidence Boundary

SDD 只保存能定位任务状态和验证入口的核心摘要，不保存完整命令输出或全量 diff。

推荐写入：

- **验证摘要**：命令、结果、关键结论。
- **核心影响面**：必要时记录改变系统行为、事实源、工作流门禁、公共接口或验证方式的区域。
- **追溯入口**：commit id、必要 artifact 路径或重要对话摘要引用。

不推荐写入：

- 每个临时命令、每次文件读取、完整终端输出。
- 所有被修改文件的完整列表。
- 同步副本或自动生成文件的逐项变化。
- 与最终设计无关的中间过程噪音。

## Worktree Record

当 SDD 任务使用、建议或明确跳过 git worktree 时，`progress.md` 的 State 或少量 Decision 应记录最小 worktree 上下文：

| Field | Meaning |
| --- | --- |
| Git Boundary | 本任务实际操作的仓库绝对路径 |
| Worktree | `none` 或 worktree 绝对路径 |
| Branch | 当前分支或建议分支 |
| Baseline Status | 执行前 `git status --short` 的摘要，不复制完整噪音输出 |
| Cleanup Status | `not-created`、`clean-removable`、`dirty-preserve` 或 `removed` |
| Submodule Boundary | 是否涉及 submodule；涉及时说明只读、指针更新或禁止直接改动 |

只读审计、低风险文档修改或用户要求直接在当前工作区执行时，可以记录 `Worktree: none`，但必须说明原因。dirty workspace 不能作为自动清理、覆盖或删除的理由。

## FeatureSpec / BDD Boundary

FeatureSpec 是设计冻结后的功能实现规格；BDD 是其中的行为场景层。SDD `bdd.md` 是兼容和任务摘录入口，不是长期 FeatureSpec 事实源。

规则：

1. 长期功能实现规格优先放在项目级设计文档旁，命名为 `<设计文档名>.FeatureSpec.md`。
2. 小型单点设计可以在设计文档中使用 `## FeatureSpec` 章节。
3. 子 SDD `bdd.md` 可以只摘录本任务实际执行的行为场景，并用 `Source:` 链接回 FeatureSpec。
4. 如果设计旁 FeatureSpec 已经完整，子 SDD 可以只列 `Executed features` / `Executed scenarios`，不复制全文。
4. 文档治理、目录整理、skill/rule 调整、研究任务默认可以 `Required: false`，说明原因即可。
5. validate 不应要求每个 SDD 都有多条 Scenario；有有效 FeatureSpec Source 引用或 not-required reason 即可。

## Design Reference Boundary

项目级 `design/` 是长期共享设计事实源。项目子 SDD 默认引用项目级设计路径，只在自身 `design/` 保存任务特定差异、局部裁决或一次性对照材料。

规则：

1. `design/INDEX.md` 可以登记 shared design references，不要求把项目级设计复制进子 SDD。
2. `main.md` 可以很短，只写本 SDD 的局部目标、差异和不做什么。
3. 外部一次性研究资料可以在 `notes.md` 或 `design/INDEX.md` 引用；只有会长期成为当前设计事实源时才复制到项目级 `design/`。
4. `design-import` 是保留历史材料的手动工具，不是创建子 SDD 的默认步骤。
5. `validate` 不应把“只引用 project/design”视为设计缺失；它只应提醒真正空壳、模板残留或无法恢复的设计入口。

## Key Files Boundary

`Key Files` 不是 git diff 文件清单。只有改变系统行为、长期事实源、AI 工作流门禁、公共接口或验证方式的文件才应作为核心文件记录。超过 8 个文件时，优先记录 `Key Areas` 和 commit id。
