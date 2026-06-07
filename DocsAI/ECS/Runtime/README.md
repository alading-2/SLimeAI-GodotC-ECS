# ECS Runtime 文档

> 状态：current
> 定位：SlimeAI ECS 共享内核文档入口。
> 更新：2026-06-01

## 定位

`Runtime/` 承载所有 Capability 共享的 ECS 基础设施。这里不放 Ability、Damage、Movement 等业务 owner 规则。

## Owner

| Owner | 当前文档来源 | 目标源码 |
| ---- | ---- | ---- |
| Entity | [Entity/](Entity/) | `Src/ECS/Runtime/Entity/` |
| Component | [Component/](Component/) | `Src/ECS/Runtime/Component/` 与 `Src/ECS/Runtime/Entity/Components/` |
| Data | [Data/](Data/) | `Src/ECS/Runtime/Data/` 与 `Data/DataOS/` |
| Event | [Event/](Event/) | `Src/ECS/Runtime/Event/` |
| System | [System/](System/) | `Src/ECS/Runtime/System/` |
| Mount | [Mount/](Mount/) | `Src/ECS/Runtime/Mount/` |
| NodeLifecycle | [NodeLifecycle/](NodeLifecycle/) | `Src/ECS/Runtime/NodeLifecycle/` |

## 当前状态

SDD-0025 后，`Src/ECS/Base/` 不再保留当前源码入口。具体 Entity、Component、Preset 已按 owner 分散到 `Src/ECS/Capabilities/<owner>/`、`Src/ECS/Tools/`、`Src/ECS/UI/` 或 `Src/ECS/Test/`。

执行任务时先读本索引，再读对应 owner 文档；旧路径只在迁移清单和历史迁移来源标记中用于追溯。

## 红线

- Runtime 不承载具体玩法流程。
- Runtime 不定义功能域事件 payload，除非它是生命周期或内核协议。
- Runtime/Component 不承载具体业务组件；业务组件归 Capability owner。
- Runtime/Mount 只管理运行时 SceneTree 挂载点，不管理 Entity 生命周期或对象池状态。
- Runtime/NodeLifecycle 只做底层 Node 注册与 diagnostics，不作为 gameplay 全局查询 API。
- Runtime 改动必须按共享基础设施影响面运行构建和测试。
