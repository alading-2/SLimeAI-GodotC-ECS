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
| `TestProjectile.cs` / `TestEffect.cs` / `VisualTestBullet.cs` | demo 用 `Node2D` 对象 | 演示 `IPoolable`、生命周期、静态归还 | 根节点不是 `CollisionObject2D`，无法覆盖场外常驻、激活首帧 guard、parking grid 压力和 fallback 对照。 |

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

目标：验证当前裁决中的 `ParkedInTree + CollisionLogicGuard + ActivationFrameEmbargo` 确实覆盖 Godot 物理根节点复用风险。

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
| `collision_area_release_parked_in_tree` | 根节点 `Area2D` 回池 | 节点仍在树中，隐藏、停处理、位于 parking grid，`CollisionLogicActive=false`。 |
| `collision_character_release_parked_in_tree` | 根节点 `CharacterBody2D` 回池 | 节点仍在树中，位于 parking grid，速度清零或不再由业务驱动，`CollisionLogicActive=false`。 |
| `collision_activate_first_frame_embargo` | `Activate()` 后第一 physics frame | 即使收到 entered，也被 guard 丢弃，不触发业务事件 / damage / destroy。 |
| `collision_activate_after_ready_frame` | 到达 `CollisionReadyPhysicsFrame` 后 | 只处理新位置产生的期望碰撞。 |
| `collision_immediate_reuse_same_frame` | 同帧或相邻帧 release → get(false) → activate | 旧 signal 不进入业务；artifact 记录 release/acquire/activate/ready frame。 |
| `collision_parking_grid_pressure` | 大量对象回池停放 | 停车区 pair / event / frame time 在阈值内。 |
| `collision_detach_fallback_control` | 显式启用 fallback detach | fallback 可用但不作为默认成功条件。 |
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
- 物理根节点回池后仍在树中，位于 parking grid。
- 回池对象 `CollisionLogicActive=false`，不会进入业务碰撞。
- activate 后第一 physics frame 被 guard 丢弃。
- ready frame 后只处理新位置期望碰撞。
- fallback detach 只作为对照路径。

passCriteria:
- artifact status=pass。
- checks[] 中所有声明 check 为 pass。
- 旧 signal / 停车区事件没有进入业务事件、Damage 或销毁逻辑。
- parking grid 压力在阈值内。
- failureReasons=[]。

failCriteria:
- 缺 README 五字段或 artifactPath。
- 只有 stdout PASS marker。
- 回池对象进入业务碰撞。
- 激活首帧触发业务命中。
- 默认路径发生 RemoveChild、关碰撞或 layer/mask/shape 修改。
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
  "expectedObservations": "collision roots stay parked in tree while collision logic guard blocks pooled and first-frame events",
  "passCriteria": "all checks pass, stale events do not enter business logic, and parking grid pressure is within threshold",
  "failCriteria": "any missing oracle field, pooled business collision, first-frame hit, default detach/collision-disable, or real pool name collision",
  "artifactPath": ".ai-temp/scene-tests/artifacts/objectpool-collision-isolation-validation.json",
  "checks": [],
  "poolStats": {},
  "nodeStates": [],
  "collisionEvents": [],
  "businessCollisionEvents": [],
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

- Godot 4.6 `Area2D.get_overlapping_bodies()`：overlap 列表每个 physics step 更新一次，不会在对象移动后立即同步。
- Godot 4.6 `Object.set_deferred()`：属性在当前帧末尾赋值；它是安全提交工具，不是立即退场语义。
- Godot 4.6 `CollisionShape2D.disabled`：官方文档要求用 `Object.set_deferred()` 修改；新策略默认不切 shape，避免引入 pair 重建窗口。
- Godot 4.6 `Area2D.monitoring/monitorable`：只控制检测与被检测；新策略默认不切它们。
- Godot `CollisionObject2D.disable_mode`：只在 `ProcessMode.Disabled` 语义下定义物理行为；可作为 fallback / control check，不作为默认策略。
- Unity / Game Programming Patterns 对象池资料：对象池的稳定边界是复用、容量、reset、统计和归属；业务生命周期、碰撞归因和实体有效性仍应由上层 owner 判断。

## 6. 实现前裁决

- 先补 `README.md` 五字段和 validation scene skeleton，再改实现；Godot 场景按 `SceneValidationSession.Check()` 或等价 artifact check 进入 RED/GREEN。
- 不把现有 UI demo 硬改成自动化门禁；demo 和 validation 分离。
- 默认取消物理根节点脱树和关碰撞；`Detach` 只作为 fallback / control check。
- 不用“延迟一帧后没有报错”作为通过标准；必须证明旧 signal 没进业务。
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
  reason: 校准对象池碰撞隔离是否应取消默认脱树，以及 validation scene 应覆盖哪些引擎时序风险。
  expires: current-task
copiedCodeOrAssets: none
```

### 7.1 Evidence

| 来源 | 证据 | 对测试设计的影响 |
| --- | --- | --- |
| Context7 / Godot 4.6 `Area2D` docs | `get_overlapping_areas/bodies` 列表在 physics step 中更新，不会在移动对象后立即同步。 | validation 不能只移动节点后立即读 overlap；必须记录物理帧后的事件序列、业务事件和 ready frame。 |
| Context7 / Godot 4.6 `Object.set_deferred` docs | 属性在当前帧末尾赋值。 | 默认策略不依赖切碰撞属性；fallback / 对照路径才验证 deferred 影响。 |
| Godot issue `godotengine/godot#69407` | 同一函数/同一 physics frame 中先移动出 Area 再恢复 `collision_mask`，仍可能补发 `body_entered`。 | 新场景必须覆盖同帧或相邻帧 release -> get(false) -> activate，且旧 signal 不得进入业务。 |
| Godot issue `godotengine/godot#79464` | toggle `monitorable` 时，已有重叠的 Area2D 不一定重新触发 `area_entered`。 | 默认不切 monitorable；对照路径必须记录事件序列。 |
| Godot issue `godotengine/godot#74988` | `ProcessMode.Disabled -> Inherit` 可能让 Area2D overlap 状态异常。 | `ProcessMode.Disabled` 不是业务正确性证明，必须配合 runtime state guard。 |
| 本地 Godot 4.6.2 源码分析 | shape、layer、monitorable、tree enter/exit 都进入不同物理状态机。 | 保留这些风险作为 fallback 和 validation 对照，不再作为默认脱树裁决。 |

### 7.2 Inference

| 推断 | 裁决 |
| --- | --- |
| 这不是单纯“Godot bug 修完就不用管”的问题，而是 Godot Node 场景树、PhysicsServer RID、broadphase pair 和 Area query flush 叠在一起的时序约束。 | ObjectPool 通过 runtime state + guard + validation 处理，不等待上游修复。 |
| 其他 ECS / Unity Physics / Bevy 等框架较少出现同类问题，是因为它们不把 Node 场景树出入、物理 pair 队列和脚本 signal 混在同一个对象复用身份里。 | 采纳它们的 deferred structural boundary / stateful event validation 形态，不复制其 physics API。 |
| `await physics_frame`、`CallDeferred`、`SetDeferred` 都是调度工具，能减少回调期非法修改或旧 pair 竞态，但无法证明对象池对象已完成业务初始化。 | validation 的通过标准必须是显式 pool state + ready frame + 业务事件序列 + artifact checks。 |

### 7.3 Adopt / Reject

| 项 | 决策 | SlimeAI 落点 |
| --- | --- | --- |
| `ParkedInTree` | Adopt Now | `collision_area_release_parked_in_tree`、`collision_character_release_parked_in_tree` 作为 P0 validation checks。 |
| 激活第一帧不处理碰撞 | Adopt Now | `collision_activate_first_frame_embargo` 必须证明业务事件 / damage / destroy 未触发。 |
| 同帧或相邻帧立即复用 | Adopt Now | `collision_immediate_reuse_same_frame` 必须记录 release/acquire/activate/ready frame，旧 signal 不进入业务。 |
| `ObjectPoolRuntimeStateStore` | Adopt Now | validation artifact 必须记录 `CollisionLogicActive`、`CollisionReadyPhysicsFrame` 和 parking position。 |
| `disable_mode=REMOVE` / detach | Adopt Later | 增加 `collision_detach_fallback_control` 对照，不作为默认替代。 |
| 默认切 `monitoring/monitorable` 或 `CollisionShape2D.disabled` | Reject | 这会重新引入属性切换、deferred 和 pair 重建窗口。 |
| 等待一帧后判定通过 | Reject | 只能作为测试步骤之一，不能替代 artifact oracle 和 checks。 |

### 7.4 新增 validation checks

在 §2.2 的 Godot collision validation 表基础上，后续实现还应加入以下对照/负向 check：

| Check | 目的 | 通过标准 |
| --- | --- | --- |
| `collision_shape_disable_recreates_pair` | 证明 disabled -> enabled 会触发 broadphase remove/create 相关风险。 | 对照路径产生的 enter/exit 序列必须被记录；该路径不能被标记为默认策略。 |
| `collision_area_pair_monitorable_snapshot` | 覆盖 area-area pair 构造时快照 `monitorable` 的风险。 | 切换 monitorable 后，不用属性值推断事件状态；必须以实际 signal/query 序列为准。 |
| `collision_detach_fallback_control` | 验证 fallback detach 与默认 `ParkedInTree` 的差异。 | fallback 可用但只标为 control/adopt-later；默认路径仍必须场外常驻。 |
