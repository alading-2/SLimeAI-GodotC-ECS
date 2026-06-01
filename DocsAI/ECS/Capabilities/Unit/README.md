# Unit Capability

> 状态：current
> 定位：单位 owner 文档入口。
> 更新：2026-06-01

## 入口

| 文档 | 说明 |
| ---- | ---- |
| [Component/Common/AttackComponent/AttackComponent.md](Component/Common/AttackComponent/AttackComponent.md) | 攻击状态机、双计时器和事件流程 |
| [Component/Common/DataInitComponent/DataInitComponent.md](Component/Common/DataInitComponent/DataInitComponent.md) | 单位生成时的运行时数据初始化 |

## 源码

```text
Src/ECS/Capabilities/Unit/
├── Entity/
├── Component/
├── Events/
└── Presets/
```

Unit EventType 已迁入 `Src/ECS/Capabilities/Unit/Events/`；DataKey 当前仍由 `Data/DataKey/Unit/` 管理，generated 输出路径迁移需等待 DataOS generator 决策。
