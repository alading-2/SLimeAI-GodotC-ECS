# Architecture 与 Command 改造方案

## 总体原则

不要把 QFramework 的 `Architecture<T>` 当成 SlimeAI 的根。

SlimeAI 应该把它拆成四个独立概念：

```text
Kernel Facade:
  读得懂当前 runtime 有哪些 Feature / System / Data / Event / Command。

Service Registry:
  只注册 owner service / system factory，不存业务字段。

Command Dispatcher:
  执行 typed intent，找到 owner handler 或 pipeline。

Feature Manifest:
  像 QFramework Init() 一样可读，但内容来自 SlimeAI owner。
```

这样既保留 QFramework 的“架构即清单”，又不会让一个单例容器吞掉 DataOS、SystemManager 和 Godot 生命周期。

## QFramework 到 SlimeAI 的映射

| QFramework | SlimeAI 推荐映射 | 说明 |
| --- | --- | --- |
| `Architecture<T>.Interface` | `SlimeRuntimeKernel` / Godot AutoLoad 持有的 runtime instance | 可以有静态访问 facade，但事实源必须是 Godot runtime instance，不是泛型静态单例。 |
| `IOCContainer` | `SystemRegistry`、owner service registry、FeatureHandlerRegistry | registry 只做索引和工厂，不保存 gameplay state。 |
| `IModel` | Data / Profile / Component state / System state | 不再有单一 Model 容器。 |
| `ISystem` | 现有 `SystemManager` 管理的 system / service | 保留运行条件、diagnostics、lifecycle trace。 |
| `IUtility` | Tools / GodotBridge / ResourceLoading / Timer | 保持无上层依赖。 |
| `IController` | Godot Node bridge / UI / Adapter | 不允许成为 gameplay 万能入口。 |
| `ICommand` | `readonly record struct XxxRequest` + handler/pipeline | 消息和处理器分离。 |
| `IQuery` | readonly Query facade / TargetQuery / HealthQuery | 查询不返回可变内部对象。 |
| `TypeEventSystem` | Object / Feature / Runtime scoped `EventBus` | 必须有 scope 和生命周期 token。 |
| `BindableProperty` | DataChanged / Observable readonly view | 保留当前值订阅语义。 |
| `ICanXxx` | CommandContext / QueryContext / FeatureContext 能力接口 | 用编译期限制减少边界绕过。 |

## SlimeAI Architecture 应该长什么样

候选名称不建议叫 `QArchitecture`。推荐：

```text
SlimeRuntimeKernel
SlimeRuntimeContext
SlimeFeatureKernel
KernelManifest
```

它的职责：

- 聚合 `SystemManager`、`DataRuntimeBootstrap`、`RuntimeMountRegistry`、`NodeLifecycleRegistry`、`FeatureHandlerRegistry`。
- 输出只读 manifest / diagnostics。
- 提供 command/query/event/data observable 的统一入口。
- 不直接保存 gameplay 字段。
- 不绕过 Godot Node lifecycle。

它不做：

- 不替代 `SystemManager` 的运行条件和 command gate。
- 不替代 `DataOS` 的 descriptor / snapshot。
- 不替代 Feature owner 的业务逻辑。
- 不持有无 scope 的全局事件总线。

## Command 改造方案

### 推荐形态

SlimeAI 的 Command 是 typed intent，不是可执行对象。

```text
ApplyDamageRequest
  -> DamageCommandHandler / DamageService Pipeline
  -> Data / Component / System authoritative write
  -> DataChanged / DamageApplied Event / Log / Validation
```

原则：

- request/result 用不可变值对象。
- handler 属于 owner。
- 复杂流程走 Service Pipeline。
- dispatcher 负责路由、门禁、trace、错误报告。
- hot path 可以直接调用 owner API，但必须仍在 owner 边界内。

### 与现有 SystemManager 的关系

现有 `SystemManager.Execute<TSystem, TRequest, TResult>` 已经是 Command gate，不应被 QFramework `SendCommand` 替代。

后续可以有两层：

```text
KernelCommandDispatcher
  -> 如果目标是 managed system：委托 SystemManager.Execute
  -> 如果目标是 feature owner：调用 FeatureCommandRegistry handler
  -> 如果目标是 local object/component owner：调用 owner API，并记录 trace
```

这样可以兼容当前 SystemManifest，又能覆盖不是 system 的 Feature / Component 写入口。

### Command 和 Event 的硬边界

```text
Command / Request:
  请做某事。
  通常需要明确 handler。
  可以失败，应有 result 或 report。

Event:
  某事已经发生。
  可以无人监听。
  不应要求返回结果。

Query:
  请给我只读结果。
  不能修改状态。
  不返回 mutable owner 内部对象。
```

这条规则应写进后续 Kernel / Event / Data 文档，成为 grep review 和 code review 的核心。

## 能力接口怎么学 QFramework

QFramework 最值得复制的是：

```text
接口本身没有方法。
扩展方法只挂在特定接口上。
不实现接口就不能调用对应能力。
```

SlimeAI 可以设计类似上下文：

```text
ICanReadData
ICanWriteOwnerData
ICanSendCommand
ICanPublishFactEvent
ICanQueryRuntime
ICanAccessSystemManager
ICanUseDebugWrite
```

示例含义：

- UI context 可以 `ICanReadData` 和 `ICanQueryRuntime`，不能 `ICanWriteOwnerData`。
- loader context 可以 `ICanWriteOwnerData`，但 source 是 Loader。
- Feature handler 可以写自己 owner 的 Data / Component authoritative 字段。
- Query handler 不具备 write 能力。

注意：不要一次性创建很多接口。第一刀只给 Command / Query / UI binding 三类上下文加能力接口即可。

## Architecture 改造失败信号

如果出现下面情况，说明又走回错误方向：

- 新 `Architecture` 类开始保存业务字段。
- Godot Node 大量实现 `IController` 并直接改 Data / Component。
- Command handler 分散在 Command 对象里，owner 找不到写入口。
- Event 重新变成全局广播。
- DataOS descriptor 被 `Model` 或 C# 手写 meta 替代。
- `SystemManager.Execute` 被绕过，系统运行条件失效。

