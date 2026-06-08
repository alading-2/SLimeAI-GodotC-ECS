# Damage Capability

> 状态：current
> 定位：伤害 owner 文档入口。
> 更新：2026-06-08

## 入口

| 文档 | 说明 |
| ---- | ---- |
| [System/Usage.md](System/Usage.md) | DamageService、DamageInfo 和 10 阶段处理管线 |
| [System/Concept.md](System/Concept.md) | Damage pipeline 概念和职责边界 |

## 源码

```text
Src/ECS/Capabilities/Damage/
├── System/
└── Tests/
```

`HealthComponent`、`LifecycleComponent` 等 Unit 组件暂归 `Unit` 能力切片处理；Damage 本轮只迁伤害服务、处理器、统计系统和测试。
