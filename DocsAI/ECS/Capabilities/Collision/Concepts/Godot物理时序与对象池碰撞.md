# Godot 物理时序与对象池碰撞

> 状态：current
> 定位：Collision 与 ObjectPool 协作的当前总览、调研结论和 Godot 源码分析摘要。
> 更新：2026-06-03

## 1. 总体状况

当前问题不是“对象池导致所有幽灵碰撞”，而是下面几类因素叠在一起：

```text
Godot 物理节点复用
  + Area2D signal / query flush 时序
  + Entity 多阶段 spawn / activate
  + 旧碰撞属性切换 / 脱树实现
  + ContactDamage 旧 attacker 引用
  + Node2D 父链结构风险
```

当前设计裁决：

- ObjectPool 保留，但目标默认策略改为 `ParkedInTree`。
- Collision 不接管对象池容量或节点创建，只负责信号桥接和业务入口 guard。
- Entity Runtime 负责编排 `Get(false)` 后的 Data、Transform、Component、Registry 初始化，并在完成后调用 `pool.Activate()`。
- Damage / Movement / Projectile / Effect 负责解释有效命中、伤害、移动停止、穿透和销毁。

## 2. 当前目标策略

```text
Release:
  IsInPool = true
  CollisionLogicActive = false
  CollisionReadyPhysicsFrame = int.MaxValue
  hide + disable process
  move to parking grid
  do not RemoveChild
  do not disable collision
  do not change layer/mask/shape

Activate:
  IsInPool = false
  CollisionLogicActive = true
  CollisionReadyPhysicsFrame = currentPhysicsFrame + 1
  show + inherit process

Collision callback:
  reject if pooled
  reject if !CollisionLogicActive
  reject if currentPhysicsFrame < CollisionReadyPhysicsFrame
  then check entity valid, target valid, team, owner, lifecycle
```

`Detach`、`CollisionObject2D.disable_mode=REMOVE`、`SetCollisionTreeActive(false)` 和 `CollisionShape2D.Disabled` 只保留为 fallback 或验证对照，不再是默认路径。

## 3. Godot 源码分析摘要

本轮分析使用本地 Godot 4.6.2 源码、Godot 4.6 class docs 和公开 issue 作为校准输入。结论只作为 SlimeAI 设计约束，不复制外部代码。

| Godot 机制 | 对 SlimeAI 的含义 |
| --- | --- |
| `CollisionObject2D` enter/exit tree 会把 area/body 的 physics space 设为 world space 或 `RID()`。 | 脱树确实能退出物理世界，但也引入 `RemoveChild/AddChild`、生命周期回调和重新入树窗口。 |
| `CollisionShape2D.disabled` 会让 shape 从 broadphase 移除，恢复时重新加入。 | 默认回池不应反复禁用/启用 shape，避免 pair remove/create 和旧 enter/exit 序列。 |
| `Area2D.monitoring/monitorable` 在 query flush / signal 期间修改受限，且已有 pair 不一定按属性值立即重算。 | 不把 `Monitoring/Monitorable` 当对象池默认退场开关。 |
| `Object.set_deferred()` 在当前帧末尾赋值。 | 它是安全提交工具，不是“立即从物理世界消失”的证明。 |
| `Area2D.get_overlapping_bodies()` 列表按 physics step 更新。 | validation 不能只看“移动后立即查询”；必须记录事件序列、physics frame 和业务事件。 |
| `DISABLE_MODE_REMOVE` 本质是 disabled 时从 physics space 移除、enabled 时恢复。 | 可作为 fallback / control check，不替代默认 `ParkedInTree`。 |

## 4. 旧脱树分析为何进入 History

旧文档对 `RemoveChild/AddChild`、`SetDeferred`、shape disabled 和 broadphase pair 的源码分析仍有参考价值，但其默认结论已经过时：

- 旧策略追求回池对象从物理世界彻底退出。
- 新策略追求减少物理状态切换，把业务正确性放到 runtime state guard 和激活首帧 embargo。
- 旧策略中的“停到远点”保留并升级为分散 parking grid，但它不是唯一正确性证明。
- 旧策略中的“关碰撞 / 关监控 / 脱树”只作为 fallback 或组件真实卸载时的工具。

完整旧分析保存在 [History/对象池碰撞兼容说明.md](History/对象池碰撞兼容说明.md)。

## 5. Owner 边界

| Owner | 负责 | 不负责 |
| --- | --- | --- |
| ObjectPool | 复用、容量、池归属、显隐/处理模式、parking grid、pool runtime state、fallback detach 事实 | Data 初始化、Entity 注册、阵营、伤害、命中语义 |
| Entity Runtime | `Get(false)` 后的 Data / Visual / Transform / Component / Registry / Lifecycle 初始化，完成后 `Activate()` | Godot pair 细节、业务命中解释 |
| Collision | `Area2D` 信号桥接、Hurtbox/Pickup/ContactDamage 输入事件、layer/mask、场景结构约束、pool state guard | 对象池容量、节点创建、伤害结算 |
| Movement / Damage | 有效碰撞解释、停止/销毁、伤害结算、旧引用清理 | 对象复用和 parking 策略 |

## 6. 验证要求

后续 SDD 必须用结构化验证证明目标行为：

- 回池对象仍在树中、位于 parking grid、业务碰撞关闭。
- `Activate()` 后第一 physics frame 的旧 signal 不进入业务。
- ready frame 后只处理新位置的期望碰撞。
- `ContactDamageComponent` timer tick 遇到回池 attacker 会取消并清理旧引用。
- parking grid 压力在阈值内。
- fallback detach 可用，但 artifact 必须标记为对照路径。

不能用“无 error”“等待一帧后没有命中”或单纯属性值读数替代 oracle。
