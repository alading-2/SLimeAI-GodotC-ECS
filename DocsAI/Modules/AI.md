# AI 模块契约

本文是 AI 修改行为树、AIComponent、AI DataKey 或敌人行为组装前必须阅读的执行契约。长设计背景见 `Docs/框架/ECS/System/AI/AI系统说明.md`。

## 职责边界

AI 负责决策和意图表达。AI 节点读取 `Entity.Data`，通过 Data 或事件表达“想做什么”，由 Movement、Attack、Ability、Animation 等系统执行。

AI 不负责：

- 直接移动物理体。
- 直接播放动画。
- 直接扣血或调用伤害管道。
- 直接生成 / 销毁 Entity。
- 直接操作其它 Component 方法。

## 核心入口

- 组件：`Src/ECS/Base/Component/Unit/Enemy/AI/AIComponent.cs`
- 行为树核心：`Src/ECS/AI/Core/`
- 条件节点：`Src/ECS/AI/Conditions/`
- 动作节点：`Src/ECS/AI/Actions/`
- 行为积木：`Src/ECS/AI/Nodes/EnemyBehaviorBlocks.cs`
- 行为树工厂：`Src/ECS/AI/Nodes/EnemyBehaviorTreeBuilder.cs`
- AI DataKey：`Data/DataKey/AI/DataKey_AI.cs`
- 源码入口：`Src/ECS/AI/README.md`

## 数据 / 事件 / 生命周期

- `AIComponent` 在 `OnComponentRegistered` 中缓存 `IEntity/Data`，默认写入 `AIEnabled=true`，并设置默认近战行为树。
- `AIComponent._Process` 每帧复用同一个 `AIContext`，避免每帧分配。
- `AIEnabled=false`、`LifecycleState=Dead`、`StatusCanThink=false` 时不 Tick 行为树。
- `AIContext` 只持有 `Entity`，节点自行通过 `ctx.Entity.Data` 和 `ctx.Entity.Events` 表达意图。
- 移动意图写入 `DataKey.AIMoveDirection` 和 `DataKey.AIMoveSpeedMultiplier`，由 `MoveMode.AIControlled` 消费。
- 目标缓存使用 `DataKey.TargetNode`。它是 `const string` 特殊引用键，不走 `DataRegistry` 类型约束。
- 巡逻运行态使用 `SpawnPosition`、`PatrolTargetPoint`、`PatrolWaitDone` 等 DataKey。

## 节点规则

- Condition 节点应尽量纯查询，返回 `Success/Failure`。
- Action 节点可以写 Data 或发事件，但不应直接执行跨系统业务。
- 长逻辑写独立 `BehaviorNode` 子类，不要塞进委托式 `ActionNode/ConditionNode`。
- 需要打断后清理黑板数据时，实现 `Reset(AIContext? ctx)`。
- 行为树组合优先放在 `EnemyBehaviorBlocks`，完整预制树放在 `EnemyBehaviorTreeBuilder`。

## 与其它系统协作

- 索敌：用 `EntityTargetSelector.Query`，不要手写组扫描或距离过滤。
- 移动：写 AI 移动意图，执行交给 `EntityMovementComponent + AIControlledStrategy`。
- 普通攻击：发 `GameEventType.Attack.Requested`，执行交给 `AttackComponent`。
- 自动施法：通过 Ability 触发入口，不直接执行技能效果。
- 计时等待：用 `TimerManager`，不要在节点里手写 Godot Timer。
- 伤害：AI 不直接构造 `DamageInfo`，具体攻击 / 技能命中再进入 DamageSystem。

## 新增 / 修改 AI 流程

1. 判断是新增条件节点、动作节点、积木块、预制树，还是 AI DataKey。
2. 修改 DataKey 时先读 `DocsAI/Modules/DataAuthoring.md`。
3. 新节点继承 `BehaviorNode` 并实现 `Evaluate(AIContext ctx)`。
4. 只通过 Data 和事件表达意图。
5. 在 `EnemyBehaviorBlocks` 中组合可复用分支。
6. 在 `EnemyBehaviorTreeBuilder` 中暴露完整预制树。
7. 运行构建和相关 AI / Movement / Ability 测试。

## 禁止事项

- 禁止在 AI 节点直接改 `CharacterBody2D.Velocity` 或 `GlobalPosition`。
- 禁止直接操作动画节点。
- 禁止直接调用其它 Component 方法。
- 禁止手写范围搜索替代 `TargetSelector`。
- 禁止在 `_Process` / `Evaluate` 高频路径中引入无必要分配。
- 禁止把伤害、技能效果、动画表现塞进行为树节点。

## 推荐测试

```bash
dotnet build
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs list --filter AI
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run res://Src/ECS/Test/SingleTest/ECS/System/Movement/MovementComponentTestScene.tscn --build
```
