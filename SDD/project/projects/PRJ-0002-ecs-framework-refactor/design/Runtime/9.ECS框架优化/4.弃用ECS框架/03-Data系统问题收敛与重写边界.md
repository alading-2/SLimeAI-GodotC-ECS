# Data 系统问题收敛与重写边界

> 本文整合 `Runtime/2.Data系统优化/5.Data类型系统重构/` 与 `Runtime/2.Data系统优化/6.架构学习/` 的问题结论。  
> 状态：problem statement only；不冻结实现方案。

## 结论

Data 系统需要重写，但现在不能继续沿旧路线设计。旧路线仍默认：

```text
Data 是 ECS 核心数据结构。
DataOS descriptor 是 runtime field 事实源。
Runtime DataDefinition 负责类型、policy、modifier、computed、presentation、owner。
后续再瘦身、类型契约化、runtimeId array。
```

弃用 ECS 后，这个前提失效。

新问题应该写成：

```text
SlimeAI 需要一个共享状态机制，但不需要一个统一承载所有状态的 ECS Data runtime。
```

## 旧 Data 文档留下的有效问题

`5.Data类型系统重构` 和 `6.架构学习` 中仍有效的问题是：

- DataDefinition 太重，混入 authoring、runtime、presentation、policy、AI routing。
- 类型信息在 DataOS、snapshot、generated key、catalog、slot、converter 中重复出现。
- `object` / text / runtime type 恢复路径太多，导致类型系统难以稳定。
- `write_policy` 等组织纪律被塞进 runtime，收益低、复杂度高。
- 字典可以做索引，但不应承担类型系统。
- QFramework 证明强类型 Model/Property 能避免大量类型恢复问题。
- 成熟 ECS 证明真正数据结构应是 storage/query/schedule，不是万能 Data dictionary。
- DataOS authoring 和 validator 有价值，但不能倒灌成 runtime 大而全对象。

这些问题仍然成立。

## 旧 Data 文档需要废弃的路线

以下路线不再作为当前推荐：

- `Data Runtime Simplification -> Data Type Contract -> Generated RuntimeId Storage` 作为默认后续 SDD。
- 继续把 `DataKey<T>` / `DataSlot<T>` 当成框架核心协议。
- 继续让 DataOS descriptor 决定大部分 runtime state。
- 在旧 Data runtime 上继续小修 `DataComputeRegistry`、catalog report、runtime id 等。
- 以“传统 ECS 数据结构”作为 Data 重写的隐藏目标。

这些不是一定永远不能做，而是必须先经过新 Data 设计重新确认。

## 新 Data 问题定义

Data 重写前必须先回答：

```text
哪些状态必须共享？
谁读？
谁写？
为什么不能留在 Component / Feature / System 内部？
是否需要 DataOS authoring？
是否需要 AI/validator/log/test 追踪？
是否需要持久化？
```

只有答案明确的字段才进入 Data。

## Data 的候选范围

应该进入 Data 的候选：

- 被多个 Feature 读取或写入的游戏状态。
- 需要表格配置初始化的对象状态。
- 需要出现在 debug panel / log / validation 的状态。
- 需要 save/load 或回放验证的状态。
- 需要统一 modifier/computed 的少数字段。

不应默认进入 Data：

- Component 私有状态。
- System 内部缓存。
- 查询索引。
- UI 表现状态。
- 动画、特效、音效播放状态。
- 一帧内临时值。
- 只被一个 Feature 使用的配置。

## 可能的新方案，但尚未冻结

### 方案 A：Component-local state + 少量 Data

默认状态存在 Component 或 Feature/System 中。Data 只承载跨功能字段、表格驱动字段和可观察字段。

优点：

- 最贴合 Godot/OOP。
- Data 重写范围最小。
- 功能开发直接。

风险：

- 需要明确共享字段进入条件，否则可能回到乱放字段。
- AI 查状态需要 owner 文档和 manifest 支持。

### 方案 B：QFramework 式 Model / Property 层

每个 Feature 有自己的 Model，Model 内是 C# 强类型字段或 observable property。

优点：

- 类型简单。
- 读写清楚。
- 适合 UI binding。

风险：

- 容易引入全局 Architecture/Model 思维。
- per-object 状态和多实例对象需要额外设计。
- 不能直接替代 Godot Node 状态。

### 方案 C：保留精简 Data Runtime

保留 generated key / typed slot / DataOS authoring，但大幅缩小字段范围和 runtime 职责。

优点：

- 保留现有 DataOS/validator/AI 路由资产。
- 对跨功能共享字段仍有统一验证。

风险：

- 容易滑回旧 Data 大而全。
- 需要非常严格的进入条件和字段清单。

## 推荐默认方向

下一步不是立刻写代码，而是创建新的设计包：

```text
Runtime/10.GodotOOP框架方向/
  01-Object与Component模型.md
  03-Feature与System边界.md
  Data/README.md
  04-事件驱动与生命周期规则.md
```

Data 重写作为其中一个子设计，不再单独从旧 `2.Data系统优化` 继续推进。

## 对 SDD-0044 的处理

`SDD-0044 DataComputeRegistry 单例与 Catalog 验证收敛` 不应继续执行。它基于旧 Data 仍作为核心 runtime 的前提。

建议：

```text
项目入口不再引用该 SDD。
保留其中“fatal 前需要结构化 report”的思想。
不再执行 registry singleton 小修。
```

## 文档整合建议

`5.Data类型系统重构` 与 `6.架构学习` 不建议继续扩展。它们应作为历史问题证据保留，入口 README 标记：

```text
Superseded by 9.ECS框架优化/4.弃用ECS框架/03-Data系统问题收敛与重写边界.md
```

后续新文档只描述问题和新边界，不把旧文档逐篇搬运。
