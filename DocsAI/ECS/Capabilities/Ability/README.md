# Ability Capability

> 状态：current
> 定位：技能 owner 文档入口。
> 更新：2026-06-04

## 入口

| 文档 | 说明 |
| ---- | ---- |
| [System/Usage.md](System/Usage.md) | AbilitySystem 使用说明和施法流水线 |
| [System/Concept.md](System/Concept.md) | Ability 三层架构概念 |
| [System/TargetingSystem/Usage.md](System/TargetingSystem/Usage.md) | 异步点选目标会话 |
| [Component/TriggerComponent/README.md](Component/TriggerComponent/README.md) | 触发组件 |
| [Component/CooldownComponent/README.md](Component/CooldownComponent/README.md) | 冷却组件 |
| [Component/ChargeComponent/README.md](Component/ChargeComponent/README.md) | 充能组件 |
| [Component/CostComponent/README.md](Component/CostComponent/README.md) | 消耗组件 |
| [../../Runtime/Component/ComponentManifest.md](../../Runtime/Component/ComponentManifest.md) | Ability 默认 Component profile 和注册门禁 |

## 源码

```text
Src/ECS/Capabilities/Ability/
├── System/
├── Component/
├── Entity/
│   └── AbilityComponentCompositionProfiles.cs
├── Events/
├── Presets/  # legacy Component Preset 对照输入，不作为新组合事实源
└── Tests/
```

Ability EventType 已迁入 `Src/ECS/Capabilities/Ability/Events/`；DataKey 当前仍由 `Data/DataKey/Ability/` 管理，generated 输出路径迁移需等待 DataOS generator 决策。

Ability 默认组件组合由 `AbilityComponentCompositionProfiles.Default()` 提供。新增 Ability 默认组合时只改 C# profile，不新增 Component Preset `.tscn`。

## Log

Ability owner 使用 `owner=Ability`。施法入口 `AbilitySystem.TryTriggerAbilityWithContext` 写 `OperationTrace`：

| operation | phase | outcome | 关键字段 |
| --- | --- | --- | --- |
| `AbilityTryTrigger` | `Ability` | `Succeeded` | `abilityName`、`triggerMode` |
| `AbilityTryTrigger` | `Ability` | `Failed` | `abilityName`、`reason`，或阻断 message |

规则：

- 冷却、充能、消耗和目标选择失败要写 `outcome=Failed`，不要用 `severity=Success/Warning` 表达业务结果。
- 高频自动触发不要逐帧 trace；只在触发尝试完成时写 flow summary。
- 测试中的断言结果走 `ValidationSession`，不要输出 `[PASS]` / `[FAIL]`。

查询示例：

```bash
Workspace/Tools/logctl/logctl query --analysis-dir <run>/analysis owner=Ability operation=AbilityTryTrigger
```
