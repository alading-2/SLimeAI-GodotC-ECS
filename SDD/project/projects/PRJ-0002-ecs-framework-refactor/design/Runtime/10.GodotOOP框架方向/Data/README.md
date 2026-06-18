# Data 方案入口

> 状态：current direction / not implementation spec  
> 上级文档：[`../README.md`](../README.md)。  
> 原始问题：[`../source-request.md`](../source-request.md)。

## 一句话结论

Data 名字保留。

Data 在 SlimeAIFramework 中的定位是：

```text
Data = 受控共享状态 + 表格驱动 + descriptor 约束 + modifier/computed + 可观察同步
```

Data 不再是 ECS storage，也不再默认承载所有 Component 字段。Component 可以保存内部字段；Data 只负责需要共享、DataOS 初始化、验证追踪、持久化、debug/UI 或 modifier/computed 的字段。

## 本目录文档

| 文档 | 职责 |
| --- | --- |
| [`01-Data进入条件与双层状态模型.md`](./01-Data进入条件与双层状态模型.md) | 判断哪些字段进入 Data，哪些留在 Component，并定义 authority |
| [`02-DataOS到Component同步方案.md`](./02-DataOS到Component同步方案.md) | 说明数据库到 Data，再到 Component 字段的传递链路和对象池复用同步 |
| [`03-Descriptor约束与DataModifier裁决.md`](./03-Descriptor约束与DataModifier裁决.md) | 回答 Description、数值限制、allowed values、computed、DataModifier 是否保留 |
| [`04-迁移与验证路线.md`](./04-迁移与验证路线.md) | 给出后续 SDD 和验证场景拆分 |
| [`05-外部方案证据与采纳边界.md`](./05-外部方案证据与采纳边界.md) | 汇总 Godot、Unity Entities、Unreal GAS 的外部证据和 SlimeAI 采纳边界 |

## 先回答用户的关键问题

### Component 内部字段可以放 Component 里吗？

可以，而且新方向默认允许。

但要区分：

- **Component 内部字段**：本组件局部使用，不需要表格、共享、debug/test 追踪。
- **Data 字段**：多个功能读取/修改，或需要 DataOS、约束、modifier、computed、save/load、UI/debug/test。
- **Component 镜像字段**：来源是 Data，但为了热路径或 API 手感缓存到 Component 内。

### Data 怎么传到 Component？

不要靠 Component 到处 `Data.Get`，也不要靠字段名反射。推荐显式 `DataBinding`：

```text
DataOS SQLite
  -> runtime_snapshot.json descriptors + records
  -> DataDefinitionCatalog
  -> Object.Data apply record
  -> DataBinding.BindInitial(object)
  -> Component.Configure / ApplyData / SetXxx
  -> DataChanged<T> 后续增量同步
```

### 对象池拿对象出来时怎么更新？

对象池 acquire 必须先刷新 Data，再刷新 Component：

```text
Acquire
  -> 清旧 runtime state / modifier source
  -> Apply 新 Data record
  -> Component.ResetLocalState()
  -> DataBinding.BindInitial()
  -> Activate
```

这样 Component 的内部镜像字段不会保留上一只怪、上一颗子弹或上一段技能的旧值。

### Description、数值限制、allowed values 还怎么实现？

保留在 Data descriptor 中。

只要字段进入 Data，就继续拥有：

- `displayName`
- `description`
- `rangePolicy`
- `minValue`
- `maxValue`
- `allowedValues`
- `storagePolicy`
- `writePolicy`
- `modifierPolicy`
- `computeId`
- `dependencies`

如果字段只留在 Component 内，它不享受 Data descriptor 约束；这不是缺陷，而是说明它不是 Data 字段。若它也需要 authoring/validator/UI/AI 解释，就应升级为 Data 或 typed options descriptor。

### DataModifier 要保留吗？

保留，但收窄。

DataModifier 只应作用于 attribute-like numeric Data 字段，例如：

- MaxHp
- MoveSpeed
- Attack
- AbilityDamageBonus
- DamageReduction
- CritChance

不应作用于：

- Component 私有缓存。
- UI 动画状态。
- 资源路径。
- 一帧临时值。
- 任意字符串或 object_ref 字段。

## 外部方案采纳

- Godot：采纳 Node/OOP/scene composition；Data 不替代 Node 状态。
- Unity Entities：只作为反证，说明真正 ECS storage 是 archetype/chunk，不是 SlimeAI Data。
- Unreal GAS：采纳“Attribute + GameplayEffect modifier + defaults table”的思想，支持 DataModifier 保留在属性类字段上。
- QFramework：采纳强类型 Model/Property、Command/Query/Event 的边界感，不接入全局 Architecture。

证据和取舍详见 [`05-外部方案证据与采纳边界.md`](./05-外部方案证据与采纳边界.md)。

## 后续实现默认假设

- DataOS 保留。
- runtime snapshot 保留。
- generated `DataKey<T>` 保留。
- DataModifier 保留但收窄。
- computed resolver 保留但只用于少数派生共享字段。
- Data changed event 作为 Data -> Component / UI / debug / test 的同步机制。
- Component 热路径可以缓存 Data 字段，但必须声明绑定和 authority。
