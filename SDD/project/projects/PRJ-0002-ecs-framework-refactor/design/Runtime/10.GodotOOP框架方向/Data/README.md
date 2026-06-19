# Data 方案入口

> 状态：current direction / design package
> 上级文档：[`../README.md`](../README.md)
> 用户原始问题：[`source-request.md`](./source-request.md)

## 一句话结论

Data 名字保留，但 Data 不再是“所有运行时字段的唯一容器”。

新的 Data 方案是：

```text
定义集中。
装载统一。
承载分区。
权威明确。
修改受控。
同步显式。
```

对应到 SlimeAIFramework：

```text
DataOS descriptor / runtime_snapshot 负责字段定义、表格输入、约束和生成。
RuntimeRecordBinder 负责把一条 record 拆给 Data / Profile / Component / System。
Data 只承载共享、表格驱动、约束、modifier/computed、UI/debug/test/AI 观察需要的字段。
Component / Profile / System 可以保存自己的运行时字段。
Command / owner API / Service Pipeline 负责受控修改，不允许外部随意拿 Component 改状态。
DataBinding 负责 Data authoritative 字段到 Component mirror 的初始化和增量同步。
```

## 为什么要重构

旧 Data 方向的问题不是“字段集中”本身，而是把五件事混成了一个大容器：

- 字段定义：字段叫什么、类型是什么、描述和约束是什么。
- authoring 输入：数据库、JSON、snapshot 里有哪些初始值。
- 运行时承载：对象实例当前真正保存什么值。
- 运行时修改：谁能改、怎么改、改完怎么传播。
- 可观察能力：UI、debug、test、AI、存档和日志怎么读取。

这些职责全塞进 `Data` 后，Component 内部临时字段、表现状态、策略缓存、系统索引也会被误认为必须进入 Data。结果是 Data 膨胀，Component 不敢保存字段，`Data.Get/Set` 散点增多，字段到底谁说了算也不清楚。

## 当前裁决

| 问题 | 裁决 |
| --- | --- |
| 是否回退旧 `DataMeta` 静态定义 | 不回退。保留 DataOS descriptor / runtime snapshot / generated `DataKey<T>`，补足 authority、runtimeOwner、bindingPolicy。 |
| 字段能否放 Component | 可以。Component 是功能单元，可以保存内部字段、热路径缓存、表现状态和策略状态。 |
| 定义是否仍集中 | 是。集中的是 schema、约束、描述、生成和装载路由，不是所有实例值。 |
| 其他地方要读写怎么办 | 读走 Query / readonly view / Data 共享字段；写走 Command / owner API / Service Pipeline，不直接获取 Component 改值。 |
| Command 是否可用 | 可以用，但是 typed command/request + owner handler/pipeline，不照搬 QFramework 的每个操作一个引用类型 Command 类。 |
| DataModifier 是否保留 | 保留但收窄到 attribute-like numeric Data 字段。 |
| DataBinding 是否保留 | 保留，而且必须显式声明，不靠字段名反射和 Component 自己散点读 Data。 |

## 本目录文档

| 文档 | 职责 |
| --- | --- |
| [`source-request.md`](./source-request.md) | 保存本轮 Data 方案用户原始问题和追加确认。 |
| [`01-Data进入条件与双层状态模型.md`](./01-Data进入条件与双层状态模型.md) | 定义哪些字段进入 Data、哪些留在 Component/Profile/System，并解释 authority / projection。 |
| [`02-DataOS到Component同步方案.md`](./02-DataOS到Component同步方案.md) | 说明数据库到 record，再到 Data / Profile / Component 的拆分、注入、绑定和对象池复用。 |
| [`03-Descriptor约束与DataModifier裁决.md`](./03-Descriptor约束与DataModifier裁决.md) | 说明 descriptor 不回退 meta、要新增哪些路由字段，以及 DataModifier 的保留范围。 |
| [`04-迁移与验证路线.md`](./04-迁移与验证路线.md) | 给出后续 SDD、字段审计、实现切片、验证和 grep gate。 |
| [`05-外部方案证据与采纳边界.md`](./05-外部方案证据与采纳边界.md) | 汇总 Godot、Unity Entities、Unreal GAS、QFramework 对 Data 方案的证据和边界。 |
| [`06-外部非ECS框架实体数据管理调研.md`](./06-外部非ECS框架实体数据管理调研.md) | 参考资料：非 ECS 框架如何管理异构实体属性。 |
| [`07-OOP中数据定义与运行时管理方案.md`](./07-OOP中数据定义与运行时管理方案.md) | 完整方案：Definition / Authoring / Assembly / Runtime Holder / Mutation / Observation 分层。 |
| [`08-Command与数据修改入口.md`](./08-Command与数据修改入口.md) | 专项分析 Command、Event、Query、owner API、Service Pipeline 的边界和 SlimeAI 采用方式。 |
| [`09-Data底层执行草案.FeatureSpec.md`](./09-Data底层执行草案.FeatureSpec.md) | 执行规格草案：先确定 Data 底层怎么改，暂不做数据库到 Data、DataOS record 加载和 RuntimeRecordBinder。 |

## 字段归属的直观解释

本轮用户问到 `AttackState / MoveMode / Velocity / AIState` 是否继续 Data authoritative。这里的核心是：

```text
authority = 谁是事实源，谁说了算。
projection = 为了 UI / AI / test / debug，把事实源的值投影一份到 Data。
```

示例：

| 字段 | 推荐方向 | 原因 |
| --- | --- | --- |
| `CurrentHp` | Data authoritative | Damage、UI、AI、Test、存档、日志都需要读写，且需要约束和事件。 |
| `MoveSpeed` / `FinalMoveSpeed` | Data authoritative | Buff、属性、Movement、debug 都需要，且适合 modifier/computed。 |
| `AttackState` | Component authoritative + Data projection | 真正状态机在 `AttackComponent._state`；Data 里的 `AttackState` 主要给 AI/UI/Test 观察。 |
| `MoveMode` | Component authoritative + Data projection | 真正策略切换在 `EntityMovementComponent`；Data 用于观察当前模式。 |
| `Velocity` | 需要再拆 | 若是跨 AI/Movement/knockback 的速度协议可暂留 Data；若只是每帧内部结果，应下沉到 MovementComponent/VelocityResolver 并只投影必要值。 |
| `AIState` | AI System/AIComponent authoritative + Data projection | AI 行为树或 AIComponent 是事实源；Data 用于 debug/UI/测试。 |

这不是最终字段清单，而是后续 SDD 必须执行的字段审计规则。

## QFramework 的采纳边界

QFramework 值得参考的是概念，不是 API：

- Model：提醒我们相关状态要有明确 owner。
- Command：提醒我们写操作要可追踪、可测试、可拦截。
- Query：提醒我们读操作不要变成随处拿对象改状态。
- Event：提醒我们变化需要通知，但 Event 和 Command 不是一回事。

SlimeAI 不引入 QFramework 的 Architecture 单例、每模块一个接口、每操作一个 `AbstractCommand` 类。SlimeAI 更适合：

```text
typed command/request record
  -> Feature owner / Service Pipeline handler
  -> Data.Set 或 Component authoritative method
  -> DataChanged / typed Event / Log / Validation
```

## 下一步默认假设

- DataOS 保留为字段定义和 authoring 输入事实源。
- 不回退旧 `DataMeta` 静态注册方案。
- descriptor 新增 `authority`、`runtimeOwner`、`bindingPolicy`、`writeEntry` 等路由字段。
- DataModifier 保留但只用于数值属性类 Data。
- 第一个实现切片建议选 `Unit + Health + Damage + Recovery`。
- 运行时代码迁移前先创建 SDD，先做字段审计和 FeatureSpec，不直接全仓改 `Data.Get/Set`。
