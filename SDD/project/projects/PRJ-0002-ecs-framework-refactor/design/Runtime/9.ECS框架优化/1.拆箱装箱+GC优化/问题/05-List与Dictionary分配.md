---
title: List与Dictionary热路径分配问题
status: 初稿
created: 2026-06-06
tags: [ecs, allocation, list, dictionary, high]
description: 热路径中new List/Dictionary导致的堆分配问题
---

# List与Dictionary热路径分配问题

> 严重度：🟠 HIGH（TargetSelector）/ 🟡 MEDIUM（其他）
> 影响范围：目标选择、鼠标拾取、实体销毁

## 涉及代码（6处）

### 1. EntityTargetSelector.Query — 3次new List\<IEntity\>()

**文件：** `ECS/Tools/TargetSelector/EntityTargetSelector.cs` 第28, 33, 74行
```csharp
var candidates = new List<IEntity>();   // 第28行
candidates = new List<IEntity>();       // 第33行（冗余！覆盖第28行）
var filtered = new List<IEntity>();     // 第74行
```
**问题：** 每次Query调用分配3个List对象。第33行完全冗余。无预设容量。

### 2. EntityTargetSelector.GetRange — 新List分配

**文件：** `ECS/Tools/TargetSelector/EntityTargetSelector.cs` 第59行
```csharp
candidates = candidates.GetRange(0, query.MaxTargets);
```
**问题：** GetRange() 分配一个新List。

### 3. MouseSelectionSystem — new List/HashSet

**文件：** `ECS/Tools/Input/MouseSelection/MouseSelectionSystem.Picking.cs` 第47, 125行
```csharp
var visited = new HashSet<ulong>();
var entities = new List<IEntity>();
```

### 4. EntityManager.Destroy — new HashSet\<string\>()

**文件：** `ECS/Runtime/Entity/Manager/EntityManager.cs` 第355行
```csharp
public static void Destroy(Node entity)
{
    Destroy(entity, new HashSet<string>());
}
```

### 5. DataSlot.GetModifiers() — 防御性拷贝

**文件：** `ECS/Runtime/Data/DataRuntimeStorage.cs` 第228行
```csharp
public List<DataModifier> GetModifiers()
{
    return new List<DataModifier>(_modifiers); // 每次调用分配新List
}
```

### 6. Data.GetAll() — 新Dictionary分配

**文件：** `ECS/Runtime/Data/Data.cs` 第608-626行
```csharp
public Dictionary<string, object> GetAll()
{
    var result = new Dictionary<string, object>(runtimeValues.Count);
    foreach (var pair in runtimeValues)
    {
        if (pair.Value != null)
            result[pair.Key] = pair.Value; // 已装箱的值
    }
    return result;
}
```

## 解决方案

### TargetSelector — 对象池 + 消除冗余

```csharp
// 1. 删除第33行冗余的 new List<IEntity>()
// 2. 使用对象池
private static readonly ObjectPool<List<IEntity>> _listPool = new(() => new List<IEntity>(16));

var candidates = _listPool.Get();
try { /* ... */ }
finally { candidates.Clear(); _listPool.Return(candidates); }
```

### GetRange — RemoveRange替代

```csharp
// 优化前（分配新List）
candidates = candidates.GetRange(0, query.MaxTargets);

// 优化后（原地修改）
if (candidates.Count > query.MaxTargets)
    candidates.RemoveRange(query.MaxTargets, candidates.Count - query.MaxTargets);
```

### Data.GetModifiers — 返回只读接口

```csharp
// 优化前
public List<DataModifier> GetModifiers() => new List<DataModifier>(_modifiers);

// 优化后
public IReadOnlyList<DataModifier> GetModifiers() => _modifiers;
```