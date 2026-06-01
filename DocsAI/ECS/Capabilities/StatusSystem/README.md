# StatusSystem Capability

> 状态：current
> 定位：实体状态效果 owner 文档入口。
> 更新：2026-06-01

## 入口

| 文档 | 说明 |
| --- | --- |
| [System/Concept.md](System/Concept.md) | StatusSystem 当前概念、职责边界和依赖 |
| [Concepts/实体状态效果系统设计.md](Concepts/实体状态效果系统设计.md) | 状态定义、实例、集合、快照和状态控制器设计 |
| [Concepts/实体状态管理与AI系统协调方案.md](Concepts/实体状态管理与AI系统协调方案.md) | 状态效果与 AI / 攻击 / 移动协调 |

## 源码

```text
Src/ECS/Capabilities/StatusSystem/
├── System/
└── Component/
```

状态效果是 Unit、AI、Attack、Movement 等系统共享的能力限制层；不要把它放回 Runtime/System，也不要用单一布尔值替代多来源状态实例。

