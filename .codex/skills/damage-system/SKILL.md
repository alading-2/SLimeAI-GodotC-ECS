---
name: damage-system
description: 处理伤害计算、造成伤害、扩展伤害处理器时使用。适用于：子弹/技能命中造成伤害，实现暴击/闪避/护甲等伤害修正，扩展新的伤害处理阶段。触发关键词：伤害、DamageService、DamageInfo、IDamageProcessor、暴击、闪避、护甲减伤、吸血、造成伤害。
---

# DamageSystem 入口

## 什么时候用

- 造成伤害、批量伤害或 DoT。
- 修改 `DamageInfo`、`DamageService`、`IDamageProcessor`。
- 扩展暴击、闪避、护甲、护盾、吸血等伤害阶段。
- 修改接触伤害或技能命中伤害。

## 转向其它 Skill

- 技能触发、目标、冷却、充能 -> `@ability-system`
- 通用 Feature 生命周期 -> `@feature-system`
- DataKey / 配置字段 -> `@data-authoring`
- 系统门禁和运行状态 -> `DocsAI/Modules/SystemCore.md`

## 必读

- `DocsAI/Modules/DamageSystem.md`
- 涉及 Ability 命中读 `DocsAI/Modules/AbilitySystem.md`
- 涉及系统启停或命令门禁读 `DocsAI/Modules/SystemCore.md`
- 测试选择读 `DocsAI/Tests/测试矩阵.md`
- SlimeAI 迁移目标已存在 Damage / ContactDamage / 处理器管线 / HealService / DamageTool / AttackService 第一批、GodotAttackComponent bridge 和 Attack 动画事件桥第一段：`/home/slime/Code/SlimeAI/SlimeAI/GameOS/Capabilities/Damage`、`/home/slime/Code/SlimeAI/SlimeAI/GameOS/Capabilities/Attack`、`/home/slime/Code/SlimeAI/SlimeAI/GameOS/GodotBridge/GodotContactDamageComponent.cs`、`/home/slime/Code/SlimeAI/SlimeAI/GameOS/GodotBridge/GodotAttackComponent.cs` 和 `/home/slime/Code/SlimeAI/SlimeAI/GameOS/GodotBridge/GodotUnitAnimationComponent.cs`

## 最短流程

1. 判断是业务造成伤害、DoT、处理器、DamageInfo 还是接触伤害。
2. 旧仓库外部伤害入口统一走 `SystemManager.Execute<DamageService, DamageProcessRequest, DamageProcessResult>(...)`；SlimeAI 迁移目标当前走 `DamageService.Instance.Process(...)` 和默认 `IDamageProcessor` 管线，后续接 SystemCore 后再统一门禁。
3. 多目标和持续伤害优先用 `DamageTool`。
4. 新处理器实现 `IDamageProcessor`，选择明确优先级。
5. 补充或运行 DamageSystem / Ability / 碰撞相关测试。
6. 更新 `DocsAI/Modules/DamageSystem.md` 或相关文档。

## 禁止事项

- 不要直接改 `CurrentHp` 扣血。
- 不要手写暴击、闪避、护甲减伤。
- 不要业务代码直接调用 `DamageService.Process` 绕过 SystemCore。
- 不要手写 foreach / TimerManager 复制 `DamageTool` 能力。
- 迁移目标当前已迁入基础 HP 扣减、Damage 事件、ContactDamage、AttackService 普通攻击结算、GodotAttackComponent bridge、暴击、闪避、护盾、护甲、受伤倍率、吸血、统计处理器、DamageTool 和 HealService；SystemCore 门禁仍需分批迁入。

## 推荐验证

```bash
dotnet build
node .codex/skills/godot-scene-test/scripts/godot-scene-runner.mjs run res://Src/ECS/Test/SingleTest/ECS/System/DamageSystemTest/DamageSystemTest.tscn --build
```
