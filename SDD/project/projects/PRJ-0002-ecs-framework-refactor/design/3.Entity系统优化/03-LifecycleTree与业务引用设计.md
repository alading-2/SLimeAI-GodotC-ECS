# LifecycleTree 与业务引用设计

> 更新：2026-05-29
> 目标：把旧 Relationship 的所有语义拆开，逐项说明新系统中怎么表达。

## 0. 旧 Relationship 语义拆分

当前 `EntityRelationshipType` 实际混合了 5 类语义：

| 旧类型 | 真实语义 | 新归属 |
| --- | --- | --- |
| `PARENT` | 生命周期 parent、递归销毁、detach | `LifecycleTree` |
| `ENTITY_TO_COMPONENT` | Godot component owner 反查 | `ComponentRegistrar` 内部 index |
| `ENTITY_TO_PROJECTILE` | source -> projectile owned list / debug | Projectile typed DataKey + owner list |
| `ENTITY_TO_EFFECT` | source/host -> effect owned list / cleanup | Effect typed DataKey + owner list |
| `ENTITY_TO_ABILITY` | owner -> ability owned list | Ability typed DataKey + owner list |
| `ENTITY_TO_ITEM` | inventory / equipment ownership | Item / Inventory service typed DataKey |
| `ENTITY_TO_UI` | UI control 绑定 entity | UI binding registry / GodotBridge |
| `ABILITY_TO_EFFECT` | ability spawn/trigger effect | Ability / Effect DataKey |
| `ITEM_TO_ABILITY` | item grants ability | Item / Ability service explicit grant record |

新系统不再提供“通用关系类型”。每个语义必须进入自己的 owner 模块。

## 1. LifecycleTree

### 1.1 只表达生命周期

`LifecycleTree` 只回答：

- 谁随谁销毁。
- 父销毁时 child 是递归销毁还是 detach。
- 当前 child 的 lifecycle parent 是谁。
- parent 下有哪些 lifecycle children。

它不回答：

- 谁造成伤害。
- projectile 来自哪个 weapon。
- ability 属于哪个玩家。
- UI control 绑定哪个 entity。
- component 所属 entity。

### 1.2 单 parent

每个 entity 至多一个 lifecycle parent。这个约束来自当前玩法需要和 AI-first 可理解性：

```text
如果一个 child 可以有多个 lifecycle parent
  -> 父销毁策略冲突
  -> 防环复杂
  -> 统计归因更容易误用
  -> AI 难判断哪个 parent 是主链
```

多 owner、多 source、多 target 都不是 lifecycle parent，改用 typed DataKey 或 owner list。

### 1.3 Destroy policy

```csharp
public enum ParentDestroyPolicy
{
    DestroyRecursively = 0,
    Detach = 1
}
```

规则：

- `DestroyRecursively`：父销毁时递归销毁 child。
- `Detach`：父销毁时只断开 lifecycle link，child 存活。
- policy 是 `LifecycleLink` 字段，不存在 `Dictionary<string, object>`。
- 缺 policy 是 bug，不使用隐式默认字典回退。

### 1.4 Destroy 顺序

推荐顺序：

```text
Destroy(entityId)
  1. Snapshot lifecycle children
  2. Destroy recursive children
  3. Detach all lifecycle links involving entity
  4. Notify OwnedReferenceRegistry
  5. Publish EntityDestroying if needed
  6. Unregister components
  7. Clear Data / Events
  8. Unregister entity
  9. Return to pool or QueueFree
  10. Publish EntityDestroyed summary / observation
```

注意：

- children 必须 snapshot，不能边遍历边读内部 list。
- owner reference cleanup 不影响 lifecycle destroy 选择。
- Component unregister 在 Data clear 前执行，让 component 回调仍可读 owner Data。

## 2. Business reference

业务引用分为两类：

```text
single reference:
  DataKey<EntityId?> 或旧 DataOS 暂用 DataKey<string> + EntityId 规则

multi reference:
  DataKey<EntityIdList> 或旧 DataOS 暂用 stable string list + typed wrapper
```

如果当前 Data runtime 暂不支持 `EntityId` 作为 DataValueType，可以分两步：

1. 运行时服务 API 使用 `EntityId`。
2. DataOS 内存和 snapshot 序列化暂存 string，读写处显式 `EntityId.From(value)` / `.Value`。

这不是兼容旧 Relationship，而是 DataOS 类型能力未完成前的序列化策略。

## 3. Projectile

### 旧语义

```text
owner -> projectile:
  PARENT
  ENTITY_TO_PROJECTILE
```

实际包含：

- 生命周期：owner 销毁时 projectile 是否销毁。
- 来源：projectile 是谁发出的。
- 统计：projectile 伤害记给谁。
- 调试：owner 下有哪些 projectile。

### 新语义

| 需求 | 新表达 |
| --- | --- |
| owner 销毁时 projectile 销毁 | `LifecycleTree.Attach(ownerId, projectileId, DestroyRecursively)` |
| projectile 直接来源 | `Projectile.SourceEntityId` |
| projectile 关联 ability | `Projectile.AbilityEntityId` |
| projectile 目标 | `Projectile.TargetEntityId` |
| owner 下 projectile 列表 | `Projectile.SpawnedProjectileIds` on owner |
| 伤害记给谁 | `DamageAttribution.DamageCreditEntityId` |

### Spawn 流程

```text
ProjectileTool.Spawn(sourceId, abilityId, targetId, ...)
  Spawn ProjectileEntity with LifecycleParentId = sourceId
  Set Projectile.SourceEntityId = sourceId
  Set Projectile.AbilityEntityId = abilityId
  Set Projectile.TargetEntityId = targetId
  Append projectileId to source.SpawnedProjectileIds
  Return projectileId/node
```

如果 projectile 来源是 weapon，玩家是 weapon owner：

- `Projectile.SourceEntityId = weaponId`
- `DamageAttribution.WeaponEntityId = weaponId`
- `DamageAttribution.DamageCreditEntityId = playerId`

不要通过 `weapon -> player -> projectile` parent chain 推断。

## 4. Effect

### 旧语义

Effect 可能挂 host，也可能只有 owner。旧代码用 `BindParentRelationships(... ENTITY_TO_EFFECT)` 同时表示宿主、归属和清理。

### 新语义

| 需求 | 新表达 |
| --- | --- |
| 附着特效随 host 销毁 | lifecycle parent = hostId |
| 独立特效随 caster/source 销毁 | lifecycle parent = sourceId 或 scene scope id |
| 特效来源 | `Effect.SourceEntityId` |
| 特效目标 | `Effect.TargetEntityId` |
| 特效来源 ability | `Effect.AbilityEntityId` |
| owner 下 effect 列表 | `Effect.SpawnedEffectIds` |
| 根据 host 销毁特效 | `EffectService.DestroyByHost(hostId)` 读 host 的 typed list 或 effect index |

`DestroyByHost` 不再查 `EntityRelationshipManager.GetChildEntitiesByParentAndType(hostId, ENTITY_TO_EFFECT)`。

## 5. Ability

### 旧语义

`EntityManager_Ability` 作为 EntityManager partial 创建 AbilityEntity，并用 `ENTITY_TO_ABILITY` 做 owner -> ability 查询。

### 新语义

| 需求 | 新表达 |
| --- | --- |
| ability lifecycle | `LifecycleTree.Attach(ownerId, abilityId, DestroyRecursively)` |
| ability owner | `Ability.OwnerEntityId` |
| owner ability list | `Ability.OwnedAbilityIds` |
| 查所有技能 | `AbilityService.GetAbilities(ownerId)` |
| 移除技能 | `AbilityService.RemoveAbility(ownerId, abilityId)` |

`EntityManager_Ability.cs` 删除。Ability service 是唯一 owner。

### 为什么 Ability 仍可 lifecycle child

AbilityEntity 通常随 owner 生命周期存在，owner 销毁后 ability 没有意义。因此 lifecycle parent 可以是 owner。但“ability 属于 owner”的查询事实仍放在 `Ability.OwnerEntityId / OwnedAbilityIds`，不是通过 lifecycle children 查询。

这样做的原因：

- 生命周期和业务列表可以分别测试。
- 如果未来某种 ability 不随 owner 销毁，只改 lifecycle policy，不影响 owner 查询。
- 统计和 UI 不需要遍历 lifecycle tree。

## 6. Item / Weapon

Item / Weapon 比 Ability 更容易出现非生命周期归属：

- 掉落在地上时没有 owner。
- 装备后 owner 是玩家。
- 玩家死亡后可能掉落，也可能销毁，也可能转移。

所以 Item / Weapon 不默认用 lifecycle parent 表达 owner。推荐：

```text
Item.OwnerEntityId
Inventory.OwnedItemIds
Weapon.OwnerEntityId
Equipment.EquippedWeaponIds
```

生命周期策略由 item/equipment service 决定：

- 装备随玩家销毁：attach lifecycle。
- 玩家死后掉落：detach lifecycle + owner id clear。
- 关卡结束清理：attach 到 scene lifecycle root。

## 7. UI binding

UI 绑定不是 gameplay relationship。

新设计：

```csharp
public sealed class UiBindingRegistry
{
    public void Bind(EntityId entityId, Control control);
    public void Unbind(EntityId entityId, Control control);
    public IReadOnlyList<Control> GetControls(EntityId entityId);
    public void ClearEntity(EntityId entityId);
}
```

规则：

- UI binding 不进入 `LifecycleTree`。
- UI control 的 Godot parent 由 UI 场景树决定。
- Entity 销毁时，UI registry 通过 entity destroyed event 清理绑定。

## 8. Component owner

Component owner 是框架内部索引，不是业务关系。

新设计：

```text
ComponentRegistrar:
  ownerId -> component nodes
  component instance id -> ownerId
```

替换：

- `GetEntityByComponent(component)` -> `ComponentRegistrar.GetOwnerEntityId(component)` + `EntityRegistry.Get(ownerId)`
- `GetComponentsByType<T>()` -> registry/component index service
- `GetComponent<T>(entity)` -> ownerId -> component list

如果 Component 需要频繁读 owner Data，推荐在 `OnComponentRegistered(Node owner)` 时缓存 owner `IEntity` 或 owner `EntityId`，但缓存必须在 unregister 时清理。

## 9. DamageAttribution

### 9.1 字段语义

```csharp
public readonly record struct DamageAttribution(
    EntityId SourceEntityId,
    EntityId DamageCreditEntityId,
    EntityId WeaponEntityId,
    EntityId AbilityEntityId,
    EntityId EffectEntityId,
    EntityId ProjectileEntityId);
```

| 字段 | 语义 |
| --- | --- |
| `SourceEntityId` | 直接产生伤害的来源，例如 weapon hitbox、projectile、effect。 |
| `DamageCreditEntityId` | 主要记账对象，通常是 player/enemy unit。 |
| `WeaponEntityId` | 武器统计对象，可空。 |
| `AbilityEntityId` | 技能统计对象，可空。 |
| `EffectEntityId` | 特效来源，可空。 |
| `ProjectileEntityId` | 投射物来源，可空。 |

### 9.2 构造位置

Attribution 应在“伤害请求产生处”构造，而不是在 damage processor 末端猜：

| 来源 | 构造者 |
| --- | --- |
| 普攻 hitbox | AttackService / AttackComponent |
| projectile hit | ProjectileTool / Projectile hit handler |
| effect tick damage | EffectTool / EffectComponent |
| ability instant damage | AbilityService / DamageTool |
| contact damage | ContactDamageComponent |

### 9.3 统计处理

`StatisticsProcessor` 只读 attribution：

```text
if DamageCreditEntityId not empty:
  add player/enemy unit stats
if WeaponEntityId not empty:
  add weapon stats
if AbilityEntityId not empty:
  add ability stats
```

禁止：

- 禁止 `EntityRelationshipTraversal.GetAncestorChain(info.Attacker)`。
- 禁止 `FindAncestorOfType<IUnit>` 作为统计主路径。
- 禁止因为 attribution 缺失而 fallback 到 parent chain。

## 10. Owner cleanup

### 10.1 为什么需要 cleanup registry

如果 child 销毁，owner list 必须同步移除 child id。否则 AI 和 UI 会看到已销毁 entity id。

### 10.2 Descriptor

```csharp
public readonly record struct OwnedReferenceDescriptor(
    DataKey<EntityId?> ChildToOwnerKey,
    DataKey<EntityIdList> OwnerListKey);
```

旧 DataOS 暂存 string 时可以用专门 descriptor：

```csharp
public readonly record struct StringOwnedReferenceDescriptor(
    DataKey<string> ChildToOwnerKey,
    DataKey<string> OwnerListKey);
```

但执行目标应是 typed DataKey。

### 10.3 生命周期

```text
Capability.Initialize:
  OwnedReferenceRegistry.Register(Projectile.SourceEntityId, Projectile.SpawnedProjectileIds)

EntityDestroyPipeline.Destroy(childId):
  registry.NotifyDestroying(childId)
  -> read child.SourceEntityId
  -> resolve owner
  -> remove childId from owner.SpawnedProjectileIds
```

Owner cleanup 不销毁 child；销毁只由 `LifecycleTree` 决定。

## 11. Observation dump

建议输出 4 个段落：

```json
{
  "entityId": "player-1",
  "lifecycle": {
    "parentEntityId": "",
    "children": ["ability-1", "projectile-7"]
  },
  "typedReferences": {
    "ownedAbilityIds": ["ability-1"],
    "spawnedProjectileIds": ["projectile-7"]
  },
  "componentOwner": {
    "components": ["HealthComponent", "AttackComponent"]
  }
}
```

Damage trace：

```json
{
  "event": "DamageApplied",
  "sourceEntityId": "projectile-7",
  "damageCreditEntityId": "player-1",
  "weaponEntityId": "weapon-2",
  "abilityEntityId": "ability-1"
}
```

AI 调试看 dump，不查 runtime Relationship 图。
