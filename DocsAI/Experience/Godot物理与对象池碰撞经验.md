# Godot 物理与对象池碰撞经验

本文沉淀对象池、碰撞关闭、脱树、PhysicsServer2D 时序相关经验。详细历史分析见 `Docs/框架/ECS/Collision/对象池碰撞兼容说明.md` 和 `Docs/思考/碰撞问题/幽灵碰撞问题深度分析.md`。

## 问题现象

- 对象已经回池、隐藏或 `ProcessMode = Disabled`，但仍触发碰撞。
- 出池后在旧位置触发 ENTER，表现为幽灵碰撞。
- 脚本侧看到 `CollisionShape2D.disabled` 或 layer/mask 已变，物理后端仍像旧状态一样分发事件。
- `CharacterBody2D` 位置脚本侧已更新，但 PhysicsServer2D 后端 transform / broadphase / pair 可能尚未同帧同步到可用状态。

## 关键根因

- Godot 的 Node/脚本状态和 PhysicsServer2D 后端不是同一个状态容器。
- shape disable/enable、space enter/exit、broadphase pair 创建/销毁、query flush 有固定时序。
- 碰撞回调触发时，后端 query 已经排队；业务层再改属性经常晚一帧。
- `ProcessMode = Disabled` 只禁用逻辑处理，不代表物理体离开 physics space。
- 如果 2D 物理节点上层夹了普通 `Node`，会断开 Transform 继承，碰撞体可能卡在 `(0, 0)`。

## 源码证据入口

本地 Godot 4.6.2 源码重点入口：

- `modules/godot_physics_2d/godot_physics_server_2d.cpp`
- `modules/godot_physics_2d/godot_collision_object_2d.cpp`
- `modules/godot_physics_2d/godot_area_2d.cpp`
- `modules/godot_physics_2d/godot_area_pair_2d.cpp`
- `scene/2d/physics/collision_object_2d.cpp`

公开参考：

- Godot issue #92691：https://github.com/godotengine/godot/issues/92691

## 当前项目短期方案

对象池碰撞实体继续使用组合防线：

- 回池前移动到泊车位。
- 禁用逻辑、隐藏节点。
- 递归禁用碰撞 shape、layer/mask、monitoring/monitorable。
- 碰撞根节点脱树，让 physics space 清理 monitored 状态。
- 出池时先挂树但保持碰撞禁用。
- 设置新位置后 `ForceUpdateTransform()`。
- 最后统一激活碰撞。
- `CharacterBody2D` 必要时延迟 `MoveAndSlide` 触发物理代理同步。

## 中期底层 Debug 方案

Godot fork 应先加可开关 trace，不先改物理行为。

需要记录：

- RID。
- ObjectID。
- NodePath。
- shape index。
- space enter/exit。
- broadphase add/remove。
- pair enter/exit。
- query flush。
- 当前 transform。

GameOS Observation 层应把这些信息串成：

```text
EntityId -> NodePath -> ObjectID -> RID -> shape -> pair -> query callback
```

## 下次 AI 应先检查

1. 物理节点父链是否全是 `Node2D` / `CanvasItem` 可传递 Transform 的节点。
2. 回池对象是否仍在 SceneTree 的 physics space。
3. 是否只改了 `ProcessMode`，没有真正处理碰撞。
4. 是否在 flush query 期间直接改了物理状态。
5. 出池恢复碰撞前，PhysicsServer2D 是否已经得到新 transform。
6. 是否有底层 trace 或场景测试日志能证明事件来自旧位置。

## 相关 Skill

- `collision-system`
- `tools`
- `godot-scene-test`
- 未来 `debug-godot-engine`
