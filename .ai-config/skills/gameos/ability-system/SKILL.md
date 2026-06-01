---
name: ability-system
description: 修改 SlimeAI.GameOS AbilityService、AbilityDataKeys、目标选择、冷却充能、自动索敌或技能触发执行流程时使用。
---

# Ability Capability 入口

## 必读入口

- `DocsAI/GameOS/Contracts.md`
- `DocsAI/GameOS/ApiIndex.md`
- `DocsAI/ProjectState.md`
- `DocsAI/DataOS/Overview.md`

## 源码位置

- `GameOS/Capabilities/Ability/`
- `GameOS/Capabilities/Feature/`
- `GameOS/Capabilities/Damage/`
- `GameOS/Capabilities/Projectile/`
- `/home/slime/Code/SlimeAI/Games/BrotatoLike/Src/Game/BrotatoLikeAbilityHandlers.cs`

## 参考资料

- 旧 Ability handler 参数语义：`references/ability-logic-parameters.md`

## 规则

- `AbilityService` 负责门禁、冷却、充能、触发和调度，不隐式做输入层点选。
- 自动索敌通过 `AbilityTargetingTool` 显式准备 `AbilityCastContext`；候选目标由 `IAbilityTargetQuery` 注入，`RuntimeAbilityTargetQuery` 只是纯 Runtime 全量扫描回退。
- `AbilityService` 构造注入 `TimerManager`，可注入 `FeatureService` / `DamageService`；测试和 scoped world 必须用独立实例，不依赖 `Default / Instance`。
- Ability → Feature handler flow 使用 typed `FeatureContext.ActivationPayload` 和 `AbilityExecutedResult`，不要再写 raw `ActivationData` 或从 `ExecuteResult` cast。
- Ability owner 清单统一走 `AbilityInventoryService.Runtime`；新增授予、移除、查询和 UI/AI 消费者不要再调用 `EntityManager.AddAbility/GetAbilities/GetAbilityByName/GetManualAbilities`，兼容 facade 只服务旧调用点。
- Ability owner projection 由 `AbilityInventoryService.OwnerDescriptor` 注册到 `OwnedReferenceRegistry`，运行时用 `AbilityOwnerEntityId` + `OwnedAbilityIds` 投影；禁止恢复 `EntityRelationshipType.ENTITY_TO_ABILITY`。
- 具体技能效果优先实现为 Feature handler 或游戏侧 handler，不把 BrotatoLike 逻辑写进框架通用 Ability。
- 技能造成伤害统一走 `DamageTool` / `DamageService`；投射物走 `ProjectileTool`。

## 运行时 DataKey（AbilityDataKeys）

| DataKey                        | 类型                   | 说明                                                       |
| ------------------------------ | ---------------------- | ---------------------------------------------------------- |
| `Ability.Name`                 | string                 | 技能名称                                                   |
| `Ability.Type`                 | AbilityType            | Active / Passive / Weapon                                  |
| `Ability.TriggerMode`          | AbilityTriggerMode     | Manual / Periodic / OnEvent / Permanent                    |
| `Ability.TargetSelection`      | AbilityTargetSelection | None / Entity / Point / EntityOrPoint                      |
| `Ability.Cooldown`             | float                  | 冷却时间（秒）                                             |
| `Ability.CooldownRemaining`    | float                  | 剩余冷却（运行时）                                         |
| `Ability.Damage`               | float                  | 单次基础伤害                                               |
| `Ability.AutoTargetRange`      | float                  | 自动索敌半径，-1 为不限制                                  |
| `Ability.AutoTargetMaxTargets` | int                    | 自动索敌最大目标数，-1 为不限制                            |
| `Ability.FeatureHandlerId`     | string                 | Feature handler Id，空则走默认伤害                         |
| `Ability.CurrentAbilityIndex`  | int                    | 玩家当前选中的主动技能索引（运行时）                       |
| `Ability.OwnerEntity`          | EntityId?              | ability 实体上指向 owner 的 typed 单引用（运行时）         |
| `Ability.OwnedAbilityIds`      | EntityIdList           | owner 实体拥有的技能 EntityId 列表（typed 多引用，运行时） |

当前框架的 owner-list runtime helper 是 `OwnedReferenceRegistry`，入口挂在 `EntityManager.RegisterOwnedReference / AddOwnedReference / RemoveOwnedReference`。DataOS 尚未原生生成 `DataKey<EntityIdList>`，因此落地迁移前 owner projection 暂以 `DataKey<string>` + `DataKey<string[]>` descriptor 表达；Ability public API 仍必须暴露 `EntityId / EntityIdList` 语义，不要回退到 `RelationshipManager` 或 `DataKey<List<string>>`。

## 验证

```bash
Tools/run-build.sh
Tools/run-tests.sh
cd /home/slime/Code/SlimeAI/Games/BrotatoLike
Tools/run-build.sh
Tools/run-godot-scene.sh run res://SlimeAI/Src/Validation/GameOS/Capabilities/Ability/AbilityCapabilityValidation.tscn --timeout 10 --log-dir .ai-temp/scene-tests/runs
Tools/run-godot-scene.sh run-main-smoke --log-dir .ai-temp/scene-tests/runs
```
