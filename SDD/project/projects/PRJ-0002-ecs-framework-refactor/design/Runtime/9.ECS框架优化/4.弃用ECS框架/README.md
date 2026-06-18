# 弃用 ECS 框架方向

> 状态：current / direction decision  
> 日期：2026-06-16  
> 原始问题：见 [`source-request.md`](./source-request.md)。  
> 上游研究：[`DocsAI/思考/框架/ECS框架/`](../../../../../../../../DocsAI/思考/框架/ECS框架/)。

## 一句话结论

SlimeAI 应正式弃用“ECS 框架”作为目标方向，转为 `SlimeAIFramework`：

```text
SlimeAIFramework
  = Godot Node / OOP / Scene composition 优先
  + Object / Component / System / Feature 功能解耦
  + Event-driven communication
  + Data authoring / 受控共享状态
  + AI-first 文档、验证、日志和工作流
```

这不是“放弃解耦”，而是承认 SlimeAI 真正需要的是功能驱动、快速 MVP 和 Godot 原生组合能力，不是重写一个 data-oriented ECS storage/query/schedule runtime。

## 为什么这是必要裁决

旧方向最大问题不是名字，而是方向会持续误导实现：

```text
ECS 框架
  -> Data 必须成为核心
  -> Component 必须数据逻辑分离
  -> System 必须承载功能执行
  -> Godot Node 只能做 bridge
  -> DataOS / policy / descriptor 不断扩张
```

但 SlimeAI 的真实目标是：

```text
快速开发游戏 MVP
功能能快速添加、删除、启停
模块之间少互相调用实现细节
AI 能看懂、能改、能验证
Godot C# 性能不差，性能热点再局部优化
```

这组目标用 Godot OOP / Node composition / event-driven architecture 更直接。

## 阅读顺序

1. [`01-方向裁决与真实问题.md`](./01-方向裁决与真实问题.md)  
   记录为什么弃用 ECS，真实问题是什么，新的框架目标是什么。
2. [`02-新框架概念边界.md`](./02-新框架概念边界.md)  
   定义 Object / Component / System / Feature / Event / Data 的新语义。
3. [`03-Data系统问题收敛与重写边界.md`](./03-Data系统问题收敛与重写边界.md)  
   整合旧 `5.Data类型系统重构` 与 `6.架构学习` 的问题结论，只保留问题和待定边界。
4. [`04-QFramework采纳边界.md`](./04-QFramework采纳边界.md)  
   判断 QFramework 是否适合直接采用，以及哪些规则可以学习。
5. [`05-后续迁移路线与确认点.md`](./05-后续迁移路线与确认点.md)  
   给出后续文档、Data、命名和实现迁移顺序，以及必须确认的问题。
6. [`DocsAI/思考/框架/ECS框架/04-外部研究证据摘要.md`](../../../../../../../../DocsAI/思考/框架/ECS框架/04-外部研究证据摘要.md)  
   作为本方向的外部资料证据摘要，记录 Evidence / Inference / Unknown 和 Adopt / Reject 边界。

## 当前裁决

- `Adopt Now`：弃用 ECS 作为框架身份，正式框架名为 `SlimeAIFramework`；文档和后续设计改用 Godot/OOP/Object/Component/System/Feature/Event/Data 语义。
- `Adopt Now`：保留 Godot Node 和 C# Component 作为功能承载者，允许 Component 持有内部状态。
- `Adopt Now`：保留事件驱动作为解耦通信主线。
- `Adopt Now`：Data 名字保留，只作为受控共享状态 / authoring / validation 工具重新设计，不再默认承载所有状态。
- `Adopt Now`：QFramework 只学习少规则、Command/Query/Event、Model 强类型数据和架构图式注册，不直接引入依赖。
- `Adopt Later`：全仓路径 `ECS`、`Entity`、`Data` 命名迁移，单独开 SDD；本轮只冻结方向。
- `Reject`：不重写完整 ECS runtime，不实现 archetype/chunk/query/schedule 作为 SlimeAI 底层。
- `Reject`：不再把 Data 系统复杂化解释为 ECS 必要成本。
- `Reject`：不把 QFramework `Architecture<T>`、`IController`、全局 `TypeEventSystem` 或 `BindableProperty<T>` 直接搬入 SlimeAI runtime。

## 立即影响

- `DocsAI/ECS框架与AIFirst方向决策.md` 和 `DocsAI/ECS/README.md` 应标记为被本裁决覆盖。
- 后续 Data SDD 不应继续执行 `Data Runtime Simplification -> Type Contract -> RuntimeId Storage` 旧路线，除非先被新 Data 设计重新确认。
- `5.Data类型系统重构` 与 `6.架构学习` 保留为历史问题证据，不再作为当前实现路线入口。
- 新实现前必须先冻结 `Object / Component / System / Feature / Event / Data` 概念边界；当前冻结入口是 [`../../10.GodotOOP框架方向/README.md`](../../10.GodotOOP框架方向/README.md)。

## 外部资料边界

本轮使用的外部资料：

- Godot 官方文档：FAQ、Design Philosophy、Node composition / interface-like patterns。
- Unity Entities 官方文档：archetype / chunk / component storage。
- Bevy 官方文档：Entity / Component / System / Query / Schedule。
- Flecs 官方文档：fast iteration、cache miss、module、deferred operations。
- QFramework Context7 `/liangxiegame/qframework` 和本地源码 `/home/slime/Code/SlimeAI/Resources/Engine/Engine/QFramework`。
- 证据摘要：`DocsAI/思考/框架/ECS框架/04-外部研究证据摘要.md`。

采纳方式：只采纳概念和边界，不复制外部 API 或代码。
