# Collision 使用说明

> status: current
> sourcePaths: Src/ECS/Capabilities/Collision/Component/
> relatedDocs: ./Collision概念.md, ../Concepts/README.md, ../Concepts/对象池碰撞兼容说明.md
> lastReviewed: 2026-06-02

## 1. 源码入口

- `Src/ECS/Capabilities/Collision/Component/CollisionComponent/` — 视觉体碰撞桥接
- `Src/ECS/Capabilities/Collision/Component/HurtboxComponent/` — 受击区碰撞桥接
- `Src/ECS/Capabilities/Collision/Component/ContactDamageComponent/` — 接触伤害
- `Src/ECS/Capabilities/Collision/Component/PickupComponent/` — 拾取
- `Data/DataKey/Base/CollisionLayers.cs` — 碰撞层常量

完整迁移全文：

- [CollisionComponent.md](../Component/CollisionComponent/CollisionComponent.md)
- [ContactDamageComponent.md](../Component/ContactDamageComponent/ContactDamageComponent.md)
- [PickupComponent.md](../Component/PickupComponent/PickupComponent.md)

## 2. 常见调用流程

### 视觉体碰撞（子弹、特效）

```text
Area2D Entity
  ├─ CollisionComponent
  └─ VisualRoot / CollisionShape2D

CollisionComponent → CollisionEntered/CollisionExited → EntityMovementComponent
```

### 受击区碰撞（角色）

```text
CharacterBody2D Entity
  ├─ HurtboxComponent (Area2D)
  │   └─ CollisionShape2D
  └─ ContactDamageComponent（可选）

HurtboxComponent → HurtboxEntered/HurtboxExited → ContactDamageComponent
```

### 移动碰撞

```text
EntityMovementComponent
  ├─ Area2D 路径：订阅 CollisionEntered
  └─ CharacterBody2D 路径：MoveAndSlide() 后检查 slide collision

→ MovementCollisionPolicy.TryAccept()
→ MovementCollision 事件
→ 命中 StopAfterCollisionCount 时发 MovementStopRequested
```

### 碰撞配置

```csharp
Collision = new MovementCollisionParams
{
    TeamFilter = TeamFilter.Enemy,
    EntityTypeFilter = EntityType.Unit,
    StopAfterCollisionCount = 1,
    DestroyOnStop = true,
    EmitCollisionEvent = true
};
```

## 3. 数据和事件

### 事件语义

- `CollisionEntered(Source, Target)` / `CollisionExited` — 视觉体碰撞，不代表业务结论
- `HurtboxEntered(Source, Hurtbox, Target, TargetEntity)` / `HurtboxExited` — 受击区事件
- `MovementCollision` — 移动系统接受的有效碰撞（含 Mode、Target、CollisionCount、WillStop）

### Layer/Mask 约定

| 对象 | Layer | Mask |
| ---- | ---- | ---- |
| Player 物理体 | `2+256` | `1` |
| Enemy 物理体 | `4+256` | `1+4` |
| Player Hurtbox | `8` | `4` |
| Enemy Hurtbox | `64` | `128` |
| Projectile | `32` | 视业务 |
| WeaponHitbox | `128` | `64` |

## 4. 修改边界

- **视觉体碰撞用 CollisionComponent**：只负责 Area2D 根节点
- **受击区碰撞用 HurtboxComponent**：自身就是 Area2D
- **CharacterBody2D 移动碰撞不接 CollisionComponent**：通过 slide collision
- **"命中后停/销毁/穿透"配置 MovementParams.Collision**
- **"只到终点触发"的技能不依赖 MovementCollision**
- **碰撞形状调整改 Entity 场景，不在业务层硬编码**

### 对象池碰撞实体时序

物理根节点池化实体由 ObjectPool / Entity Runtime 协作处理，Collision 只消费最终事件：

```text
回池：
  ObjectPool.Release
  -> 组件卸载 / 事件解绑
  -> 禁用处理与显示
  -> 泊车位
  -> 脱树

出池：
  ObjectPool.Get(false)
  -> 挂树但保持碰撞关闭
  -> EntitySpawnPipeline 设置 Data / Visual / Transform / Component
  -> ObjectPool.Activate 恢复碰撞
  -> CharacterBody2D CallDeferred(MoveAndSlide)
```

收到 `entered/exited` 后仍要过滤：

- Source Entity 是否仍有效。
- Target Entity 是否仍有效。
- Source / Target 是否处于回池、释放中或半初始化状态。
- team / owner / source / target / lifecycle 是否仍匹配业务语义。

## 5. Debug 入口

- Godot 编辑器"可见碰撞形状"选项
- 碰撞层常量：`Data/DataKey/Base/CollisionLayers.cs`
- 对象池碰撞诊断：优先看 `DocsAI/ECS/Tools/ObjectPool/README.md`、`ObjectPoolObservability`、`ObjectPoolManager.GetAllStats()` 和后续节点状态快照
- 深度技术链路：`DocsAI/ECS/Capabilities/Collision/Concepts/对象池碰撞兼容说明.md`
