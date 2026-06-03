# ObjectPool 概念

> status: current
> sourcePaths: Src/ECS/Tools/ObjectPool/
> relatedDocs: DocsAI/ECS/Tools/ObjectPool/README.md, DocsAI/ECS/Tools/ObjectPool/Usage.md, DocsAI/ECS/Capabilities/Collision/Concepts/Godot物理时序与对象池碰撞.md
> lastReviewed: 2026-06-03

## 1. 一句话定位

通用 C# 对象池，负责对象复用、容量、池归属、基础 Node 出入池状态；针对 Godot 4.x 的 `CollisionObject2D` 根节点默认采用 `ParkedInTree` 场外常驻策略，支持两阶段激活、pool runtime state、激活首帧碰撞逻辑禁用和 `IPoolable` 生命周期。

当前裁决：**保留对象池，物理根节点默认不脱树、不关碰撞、不改 layer/mask/shape；后续重构重点是把 Node lifecycle、parking grid、ObjectPoolRuntimeStateStore、CollisionLogicGuard 和 fallback detach 显式拆成内部策略，不改变 public API。**

## 2. 核心概念

### ParkedInTree 场外常驻策略

针对 Godot 物理引擎的碰撞对生命周期问题，默认不再切换物理参与状态：

- **回池**：标记 `IsInPool=true`、`CollisionLogicActive=false`，隐藏、停处理，移动到分散 parking grid。
- **出池**：`Get(false)` 后由 Entity pipeline 设置 Data / Visual / Transform / Component。
- **激活**：`Activate()` 标记 `CollisionLogicActive=true`，设置 `CollisionReadyPhysicsFrame=currentPhysicsFrame+1`，激活第一帧不处理业务碰撞。

这条策略的重点是避免默认 `RemoveChild/AddChild` 和 layer/mask/shape/monitoring 反复切换，让旧 signal / 停车区事件在业务入口被 guard 丢弃。

### 两阶段激活

```
pool.Get(false)  → 取出对象，不触发 OnPoolAcquire
pool.Activate()  → 激活对象，触发 OnPoolAcquire
```

两阶段激活的 owner 是 Entity Runtime：

```text
ObjectPool.Get(false)
  -> EntitySpawnPipeline Apply Data / Visual / Transform / Component
  -> ObjectPool.Activate()
  -> CharacterBody2D CallDeferred(MoveAndSlide)
```

ObjectPool 不在 `Get(false)` 阶段提前发业务事件，也不决定这次碰撞是否有效。

### IPoolable 生命周期

```csharp
public interface IPoolable
{
    void OnPoolAcquire();   // 从池中取出并激活后
    void OnPoolRelease();   // 归还池前
    void OnPoolReset();     // 重置（可选）
}
```

### 全局管理器

`ObjectPoolManager` 提供按名查找和静态归还。

- Node 对象通过 `ObjectPoolName` Meta 找回所属池。
- 纯 C# 对象通过 `_objectToPoolMap` 找回所属池。
- `DestroyAll()` 用于场景切换或全局清理，避免旧池继续持有已释放对象。

## 3. 职责边界

| ObjectPool 做 | ObjectPool 不做 |
| ---- | ---- |
| 对象复用、容量、活跃/闲置统计 | 业务逻辑 |
| 池归属映射和静态归还 | Entity 注册、Data 初始化 |
| Node 显隐、ProcessMode、ParentPath 基础挂载 | owner/source/target、阵营、伤害归因 |
| `CollisionObject2D` 根节点 parking grid、pool runtime state、激活首帧 embargo、fallback detach | 判断 `entered/exited` 是否业务有效 |
| `Get(false)` / `Activate()` 两阶段生命周期 | 组件注册、lifecycle parent、GlobalEvent 发送 |

## 4. AI-first 当前判断

ObjectPool 当前问题不是“使用对象池本身错误”，而是策略藏在一个泛型工具类中。AI 读代码时容易把以下职责混在一起：

- 复用工具职责：`Stack<T>`、容量、预热、统计。
- Godot Node 生命周期：显隐、处理模式、ParentPath、QueueFree/Free。
- Godot 物理协作：parking grid、runtime state、激活首帧 embargo、fallback detach。
- Entity 编排配合：`Get(false)`、`Activate()`、`MoveAndSlide` 时序。

后续重构应拆出内部策略：

```text
ObjectPool<T>
  -> PoolNodeLifecycleStrategy
  -> PoolParkingStrategy
  -> ObjectPoolRuntimeStateStore
  -> CollisionLogicGuard
  -> DetachFallbackStrategy
  -> PoolLifecycleContext
  -> PoolNodeStateSnapshot / Observation
```

这类拆分是为了可读、可测、可观测，不是为了引入新的 public abstraction。

## 5. 依赖关系

- **IPoolable**：池化对象接口
- **ObjectPoolManager**：全局池管理
- **EntitySpawnPipeline / EntityManager**：Entity 注册、初始化和最终激活
- **Collision Capability**：Area2D 信号桥接、pool runtime state guard、事件过滤和 layer/mask 约定
- **Godot 物理引擎**：碰撞对、监控队列和物理帧时序
