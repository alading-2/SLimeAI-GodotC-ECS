# Log 与 Debug 边界裁决

> 状态：current
> 日期：2026-06-11
> 来源：基于 PRJ-0002 Log 第三部分与 SystemAgent Debug workflow 的联合复盘。
> 作用：补足 2026-06-08 主裁决中未系统展开的 Log / Debug / Validation / Test 边界，避免再次把“analyzer 完成”误说成“整个 Log 已完成”。

## 与 2026-06-08 主裁决的关系

`2026-06-08-SystemAgent工作流内化与核心优化裁决.md` **不需要整体重构**。

它当前仍然承担有效的主裁决职责：
- 不做外层 AI CLI manager
- 不魔改 Warp
- SystemAgent 聚焦项目内 workflow、SDD、skill、hook、subagent、会话记录、验证和复盘协议

这份新文档不是推翻旧文档，而是补一层当时没有展开清楚的边界：

```text
SystemAgent 是控制面（control plane）
Log / Validation / Test 是证据面（evidence plane）
```

## 核心裁决

### D1：Log 不属于 SystemAgent 控制面本体

裁决：Adopt。

原因：
- Log 不是 Route、Actor、Rule、Gate、Skill。
- Log 不负责任务路由、角色激活、门禁判定或复盘策略。
- Log 的职责是 Observation：记录运行事实、提供 artifact、提供离线分析入口。

因此，不能把 Log 写成“SystemAgent 本体的一部分”。

### D2：Log 属于 AI-first 工程层的证据面，并且是 SystemAgent 的关键依赖

裁决：Adopt。

推荐表述：

```text
SystemAgent 负责 AI 的执行流程与审查门；
Log / Validation / Test 负责为这些流程提供可复查、可分析、可判定的运行时证据。
```

原因：
- 没有 Log / Validation / Test，DebugFix、Reviewer、Verifier、Retrospective 都只能依赖弱信号或自然语言猜测。
- AI-first 的目标不是让 AI 更会猜，而是让 AI 更少猜。

### D3：Log 不能再被表述为“已整体完成”

裁决：Adopt。

必须固定三层状态：

| 层 | 状态 |
| --- | --- |
| 记录层 | 第一版已落地 |
| 离线整理层 | 第一版已落地 |
| 源码调用点语义化层 | 未完成，属于独立大阶段 |

这条裁决必须进入：
- Logger owner 文档
- SystemAgent Debug 说明文档
- SystemAgent 完成度分析文档

### D4：Log 与 Debug 需要分成两份单独文档

裁决：Adopt。

原因：
- Logger 文档要解释“事实如何被记录、整理、收口”。
- Debug 文档要解释“workflow 如何消费这些事实并给出判定”。
- 如果合在一份大文档里，控制面与证据面会再次混淆。

## 产物更新策略

### Logger 侧

新增一份 Observation 文档，说明：
- Log 的 AI-first 定位
- 三层模型
- 当前完成边界
- 与 SystemAgent / Validation / Test 的关系

### SystemAgent 侧

新增一份 Debug 证据链文档，说明：
- DebugFix / Debugger / ReviewGates
- Log / Validation / Test / artifact / SDD 的协作关系
- 为什么 `no-failure-observed` 不能算 `passed`

## 风险提示

如果不补这层边界，会持续出现三类误判：

1. **analyzer 完成 = Log 完成**
2. **Log = SystemAgent 本体**
3. **clean stdout / exit code 0 = 通过**

这三类误判都会直接削弱 AI-first 的核心目标：让 AI 基于证据，而不是基于猜测。
