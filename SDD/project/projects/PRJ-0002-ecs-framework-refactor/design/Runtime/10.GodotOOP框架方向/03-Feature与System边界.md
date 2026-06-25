# Feature 与 System 边界

## 结论

Feature 和 System 都保留，但职责不同：

```text
Feature = 玩法能力 owner
System  = 跨对象/跨功能的运行时协调器
```

Feature 解决“这个功能包含什么、如何组合、如何验证”；System 解决“多个对象之间怎么协调、怎么查询、怎么批处理、怎么调度”。

## Feature 负责什么

Feature 是 SlimeAIFramework 的主要业务边界。旧 `Capability` 可以逐步迁移为 Feature。

Feature 应明确：

- 需要哪些 Component。
- 是否需要 System。
- 定义哪些 Event。
- 哪些字段进入 Data。
- 哪些字段留在 Component。
- DataBinding 规则。
- 日志、验证场景和 DocsAI 入口。

示例：

```text
Feature: Damage
  Components:
    DamageReceiverComponent
    HurtboxComponent
  Systems:
    DamageSystem 或 DamageService
  Events:
    DamageRequested
    DamageApplied
  Data:
    CurrentHp
    MaxHp
    DamageReduction
    IsDead
  Component local:
    hit flash timer
    last hit VFX state
```

## System 负责什么

System 名称保留，但不再表示传统 ECS System。它是 SlimeAIFramework runtime service / coordinator。

适合 System：

- 跨多个 Object 查询或批处理。
- 需要统一调度顺序。
- 输入、计时器、对象池、资源加载、日志等 runtime 工具。
- Damage、TargetQuery、Spawn 等需要全局规则的玩法协调。
- 需要可启停、preflight、diagnostics、manifest 的服务。

不适合 System：

- 单个 Component 内部可以直接完成的局部逻辑。
- 只为“看起来像 ECS”而抽出来的转发层。
- UI 动画、节点表现、一次性缓存。
- 没有跨对象协作需求的简单功能。

还要补一个这轮新确认：System 在 SlimeAIFramework 里的语义，确实更接近“后端/应用服务层”——负责协调、规则、查询和运行时基础设施；它不是前端交互层，也不应该接管 UI 节点自己的表现逻辑。

## Feature 与 System 的关系

```text
Feature owns system usage.
System should not own feature meaning.
```

也就是说，System 可以提供能力，但 Feature 决定某个玩法怎么使用它。

示例：

```text
TargetQuerySystem:
  提供目标查询 API、候选来源、排序、diagnostics。

AbilityFeature:
  决定某个 Ability 用哪种 TargetQueryProfile、哪些 Data 字段参与范围和筛选。
```

## Data 进入条件由 Feature 决定

Data 不应由底层 Runtime 自动收集所有字段。每个 Feature 应声明自己的 Data 进入条件：

```text
FeatureDataContract:
  Fields:
    CurrentHp:
      authority: Data
      binder: HealthComponent.SetCurrentHp
      modifiers: no
      observedBy: UI, TargetQuery, Test
    MoveSpeed:
      authority: Data
      binder: MovementComponent.SetMoveSpeed
      modifiers: numeric
      observedBy: Movement, Buff, Debug
```

这能保留“填表格就能传数据”，同时防止 Data 又变回所有字段的垃圾桶。

## System 启停

System 可以受控启停，但必须有清晰规则：

- 启动前由 profile 决定启用哪些 Feature / System。
- 运行中启停必须进入 command buffer 或明确 phase。
- 停用 System 时必须说明对订阅、Timer、DataBinding、对象池对象的影响。
- 失败时输出 blocked reason 和 structured diagnostics。

## 不推荐方向

- 不推荐把 Feature 退化为文件夹名，真正逻辑都塞进 System。
- 不推荐把 System 改名成 Service 后丢失已有 manifest/preflight/diagnostics 资产。
- 不推荐在 Feature 之间直接互调 Component 实现。
- 不推荐用 Data 替代所有 Feature 与 System 的 API 边界。

