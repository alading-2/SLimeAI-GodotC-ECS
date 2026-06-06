---
title: EventBus装箱与反射问题
status: 初稿
created: 2026-06-06
tags: [ecs, boxing, reflection, event, critical]
description: EventBus.EmitDynamic和Action<object>路径的装箱与反射开销分析
---

# EventBus装箱与反射问题

> 严重度：🔴 CRITICAL（EmitDynamic）/ 🟡 MEDIUM（Action<object>路径）
> 影响范围：所有动态事件触发（Feature系统、数据驱动场景）

## 问题描述

EventBus的泛型路径 `Emit<T>(in T data)` 是零装箱的。但存在两条有问题的路径：
1. `EmitDynamic(object eventData)` — 反射+装箱+数组分配
2. `Action<object>` 订阅者路径 — struct事件被装箱

## 涉及代码（4处）

### 1. EmitDynamic — 完整反射链 + 装箱

**文件：** `ECS/Runtime/Event/EventBus.cs` 第198-213行
```csharp
public void EmitDynamic(object eventData)
{
    if (eventData == null) return;
    var eventType = eventData.GetType();
    var triggerMethod = typeof(EventBus).GetMethod(
        nameof(TriggerDynamicInner),
        BindingFlags.NonPublic | BindingFlags.Instance);
    if (triggerMethod == null) return;
    var generic = triggerMethod.MakeGenericMethod(eventType);
    generic.Invoke(this, new[] { eventData }); // 3次GC分配
}

private void TriggerDynamicInner<T>(object data) where T : struct
{
    Trigger((T)data); // 拆箱
}
```

**每次调用的GC开销：**
- `GetMethod()` — 返回MethodInfo对象
- `MakeGenericMethod()` — 创建泛型方法实例
- `new[] { eventData }` — 分配object[]数组
- `eventData`如果是struct — 装箱为object
- `Invoke()` — 参数打包

**调用者：** `EmitEventAction.cs` 第19、21行 — Feature系统中使用

### 2. Action<object> 订阅路径 — struct装箱

**文件：** `ECS/Runtime/Event/EventBus.cs` 第248-251行
```csharp
else if (sub.Handler is Action<object> objectHandler)
{
    objectHandler(data); // struct data 装箱为 object
}
```

### 3. Subscription 是 class — 每次订阅堆分配

**文件：** `ECS/Runtime/Event/EventBus.cs` 第34-66行
```csharp
private class Subscription { ... }
```
**问题：** 每次On<T>调用都创建一个Subscription堆对象。

### 4. Trigger 方法中的类型检查分支

**文件：** `ECS/Runtime/Event/EventBus.cs` 第220-260行
```csharp
var directHandler = sub.Handler as Action<T>;
if (directHandler != null)
{
    directHandler(data); // OK: 零装箱
}
else if (sub.Handler is Action<object> objectHandler)
{
    objectHandler(data); // 装箱！
}
```

## 解决方案

### 方案A：缓存泛型方法（最小改动）

```csharp
private static readonly ConcurrentDictionary<Type, MethodInfo> _emitDynamicCache = new();

public void EmitDynamic(object eventData)
{
    if (eventData == null) return;
    var eventType = eventData.GetType();
    var method = _emitDynamicCache.GetOrAdd(eventType, t =>
        typeof(EventBus).GetMethod(nameof(TriggerDynamicInner), BindingFlags.NonPublic | BindingFlags.Instance)!
            .MakeGenericMethod(t));
    method.Invoke(this, new[] { eventData });
}
```

### 方案B：类型注册表（推荐，零反射）

```csharp
private readonly Dictionary<Type, Action<object>> _dynamicEmitters = new();

public void RegisterDynamicEmitter<T>() where T : struct
{
    _dynamicEmitters[typeof(T)] = data => Trigger((T)data);
}

public void EmitDynamic(object eventData)
{
    if (eventData == null) return;
    if (_dynamicEmitters.TryGetValue(eventData.GetType(), out var emitter))
        emitter(eventData);
}
```

### 方案C：完全消除EmitDynamic（最佳）

```csharp
// 替代 EmitDynamic(new Unit.Damaged(...))
EventBus.Emit(new Unit.Damaged(...)); // 零装箱零反射
```