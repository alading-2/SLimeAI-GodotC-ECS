# SDD 系统详解

## 当前定位

SDD 是 SlimeAI 的轻量任务状态系统，不是完整工作日志，也不是设计文档仓库。

一句话定位：

```text
SDD 只记录任务做到哪、下一步是什么、是否阻塞、验证入口在哪里。
```

长期事实源分别放在：

- `project/design/`：设计结论。
- `DocsAI/`：当前框架使用说明。
- `tasks.md`：任务完成状态。
- Validation artifact / logctl / build 输出：行为证据。
- git：文件变化和 diff。

## 为什么要精简

PRJ-0002 后期 SDD 已经明显膨胀：

| 样本 | 行数 | 主要问题 |
| --- | --- | --- |
| SDD-0040 `progress.md` | 282 | task command、长验证摘要、重复 Resume |
| SDD-0031 `progress.md` | 218 | RED/GREEN 证据过长，任务完成流水账 |
| SDD-0024 `progress.md` | 210 | 每个切片都写长 timeline |
| SDD-0040 `bdd.md` | 100 | 设计要求复述过多 |
| PRJ-0002 `Core/progress.md` | 476 | 项目状态、历史过程和子 SDD 信息混杂 |

这些内容大多不能帮助 AI 更快恢复，反而让 AI 读入口时先被过程噪音淹没。

## 与 OpenSpec 的关系

SDD 参考 OpenSpec 的轻量 change 思想：proposal / tasks / design 按需存在，完成状态主要看 tasks，不把每个操作都写成日志。

SlimeAI 比 OpenSpec 多保留少量内容：

- 当前 SDD / 当前任务。
- blocker。
- 下一步。
- 最小验证入口。

除此之外，SDD 应尽量回到轻量模型。

## 文件边界

| 文件 | 职责 |
| --- | --- |
| `README.md` | 入口卡：状态、目标一句话、当前任务、下一步、关键链接。 |
| `sdd.json` | CLI metadata。 |
| `tasks.md` | 唯一任务进度事实源。 |
| `progress.md` | 状态面板：current / next / blocker / 少量 decision / validation summary。 |
| `design/INDEX.md` | shared design refs 或本 SDD 局部设计引用。 |
| `design/main.md` | 可选，只写本 SDD 独有差异。 |
| `bdd.md` | FeatureSpec 行为场景摘录、Source 引用或 not required reason。 |
| `notes.md` | 可选参考和开放问题。 |
| `artifacts/` | 必须留档且没有稳定外部路径的证据。 |

## progress.md 新规则

默认格式：

```markdown
# Progress

## State

- Status: active
- Current: T2.1
- Next: ...
- Blocker: none

## Decisions

- 2026-06-12: ...

## Validation

- pending
```

不再默认写：

- P001/P002 timeline。
- 每个 task done 记录。
- `Context / Conclusion / Evidence / Impact / Resume` 五段式。
- 完整命令输出。
- 修改文件列表。
- “继续处理下一个未完成任务”。

只有方向改变、阻塞、用户裁决或最终验证时，才值得写入 progress。

## FeatureSpec / BDD 新规则

FeatureSpec 是设计冻结后的功能实现规格，负责描述功能、行为、实现指引和 TDD 交接。BDD 收缩为 FeatureSpec 中的行为场景层，不再承担完整实现规格职责。

新规则：

- 长期功能事实源优先使用设计文档旁的 `.FeatureSpec.md`。
- 子 SDD `bdd.md` 只摘录本任务要执行的 FeatureSpec 行为场景，或链接 FeatureSpec。
- 非行为任务可以 `Required: false`。
- 不要求每个 SDD 都写 Given/When/Then。
- TDD 和 Code Review 优先读取 FeatureSpec；SDD `bdd.md` 只说明本轮执行范围。

## project 容器边界

项目级文件也要分工：

| 文件 | 职责 |
| --- | --- |
| `README.md` | 项目入口，最多保留少量必读链接。 |
| `Core/roadmap.md` | 设计到 SDD 的执行路线和下一步。 |
| `Core/progress.md` | 当前项目状态、跨 SDD blocker、少量阶段裁决。 |
| `design/INDEX.md` | 设计事实源索引，不追踪执行进度。 |

避免 README、roadmap、progress、design index 四处重复同一份状态。

## validate 边界

`python3 Workspace/SDD/sdd.py validate` 只检查：

- 必要文件和 metadata 是否存在。
- 状态是否合法。
- done 是否仍有未完成任务。
- blocked 是否有 blocker。
- `bdd.md` 是否有场景、FeatureSpec / Source 引用或 not-required reason。
- 是否存在明显模板残留或空壳完成。

它不证明业务实现正确，也不应该鼓励写更多过程记录。

代码、数据、Godot、skill 或文档变更仍要跑各自验证。
