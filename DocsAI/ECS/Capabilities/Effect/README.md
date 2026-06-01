# Effect Capability

> 状态：current
> 定位：特效 owner 文档入口。
> 更新：2026-06-01

## 入口

| 文档 | 说明 |
| ---- | ---- |
| [System/Concept.md](System/Concept.md) | EffectTool、EffectComponent、EffectEntity 三层架构 |

## 源码

```text
Src/ECS/Capabilities/Effect/
├── System/
├── Component/
└── Entity/
```

Effect DataKey 当前仍由 `Data/DataKey/Effect/` 和 generated handle 管理；generated 输出路径迁移需等待 DataOS generator 决策。
