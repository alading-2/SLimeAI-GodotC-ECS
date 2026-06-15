# QFramework 架构学习与采纳边界

> 原始问题：见 [`source-request.md`](./source-request.md)。  
> 主要证据：`Resources/Engine/Engine/QFramework/README.md`、`QFramework.cs`、`QFramework API.md`、Context7 `/liangxiegame/qframework`、临时 clone `fanqie404/FrameworkDesign`、本地报告 `Resources/Engine/Docs/FrameworkAnalysis/Reports/QFramework/`。

## QFramework 是什么

QFramework 是 Unity / Godot 上的应用层架构框架。它的 README 明确强调 SOLID、DDD、事件驱动、数据驱动、分层、MVC、CQRS、模块化和易扩展，并且核心 `QFramework.cs` 可以作为不到千行的单文件引入。

它不是完整底层 ECS runtime，也不是 SlimeAI 这种 DataOS + per-entity runtime state protocol。

核心结构是：

```text
Architecture<T>
  -> RegisterSystem / RegisterModel / RegisterUtility
  -> SendCommand / SendQuery / SendEvent
  -> IOCContainer(Type -> object)
  -> TypeEventSystem(Type -> EasyEvent)
```

核心角色是：

```text
IController  表现层，接收输入和表现变化
IModel       数据层，保存共享数据
ISystem      系统层，处理跨表现层业务逻辑
IUtility     工具层，封装基础设施
ICommand     写操作
IQuery       读操作
Event        下层通知上层
BindableProperty<T> 数据 + 变更通知
```

## 它为什么显得成熟

QFramework 的成熟不是来自复杂数据结构，而是来自清晰规则。

FrameworkDesign 的 README 把规则写得很直接：

```text
IController 更改 ISystem / IModel 状态必须用 Command。
ISystem / IModel 状态发生变更后通知 IController 必须用 Event 或 BindableProperty。
IController 可以获取 ISystem / IModel 做数据查询。
ICommand 不能有状态。
上层可以直接获取下层，下层不能获取上层。
下层向上层通信用事件。
上层向下层通信用方法调用；状态变更用 Command。
```

这些规则对 SlimeAI 很重要。SlimeAI 当前的 Data 复杂化，部分原因就是缺少类似“什么状态应该进入共享协议，什么状态留在 owner 内部”的简单判断标准。

QFramework 给出的判断是：

```text
需要共享的数据 -> Model
跨表现层业务逻辑 -> System
改变状态的交互逻辑 -> Command
只读查询 -> Query
基础设施 -> Utility
```

SlimeAI 可以把这个判断翻译为：

```text
跨 Capability 共享、需要验证追溯的 runtime state -> Data
单 Capability 内部临时状态 / cache / index -> owner service 或 component
改变 gameplay 状态的请求 -> Capability service / event request / runtime command handler
只读查询 -> selector / query facade / projection
基础设施 -> Tools / GodotBridge / ResourceLoading / Timer
```

## QFramework 数据为什么简单

QFramework 没有 SlimeAI 当前这种类型转换问题，因为它的数据通常是 C# 代码里的强类型字段：

```text
Model C# 字段
  -> BindableProperty<T>
  -> Command / Query / System 访问
  -> Event 或 BindableProperty 通知
```

本地源码证据：

- `BindableProperty<T>` 内部保存 `protected T mValue`。
- `IOCContainer` 使用 `Dictionary<Type, object>` 保存模块实例，但取出后回到 `T`。
- `EasyEvents` 使用 `Dictionary<Type, IEasyEvent>` 保存事件表，但事件 payload 仍由泛型类型约束。

这说明 QFramework 并不排斥字典或 `object`。它只是把它们限制在注册表和事件表边界，不让它们承担“业务字段类型系统”。

这对 SlimeAI Data 的启发非常直接：

```text
Dictionary<string, IDataSlot> 可以作为 stableKey 索引。
object 可以留在 loader/debug/diagnostic 边界。
业务 payload 必须尽快回到 DataKey<T> / DataSlot<T>。
```

## SlimeAI 应该学什么

### Adopt Now

- 学 QFramework 的少量硬规则。Data 进入条件、Capability 边界、Event 边界都应有类似可记忆的规则。
- 学 `Architecture.Init()` 的可读性。SlimeAI 的 `CapabilityIndex` / manifest / generated report 应像架构图一样一眼看出有哪些模块。
- 学 Command/Query 的读写分离表达，但落点应是 SlimeAI 现有 Capability service / request handler，不新增 QFramework 对象层。
- 学 BindableProperty 的 `RegisterWithInitValue` 语义。UI / Debug / bridge 订阅 Data 时应能“注册时立即得到当前值”，而不是等下一次变化。
- 学渐进式教程路径。SlimeAI 需要一个极小可运行学习切片，让新开发者先理解 Entity/Data/Event/Capability 的一圈闭环。

### Adopt Later

- 参考 Command / Query 术语整理 SlimeAI 请求和查询文档，但只有现有 API 表达不清时才引入。
- 参考 QFramework Toolkits 的模块包思路，改进 Capability 文档和验证入口，不复制 Unity toolkit。
- 参考 FrameworkDesign 的纸上设计方式，把大型 Runtime 改动先画成角色关系图或清单。

### Reject

- 不引入 `Architecture<T>` 静态单例。SlimeAI runtime 需要实例化、可测试、可跨场景清理。
- 不把 Godot Node 变成 QFramework `IController`。Godot Node 是 bridge / view / scene lifecycle 承载者，不是 gameplay controller。
- 不引入 QFramework `ICommand` 对象层替代现有 Capability service / runtime command handler。
- 不用 `TypeEventSystem.Global` 替代 `Entity.Events` / `WorldEventBus`。
- 不用 `BindableProperty<T>` 替代 Entity.Data。
- 不把 QFramework Godot CounterApp 当作 SlimeAI GodotBridge 参考实现。

## 对 Data 的直接影响

QFramework 支持 `5.Data类型系统重构/09-Data系统根本裁决与重构路线.md` 的方向：

```text
Data 不应该承担所有状态、展示、权限、策略和类型恢复。
共享状态需要明确进入条件。
业务读写路径必须强类型。
动态索引只做路由，不做 payload 类型系统。
架构规则越少越好，能通过文档 / validator / test / code review 控制的，不要都塞进 runtime Set。
```

所以 QFramework 不是 Data 的替代品，而是 Data 简化的反面教材和正面教材同时存在：

- 正面：强类型字段、少规则、清晰层级。
- 反面：如果直接照搬，会丢失 SlimeAI 的 DataOS / snapshot / generated key / per-entity capability 追溯。

