# Data 架构学习入口

> 状态：current / research decision  
> 日期：2026-06-15  
> 原始问题：见 [`source-request.md`](./source-request.md)。  
> 上游设计：[`../5.Data类型系统重构/00-README.md`](../5.Data类型系统重构/00-README.md)、[`../5.Data类型系统重构/09-Data系统根本裁决与重构路线.md`](../5.Data类型系统重构/09-Data系统根本裁决与重构路线.md)、[`06-运行时解耦第一原则与框架目标.md`](./06-运行时解耦第一原则与框架目标.md)。  
> 资源边界：`externalResources.enabled = engine-framework, official-docs`；读取范围为本地 `Resources/Engine/Docs`、`Resources/Engine/Engine/QFramework`、临时 clone 的 `fanqie404/FrameworkDesign`、Context7 QFramework / Unity Entities / Bevy ECS / Flecs / .NET docs；`copiedCodeOrAssets: none`。

## 一句话结论

用户的担心成立：框架不是“多写几个 manager”就能稳定的东西，尤其 C# 游戏框架同时牵涉类型系统、生命周期、事件、数据输入、验证、性能和工具链。继续闭门自研会反复碰壁。

但当前不建议直接把 SlimeAI 换成 QFramework、Friflo、Arch、Unity Entities 或任何成熟 ECS。原因不是这些框架不好，而是 SlimeAI 的目标不是单纯做 Unity 应用层架构或纯 ECS runtime；SlimeAI 的核心目标已经校准为：

```text
Godot C# 可运行
多游戏功能可组合、可裁剪、可启停
功能 owner 边界清楚
AI 能稳定路由和修改
DataOS 可生成、可验证、可追溯
运行时协议强类型
验证和日志可观察
```

推荐方向是：

```text
先学习成熟框架的边界和取舍，再继续做 SlimeAI。

QFramework 用来学习架构层级、Command/Query/Event、复杂度控制和教学路径。
传统 ECS / C# ECS 用来学习存储、typed payload、query、deferred command 和 perf observation。
Unity Entities / Data baking 用来学习 authoring -> runtime 的边界。
SlimeAI 不复制 API，只吸收机制。
```

## 阅读顺序

1. [`01-问题判断与总体裁决.md`](./01-问题判断与总体裁决.md)  
   判断“是否应该直接用成熟框架”的问题是否成立，以及为什么推荐学习后自研。
2. [`02-QFramework架构学习与采纳边界.md`](./02-QFramework架构学习与采纳边界.md)  
   专门分析 QFramework / FrameworkDesign：学什么、拒绝什么、对 Data 有什么启发。
3. [`03-成熟ECS与CSharp框架学习路线.md`](./03-成熟ECS与CSharp框架学习路线.md)  
   给出应该看的成熟源码、阅读顺序和学习目标。
4. [`04-Data系统学习落点与重构建议.md`](./04-Data系统学习落点与重构建议.md)  
   把外部学习转成 SlimeAI Data 后续 SDD 的具体边界。
5. [`05-证据与采纳决策.md`](./05-证据与采纳决策.md)  
   记录 Evidence / Inference / Unknown、Adopt Now / Later / Reject 和后续开放问题。
6. [`06-运行时解耦第一原则与框架目标.md`](./06-运行时解耦第一原则与框架目标.md)  
   回答用户补充问题：SlimeAI 第一目标是 runtime 功能解耦，Component/System 解耦必须保留；表格驱动和 AI-first 都排在底层 runtime 之后。
7. [`07-QFramework之后的CSharp源码学习顺序.md`](./07-QFramework之后的CSharp源码学习顺序.md)<br>
   回答“只会 C#，QFramework 之后看什么”：QFramework 学规则，Friflo 学 runtime 边界，Arch 学 storage/query，DefaultEcs 学易用 API，Bevy / Unity Entities 学概念。
8. [`08-框架理论学习策略.md`](./08-框架理论学习策略.md)<br>
   回答“是否需要网上查教程学习框架理论”：需要补少量理论，但只学能约束 Runtime 解耦的内容，不泛搜教程重搭框架。

## 当前裁决

- `Adopt Now`：把 QFramework 当成架构学习第一案例，重点学“少规则 + 明确层级 + Command/Query/Event + BindableProperty 初值通知 + Architecture.Init 作为架构图”。
- `Adopt Now`：把成熟 ECS 的 typed component / storage lane / deferred command / perf observation 当成 SlimeAI runtime 设计参考。
- `Adopt Now`：把启动前 Capability/Profile/System 组合和运行中 System 受控启停写成框架第一目标；Component/System 解耦保留，但必须带 manifest、phase、command buffer 和 diagnostics 边界。
- `Adopt Now`：Data 后续继续执行 `Runtime Simplification -> Type Contract -> RuntimeId Storage`，不要被 QFramework 的 Model 形态或传统 ECS chunk 形态打断。
- `Adopt Later`：表格驱动功能拼装、Capability activation actions 和上层 Feature 简化，排在底层 runtime 简化之后。
- `Adopt Later`：当 SlimeAI Data 简化后仍出现性能瓶颈，再评估 numeric lane、typed sparse lane 或完整 ECS storage 分支。
- `Reject`：不把 QFramework `Architecture<T>`、`IController`、`ICommand` 对象层、`TypeEventSystem.Global`、`BindableProperty<T>` 直接移入 SlimeAI runtime。
- `Reject`：不引入外部 ECS runtime 依赖，不把 SlimeAI 改造成 Unity Entities / Bevy / Arch / Friflo 克隆。
- `Learning Order`：QFramework 之后优先读 Friflo.Engine.ECS，再读 Arch 和 DefaultEcs；Bevy / Unity Entities 暂只学 plugin/schedule/run condition 和 authoring/runtime 分层。
- `Theory Strategy`：理论学习只占辅助位置，优先读 Game Programming Patterns 和 .NET API 设计规则；所有理论必须回写到 SlimeAI 的 Adopt / Reject、规则表、RFC 或 SDD。
