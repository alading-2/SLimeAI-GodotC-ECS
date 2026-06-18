# SlimeAI 应保留与放弃的 ECS 概念

> 日期：2026-06-16  
> 目标：把 ECS 研究转成 SlimeAI 可执行的概念边界，避免“全部推翻”和“继续伪 ECS”两个极端。

## 结论

SlimeAI 应弃用 ECS 作为框架身份，但不需要删除所有 ECS 词汇背后的有用概念。

推荐边界是：

```text
放弃：ECS runtime 目标、纯数据 component storage、archetype/chunk 路线、Data 作为框架核心。
保留：组合优先、功能 owner、事件驱动、稳定身份、可启停服务、必要的批处理和可观察验证。
```

## 概念映射

| 旧 ECS 词 | 新语义 | 处理 |
| --- | --- | --- |
| Entity | Object / RuntimeObject / GameObjectRef | 改概念，不急着改路径；保留稳定 ID 和生命周期追踪 |
| Component | Godot Node/C# 功能组件 | 保留，但允许保存内部状态，不再强制纯数据 |
| System | Runtime system / coordinator | 名字保留，但不再表示传统 ECS System |
| Data | 受控共享状态 / 表格驱动协议 | 名字保留，大幅收窄，只放跨功能共享、表格驱动或验证追踪状态 |
| Event | Event / Message / Signal bridge | 保留，作为解耦通信主线 |
| World | RuntimeContext / GameRuntime | 可后续重命名，用于承载服务、对象和调度 |
| Capability | Feature / Module / Gameplay Feature | 保留并提升为核心功能边界 |

## 必须放弃的旧假设

### Data 是 ECS 核心

这个假设应放弃。真正 ECS 的核心数据结构是 component storage/query/schedule，不是一个统一 Data dictionary。SlimeAI 当前 Data 也没有形成传统 ECS storage，继续称它为 ECS 核心只会误导设计。

### Component 必须无状态

在 Godot OOP 框架里，Component 可以保存内部状态。需要被多个功能共享、被 DataOS/AI/validator/diagnostic 追踪的状态才进入 Data。单功能内部缓存、计时、临时索引不应强制外移。

### System 必须集中处理所有逻辑

Godot Node 本身已经提供生命周期和局部行为。只有跨对象、跨组件、批处理、全局协调、查询、资源管理等场景才需要 service/system。

### 所有解耦都靠统一数据层

解耦可以来自：

- Node composition
- 接口和 owner service
- 事件驱动
- 功能 manifest
- 依赖注入或 runtime context
- 测试和日志契约
- DataOS / authoring 生成

统一 Data 只是其中一种工具，不应成为默认答案。

## 应保留的经验

### 组合替代继承

这是 ECS 和 Godot 都支持的共同点。SlimeAI 仍应避免巨型继承树，用小组件、功能 owner 和 scene composition 组合对象。

### 数据和逻辑边界清晰

放弃 ECS 不等于把所有逻辑写回巨型类。Component 内部状态可以存在，但跨功能状态、公共读写、事件输出和验证入口必须清楚。

### 结构变化要受控

动态添加/移除 Node 或功能组件可以保留，但必须有生命周期和事件注销规则。运行中启停服务也必须有 blocked reason、diagnostics 和验证。

### 性能热点局部 DOD

如果某个 owner 真的遇到性能瓶颈，可以局部使用数组、池、sparse set、batch query 或 command buffer。不要为了未来可能的性能问题重写整个框架。

## 新框架第一原则

```text
SlimeAIFramework 是 Godot C# OOP gameplay framework。

第一目标：快速开发游戏 MVP。
第二目标：功能高度解耦，可添加、移除、启停。
第三目标：AI 能稳定理解、修改、验证。
性能目标：C# + Godot + 局部优化，先不做全局 ECS runtime。
```

## 后续命名建议

短期不做全仓重命名，避免路径 churn。文档先明确：

- `DocsAI/ECS` 是历史路径名，不代表新框架方向。
- `Src/ECS` 是历史源码目录，不代表继续实现 ECS runtime。
- 新设计文档使用 `Object / Component / System / Feature / Event / Data`。

中期再通过单独 SDD 做目录和命名迁移，例如：

```text
DocsAI/ECS -> DocsAI/Framework
Src/ECS -> Src/Framework 或 Src/Runtime
Entity -> Object
Capabilities -> Features
```

是否迁移取决于成本和当前任务优先级，不应在方向未冻结前做机械改名。
