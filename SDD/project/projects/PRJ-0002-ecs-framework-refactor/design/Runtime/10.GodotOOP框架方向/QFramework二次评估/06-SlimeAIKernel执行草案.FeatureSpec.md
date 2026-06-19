# SlimeAI Kernel 执行草案 FeatureSpec

> 状态：draft / candidate  
> Source Design:
> - [`README.md`](./README.md)
> - [`04-Architecture与Command改造方案.md`](./04-Architecture与Command改造方案.md)
> - [`../Data/08-Command与数据修改入口.md`](../Data/08-Command与数据修改入口.md)

## 目标和非目标

目标：

- 用一个最小 SlimeAI-native Kernel 切片验证 QFramework-inspired 方向。
- 统一一个 owner 的 Command / Query / Event 语义。
- 提供 Data 当前值订阅能力。
- 不破坏现有 SystemManager、DataOS、EventBus 和 Feature owner。

非目标：

- 不引入 QFramework 源码或依赖。
- 不全仓改名 `ECS -> SlimeAIFramework`。
- 不迁移所有 System / Data / Event。
- 不做 undo/replay/async command bus。
- 不替代 `SystemManager.Execute`。

## Feature List

| ID | Feature | Priority | Status | Notes |
| --- | --- | --- | --- | --- |
| KF-1 | KernelCommandDispatcher 最小原型 | P0 | planned | 委托现有 SystemManager 或 owner handler。 |
| KF-2 | Command / Event / Query 命名和 trace 契约 | P0 | planned | 先覆盖 Health/Damage/Recovery 文档和测试。 |
| KF-3 | Data SubscribeWithCurrentValue | P0 | planned | 学 QFramework `RegisterWithInitValue`，不引 BindableProperty。 |
| KF-4 | FeatureManifest 草案 | P1 | planned | 先文档/JSON/Markdown，后续再考虑生成。 |
| KF-5 | 能力接口原型 | P1 | planned | 只给 CommandContext / QueryContext / DataBindingContext。 |

## KF-1: KernelCommandDispatcher 最小原型

### Goal

提供一个统一入口，让非 system owner 和 system owner 的 command 都能被追踪、阻断和验证。

### Behavior

Given 一个 `DamageProcessRequest`  
When 通过 Kernel dispatcher 执行  
Then dispatcher 应委托 `SystemManager.Execute<DamageService, DamageProcessRequest, DamageProcessResult>`，保留现有运行条件阻断结果，并输出 command trace。

Given 一个 owner-local command  
When 该 command 不属于 managed system  
Then dispatcher 应调用注册的 owner handler，并输出同形状 report。

### Implementation Guidance

- Owner: Runtime/Command 或 Runtime/Kernel。
- Key areas:
  - `Src/ECS/Runtime/System/SystemManager.cs`
  - `Src/ECS/Runtime/System/Lifecycle/ISystem/ISystemCommandHandler.cs`
  - `Src/ECS/Capabilities/Damage/System/DamageProcessCommand.cs`
  - `Src/ECS/Capabilities/Damage/System/DamageService_SystemInfo.cs`
- Public API:
  - 候选 `KernelCommandDispatcher.Execute<TRequest, TResult>(in TRequest request)`
  - 候选 `CommandExecutionReport<TResult>`
- Constraints:
  - 对 managed system 必须复用 `SystemManager.Execute`。
  - request/result 优先用 readonly record struct。
  - 不用 string command id 作为主路由。
- Forbidden:
  - 不新增 `AbstractCommand`。
  - 不让 Command 对象自己持有 handler 逻辑。
  - 不绕过 System run condition。

### TDD Handoff

- expectedInputs: `DamageProcessRequest` 在 `FrontEnd` / `SessionPlaying` 两种状态执行。
- expectedObservations: blocked reason code 与现有 `SystemManager.Execute` 一致；成功时 Damage pipeline 仍执行。
- passCriteria: build 通过；SystemCore command gate 测试通过；新增 dispatcher 测试能证明委托路径。
- failCriteria: 直接调用 `DamageService.Instance.Process` 或丢失 blocked reason code。

## KF-2: Command / Event / Query 命名和 trace 契约

### Goal

让“意图、事实、读取”在代码和日志里不混用。

### Behavior

- `Requested` 类事件需要标记为 command event 或迁移为 request。
- `Applied / Changed / Finished` 表示事实 event。
- Query 只能返回只读值或 readonly view。

### Implementation Guidance

- Owner: Runtime/Event + Runtime/Command + affected capability owner。
- Key areas:
  - `DocsAI/ECS/Runtime/Event/`
  - `DocsAI/ECS/Runtime/System/`
  - `DocsAI/ECS/Capabilities/Damage/`
  - `DocsAI/ECS/Capabilities/Unit/`
- Constraints:
  - 文档先行，代码只改最小切片。
  - 现有事件不做大规模机械改名。

### TDD Handoff

- expectedInputs: Health/Damage/Recovery command/event/query 清单。
- expectedObservations: grep gate 能区分 request 和 fact event。
- passCriteria: 文档中每个写入口都有 owner；事实 event 不要求返回值。

## KF-3: Data SubscribeWithCurrentValue

### Goal

把 QFramework `RegisterWithInitValue` 的 UI 绑定优势移植到 SlimeAI Data。

### Behavior

Given UI 绑定 `CurrentHp`  
When 调用当前值订阅  
Then handler 立即收到当前 `CurrentHp`，之后每次 `DataChanged<CurrentHp>` 再收到增量。

Given entity 被释放或 binding token 被 dispose  
When Data 再变化  
Then 已释放 handler 不再收到通知。

### Implementation Guidance

- Owner: Runtime/Data + UI binding。
- Key areas:
  - `Src/ECS/Runtime/Data/Data.cs`
  - `Src/ECS/Runtime/Data/Events/GameEventType_Data.cs`
  - `Src/ECS/UI/`
- Public API:
  - 候选 `Data.SubscribeWithCurrentValue<T>(DataKey<T> key, Action<T> handler)`
  - 返回 subscription token。
- Constraints:
  - 使用现有 `Entity.Events` 或 Data changed path。
  - token 必须支持生命周期释放。
- Forbidden:
  - 不引入 `BindableProperty<T>`。
  - 不让 UI 直接写 gameplay Data。

### TDD Handoff

- expectedInputs: Data 初始值、订阅 handler、一次 Data.Set。
- expectedObservations: handler 被调用两次：当前值一次，变化一次。
- passCriteria: 取消订阅后不再调用；不产生全局 event 泄漏。

## KF-4: FeatureManifest 草案

### Goal

让 Feature owner 像 QFramework `Architecture.Init()` 一样可读。

### Behavior

每个试点 owner 至少列出：

- authoritative Data。
- projection Data。
- commands / requests。
- queries。
- fact events。
- systems / services。
- DataBinding。
- tests。

### Implementation Guidance

- 先写 Markdown manifest，不急着生成。
- 第一批可放在 `DocsAI/ECS/Capabilities/Damage/` 或 `DocsAI/ECS/Capabilities/Unit/`。
- 不把 manifest 当 runtime 配置源。

### TDD Handoff

- passCriteria: AI 能从 manifest 定位写入口和验证入口；manifest 不复制 DataOS 字段全量表。

## KF-5: 能力接口原型

### Goal

用 QFramework `ICanXxx` 思路减少越界调用。

### Behavior

- Command handler context 有写 owner state 的能力。
- Query context 没有写能力。
- UI binding context 只有读和订阅能力。

### Implementation Guidance

- 不复制 QFramework 全套接口名。
- 第一刀只做上下文接口，不改所有 owner。
- 能力接口必须带来编译期限制，否则不新增。

### Review Checklist

- 没有引入 QFramework 依赖。
- 没有新增 static generic Architecture 根。
- managed system command 仍走 `SystemManager.Execute`。
- DataOS descriptor / generated `DataKey<T>` 仍是字段定义事实源。
- Event 保持 typed scoped，不回退全局广播。
- 新 API 能解释得比旧文档更短。

