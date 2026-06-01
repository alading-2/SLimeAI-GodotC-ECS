---
name: ai-system
description: 修改 SlimeAI ECS AI Capability、行为树节点、AIDataKeys、目标查询、巡逻、攻击请求或 AI adapter 时使用。
---

# AI Capability 入口

## 必读入口

- `DocsAI/README.md`
- `DocsAI/ECS/Capabilities/AI/README.md`
- `DocsAI/ECS/Capabilities/Movement/README.md`
- `DocsAI/ECS/Capabilities/Ability/README.md`
- `DocsAI/ECS/Capabilities/Unit/README.md`
- `DocsAI/ECS/Runtime/Data/Data系统说明.md`

## 源码位置

- `Src/ECS/Capabilities/AI/`
- `Src/ECS/Capabilities/Movement/`
- `Src/ECS/Capabilities/Ability/`
- `Src/ECS/Capabilities/Unit/`
- `Data/DataKey/AI/`

## 规则

- AI 节点只写意图、目标和请求事件，不直接移动节点、不直接造成伤害。
- 移动意图写入 `MovementDataKeys.AIMoveDirection / AIMoveSpeedMultiplier`。
- 攻击请求当前通过 Unit/AttackComponent 兼容入口消费；新增 Attack 专属 Capability 前不要把 Attack 作为独立事实源。
- AI 目标候选由 `IAITargetQuery` 注入；`RuntimeAITargetQuery` 只是纯 Runtime 全量扫描回退，Godot/game 可注入 physics-aware query。
- 自动施法上下文通过 `AbilityTargetingTool` / BehaviorNode 显式准备，Ability 候选目标归 `IAbilityTargetQuery`。
- `GodotAIComponent` 是 GodotBridge Adapter legacy class name；`AbilityService` 可替换，默认只在 adapter boundary 使用进程级入口。

## 验证

```bash
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
# 如果承载游戏提供 runner，再执行 Godot smoke:
# cd /home/slime/Code/SlimeAI/Games/<GameWithRunner>
# Tools/run-godot-scene.sh run-main-smoke --log-dir .ai-temp/scene-tests/runs
```
