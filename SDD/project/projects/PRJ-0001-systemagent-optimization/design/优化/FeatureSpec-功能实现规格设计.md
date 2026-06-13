# FeatureSpec 功能实现规格设计

> 来源：用户指出 BDD 当前效果弱，真正需要的是设计冻结后指导 AI 实现的“功能驱动 / 功能 + 代码”规格。
> 日期：2026-06-13
> 状态：proposed
> 优先级：P0

## 用户原始问题

用户认为当前 BDD 没有做好，实际效果可有可无；但从行为驱动和功能驱动角度看，BDD 本应非常重要。用户进一步裁决：现有语义不必强行叫 BDD，`FeatureSpec` 可以接受；相比把内容塞进设计文档章节，更推荐新建同目录旁路文档，命名为 `.FeatureSpec.md`，避免设计文档过长。设计文档主要负责确定思路、方向和需要考虑的问题；FeatureSpec 负责详细描述要做的功能以及怎么做，也就是功能 + 代码，指导 AI 实现设计文档。

## 真实问题

当前问题不是“BDD 这个词是否正确”，而是 SlimeAI 缺少一个稳定的设计到实现翻译层。

已有文档已经把 SDD 精简成任务状态容器，把 TDD 定义为行为标准答案和 RED/GREEN 证据。但中间仍有断点：

- 设计文档讲清了问题、方向、取舍和边界，却不一定把功能拆成 AI 可执行的实现单元。
- 旧 `bdd.md` 常常只写几条 Given/When/Then，能通过 SDD validate，却不能指导代码落点、owner、数据、事件、日志和 artifact。
- TDD 需要 `expectedInputs / expectedObservations / passCriteria / failCriteria / artifactPath`，但这些字段不应由测试实现时临场发明。
- Code Review 需要逐条对照功能目标，但现有目标可能散在用户聊天、设计文档、SDD tasks 和 BDD 摘录中。

因此，BDD 作为“行为例子”仍有价值，但它不是完整答案。真正缺的是 **FeatureSpec：设计冻结后的功能实现规格**。

## 核心裁决

采用 `FeatureSpec` 作为正式名称。

```text
Design Document
  解决：为什么做、方向是什么、问题和取舍是什么

FeatureSpec
  解决：做哪些功能、行为是什么、代码怎么落、验证怎么接

TDD
  解决：怎样用 RED/GREEN 和 artifact 证明行为实现

SDD
  解决：任务做到哪、下一步是什么、是否阻塞、验证入口在哪里
```

BDD 不再作为独立大环节名称使用。后续将 BDD 收缩为 FeatureSpec 中的一个层次：**行为场景 / 行为验收例子**。

## 命名和位置

默认新建旁路文档，放在设计文档同目录。

推荐命名：

```text
<设计文档名>.FeatureSpec.md
```

示例：

```text
Data系统优化.md
Data系统优化.FeatureSpec.md
```

如果一个目录下已有主设计入口，也可以使用：

```text
FeatureSpec.md
<子主题>.FeatureSpec.md
```

默认不把完整 FeatureSpec 写进 SDD 子目录。SDD 子任务只引用或摘录当前任务需要执行的功能项。

允许例外：

- 很小的单点设计可以把 FeatureSpec 作为设计文档中的 `## FeatureSpec` 章节。
- 长期项目、跨模块功能、AI 执行风险高的任务应使用独立 `.FeatureSpec.md`。
- 历史 `bdd.md` 暂时保留，但只作为 SDD 兼容入口、摘录或引用，不作为长期功能事实源。

## 边界

### 设计文档负责什么

- 用户原始问题。
- 真实问题和根因。
- 目标方向和方案取舍。
- 架构边界、风险和不做什么。
- 关键设计决策。

设计文档可以提到功能，但不必把每个功能拆到实现粒度。

### FeatureSpec 负责什么

- 功能列表和优先级。
- 每个功能的目标行为。
- 行为场景，即原 BDD 的 Given/When/Then 或等价描述。
- 实现指引：owner、关键模块、接口、数据、事件、日志、artifact、禁止路径。
- TDD 交接：expectedInputs、expectedObservations、passCriteria、failCriteria、artifactPath。
- Code Review 对照目标：每个功能项必须是可验证陈述。

FeatureSpec 可以包含代码建议，但不直接替代实现代码。它写的是实现方向、契约和关键落点，不写完整生产代码。

### TDD 负责什么

TDD 不再临场推导行为。它消费 FeatureSpec，把行为和实现指引转成可失败、可通过、可复查的验证。

### SDD 负责什么

SDD 不拥有 FeatureSpec。它只记录：

- 本任务引用哪个 FeatureSpec。
- 本轮执行哪些 feature item。
- `bdd.md` 中必要的行为摘录或 `Source:`。
- 当前状态、阻塞和最终验证摘要。

## 推荐结构

```markdown
# <主题> FeatureSpec

## Source Design

- `<设计文档路径>`

## Feature List

| ID | Feature | Priority | Status | Notes |
| --- | --- | --- | --- | --- |
| FS-1 | ... | P0 | planned | ... |

## FS-1: <功能名>

### Goal

这个功能要让系统具备什么能力。

### Behavior

描述可观察行为。可以使用自然语言，也可以使用 Given / When / Then。

### Implementation Guidance

- Owner:
- Key files / areas:
- Data keys / schema:
- Events / commands:
- Public API:
- Log / Validation artifact:
- Constraints:
- Forbidden:

### TDD Handoff

- expectedInputs:
- expectedObservations:
- passCriteria:
- failCriteria:
- artifactPath:

### Review Checklist

- ...
```

## 执行时机

FeatureSpec 在设计文档冻结后、TDD 之前生成。

```text
DeepThink / DesignCritic
  -> Design Document
  -> FeatureSpec
  -> TestDesigner / TDD
  -> SDD tasks execution
  -> Code Review / Verifier / Retrospective
```

如果设计仍在反复变化，不应急着写完整 FeatureSpec。可以先写草稿，但实现前必须以当前设计文档为准刷新 FeatureSpec。

## 与旧 BDD 的迁移策略

不批量重写历史 SDD。

新任务采用：

- 长期功能事实源：`.FeatureSpec.md`
- SDD `bdd.md`：`Source:` + `Executed features / scenarios`
- TDD：引用 FeatureSpec 的 feature ID 和 behavior item

旧任务保留 `bdd.md`。只有继续执行、复盘或迁移时，才按需提取到 FeatureSpec。

## 对现有文档和工具的影响

需要同步更新：

- `Workspace/SystemAgent/Docs/06-SDD系统详解.md`
- `Workspace/SystemAgent/Docs/07-TDD与测试系统.md`
- `Workspace/SystemAgent/Docs/11-FeatureSpec功能实现规格.md`
- `Workspace/SystemAgent/Rules/DesignDocument.md`
- `Workspace/SystemAgent/Rules/TDD.md`
- `Workspace/SystemAgent/Rules/ReviewGates.md`
- `Workspace/SystemAgent/Actors/TestDesigner.md`
- `Workspace/SDD/docs/SDDFormat.md`
- `Workspace/SDD/docs/ValidationRules.md`
- `.ai-config/skills/*` 相关入口

后续实现项：

- SDD 模板仍默认生成 `bdd.md`，可以继续保留兼容，但内容应改成 FeatureSpec 引用优先。
- SDD validate 当前主要检查 `Scenario:` 或 `Source`，后续应允许 `.FeatureSpec.md` Source 和 `Executed features`。
- Code Review 目标提取应优先读取 FeatureSpec，其次读取 SDD `bdd.md` 摘录。

## 不做什么

- 不把 FeatureSpec 写成第二份架构设计。
- 不把完整代码实现提前写进文档。
- 不强制每个小修都有独立 FeatureSpec。
- 不让 SDD 子目录成为长期 FeatureSpec 事实源。
- 不批量迁移历史 done SDD。
