# ObjectPool 工具设计包

> 更新：2026-06-03
> 状态：current design package
> 入口：`README.md`
> 裁决：对象池保留，不做整体重写；默认碰撞策略改为 `ParkedInTree` 场外常驻：不脱树、不关碰撞、不改 layer/mask/shape，只隐藏、停处理、移动到 parking grid，并通过统一碰撞逻辑 guard 拦截回池对象和激活首帧事件。

## 0. 本设计包回答什么

这份设计包回答当前对象池相关的核心问题：

- 现在还要不要把“脱树 / 关碰撞”作为默认策略。
- `ObjectPool` 是否需要重构。
- Godot 碰撞更新延迟导致的幽灵碰撞应由谁负责。
- 在 AI-first ECS 方向下，对象池、Entity、Collision、Component 的边界应该怎么切。
- 后续代码重构和验证门禁应该按什么顺序推进。

结论先写清楚：**默认不脱树、不关碰撞。对象池负责场外常驻和运行时状态，Collision / Movement / Damage 等业务入口负责碰撞逻辑 guard。Detach 只保留为 fallback。**

2026-06-03 重新校准后补充裁决：

- 新思路的核心是 `ParkedInTree`：回池后仍在 `SceneTree`，仍保留碰撞体，不 `RemoveChild`，不 `SetCollisionTreeActive(false)`，不改 layer/mask/shape disabled。
- 回池只做：runtime state = InPool、`Visible=false`、`ProcessMode=Disabled`、移动到分散 parking grid。
- 激活时设置 `CollisionReadyPhysicsFrame = currentPhysicsFrame + 1`；激活第一帧默认不处理业务碰撞。
- `Entity.Data` 保持原样；对象池运行时状态放 `ObjectPoolRuntimeStateStore`，不污染 DataOS。
- `ContactDamageComponent` 的旧 timer / attacker 引用是独立 bug，必须在 timer tick 和接触入口查 pool runtime state。
- 当前源码仍是旧实现，后续代码迁移必须显式删除默认脱树 / 默认关碰撞路径；文档裁决先行。

2026-06-02 对 `Src/ECS/Tools/ObjectPool/Tests` 的补充裁决：

- 现有 `ObjectPoolVisualTest` / `ObjectPoolManagerTest` 保留为 manual demo，不再作为回归验证入口。
- 现有 demo 的根节点对象都是 `Node2D`，不能验证 `CollisionObject2D` 根节点脱树，也不能证明幽灵碰撞已解决。
- 测试池名不能再使用 `ProjectilePool` / `EffectPool` 这类真实池名，必须改成 `Test/ObjectPool/...` 或 `Demo/ObjectPool/...` 隔离名称。
- demo 退出时不应调用会污染全局状态的 `ObjectPoolManager.DestroyAll()`，除非该场景只创建隔离池并明确验证全局清理。
- 后续实现必须补两类自动化验证：Runtime contract checks 与 Godot collision validation scene；后者必须有 README 五字段、PASS artifact 和 `checks[]`。

## 1. 读取事实源

本轮已读取的本地事实源：

| 来源 | 结论 |
| --- | --- |
| `DocsAI/ECS框架与AIFirst方向决策.md` | 当前方向是 AI-first ECS，不是抛弃 ECS，也不是复制外部 GameOS；应保留 Entity / Data / Event / System 心智模型并强化契约、验证和观察面。 |
| `DocsAI/ECS/Capabilities/Collision/Concepts/Node2D父链约束.md` | 旧幽灵碰撞的一个根因是空间链路中插入普通 `Node`，导致 `Area2D` 等空间节点无法继承 `Node2D` transform；这属于场景结构问题，不应长期由对象池兜底。 |
| `DocsAI/ECS/Capabilities/Collision/Concepts/README.md` | 当前 Collision Concepts 已把场景结构、对象池兼容、碰撞层和历史问题分成不同职责。 |
| `DocsAI/ECS/Capabilities/Collision/Concepts/Godot物理时序与对象池碰撞.md` | Godot 物理 pair、deferred 与监控队列存在时序风险；这些分析继续保留，但默认应对策略改为场外常驻 + 逻辑 guard。 |
| `DocsAI/ECS/Tools/ObjectPool/Concept.md`、`Usage.md` | 当前文档曾把 detach-isolate-reattach 作为标准做法；本轮需要同步为 `ParkedInTree`。 |
| `DocsAI/ECS/Runtime/Entity/README.md`、`DocsAI/ECS/Runtime/Entity/EntityManager.md` | Entity hard cutover 目标是拆分 `EntityManager` 的 spawn / registry / component / destroy / pool 编排职责。 |
| `Src/ECS/Tools/ObjectPool/ObjectPool.cs` | 当前对象池仍实现 `NeedsTreeDetach(node is CollisionObject2D)`、泊车位、递归碰撞开关、`Get(false)` / `Activate()` 两阶段激活；这是待迁移的旧实现证据，不再是目标默认策略。 |
| `Src/ECS/Capabilities/Collision/Component/*` | `HurtboxComponent`、`CollisionComponent`、`PickupComponent` 仍各自处理 `Area2D` 信号和监控关闭；说明碰撞职责已经部分下沉到组件，但缺统一策略契约。 |
| `/home/slime/Code/SlimeAI/Resources/Engine/Docs` 与本地 Godot 源码资源 | 本地有 Godot 4.6.2 源码与引擎分析资料，可继续用于源码级验证；本设计不直接复制外部框架结构。 |
| `Src/ECS/Tools/ObjectPool/Tests/*` | 当前测试目录实际是 UI/鼠标 demo：`Control` 场景、随机位置、`Node2D` demo 对象、无 artifact oracle；需要重构为 demo + contract + validation 三层。 |

外部资料校准：

- Context7 `/godotengine/godot-docs` 与 `/websites/godotengine_en_4_6`：校准 `Area2D.monitoring`、`monitorable`、`CollisionShape2D.disabled`、`CollisionPolygon2D.disabled`、`Object.set_deferred` 和 `CollisionObject2D.disable_mode` 的官方语义。
- Godot 官方类文档和 issue `godotengine/godot#69407` / `#79464` / `#74988`：校准同帧移动或碰撞开关、`monitorable` 切换、`ProcessMode.Disabled` 与 Area2D 事件时序风险。
- Microsoft / Unity / Game Programming Patterns 对象池资料：校准通用对象池边界；对象池应处理复用、容量、reset policy 和统计，不应承载业务生命周期真相。
- `/home/slime/Code/SlimeAI/Resources/Engine/Docs/FrameworkAnalysis/Reports/godot-4.6.2-stable/12-Godot-源码分析报告.md`：校准 SlimeAI 不应直接绕过 Godot Node 代理去操作裸 PhysicsServer2D RID。
- `/home/slime/Code/SlimeAI/Resources/Engine/Docs/FrameworkAnalysis/Reports/Arch/04-Arch-源码分析报告.md` 与 `ET-Framework/13-ET-Framework-源码分析报告.md`：提供 pooled reference / generation / stale reference 的后续观察项，不要求当前立刻替换 EntityId。

## 2. 总裁决

采用 **AI-first Lifecycle Pool**：

```text
ObjectPool
  只负责复用、容量、活跃/闲置统计、池归属、Node 出入池基础状态、parking grid 和 runtime state

EntitySpawnPipeline / EntityDestroyPipeline
  负责 Data、Transform、Component 注册、生命周期事件、pool Activate/Release 编排

Collision Component / Collision Service
  负责信号绑定、业务碰撞事件 guard、组件自身退场闭环和 ContactDamage 旧引用清理

Observation / Scene Test
  证明回池对象不进入业务碰撞、激活首帧 embargo 生效、parking grid 不制造不可接受性能压力
```

### 2.1 是否继续默认脱树

**不继续默认脱树。**

新的默认策略是 `ParkedInTree`：

- 根节点是 `CollisionObject2D` 也不自动脱树。
- 回池后仍在树中、仍保留碰撞体。
- 不默认关闭 `monitoring/monitorable`、shape disabled、layer/mask。
- 碰撞是否进入业务由 runtime state guard 判断。
- 激活后的第一帧不处理碰撞，覆盖旧 dispatch / 同帧复用风险。

原因：如果默认仍脱树或仍关碰撞，新方案就失去意义。Godot 的旧帧 / deferred / pair 重建问题仍要保留分析，但默认应对方式从“物理退场”改为“避免碰撞属性切换 + 场外停放 + 业务逻辑验证”。`Detach` 只作为 fallback，用于 validation 证明 `ParkedInTree` 无法承受的特殊场景。

### 2.2 什么不再让对象池兜底

对象池不再被视为这些问题的最终归属：

- `Node` / `Node2D` 场景层级错误导致 transform 断链。
- Component 注册时机错误。
- Entity identity、Data 初始化、Visual 注入和 Component 反查。
- 碰撞层、阵营、伤害归因、owner 关系。
- `Area2D` 事件是否应该被业务接受。
- 场景切换时全局池生命周期的所有清理策略。

这些应分别进入 Entity、Collision、Data、Event、Lifecycle 和 Observation owner。

## 3. 当前实现判断

当前 `ObjectPool<T>` 不是完全错误，它已经吸收了大量真实项目经验：

- `Stack<T>` 复用，适合 LIFO 热对象。
- `InitialSize / MaxSize`，避免战斗中突发实例化和无限增长。
- `PoolStats` 与 `ObjectPoolObservability`，已有基础观测面。
- `IPoolable.OnPoolAcquire/Release/Reset`，有生命周期 hook。
- `Get(false)` + `Activate()`，允许 EntityManager 先注入位置和数据再恢复碰撞。
- 当前 `NeedsTreeDetach(Node node) => node is CollisionObject2D`、`SetCollisionTreeActive`、`RemoveChild` 是旧实现，后续要迁移为默认不执行。
- `PoolParkingPosition` 需要升级为 pool-aware parking grid，避免大量节点停在同一点。
- `Get(false)` + `Activate()` 仍保留，但 `Activate()` 负责设置 `CollisionReadyPhysicsFrame = currentPhysicsFrame + 1`，不是恢复碰撞属性。

真正的问题是：**这些策略都藏在一个泛型工具类里，AI 看到 `ObjectPool` 时无法立刻判断哪些是复用职责，哪些是 Godot 物理隔离策略，哪些是 Entity spawn 时序补丁。**

## 4. 问题拆分

### 4.1 历史包袱 A：对象池承担过多隐式策略

当前 `ObjectPool<T>` 同时做：

- 对象创建和预热。
- 父节点路径注册。
- Node 命名和 Meta。
- 显隐与处理模式。
- 递归碰撞禁用。
- 泊车位移动。
- 根节点脱树和挂回。
- 两阶段激活。
- 生命周期回调。
- 活跃对象集合。
- 统计和全局 manager 注册。

这些功能不是都错，但它们没有清晰的策略对象或阶段对象。AI 修改时容易误删“看起来多余”的防线。

### 4.2 历史包袱 B：碰撞物理状态和业务过滤混在一起

旧策略试图让对象池保证“退场时尽量不在物理世界里”。新策略不再要求默认物理退场；对象池只提供 runtime state。但无论哪种策略，对象池都不能保证：

- 业务是否应该处理这次 `entered`。
- 目标实体是否仍然 alive。
- 对方是否还属于有效 team / faction。
- 这次事件是否来自旧帧队列。

这些必须由 Collision / Damage / Entity lifecycle 共同完成。对象池只能提供 pool runtime state、parking position 和可观测状态。

### 4.3 历史包袱 C：`SetDeferred` 是安全 API，不是完整状态机

`SetDeferred` 适合避免在物理 query flush 期间直接修改属性，但它不等于“立即退出物理世界”。当前实现已经用 `ForceDisableCollisionsDirect` 覆盖同帧回收再出池窗口，这说明单靠 deferred 不是完整策略。

AI-first 的规则应写成：

```text
默认退场：ParkedInTree，不脱树、不关碰撞、不改 layer/mask/shape。
碰撞属性修改：仅用于组件真实卸载、fallback 或显式验证场景。
业务过滤：用于 signal / movement / damage 入口判断事件是否仍有效。
激活保护：Activate 后第一 physics frame 默认不处理碰撞。
```

### 4.4 历史包袱 D：场景结构错误不应由池子补偿

`DocsAI/ECS/Capabilities/Collision/Concepts/Node2D父链约束.md` 已经说明，普通 `Node` 插在 `Node2D` 空间链路中会让空间节点无法继承 transform。这个问题的解决方式是：

- 场景结构门禁。
- Component / preset 基类约束。
- Godot scene test 可视化或日志验证。

不是在对象池里继续加更多“远点停放”“延迟帧”“距离二次确认”。

## 5. 目标架构

### 5.1 保留的核心 API

短期不建议大改公开 API：

- `ObjectPool<T>.Get(bool activateNode = true)`
- `ObjectPool<T>.Activate(T obj)`
- `ObjectPool<T>.Release(T obj)`
- `ObjectPoolManager.GetPool<T>(string name)`
- `ObjectPoolManager.ReturnToPool(object instance)`
- `IPoolable`
- `PoolStats`

原因：当前 Entity、Timer、UI、Projectile、Effect、Ability 已经依赖这些入口。立即换 API 会扩大 blast radius。

### 5.2 新增概念：Pool Lifecycle Strategy / Runtime State Store

建议把当前散落在 `ObjectPool<T>` 内的策略拆成内部策略类，不急着暴露给业务层：

```text
PoolNodeLifecycleStrategy
  ApplyInactive(Node node, PoolLifecycleContext context)
  PrepareForAcquire(Node node, PoolLifecycleContext context)
  ApplyActive(Node node, PoolLifecycleContext context)
  Discard(Node node, PoolLifecycleContext context)

PoolParkingStrategy
  AllocateParkingPosition(Node node, PoolLifecycleContext context)
  Park(Node2D node, Vector2 position)
  ForceUpdateTransformIfNeeded(Node2D node)

ObjectPoolRuntimeStateStore
  GetOrCreate(Node node)
  MarkReleased(Node node, frame, parkingPosition)
  MarkActivated(Node node, frame, collisionReadyFrame)
  IsCollisionLogicActive(Node node, frame)
```

第一阶段可以仍放在 `Src/ECS/Tools/ObjectPool/` 下，不需要新依赖，不改业务调用点。重构目标是让 AI 能读出职责，而不是制造新抽象层级。

### 5.3 Entity pipeline 中的边界

Entity hard cutover 后，推荐顺序是：

```text
Acquire
  EntityNodeFactory 从池获取 node: pool.Get(false)

Initialize
  EntityRegistry 分配 typed EntityId
  EntityDataInitializer apply runtime snapshot
  EntityVisualInitializer 注入视觉
  EntityTransformInitializer 设置 GlobalPosition / Rotation / ForceUpdateTransform
  ComponentRegistrar 注册组件

Activate
  pool.Activate(entity)
  emit typed Entity lifecycle event
  CharacterBody2D 如需要再 deferred MoveAndSlide
```

这个顺序的核心是：**对象池不在 `Get(false)` 时触发业务激活；Entity 初始化完成后才恢复碰撞与生命周期回调。**

### 5.4 Collision owner 中的边界

Collision 系统应补齐三类契约：

- 组件退场：`OnComponentUnregistered` 必须解绑信号、清理引用；只有组件真实卸载时才关闭自身监控。
- 事件过滤：收到 `entered/exited` 时必须确认自身实体、目标实体和生命周期状态仍有效。
- 场景结构：物理/视觉节点的父链必须是空间节点，不能被普通 `Node` 阻断。

对象池只提供“我当前在池中 / 碰撞逻辑是否 active / ready frame / parking position / fallback 是否启用”的状态，不替 Collision 判断业务有效性。

## 6. 推荐重构路线

### 6.1 P0：文档和门禁先行

- `DocsAI/ECS/Tools/ObjectPool/Concept.md` 更新：明确默认 `ParkedInTree`，不脱树、不关碰撞。
- `DocsAI/ECS/Tools/ObjectPool/Usage.md` 更新：补充 activation-frame embargo、parking grid 和 guard 调用点。
- `DocsAI/ECS/Capabilities/Collision/*` 更新：说明对象池只提供 pool runtime state，业务碰撞入口必须 guard。
- owner skill 更新：修改 ObjectPool / Collision / Entity 相关实现时必须同步对应文档和验证。

### 6.2 P1：迁移默认对象池策略

不改外部 API，先把 `ObjectPool<T>` 内部默认行为迁移到 `ParkedInTree`：

- `Release`：标记 `IsInPool=true`、`CollisionLogicActive=false`，隐藏、停处理、移动到 parking grid。
- `Activate`：标记 `CollisionLogicActive=true`，设置 `CollisionReadyPhysicsFrame=currentPhysicsFrame+1`，显示、恢复处理。
- 默认不 `RemoveChild`。
- 默认不调用 `SetCollisionTreeActive(false)`。
- 默认不改 layer/mask/shape。

### 6.3 P2：补显式状态与观测

为 Node 池化对象补显式状态：

```text
PooledInactive
AcquiredUninitialized
ActiveButCollisionEmbargoed
Active
Releasing
Discarded
```

观测字段至少包括：

- `poolName`
- `nodeName`
- `nodeType`
- `state`
- `insideTree`
- `inPoolMeta`
- `collisionLogicActive`
- `collisionReadyPhysicsFrame`
- `parkingPosition`
- `detachFallbackEnabled`
- `activeCount`
- `idleCount`
- `lastAcquireFrame`
- `lastReleaseFrame`

这些字段用于 scene test 和 AI debug，不进入 `Entity.Data`。

### 6.4 2026-06-03 ResearchAdoption 复核

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
  reason: 复核对象池碰撞隔离是否取消默认脱树，并校准 ParkedInTree / logic guard / activation frame embargo 的 oracle。
  expires: current-task
copiedCodeOrAssets: none
```

#### Evidence

| 证据 | 结论 |
| --- | --- |
| Godot 4.6 docs / Context7 | `Area2D` overlap 列表在 physics step 更新，不会在对象移动后立即同步；`Object.set_deferred()` 在帧末赋值。 |
| Godot issue `#69407` | 同一 physics frame 中移动出 Area 并恢复 collision mask 仍可能触发 `body_entered`；新策略默认不切 mask，避开该类重建风险。 |
| Godot issue `#79464` | `monitorable` toggle 对已有重叠 Area2D 不保证重新发 enter；新策略默认不切 monitorable。 |
| Godot issue `#74988` | `ProcessMode.Disabled -> Inherit` 与 Area2D overlap 存在异常案例；ProcessMode 不能单独当作业务正确性证明。 |
| 本地 Godot 4.6.2 源码分析 | shape / layer / monitorable / tree enter-exit 都会触发不同物理状态机；文档保留这些风险作为 fallback 和 validation 对照。 |
| Resources/Engine Unity / Bevy / IFramework 分析 | 外部框架可采纳的是 stateful event validation、deferred structural boundary、pool capacity/reset/observability；不复制它们的物理 API。 |

#### Inference

| 推断 | 设计落点 |
| --- | --- |
| Godot 对象池碰撞问题不是单点 bug，而是 Node 场景树、PhysicsServer RID、broadphase pair 和 Area signal queue 的组合时序。 | SlimeAI 不再把默认策略绑定到脱树；改为 runtime state + logic guard，并保留 fallback。 |
| 不切 layer/mask/shape/monitoring 可以减少 pair 重建窗口，但不能消灭已排队 signal。 | 激活第一帧不处理碰撞成为默认规则。 |
| 仅靠属性值读数不能证明业务正确。 | Godot validation scene 必须输出 `checks[]`、事件列表、release/acquire/activate/ready frame。 |

#### Adopt Now / Later / Reject

| 候选机制 | 决策 | 原因 |
| --- | --- | --- |
| `ParkedInTree` | Adopt Now | 符合新方案核心：不脱树、不关碰撞，只停业务处理并场外停放。 |
| 激活第一帧不处理碰撞 | Adopt Now | 低成本覆盖旧 signal dispatch / 同帧复用风险。 |
| `ObjectPoolRuntimeStateStore` | Adopt Now | 不污染 `Entity.Data`，比继续扩展 `Node.SetMeta` 更集中可测。 |
| Godot collision validation artifact | Adopt Now | 没有结构化事件序列，就无法区分旧 signal、停车区事件和新命中。 |
| Detach / `disable_mode=REMOVE` | Adopt Later | 仅作为 fallback / control check，不作为默认策略。 |
| 默认切 `monitoring/monitorable/disabled/layer=0` | Reject | 这会重新引入属性切换、deferred 和 pair 重建窗口。 |
| 复制 Unity Physics / Bevy physics API | Reject | SlimeAI 当前问题是 Godot Node/PhysicsServer 时序，不是缺少底层物理 DSL。 |

### 6.5 P3：补 Godot 场景验证

最小验证场景：

1. 敌人在玩家身上死亡并回池，同帧或下一帧在远处出池；旧 signal 不应进入业务伤害。
2. `Area2D` 投射物命中后回池并立即复用；激活第一帧不应触发新子弹误命中/销毁。
3. 大量回池对象停在 parking grid；停车区 pair / event / frame time 不超过阈值。
4. Component 父链中插入普通 `Node` 的负向场景应被检测出来，不能再误判为对象池问题。
5. fallback detach 对照场景只证明 fallback 可用，不作为默认成功条件。

### 6.6 P3 补充：重构 `Src/ECS/Tools/ObjectPool/Tests`

本阶段不直接把现有 UI demo 改成验证场景，而是拆分测试职责：

| 层级 | 目标文件 | 裁决 |
| --- | --- | --- |
| Runtime contract | `Src/ECS/Tools/ObjectPool/Tests/ObjectPoolContractRuntimeTest.cs` | 自动验证池容量、统计、重复归还、active snapshot、静态归还、manager mapping 和测试池隔离。 |
| Collision validation | `Src/ECS/Tools/ObjectPool/Tests/ObjectPoolCollisionIsolationValidation.cs/.tscn` | 自动验证 `Area2D` / `CharacterBody2D` 根节点场外常驻、激活首帧 guard、parking grid 压力和 fallback detach 对照。 |
| Scene README | `Src/ECS/Tools/ObjectPool/Tests/README.md` | 提供 `expectedInputs / expectedObservations / passCriteria / failCriteria / artifactPath`。 |
| Manual demo | 当前 `ObjectPoolVisualTest` / `ObjectPoolManagerTest` | 保留演示价值，改名或 README 标记为 legacy/manual demo，不作为 PASS/FAIL。 |

`ObjectPoolCollisionIsolationValidation` 的最小 check：

- `collision_area_release_parked_in_tree`
- `collision_character_release_parked_in_tree`
- `collision_activate_first_frame_embargo`
- `collision_activate_after_ready_frame`
- `collision_immediate_reuse_same_frame`
- `collision_parking_grid_pressure`
- `collision_detach_fallback_control`
- `collision_artifact_oracle_complete`

PASS artifact 建议：

```text
.ai-temp/scene-tests/artifacts/objectpool-collision-isolation-validation.json
```

Verifier 不能用“无 error”“exit code 0”或 stdout `PASS` 替代 artifact oracle。通过声明必须同时检查 run `index.json`、per-scene `result.json`、scene artifact、artifact 五字段和 `checks[]`。

## 7. 不推荐方向

不建议：

- **默认脱树 / 默认关碰撞**：这会让新方案失去意义，并继续承担 `RemoveChild/AddChild`、deferred 和碰撞属性恢复窗口。
- **默认只靠 `monitoring/disabled/layer=0`**：这会回到历史问题，尤其是同帧回收复用和 Godot 物理事件队列场景。
- **把所有 Node 都脱树**：UI、Timer、纯组件容器和非物理节点不需要承受 reparent 成本，也不应触发 `_EnterTree/_ExitTree` 副作用。
- **把对象池升级成 EntityManager 替代品**：对象池不应注册组件、不应写 Data、不应决定 owner、不应发业务事件。
- **给 `Entity.Data` 加不清空分区保存池状态**：pool lifecycle 是 runtime infrastructure，不是 DataOS descriptor-first gameplay data。
- **继续用自然语言日志调碰撞问题**：必须输出结构化池状态、节点树状态、碰撞启停状态和事件来源。

## 8. 默认策略表

| 类型 | 根节点 | 是否池化 | 默认策略 |
| --- | --- | --- | --- |
| Enemy | `CharacterBody2D` | 是 | `ParkedInTree`；回池场外常驻；激活首帧不处理 ContactDamage / Hurtbox。 |
| Player | `CharacterBody2D` | 通常否 | 不进池；常驻 Entity 生命周期。 |
| Projectile | `Area2D` | 是 | `ParkedInTree`；命中/过期回池后旧 signal 不进业务；新子弹 ready frame 后才可命中。 |
| Effect | `Area2D` 或 `Node2D` | 是 | `ParkedInTree`；纯视觉仍只做显隐和处理模式。 |
| AbilityEntity | `Node2D`/容器 | 是 | 不按能力名处理碰撞；按具体业务碰撞入口走 guard。 |
| HealthBarUI / DamageNumberUI | `Control` | 是 | 不脱树；只做显隐、处理模式和 UI 绑定清理。 |
| GameTimer / 纯 C# 对象 | 非 Node | 是 | 不走 Godot Node 生命周期；只清理字段和归属映射。 |

## 9. 完成定义

ObjectPool 重构完成不是“没有幽灵碰撞日志”。

必须同时满足：

- 物理根节点池化对象默认 `ParkedInTree`，且策略显式可读。
- 默认不 `RemoveChild`、不关碰撞、不改 layer/mask/shape。
- `Get(false)` / `Activate()` 的两阶段语义被 Entity 文档和代码共同保证。
- Collision / Movement / ContactDamage 入口有 pool runtime state guard，不依赖对象池独自保证业务正确。
- Pool 观测能显示 `CollisionLogicActive`、`CollisionReadyPhysicsFrame`、parking position 和 fallback 策略。
- Godot scene test 能复现并验证旧 signal 不进入业务、停车区压力可控、fallback detach 可作为对照。
- DocsAI / Skill / SDD 索引同步，AI 下次能从 owner 入口找到这份裁决。
