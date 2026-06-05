---
name: attack-system
description: 修改 SlimeAI ECS Unit Attack 兼容入口、AttackDataKeys、攻击请求事件、前摇后摇冷却或 AttackComponent 时使用。
---

# Attack Capability 入口

## 必读入口

- `DocsAI/README.md`
- `DocsAI/ECS/Capabilities/Unit/README.md`
- `DocsAI/ECS/Capabilities/Damage/README.md`
- `DocsAI/ECS/Capabilities/AI/README.md`
- `DocsAI/ECS/Runtime/Data/Data系统说明.md`

## 源码位置

- `Src/ECS/Capabilities/Unit/Component/Common/AttackComponent/`
- `Src/ECS/Capabilities/Damage/`
- `Src/ECS/Capabilities/AI/System/Actions/Combat/RequestAttackAction.cs`
- `Data/DataKey/Unit/`

## 规则

- AI / 输入层只发起攻击请求或写入 AttackComponent 兼容数据，不直接扣血。
- 攻击伤害统一走 `DamageService`，标签使用 `DamageTags.Attack`。
- 前摇 / 后摇 / 冷却统一用 `TimerManager`。
- 当前仓尚未落地独立 Attack Capability；Attack 事实源暂归 `Capabilities/Unit/Component/Common/AttackComponent`，新增独立 Attack Capability 需先走 SDD 设计。
- 旧场景类名兼容放 `AttackComponent` 包装，不污染新 API。

## 验证

```bash
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
# 如果承载游戏提供 runner，再执行 Godot smoke:
# cd /home/slime/Code/SlimeAI/Games/<GameWithRunner>
# Tools/run-godot-scene.sh run-main-smoke --log-dir .ai-temp/scene-tests/runs
```
