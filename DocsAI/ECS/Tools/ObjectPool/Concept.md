# ObjectPool 概念

> status: current
> sourcePaths: Src/ECS/Tools/ObjectPool/
> relatedDocs: DocsAI/ECS/Tools/ObjectPool/Usage.md
> lastReviewed: 2026-05-30

## 1. 一句话定位

通用 C# 对象池，针对 Godot 4.x 的 CollisionObject2D 实现 detach-isolate-reattach 模式，防止幽灵碰撞，支持两阶段激活和 IPoolable 生命周期。

## 2. 核心概念

### Detach-Isolate-Reattach 模式

针对 Godot 物理引擎的碰撞对生命周期问题：
- **回池**：先移动到泊车位 → 脱树（set_space(null) 彻底清空物理状态）
- **出池**：先挂树（碰撞关闭）→ 设置位置 → Activate 恢复碰撞

### 两阶段激活

```
pool.Get(false)  → 取出对象，不触发 OnPoolAcquire
pool.Activate()  → 激活对象，触发 OnPoolAcquire
```

### IPoolable 生命周期

```csharp
public interface IPoolable
{
    void OnPoolAcquire();   // 从池中取出并激活后
    void OnPoolRelease();   // 归还池前
    void OnPoolReset();     // 重置（可选）
}
```

### 线程安全全局管理器

ObjectPoolManager 提供静态归还和按名查找。

## 3. 职责边界

| ObjectPool 做 | ObjectPool 不做 |
| ---- | ---- |
| 对象复用与内存管理 | 业务逻辑 |
| 碰撞型实体的物理隔离 | Entity 注册（归 EntityManager） |
| IPoolable 生命周期回调 | 数据注入 |

## 4. 依赖关系

- **IPoolable**：池化对象接口
- **ObjectPoolManager**：全局池管理
- **EntityManager**：Entity 注册与生命周期
- **Godot 物理引擎**：碰撞对管理
