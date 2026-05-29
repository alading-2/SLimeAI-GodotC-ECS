# 移动碰撞语义重构与 ArcShot 修正方案

## 执行状态（2026-04）

当前方案已按本文落地到代码，核心实现如下：

- `MovementParams` 已移除 `DestroyOnCollision`，改为 `MovementCollisionParams? Collision`
- 已新增 `MovementCollisionPolicy / MovementStopCoordinator / MovementCollisionContext`
- `EntityMovementComponent` 已改为“原始碰撞候选 -> 过滤/计数 -> 通知 -> 条件停止”
- 已新增 `GameEventType.Unit.MovementStopRequested`
- `MovementCompletedEventData` 已扩展 `Reason / CollisionTarget`
- `ArcShot` 已改为 `OnStop(Completed)` 驱动命中，不再依赖 `MovementCollision`
- `SineWaveShot / BezierShot / ParabolaShot / BoomerangThrow / OrbitSkill / AuraShield` 已迁到新碰撞配置
- 已新增运行时测试：`Src/ECS/Test/SingleTest/ECS/System/Movement/MovementCollisionRuntimeTest.tscn`

## Summary

  当前问题不是 ArcShot 单点写错，而是移动层把 3 件事耦合成了一条硬编码链路：

  - 原始碰撞进入
  - 业务命中通知
  - 运动完成/实体销毁

  这导致 Src/ECS/Base/Component/Movement/EntityMovementComponent.cs 中 HandleMovementCollision() 一旦收到碰撞就直接走完成链路，和 Src/ECS/Base/System/Movement/Config/MovementParams.cs 里“碰撞仅
  通知、销毁可选”的文档语义已经不一致。
  本次重构采用“组件编排 + 子模块”的方向，不做全局集中系统；碰撞桥接仍留在碰撞系统，移动组件只负责消费“候选碰撞”，再通过声明式碰撞策略决定是否通知、计数、停止、销毁。

  ## Key Changes

  ### 1. 重做移动碰撞语义分层

  在移动层拆成 4 个明确阶段，禁止再由单个方法硬编码串死：

  1. RawCollision
     由 CollisionComponent 或 CharacterBody2D 滑动碰撞产生原始候选。
  2. CollisionFilter
     由移动碰撞策略模块判定是否接受本次碰撞。
  3. CollisionNotify
     仅对“通过过滤”的碰撞发出 OnCollision 回调和 MovementCollision 事件。
  4. CollisionStop
     只有命中“自动停止阈值”时，才触发停止流程；停止后是否销毁再单独判定。

  这样 MovementCollision 不再等同于“运动结束”，只是“本次碰撞被移动策略接受”的通知事件。

  这里要先明确一个引擎事实，避免后续设计跑偏：

  - `CollisionComponent` 当前绑定的 4 个事件，只对 `Area2D` 根节点生效
  - `CharacterBody2D` 不是 `Area2D`，不能直接复用 `body_entered / body_exited / area_entered / area_exited` 这套事件桥接
  - `CharacterBody2D` 的标准碰撞读取方式，本来就是 `MoveAndSlide()` 后读取 `GetSlideCollisionCount() / GetSlideCollision()`

  所以本次重构不能建立在“让 `CharacterBody2D` 直接订阅 `Area2D` 的 4 个事件”这个错误前提上。

  正确方向只有两类：

  - 保留 `CharacterBody2D` 的滑动碰撞采样，把采样结果统一包装成 `RawCollision`
  - 如果某些玩法真的需要事件式进入/离开语义，就在 `CharacterBody2D` 外挂独立 `Area2D` 传感器节点，由传感器负责事件，主体仍负责移动

  无论选哪条，核心都一样：

  - 物理采样层负责“我碰到了谁”
  - 移动碰撞策略层负责“这次碰撞算不算有效”
  - 生命周期层负责“要不要停、要不要销毁”

  禁止再把这三层写死在一个 `HandleMovementCollision()` 里。

  ### 2. 为 MovementParams 增加声明式碰撞策略对象

  不要继续往 MovementParams 平铺更多 bool。新增一个嵌套配置对象，例如 MovementCollisionParams，由 MovementParams 持有。该对象至少包含：

  - TeamFilter
    默认 All，用于筛选友军/敌军/中立/自身。
  - EntityTypeFilter
    默认 None=不过滤。
  - TargetMatchMode
    明确支持 Any / TrackedTargetOnly / SpecificNode。
    ArcShot 用 TrackedTargetOnly 或直接关闭碰撞停止。
  - StopAfterCollisionCount
    默认 -1，表示永不因碰撞自动停止；1 表示首个有效碰撞停止；2 表示穿透 2 个后停止。
  - DestroyOnStop
    默认 false，仅在“因碰撞停止”已发生时决定是否销毁。
  - EmitCollisionEvent
    默认 true，允许完全关闭 MovementCollision。
  - OnCollision
    新增回调，签名建议为 Action<MovementCollisionContext>?。

  旧字段 DestroyOnCollision 废弃并迁移到 Collision.StopAfterCollisionCount + Collision.DestroyOnStop 组合表达。

  ### 3. 新增碰撞上下文与事件数据

  新增 MovementCollisionContext，供 OnCollision 回调和内部停止决策共用，至少包含：

  - Mode
  - TargetNode
  - TargetEntity
  - CollisionCount
  - WillStop
  - Params

  并扩展 MovementCollisionEventData，与上下文保持一致，至少新增：

  - IEntity? TargetEntity
  - int CollisionCount
  - bool WillStop

  这样技能侧不需要每次自己再 ResolveOwningIEntity，而且能知道“这是第几次有效碰撞”“这次是否会触发停止”。

  ### 4. 将停止接口事件化，而不是暴露组件直接调用

  根据项目红线，不能通过外部直接调组件方法来停移动。
  新增 GameEventType.Unit.MovementStopRequested，并新增对应 MovementStopRequestedEventData，由 Src/ECS/Base/Component/Movement/EntityMovementComponent.cs 监听处理。字段固定为：

  - Reason
    已新增 `Requested` 停止原因，避免滥用 `Interrupted`。
  - EmitCompletedEvent
    默认 true，这是你要求的行为。
  - NextMode
    默认按现有逻辑回退 DefaultMoveMode。
  - CollisionTarget
    可空，仅供碰撞来源停止透传。
  - DestroyEntity
    默认 false，仅在明确需要时覆盖默认生命周期。

  完成事件是否发出，不再绑定“停止原因”，而是由 EmitCompletedEvent 明确控制。

  ### 5. EntityMovementComponent 保留编排，但把碰撞决策抽成子模块

  不改成全局系统。原因已经明确：

  - 当前碰撞来源本来就是局部物理事件，不需要全局逐帧扫描。
  - 挪成集中系统会增加状态同步和事件回流，复杂度高于收益。
  - 穿透计数、目标过滤、本次运动上下文，本质都属于“当前实体本次移动”的局部状态。

  落地结构：

  - EntityMovementComponent
    仍负责订阅事件、持有当前策略/当前参数、统一停止流程，并把 `Area2D` 事件或 `CharacterBody2D` 滑动碰撞统一转换成 `RawCollision` 输入。
  - 新的 MovementCollisionPolicy
    纯逻辑对象，负责过滤、计数、是否停止、生成 MovementCollisionContext。
  - 新的 MovementStopCoordinator
    统一处理“完成事件是否发”“是否销毁”“是否回退默认模式”。

  组件保留状态持有，但不再自己堆满碰撞决策分支。

  需要特别禁止的错误方向：

  - 不要试图给 `CharacterBody2D` 主体直接补订阅 `body_entered`
  - 不要继续把 `_hasCollided` 当成最终业务语义
  - 不要把“采样到碰撞”直接等同于“业务命中并停止”

  ### 6. ArcShot 改为“策略完成驱动命中”，不再订阅 MovementCollision

  DataOS removed legacy Data/Ability/Ability/Movement/ArcShot/ArcShot.cs 的改法固定如下：

  - 不再订阅 MovementCollision 作为命中入口。
  - 使用 CircularArc + isTrackTarget + ReachDistance 作为唯一完成条件。
  - 通过 MovementParams.OnStop 处理命中逻辑。
  - OnStop 内只在 ctx.Reason == Completed 且目标仍有效时结算伤害。
  - 默认不配置碰撞自动停止；其他敌人/玩家的碰撞不应终止 ArcShot。
  - 若仍需要保留碰撞通知，仅允许配置为 notify-only，不参与停止。

  ### 7. 其他移动技能迁移规则

  典型迁移标准固定如下：

  - 直线子弹类
    Collision.TeamFilter=Enemy，StopAfterCollisionCount=1，DestroyOnStop=true。
  - 可穿透子弹
    Collision.TeamFilter=Enemy，StopAfterCollisionCount=N，DestroyOnStop=true。
  - 持续接触/环绕类
    Collision.TeamFilter=Enemy，StopAfterCollisionCount=-1，只使用 OnCollision 或 MovementCollision。
  - Dash 这类“到终点触发落地效果”
    继续使用 OnStop，不依赖 MovementCollision。

  ### 8. CharacterBody2D 的重复碰撞去重规则

  先把当前旧实现里 [EntityMovementComponent.cs](../../../../Src/ECS/Base/Component/Movement/EntityMovementComponent.cs) `ApplyMovement()` 那段 `CharacterBody2D` 碰撞判断说清楚：

  - `Area2D` 路径已经有 `CollisionComponent -> CollisionEntered` 这条“进入事件”桥接
  - `CharacterBody2D` 路径没有同等桥接，只能在 `MoveAndSlide()` 之后读取“本帧撞到了谁”
  - `MoveAndSlide()` 提供的是“这一帧发生碰撞”，不是 `body_entered` 那种“首次进入一次”
  - 所以旧实现才会用 `_hasCollided` 人工模拟一个“一次运动只响应一次”的最小版进入语义

  这里要写死一个认知结论：

  - 这不是“我们偷懒没接信号”
  - 而是 `CharacterBody2D` 本来就没有 `Area2D` 那套 4 个进入/离开信号
  - 当前奇怪的地方，不在“手动读取滑动碰撞”，而在“读取后直接绑定业务停止语义”

  这段代码在当前版本里不是多余代码，因为删掉以后：

  - `CharacterBody2D` 临时运动将失去运动碰撞入口
  - 依赖 `MovementCollision` 的物理体技能会失效
  - `Area2D` 与 `CharacterBody2D` 的碰撞语义会再次分裂

  但它也正是当前问题来源之一：

  - `_hasCollided` 是“一次运动全局只碰一次”的粗粒度锁
  - 它只能解决“持续顶墙时不要每帧刷事件”
  - 它无法表达“碰到第 2 个敌人才停”“只对敌方计数”“只对锁定目标停止”
  - 因此它对 `ArcShot` 这种“追特定目标自然完成”的语义是错误的

  所以本次重构不是删除 `CharacterBody2D` 的局部碰撞入口，而是替换掉旧的 `_hasCollided -> HandleMovementCollision()` 硬编码链路。

  新规则固定为：

  - `Area2D` 路径
    以进入事件为准，每次有效进入都可参与计数。
  - `CharacterBody2D` 路径
    继续允许使用 `MoveAndSlide()` 后的滑动碰撞作为采样入口，但不再用单个 `_hasCollided` 全局锁死。
    改为记录“本次运动已接受过的碰撞对象集合”，键优先用 `TargetEntity`，否则用 `Node2D` 实例。
  - `CharacterBody2D + 传感器` 路径（可选）
    如果确实需要进入/离开事件语义，允许外挂 `Area2D` 传感器节点，但传感器只负责感知，不直接决定停止与销毁。
  - 同一对象在同一次运动内只计数一次，防止推墙/持续贴脸每帧刷计数。
  - 若后续确实需要“离开后重新计数”，再单独做 v2，不在本次设计里扩展。

  ## Public API / Interface Changes

  - Src/ECS/Base/System/Movement/Config/MovementParams.cs
    新增 Collision 配置对象与 OnCollision 回调；废弃 DestroyOnCollision。
  - Data/EventType/Unit/GameEventType_Unit_Movement.cs
    扩展 MovementCollisionEventData；新增 MovementStopRequested 事件。
  - MovementStopReason
    已新增 `Requested`。
  - MovementStopContext
    已补充 `IsRequested` 便捷判定。
  - ProjectileEntity / 技能执行器示例文档
    全部改成新碰撞配置写法，禁止再写“监听 MovementCollision + DestroyOnCollision=true = 命中即销毁”这种绑定语义。

  ## Test Plan

  必须补以下场景，至少覆盖单测或运行时测试场景：

  - ArcShot 穿过非锁定敌人时不停，接近锁定目标后通过 OnStop(Completed) 命中。
  - ArcShot 目标死亡或失效时，不因随机碰撞误伤其他单位；停止语义符合设计。
  - 子弹配置 StopAfterCollisionCount=2 时，前 2 次有效敌方碰撞都发 MovementCollision，第 2 次后才停止并销毁。
  - 子弹碰到友军/自身/中立时，按 TeamFilter 正确过滤，不计数、不停止。
  - StopAfterCollisionCount=-1 时，碰撞只通知不停止，MovementCompleted 不应被碰撞触发。
  - 外部发送 MovementStopRequested(EmitCompletedEvent=true) 时，正常触发 MovementCompleted。
  - 外部发送 MovementStopRequested(EmitCompletedEvent=false) 时，只停当前运动，不发 MovementCompleted。
  - CharacterBody2D 同一堵墙持续碰撞不会无限累计计数。
  - 默认移动模式 AIControlled / PlayerInput 仍不消费移动碰撞策略，不产生噪声事件。

  ## 当前测试落地

  已有运行时测试覆盖：

  - `MovementCollisionParams` 默认值
  - `MovementStopRequestedEventData` 默认值
  - `MovementCompletedEventData.Reason`
  - `MovementStopCoordinator.Resolve(...)`
  - `MovementCollisionPolicy` 的阵营过滤、重复目标去重、第二次碰撞停止
  - `TrackedTargetOnly`

  仍建议继续补的高层行为测试：

  - ArcShot 穿过非锁定敌人时不停，接近锁定目标后通过 OnStop(Completed) 命中。
  - ArcShot 目标死亡或失效时，不因随机碰撞误伤其他单位；停止语义符合设计。
  - 外部发送 `MovementStopRequested(EmitCompletedEvent=false)` 时，只停当前运动，不发 `MovementCompleted`。

  ## Assumptions

  - 本次不把移动系统改造成全局逐帧集中系统，保持局部事件驱动。
  - 本次不引入“任意自定义碰撞谓词 delegate”，声明式过滤就是上限；复杂业务走 OnCollision。
  - ArcShot 的正确命中语义定义为“到达追踪目标后结算”，不是“物理碰到任意单位就结算”。
  - 实现时需要同步更新移动系统相关文档、项目索引以及所有引用旧 DestroyOnCollision 语义的说明，避免 Skill/文档继续失真。
