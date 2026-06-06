---
title: LINQ热路径分配问题
status: 初稿
created: 2026-06-06
tags: [ecs, linq, allocation, high]
description: 热路径中使用LINQ导致的迭代器和集合分配问题
---

# LINQ热路径分配问题

> 严重度：🟠 HIGH（GetManualAbilities）/ 🟡 MEDIUM（其他）
> 影响范围：能力查询、组件查询、目标选择

## 涉及代码（5处）

### 1. AbilityInventoryService.GetManualAbilities — .Where().ToList()

**文件：** `ECS/Capabilities/Ability/System/AbilityInventoryService.cs` 第198-205行
```csharp
public List<EntityId> GetManualAbilities(EntityId owner)
{
    return GetAbilities(owner)
        .Where(ability => { /* 过滤逻辑 */ })
        .ToList();
}
```
**每次分配：** .Where()迭代器 + .ToList()新List+内部数组

### 2. GetAbilityByName / GetAbilityById — .FirstOrDefault() 闭包

**文件：** `ECS/Capabilities/Ability/System/AbilityInventoryService.cs` 第213-223行
```csharp
return GetAbilities(owner)
    .FirstOrDefault(ability => ability.Data.Get<string>(...) == abilityName);
```
**问题：** .FirstOrDefault(lambda) 分配闭包对象（捕获abilityName参数）。

### 3. ComponentRegistrar — .Distinct().ToArray()

**文件：** `ECS/Runtime/Entity/Components/ComponentRegistrar.cs` 第44行
```csharp
foreach (var component in components.Distinct().ToArray())
```
**分配：** Distinct()的HashSet + 迭代器 + ToArray()数组

### 4. ComponentRegistrar.GetComponents — ToArray() + OfType<T>()

**文件：** `ECS/Runtime/Entity/Components/ComponentRegistrar.cs` 第167-178行
```csharp
public IReadOnlyList<Node> GetComponents(Node? entity)
{
    return components.ToArray(); // 每次分配新数组
}

public T? GetComponent<T>(Node? entity) where T : Node
{
    return GetComponents(entity).OfType<T>().FirstOrDefault(); // 三层分配叠加
}
```

### 5. TriggerComponent — .Where().ToArray()

**文件：** `ECS/Capabilities/Ability/Component/TriggerComponent/TriggerComponent.cs` 第111-112行
```csharp
.Where(item => !string.IsNullOrWhiteSpace(item))
.ToArray();
```

## 解决方案

### GetManualAbilities — 手动循环 + 复用List

```csharp
private static readonly List<EntityId> _tempManualAbilities = new(8);

public List<EntityId> GetManualAbilities(EntityId owner)
{
    _tempManualAbilities.Clear();
    foreach (var ability in GetAbilities(owner))
    {
        if (ability.Data.Get<bool>("isManual"))
            _tempManualAbilities.Add(ability);
    }
    return _tempManualAbilities;
}
```

### GetAbilityByName — 手动循环

```csharp
public EntityId? GetAbilityByName(EntityId owner, string abilityName)
{
    foreach (var ability in GetAbilities(owner))
    {
        if (ability.Data.Get<string>("name") == abilityName)
            return ability;
    }
    return null;
}
```

### GetComponent<T> — 类型索引

```csharp
private readonly Dictionary<Type, Dictionary<Node, Node>> _typeIndex = new();

public T? GetComponent<T>(Node? entity) where T : Node
{
    if (_typeIndex.TryGetValue(typeof(T), out var index) &&
        index.TryGetValue(entity, out var component))
        return (T)component;
    return null;
}
```