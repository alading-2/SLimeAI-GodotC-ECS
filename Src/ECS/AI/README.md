# AI 源码入口

本目录存放行为树核心、AI 条件节点、动作节点和敌人行为组装。AI 执行契约见 `DocsAI/Modules/AI.md`，设计说明见 `Docs/框架/ECS/System/AI/AI系统说明.md`。

## 目录职责

- `Core/`：`BehaviorNode`、组合节点、装饰节点、`AIContext`、`BehaviorTreeRunner`
- `Conditions/`：纯查询条件节点
- `Actions/`：写 Data 或发事件的动作节点
- `Nodes/`：敌人行为积木 (`EnemyBehaviorBlocks`) 和预制行为树工厂 (`EnemyBehaviorTreeBuilder`)

## 关键入口

- 组件：`Src/ECS/Base/Component/Unit/Enemy/AI/AIComponent.cs`
- DataKey：`Data/DataKey/AI/DataKey_AI.cs`

## 核心原则

AI 只做决策和意图表达，通过 Data 和事件输出“想做什么”，由 Movement、Attack、Ability、Animation 等系统执行。完整规则、协作协议、禁止事项和新增节点流程见 `DocsAI/Modules/AI.md`。

## 测试

```bash
dotnet build
./.claude/skills/GodotSkill/scripts/run-test.sh res://Src/ECS/Test/SingleTest/ECS/System/Movement/MovementComponentTestScene.tscn --build
```
