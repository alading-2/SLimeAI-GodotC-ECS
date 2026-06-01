# AI Capability

> 状态：current
> 定位：AI owner 文档入口。
> 更新：2026-06-01

## 入口

| 文档 | 说明 |
| ---- | ---- |
| [System/Usage.md](System/Usage.md) | 行为树目录、节点扩展和敌人 AI 组装方式 |
| [System/Concept.md](System/Concept.md) | AI 行为树概念、AIContext 和职责边界 |

## 源码

```text
Src/ECS/Capabilities/AI/
├── System/
│   ├── Actions/
│   ├── Conditions/
│   ├── Core/
│   └── Nodes/
└── Component/
    └── AIComponent/
```

AI DataKey 当前仍由 `Data/DataKey/AI/` 管理；generated 输出路径迁移需等待 DataOS generator 决策。
