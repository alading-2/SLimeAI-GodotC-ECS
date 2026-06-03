# ObjectPool 测试与验证设计

> 状态：current test design
> 更新：2026-06-03
> 范围：`Src/ECS/Tools/ObjectPool/Tests`
> 裁决：当前目录里的两个 `Control` 场景保留为人工 demo；回归验证必须新增自动化 contract test 和 Godot collision validation scene，不能继续用 UI/鼠标演示代替场景门禁。

## 1. 当前测试现状

| 文件 | 当前性质 | 可保留价值 | 不能作为门禁的原因 |
| --- | --- | --- | --- |
| `ObjectPoolVisualTest.cs/.tscn` | 单池可视化 demo | 展示复用率、活跃/闲置统计、`ReleaseAll` 和手动生成 | 依赖 UI、鼠标、随机位置、每帧刷新；没有断言、artifact、README 五字段。 |
| `ObjectPoolManagerTest.cs/.tscn` | 多池 manager demo | 展示 `ObjectPoolManager.GetAllStats()`、`CleanupAll()`、`DestroyAll()` | 使用 `ProjectilePool` / `EffectPool` 这类真实池名，存在覆盖全局池风险；`DestroyAll()` 会污染其它测试。 |
| `TestProjectile.cs` / `TestEffect.cs` / `VisualTestBullet.cs` | demo 用 `Node2D` 对象 | 演示 `IPoolable`、生命周期、静态归还 | 根节点不是 `CollisionObject2D`，无法覆盖脱树、旧位置幽灵碰撞和 `Get(false)` 安全窗口。 |

结论：这些文件应改名或归类为 `Demo`，不要删除；但它们不再承担 ObjectPool 回归门禁。

## 2. 目标测试分层

### 2.1 Runtime contract

目标：验证对象池作为复用工具的纯契约，不依赖 UI、鼠标或物理帧。

建议路径：

```text
Src/ECS/Tools/ObjectPool/Tests/ObjectPoolContractRuntimeTest.cs
```

必测 check：

| Check | 断言 |
| --- | --- |
| `contract_warmup_stats` | `InitialSize` 后 `Count / TotalCreated / ActiveCount` 正确。 |
| `contract_get_release_stats` | `Get`、`Release` 后 `TotalAcquired / TotalReused / TotalReleased / ReuseRate` 正确。 |
| `contract_duplicate_release_guard` | 重复归还不会让 `ActiveCount` 负数，不重复触发 release 生命周期。 |
| `contract_capacity_discard` | 超过 `MaxSize` 归还时进入 discard，`TotalDiscarded` 可观测。 |
| `contract_static_return_node` | Node 经 `ObjectPoolName` meta 能静态归还到正确池。 |
| `contract_static_return_plain_object` | 纯 C# 对象必须从池 `Get()` 后才允许 `ReturnToPool`。 |
| `contract_active_snapshot_is_copy` | `GetActiveSnapshot()` 是快照，不因后续 `Release` 修改枚举状态。 |
| `contract_manager_pool_isolation` | 测试池名不覆盖真实 `ObjectPoolNames`，退出只清理自己创建的池。 |

命名规则：

- 测试池名使用 `Test/ObjectPool/<CaseName>`，不要使用 `ProjectilePool`、`EffectPool`、`EnemyPool` 等真实池名。
- 测试结束优先调用当前池实例的 `Destroy()`；只有专门验证全局清理时才调用 `ObjectPoolManager.DestroyAll()`。

### 2.2 Godot collision validation

目标：验证当前裁决中的“泊车位 + 脱树 + 挂树后同步禁用 + 两阶段激活”确实覆盖 Godot 物理根节点复用风险。

建议路径：

```text
Src/ECS/Tools/ObjectPool/Tests/ObjectPoolCollisionIsolationValidation.cs
Src/ECS/Tools/ObjectPool/Tests/ObjectPoolCollisionIsolationValidation.tscn
Src/ECS/Tools/ObjectPool/Tests/README.md
```

场景必须 headless-friendly，自动执行，不依赖点击、滑条、随机位置或人工观察。

必测 check：

| Check | 验证行为 | 通过标准 |
| --- | --- | --- |
| `collision_area_release_detaches` | 根节点 `Area2D` 回池 | `InPool=true`，`IsInsideTree=false`，旧位置不再产生 `area_entered/body_entered`。 |
| `collision_character_release_detaches` | 根节点 `CharacterBody2D` 回池 | 节点脱树，速度清零，下一物理帧没有旧位置触发。 |
| `collision_get_false_attached_disabled` | `Get(false)` 后挂树未激活 | 节点已挂树，但 `Area2D.monitoring/monitorable=false`，shape disabled，未触发 entered。 |
| `collision_activate_after_transform` | 设置新位置并 `Activate()` | 只允许在新位置产生期望碰撞，不允许补发旧位置事件。 |
| `collision_immediate_reuse_same_frame` | 同帧或相邻帧 release → get(false) → activate | 事件序列中没有旧 entity / 旧坐标 / 旧 frame 的 entered。 |
| `collision_non_collision_node_not_detached` | 根节点 `Node2D` / `Control` 回池 | 只切 `ProcessMode` / `Visible`，不脱树。 |
| `collision_artifact_oracle_complete` | artifact 自检 | 五字段非空，`checks[]` 覆盖以上 check，`failureReasons=[]`。 |

负向场景说明：

- 普通 `Node` 阻断 `Node2D` 空间链路属于 Collision scene structure gate，应该在 Collision 验证中覆盖。
- ObjectPool validation 只引用该风险，不在对象池里继续堆补丁。

### 2.3 Manual demo

当前 `ObjectPoolVisualTest` 和 `ObjectPoolManagerTest` 可继续保留，但应降级为人工 demo：

- 文件名或 README 标记为 `legacy/manual demo`。
- UI 文案保留演示价值即可，不承担 PASS/FAIL。
- demo 池名改为 `Demo/ObjectPool/VisualBullet`、`Demo/ObjectPool/Projectile`、`Demo/ObjectPool/Effect` 这类隔离名称。
- demo 退出时不调用全局 `ObjectPoolManager.DestroyAll()`，除非 demo 先证明它只创建了隔离池。

## 3. README 五字段

`Src/ECS/Tools/ObjectPool/Tests/README.md` 必须包含：

```markdown
expectedInputs:
- 自动创建隔离测试池，不依赖全局游戏池。
- 自动执行 Area2D、CharacterBody2D、Node2D 三类池化对象 release / get(false) / activate 序列。

expectedObservations:
- 物理根节点回池后脱树。
- get(false) 后挂树但碰撞关闭。
- activate 后只在新位置恢复碰撞。
- 非物理根节点不脱树。

passCriteria:
- artifact status=pass。
- checks[] 中所有声明 check 为 pass。
- 旧位置 entered 计数为 0。
- failureReasons=[]。

failCriteria:
- 缺 README 五字段或 artifactPath。
- 只有 stdout PASS marker。
- 旧位置产生 entered。
- get(false) 窗口内 collision enabled。
- 测试池覆盖真实 ObjectPoolNames。

artifactPath:
- .ai-temp/scene-tests/artifacts/objectpool-collision-isolation-validation.json
```

## 4. PASS artifact

Godot validation scene 必须写出结构化 JSON，最小形态：

```json
{
  "status": "pass",
  "expectedInputs": "isolated object pool collision validation sequence",
  "expectedObservations": "collision roots detach while non-collision nodes stay attached",
  "passCriteria": "all checks pass and old-position entered count is zero",
  "failCriteria": "any missing oracle field, stale entered event, active collision during get(false), or real pool name collision",
  "artifactPath": ".ai-temp/scene-tests/artifacts/objectpool-collision-isolation-validation.json",
  "checks": [],
  "poolStats": {},
  "nodeStates": [],
  "collisionEvents": [],
  "failureReasons": []
}
```

Verifier 声明通过时必须同时引用：

- run `index.json`
- per-scene `result.json`
- scene artifact
- artifact `checks[].name`

`exit code 0`、`无 error` 或 stdout `PASS` 只能作为辅助信息。

## 5. 外部资料校准

- Godot 4.6 `CollisionShape2D.disabled`：官方文档要求用 `Object.set_deferred()` 修改，该属性只说明 shape 是否影响物理世界，不等于完整退场状态机。
- Godot 4.6 `Area2D.monitoring/monitorable`：只控制检测与被检测，不证明旧物理 pair 和事件队列已经被清空。
- Godot `CollisionObject2D.disable_mode`：只在 `ProcessMode.Disabled` 语义下定义物理行为；可作为辅助验证项，不作为替代脱树的默认策略。
- Unity / Game Programming Patterns 对象池资料：对象池的稳定边界是复用、容量、reset、统计和归属；业务生命周期、碰撞归因和实体有效性仍应由上层 owner 判断。

## 6. 实现前裁决

- 先补 `README.md` 五字段和 validation scene skeleton，再改实现；Godot 场景按 `SceneValidationSession.Check()` 或等价 artifact check 进入 RED/GREEN。
- 不把现有 UI demo 硬改成自动化门禁；demo 和 validation 分离。
- 不取消物理根节点脱树。
- 不用“延迟一帧后没有报错”作为通过标准。
- 不让 ObjectPool 接管 Collision / Damage / Entity 的业务过滤。

## 7. 2026-06-03 Godot 源码与社区校准

本轮研究范围：

```yaml
externalResources:
  enabled:
    - engine-framework
    - web
    - context7
  scope:
    - /home/slime/Code/SlimeAI/Resources/Engine/Engine/godot-4.6.2-stable
    - /home/slime/Code/SlimeAI/Resources/Engine/Docs
    - Godot 4.6 Area2D / CollisionShape2D / CollisionObject2D docs
    - Godot issue 69407 / 79464 / 74988 / godot-proposals 3424
  reason: 校准对象池碰撞隔离是否应继续脱树，以及 validation scene 应覆盖哪些引擎时序风险。
  expires: current-task
copiedCodeOrAssets: none
```

### 7.1 Evidence

| 来源 | 证据 | 对测试设计的影响 |
| --- | --- | --- |
| Context7 / Godot 4.6 `Area2D` docs | `get_overlapping_areas/bodies` 列表在 physics step 中更新，不会在移动对象后立即同步。 | validation 不能只移动节点后立即读 overlap；必须记录物理帧后的事件序列和旧坐标 entered 计数。 |
| Context7 / Godot 4.6 `CollisionShape2D.disabled` docs | disabled shape 在物理世界无效，但官方要求通过 `Object.set_deferred()` 修改。 | `SetDeferred` 是安全提交方式，不是对象池完整退场状态机；测试要覆盖 deferred 未生效窗口。 |
| Context7 / Godot 4.6 `CollisionObject2D.disable_mode` docs | `DISABLE_MODE_REMOVE` 只在 `Node.process_mode=Disabled` 时把对象移出 physics simulation，并在恢复处理时自动加回。 | `disable_mode=REMOVE` 只能作为对照 check，不能替代 `Get(false)` 到 `Activate()` 的半初始化窗口验证。 |
| Godot issue `godotengine/godot#69407` | 同一函数/同一 physics frame 中先移动出 Area 再恢复 `collision_mask`，仍可能补发 `body_entered`；等待 `physics_frame` 可绕过。 | 新场景必须覆盖同帧或相邻帧 release -> get(false) -> activate，且断言旧位置 entered 为 0。 |
| Godot issue `godotengine/godot#79464` | toggle `monitorable` 时，已有重叠的 Area2D 不一定重新触发 `area_entered`。 | 测试不能把 `monitorable=true/false` 当作 pair 状态已重建的证明。 |
| Godot issue `godotengine/godot#74988` | `ProcessMode.Disabled -> Inherit` 可能让 Area2D overlap 状态异常。 | `ProcessMode.Disabled` 不可作为唯一隔离策略，必须和脱树对照。 |
| 本地 Godot 4.6.2 源码 `scene/2d/physics/collision_object_2d.cpp:37-62,93-107` | 进树会设置 transform 和 world space；出树会 `area_set_space/body_set_space(RID())` 并 `_space_changed(RID())`。 | 脱树是测试的主路径：release 后必须断言物理根节点不在树中，并在旧位置不再触发。 |
| 本地 Godot 4.6.2 源码 `collision_object_2d.cpp:239-279` | `DISABLE_MODE_REMOVE` 的 `_apply_disabled/_apply_enabled` 本质也是 set space null / set world space。 | `collision_disable_mode_remove_control` 作为实验项：比较它是否减少 pair，但不得替换脱树默认。 |
| 本地 Godot 4.6.2 源码 `scene/2d/physics/area_2d.cpp:356-453` | `_space_changed(null)` 会 `_clear_monitoring()`；`set_monitoring/set_monitorable` 在 signal/flush 中被限制并提示 deferred。 | validation 要断言 release 后 monitoring map 被清空的可观测后果，而不是只读属性。 |
| 本地 Godot 4.6.2 源码 `godot_collision_object_2d.cpp:75-100,160-184,214-233` | shape disabled 会从 broadphase remove；重新启用会 pending update 并 create/move broadphase entry；set_space(null) 会从 old space 移除所有 shape broadphase id。 | `collision_shape_disable_recreates_pair` 作为负向/对照 check，说明只切 shape 不是完整隔离。 |
| 本地 Godot 4.6.2 源码 `godot_area_pair_2d.cpp:34-118,122-203` | pair setup/pre_solve 才把 add/remove 写入 query；area-area pair 构造时快照 `monitorable`；析构时可能排 remove query。 | artifact 必须记录事件来源 frame / position / entity id，防止旧 pair 析构/重建事件误判为新命中。 |
| 本地 Godot 4.6.2 源码 `godot_physics_server_2d.cpp:1290-1325,1369-1374` | physics step 先 `_update_shapes()`，之后 step；`flush_queries()` 再统一回调 query。 | scene 需要跨 physics step 采样，不能用 stdout `PASS` 或单帧属性读数作 oracle。 |

### 7.2 Inference

| 推断 | 裁决 |
| --- | --- |
| 这不是单纯“Godot bug 修完就不用管”的问题，而是 Godot Node 场景树、PhysicsServer RID、broadphase pair 和 Area query flush 叠在一起的时序约束。 | ObjectPool 仍按引擎约束设计隔离状态机，不等待上游修复。 |
| 其他 ECS / Unity Physics / Bevy 等框架较少出现同类问题，是因为它们不把 Node 场景树出入、物理 pair 队列和脚本 signal 混在同一个对象复用身份里。 | 采纳它们的 deferred structural boundary / stateful event validation 形态，不复制其 physics API。 |
| `await physics_frame`、`CallDeferred`、`SetDeferred` 都是调度工具，能减少回调期非法修改或旧 pair 竞态，但无法证明对象池对象已完成业务初始化。 | validation 的通过标准必须是显式状态 + 事件序列 + artifact checks，而不是延迟后没报错。 |

### 7.3 Adopt / Reject

| 项 | 决策 | SlimeAI 落点 |
| --- | --- | --- |
| 物理根节点回池脱树 | Adopt Now | `collision_area_release_detaches`、`collision_character_release_detaches` 作为 P0 validation checks。 |
| `Get(false)` 挂树但碰撞关闭 | Adopt Now | `collision_get_false_attached_disabled` 必须覆盖 Area2D monitoring/monitorable、shape disabled 和事件计数。 |
| 同帧或相邻帧立即复用 | Adopt Now | `collision_immediate_reuse_same_frame` 必须记录旧 entity / 旧坐标 / 旧 frame，旧位置 entered 必须为 0。 |
| `disable_mode=REMOVE` | Adopt Later | 增加 `collision_disable_mode_remove_control` 对照，不作为默认替代脱树。 |
| 只靠 `monitoring/monitorable` 或 `CollisionShape2D.disabled` | Reject | 只能作为隔离防线之一，不能作为 ObjectPool 默认退场策略。 |
| 等待一帧后判定通过 | Reject | 只能作为测试步骤之一，不能替代 artifact oracle 和 checks。 |

### 7.4 新增 validation checks

在 §2.2 的 Godot collision validation 表基础上，后续实现还应加入以下对照/负向 check：

| Check | 目的 | 通过标准 |
| --- | --- | --- |
| `collision_shape_disable_recreates_pair` | 证明 disabled -> enabled 会触发 broadphase remove/create 相关风险。 | 对照路径产生的 enter/exit 序列必须被记录；该路径不能被标记为默认隔离成功条件。 |
| `collision_area_pair_monitorable_snapshot` | 覆盖 area-area pair 构造时快照 `monitorable` 的风险。 | 切换 monitorable 后，不用属性值推断事件状态；必须以实际 signal/query 序列为准。 |
| `collision_disable_mode_remove_control` | 验证 `ProcessMode.Disabled + DisableMode.Remove` 与脱树的差异。 | 若该路径减少 pair，也只能标为 control/adopt-later；`Get(false)` 半初始化窗口仍需脱树路径覆盖。 |
| `collision_detach_clears_space_and_monitoring` | 验证脱树触发 space=null 后的监控清理效果。 | release 后节点 `IsInsideTree=false`，旧位置 entered/body_entered/area_entered 计数为 0，artifact 记录释放帧和下一 physics frame 结果。 |
