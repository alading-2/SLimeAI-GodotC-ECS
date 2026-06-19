# Data 进入条件与状态分层

> 用户原始问题：[`source-request.md`](./source-request.md)
> 入口：[`README.md`](./README.md)

## 真正的问题

用户指出“很多字段都是 Component 内部用的，集中在一起反而出问题”。这个判断成立。

旧 Data 的错误不是“字段定义集中”，而是把运行时实例值也默认集中。字段定义可以集中，因为它服务 DataOS、validator、AI 解释、文档和生成；运行时状态不应该默认集中，因为 Component、Profile、System 才是 OOP 框架里的功能单元。

所以本页要解决的问题不是“字段放 Data 还是 Component”二选一，而是：

```text
哪些字段值得进入 Data。
哪些字段留给 Component/Profile/System。
Data 里出现的字段到底谁是事实源。
如果 Data 只是投影，别人能不能改它。
```

## Data 进入条件

字段进入 Data 之前至少满足一条强条件，最好满足两条以上：

| 条件 | 示例 |
| --- | --- |
| 多个 Feature / Component / System 需要读写 | `CurrentHp` 被 Damage、UI、AI、TargetQuery、Test 读取 |
| 来自 DataOS 表格或需要表格覆盖 | `MoveSpeed`、`AbilityCooldown`、`BaseAttack` |
| 需要 descriptor 约束和 validator | range、allowed values、enum、resource ref |
| 需要 modifier / computed | `MoveSpeed`、`FinalAttack`、`DamageReduction` |
| 需要 UI / debug / test / AI 稳定观察 | `HpPercent`、`IsDead`、`AbilityCurrentCharges` |
| 需要 save/load、replay 或跨运行期对比 | 等级、经验、装备属性、核心进度 |

不满足这些条件的字段默认不进 Data。

## 字段不进 Data 的正当理由

这些字段默认留在 Component / Profile / System：

| 类型 | 存放位置 | 示例 |
| --- | --- | --- |
| Component local | Component 私有字段 | `_phaseTimer`、`_currentTarget`、`_hitFlashTimer`、`_currentStrategy` |
| 运行时中间值 | Component / 策略类 | `_lastResolvedVelocity`、碰撞法线、动画 blend 临时值 |
| Godot 节点缓存 | Component | `_body`、`Sprite2D` 缓存、绑定节点引用 |
| 系统索引 | System | 恢复系统注册集合、TargetQuery 诊断、对象池空闲列表 |
| 表现状态 | UI / Visual Component | 当前动画播放、hit flash、death animation started |

字段不进 Data 不代表“没有管理”，而是由对应 owner 管理。必要时通过 manifest、typed options、Component preflight 或 owner validator 约束。

## Authority：谁是事实源

`authority` 是本轮 Data 方案最关键的词。

```text
authority = 这个值到底谁说了算。
```

如果不写清楚 authority，就会出现三份状态：

```text
Data 一份。
Component 一份。
System 再算一份。
```

最后 AI、UI、测试和业务代码都不知道哪一份是真的。

## 三种 authority

### Data authoritative

Data 是事实源。其他地方可以缓存镜像，但不能把镜像当真值。

适合：

- `CurrentHp`
- `CurrentMana`
- `BaseHp`
- `FinalHp`
- `MoveSpeed`
- `FinalMoveSpeed`
- `BaseAttack`
- `FinalAttack`
- `DamageReduction`
- `AbilityCurrentCharges`
- `AbilityCooldown`

修改规则：

```text
外部 -> Command / owner API / Service Pipeline -> Data.Set / DataModifier -> DataChanged -> DataBinding/UI/Test
```

Component 可以持有 mirror，但 mirror 只由 DataBinding 或 owner API 更新。

### Component authoritative

Component 是事实源。Data 里如果有同名或相近字段，只是 projection。

适合：

- `AttackComponent._state`
- `EntityMovementComponent._currentStrategy`
- `MovementParams`
- `_moveCompleted`
- `_facingDirection`
- `_phaseTimer`
- `_validationTimer`

修改规则：

```text
只能由 Component 自己或 owner handler 改。
需要外部观察时，Component 投影到 Data 或发 typed Event。
```

Data projection 字段不能被外部随意 `Data.Set`，否则会伪造 Component 状态。

### System authoritative

System 是事实源，负责跨对象状态、索引、查询和批处理结果。

适合：

- TargetQuery 结果和诊断。
- Spawn wave runtime state。
- RecoverySystem 注册集合。
- DamageStatisticsSystem 统计。
- Ability / Projectile / Effect ownership index。

修改规则：

```text
只有 owning System 写。
其他对象通过 Query / readonly view / diagnostic projection 读。
```

## Projection：Data 里的观察副本

`projection` 是“为了观察而把权威状态投影到 Data”。

例如：

```text
AttackComponent._state 是事实源。
Data.AttackState 是给 AI/UI/Test 读的投影。
```

这时必须写清：

```text
authority = Component
runtimeOwner = AttackComponent
bindingPolicy = projection
writeEntry = AttackComponent internal only
```

投影字段的限制：

- 外部不能把它当普通 Data authoritative 字段写。
- DataModifier 不作用于 projection。
- DataOS record 通常不负责初始化 projection 的运行时值，除非它也是配置默认值。
- 对象池 acquire/release 时 projection 必须跟 Component local state 一起清理。

## 当前重点字段的初步判断

| 字段 | 初步 authority | 说明 |
| --- | --- | --- |
| `CurrentHp` | Data | Damage、Heal、UI、AI、Test 共享，适合 DataChanged 和约束。 |
| `CurrentMana` | Data | Ability cost、Recovery、UI/Test 共享；后续应收口写入口。 |
| `MoveSpeed` | Data | attribute-like numeric，适合 modifier 和 computed。 |
| `FinalMoveSpeed` | Data computed | 由 `MoveSpeed + MoveSpeedBonus` 派生。 |
| `AttackState` | Component + projection | 真正状态机在 `AttackComponent._state`；Data 只供观察。 |
| `MoveMode` | Component + projection | 真正策略在 MovementComponent；Data 供 AI/debug/test 观察。 |
| `Velocity` | 待审计 | 如果是跨 AI/Movement/knockback 协议，可暂留 Data；如果只是本帧内部结果，应下沉。 |
| `LastMoveDirection` | Component + projection 或 Data | Dash 等技能读取它，若作为跨 Feature 输入可留 Data；否则投影。 |
| `AIState` | AI owner + projection | 行为树/AIComponent 是事实源，Data 用于 debug/test/UI。 |
| `TargetNode` | System authoritative | Target/AI owner 写，其他方只读。 |
| `FeatureIsActive` | Feature owner | FeatureSystem 是事实源，可 Data authoritative 或 owner projection，需按 Feature 设计审计。 |

这张表不是最终裁决；它定义后续字段审计的第一批对象。

## 双层状态模型

推荐把运行时状态理解为两层：

```text
Contract layer:
  DataOS descriptor / DataDefinition / DataKey<T> / Feature manifest

Holder layer:
  Data / Profile / Component / System
```

Contract layer 可以集中；Holder layer 必须分区。

也就是：

```text
Data owns shared contract.
Component/Profile/System owns execution state.
```

## Health 示例

```text
Data authoritative:
  BaseHp
  FinalHp
  CurrentHp
  HpPercent
  IsDead

HealthComponent:
  _currentHpMirror
  _isDeadMirror
  _hitFlashTimer
  _deathAnimationStarted

Mutation:
  DamageCommand / HealCommand
    -> HealthFeature / DamageService
    -> Data.Set(CurrentHp)
    -> DataChanged(CurrentHp)
    -> HealthComponent mirror + UI/Test
```

`CurrentHp` 是 Data authoritative，因为它被多个功能共享。`_hitFlashTimer` 是 Component local，因为它只服务表现。

## Attack 示例

```text
Component authoritative:
  AttackComponent._state
  AttackComponent._currentTarget
  AttackComponent._phaseTimer

Data projection:
  AttackState

Read:
  AI/Test/UI 读取 Data.AttackState 或 Query。

Write:
  只有 AttackComponent 状态机改变 _state，再投影 AttackState。
```

这能解释为什么“Data 里有 `AttackState`”不等于 “Data 是攻击状态机的事实源”。

## Movement 示例

```text
Data authoritative:
  MoveSpeed
  FinalMoveSpeed
  IsMovementLocked

Component authoritative:
  _currentStrategy
  MovementParams
  _moveCompleted
  _facingDirection

待审计:
  Velocity
  LastMoveDirection
  MovementFacingDirection
  MoveMode
```

Movement 是最容易混乱的区域，因为它既有跨系统输入，又有每帧内部结果。后续 SDD 应把它拆成：

- attribute-like speed：留 Data。
- movement command/request：走 Event/Command。
- per-frame internal state：留 Component。
- debug/test 需要：projection。

## 反例

不应进入 Data authoritative：

- `EntityOrientationComponent.Sink`：typed options / Component profile。
- 攻击当前目标 `_currentTarget`：Component local，必要时发 Event 或 diagnostic projection。
- 对象池内部索引：ObjectPool System。
- TargetQuery 临时候选列表：查询结果，不是 Object 状态。
- 当前动画播放细节：Visual Component 或 UI 状态。

## 本页裁决

字段进 Data 之前必须回答：

```text
1. 它为什么需要 Data 的能力？
2. 谁是 authority？
3. 如果不是 Data authority，Data 里是否只是 projection？
4. 谁能写它？
5. 对象池复用时谁清它？
```

回答不清的字段不进入新 Data 方案，也不应该继续扩散 `Data.Get/Set` 调用点。
