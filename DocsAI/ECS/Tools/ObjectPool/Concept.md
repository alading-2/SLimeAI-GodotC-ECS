# ObjectPool 概念

> status: current
> sourcePaths: Src/ECS/Tools/ObjectPool/
> relatedDocs: DocsAI/ECS/Tools/ObjectPool/README.md, DocsAI/ECS/Tools/ObjectPool/Usage.md, DocsAI/ECS/Capabilities/Collision/Concepts/对象池碰撞兼容说明.md
> lastReviewed: 2026-06-02

## 1. 一句话定位

通用 C# 对象池，负责对象复用、容量、池归属、基础 Node 出入池状态；针对 Godot 4.x 的 `CollisionObject2D` 根节点实现 detach-isolate-reattach 模式，支持两阶段激活和 `IPoolable` 生命周期。

当前裁决：**保留对象池，继续对物理根节点默认脱树；后续重构重点是把 Node lifecycle 与 Collision isolation 显式拆成内部策略，不改变 public API 和现有行为。**

## 2. 核心概念

### Detach-Isolate-Reattach 模式

针对 Godot 物理引擎的碰撞对生命周期问题：
- **回池**：先移动到泊车位 → 脱树（set_space(null) 彻底清空物理状态）
- **出池**：先挂树（碰撞关闭）→ Entity pipeline 设置 Data / Visual / Transform / Component → Activate 恢复碰撞

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
| `CollisionObject2D` 根节点泊车、脱树、挂树后同步禁用 | 判断 `entered/exited` 是否业务有效 |
| `Get(false)` / `Activate()` 两阶段生命周期 | 组件注册、lifecycle parent、GlobalEvent 发送 |

## 4. AI-first 当前判断

ObjectPool 当前问题不是“使用对象池本身错误”，而是策略藏在一个泛型工具类中。AI 读代码时容易把以下职责混在一起：

- 复用工具职责：`Stack<T>`、容量、预热、统计。
- Godot Node 生命周期：显隐、处理模式、ParentPath、QueueFree/Free。
- Godot 物理隔离：泊车、脱树、同步禁用、deferred 恢复。
- Entity 编排配合：`Get(false)`、`Activate()`、`MoveAndSlide` 时序。

后续重构应拆出内部策略：

```text
ObjectPool<T>
  -> PoolNodeLifecycleStrategy
  -> CollisionIsolationStrategy
  -> PoolLifecycleContext
  -> PoolNodeStateSnapshot / Observation
```

这类拆分是为了可读、可测、可观测，不是为了引入新的 public abstraction。

## 5. 依赖关系

- **IPoolable**：池化对象接口
- **ObjectPoolManager**：全局池管理
- **EntitySpawnPipeline / EntityManager**：Entity 注册、初始化和最终激活
- **Collision Capability**：Area2D 信号桥接、事件过滤和 layer/mask 约定
- **Godot 物理引擎**：碰撞对、监控队列和物理帧时序
