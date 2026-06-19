# OOP 中数据定义与运行时管理方案

> 状态：current direction
> 用户原始问题：[`source-request.md`](./source-request.md)
> 入口：[`README.md`](./README.md)

## 这篇文档回答什么

本轮真正的问题不是“Data 还要不要”，而是：

```text
不用 ECS 后，OOP 框架里数据怎么定义、加载、承载、修改和观察？
```

用户提出的几个判断都成立：

- 很多字段确实是 Component 内部字段，集中到 Data 会出问题。
- 定义集中没有问题，但运行时值要按功能单元分开承载。
- 不能让外部直接获取 Component 然后改数据，否则耦合会很高。
- QFramework 的 Command / Query / Model / Event 有启发，但不能照搬。
- Data 系统还有大量未解决问题，必须先冻结方案再实施。

## 总体方案

推荐模型：

```text
Definition 集中。
Authoring 集中。
Assembly 统一。
Runtime Holder 分区。
Mutation 受控。
Observation 有选择。
```

展开：

```text
Definition:
  DataOS descriptor / generated contract 描述字段是什么。

Authoring:
  DataOS SQLite / runtime_snapshot records 提供初始输入。

Assembly:
  RuntimeRecordBinder 把 record 拆给 Data / Profile / Component / System。

Runtime Holder:
  Data 只承载共享和治理字段。
  Profile 承载中粒度业务数据组。
  Component 承载行为状态和局部字段。
  System 承载跨对象索引、批处理和查询状态。

Mutation:
  Command / Request / owner API / Service Pipeline 控制写入。

Observation:
  DataChanged / Event / Query / Log / Test 只覆盖值得观察的字段。
```

## 五层模型

### 1. Definition Layer

职责：描述字段，不保存实例值。

字段定义继续集中在 DataOS descriptor，并生成 C# contract。

需要表达：

- stable key / type / default。
- description / display / UI group。
- range / allowed values。
- modifier / computed。
- owner capability / skill。
- `authority`。
- `runtimeOwner`。
- `bindingPolicy`。
- `writeEntry`。
- `resetPolicy`。

这层回答：

```text
这个字段是什么？
谁拥有它？
它是否能 modifier / computed？
它运行时应该交给谁？
```

### 2. Authoring Input Layer

职责：保存静态配置和初始值。

对应：

```text
DataOS SQLite
runtime_snapshot.json
object / ability / feature / system records
```

这层可以平铺，便于表格编辑。例如一个 enemy record 里同时有：

```text
Name
BaseHp
CurrentHp
MoveSpeed
BaseAttack
AttackRange
DetectionRange
VisualScenePath
```

但平铺输入不代表运行时也必须平铺存储。

### 3. Assembly Layer

职责：把一条 record 拆成运行时对象需要的多个部分。

推荐核心对象：

```text
RuntimeRecordBinder
ObjectFactory / ObjectAssembler
```

流程：

```text
record
  -> validate descriptors
  -> group by authority/runtimeOwner/bindingPolicy
  -> DataInit
  -> ProfileInitData
  -> ComponentInitData
  -> BindingManifest
  -> diagnostics report
```

这层是旧方案缺失最严重的一层。没有它，就会出现两种坏结果：

- 所有字段都进 Data。
- 每个 Component 自己解析同一条 record。

### 4. Runtime Holder Layer

职责：运行时真正保存值。

#### Data

保存：

- 共享字段。
- 表格驱动字段。
- 需要 descriptor 约束的字段。
- 需要 modifier / computed 的字段。
- UI/debug/test/AI 需要稳定观察的字段。

#### Profile

保存中粒度业务数据组。

示例：

```text
HealthProfile
MovementProfile
CombatProfile
AbilityProfile
IdentityProfile
```

Profile 的价值是让字段不直接散成碎片，也不塞回 DataBag。

#### Component

保存：

- Godot Node bridge。
- 行为状态机。
- Timer handle。
- 临时缓存。
- 表现状态。
- 策略内部状态。

#### System

保存：

- 跨对象集合。
- 查询索引。
- 批处理状态。
- diagnostics snapshot。
- 服务生命周期。

### 5. Mutation Layer

职责：控制谁能改、怎么改、改完怎么传播。

推荐：

```text
Command / Request
  -> owner handler
  -> Service Pipeline if needed
  -> Data / Component / System authoritative write
  -> DataChanged / Event / Log / Validation
```

不要：

- 外部直接 `GetComponent<T>()` 改内部字段。
- UI 直接 `Data.Set` gameplay state。
- 任意系统写 projection 字段。
- 用 Event 替代所有 Command。

详见 [`08-Command与数据修改入口.md`](./08-Command与数据修改入口.md)。

## authority 是核心

每个字段都必须回答：

```text
谁是事实源？
```

三类：

```text
Data authoritative:
  Data 是事实源。Component 可以有 mirror，但不能私改 mirror 当真值。

Component authoritative:
  Component 是事实源。Data 只做 projection 或不出现。

System authoritative:
  System 是事实源。其他方通过 Query / projection / diagnostics 读。
```

如果 Data 里只是 projection，就必须写清楚：

```text
bindingPolicy = projection
writeEntry = owning component/system only
```

否则 projection 会被误当成普通 Data authoritative 字段。

## 数据定义方式的裁决

### 不回退 DataMeta

旧 DataMeta 静态方式直观，但会让事实源分裂。SlimeAI 现在已经有：

- DataOS authoring。
- runtime snapshot。
- descriptor validator。
- DataDefinitionCatalog。
- generated `DataKey<T>`。
- typed DataSlot。
- modifier/computed。

回退 DataMeta 会让这些链路变成镜像或重复来源。

### 继续数据库方式

但数据库方式必须补：

- 字段路由。
- authority。
- runtimeOwner。
- bindingPolicy。
- generated C# contract。
- binder diagnostics。

也就是说：

```text
不是回退到 meta。
而是让 descriptor 从“字段定义”升级成“字段定义 + 运行时路由 contract”。
```

## 运行时分类示例

### Health

```text
Data authoritative:
  BaseHp
  FinalHp
  CurrentHp
  HpPercent
  IsDead

Profile:
  HealthProfileInit

Component:
  HealthComponent._currentHpMirror
  HealthComponent._hitFlashTimer
  HealthComponent._deathAnimationStarted

Mutation:
  ApplyDamageCommand / ApplyHealCommand
  -> Health owner / DamageService
  -> Data.Set(CurrentHp)
  -> DataChanged
  -> HealthComponent mirror / UI / Test
```

### Attack

```text
Data authoritative:
  AttackRange
  AttackInterval
  AttackWindUpTime
  AttackRecoveryTime

Component authoritative:
  AttackComponent._state
  AttackComponent._currentTarget
  AttackComponent._phaseTimer
  AttackComponent._validationTimer

Projection:
  AttackState

Mutation:
  AttackCommand / Attack.Requested
  -> Attack owner
  -> Component state machine
  -> project AttackState
  -> AttackStarted / AttackFinished event
```

### Movement

```text
Data authoritative:
  MoveSpeed
  FinalMoveSpeed
  IsMovementLocked

Component authoritative:
  current strategy
  MovementParams
  per-frame facing / completion state

Needs audit:
  Velocity
  MoveMode
  LastMoveDirection
  MovementFacingDirection
```

Movement 需要后置审计，因为它同时包含属性、命令、每帧结果和观察投影。

### AI / Target

```text
AI owner:
  AIState
  AIMoveDirection
  AIMoveSpeedMultiplier
  PatrolTargetPoint

System owner:
  TargetNode
  target diagnostics

Projection:
  AIState / TargetNode 可进入 Data 供 debug/test/行为树条件读取，但写入口必须受 owner 控制。
```

## 对象池下的完整闭环

Acquire：

```text
clear old subscriptions
unbind DataBinding
remove old modifier sources
reset Component local state
apply new record through RuntimeRecordBinder
apply Data authoritative fields
configure Profile / Component
bind DataBinding
activate
```

Release：

```text
disable
unbind DataBinding
unsubscribe Event / Timer
remove modifier sources
reset Component local state
clear projections if needed
park
```

对象池验证是判断方案是否成立的关键。如果 acquire 后 Component mirror 仍是上一只对象的值，方案就是失败的。

## 与 QFramework 的对比

QFramework 值得参考：

- Model 提醒我们状态需要 owner。
- Command 提醒我们写操作要集中。
- Query 提醒我们读操作要只读。
- Event 提醒我们变化后需要通知。

SlimeAI 不采纳：

- 全局 Architecture。
- 每模块一个接口。
- 每操作一个 `AbstractCommand` 对象。
- BindableProperty 替代 Data。
- Global EventBus 作为默认通信。

SlimeAI 的替代：

```text
Data = typed shared runtime state protocol
Event = typed scoped fact message
Command = typed intent / request
Query = readonly read API
Feature owner = 修改和绑定规则的归属
Service Pipeline = 复杂写操作的处理链
```

## 最终裁决

```text
1. 字段定义继续集中在 DataOS descriptor，不回退 DataMeta。
2. 运行时值按 Data / Profile / Component / System 分区承载。
3. 每个进入新方案的字段必须声明 authority。
4. Data authoritative 字段通过 DataBinding 同步 Component mirror。
5. Component/System authoritative 字段只投影必要观察值到 Data。
6. 共享状态修改走 Command / owner API / Service Pipeline。
7. 第一个实现切片选择 Health / Damage / Recovery，不先碰 Movement / AI 大混合区。
```

## 下一步

进入实现前先创建 SDD：

```text
Data Contract Routing Design
```

该 SDD 只做字段审计和 contract 设计，不直接全仓改代码。字段审计冻结后，再进入 `RuntimeRecordBinder + Health DataBinding` 纵切实现。
