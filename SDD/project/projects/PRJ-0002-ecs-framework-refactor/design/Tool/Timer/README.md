# Timer 工具设计包

> 更新：2026-06-01
> 状态：current design package
> 入口：`README.md`
> 裁决：Timer 当前功能对人基本够用，但对 AI-first ECS 还缺少 owner 生命周期、typed purpose/tag、文档一致性、结构化观测和验证门禁。优化方向是强化契约和可观察性，不是推翻 `TimerManager`。

## 0. 本设计包回答什么

这份设计包回答当前 Timer 相关的核心问题：

- `Src/ECS/Tools/Timer` 是否需要完善。
- 当前 `TimerManager` 是否还适合作为统一计时入口。
- 给人用够不够，给 AI 用哪里有问题。
- 是否要参考 Godot Timer、Bevy Timer、Unity Time/Coroutine 或其它框架。
- 后续代码优化应先动哪里，哪些不该做。

结论先写清楚：**Timer 要完善，但不要重写。当前 API 已支撑 gameplay；AI-first 缺口集中在生命周期归属、取消规则、标签类型安全、文档漂移和 observation。**

## 1. 文件结构

| File | Role | 说明 |
| --- | --- | --- |
| `README.md` | design-index | 本文件。给出总裁决、阅读顺序、边界和完成定义。 |
| `01-现状证据与AI-first裁决.md` | research-decision | 当前 `TimerManager/GameTimer`、调用点、文档漂移和外部资料对照。 |
| `02-目标架构与优化路线.md` | architecture-roadmap | 定义 owner handle、purpose/tag、clock/pause、observation 和路线。 |
| `03-调用点迁移与验证计划.md` | migration-test-plan | 调用点清单、迁移顺序、grep gate、测试和场景验证。 |

## 2. 总裁决

采用 **AI-first Timer Lifecycle Tool**：

```text
TimerManager
  -> 继续作为统一计时器创建、驱动、暂停和批量管理入口

GameTimer
  -> 继续作为池化 timer 实例，但补足 owner/purpose/状态可观测语义

TimerHandle / TimerOwner / TimerPurpose
  -> 让 AI 能判断定时器归谁、为什么存在、何时必须取消

TimerObservation
  -> 输出 active count、owner、purpose、remaining、pause source、cancel reason

DocsAI / Validation
  -> 固定 API 事实，禁止文档示例漂移
```

不采用：

- 不恢复每个节点自己 `new Timer()` 或 `GetTree().CreateTimer()` 作为 gameplay 默认做法。
- 不复制 Bevy schedule 或 Unity coroutine 体系。
- 不让 string tag 继续承担唯一批量管理语义。
- 不把 Timer 做成 Ability/Cooldown/Feature 的业务状态源。

## 3. AI-first 原则

| 旧问题 | AI-first 规则 |
| --- | --- |
| 业务只保存 `GameTimer?` 字段 | timer 必须能说明 owner、purpose、创建位置和取消责任。 |
| tag 是自由字符串 | tag/purpose 应收敛到 typed id 或稳定枚举/常量。 |
| 文档 API 与源码不一致 | DocsAI 示例必须直接匹配源码可调用 API。 |
| 只看 active count | observation 应能按 owner/purpose 展开，辅助 AI 查泄漏和未取消。 |
| 取消靠调用点自觉 | 组件注销、系统停止、实体销毁时必须有明确取消规则。 |

## 4. 目标边界

| 模块 | 目标职责 | 禁止职责 |
| --- | --- | --- |
| `TimerManager` | 创建、驱动、暂停、批量操作、统计 | 不承载 cooldown/DoT/AI wait 的业务规则。 |
| `GameTimer` | 单个 timer 状态、回调、进度、取消 | 不知道 Entity 业务含义。 |
| `TimerHandle` | 给调用方持有、取消、查询状态 | 不让池化对象被误用为长期身份。 |
| `TimerOwner` | 标识归属实体、组件、系统或 feature | 不替代 Entity lifecycle。 |
| `TimerPurpose` | 标识用途，如 Cooldown、DoT、SpawnWave | 不使用随意自然语言字符串。 |
| `TimerObservation` | 输出 AI 可读状态和泄漏线索 | 不参与业务决策。 |

## 5. 完成定义

Timer 优化完成不是“还能延迟回调”。

必须同时满足：

- DocsAI Timer 文档与源码 API 完全一致。
- Gameplay 计时默认继续走 `TimerManager`。
- 组件/系统的 timer 有明确取消点。
- string tag 不再是唯一 owner/purpose 表达。
- Observation 能回答当前有哪些 timer、归谁、还剩多久、为何暂停。
- 构建、测试和关键 Godot 场景 smoke 通过。

## 6. 阅读顺序

1. 先读 `01-现状证据与AI-first裁决.md`，确认为什么要完善。
2. 再读 `02-目标架构与优化路线.md`，确认完善到什么形状。
3. 最后读 `03-调用点迁移与验证计划.md`，确认怎么迁移和验证。
