---
name: ai-system
description: 修改 AI 系统时使用。适用于：AIComponent、行为树 BehaviorNode、AIContext、EnemyBehaviorBlocks、EnemyBehaviorTreeBuilder、AI Conditions/Actions、AI DataKey、索敌、追逐、巡逻、自动攻击、自动施法。
---

# AISystem 入口

## 什么时候用

- 新增或修改 AI 条件节点 / 动作节点。
- 修改 `AIComponent` 或行为树 Tick 生命周期。
- 组装敌人行为树和行为积木。
- 新增 AI DataKey、黑板数据或状态。
- 调整索敌、追逐、巡逻、逃跑、自动攻击 / 施法。

## 转向其它 Skill

- 移动策略和物理执行 -> `@movement-system`
- 攻击和造成伤害 -> `@damage-system`
- 技能触发和施法流水线 -> `@ability-system`
- DataKey / DataNew 配置 -> `@data-authoring`
- TargetSelector / TimerManager -> `@tools`

## 必读

- `DocsAI/Modules/AI.md`
- 涉及移动意图读 `DocsAI/Modules/Movement.md`
- 涉及技能读 `DocsAI/Modules/AbilitySystem.md`
- 涉及目标查询读 `DocsAI/Modules/Tools.md`

## 最短流程

- SlimeAI 迁移目标已存在 AI Runtime 最小行为树：`/home/slime/Code/SlimeAI/SlimeAI/GameOS/Capabilities/AI`。
- 当前覆盖 `AIDataKeys / AIContext / AIService / BehaviorNode / SequenceNode / SelectorNode / FindNearestTargetAction / MoveToTargetAction / IsTargetInRangeCondition / RequestAttackAction / PrepareAbilityAutoTargetContextsAction / TickAbilityAutoTriggersAction / PatrolAction / EnemyBehaviorBlocks / EnemyBehaviorTreeBuilder / GodotAIComponent / GodotAIBehaviorTreeKind / GameEventType.Attack`；攻击请求可由 `AttackService` 消费并通过 DamageService 结算，技能自动施法可先用 `PrepareAbilityAutoTargetContextsAction` 显式准备 `AbilityCastContext` 再交给 `TickAbilityAutoTriggersAction`，Godot 层已有 `GodotAttackComponent` bridge 第一段负责服务注册和节点目标映射，已有旧 `AttackComponent` 类名兼容包装，并已有 `GameEventType.Unit / GodotUnitAnimationComponent` 承接 Attack 动画请求。

1. 判断是条件、动作、积木块、预制树还是 DataKey。
2. 节点继承 `BehaviorNode`，实现 `Evaluate(AIContext ctx)`。
3. Condition 尽量纯查询；Action 只写 Data 或发事件。
4. 移动写 `AIMoveDirection / AIMoveSpeedMultiplier`。
5. 攻击通过 `RequestAttackAction` 发 `GameEventType.Attack.Requested`；技能走 Ability 触发入口。
6. 在 `EnemyBehaviorBlocks` / `EnemyBehaviorTreeBuilder` 组合。
7. 运行构建和相关 Movement / Ability 测试。

## 禁止事项

- 不要直接改 `CharacterBody2D.Velocity` 或 `GlobalPosition`。
- 不要直接操作动画节点。
- 不要直接调用其它 Component 方法。
- 不要手写范围搜索替代 `TargetSelector`。
- 不要在 AI 节点里直接造成伤害。

## 推荐验证

```bash
dotnet build
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs list --filter AI
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run res://Src/ECS/Test/SingleTest/ECS/System/Movement/MovementComponentTestScene.tscn --build
```
