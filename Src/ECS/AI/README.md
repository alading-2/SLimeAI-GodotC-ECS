# AI 源码入口

本目录存放行为树核心、AI 条件节点、动作节点和敌人行为组装。AI 执行契约见 `DocsAI/Modules/AI.md`，长设计背景见 `Docs/框架/ECS/System/AI/AI系统说明.md`。

## 目录职责

- `Core/`：`BehaviorNode`、组合节点、装饰节点、`AIContext`、`BehaviorTreeRunner`。
- `Conditions/`：纯查询条件节点。
- `Actions/`：写 Data 或发事件的动作节点。
- `Nodes/`：敌人行为积木和预制行为树工厂。

## 关键入口

- 组件：`Src/ECS/Base/Component/Unit/Enemy/AI/AIComponent.cs`
- DataKey：`Data/DataKey/AI/DataKey_AI.cs`
- 积木块：`Nodes/EnemyBehaviorBlocks.cs`
- 预制树：`Nodes/EnemyBehaviorTreeBuilder.cs`

## 当前协作规则

- AI 只做决策和意图表达。
- 移动意图写 `DataKey.AIMoveDirection` / `DataKey.AIMoveSpeedMultiplier`。
- 索敌使用 `EntityTargetSelector.Query`。
- 普通攻击发 `GameEventType.Attack.Requested`。
- 自动施法走 Ability 触发入口。
- 实际移动由 `EntityMovementComponent + MoveMode.AIControlled` 执行。

## 新增节点流程

1. 判断是 `Condition` 还是 `Action`。
2. 新类继承 `BehaviorNode`。
3. 实现 `Evaluate(AIContext ctx)`。
4. 需要打断清理时实现 `Reset(AIContext? ctx)`。
5. 在 `EnemyBehaviorBlocks` 中组合可复用分支。
6. 在 `EnemyBehaviorTreeBuilder` 中暴露完整预制树。
7. 更新 `DocsAI/Modules/AI.md` 或项目索引。

## 禁止事项

- 不要直接改 `CharacterBody2D.Velocity` 或 `GlobalPosition`。
- 不要直接操作动画节点。
- 不要直接调用其它 Component 方法。
- 不要在 AI 节点里直接造成伤害。
- 不要手写范围搜索替代 `TargetSelector`。

## 测试

```bash
dotnet build
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs list --filter AI
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run res://Src/ECS/Test/SingleTest/ECS/System/Movement/MovementComponentTestScene.tscn --build
```
