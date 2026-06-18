# Data 进入条件与双层状态模型

## 问题

旧方向把很多状态默认放进 Data，导致 Data 同时承担 authoring、runtime、policy、modifier、computed、presentation、AI routing、diagnostics。用户这轮提出的疑问很关键：如果 Component 内部字段也可以存在，Data 到底还管什么？

答案是：Data 不管所有字段，只管有共享和工具链价值的字段。

## Data 进入条件

字段进入 Data 之前必须至少满足一条强条件，最好能满足两条以上：

| 条件 | 示例 |
| --- | --- |
| 多 Feature / Component / System 读写 | CurrentHp 被 Damage、UI、TargetQuery、AI 读取 |
| 来自 DataOS 表格 | unit.enemy 的 MaxHp、MoveSpeed、Attack |
| 需要 validator / log / test / debug 追踪 | IsDead、LifecycleState、AbilityCurrentCharges |
| 需要持久化、回放或对比验证 | Level、Exp、装备属性 |
| 需要 modifier / computed | MoveSpeed、FinalAttack、DamageReduction |
| 需要 UI binding | CurrentHp、CurrentMana、Cooldown |

不满足这些条件的字段默认不进 Data。

## 字段分类

| 类别 | 存放位置 | 示例 | 说明 |
| --- | --- | --- | --- |
| Component local | Component 字段 | hit flash timer、cached node、animation state | 不进 Data |
| Component options | typed options / profile | Orientation sink、visual flip mode | 固定结构参数，不进 DataOS |
| Data authoritative | Data + Component 镜像 | CurrentHp、MoveSpeed | Data 是共享事实源 |
| Component authoritative projection | Component 字段 + 可选 Data projection | LastMoveDirection、current animation | Component 是事实源，Data 只供观察 |
| System authoritative | System state + Data/diagnostics 投影 | target query diagnostics、spawn wave runtime | System 是事实源 |
| Authoring config | DataOS table / profile | ability cast range、resource path | 不一定都进入 Object.Data |

## Authority 必须显式声明

每个 Data 字段都要说明谁是权威：

```text
authority = Data
  其他代码通过 Data / owner service 修改，Data changed 同步给 Component。

authority = Component
  Component 是事实源，只在需要观察或持久化时投影到 Data。

authority = System
  System 统一计算或写入，Component 只消费结果。
```

没有 authority 的字段不能进入新 Data 方案。否则会回到旧问题：Component 写一份，Data 写一份，System 又计算一份。

## 双层状态模型

推荐模型：

```text
Data owns contract.
Component owns execution.
```

Data 负责：

- 初始值。
- 类型和约束。
- modifier/computed。
- changed event。
- diagnostics。
- save/load 或 replay 所需字段。

Component 负责：

- 热路径使用。
- Godot 节点桥接。
- 内部临时状态。
- 表现状态。
- 局部算法。

## 示例：Health

```text
Data:
  MaxHp
  CurrentHp
  IsDead

HealthComponent:
  _currentHpMirror
  _isDeadMirror
  _hitFlashTimer
  _deathAnimationStarted
```

`CurrentHp` 是 Data 权威，因为 Damage、UI、TargetQuery、Test 都要读取。`hitFlashTimer` 是 Component local，因为它只影响表现。

## 示例：Movement

```text
Data:
  MoveSpeed
  IsMovementDisabled

MovementComponent:
  _moveSpeed
  _desiredDirection
  _lastResolvedVelocity
  _wasBlockedLastFrame
```

`MoveSpeed` 进入 Data，因为 Buff、Ability、Debug 和 Movement 都要读写。`_lastResolvedVelocity` 默认留 Component；只有 TargetQuery、UI 或测试需要稳定读取时，才考虑投影。

## 反例

不应进入 Data：

- `EntityOrientationComponent.Sink`：它只是 Godot bridge 策略，放 typed options。
- 动画当前播放状态：除非 UI/test/save 需要统一读取。
- 对象池内部索引：属于 ObjectPool System。
- TargetQuery 临时候选列表：属于查询结果，不是对象状态。

