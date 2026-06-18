# Event 驱动与生命周期规则

## 结论

Event 驱动仍是 SlimeAIFramework 的基础。弃用 ECS 不等于回到组件互相直接调用实现细节。

推荐模型：

```text
局部同步调用：同 owner 内、明确 API、无跨生命周期风险时允许。
Object 内通信：typed local Event。
跨 Feature 通信：typed Event / Message。
Godot Signal：表现层、UI、动画、编辑器和引擎桥接。
```

## Event 要解决什么

Event 负责这些问题：

- 避免 Component 互相持有具体实现。
- 让 Feature 之间只依赖稳定 payload。
- 让日志、测试和 AI 能看到行为发生过。
- 让生命周期解绑可被检查。
- 让 Data changed 能同步给 Component、UI 和 debug。

## Typed payload 是硬规则

新 Event 必须有强类型 payload。不要恢复 string event name + object payload 的主链路。

```text
DamageRequested(DamageRequest request)
DamageApplied(DamageAppliedResult result)
DataChanged<T>(DataKey<T> key, T oldValue, T newValue)
```

Godot Signal 的 payload 受 Variant 约束，适合 UI/动画/表现层，不适合作为核心玩法协议主线。

## 生命周期绑定

任何 Event 订阅必须绑定生命周期：

| 生命周期 | 必须处理 |
| --- | --- |
| Component registered | 建立 local event / Data changed / global event 订阅 |
| Component disabled | 暂停或忽略需要 active 的回调 |
| Component unregistered | 解绑所有订阅、Timer 和外部引用 |
| Object pooled | 清理旧 source 和对象池复用状态 |
| Object destroyed | 清理 EventBus、DataBinding、modifier source 和 diagnostics |

这条规则比“所有通信必须走 Event”更重要。没有生命周期清理的 Event 会让对象池复用、场景切换和测试污染。

## Data changed 是 Event 的一种

Data 字段发生变化时，Data 应发出 typed changed event。DataBinding、UI、debug panel 和测试可以订阅它：

```text
Data.Set(MoveSpeed, 3.5)
  -> DataChanged<float>(MoveSpeed, old, new)
  -> MovementComponent.SetMoveSpeed(new)
  -> Debug panel / ValidationSession record
```

这让 Data 和 Component 字段同步不需要轮询，也不需要每帧在 Component 中读 Data。

## 对象池与 Event

对象池复用时最容易出错的不是 Data 值本身，而是旧订阅、旧 timer、旧 modifier source 和旧碰撞回调还活着。

复用规则：

- Release 时先停用，再解绑，再清本地状态。
- Acquire 时先刷 Data，再绑定 Component，再激活。
- 激活第一帧可以保留 embargo，避免旧碰撞状态在 Data/Component 尚未完全同步时触发。
- EventBus / DataBinding 必须输出可诊断的订阅数量和 owner。

## 允许直接调用的场景

不是所有通信都要 Event。以下场景可以直接调用：

- 同一个 Feature 内的 private helper。
- Component 调用自己 owner System 的明确 API。
- 没有异步、生命周期或跨 owner 风险的同步查询。
- 性能热点里经过 profiler 证明 Event 分发成本不可接受。

直接调用必须依赖接口或 owner API，不依赖另一个 Component 的具体实现细节。

## 不推荐方向

- 不推荐用 Godot Signal 承载核心玩法协议。
- 不推荐 string event name + object payload。
- 不推荐 Component 直接持有另一个 Component 并跨生命周期调用。
- 不推荐 Data changed 只做 debug，不接入 Component 同步。
- 不推荐对象池复用时跳过订阅和 modifier source 清理。

