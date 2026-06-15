# 成熟 ECS 与 C# 框架学习路线

> 原始问题：见 [`source-request.md`](./source-request.md)。  
> 本文目标：回答“应该去看哪些源码、怎么看、学习什么模式”。

## 学习原则

不要按“哪个框架最强”来学。应该按 SlimeAI 当前缺的能力来学：

```text
架构边界怎么拆
数据类型怎么固定
authoring 和 runtime 怎么分层
启动前功能怎么组合
运行中系统怎么启停
结构变更怎么延迟
系统执行怎么观察
工具链和文档怎么服务长期维护
```

每读一个框架，只回答四个问题：

```text
它解决什么问题？
它用什么核心机制解决？
SlimeAI 是否有同类问题？
机制应该 Adopt Now / Adopt Later / Reject？
```

不要复制 public API。成熟框架的 API 形状往往绑定自己的语言、引擎、调度器、编辑器和生态。

## 第一组：QFramework / FrameworkDesign

### 看什么

- `Resources/Engine/Engine/QFramework/README.md`
- `Resources/Engine/Engine/QFramework/QFramework.cs`
- `Resources/Engine/Engine/QFramework/QFramework API.md`
- `Resources/Engine/Docs/FrameworkAnalysis/Reports/QFramework/`
- `https://github.com/fanqie404/FrameworkDesign`，本轮临时 clone 到 `.ai-temp/research/FrameworkDesign`

### 学什么

- 框架如何从 MVC 演进到 Command / System / Query / Architecture。
- 如何用少量规则降低项目协作成本。
- 为什么 `Architecture.Init()` 可以成为架构图。
- 如何判断“共享数据”“跨模块逻辑”“写操作”“只读查询”。
- 为什么强类型 Model / BindableProperty 比动态 Data 容器更容易理解。

### 不学什么

- 不学静态单例入口。
- 不学 Unity / Godot Controller 承载玩法。
- 不学把事件全部放全局。
- 不学直接用 BindableProperty 替代 runtime state protocol。

## 第二组：Unity Entities

### 看什么

- Context7：`/websites/unity3d_packages_com_unity_entities_1_4`
- 本地：`Resources/Engine/Engine/EntityComponentSystemSamples/`
- 报告：`Resources/Engine/Docs/FrameworkAnalysis/Reports/EntityComponentSystemSamples/07-Unity-Entities-Samples-源码分析报告.md`

### 学什么

- `IComponentData` 用 C# struct 固定 runtime component 类型。
- authoring component / Baker 把 editor 数据转换成 runtime data。
- archetype / chunk 说明运行时数据不回头猜 authoring 类型。
- Editor-only authoring 与 runtime component 分离。

### 对 SlimeAI 的落点

```text
DataOS validator / generator 就是 SlimeAI 的 Baker。
runtime_snapshot.json 是运行时输入，不是 authoring DB。
DataRuntimeStorage 不应每次 Get/Set 再恢复类型。
```

### 不学什么

- 不复制 Unity DOTS chunk storage。
- 不复制 Unity Baker API。
- 不把 Godot authoring 强行改 Unity editor 形态。

## 第三组：Bevy ECS

### 看什么

- Context7：`/websites/rs_bevy_ecs_0_16_1_bevy_ecs`
- 本地：`Resources/Engine/Engine/bevy/crates/bevy_ecs`
- 报告：`Resources/Engine/Docs/FrameworkAnalysis/Reports/bevy/01-Bevy-源码分析报告.md`

### 学什么

- `StorageType.Table` 默认适合缓存友好迭代。
- `StorageType.SparseSet` 适合频繁增删。
- component 是真实 Rust 类型；storage lane 只是存储策略。
- schedule / commands / plugin 让大框架按模块组合。

### 对 SlimeAI 的落点

```text
Data 不必只有一种存储结构。
Dictionary 可以是短期 stableKey 索引。
runtimeId array / sparse lane / numeric lane 可以按访问模式逐步引入。
Capability 和 Tools 应像 plugin/profile 一样可路由。
System 运行条件应像 run_if 一样成为调度层事实，而不是散在系统内部。
```

### 不学什么

- 不复制 Rust ECS API。
- 不暴露通用 world query DSL 给 AI。
- 不为了“像 Bevy”而重写整个 runtime。

## 第四组：Friflo.Engine.ECS

### 看什么

- 本地：`Resources/Engine/Engine/Friflo.Engine.ECS`
- 报告：`Resources/Engine/Docs/FrameworkAnalysis/Reports/Friflo.Engine.ECS/10-Friflo.Engine.ECS-源码分析报告.md`

### 学什么

- 纯托管 C# 也可以做高性能 ECS，不必过早引入 unsafe / C++。
- `CommandBuffer.Playback` 有严格阶段顺序。
- 遍历中结构变更应 hard fail，而不是靠约定。
- `SystemPerf` 把 ms、alloc、entity count、executions 变成一等观察字段。
- Entity 值类型里带 Id / Revision 的引用安全思路。

### 对 SlimeAI 的落点

```text
RuntimeCommandBuffer 后续应阶段化。
EntityManager 在 tick 遍历中 Spawn/Destroy 应有 guard。
Observation 不应只是日志，应有系统 perf snapshot。
运行中启停必须产生 blocked reason 和 lifecycle trace。
```

### 不学什么

- 不引入 Friflo runtime 依赖。
- 不复制 25+ 泛型 query API。
- 不把 SlimeAI DataOS 变成 Friflo runtime JSON serializer。

## 第五组：Arch / DefaultEcs / Flecs / EnTT

### 看什么

- `Resources/Engine/Docs/FrameworkAnalysis/Reports/Arch/04-Arch-源码分析报告.md`
- `Resources/Engine/Docs/FrameworkAnalysis/Reports/05-DefaultEcs-源码分析报告.md`
- `Resources/Engine/Docs/FrameworkAnalysis/Reports/flecs/02-Flecs-源码分析报告.md`
- `Resources/Engine/Docs/FrameworkAnalysis/Reports/entt/03-EnTT-源码分析报告.md`

### 学什么

- registry 如何保持小核心。
- query / view / group 如何隔离遍历和结构变更。
- relationship 为什么不能随便变成通用字符串图。
- owner-owned selector / index / map 如何避免全局 query 滥用。
- deferred command / recorder 如何保护结构变更。

### 对 SlimeAI 的落点

```text
TargetSelector / TargetQueryEngine 应由 owner 持有。
LifecycleTree 和业务引用继续分开。
RuntimeCommandBuffer 可以参考 recorder，但只覆盖 SlimeAI 结构变更边界。
```

### 不学什么

- 不开放 pair graph / query DSL 给游戏或 AI。
- 不复制 unsafe chunk storage。
- 不把所有组件系统化成传统 ECS。

## 第六组：Unreal GAS / ET-Framework / IFramework

### 看什么

- `Resources/Engine/Docs/FrameworkAnalysis/Reports/09-Unreal-GAS-文档对照报告.md`
- `Resources/Engine/Docs/FrameworkAnalysis/Reports/ET-Framework/13-ET-Framework-源码分析报告.md`
- `Resources/Engine/Docs/FrameworkAnalysis/Reports/IFramework/14-IFramework-源码分析报告.md`

### 学什么

- GAS：Ability / Effect / Tag / Cue 的职责分离，但不要复制其重量级生态。
- ET：行为机、CancellationToken、数值修饰公式和 watcher。
- IFramework：事件优先级、帧限速、模块生命周期。

### 对 SlimeAI 的落点

```text
Ability / Feature / Damage 分工继续保留。
AI 行为可观察 ET 行为机，但不急于替换行为树。
EventBus 后续可评估低频优先级和帧限速。
```

## 推荐阅读节奏

第一周只读 QFramework / FrameworkDesign：

```text
目标：能用自己的话解释 Model/System/Command/Query/Event 的边界。
产出：给 SlimeAI 写一页“数据进入 Data 的条件”和“状态改变入口规则”。
```

第二周读 Unity Entities / Bevy：

```text
目标：理解 authoring -> runtime、typed storage、Table/SparseSet 取舍。
产出：给 Data Type Contract 写一页“类型在哪里固定”。
```

第三周读 Friflo / Arch / DefaultEcs：

```text
目标：理解 deferred command、query guard、perf observation。
产出：给 RuntimeCommandBuffer 和 Observation 写一页 RFC 草案。
```

第四周回到 SlimeAI：

```text
目标：创建 Data Runtime Simplification SDD。
产出：只改 SlimeAI 需要的机制，不复制外部 API。
```
