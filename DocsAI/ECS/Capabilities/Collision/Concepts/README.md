# Collision Concepts 入口

> 状态：current
> 定位：Collision 历史问题、碰撞层、对象池兼容和场景结构约束的概念索引。
> 更新：2026-06-03

## 当前裁决

Collision Capability 不负责对象复用，也不负责对象池容量；它负责把 Godot 物理接触桥接成 ECS 事件，并在事件进入业务前提供清晰的组件退场、信号解绑、场景结构和事件过滤契约。

ObjectPool 负责物理根节点的池化隔离。根节点是 `CollisionObject2D` 的池化实体继续默认脱树；这条规则由 ObjectPool owner 执行，由 Entity Runtime 编排激活时序，由 Collision owner 过滤业务事件。

## 文档职责

| 文档 | 状态 | 职责 |
| ---- | ---- | ---- |
| [对象池碰撞兼容说明.md](对象池碰撞兼容说明.md) | current detail | 保留详细 Godot 物理时序、宽相 pair、deferred、泊车位、脱树和两阶段激活分析。不要压缩成摘要。 |
| [碰撞系统说明.md](碰撞系统说明.md) | current summary | 碰撞桥接层、业务解释层、移动碰撞、对象池兼容边界的系统级说明。 |
| [碰撞层级说明.md](碰撞层级说明.md) | current reference | Layer / Mask 约定、当前 9 个层和常用配置。 |
| [碰撞问题需要注意.md](碰撞问题需要注意.md) | historical detail | 记录 `Node` 阻断 `Node2D` Transform 链、早期幽灵碰撞排查和失败方案。当前只作历史教训，不作为完整对象池策略事实源。 |

## 总结要点

- `Node` 不能插在需要继承 2D 空间变换的 `Node2D` / `Area2D` 父链中；这会导致碰撞体被钉在错误全局坐标。
- `ProcessMode.Disabled` 不是完整物理退场；它主要停逻辑回调。
- `Monitoring / Monitorable / CollisionShape2D.Disabled / layer/mask` 是必要防线，但单独使用不能证明池化对象已完整退出物理世界。
- `SetDeferred` 是避免物理回调中直接修改属性的安全提交方式，不是“立即从物理世界消失”的证明。
- 泊车位是挂树窗口防线；脱树才是物理根节点池化对象的默认退场语义。
- `Get(false)` 到 `Activate()` 之间必须保持碰撞关闭，让 Entity pipeline 先完成 Data、Visual、Transform 和 Component 注册。
- Collision 事件到达后仍要按 Entity 有效性、池状态、team、owner、source、target 和 lifecycle 做过滤。
- 2026-06-03 源码复核后，`disable_mode=REMOVE` 只作为对照/辅助验证项；它依赖 `ProcessMode.Disabled` 的 set space null 语义，不能替代脱树和两阶段激活。
- 对象池碰撞验证必须记录事件序列、旧/新坐标、release/acquire/activate frame 和 `checks[]`；不能用属性值或等待一帧后无报错替代 oracle。

## 不再推荐的表述

- “对象池导致了所有幽灵碰撞”：错误。对象池只是物理节点复用问题的入口之一，场景结构、事件队列、Entity 初始化时序都可能参与。
- “修复了 `Node` Transform 链后就可以取消脱树”：错误。Transform 链问题和池化物理退场问题是两类问题。
- “只要 `SetDeferred` 关闭 shape 就安全”：错误。deferred 只保证安全提交时机，不保证同帧旧 pair / 旧 transform 事件已经消失。
- “ObjectPool 负责碰撞业务正确性”：错误。ObjectPool 只提供生命周期隔离事实，业务正确性归 Collision / Damage / Movement / Entity owner。
