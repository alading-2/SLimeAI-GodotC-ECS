# SDD 精简设计

> 来源：用户指出 PRJ-0002 SDD、BDD、project 索引和 CLI 记录过度冗余后的 SystemAgent 深度分析
> 日期：2026-06-12
> 优先级：P0

## 用户原始问题

用户指出 `SDD/project/projects/PRJ-0002-ecs-framework-refactor/sdds` 下的 SDD 过度详细：`progress.md` 记录了大量过程、命令、结论、修改文件和重复 Resume；设计文档已经在项目 `design/` 中，不需要子 SDD 再复制；BDD 写在 SDD 里常常没用，应该更靠近设计文档并实时更新；project README、roadmap、progress、design index 也有冗余。用户要求参考 OpenSpec 的轻量思想，在其基础上小改，而不是继续用当前冗余思维分析。

## 真实问题

用户判断成立，而且问题比“progress 太长”更大。

PRJ-0002 样本显示：

- 子 SDD `progress.md` 最大 282 行，多个超过 150 行。
- 项目级 `Core/progress.md` 已到 476 行，和 README / roadmap / 子 SDD progress 互相重复。
- `bdd.md` 最大 100 行，但很多 Scenario 是设计要求的再表达，不是可直接执行的验收例子。
- `README.md` 的 Reading Order 经常变成几十行路径清单，AI 读入口反而更难知道下一步。
- `design/INDEX.md` 和 roadmap 都在追踪设计文档，存在双重索引。

这说明当前 SDD 不只是“记录太多”，而是职责边界错了：它把设计事实源、任务状态、验证证据、工作日志、索引和恢复提示混在一起。

## 核心裁决

SDD 应该退回到 OpenSpec-like 的轻量变更协议，再加 SlimeAI 必需的少量任务状态。

新的定位：

```text
SDD = change/task state wrapper
project/design = 长期设计事实源
DocsAI = 当前框架使用事实源
tests/artifacts/logctl = 行为证据事实源
git = diff / 文件变化事实源
```

SDD 不再负责保存“重要信息”。这个功能可以大幅砍掉。

原因是：真正重要的信息已经有更好的位置：

- 设计阶段的重要结论写在 `project/design/` 或 DocsAI。
- 行为预期写在设计旁的 BDD / 验收小节。
- 任务进度写在 `tasks.md` checkbox。
- 验证结果写在 artifact / logctl / 最终简短验证摘要。
- 改了哪些文件看 git。
- 为什么改成这样看设计文档和 commit。

`progress.md` 只保留“下一次打开这个 SDD 时需要马上知道什么”，而不是保存过程。

## 与 OpenSpec 的取舍

OpenSpec 的可取之处：

- change 很轻：proposal / tasks / specs / design 按需存在。
- tasks 是执行事实源，不把每步过程写成日志。
- archive 后回到 specs，而不是永久维护一堆任务过程。

OpenSpec 对 SlimeAI 不够的地方：

- SlimeAI 有多个长期项目容器，需要知道当前 active / blocked SDD。
- Godot / DataOS / Log artifact 验证链路比普通代码库更复杂，需要保存最小验证入口。
- AI 需要快速知道“下一步是不是 blocked”，不能只靠人记忆。

所以 SlimeAI SDD 不照搬 OpenSpec，但应该比现在薄很多：

```text
OpenSpec proposal/tasks/spec思想
+ SlimeAI status/blocker/current task
+ 最小验证入口
- timeline
- 设计快照复制
- 长篇恢复笔记
- 双重/三重索引
```

## 文件边界重定

| 文件 | 新边界 |
| --- | --- |
| `README.md` | 只做入口卡：状态、目标一句话、当前任务、下一步、关键设计链接。 |
| `sdd.json` | 机器 metadata：id、status、progress、shared design refs。 |
| `tasks.md` | 唯一任务进度事实源。checkbox 就是完成状态，不再复制到 progress。 |
| `progress.md` | 只保留状态面板：current / next / blocker / final validation summary。默认不写 timeline。 |
| `design/INDEX.md` | 可选引用索引；项目子 SDD 默认只列 shared design refs。 |
| `design/main.md` | 可选，仅写本 SDD 独有差异。多数子 SDD 可以没有实质正文。 |
| `bdd.md` | 降级为任务摘录或链接。长期 BDD 应靠近设计文档。非行为任务默认 not required。 |
| `notes.md` | 可选参考链接和开放问题，不保存过程分析。 |
| `artifacts/` | 只放必须留档且外部没有稳定路径的证据。 |

## progress.md 新边界

`progress.md` 可以更薄，默认结构：

```markdown
# Progress

## State

- Status: active
- Current: T2.1
- Next: 在 DataRuntimeTestScene 中为 range policy 添加 RED check
- Blocker: none

## Decisions

- 2026-06-12: Data 试点走 Godot scene + ValidationSession，不新增脱离 Godot 的测试框架。

## Validation

- pending
```

完成时：

```markdown
# Progress

## State

- Status: done
- Current: complete
- Next: none
- Blocker: none

## Decisions

- 2026-06-12: Data 试点走 Godot scene + ValidationSession，不新增脱离 Godot 的测试框架。

## Validation

- `dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly`: passed
- `Tools/run-godot-scene.sh run ...DataRuntimeTestScene.tscn`: passed, artifact `.ai-temp/.../data-runtime.json`
```

不再默认写：

- `P001 / P002 / P003` 时间线。
- 每个 task done 记录。
- `Context / Conclusion / Evidence / Impact / Resume` 五段式样板。
- 完整命令输出。
- 修改文件列表。
- “继续处理下一个未完成任务”。

如果中途出现真正改变方向的事实，可以写一条 Decision。没有就不写。

## BDD 新边界

当前 SDD 的 `bdd.md` 问题是：位置和用途都容易错。

BDD 有意义的前提：

- 它是具体例子，不是设计文档的复述。
- 它能指导测试 check。
- 它跟设计同步更新。
- 它有明确 artifact / testRef 或至少能映射到验证场景。

新的建议：

1. 长期行为场景放在项目 `design/` 对应文档旁边，或直接作为设计文档的“行为验收”小节。
2. 子 SDD 的 `bdd.md` 只摘录本任务要执行的少量场景，并链接回设计源。
3. 文档治理、目录整理、skill 调整这类非用户行为任务，`bdd.md` 默认 `Required: false`，写一句原因即可。
4. 不再要求每个 SDD 都有多条 Given/When/Then。

示例：

```markdown
# BDD

Required: true
Source: ../../design/Runtime/Data/Data测试试点设计.md#行为验收

## Scenario: Range policy rejects out-of-range write

Given CurrentHp range is 0..100 and current value is 50
When Data tries to set CurrentHp to 120
Then TrySet returns false
And CurrentHp remains 50
And artifact reasonCode is range_violation

testRef: Src/ECS/Runtime/Data/Tests/DataOS/DataRuntimeTestScene.tscn
```

如果设计旁已经维护完整 BDD，子 SDD 可以只写：

```markdown
# BDD

Required: true
Source: ../../design/.../Data测试试点设计.md#行为验收
Executed scenarios: range-policy-rejects-out-of-range-write
```

## project 容器精简

项目级 SDD 当前也冗余。建议：

- `README.md`：项目入口，只保留当前状态、当前 SDD、3-8 条必读链接。不要 50 条 Reading Order。
- `Core/roadmap.md`：保留唯一路线图，列设计文档到 SDD 的映射和下一步。
- `Core/progress.md`：可砍到项目状态面板，只记录跨 SDD 的阶段裁决和 blocker；不记录每个子 SDD 的完成过程。
- `design/INDEX.md`：保留设计目录索引，但不和 roadmap 重复追踪进度；只说明文档角色和 current/historical。

换句话说：

```text
README = 入口
roadmap = 要做什么 / 做到哪
progress = 当前项目状态和跨 SDD blocker
design/INDEX = 有哪些设计事实源
```

每个文件只承担一个问题。

## CLI / validate 边界

SDD CLI 不应该鼓励冗余。

调整方向：

- `task done` 只改 `tasks.md` 和 metadata，不追加 progress timeline。
- `note` 保留，但作为手动工具，不作为每步执行默认动作。
- `done` 写最终 validation summary，不生成长 Conclusion/Resume。
- `validate` 只检查结构、状态一致性、任务完成度、blocked/done 必要信息。
- `validate` 不检查“记录够不够详细”，只检查是否空壳、是否状态矛盾。
- `validate` 不能被描述成业务验证成功。

可以删除或降级的检查：

- progress 很多但 State 仍弱：新模型下 progress 本来就应该少。
- validation 摘要必须很详细：只需要命令 + result + artifact/ref。
- BDD required=true 必须有多条 Scenario：如果引用设计旁 BDD，也应通过。
- design/INDEX 必须有 main/current：项目子 SDD 只引用 shared design refs 时不应 warning。

## 实施路径

| 阶段 | 内容 |
| --- | --- |
| Phase 1 | 先更新文档、skill、规则，停止新增冗余 SDD。 |
| Phase 2 | 改 SDD 模板：progress 默认状态面板，BDD 默认可引用设计旁来源。 |
| Phase 3 | 改 CLI：task/status 写操作不再追加 timeline 样板。 |
| Phase 4 | 改 validate：围绕空壳、矛盾、done/blocked 必备信息，不鼓励写更多。 |
| Phase 5 | 选择性压缩仍 active/blocked 的膨胀 SDD；历史 done 默认不迁移。 |

## 不做什么

- 不把当前 SDD 继续包装成重型恢复文档然后保留长进度。
- 不再默认记录“关键过程信息”；除非它改变方向、阻塞或验证结论。
- 不要求子 SDD 复制 project/design。
- 不要求每个 SDD 写完整 BDD。
- 不用 SDD validate 证明代码正确。
- 不批量重写历史 done SDD。
