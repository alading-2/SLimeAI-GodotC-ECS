---
name: ai-system
description: 修改 SlimeAI.GameOS AIService、行为树节点、AIDataKeys、目标查询、巡逻、攻击请求或 Godot AI bridge 时使用。
---

# AI Capability 入口

## 必读入口

- `DocsAI/GameOS/Contracts.md`
- `DocsAI/GameOS/ApiIndex.md`
- `DocsAI/ProjectState.md`

## 源码位置

- `GameOS/Capabilities/AI/`
- `GameOS/Capabilities/Movement/`
- `GameOS/Capabilities/Ability/`
- `GameOS/GodotBridge/GodotAIComponent.cs`
- `Tests/SlimeAI.GameOS.Tests/`

## 规则

- AI 节点只写意图、目标和请求事件，不直接移动节点、不直接造成伤害。
- 移动意图写入 `MovementDataKeys.AIMoveDirection / AIMoveSpeedMultiplier`。
- 攻击通过 `Capabilities.Attack.Events.Requested` 和 AttackService 消费。
- AI 目标候选由 `IAITargetQuery` 注入；`RuntimeAITargetQuery` 只是纯 Runtime 全量扫描回退，Godot/game 可注入 physics-aware query。
- 自动施法上下文通过 `AbilityTargetingTool` / BehaviorNode 显式准备，Ability 候选目标归 `IAbilityTargetQuery`。
- `GodotAIComponent` 是 GodotBridge Adapter legacy class name；`AbilityService` 可替换，默认只在 adapter boundary 使用进程级入口。

## 验证

```bash
Tools/run-build.sh
Tools/run-tests.sh
cd /home/slime/Code/SlimeAI/Games/BrotatoLike
Tools/run-build.sh
Tools/run-godot-scene.sh run res://SlimeAI/Src/Validation/GameOS/Capabilities/AI/AICapabilityValidation.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/run-godot-scene.sh run-main-smoke --log-dir .ai-temp/scene-tests/runs
```
