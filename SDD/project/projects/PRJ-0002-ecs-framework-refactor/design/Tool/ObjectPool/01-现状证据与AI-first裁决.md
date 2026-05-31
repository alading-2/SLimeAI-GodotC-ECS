# 现状证据与 AI-first 裁决

> 更新：2026-05-31
> 状态：current design note

## 1. 问题重新定义

对象池当前被讨论为“幽灵碰撞问题的根源”，但更准确的定义是：

```text
Godot 物理节点复用
  + 多阶段 Entity spawn
  + Area2D 事件队列
  + deferred 属性更新
  + 场景树/Transform 结构风险
  = 需要一个显式生命周期隔离策略
```

对象池只是这个组合问题的入口之一。它不能独自证明碰撞正确，但它必须保证池化对象的退场和重新激活不制造额外物理事件。

## 2. 当前代码证据

`Src/ECS/Tools/ObjectPool/ObjectPool.cs` 当前已经实现了几条关键防线：

- `PoolParkingPosition`：回池时先把物理根节点停到远点。
- `NeedsTreeDetach(Node node) => node is CollisionObject2D`：只有物理根节点默认脱树。
- `SetCollisionTreeActive`：递归关闭 `Area2D.Monitoring/Monitorable` 和 shape disabled。
- `ApplyInactiveState`：关闭处理、隐藏、停放、提交碰撞关闭，然后 `RemoveChild`。
- `ReattachToTree`：重新挂树后立即 `ForceDisableCollisionsDirect`，覆盖 deferred 尚未生效窗口。
- `Get(false)`：允许先取对象但不触发 `OnPoolAcquire`。
- `Activate`：Entity 初始化完成后再恢复处理、显示、碰撞和生命周期回调。

这些不是随意堆补丁，而是对 Godot 物理时序风险的分层防线。

## 3. 外部资料校准

本轮外部资料只用于校准边界，不用于替换当前框架：

| 来源 | 对本设计的约束 |
| --- | --- |
| Godot `Area2D` 官方文档 | `Area2D` 会检测其它 `CollisionObject2D` 的进入/退出，并跟踪尚未退出的重叠对象；`body_entered / area_entered` 等信号要求 `monitoring=true`。这说明它不是普通开关状态，而是物理世界中的持续 overlap 关系。 |
| Godot `Object.set_deferred` 官方文档 | `set_deferred` 在当前帧末尾赋值，等价于通过 `call_deferred` 调用 `set`。所以它适合避开不安全修改点，但不能当作“此刻立即从物理世界消失”的证明。 |
| Godot `CollisionObject2D.disable_mode` 官方文档 | `DISABLE_MODE_REMOVE` 表示 `process_mode=disabled` 时从物理模拟中移除并在恢复处理时重新加入；这提供一层引擎级语义，但不替代对象池在多阶段 spawn 中对旧位置、挂树窗口和业务激活的控制。 |
| Godot issue `#69407` | 同一物理帧内移动对象并重新启用碰撞时，存在 `body_entered` 错发的复现场景；这与历史幽灵碰撞问题一致，说明只靠属性切换和同帧位置更新风险很高。 |
| Microsoft `ObjectPool` 文档 | 对象池适合昂贵初始化、有限资源、可预测高频复用；对象池不总是提升性能，核心抽象是 `Get/Return`、创建策略和 reset policy。这支持把 SlimeAI 的业务生命周期从 ObjectPool 内收口出去。 |

## 4. 当前主要风险

### 4.1 策略隐式

AI 读 `ObjectPool<T>` 时，难以区分：

- 哪些是通用对象池逻辑。
- 哪些是 Godot Node 生命周期。
- 哪些是碰撞隔离策略。
- 哪些是 Entity spawn pipeline 的配合点。

这会导致后续维护时误删泊车位、脱树、同步禁用或两阶段激活。

### 4.2 责任过宽

`ObjectPool<T>` 当前承担了 Node 命名、Meta、ParentPath、显隐、ProcessMode、碰撞递归关闭、脱树、生命周期回调、统计和全局 manager 注册。短期可接受，长期不利于 AI 路由。

### 4.3 缺显式状态

现在池状态主要靠 `InPool` Meta、`_activeItems` 和 `PoolStats`。但 AI 调试碰撞问题时更需要看到：

- 是否已挂树。
- 是否已激活。
- 碰撞是否仍处于禁用窗口。
- 上次 release/acquire 的帧和位置。
- 是否走了 detach 策略。

这些状态目前没有稳定 observation contract。

### 4.4 场景结构错误容易误归因到对象池

历史碰撞文档已经说明，普通 `Node` 插在空间链路中会让 `Area2D` 无法继承移动。这个问题如果不做场景结构门禁，后续仍可能被误判为“对象池复用延迟”。

## 5. AI-first 裁决

### 5.1 对象池继续保留

对象池仍然是合理工具。AI-first ECS 不等于每次都 instantiate/free。高频敌人、投射物、特效、UI 和 timer 仍需要复用来降低运行时分配、销毁和 Godot Node 创建成本。

### 5.2 脱树继续保留

根节点参与物理世界的池化对象继续默认脱树。脱树不是性能优先策略，而是正确性优先策略。对于 `CharacterBody2D`、`Area2D` 这类对象，正确退出物理世界比省一次 `RemoveChild/AddChild` 更重要。

### 5.3 策略必须显式化

下一步不是推翻 `ObjectPool`，而是把隐藏在泛型池里的 Godot 物理策略拆出来，使代码结构变成：

```text
ObjectPool<T>
  -> 管容量、复用、统计、生命周期 hook

PoolNodeLifecycleStrategy
  -> 管 Node 出入池的显隐、处理模式、挂树和释放

CollisionIsolationStrategy
  -> 管物理根节点的泊车、脱树、同步禁用和恢复
```

### 5.4 业务正确性回到 owner

碰撞事件是否有效，应由 Collision / Damage / Entity lifecycle 判断；对象池只提供生命周期隔离事实。

## 6. 结论

当前对象池需要重构，但不是删除或取消脱树。

推荐策略：

1. 保留现有 API 和行为。
2. 先补文档、测试和 observation。
3. 再做内部策略拆分。
4. 最后由 Entity hard cutover 接管 spawn/destroy 编排。
