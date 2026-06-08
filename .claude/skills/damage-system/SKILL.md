---
name: damage-system
description: 修改 SlimeAI ECS Damage Capability、DamageInfo、处理器管线、HealService、DamageTool 或接触伤害桥时使用。
---

# Damage Capability 入口

## 必读入口

- `DocsAI/README.md`
- `DocsAI/ECS/Capabilities/Damage/README.md`
- `DocsAI/ECS/Capabilities/Collision/README.md`
- `DocsAI/ECS/Capabilities/Unit/README.md`
- `DocsAI/ECS/Runtime/Data/Data系统说明.md`

## 源码位置

- `Src/ECS/Capabilities/Damage/`
- `Src/ECS/Capabilities/Damage/Tests/`
- `Src/ECS/Capabilities/Collision/`
- `Src/ECS/Capabilities/Unit/`
- `Data/DataKey/Unit/`

## 规则

- 伤害和治疗入口分别是 `DamageService.Default` / `HealService`（`Instance` 是别名，向后兼容）。
- `DamageService` 构造接受可选 `HealService? healService = null`；`LifestealProcessor` 通过 `DamageService` 构造注入 `HealService`，不再硬编码 `HealService.Instance`。
- **测试必须用 `new DamageService()` 或 `new DamageService(new HealService())` 独立实例，禁用 `Default / Instance`**。
- 概率值统一 0-100，计算时再除以 100。
- 新伤害修正写成 `IDamageProcessor` 并明确优先级。
- framework 统计命名使用 total / encounter / combat / session 等中性词；不要新增 BrotatoLike wave-specific DataKey。旧 `Damage.Wave*` 已按 Bucket C 改为 `Damage.Encounter*`。
- 接触、攻击、技能、投射物只组装 `DamageInfo`，不要绕过 `DamageService` 直接写 `CurrentHp`。
- `DamageService` 命令执行成功不等于实际扣血；`DamageProcessResult.Processed` 表示管线处理完成，`AppliedDamage / ActualDamage` 才表示 HealthComponent 实际扣血。`DamageTool.ApplyToList` 和技能命中计数必须只把 `AppliedDamage=true` 算作实际命中。
- `DamageInfo.Attacker` 是直接来源；暴击、吸血、统计和击杀归属通过 `EntityAttributionResolver.ResolveUnit/ResolveChain` 读取 Projectile / Effect / Source / Origin projection。不要恢复 `EntityRelationshipTraversal.FindAncestorOfType` 或 parent-chain attribution。
- `GodotContactDamageComponent` 是 GodotBridge Adapter legacy class name，`DamageService` / `TimerManager` 可替换；默认进程级入口只允许在 adapter boundary 使用。

## 验证

```bash
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
# 如果承载游戏提供 runner，再执行 Godot smoke:
# cd /home/slime/Code/SlimeAI/Games/<GameWithRunner>
# Tools/run-godot-scene.sh run-main-smoke --log-dir .ai-temp/scene-tests/runs
```
