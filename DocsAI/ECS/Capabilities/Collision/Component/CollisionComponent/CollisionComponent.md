<!-- migrated-from: Src/ECS/Base/Component/Collision/CollisionComponent/CollisionComponent.md -->

> 迁移来源：`Src/ECS/Base/Component/Collision/CollisionComponent/CollisionComponent.md`
> 迁移说明：本文主体从原 `Src/ECS` 文档迁入 `DocsAI` 统一管理；原 `Src/ECS` Markdown 文件已删除。

# CollisionComponent 说明

## 1. 组件定位

`CollisionComponent` 是当前项目统一的**视觉体碰撞桥接组件**。

它只做一件事：

- 把 **Entity 根节点为 `Area2D`** 的 entered/exited 事件转发到 `Entity.Events`

它明确**不负责**：

- 伤害结算
- 拾取业务
- 移动停止/销毁决策
- `CharacterBody2D` 的移动碰撞采样

## 2. 桥接范围

### 2.1 支持的来源

`CollisionComponent` 只桥接一类碰撞来源：

- **Entity 根节点是 `Area2D`**

它内部绑定：

- `BodyEntered`
- `BodyExited`
- `AreaEntered`
- `AreaExited`

然后发出：

- `GameEventType.Collision.CollisionEntered`
- `GameEventType.Collision.CollisionExited`

### 2.2 不支持的来源

它**不桥接**：

- `CharacterBody2D` 的 slide collision
- `HurtboxComponent` 的受击区语义

原因不是实现缺失，而是职责边界不同：

- `HurtboxComponent` 有自己的 `HurtboxEntered / HurtboxExited`
- `CharacterBody2D` 的移动碰撞必须由 `MoveAndSlide()` 后读取 `GetSlideCollision(i)` 处理

## 3. 事件数据语义

当前碰撞事件结构位于：

`Src/ECS/Capabilities/Collision/Events/GameEventType_Collision.cs`

核心字段只有：

- `Source`：当前拥有 `CollisionComponent` 的实体
- `Target`：进入或离开的目标节点

它不再携带旧式 `CollisionType` 标记，也不做业务解释。

## 4. 与其它系统的分工

### 4.1 与 `EntityMovementComponent`

`EntityMovementComponent` 只在**非默认运动模式**下消费视觉体碰撞。

- `Area2D` 路径：监听 `CollisionEntered`
- `CharacterBody2D` 路径：自己在 `ApplyMovement()` 里读取 slide collision

收到候选碰撞后，移动组件会继续交给 `MovementCollisionPolicy` 过滤、去重、计数；`CollisionComponent` 本身不参与“碰到后是否停止/销毁”的决策。

### 4.2 与 `HurtboxComponent`

`HurtboxComponent` 负责受击区专用桥接：

- `HurtboxEntered`
- `HurtboxExited`

接触伤害、受击判定等业务应直接消费 Hurtbox 事件，不要回退到 `CollisionComponent`。

### 4.3 与技能/投射物

投射物如果需要“碰撞后通知 / 穿透 N 次后停止 / 停止后销毁”，正确做法是：

1. 挂 `CollisionComponent` 让 `Area2D` 进入事件进入 ECS
2. 在 `MovementParams.Collision` 中声明移动碰撞策略
3. 在 `MovementCollision` 或 `OnCollision` 中处理命中业务

而不是把“碰到了”直接写死为“马上销毁”。

## 5. 标准节点结构

### 5.1 特效 / 子弹实体

```text
ProjectileEntity / EffectEntity (Area2D)
  ├─ Component
  │   ├─ CollisionComponent
  │   └─ EntityMovementComponent（可选）
  └─ VisualRoot
      └─ CollisionShape2D
```

### 5.2 角色实体

```text
Player / Enemy (CharacterBody2D)
  ├─ Component
  │   ├─ HurtboxComponent
  │   └─ ContactDamageComponent（可选）
  ├─ CollisionShape2D
  └─ VisualRoot
```

`CharacterBody2D` 是否挂 `CollisionComponent`，取决于该实体是否还需要独立 `Area2D` 视觉碰撞桥接；它与移动碰撞不是一回事。

## 6. 使用建议

- 视觉体根节点是 `Area2D` 时，优先复用 `CollisionComponent`
- 受击区业务优先复用 `HurtboxComponent`
- 不要试图让 `CollisionComponent` 统一接管 `CharacterBody2D` 的运动碰撞
- 运动碰撞链路当前约定 `CollisionEntered/Exited.Target` 直接就是 `IEntity` 根节点；不要再依赖 `EntityManager.ResolveOwningIEntity(...)` 做宿主回溯

## 7. 关键文件

- 组件实现：`Src/ECS/Capabilities/Collision/Component/CollisionComponent/CollisionComponent.cs`
- 受击区桥接：`Src/ECS/Capabilities/Collision/Component/HurtboxComponent/HurtboxComponent.cs`
- 移动调度器：`Src/ECS/Capabilities/Movement/Component/EntityMovementComponent.cs`
- 碰撞系统总览：`DocsAI/ECS/Capabilities/Collision/System/Collision使用说明.md`
