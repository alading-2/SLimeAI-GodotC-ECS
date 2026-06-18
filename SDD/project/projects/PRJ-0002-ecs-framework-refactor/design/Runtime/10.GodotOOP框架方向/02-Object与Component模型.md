# Object 与 Component 模型

## 核心结论

`Entity` 概念改成 `Object` 是对的。SlimeAIFramework 的 Object 不是 ECS entity，而是 Godot/OOP 运行时对象入口。

```text
Object
  -> Godot Node / Scene root / 纯 C# runtime object
  -> 有稳定 runtime id
  -> 有生命周期
  -> 有 Component 组合
  -> 有 Event bus
  -> 可选接入 Data
```

Component 名称保留。Component 是 Object 上的功能单元，可以有内部字段、缓存、节点引用和局部状态。

## Object 要承担什么

Object 至少需要这些能力：

| 能力 | 说明 |
| --- | --- |
| 身份 | 稳定 id、debug name、owner、source record |
| 生命周期 | create / registered / active / disabled / pooled / destroyed |
| 组件组合 | 通过代码化 profile / composer 挂载 Component |
| 事件入口 | 本对象内 typed event bus |
| Data 入口 | 可选；只有需要共享或表格驱动时绑定 Data |
| Diagnostics | 当前状态、组件列表、Data record、Feature 来源可被 AI 和测试读取 |

短期源码仍可能叫 `Entity`，但新设计文档中统一解释为历史命名下的 Object。

## Component 可以保存内部字段

Component 内部字段是允许的，而且是 Godot/OOP 方向下的默认选择。

适合留在 Component 的字段：

- 节点引用。
- 动画状态。
- 输入临时状态。
- 上一帧缓存。
- 算法中间结果。
- 只被本 Component 使用的参数。
- 只服务表现层的状态。
- 对象池复用时由 Component 自己清理的临时字段。

这些字段不需要 DataKey，不需要 DataOS descriptor，也不需要 DataModifier。

## 什么字段应该进入 Data

判断一个字段是否进入 Data，不看它是不是“数据”，而看它是否有共享和工具链价值：

| 问题 | 是 | 否 |
| --- | --- | --- |
| 多个 Feature / Component / System 都要读写？ | Data | Component 字段 |
| 需要从 DataOS 表格初始化？ | Data | typed options 或代码 profile |
| 需要 UI / debug panel / log / validator / test 追踪？ | Data | Component 字段 |
| 需要 save/load、回放、对比验证？ | Data | Component 字段 |
| 需要 DataModifier 或 computed？ | Data | Component 字段或 owner 规则 |
| 只是桥接 Godot 节点如何表现？ | Component options | Data |

## 双层状态模型

为了同时保留 OOP 手感和 Data 能力，推荐使用双层状态模型：

```text
Data = 受控共享状态和配置事实源
Component field = 本组件运行时使用的局部镜像或内部状态
```

例如 `MoveSpeed`：

```text
Data.MoveSpeed
  来源：DataOS record / modifier / computed
  用途：AI、Buff、TargetQuery、Debug、Test 都可能读取

MovementComponent._moveSpeed
  来源：Data binder 从 Data 同步
  用途：MovementComponent 热路径直接使用，避免每帧查 Data
```

这个模型不会要求每帧读写 Data。Data 只在初始化、modifier 改变、外部系统写入或需要观察时参与；Component 热路径可以使用自己的字段。

## Data 到 Component 怎么传

推荐新增或重写一个显式绑定层，暂名 `DataBinding`：

```text
ObjectFactory / SpawnPipeline
  -> 创建 Object
  -> DataOS record apply 到 Object.Data
  -> ComponentComposer 按 profile 创建 Component
  -> DataBinding.BindInitial(object)
       -> 读取 DataKey<T>
       -> 调用 Component.Configure / ApplyData / SetXxx
       -> 建立 Data changed 订阅
  -> Component OnRegistered / Activate
```

绑定规则必须显式写在 owner 文档或 profile 中，不靠反射扫描字段名。

示例：

```text
MovementFeature binding:
  Data.MoveSpeed -> MovementComponent.SetMoveSpeed(float)
  Data.IsMovementDisabled -> MovementComponent.SetDisabled(bool)
  Data.LastMoveDirection -> 只在需要 debug/test 时从 component projection 写回 Data
```

## Component 到 Data 怎么写

Component 不应该随便写所有 Data。写入必须经过 owner API 或绑定策略：

| 类型 | 推荐方向 |
| --- | --- |
| Data 权威字段 | 外部通过 Data.Set / owner service 改 Data，Data changed 同步到 Component |
| Component 权威字段 | Component 内部维护，必要时按 projection 规则写回 Data |
| System 权威字段 | System 统一计算并写 Data 或调用 Component API |

为了避免循环：

- DataBinding 写 Component 时要带 source 标记。
- Component projection 写 Data 时要区分 `Runtime` / `System` / `Debug` 来源。
- 同一帧内 Data changed 和 Component projection 的顺序必须由 owner 定义。

## 对象池复用时怎么同步

对象池拿对象出来时必须按“先 Data 后 Component”的顺序刷新：

```text
Acquire from pool
  -> Object.ResetPooledRuntimeState()
  -> 清理旧 Data runtime-only 字段和旧 modifier source
  -> Apply 新 runtime snapshot record 到 Object.Data
  -> Component.ResetLocalState()
  -> DataBinding.BindInitial(object)
  -> 激活首帧 guard / collision embargo
  -> Activate
```

放回池时：

```text
Release to pool
  -> Disable
  -> Unsubscribe Event / Data changed / Timer
  -> Remove modifiers by source
  -> Component.ResetLocalState()
  -> 标记 pooled
```

这回答了用户提出的关键点：从对象池拿对象出来更新 Data 时，确实应该同步更新 Component 的数据字段，但同步不应靠每个 Component 手写散点逻辑，而应由 Feature owner 的 DataBinding 规则统一执行。

## 不推荐方向

- 不推荐 Component 字段和 Data 字段长期双写但没有 authority 规则。
- 不推荐通过字段名反射自动绑定 Data。
- 不推荐所有 Component 字段都进 DataOS。
- 不推荐在 `_Process` 中每帧 `Data.Get` 作为默认模式。
- 不推荐把对象池复用时的 Data 刷新藏在 `OnReady` 或 Godot `_Ready()` 中。

