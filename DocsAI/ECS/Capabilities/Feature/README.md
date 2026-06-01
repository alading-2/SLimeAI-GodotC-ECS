# Feature Capability

> 状态：current
> 定位：Feature 生命周期 owner 文档入口。
> 更新：2026-06-01

## 入口

| 文档 | 说明 |
| ---- | ---- |
| [System/Usage.md](System/Usage.md) | FeatureSystem 生命周期、FeatureContext、Handler 与 Ability 接入 |
| [System/Concept.md](System/Concept.md) | Feature 授予、启用、激活、结束、移除的概念边界 |

## 源码

```text
Src/ECS/Capabilities/Feature/
├── Events/
└── System/
    └── Action/
```

Feature EventType 已迁入 `Src/ECS/Capabilities/Feature/Events/`；authoring 和 runtime projection 当前仍由 `Data/DataOS/`、`Data/Feature/`、`Data/DataKey/Feature/` 管理，generated 输出路径迁移需等待 DataOS generator 决策。
