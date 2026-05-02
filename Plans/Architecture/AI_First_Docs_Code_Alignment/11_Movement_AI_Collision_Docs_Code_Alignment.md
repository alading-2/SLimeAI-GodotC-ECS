# 11 Movement / AI / Collision Docs-Code Alignment

## 目标

深层对齐 Movement、AI、Collision 三个模块族，让 AI 执行入口、源码旁 README、DocsAI 契约和项目索引按当前代码一致。

## 范围

- 新增 `DocsAI/Modules/Movement.md`。
- 新增 `DocsAI/Modules/AI.md`。
- 新增 `DocsAI/Modules/Collision.md`。
- 新增短 Skill：
  - `.codex/skills/movement-system/SKILL.md`
  - `.codex/skills/ai-system/SKILL.md`
  - `.codex/skills/collision-system/SKILL.md`
- 压缩源码旁入口：
  - `Src/ECS/Base/System/Movement/README.md`
  - `Src/ECS/Base/System/Movement/ScalarDriver/README.md`
  - `Src/ECS/Base/System/Movement/Strategies/README.md`
  - `Src/ECS/Base/System/Movement/Strategies/数学与物理概念详解.md`
  - `Src/ECS/AI/README.md`
- 更新 `DocsAI/INDEX.md`、Skill 映射、Component / DamageSystem 摘要、项目索引和计划状态。

## 代码事实

- Movement 当前 `MoveMode` 为 `None / Charge / Orbit / SineWave / BezierCurve / Boomerang / AttachToHost / PlayerInput / AIControlled / Parabola / CircularArc`。
- `EntityMovementComponent` 是策略调度器；策略只写 `DataKey.Velocity`，位移由调度器执行。
- `EntityOrientationComponent` 是最终朝向输出层，Movement 只发布 `MovementFacingDirection`。
- `MovementCollision` 表示有效碰撞通知，不等价于运动结束。
- `AIComponent` 每帧复用 `AIContext`，AI 通过 Data 和事件表达意图。
- AI 移动意图由 `AIControlledStrategy` 消费。
- `CollisionComponent` 只桥接根节点 `Area2D` 视觉体碰撞。
- `HurtboxComponent` 是受击区传感器。
- `ContactDamageComponent` 消费 Hurtbox 事件，并通过 DamageSystem 请求伤害。

## 验收

- `DocsAI/Modules/Movement.md`、`AI.md`、`Collision.md` 存在并指向当前源码入口。
- 三个新 Skill 是短入口，不复制长设计。
- 源码旁 Movement / AI README 不再携带旧 MoveMode 或长篇行为树教程。
- `DocsAI/Modules/Component.md` 和 `DamageSystem.md` 只保留摘要并指向专门契约。
- 项目索引包含 Movement / AI / Collision 的 DocsAI 入口。
- 构建和一致性扫描结果记录到 `Done.md`。

## 不做

- 不改运行时代码。
- 不修旧 `MainTest` 历史失败。
- 不为历史文档做大规模删除。
- 不新增依赖。
