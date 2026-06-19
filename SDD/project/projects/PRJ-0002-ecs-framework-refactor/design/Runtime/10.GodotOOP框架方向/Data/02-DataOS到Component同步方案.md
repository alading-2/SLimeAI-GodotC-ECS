# DataOS 到 Component 同步方案

> 用户原始问题：[`source-request.md`](./source-request.md)
> 入口：[`README.md`](./README.md)

## 要保留的能力

用户明确要求保留：

```text
填表格就能传数据。
数据库到 Data 这条路必须实现。
对象池拿对象出来更新 Data 时，也必须更新 Component 字段。
```

本方案不删除 DataOS，也不让每个 Component 自己解析数据库。新的原则是：

```text
统一读取。
统一拆分。
按 owner 注入。
显式绑定。
激活前完成同步。
```

## 目标链路

```text
DataOS SQLite authoring
  -> generator / validator
  -> runtime_snapshot.json
       descriptors: 字段定义、约束、modifier/computed、authority、runtimeOwner、bindingPolicy
       records: object / ability / feature / system 初始值
  -> DataDefinitionCatalog
  -> RuntimeRecordBinder
  -> grouped init payloads
       DataInit
       HealthProfileInit
       MovementProfileInit
       AttackComponentInit
       AbilityProfileInit
  -> Object.Data apply shared fields
  -> Profile.Initialize(init)
  -> Component.Configure(init/profile)
  -> DataBinding.BindInitial(object)
  -> Activate
```

当前实现已有 DataOS、snapshot、catalog、typed `DataKey<T>` 和 Data runtime。缺的是 `RuntimeRecordBinder`、字段路由元数据和 DataBinding contract。

## RuntimeRecordBinder

`RuntimeRecordBinder` 是当前最缺的一层。

职责：

- 读取一条 runtime record。
- 根据 descriptor 的 `authority`、`runtimeOwner`、`bindingPolicy` 分组字段。
- 生成 Data / Profile / Component / System 初始化 payload。
- 校验 record 字段是否能找到 owner。
- 输出结构化 report，说明字段去了哪里、哪些字段缺失、哪些字段被拒绝。

不做：

- 不让每个 Component 自己遍历整份 record。
- 不在 Component 里写字段名字符串解析。
- 不靠同名字段反射自动灌值。
- 不把所有字段先塞进 Data，再让 Component 自己捞。

## Profile / InitData

Profile 是可选但推荐的中粒度数据组织单位。

它解决两个极端：

```text
所有字段都在 DataBag：过度集中。
所有字段直接散到 Component：过度碎片。
```

推荐结构：

```text
HealthProfileInit:
  BaseHp
  FinalHp
  CurrentHp
  IsDead

MovementProfileInit:
  MoveSpeed
  FinalMoveSpeed
  IsMovementLocked
  DefaultMoveMode

AttackProfileInit:
  AttackRange
  AttackInterval
  AttackWindUpTime
  AttackRecoveryTime

AbilityProfileInit:
  AbilityCooldown
  AbilityMaxCharges
  AbilityCurrentCharges
  AbilityCostType
  AbilityCostAmount
```

Profile 不一定一开始就落成大量 C# 类。首个 SDD 可以先定义 init payload 和 binder report，跑通 Health/Damage。

## DataBinding

DataBinding 只处理一种关系：

```text
Data authoritative 字段 -> Component mirror
```

职责：

- 声明 DataKey 到 Component setter 的映射。
- 初始化时把 Data 值注入 Component。
- 订阅 typed `DataChanged<T>`，增量更新 Component mirror。
- Component 注销或对象池 release 时解绑。
- 输出 diagnostics。

不负责：

- Component authoritative 字段的日常修改。
- System authoritative 字段的写入。
- 通过反射扫描所有字段。
- 每帧轮询 Data。

示例：

```text
HealthDataBinding
  CurrentHp -> HealthComponent.SetCurrentHpMirror
  FinalHp -> HealthComponent.SetMaxHpMirror
  IsDead -> HealthComponent.SetDeadMirror

MovementDataBinding
  FinalMoveSpeed -> EntityMovementComponent.SetMoveSpeedMirror
  IsMovementLocked -> EntityMovementComponent.SetMovementLockedMirror
```

## 初始化顺序

推荐 Object 创建顺序：

```text
ObjectFactory
  -> create Godot object root
  -> create Data container
  -> load runtime record
  -> RuntimeRecordBinder.Bind(record)
  -> apply Data authoritative fields to Object.Data
  -> create / configure Profile payloads
  -> compose Components
  -> Component.Configure(init/profile)
  -> DataBinding.BindInitial(object)
  -> register Components
  -> activate Object
```

如果现有生命周期必须先注册 Component，也要保证：

- Component active 前拿到初始值。
- DataBinding 订阅已经建立。
- 失败时阻断激活并输出 diagnostics。

## 增量同步

Data authoritative 字段的增量路径：

```text
Command / owner API / Service Pipeline
  -> Data.Set(CurrentHp, newValue)
  -> policy / range / computed / modifier
  -> DataChanged<float>(CurrentHp, old, new)
  -> DataBinding receiver
  -> HealthComponent.SetCurrentHpMirror(new)
  -> UI / debug / test / Log
```

Component authoritative 字段的投影路径：

```text
AttackComponent changes _state
  -> AttackComponent.ProjectState()
  -> Data.SetSystem(AttackState, _state) 或 typed projection API
  -> DataChanged<AttackState>
  -> AI/UI/Test reads projection
```

投影写入口要和普通 `Data.Set` 区分，避免外部伪造 Component 状态。

## 对象池 acquire / release

对象池是本方案必须优先验证的场景。

Acquire：

```text
Pool.Acquire(recordId)
  -> Disable callbacks / subscriptions
  -> DataBinding.Unbind(old)
  -> clear old modifier sources
  -> clear runtime reset groups / projections
  -> Component.ResetLocalState()
  -> load new record
  -> RuntimeRecordBinder.Bind(record)
  -> Object.Data apply Data authoritative fields
  -> Component.Configure(init/profile)
  -> DataBinding.BindInitial(object)
  -> Enable callbacks after activation guard
  -> Activate
```

Release：

```text
Pool.Release(object)
  -> Disable object
  -> DataBinding.Unbind(object)
  -> Event subscriptions off
  -> Timer cancel
  -> Remove modifiers by source
  -> Component.ResetLocalState()
  -> clear projection fields if needed
  -> park object
```

原则：

- Data authoritative 字段由新 record 或默认值覆盖。
- Component local state 必须显式 reset。
- Projection 字段跟随 owner 清理。
- Modifier source 必须按来源回滚。

## 错误处理

DataBinding / RuntimeRecordBinder 必须 fail fast 或输出结构化 report：

| 错误 | 建议 |
| --- | --- |
| descriptor 缺 `authority` | error，阻断新方案字段绑定 |
| descriptor 缺 `runtimeOwner` | error 或 migration warning，取决于字段是否进入新 binder |
| record 字段找不到 descriptor | error |
| descriptor 有默认值但 record 缺字段 | info / warning，使用默认值 |
| setter 类型不匹配 | error，阻断激活 |
| Component 不存在 | required binding 为 error，optional binding 为 warning |
| projection 被普通 runtime 写入 | error 或 warning，按迁移阶段决定 |
| Data changed 后 setter 抛错 | error，记录 object、field、component、value |

## 为什么不回到 Component 自己加载

让 Component 自己解析 record 短期看简单，但会产生：

- 同一字段被多个 Component 解析。
- 对象池 reset 点分散。
- 字段缺失时没有统一 report。
- AI 无法知道字段最终注入到了哪里。
- 运行时复用时容易串上一只对象的旧值。

所以 Component 可以持有字段，但加载和分发不能散在 Component 内部。

## 最小验证场景

首个实现切片建议：

```text
DataBindingHealthDamageValidationScene
```

场景要求：

```text
Given unit.enemy record has CurrentHp=30, BaseHp=30, MoveSpeed=2.5
When object is spawned or acquired from pool
Then Object.Data has typed values
And HealthComponent mirror has CurrentHp=30
And MovementComponent mirror has speed from DataBinding or init payload

When DamageCommand applies 10 damage
Then Object.Data.CurrentHp becomes 20
And HealthComponent mirror becomes 20
And UI/debug/test receives DataChanged

When object is released and acquired with another record
Then old HP, IsDead, modifier source and Component local state do not leak
```
