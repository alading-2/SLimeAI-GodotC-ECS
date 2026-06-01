# Runtime System 文档入口

> 状态：current
> 更新：2026-06-01
> 范围：`Src/ECS/Runtime/System/`

## 入口

| 文档 | 说明 |
| --- | --- |
| [Concept.md](Concept.md) | System Core 概念、职责边界和依赖 |
| [Usage.md](Usage.md) | SystemRegistry / SystemManager / DataOS system config 使用说明 |
| [Concepts/系统与状态分层总览.md](Concepts/系统与状态分层总览.md) | 系统治理、项目状态、实体状态和 AI 决策分层 |

## 源码

```text
Src/ECS/Runtime/System/
```

Runtime System 只管理注册、生命周期、状态门禁和系统配置。具体 Ability、Damage、Movement、StatusSystem、TestSystem 等行为系统归 `Src/ECS/Capabilities/<owner>/System/`。

