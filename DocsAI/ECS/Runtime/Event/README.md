# Runtime Event 文档入口

> 状态：current
> 更新：2026-06-01
> 范围：`Src/ECS/Runtime/Event/`、`Src/ECS/Runtime/Event/Global/`、Capability event payload。

## 入口

| 文档 | 说明 |
| --- | --- |
| [Event系统说明.md](Event系统说明.md) | EventBus、GlobalEventBus 和 typed payload 使用规则 |
| [Concepts/EventBus架构设计.md](Concepts/EventBus架构设计.md) | 为什么自定义 EventBus、三层架构、与现代框架对比 |
| [Concepts/Context模式设计.md](Concepts/Context模式设计.md) | Context Object Pattern 原理、与 EventData 区别、最佳实践 |

## 源码

```text
Src/ECS/Runtime/Event/
Src/ECS/Runtime/Event/Global/
Src/ECS/Capabilities/<owner>/Events/
```

当前事件以 payload 类型作为事件 key；新增事件优先使用 `readonly record struct`，不要恢复字符串事件名或 `XxxEventData` 双写协议。

