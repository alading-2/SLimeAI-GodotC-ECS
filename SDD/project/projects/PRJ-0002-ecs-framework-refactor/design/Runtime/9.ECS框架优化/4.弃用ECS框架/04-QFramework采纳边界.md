# QFramework 采纳边界

> 原始问题：用户要求重新判断 QFramework 的数据结构好不好、是否适合做大框架、是否适合解耦。  
> 证据：Context7 `/liangxiegame/qframework`、本地源码 `/home/slime/Code/SlimeAI/Resources/Engine/Engine/QFramework/Source/`、本地分析报告 `/home/slime/Code/SlimeAI/Resources/Engine/Engine/QFramework/Docs/`。

## 结论

QFramework 值得学习，但不应直接作为 SlimeAI 底层框架。

它适合回答：

```text
应用层架构如何分层？
如何用少量规则控制耦合？
Command / Query / Event / Model 如何组织？
强类型状态如何避免 Data 类型恢复复杂度？
```

它不适合直接回答：

```text
Godot 对象生命周期怎么统一？
对象池、碰撞、场景树、输入、物理怎么接入？
多游戏玩法 Feature 如何裁剪启停？
per-object runtime state 如何 authoring / validation / save/load？
AI-first 文档和验证如何闭环？
```

## QFramework 的数据结构为什么更简单

QFramework 常见数据路径是：

```text
Model C# class
  -> BindableProperty<T>
  -> Command / Query / System
  -> Event 或 property change notification
```

`BindableProperty<T>` 内部保存的是强类型 `T`。QFramework 也使用 `Dictionary<Type, object>`、`Dictionary<Type, IEasyEvent>`，但这些字典主要用于 IOC 注册表和事件表，不承担业务字段类型系统。

这说明 QFramework 避免 SlimeAI Data 复杂度的关键不是“用了某种高级数据结构”，而是：

```text
业务状态直接是 C# 强类型字段。
动态索引只做注册和路由。
规则少，职责清楚。
```

本轮重新核对了 Context7 `/liangxiegame/qframework` 和本地源码。核心类型集中在：

```text
IArchitecture / Architecture<T>
IModel / ISystem / IUtility
ICommand / IQuery
TypeEventSystem
BindableProperty<T>
```

这些结构说明 QFramework 的强项是应用层分层、类型注册表、命令/查询/事件和可观察属性。它没有提供 SlimeAI 需要的 Godot scene lifecycle、对象池、碰撞、目标查询、DataOS authoring、per-object Data runtime、AI validation artifact 等底层能力。

## 它是否适合做大框架

适合做应用层大框架的一部分，不适合作为 SlimeAI 底层总框架。

适合的原因：

- 分层规则清楚。
- Command / Query / Event 能减少表现层和逻辑层直接耦合。
- Model 强类型状态简单。
- Architecture.Init 像架构图，可读性强。
- 教学路径好，适合人和 AI 理解。

不适合直接采用的原因：

- `Architecture<T>` 静态单例不适合 Godot 多场景、测试隔离和运行时重置。
- `IController` 把引擎组件变成控制器，容易让 Godot Node 承担过多业务入口职责。
- `ICommand` 对象层会增加 GC 和 AI 路由成本；SlimeAI 可用 service method / event request / handler 代替。
- `TypeEventSystem.Global` 不表达 per-object / feature / world scope。
- `BindableProperty<T>` 是属性通知工具，不是完整 per-object shared state / authoring / validation 系统。
- Godot4+ 版本更像 CounterApp 演示，不覆盖 SlimeAI 需要的对象生命周期、碰撞、对象池、scene bridge。

## 对 SlimeAI 应采纳什么

### Adopt Now

- 学少规则分层：Feature 内部明确 Component、System、Model/State、Event、Query 的职责。
- 学强类型状态：能用 C# 字段/属性表达的状态，不绕到动态 Data。
- 学 Command/Query 的读写分离：写操作有明确入口，读操作有明确查询 facade。
- 学事件上行：底层状态变化通过事件通知上层，不让上层轮询或直接摸内部字段。
- 学 `RegisterWithInitValue` 语义：UI/Debug 订阅状态时应能立即拿到当前值。
- 学架构即文档：Feature 注册清单必须像架构图一样清楚。

### Adopt Later

- 如果 Feature 之间读写规则混乱，再引入轻量 Command/Query 术语。
- 如果 UI binding 需求变大，参考 BindableProperty 的初值通知和静默设置。
- 如果项目需要插件化 Feature manifest，参考 QFramework package/toolkit 的模块元数据思路。

### Reject

- 不把 QFramework 作为依赖直接接入。
- 不引入 `Architecture<T>` 静态单例。
- 不要求 Godot Node 实现 `IController`。
- 不用 `BindableProperty<T>` 替代 Data。
- 不用 QFramework 全局事件总线替代 SlimeAI 事件边界。
- 不把 QFramework Godot CounterApp 当作 GodotBridge 方案。

## 对 Data 重写的启发

QFramework 支持一个重要结论：

```text
SlimeAI Data 的问题不是没有找到更复杂的数据结构，
而是没有把动态索引和业务强类型状态分开。
```

Data 重写应优先考虑：

- 默认状态直接放 Component / Feature model。
- 共享、表格驱动或需要验证追踪的状态才进 Data。
- Data 也必须尽快回到强类型 API。
- 字典只做索引和路由。
- authoring / validation / presentation 不进入热路径对象。

## 当前 Unknown

- 没有运行 QFramework Unity/Godot 示例，只做源码和文档级分析。
- 没有评估 QFramework 在大型商业项目中的长期维护成本。
- 没有证明 QFramework 的 Command/Query 对 SlimeAI 一定收益大于成本。

这些 Unknown 不影响当前裁决：QFramework 可学习，不直接接入。
