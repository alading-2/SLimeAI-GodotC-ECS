# Unit Capability

> 状态：current
> 定位：单位 owner 文档入口。
> 更新：2026-06-04

## 入口

| 文档 | 说明 |
| ---- | ---- |
| [Component/Common/AttackComponent/AttackComponent.md](Component/Common/AttackComponent/AttackComponent.md) | 攻击状态机、双计时器和事件流程 |
| [Component/Common/DataInitComponent/DataInitComponent.md](Component/Common/DataInitComponent/DataInitComponent.md) | 单位生成时的运行时数据初始化 |
| [../../Runtime/Component/ComponentManifest.md](../../Runtime/Component/ComponentManifest.md) | Unit 默认 Component profile 和注册门禁 |

## 源码

```text
Src/ECS/Capabilities/Unit/
├── Entity/
│   └── UnitComponentCompositionProfiles.cs
├── Component/
├── Events/
└── Presets/  # legacy Component Preset 对照输入，不作为新组合事实源
```

Unit EventType 已迁入 `Src/ECS/Capabilities/Unit/Events/`；DataKey 当前仍由 `Data/DataKey/Unit/` 管理，generated 输出路径迁移需等待 DataOS generator 决策。

Player / Enemy / TargetingIndicator 默认组件组合由 `UnitComponentCompositionProfiles` 提供。新增 Unit 默认组合时只改 C# profile，不新增 Component Preset `.tscn`。
