# ObjectPool 工具设计包

> 更新：2026-05-31
> 状态：current design package
> 入口：`README.md`
> 裁决：对象池保留，但必须从“替碰撞系统兜底”的历史状态收口为 AI-first 生命周期工具；根节点参与物理的池化对象继续默认脱树，后续重构重点是策略显式化、生命周期分层和可观测验证。

## 0. 本设计包回答什么

这份设计包回答当前对象池相关的核心问题：

- 现在还要不要“脱树”。
- `ObjectPool` 是否需要重构。
- Godot 碰撞更新延迟导致的幽灵碰撞应由谁负责。
- 在 AI-first ECS 方向下，对象池、Entity、Collision、Component 的边界应该怎么切。
- 后续代码重构和验证门禁应该按什么顺序推进。

结论先写清楚：**要继续脱树，但不要把脱树当成对象池唯一职责，也不要让对象池继续无限吸收碰撞、Entity、业务状态和组件注册问题。**

## 1. 读取事实源

本轮已读取的本地事实源：

| 来源 | 结论 |
| --- | --- |
| `DocsAI/ECS框架与AIFirst方向决策.md` | 当前方向是 AI-first ECS，不是抛弃 ECS，也不是复制外部 GameOS；应保留 Entity / Data / Event / System 心智模型并强化契约、验证和观察面。 |
| `DocsAI/思考/碰撞问题/幽灵碰撞问题深度分析.md` | 旧幽灵碰撞的一个根因是空间链路中插入普通 `Node`，导致 `Area2D` 等空间节点无法继承 `Node2D` transform；这属于场景结构问题，不应长期由对象池兜底。 |
| `DocsOld/框架/ECS/Collision/碰撞问题需要注意.md` | 仅靠 `ProcessMode`、`monitoring`、`SetDeferred`、远点停放都不是完整退场语义；对象池兜底和组件内聚需要分层。 |
| `DocsOld/框架/ECS/Collision/对象池碰撞兼容说明.md` | Godot 物理 pair、deferred 与监控队列存在时序风险；历史实现选择“泊车位 + 脱树 + 同步禁用 + 延迟激活”。 |
| `DocsAI/ECS/Tools/ObjectPool/Concept.md`、`Usage.md` | current 文档已经把 detach-isolate-reattach 作为当前标准做法。 |
| `DocsAI/ECS/Entity/README.md`、`EntityManager.md` | Entity hard cutover 目标是拆分 `EntityManager` 的 spawn / registry / component / destroy / pool 编排职责。 |
| `Src/ECS/Tools/ObjectPool/ObjectPool.cs` | 当前对象池已实现 `NeedsTreeDetach(node is CollisionObject2D)`、泊车位、递归碰撞开关、`Get(false)` / `Activate()` 两阶段激活、`IPoolable` 生命周期和活跃对象统计。 |
| `Src/ECS/Base/Component/Collision/*` | `HurtboxComponent`、`CollisionComponent`、`PickupComponent` 仍各自处理 `Area2D` 信号和监控关闭；说明碰撞职责已经部分下沉到组件，但缺统一策略契约。 |
| `/home/slime/Code/SlimeAI/Resources/Engine/Docs` 与本地 Godot 源码资源 | 本地有 Godot 4.6.2 源码与引擎分析资料，可继续用于源码级验证；本设计不直接复制外部框架结构。 |

外部资料校准：

- Godot 官方类文档用于确认 `Area2D`、`Node`、`Object.set_deferred/call_deferred` 等 API 的语义边界。
- Godot issue `godotengine/godot#92691` 用于校准 `Area2D` / shape 禁用启用与监控状态变化相关的事件时序风险。
- .NET / Unity 对象池资料用于校准通用对象池边界：对象池核心价值是减少高频分配和销毁，不应承载业务生命周期真相。

## 2. 总裁决

采用 **AI-first Lifecycle Pool**：

```text
ObjectPool
  只负责复用、容量、活跃/闲置统计、池归属、Node 出入池基础状态

CollisionIsolationPolicy
  显式描述节点是否需要脱树、泊车、同步禁用、延迟激活

EntitySpawnPipeline / EntityDestroyPipeline
  负责 Data、Transform、Component 注册、生命周期事件、pool Activate/Release 编排

Collision Component / Collision Service
  负责信号绑定、监控语义、业务碰撞事件过滤和组件自身退场闭环

Observation / Scene Test
  证明没有幽灵碰撞、没有旧坐标激活、没有池状态泄漏
```

### 2.1 是否继续脱树

**继续。**

只要池化对象满足以下条件之一，默认继续走脱树：

- 根节点是 `CollisionObject2D`，例如 `CharacterBody2D`、`Area2D`。
- 业务依赖 `Area2D` 的 `body_entered / area_entered / exited` 事件。
- 对象会高频复用，并且会在死亡点、命中点或重叠区域回池。
- 对象重新出池前需要先完成 Data / Transform / Component / Visual 初始化。

原因：脱树是当前已知最稳定的物理退场语义。它让节点离开物理世界，再由 spawn pipeline 在新位置完成初始化后统一激活。相比只切换 `Monitoring`、`CollisionShape2D.Disabled`、`CollisionLayer` 或 `ProcessMode`，脱树更接近“生命周期状态切换”，而不是赌物理帧时序。

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
- `NeedsTreeDetach(Node node) => node is CollisionObject2D`，已经把脱树限制在物理根节点。
- `PoolParkingPosition` + `RemoveChild` + `ReattachToTree` + `ForceDisableCollisionsDirect`，是为 Godot 物理时序付出的合理代价。

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

### 4.2 历史包袱 B：碰撞退场和业务过滤混在一起

对象池能保证“退场时尽量不在物理世界里”。但它不能保证：

- 业务是否应该处理这次 `entered`。
- 目标实体是否仍然 alive。
- 对方是否还属于有效 team / faction。
- 这次事件是否来自旧帧队列。

这些必须由 Collision / Damage / Entity lifecycle 共同完成。对象池只能提供物理隔离和可观测状态。

### 4.3 历史包袱 C：`SetDeferred` 是安全 API，不是完整状态机

`SetDeferred` 适合避免在物理 query flush 期间直接修改属性，但它不等于“立即退出物理世界”。当前实现已经用 `ForceDisableCollisionsDirect` 覆盖同帧回收再出池窗口，这说明单靠 deferred 不是完整策略。

AI-first 的规则应写成：

```text
直接修改碰撞状态：只允许在确定非物理回调、且用于挂树后同步封锁窗口。
延迟修改碰撞状态：用于常规安全点提交。
脱树：用于根节点参与物理世界的池化对象退场。
业务过滤：用于信号到达后判断事件是否仍有效。
```

### 4.4 历史包袱 D：场景结构错误不应由池子补偿

`DocsAI/思考/碰撞问题/幽灵碰撞问题深度分析.md` 已经说明，普通 `Node` 插在 `Node2D` 空间链路中会让空间节点无法继承 transform。这个问题的解决方式是：

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

### 5.2 新增概念：Pool Lifecycle Strategy

建议把当前散落在 `ObjectPool<T>` 内的策略拆成内部策略类，不急着暴露给业务层：

```text
PoolNodeLifecycleStrategy
  ApplyInactive(Node node, PoolLifecycleContext context)
  PrepareForAcquire(Node node, PoolLifecycleContext context)
  ApplyActive(Node node, PoolLifecycleContext context)
  Discard(Node node, PoolLifecycleContext context)

CollisionIsolationStrategy
  ShouldDetach(Node node)
  Park(Node2D node)
  DisableDeferred(Node node)
  DisableDirectAfterReattach(Node node)
  EnableDeferred(Node node)
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

- 组件退场：`OnComponentUnregistered` 必须解绑信号、清理引用、关闭监控。
- 事件过滤：收到 `entered/exited` 时必须确认自身实体、目标实体和生命周期状态仍有效。
- 场景结构：物理/视觉节点的父链必须是空间节点，不能被普通 `Node` 阻断。

对象池只提供“我当前在池中 / 活跃 / 已脱树 / 已挂树但未激活”的状态，不替 Collision 判断业务有效性。

## 6. 推荐重构路线

### 6.1 P0：文档和门禁先行

- `DocsAI/ECS/Tools/ObjectPool/Concept.md` 更新：明确脱树不是临时补丁，而是物理根节点池化对象的默认隔离策略。
- `DocsAI/ECS/Tools/ObjectPool/Usage.md` 更新：补充策略表和禁止事项。
- `DocsAI/ECS/Collision/*` 更新：说明对象池只负责物理退场，碰撞事件过滤属于 Collision owner。
- owner skill 更新：修改 ObjectPool / Collision / Entity 相关实现时必须同步对应文档和验证。

### 6.2 P1：把策略从泛型池里拆出来

不改外部 API，只做文件内或同目录类拆分：

- `PoolNodeLifecycleStrategy`
- `CollisionIsolationStrategy`
- `PoolLifecycleContext`
- `PoolNodeMetadata`

拆分后 `ObjectPool<T>` 保持只读起来像对象池：

```text
CreateNew -> Warmup -> Get -> Activate -> Release -> Cleanup -> Clear
```

Godot 物理细节集中到策略类，注释说明为什么存在每一道防线。

### 6.3 P2：补显式状态与观测

为 Node 池化对象补显式状态：

```text
PooledInactive
AcquiredUninitialized
AttachedCollisionDisabled
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
- `needsDetach`
- `activeCount`
- `idleCount`
- `lastAcquireFrame`
- `lastReleaseFrame`

这些字段用于 scene test 和 AI debug，不要求全部进 Data。

### 6.4 P3：补 Godot 场景验证

最小验证场景：

1. 敌人在玩家身上死亡并回池，同帧或下一帧在远处出池，不应触发旧位置 hurtbox。
2. `Area2D` 投射物在命中点回池并立即复用，不应补发旧 `entered`。
3. 普通 UI / Timer / 非碰撞 Node 池不脱树，不受碰撞策略影响。
4. Component 父链中插入普通 `Node` 的负向场景应被检测出来，不能再误判为对象池问题。

## 7. 不推荐方向

不建议：

- **取消脱树，只靠 `monitoring/disabled/layer=0`**：这会回到历史问题，尤其是同帧回收复用和 Godot 物理事件队列场景。
- **把所有 Node 都脱树**：UI、Timer、纯组件容器和非物理节点不需要承受 reparent 成本，也不应触发 `_EnterTree/_ExitTree` 副作用。
- **把对象池升级成 EntityManager 替代品**：对象池不应注册组件、不应写 Data、不应决定 owner、不应发业务事件。
- **用一帧延迟掩盖问题**：`await physics_frame` 或 `CallDeferred` 可以作为调度工具，但不能成为状态正确性的唯一证明。
- **继续用自然语言日志调碰撞问题**：必须输出结构化池状态、节点树状态、碰撞启停状态和事件来源。

## 8. 默认策略表

| 类型 | 根节点 | 是否池化 | 默认策略 |
| --- | --- | --- | --- |
| Enemy | `CharacterBody2D` | 是 | 脱树；两阶段激活；`Activate` 后再允许物理同步。 |
| Player | `CharacterBody2D` | 通常否 | 不进池；常驻 Entity 生命周期。 |
| Projectile | `Area2D` | 是 | 脱树；命中/过期回池前关闭监控；重新出池后再恢复碰撞。 |
| Effect | `Area2D` 或 `Node2D` | 是 | 根是 `Area2D` 则脱树；纯视觉 `Node2D` 不脱树。 |
| AbilityEntity | `Node2D`/容器 | 是 | 不按能力名脱树，只按根节点是否 `CollisionObject2D` 判定。 |
| HealthBarUI / DamageNumberUI | `Control` | 是 | 不脱树；只做显隐、处理模式和 UI 绑定清理。 |
| GameTimer / 纯 C# 对象 | 非 Node | 是 | 不走 Godot Node 生命周期；只清理字段和归属映射。 |

## 9. 完成定义

ObjectPool 重构完成不是“没有幽灵碰撞日志”。

必须同时满足：

- 物理根节点池化对象默认脱树，且策略显式可读。
- 非物理对象不会被错误脱树。
- `Get(false)` / `Activate()` 的两阶段语义被 Entity 文档和代码共同保证。
- Collision 组件有自身退场和事件过滤契约，不依赖对象池独自保证业务正确。
- Pool 观测能显示对象处于池内、挂树未激活、活跃或释放中。
- Godot scene test 能复现并验证旧位置回池再出池不触发幽灵碰撞。
- DocsAI / Skill / SDD 索引同步，AI 下次能从 owner 入口找到这份裁决。

