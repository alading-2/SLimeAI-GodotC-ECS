# ECS 框架研究入口

> 状态：current / thinking reference  
> 日期：2026-06-16  
> 定位：解释真正的 ECS 是什么，以及为什么 SlimeAI 不应再把当前 Godot C# 框架描述为 ECS 框架。  
> 执行边界：本目录是概念研究，不直接作为代码修改依据；SlimeAI 方向裁决见 `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Runtime/9.ECS框架优化/4.弃用ECS框架/`。

## 一句话结论

ECS 不是“类里有 Entity / Component / System 这几个词”。真正 ECS 的核心是：

```text
Entity 是 ID
Component 是纯数据
System 是按数据查询批量执行的逻辑
World / storage 负责按 component 组合组织内存
Query / schedule 负责让系统高效、可并行、可预测地处理数据
```

它首先是 data-oriented design 的 runtime 架构，主要收益是缓存友好、批量迭代、并行调度和组合替代继承。解耦是 ECS 的收益之一，但不是只有 ECS 才能实现解耦。

SlimeAI 当前更需要的是 Godot Node/OOP 组合、功能 owner、事件驱动、强类型状态和 AI 可读验证链，而不是重写一个完整 ECS storage/query/schedule runtime。

## 阅读顺序

1. [`01-什么才是真正的ECS框架.md`](./01-什么才是真正的ECS框架.md)  
   解释 ECS 的概念、目的、数据结构、经典框架和常见误区。
2. [`02-Godot与ECS适配性.md`](./02-Godot与ECS适配性.md)  
   解释 Godot 官方 Node/SceneTree 设计为什么天然更适合 OOP + composition。
3. [`03-SlimeAI应保留与放弃的ECS概念.md`](./03-SlimeAI应保留与放弃的ECS概念.md)  
   把 ECS 概念拆成可保留心智模型和必须放弃的底层方向。
4. [`04-外部研究证据摘要.md`](./04-外部研究证据摘要.md)  
   记录 Godot / Unity Entities / Bevy / Flecs / QFramework 的证据、推断、未知项和采纳边界。

## 关键来源

- Godot FAQ：Godot 官方明确说明 Godot 不使用 ECS，而是基于继承；也可以用带脚本的 child Node 动态添加/移除行为。
- Godot Design Philosophy：Godot 强调 Object-oriented design、Scene composition 和 Node hierarchy。
- Unity Entities：通过 archetype 和 chunk 把相同 component 组合的 entity 组织到连续内存。
- Bevy ECS：把程序拆成 Entity、Component、System，并通过 Query 让 System 按数据运行。
- Flecs：强调 ECS 的快速迭代、降低 cache miss、module 和 deferred operations。
- QFramework：通过 `Architecture<T>`、Model / System / Utility、Command / Query / Event、`BindableProperty<T>` 展示应用层解耦和强类型状态，但不是 SlimeAI 底层 runtime 方案。

## 非目标

- 不要求 SlimeAI 立刻删除所有 `ECS` 路径名。
- 不把 Bevy / Unity Entities / Flecs / EnTT API 复制为 SlimeAI public API。
- 不在本目录制定 Data 重写执行任务。
- 不把 ECS 概念研究当成“以后必须重新实现 ECS”的理由。
