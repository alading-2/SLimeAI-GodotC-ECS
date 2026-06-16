---
title: DataModifier与EventContext装箱问题
status: 初稿
created: 2026-06-06
tags: [ecs, boxing, medium]
description: 次要装箱源：DataModifier.Source、EventContext._result、PropertyChanged事件
---

# DataModifier与EventContext装箱问题

> 严重度：🟡 MEDIUM
> 影响范围：数据变更通知、能力触发、修改器系统

## 涉及代码（5处）

### 1. PropertyChanged事件 — OldValue/NewValue 为 object?

**文件：** `ECS/Runtime/Data/Events/GameEventType_Data.cs` 第9行
```csharp
public readonly record struct PropertyChanged(string Key, object? OldValue, object? NewValue);
```
**问题：** 每次数据变更发射此事件。值类型OldValue/NewValue被装箱。
**频率：** 战斗中HP每帧变更 → 每帧2次装箱

### 2. DataChangeRecord — OldValue/NewValue 为 object?

**文件：** `ECS/Runtime/Data/DataRuntimeStorage.cs` 第25行
```csharp
public sealed record DataChangeRecord(string StableKey, object? OldValue, object? NewValue);
```

### 3. EventContext._result — 存储为 object?

**文件：** `ECS/Runtime/Event/EventContext.cs` 第68行
```csharp
private object? _result;
public void SetResult<T>(T result) { _result = result; }
public T? GetResult<T>() { return _result is T typedResult ? typedResult : default; }
```
**问题：** 值类型result被装箱。每次能力触发。

### 4. IFeatureHandler.OnExecute 返回 object?

**文件：** `ECS/Capabilities/Ability/System/AbilityFeatureHandler.cs` 第17行
```csharp
public object? OnExecute(FeatureContext featureContext)
```

### 5. DataModifier.Source — 存储为 object?

**文件：** `ECS/Runtime/Data/DataModifier.cs` 第76行
```csharp
public object? Source { get; init; }
```

## 解决方案

### PropertyChanged — 泛型事件

```csharp
// 优化前
public readonly record struct PropertyChanged(string Key, object? OldValue, object? NewValue);

// 优化后
public readonly record struct PropertyChanged<T>(string Key, T OldValue, T NewValue);
EventBus.On<GameEventType.Data.PropertyChanged<float>>(handler);
```

### EventContext._result — 泛型result holder

```csharp
private IResultHolder? _resultHolder;

private sealed class ResultHolder<T> : IResultHolder
{
    public T Value { get; set; }
}

public void SetResult<T>(T result)
{
    _resultHolder = new ResultHolder<T> { Value = result };
}
```