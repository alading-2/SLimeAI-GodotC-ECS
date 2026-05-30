---
name: attack-system
description: 修改 SlimeAI.GameOS AttackService、AttackDataKeys、攻击请求事件、前摇后摇冷却或 GodotAttackComponent 时使用。
---

# Attack Capability 入口

## 必读入口

- `DocsAI/GameOS/Contracts.md`
- `DocsAI/GameOS/ApiIndex.md`
- `DocsAI/ProjectState.md`

## 源码位置

- `GameOS/Capabilities/Attack/`
- `GameOS/Capabilities/Damage/`
- `GameOS/GodotBridge/GodotAttackComponent.cs`
- `GameOS/GodotBridge/AttackComponent.cs`
- `GameOS/GodotBridge/GodotUnitAnimationComponent.cs`
- `Tests/SlimeAI.GameOS.Tests/`

## 规则

- AI / 输入层发 `Capabilities.Attack.Events.Requested` 或调用 AttackService，不直接扣血。
- 攻击伤害统一走 `DamageService`，标签使用 `DamageTags.Attack`。
- 前摇 / 后摇 / 冷却统一用 `TimerManager`。
- `AttackService.Default` 是进程级默认入口，`Instance` 仅为向后兼容别名；测试用独立实例。
- 旧场景类名兼容放 `AttackComponent` 包装，不污染新 API；`GodotAttackComponent` 是 GodotBridge Adapter legacy class name。

## 验证

```bash
Tools/run-build.sh
Tools/run-tests.sh
cd /home/slime/Code/SlimeAI/Games/BrotatoLike
Tools/run-build.sh
Tools/run-godot-scene.sh run res://SlimeAI/Src/Validation/GameOS/Capabilities/Attack/AttackCapabilityValidation.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/run-godot-scene.sh run-main-smoke --log-dir .ai-temp/scene-tests/runs
```
