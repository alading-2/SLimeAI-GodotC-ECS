# FeatureSpec 功能实现规格

> 状态：current
> 作用：定义设计文档冻结后、TDD 和 SDD 执行前，AI 用来理解“要做什么功能、怎么落到代码、怎么验证”的功能实现规格。

## 一句话定位

FeatureSpec 是 **设计到实现的翻译层**。

```text
Design Document 讲为什么和方向
FeatureSpec 讲功能和代码落点
TDD 证明行为实现
SDD 记录任务状态
```

旧 BDD 不再承担完整实现规格职责。BDD 只保留为 FeatureSpec 中的“行为场景 / 行为验收例子”。

## 为什么需要它

设计文档适合记录用户原始问题、真实问题、方向、取舍和边界，但它不应该无限膨胀到每个功能的实现细节。

AI 实现时还需要一层更直接的规格：

- 要做哪些 feature。
- 每个 feature 的目标行为是什么。
- 应改哪些 owner、模块、接口、数据、事件、日志和 artifact。
- 哪些路径禁止走。
- TDD 应该验证什么。
- Code Review 应该逐条检查什么。

如果没有这一层，AI 容易从设计文档直接跳到实现，TDD 也容易在写测试时临场发明验收标准。

## 命名和位置

默认使用独立旁路文档，放在设计文档同目录：

```text
<设计文档名>.FeatureSpec.md
```

示例：

```text
Data系统优化.md
Data系统优化.FeatureSpec.md
```

小型单点设计可以直接在设计文档里写 `## FeatureSpec` 章节。中大型功能、跨模块改造和需要 AI 长任务执行的设计，默认新建 `.FeatureSpec.md`，避免设计文档过长。

SDD 子目录不保存完整 FeatureSpec。SDD 只引用或摘录本任务要执行的功能项。

## 文档边界

| Artifact | 职责 |
| --- | --- |
| Design Document | 用户原始问题、真实问题、方向、取舍、架构边界、不做什么。 |
| FeatureSpec | 功能列表、行为、实现指引、代码落点、TDD 交接和 review checklist。 |
| TDD | 把 FeatureSpec 转为 RED/GREEN check、scene、validator 和 artifact。 |
| SDD | 记录当前任务状态、引用 FeatureSpec、摘录执行场景和验证摘要。 |
| Validation artifact | 证明行为是否真的通过。 |

FeatureSpec 可以包含代码建议，但它写的是实现方向、契约和关键落点，不提前写完整生产代码。

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

可观察行为。可以使用自然语言，也可以使用 Given / When / Then。

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

## 执行顺序

```text
DeepThink / DesignCritic
  -> Design Document
  -> FeatureSpec
  -> TestDesigner / TDD
  -> SDD tasks execution
  -> Code Review / Verifier / Retrospective
```

设计还没冻结时，可以写 FeatureSpec 草稿，但不要把草稿当实现授权。实现前必须确认 FeatureSpec 对齐当前设计文档。

## 与 BDD 的关系

BDD 是行为场景，不是完整功能实现规格。

保留 Given / When / Then 的用途：

- 描述关键行为例子。
- 对齐 TDD check。
- 给 Reviewer 一个行为目标。

不要用 BDD 承载：

- 完整设计取舍。
- 长篇实现方案。
- SDD 进度。
- 完整验证输出。

## 与 SDD 的关系

SDD `bdd.md` 保留为兼容和摘录入口。

推荐写法：

```markdown
# BDD

Required: true
Source: ../../design/.../Data系统优化.FeatureSpec.md#fs-1-range-policy
Executed features: FS-1
Executed scenarios: range-policy-rejects-out-of-range-write
```

非行为任务仍可：

```markdown
# BDD

Required: false
Reason: 文档索引治理，不改变用户可观察行为。
```

## 与 TDD 的关系

TestDesigner 优先读取 FeatureSpec 的 `TDD Handoff`。如果缺失，先补 FeatureSpec 或输出明确默认假设，再写测试。

TDD 不能只凭设计文档临场推导标准答案；设计文档说明方向，FeatureSpec 才是测试标准答案的直接来源。

## 与 Code Review 的关系

Code Review 优先从 FeatureSpec 提取审查目标：

- Feature item。
- Behavior。
- Implementation Guidance 中的约束。
- Review Checklist。

SDD `bdd.md` 和 tasks 只提供本轮执行范围，不替代 FeatureSpec。

## 不做什么

- 不强制每个小修都新建 FeatureSpec。
- 不把 FeatureSpec 放进 SDD 子目录作为长期事实源。
- 不把 FeatureSpec 写成第二份完整架构设计。
- 不提前写完整生产代码。
- 不批量迁移历史 done SDD。
