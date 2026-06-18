# SlimeAIFramework Godot OOP 框架方向

> 状态：current direction / design package  
> 日期：2026-06-16  
> 原始问题：见 [`source-request.md`](./source-request.md)。  
> 上游裁决：[`../9.ECS框架优化/4.弃用ECS框架/README.md`](../9.ECS框架优化/4.弃用ECS框架/README.md)。

## 一句话结论

新框架名固定为 `SlimeAIFramework`。

`SlimeAIFramework` 不再把自己描述成 ECS，而是：

```text
Godot C# OOP gameplay framework
  + Object / Component / System / Feature / Event / Data
  + Godot Node / scene composition 优先
  + Component 可持有内部字段
  + System 只负责跨对象协调、批处理、查询和服务化能力
  + Event 驱动通信
  + Data 负责受控共享状态和表格驱动
  + AI-first 文档、验证、日志和 SDD 流程
```

用户判断成立：`Entity -> Object` 是正确方向；`Component`、`System` 这两个词可以保留，但它们不再表示传统 ECS 的纯数据组件和批量系统。`Data` 名字也保留，不改成 `SharedState`。之前文档里的 `SharedState` 应理解为 Data 的职责收窄，不是改名要求。

## 为什么这样定

真正 ECS 的核心是 data-oriented storage、query、schedule 和 cache-friendly iteration。SlimeAI 当前需要的是快速 MVP、Godot 原生组合、功能可裁剪、运行时可观察和 AI 可维护。继续把框架叫 ECS 会把 Data、Component、System 都拉回传统 ECS 语义，导致 Data 继续膨胀、Component 不敢保存内部字段、System 被误解成所有逻辑的唯一入口。

Godot 官方文档强调 Object-oriented design、scene composition 和 Node hierarchy；Unity Entities 的 archetype/chunk 资料则说明真正 ECS 会把同 archetype 的 component 数据放进 chunk。这个差异正好说明 SlimeAI 不应继续假装自己在做 ECS storage。

## 阅读顺序

1. [`01-方向裁决与概念边界.md`](./01-方向裁决与概念边界.md)  
   冻结框架名、概念映射和保留/放弃的语义。
2. [`02-Object与Component模型.md`](./02-Object与Component模型.md)  
   说明 Object、Component、Component 内部字段和 Data 绑定关系。
3. [`03-Feature与System边界.md`](./03-Feature与System边界.md)  
   说明 Feature owner、System 保留语义和不做万能 manager。
4. [`04-Event驱动与生命周期规则.md`](./04-Event驱动与生命周期规则.md)  
   说明 Event 驱动、生命周期、对象池复用和订阅清理。
5. [`Data/README.md`](./Data/README.md)  
   Data 方案入口：Data 名字保留，负责表格驱动、受控共享状态、约束、modifier 和可观察同步。
6. [`05-迁移路线与验证策略.md`](./05-迁移路线与验证策略.md)  
   后续 SDD、最小验证切片和开放确认点。

## 当前裁决

| 主题 | 裁决 |
| --- | --- |
| 框架名 | `SlimeAIFramework` |
| Entity | 概念改为 `Object`；短期不全仓改路径 |
| Component | 名称保留；语义改为 Godot/OOP 功能组件，可保存内部字段 |
| System | 名称保留；语义改为跨对象协调、批处理、查询、资源、生命周期等 runtime service |
| Feature | 作为玩法功能 owner，管理组件组合、事件、Data 进入条件和验证 |
| Event | 保留为基础通信机制；payload 强类型，订阅绑定生命周期 |
| Data | 名字保留；负责受控共享状态和 DataOS 表格驱动，不再默认承载所有状态 |
| SDD-0044 | 用户已删除；不再作为 current_sdd、路线图或恢复入口 |

## 不做什么

- 不继续实现完整 ECS storage / archetype / chunk / query world。
- 不把 `Component` 强制改成纯数据。
- 不把 `System` 当成所有逻辑必须经过的唯一执行器。
- 不把 Data 改名为 `SharedState`。
- 不直接删 `Src/ECS`、`DocsAI/ECS` 或全仓机械替换路径。
- 不直接引入 QFramework、Unity Entities、Bevy、Flecs 或 Unreal GAS 依赖。

