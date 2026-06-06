---
title: foreach迭代器分配问题
status: 初稿
created: 2026-06-06
tags: [ecs, foreach, allocation, medium]
description: Dictionary迭代和ToArray()在生产代码中的分配问题
---

# foreach迭代器分配问题

> 严重度：🟡 MEDIUM
> 影响范围：系统遍历、组件查询、状态集合

## 涉及代码（7处）

### 1. SystemManager — foreach Dictionary.Values（每帧）

**文件：** `ECS/Runtime/System/SystemManager.cs` 第72, 214, 396, 472, 503行
```csharp
foreach (var entry in _entries.Values)
```
**问题：** SystemManager每帧遍历所有系统。在Mono下 `.Values` 可能分配ValueCollection。

### 2. TimerScheduler — foreach Dictionary.Values

**文件：** `ECS/Tools/Timer/Core/TimerScheduler.cs` 第209, 281, 374行

### 3. StatusCollection — foreach _instances.Values

**文件：** `ECS/Capabilities/StatusSystem/System/StatusCollection.cs` 第41行

### 4. StatusControllerComponent — foreach _statusTimers.Values

**文件：** `ECS/Capabilities/Unit/Component/Common/StatusControllerComponent/StatusControllerComponent.cs` 第32行

### 5. AbilityInventoryService — foreach abilityIds.Values

**文件：** `ECS/Capabilities/Ability/System/AbilityInventoryService.cs` 第178行

### 6. ProjectileOwnershipService / EffectOwnershipService

**文件：** `ECS/Capabilities/Projectile/System/ProjectileOwnershipService.cs` 第109行
**文件：** `ECS/Capabilities/Effect/System/EffectOwnershipService.cs` 第108行

### 7. ComponentRegistrar — 双重 ToArray()

**文件：** `ECS/Runtime/Entity/Components/ComponentRegistrar.cs` 第113行
```csharp
var components = GetComponents(entity).ToArray(); // GetComponents内部已ToArray，双重分配
```

## 解决方案

### foreach Dictionary — 直接迭代KeyValuePair

```csharp
// 优化前
foreach (var entry in _entries.Values) { ... }

// 优化后（避免Values分配）
foreach (var kvp in _entries) { var entry = kvp.Value; ... }
```

### ComponentRegistrar — 去除冗余ToArray

```csharp
// 优化前（双重ToArray）
var components = GetComponents(entity).ToArray();

// 优化后
var components = GetComponents(entity); // 已经是数组
```