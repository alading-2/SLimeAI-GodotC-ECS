# QFramework 之后的 C# 源码学习顺序

> 原始问题：见 [`source-request.md`](./source-request.md) 的“2026-06-15 QFramework 之后看什么”。  
> 本文目标：回答“只会 C#，QFramework 比较短，读完之后应该看什么、怎么看、学什么”。  
> 外部核对：Context7 `/friflo/friflo.engine.ecs`、`/genaray/arch.docs`；GitHub 搜索结果 `friflo/Friflo.Engine.ECS`、`genaray/Arch`、`Doraku/DefaultEcs`、`LeoECSCommunity/ecslite`；`copiedCodeOrAssets: none`。

## 结论

用户判断成立：SlimeAI 用 ECS 不是因为必须信奉某个 ECS 教条，而是因为 `Component` 和 `System` 这两个概念天然适合表达功能解耦。

因此学习顺序不应该是“再找一个更大的框架照搬”，而应该按 SlimeAI 当前缺的底层能力排序：

```text
QFramework
  -> 学少规则、分层、Command/Query/Event、共享数据边界

Friflo.Engine.ECS
  -> 学 C# runtime 如何做结构变更边界、System 组织和性能观察

Arch
  -> 学极小 ECS core、archetype/chunk、query/storage 的实现取舍

DefaultEcs
  -> 学 accessible API、World/Entity/EntitySet、事件/索引如何保持简单

Bevy / Unity Entities 文档
  -> 学 plugin/schedule/run condition 和 authoring -> runtime 分层，不主读源码
```

当前不建议继续优先看大型 Unity 应用框架、完整商业框架或能力系统。因为 SlimeAI 现在最缺的不是更多上层概念，而是底层 runtime 变简单、边界变硬、规则变少。

## 先把 QFramework 读到什么程度

QFramework 不需要读到 Toolkits 全部细节。第一轮只读到能回答下面问题即可：

- 为什么 `Model / System / Utility / Command / Query / Event` 能降低协作混乱？
- 哪些状态应该是共享状态，哪些状态应该留在 owner 内部？
- 为什么改变状态要有统一入口，而不是任何地方直接改？
- 为什么事件适合“下层通知上层”，但不适合替代所有运行时协议？
- 为什么 `Dictionary<Type, object>` 可以作为注册表，却不应该成为业务字段类型系统？

建议阅读范围：

```text
Resources/Engine/Engine/QFramework/QFramework.cs
Resources/Engine/Engine/QFramework/QFramework API.md
Resources/Engine/Docs/FrameworkAnalysis/Reports/QFramework/
fanqie404/FrameworkDesign 的 README 和最小示例
```

读完后给 SlimeAI 只产出两张规则表：

- `Data 进入条件`：哪些字段允许进入 Entity.Data。
- `状态改变入口`：哪些写操作走 service/request/runtime command，哪些只留 owner 内部。

不要从 QFramework 复制 `Architecture<T>`、`IController`、`ICommand` 对象层或全局事件系统。

## 第二个看 Friflo.Engine.ECS

Friflo 是 QFramework 后最值得看的 C# 源码。原因不是它“最强”，而是它正好补 SlimeAI 当前缺的 runtime 经验：

- 纯托管 C#，不用 unsafe 也能做高性能 ECS。
- 有 `SystemRoot / SystemGroup / BaseSystem`，能看系统如何组织和执行。
- 有 `CommandBuffer.Playback`，能看结构变更如何延迟和分阶段执行。
- 有 SystemPerf 监控，能看系统执行时间、分配、entity count、执行次数如何成为一等观察字段。
- 文档和源码都相对可读，适合 C# 学习。

建议阅读顺序：

```text
README.md
src/ECS/Systems/SystemRoot.cs
src/ECS/Systems/SystemGroup.cs
src/ECS/Systems/BaseSystem.cs
src/ECS/CommandBuffer/CommandBuffer.cs
src/ECS/CommandBuffer/Playback.cs
src/ECS/EntityStore.cs
src/ECS/Entity.cs
src/ECS/Query/
src/ECS/Archetype/
```

只学四件事：

1. `System` 怎么被组织、启用、禁用、更新和观察。
2. 为什么遍历中结构变更必须走 `CommandBuffer`。
3. `Playback` 为什么要有固定阶段，而不是命令来了立刻执行。
4. 性能和 blocked/skip reason 为什么应该变成 artifact，而不是靠临时 log。

对 SlimeAI 的落点：

```text
RuntimeCommandBuffer
SchedulePhase
EntityManager mutation guard
SystemDiagnosticsSnapshot
SystemPerf / Observation artifact
```

不要学：

- 不引入 Friflo 依赖。
- 不复制它的完整 archetype runtime。
- 不复制大量泛型 query API。
- 不因为性能焦虑提前 unsafe 或多线程化。

## 第三个看 Arch

Arch 适合在 Friflo 之后看。它的价值是“极小核心 + archetype/chunk + 高性能 query”，但源码和性能取舍更偏 data-oriented，直接上来读容易被性能细节带偏。

建议阅读顺序：

```text
README / docs
Arch.Core World / Entity
Archetype / Chunk / Signature
QueryDescription / Query
CommandBuffer
Arch.System / SourceGenerator 示例
```

只学三件事：

- ECS core 能小到什么程度。
- component 类型、signature、chunk、query 是怎么配合的。
- command buffer 和 query 为什么要隔离结构变更。

对 SlimeAI 的落点：

```text
Data runtime 不要一开始就追求完整 ECS storage。
先让 DataKey<T> / DataSlot<T> / runtimeId 把类型和索引固定。
真正热点出现后，再考虑 typed lane / numeric lane / sparse lane。
```

不要学：

- 不开放通用 world query DSL 给 AI 或游戏侧。
- 不把 SlimeAI Entity.Data 改成传统 ECS component array。
- 不复制 source generator API 形态。

## 第四个看 DefaultEcs

DefaultEcs 的学习价值不是“最新”或“性能最强”，而是它比较容易读，API 目标是低约束、易用和性能平衡。它适合用来提醒 SlimeAI：框架 API 如果不容易用，底层再强也会被绕开。

建议阅读：

```text
World
Entity
EntitySet / EntitySetBuilder
World.Subscribe / Publish 或事件相关实现
EntityMap / EntityMultiMap
```

只学三件事：

- API 怎么保持可理解。
- 查询集合如何缓存，而不是每次全局扫描。
- 索引和事件如何服务 owner，而不是变成全局万能查询。

对 SlimeAI 的落点：

```text
TargetSelector / capability-owned selector 可以有索引。
但索引应该属于 owner，不能变成公开 world query 万能入口。
```

## 第五个只看概念：Bevy 和 Unity Entities

这两个很重要，但不建议作为 C# 源码主线。

Bevy 值得学：

- plugin / plugin group。
- schedule。
- `run_if` 运行条件。
- command 延迟执行。
- `Table / SparseSet` storage 取舍。

Unity Entities 值得学：

- authoring component。
- Baker。
- runtime `IComponentData`。
- authoring 和 runtime 的硬分层。

对 SlimeAI 的落点：

```text
DataOS validator / generator = SlimeAI 的 Baker。
runtime_snapshot = 运行时输入，不是 authoring DB。
Capability/Profile manifest = SlimeAI 的 plugin/profile 组合边界。
System enable/disable = 调度层事实，不是每个系统内部散落 if。
```

不要学：

- 不复制 Rust / Unity API。
- 不把 Godot 编辑器强行改成 Unity Baker 形态。
- 不把 Bevy 的 ECS 世界模型完整搬到 SlimeAI。

## 暂时不要优先看的东西

这些不是没价值，而是不适合当前阶段优先看：

| 项 | 暂缓原因 |
| --- | --- |
| Unreal GAS | 上层 Ability/Effect/Tag 很重，适合以后重看 Ability/Feature，不适合当前 Data/runtime 底层。 |
| ET-Framework | 工程体系和网络/actor/热更更重，容易把当前问题带偏。 |
| LeoECS Lite | 可以学轻量，但偏 Unity ECS 编码风格；等 Friflo/Arch/DefaultEcs 看完再补。 |
| Flecs / EnTT 源码 | C/C++ 机制强，关系/pair/query 很有价值，但当前只读文档和报告即可。 |
| 更多 MVC/MVVM 框架 | 会继续强化上层组织，而不是解决 Runtime 解耦。 |

## 每读一个框架只写一页

不要写长篇读书笔记。每个框架只写一页，固定回答：

```text
1. 它解决的核心问题是什么？
2. 它最小核心是哪几个类型？
3. 它如何处理共享状态？
4. 它如何处理结构变更？
5. 它如何处理系统启停 / 调度？
6. 它如何观察错误、性能和跳过原因？
7. SlimeAI Adopt Now / Adopt Later / Reject 分别是什么？
```

如果某个框架不能回答 `结构变更`、`系统启停`、`观察` 这三个问题，它就不是当前阶段优先源码。

## 给 SlimeAI 的学习路线

推荐实际节奏：

```text
阶段 1：QFramework，1-2 天
目标：写清 Data 进入条件和状态改变入口。

阶段 2：Friflo，3-5 天
目标：写 RuntimeCommandBuffer / SchedulePhase / SystemPerf 草案。

阶段 3：Arch，2-3 天
目标：写 Data runtimeId / typed storage 的长期方向，不立即实施完整 chunk。

阶段 4：DefaultEcs，1-2 天
目标：检查 SlimeAI API 是否能做到低约束、可理解、可测试。

阶段 5：回到 SlimeAI
目标：先做 Data Runtime Simplification，不继续扩 DataOS 表格体验。
```

一句话：

```text
QFramework 学规则。
Friflo 学 runtime 边界。
Arch 学 storage/query。
DefaultEcs 学易用 API。
Bevy / Unity Entities 学概念，不抄源码。
```

这条路线比继续找“一个完整成熟框架替换 SlimeAI”更适合当前目标，因为 SlimeAI 不是要变成某个 ECS 克隆，而是要把底层 runtime 做成简单、可组合、可裁剪、可启停、可验证的 Godot C# 多游戏框架。
