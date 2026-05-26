# Event — 类型驱动的事件系统

## 架构概览

事件系统采用 **typed event kernel** 设计，事件主键 = payload 类型（T），scope 由 marker interface 决定。

```text
IEvent (基础 marker)
├── IEntityEvent       → EntityEventBus 接受，WorldEventBus 拒绝
├── IGlobalEvent       → WorldEventBus 接受，EntityEventBus 拒绝
└── IBroadcastEvent    → EntityEventBus 接受 + 自动路由到 WorldEventBus
```

## 目录结构

| 子目录 | 职责 | 文件 |
| ------ | ---- | ---- |
| `Contract/` | 接口契约 | `IEvent.cs`, `IEventBus.cs`, `IWorldEventBus.cs`, `IWorldEventBusRouter.cs` |
| `Bus/` | 总线实现与入口 | `EntityEventBus.cs`, `WorldEventBus.cs`, `WorldEvents.cs` |
| `Observation/` | 可观测数据 | `EventBusObservation.cs` |
| `Utility/` | 工具类 | `EventBusNames.cs`, `EventSubscriptionCollector.cs` |
| `Payload/` | 框架级事件定义 | `GlobalEvents.cs` |

## 核心流程

### 发布

```text
EntityEventBus.Publish<T>
  1. Scope 检查：T 必须实现 IEntityEvent
  2. 本地派发：按注册顺序调用 handler（重入保护 + 异常隔离）
  3. 广播路由：T 同时实现 IGlobalEvent → 自动调用 WorldEventBus.RouteBroadcast

WorldEventBus.Publish<T>
  1. Scope 检查：T 必须实现 IGlobalEvent
  2. 本地派发
```

### 订阅 / 退订

```text
bus.Subscribe<T>(handler) → IDisposable token
  - token.Dispose() 退订
  - 推荐使用 EventSubscriptionCollector 批量管理
  - Entity 释放时 bus.Clear() 清空订阅列表
```

### Observation

```text
bus.ExportObservation(path)
  → 生成 eventbus-dump.json
  → 包含：订阅列表、发布计数、重入阻断、handler 异常、注册顺序
```

## 事件 payload 归属

| 归属 | 位置 | 示例 |
| ---- | ---- | ---- |
| 框架级全局事件 | `Event/Payload/GlobalEvents.cs` | `EntitySpawned`, `GameStart` |
| 模块专属事件 | 跟随 owner 模块目录 | `AbilityEvents.cs`, `UnitEvents.cs`, `CollisionEvents.cs` |
| 游戏专属事件 | 游戏侧事件目录 | 不放框架核心 |

## 旧 API 已删除

以下旧 API 不再保留：`GameEventType`, `EventContext`, `GlobalEventBus`, `EventPriority`, `On`, `Off`, `Once`, `Emit`。

## 使用示例

```csharp
// 构造实体事件总线（传入 WorldEvents.World 启用广播路由）
public IEventBus Events { get; } = new EntityEventBus("entity", WorldEvents.World);

// 订阅实体局部事件
var token = Events.Subscribe<UnitEvents.Damaged>(OnDamaged);

// 订阅全局事件（通过 EventSubscriptionCollector）
_eventSubscriptions.Subscribe<GlobalEvents.GameStart>(WorldEvents.World, OnGameStart);

// 发布实体局部事件
Events.Publish(new UnitEvents.Damaged(10));

// 发布广播事件（自动路由到 WorldEventBus）
Events.Publish(new UnitEvents.Killed(victim: this));

// 发布全局事件
WorldEvents.World.Publish(new GlobalEvents.GameStart());

// 退订
token.Dispose();
_eventSubscriptions.Clear();
```
