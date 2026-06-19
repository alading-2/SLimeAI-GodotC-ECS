# Command 与数据修改入口

> 用户原始问题：[`source-request.md`](./source-request.md)
> 入口：[`README.md`](./README.md)

## 为什么需要单独分析 Command

用户提出的关键问题是：

```text
数据要分开放。
如果其他地方要数据读写，怎么办？
不能直接获取 Component 然后改数据，这样会耦合。
QFramework 用 Command / Query / Model 管数据，是否值得参考？
Command 和 Event 好像相似。
```

这个问题是真问题。只说“字段放 Component”会立刻遇到修改入口问题：

- 外部不能直接 `GetComponent<HealthComponent>().CurrentHp -= damage`。
- 外部也不应该到处 `entity.Data.Set(CurrentHp, value)`。
- 如果每个修改都写一个 QFramework 风格 Command 类，又会文件爆炸。

SlimeAI 需要自己的中间路线。

## Command 和 Event 的区别

两者都能解耦调用者和执行者，但语义不同。

| 概念 | 语义 | 是否要求有人处理 | 是否修改状态 |
| --- | --- | --- | --- |
| Command | “请执行这个动作” | 通常需要明确 handler | 可能修改 |
| Event | “这件事发生了” | 可以无人监听 | 不应要求返回结果 |
| Query | “请给我读一个结果” | 需要明确响应者 | 不修改 |

直观例子：

```text
DamageCommand:
  对目标造成 10 点伤害。

DamageApplied event:
  伤害已经结算，实际造成了 8 点。

GetCurrentHp query:
  查询目标当前 HP。
```

Command 是意图，Event 是事实，Query 是读取。

## 为什么不照搬 QFramework Command

QFramework 的 Command 解决的是 Controller 变臃肿的问题：

```text
Controller.SendCommand(new AddGoldCommand())
  -> Command.OnExecute()
  -> Model.Gold.Value += 10
  -> SendEvent(GoldChanged)
```

这个思想有价值：写操作集中、可测试、可追踪。

但 QFramework 的对象体系不适合 SlimeAI：

- 每个操作一个引用类型 Command 类，文件数和 GC 压力都会上升。
- Command 依赖 Architecture / Model / Query 体系。
- 示例项目里规范不能强制，Model 可以被跳过。
- SlimeAI 已有 EventBus、DataKey、Service Pipeline、Log、Validation，不需要复制整套架构。

## SlimeAI 采用的 Command 形态

SlimeAI 可以使用 Command，但形态应是：

```text
typed command/request record
  -> owner handler / Feature API / Service Pipeline
  -> Data / Component / System authority 写入
  -> DataChanged / typed Event / Log / Validation
```

也就是：

- Command 是数据载体，不一定是可执行对象。
- Handler 属于 Feature owner 或 System owner。
- Pipeline 用于需要多阶段校验和处理的复杂操作。
- Event 用于处理完成后的通知。

## 推荐命名

| 场景 | 推荐命名 |
| --- | --- |
| 用户/AI/系统请求一次动作 | `XxxCommand` 或 `XxxRequest` |
| 有返回值的处理结果 | `XxxResult` |
| 多处理器链 | `XxxService.Process(command)` / `Pipeline` |
| 事实通知 | `XxxApplied` / `XxxChanged` / `XxxFinished` Event |
| 只读查询 | `XxxQuery` / readonly view |

`Command` 可以用；如果不需要强调命令模式，也可以用 `Request`。同一 owner 内保持一致即可。

## 写入规则

### Data authoritative 字段

不能随处直接写。推荐：

```text
DamageCommand
  -> DamageService.Process(command)
  -> HealthFeature.ApplyDamage(result)
  -> Data.Set(CurrentHp)
  -> DataChanged(CurrentHp)
  -> DamageApplied event
```

允许直接 `Data.Set` 的地方：

- owner 内部 handler。
- loader / bootstrap。
- debug/test 边界。
- migration helper。

不允许：

- 任意 Component 看到 Data 就写。
- UI 直接改 gameplay Data。
- 外部直接写 projection 字段。

### Component authoritative 字段

不能通过 Data 改。推荐：

```text
AttackCommand
  -> AttackComponent / AttackFeature handler
  -> AttackComponent 改 _state
  -> Project AttackState to Data
  -> AttackStarted / AttackFinished event
```

外部读：

- 读 query。
- 读 Data projection。
- 订阅 typed event。

外部写：

- 发 Command / Request。
- 不直接拿 Component 改字段。

### System authoritative 字段

由 System handler 写：

```text
FindTargetCommand
  -> TargetingSystem
  -> TargetNode projection / query result
```

其他 owner 不直接改系统索引。

## Query 的角色

Query 解决“不能直接拿 Component 改值”之外的另一个问题：读也要控制耦合。

推荐 Query 形态：

```text
HealthQuery.GetCurrentHp(object)
TargetQueryEngine.Query(query)
AbilityInventoryService.TryGetOwnedItem(owner, abilityId, out item)
```

Query 可以返回只读 view 或值对象，不返回可随意修改的内部对象。

不建议：

- Query 返回 Component 后让调用方改字段。
- Query 返回 mutable collection 后调用方长期持有。
- 用 string key + object 作为通用查询协议。

## 和 DataBinding 的关系

Command 不替代 DataBinding。

```text
Command 控制谁能改。
DataBinding 控制改完后怎么同步 mirror。
Event 控制改完后怎么通知。
Query 控制别人怎么读。
```

例如：

```text
HealCommand
  -> HealthFeature.Handle(command)
  -> Data.Set(CurrentHp)
  -> DataChanged(CurrentHp)
  -> DataBinding updates HealthComponent mirror
  -> HealApplied event
```

没有 DataBinding，Component mirror 会旧。
没有 Command/owner API，写入口会乱。
没有 Event，其他系统不知道事实发生。
没有 Query，读路径会退化成到处拿对象。

## 对当前代码的影响

当前代码里已经存在一些接近 Command 的形态：

- `DamageProcessRequest` / `DamageProcessResult` 是 typed request/result。
- `DamageService` 是 Service Pipeline。
- `RecoveryRegisterRequest` / `RecoveryUnregisterRequest` 是 system command。
- Attack 的 `GameEventType.Attack.Requested` 目前是命令事件，语义上更接近 Command。

后续不需要推翻这些名字，但要统一语义：

- `Requested` 类事件可以逐步改为 Command / Request 或明确标记为 command event。
- `Applied` / `Finished` / `Changed` 才是事实 Event。
- 写共享 Data 的调用点要归 owner。

## 首批建议

Health/Damage/Recovery 切片中建议定义：

```text
ApplyDamageCommand
  Target
  Source
  Amount
  Tags

ApplyHealCommand
  Target
  Source
  Amount

HealthMutationResult
  Applied
  OldHp
  NewHp
  IsDeadChanged
```

处理路径：

```text
DamageService.Process(ApplyDamageCommand)
  -> Health owner 写 CurrentHp
  -> DataChanged(CurrentHp)
  -> DamageApplied / DeathTriggered event

RecoverySystem
  -> ApplyHealCommand
  -> Health owner 写 CurrentHp
```

Mana 是否也放进 Health owner 需要另行命名，可能是 `ResourceFeature` 或 `UnitResourceProfile`。

## 本页裁决

```text
Command 可以用。
SlimeAI 不照搬 QFramework 的 AbstractCommand 对象体系。
Command / Request 是 typed intent，Event 是 typed fact，Query 是 readonly read。
共享 Data 写入必须走 owner API / Command handler / Service Pipeline。
Component authoritative 字段只能由 owner 改，再投影给 Data。
```
