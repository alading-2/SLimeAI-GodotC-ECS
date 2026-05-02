# Component 模块契约

本文是 AI 开发 Component 时的执行契约。人类设计背景见 `Docs/框架/ECS/Component/Component数据驱动设计理念.md`。

## 职责边界

Component 是 Entity 上的功能单元，用于实现局部能力、状态响应和事件桥接。

Component 可以包含逻辑，但不应该承担大型流程编排、跨实体生命周期管理或系统级调度。

## 核心入口

- 模板：`Src/ECS/Base/Component/TemplateComponent.cs`
- 接口：`Src/ECS/Base/Component/IComponent.cs`
- 规范：`Src/ECS/Base/Component/Component规范.md`
- 常见目录：`Src/ECS/Base/Component/`

## 必须遵守

- 单一职责：一个 Component 只做一件事。
- 运行时共享状态进 `Entity.Data`，禁止私有业务状态字段。
- 组件间通信优先 `Entity.Events`，不要直接调用其他 Component 方法。
- 事件订阅放在 `OnComponentRegistered`，不要放在 `_Ready`。
- `_Process` 中禁止 `new` 对象和 LINQ。
- 资源加载走 `ResourceManagement`，计时走 `TimerManager`。

允许私有字段：

- 节点引用，如 `_sprite`
- 缓存引用，如 `_entity`、`_data`
- 仅服务当前组件内部公式推进的运行态，如角速度、局部计时状态

需要进入 Data：

- 其他组件或系统要读取的状态
- 对外发布的计算结果
- 需要对象池复用时统一清理的业务状态

## 标准流程

1. 先查是否已有类似 Component。
2. 从 `TemplateComponent.cs` 或相近组件复制结构。
3. 在 `OnComponentRegistered(Node entity)` 中保存 `IEntity` 和 `Data`。
4. 订阅 `Entity.Events` 响应数据和事件变化。
5. 在 `OnComponentUnregistered()` 中清理引用和本组件持有的外部资源。
6. 修改完成后运行构建和相关场景测试。

## 模块转向

Movement、AI 和 Collision 已拆成独立契约。本文件只保留 Component 通用规则：

- 修改运动策略、朝向、运动碰撞：先读 `DocsAI/Modules/Movement.md`。
- 修改行为树、AIComponent、AI DataKey：先读 `DocsAI/Modules/AI.md`。
- 修改碰撞层、Hurtbox、接触伤害、对象池碰撞隔离：先读 `DocsAI/Modules/Collision.md`。

## 碰撞组件摘要

- `CollisionComponent` 只桥接 Entity 根节点为 `Area2D` 的视觉体碰撞。
- `HurtboxComponent` 本身就是 `Area2D` 受击区组件。
- 接触伤害消费 `HurtboxEntered / HurtboxExited`。
- 运动碰撞由 `EntityMovementComponent`、`MovementCollisionPolicy` 和停止协调器处理。
- `PickupComponent.cs` 当前是禁用物理监控的占位组件，仅用于保证旧场景引用可实例化；新增拾取功能前先恢复/重写实现并补测试。

完整碰撞规则见 `DocsAI/Modules/Collision.md`。

## EntityMovementComponent 摘要

- `EntityMovementComponent` 是运动策略调度器。
- 策略实现 `IMovementStrategy`，通过注册表按 `MoveMode` 切换。
- 策略禁止直接操作 `GlobalPosition`。
- `Velocity` 表示“怎么移动”，`MovementFacingDirection` 表示“朝哪看”。
- 最终朝向交给 `EntityOrientationComponent`。

完整 Movement 规则见 `DocsAI/Modules/Movement.md`。

## 推荐测试

- `dotnet build`
- `node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs list --filter Movement`
- `node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run res://Src/ECS/Test/SingleTest/ECS/System/Movement/MovementComponentTestScene.tscn --build`
- `node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run res://Src/ECS/Test/SingleTest/ECS/System/Movement/MovementCollisionRuntimeTest.tscn --build`

## 人工审查重点

- 是否把业务状态藏进私有字段。
- 是否直接调用其他 Component 方法。
- 是否绕过 EventBus、TimerManager、ResourceManagement。
- 是否在 `_Process` 中产生 GC。
- 是否破坏对象池复用后的状态清理。
