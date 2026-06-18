# DataOS 到 Component 同步方案

## 目标

保留用户强调的核心能力：

```text
填表格就能传数据。
数据库到 Data 这条路必须实现。
对象池复用时更新 Data，也必须更新 Component 字段。
```

新的方向不是删除 DataOS，而是把 DataOS 到 Component 的传递链路显式化。

## 推荐链路

```text
DataOS SQLite authoring
  -> generator / validator
  -> runtime_snapshot.json
       descriptors: 字段定义、约束、modifier/computed 元数据
       records: object / ability / system / feature 初始值
  -> DataDefinitionCatalog
  -> ObjectSpawnProfile / RuntimeDataRecord
  -> Object.Data apply record
  -> DataBinding.BindInitial(object)
  -> Component fields
```

这条链路保留现有 DataOS 资产，但不要求 Component 每帧从 Data 读取。

## DataBinding

建议新增显式 `DataBinding` 概念，作为后续实现的核心。

职责：

- 声明 DataKey 到 Component setter 的映射。
- 初始化时把 Data 值注入 Component。
- 订阅 Data changed event，增量更新 Component 镜像字段。
- Component 注销或对象池 release 时解绑。
- 输出 diagnostics，说明哪些字段绑定成功、缺失或类型不匹配。

不做：

- 不用反射自动扫描同名字段。
- 不把所有 DataKey 都绑定到所有 Component。
- 不在 `_Process` 中轮询 Data。

## 绑定声明位置

绑定应由 Feature owner 管理，而不是 Runtime 全局猜测：

```text
MovementFeatureDataBinding
  MoveSpeed -> MovementComponent.SetMoveSpeed
  IsMovementDisabled -> MovementComponent.SetMovementDisabled

HealthFeatureDataBinding
  CurrentHp -> HealthComponent.SetCurrentHp
  MaxHp -> HealthComponent.SetMaxHp
  IsDead -> HealthComponent.SetDead
```

也可以作为 ComponentCompositionProfile 的一部分，但必须能被文档和 validator 看见。

## 初始化顺序

推荐顺序：

```text
ObjectFactory
  -> create Godot object root
  -> create / attach Data container
  -> apply DataOS runtime record
  -> compose Components
  -> run DataBinding.BindInitial
  -> register Components
  -> activate Object
```

如果现有实现必须先 register 再 bind，也必须满足：

- Component 进入 active 之前已拿到初始 Data。
- DataBinding 订阅已经建立。
- 失败时阻断激活并输出 diagnostics。

## 增量同步

Data 字段变化时：

```text
Data.Set(MoveSpeed, value)
  -> policy/range/modifier/computed 处理
  -> DataChanged<float>(MoveSpeed, old, new)
  -> DataBinding receiver
  -> MovementComponent.SetMoveSpeed(new)
```

Component 写回 Data 时必须通过 owner API：

```text
HealthComponent.ApplyDamage(...)
  -> DamageSystem / HealthFeature API
  -> Data.Set(CurrentHp, newValue)
  -> DataChanged<float>
  -> HealthComponent.SetCurrentHp(newValue)
```

如果 Component 本身就是权威，可以使用 projection：

```text
MovementComponent updates _lastMoveDirection
  -> projection policy says debug/test needs it
  -> Data.TrySetSystem(LastMoveDirection, value)
```

projection 字段必须标明来源，避免 AI 把它误解为 Data 权威。

## 对象池复用

对象池 acquire 时必须执行完整刷新：

```text
Pool.Acquire(recordId)
  -> object.DisableCallbacks()
  -> object.Data.ClearRuntimeResetGroups()
  -> object.Data.RemoveModifiersBySource(oldObjectSources)
  -> DataRuntimeBootstrap.ApplyToData(object.Data, recordId)
  -> component.ResetLocalState()
  -> DataBinding.BindInitial(object)
  -> object.EnableCallbacksAfterActivationGuard()
```

对象池 release 时：

```text
Pool.Release(object)
  -> object.Disable()
  -> DataBinding.Unbind(object)
  -> Event subscriptions off
  -> Timer cancel
  -> Data modifier source cleanup
  -> component.ResetLocalState()
  -> park object
```

## 错误处理

DataBinding 必须 fail fast 或输出结构化 report：

| 错误 | 建议 |
| --- | --- |
| descriptor 缺失 | error，阻断绑定 |
| record 缺字段但 descriptor 有默认值 | warning 或 info，使用默认值 |
| setter 类型不匹配 | error，阻断激活 |
| Component 不存在 | 按 binding 是否 required 决定 error/warning |
| Data changed 后 setter 抛错 | error，记录 field、component、object id |

## 验证场景

后续实现至少需要一个 Godot headless 场景验证：

```text
DataBindingValidationScene
  Given unit.enemy record has MoveSpeed=2.5 and CurrentHp=30
  When object is spawned from pool
  Then Object.Data has typed values
  And MovementComponent._moveSpeed mirror is 2.5
  And HealthComponent mirror is 30
  When Data.Set(MoveSpeed, 4.0)
  Then MovementComponent mirror updates to 4.0
  When object is released and re-acquired with another record
  Then old mirrors and modifiers do not leak
```

