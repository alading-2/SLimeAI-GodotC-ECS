# Spawn Capability

> 状态：current
> 定位：生成 owner 文档入口。
> 更新：2026-06-01

## 入口

| 文档 | 说明 |
| ---- | ---- |
| [System/Usage.md](System/Usage.md) | SpawnSystem 使用说明、DataOS snapshot 配置和调用方式 |
| [System/Concept.md](System/Concept.md) | 程序化生成 Pipeline 概念 |

## 源码

```text
Src/ECS/Capabilities/Spawn/
├── System/
└── Tests/
```

Spawn 配置当前仍由 `Data/Config/Spawn/` 和 DataOS snapshot 管理；Spawn tests 已就近迁入 `Src/ECS/Capabilities/Spawn/Tests/`。
