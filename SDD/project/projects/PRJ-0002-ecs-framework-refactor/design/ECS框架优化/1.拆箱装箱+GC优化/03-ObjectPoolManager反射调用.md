---
title: ObjectPoolManager反射调用问题
status: 初稿
created: 2026-06-06
tags: [ecs, reflection, objectpool, medium]
description: ObjectPoolManager用反射调用Release方法导致的GC分配问题
---

# ObjectPoolManager反射调用问题

> 严重度：🟡 MEDIUM
> 影响范围：每次对象归还到池

## 涉及代码（4处）

### 1. _pools 存储为 Dictionary<string, object>

**文件：** `ECS/Tools/ObjectPool/Management/ObjectPoolManager.cs` 第16行
```csharp
private static readonly Dictionary<string, object> _pools = [];
```
**问题：** 所有ObjectPool<T>实例存储为object，类型信息丢失。

### 2. ReturnToPool 用反射调用 Release

**文件：** `ECS/Tools/ObjectPool/Management/ObjectPoolManager.cs` 第122-128行
```csharp
private static void ReturnToPool(object instance, string poolName)
{
    var poolObj = _pools[poolName];
    var releaseMethod = poolObj.GetType().GetMethod("Release");
    releaseMethod.Invoke(poolObj, new[] { instance }); // 反射 + object[] 分配
}
```

**每次调用的GC开销：** 2-3次堆分配（GetType + GetMethod + object[]）

### 3. GetAllStats / CleanupAll / DestroyAll 使用反射

**文件：** `ECS/Tools/ObjectPool/Management/ObjectPoolManager.cs` 第166-204行
```csharp
var statsMethod = poolObj.GetType().GetMethod("GetStats");
statsMethod.Invoke(poolObj, null);
```
**问题：** 生命周期方法，低频但仍有反射开销。

## 解决方案

### 接口多态替代反射（推荐）

```csharp
// 定义非泛型接口
public interface IPool
{
    void ReleaseUntyped(object instance);
    PoolStats GetStats();
    void Cleanup();
    void Destroy();
}

// ObjectPool<T> 实现接口
public class ObjectPool<T> : IPool where T : class, IPoolable
{
    public void ReleaseUntyped(object instance) => Release((T)instance);
}

// ObjectPoolManager 使用接口
private static readonly Dictionary<string, IPool> _pools = new();

private static void ReturnToPool(object instance, string poolName)
{
    if (_pools.TryGetValue(poolName, out var pool))
        pool.ReleaseUntyped(instance); // 虚方法调用，零反射
}
```