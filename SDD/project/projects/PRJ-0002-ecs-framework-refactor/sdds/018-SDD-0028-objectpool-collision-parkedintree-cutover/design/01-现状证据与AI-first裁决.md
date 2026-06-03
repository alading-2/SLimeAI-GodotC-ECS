# 现状证据与 AI-first 裁决

> 更新：2026-06-03
> 状态：current design note

## 1. 问题重新定义

对象池当前被讨论为“幽灵碰撞问题的根源”，但更准确的定义是：

```text
Godot 物理节点复用
  + 多阶段 Entity spawn
  + Area2D 事件队列
  + deferred 属性更新
  + 场景树/Transform 结构风险
  = 需要一个显式 pool runtime state 和碰撞逻辑验证策略
```

对象池只是这个组合问题的入口之一。它不能独自证明碰撞正确；它应提供“是否在池中、是否允许业务碰撞、何时允许恢复碰撞逻辑、停放在哪里”的运行时事实。真正的业务正确性由 Collision / Movement / Damage / Projectile / ContactDamage 等入口在处理前验证。

## 2. 当前代码证据

`Src/ECS/Tools/ObjectPool/ObjectPool.cs` 当前仍是旧策略实现，包含这些证据：

- `PoolParkingPosition`：回池时先把物理根节点停到远点。
- `NeedsTreeDetach(Node node) => node is CollisionObject2D`：只有物理根节点默认脱树。
- `SetCollisionTreeActive`：递归关闭 `Area2D.Monitoring/Monitorable` 和 shape disabled。
- `ApplyInactiveState`：关闭处理、隐藏、停放、提交碰撞关闭，然后 `RemoveChild`。
- `ReattachToTree`：重新挂树后立即 `ForceDisableCollisionsDirect`，覆盖 deferred 尚未生效窗口。
- `Get(false)`：允许先取对象但不触发 `OnPoolAcquire`。
- `Activate`：Entity 初始化完成后再恢复处理、显示、碰撞和生命周期回调。

这些不是随意堆补丁，而是旧阶段对 Godot 物理时序风险的分层防线。但 2026-06-03 新裁决已经调整：默认不再通过脱树 / 关碰撞提供正确性，而是让对象场外常驻，并在碰撞业务入口做 runtime state guard。

## 3. 外部资料校准

本轮外部资料只用于校准边界，不用于替换当前框架：

| 来源 | 对本设计的约束 |
| --- | --- |
| Context7 Godot 4.6 `Area2D` 文档 | overlap 列表每个 physics step 更新一次，不会在移动后立刻同步；这说明“移动到远点”不能作为本帧物理查询立即干净的唯一证明。 |
| Context7 Godot 4.6 `CollisionShape2D` / `CollisionPolygon2D` 文档 | `disabled` 控制碰撞形状是否参与检测，并应通过 `Object.set_deferred()` 修改；新策略默认不切 shape，从源头减少 deferred / pair 重建窗口。 |
| Godot `Object.set_deferred` 官方文档 | `set_deferred` 在当前帧末尾赋值，等价于通过 `call_deferred` 调用 `set`。所以它适合避开不安全修改点，但不能当作“此刻立即从物理世界消失”的证明。 |
| Godot `CollisionObject2D.disable_mode` 官方文档 | `DISABLE_MODE_REMOVE` 可作为 fallback / 对照，但不进入默认策略；新默认不主动把对象移出 physics space。 |
| Godot issue `#69407` / `#79464` / `#74988` | 同帧移动或恢复 collision mask、`monitorable` toggle、`ProcessMode.Disabled` 与 Area2D overlap 都存在时序风险；新策略通过“不默认切碰撞属性 + 激活首帧 guard”降低风险。 |
| Microsoft `ObjectPool`、Unity `ObjectPool<T>`、Game Programming Patterns `Object Pool` | 通用对象池关注复用、容量、创建/回收策略和 reset policy；重用对象不会自动清理业务状态。这支持把 SlimeAI 的业务生命周期从 ObjectPool 内收口出去。 |
| 本地 Resources Godot 4.6.2 分析 | Capability 不应直接持有或操作裸 `PhysicsServer2D` RID，物理行为应通过 Godot Node / bridge 代理，避免生命周期和 Data/Event 观察面断裂。 |
| 本地 Arch / ET 分析 | 对象复用会带来旧引用误命中风险，可后续观察 generation / destroyed flag；当前不因此替换 EntityId 或重写 Entity runtime。 |

## 4. 当前主要风险

### 4.1 策略隐式

AI 读 `ObjectPool<T>` 时，难以区分：

- 哪些是通用对象池逻辑。
- 哪些是 Godot Node 生命周期。
- 哪些是碰撞隔离策略。
- 哪些是 Entity spawn pipeline 的配合点。

这会导致后续维护时误删 parking grid、runtime state、激活首帧 guard 或业务碰撞入口过滤。

### 4.2 责任过宽

`ObjectPool<T>` 当前承担了 Node 命名、Meta、ParentPath、显隐、ProcessMode、碰撞递归关闭、脱树、生命周期回调、统计和全局 manager 注册。短期可接受，长期不利于 AI 路由。

### 4.3 缺显式状态

现在池状态主要靠 `InPool` Meta、`_activeItems` 和 `PoolStats`。但 AI 调试碰撞问题时更需要看到：

- 是否已挂树。
- 是否已激活。
- 碰撞逻辑是否允许进入业务。
- 上次 release/acquire 的帧和位置。
- `CollisionReadyPhysicsFrame`。
- parking grid 位置。
- 是否走了 fallback detach 策略。

这些状态目前没有稳定 observation contract。

### 4.4 场景结构错误容易误归因到对象池

历史碰撞文档已经说明，普通 `Node` 插在空间链路中会让 `Area2D` 无法继承移动。这个问题如果不做场景结构门禁，后续仍可能被误判为“对象池复用延迟”。

## 5. AI-first 裁决

### 5.1 对象池继续保留

对象池仍然是合理工具。AI-first ECS 不等于每次都 instantiate/free。高频敌人、投射物、特效、UI 和 timer 仍需要复用来降低运行时分配、销毁和 Godot Node 创建成本。

### 5.2 默认不再脱树

根节点参与物理世界的池化对象不再默认脱树。默认策略是 `ParkedInTree`：对象回池后仍在场景树和物理世界中，但被隐藏、停处理、移动到场外，并且 `CollisionLogicActive=false`。

`CollisionObject2D.disable_mode = REMOVE` 和 `Detach` 只保留为 fallback / 对照验证项。它们不改变当前裁决：默认不 `RemoveChild`，不 `SetCollisionTreeActive(false)`，不改 layer/mask/shape。

### 5.3 策略必须显式化

下一步不是推翻 `ObjectPool`，而是把隐藏在泛型池里的 Godot 物理策略拆出来，使代码结构变成：

```text
ObjectPool<T>
  -> 管容量、复用、统计、生命周期 hook

PoolNodeLifecycleStrategy
  -> 管 Node 出入池的显隐、处理模式、挂树和释放

PoolRuntimeCollisionStrategy
  -> 管 parking grid、runtime state、激活首帧 embargo 和 fallback detach
```

### 5.4 业务正确性回到 owner

碰撞事件是否有效，应由 Collision / Damage / Entity lifecycle 判断；对象池只提供生命周期隔离事实。

## 6. 结论

当前对象池需要重构，但不是删除对象池，也不是继续默认脱树。

推荐策略：

1. 保留现有 public API。
2. 先补文档、测试和 observation。
3. 再把默认行为迁移为 `ParkedInTree`。
4. 最后由 Entity hard cutover 接管 spawn/destroy 编排。

2026-06-02 复核后，优先级调整为：

1. P0：收敛 DocsAI / SDD 文档，明确 ObjectPool / Entity / Collision owner 边界。
2. P1：新增 `ObjectPoolRuntimeStateStore`、parking grid、激活首帧 guard。
3. P2：把 CollisionComponent / Hurtbox / MovementCollision / ContactDamage 接入 guard。
4. P3：补 Godot scene test，对照验证 `ParkedInTree`、fallback detach、parking grid 性能和错误场景结构。
