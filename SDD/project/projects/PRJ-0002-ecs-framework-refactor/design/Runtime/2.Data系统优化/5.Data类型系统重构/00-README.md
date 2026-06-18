# Data 类型系统重构设计入口

> 状态：superseded / historical problem evidence，尚未进入 runtime 实施。
> 日期：2026-06-14
> 更新：2026-06-16，弃用 ECS 方向已覆盖本目录旧路线；本目录保留为 Data 问题证据，不再作为后续 SDD 执行路线入口。
> 原始问题：见同目录 [`0.Prompt.md`](./0.Prompt.md)。
> 范围：`Src/ECS/Runtime/Data/`、`DocsAI/ECS/Runtime/Data/`、`Data/DataOS/`、`Data/DataKey/`、`Workspace/Else/参考/Data/`、`Resources/Engine/Docs`、`Resources/Engine/Engine`。

## Superseded Notice

2026-06-16 用户已裁决 SlimeAI 弃用 ECS 框架方向。Data 后续不再默认执行本目录提出的 `Data Runtime Simplification -> Data Type Contract -> Generated RuntimeId Storage` 路线。

当前入口改为：

- [`../../9.ECS框架优化/4.弃用ECS框架/03-Data系统问题收敛与重写边界.md`](../../9.ECS框架优化/4.弃用ECS框架/03-Data系统问题收敛与重写边界.md)
- [`../../9.ECS框架优化/4.弃用ECS框架/05-后续迁移路线与确认点.md`](../../9.ECS框架优化/4.弃用ECS框架/05-后续迁移路线与确认点.md)

## 一句话结论

用户判断成立：当前 Data 最大问题不是“字典一定错”，而是 **把功能解耦误实现成了数据形态解耦**。DataOS descriptor、snapshot DTO、`DataDefinition`、`DataValueConverter`、`DataRuntimeStorage` 反复承担 `text/object/valueType -> CLR T` 的转换，并把 owner、presentation、policy、modifier、computed、debug 都推到 runtime Data 核心对象里，导致 Data 这个核心协议比业务功能还复杂。

新的根本方向不是继续在当前 Data 上小修，也不是立刻复制传统 ECS chunk/archetype，而是：

```text
功能解耦优先，数据结构直接优先。

Data 只保留为跨功能共享的 typed runtime state protocol。
DataOS 退回 authoring / validator / generator 边界。
Runtime DataDefinition 只保留读写、默认值、modifier、computed 必需字段。
类型检查前移到 validator / generator / catalog build。
业务热路径只走 generated DataKey<T> 和 typed slot。
```

## 文档顺序

1. [`01-需求归纳与真实问题.md`](./01-需求归纳与真实问题.md)
   先把用户问题翻译成实际设计需求，判断哪些担心成立、哪些需要修正。
2. [`02-DataDefinition瘦身与分层方案.md`](./02-DataDefinition瘦身与分层方案.md)
   明确 `DataDefinition` 哪些字段应留在 runtime，哪些应迁到 authoring / presentation / validation artifact。
3. [`03-类型系统与运行时存储重构方案.md`](./03-类型系统与运行时存储重构方案.md)
   说明类型问题的根因、其他框架参考、`DataValueType` / `DataRuntimeStorage` / `DataSlot<T>` 的重构方向。
4. [`04-确认点与后续SDD建议.md`](./04-确认点与后续SDD建议.md)
   列出必须确认的问题、默认假设和建议执行切片。
5. [`05-Data存储结构与AI-first裁决.md`](./05-Data存储结构与AI-first裁决.md)
   给出存储结构最终裁决：`Dictionary<string, IDataSlot>` 只作为短期协议层，中期升级 `runtimeId -> slot array`，长期按 profiler 做 numeric lane。
6. [`06-DataOS字段与Policy决策说明.md`](./06-DataOS字段与Policy决策说明.md)
   用通俗语言解释 `data_key_descriptor` 每类字段和 policy 的用途；裁决 `write_policy` 权限约束降级，数据形态契约保留。
7. [`07-类型转换与生成期检查深化.md`](./07-类型转换与生成期检查深化.md)
   说明类型转换应前移到 DB validator / snapshot generator / catalog build，并提出统一 `DataTypeContract` / `DataValueCodec` 边界。
8. [`08-传统ECS数据存储与SlimeAI对比.md`](./08-传统ECS数据存储与SlimeAI对比.md)
   单独对比传统 ECS、QFramework、字典、数组、chunk 和 SlimeAI Data，说明 AI-first 不绑定某种固定存储形式。
9. [`09-Data系统根本裁决与重构路线.md`](./09-Data系统根本裁决与重构路线.md)
   记录 2026-06-15 用户授权后的根本裁决：功能解耦才是核心，数据解耦不是目标；后续按 Data runtime simplification / type contract / runtimeId storage 拆 hard cutover。
10. [`10-运行时解耦与表格驱动顺序校准.md`](./10-运行时解耦与表格驱动顺序校准.md)
   记录用户补充裁决：DataOS 表格驱动体验有价值，但属于底层 runtime 简化之后的上层能力，不能继续倒灌进 Data runtime core。
11. [`../6.架构学习/README.md`](../6.架构学习/README.md)
   记录 QFramework / FrameworkDesign / Unity Entities / Bevy / Friflo 等成熟框架学习结论：不直接换框架，先吸收边界、类型、authoring/runtime、deferred command 和 observation 机制。

## 当前裁决草案

- 用户已授权重大重构，不保兼容；后续 Data 改动默认 hard cutover，不做长期 adapter。
- SlimeAI 真正要解耦的是 Ability / Feature / Damage / AI / UI / Movement 等功能边界，不是把所有数据都放进一个动态 Data 管线。
- Data 不再是所有状态、配置、展示和权限规则的默认归宿；只有跨功能共享、需要 AI/validator/diagnostic 追溯的 runtime state 才进 Data。
- `RuntimeTypeId` 不应作为所有字段的 runtime 必备字段；只在 `enum`、`object_ref`、`modifier_list` 这类需要 CLR/Godot 类型补充的字段上保留。
- `OwnerDomain` 不应进入 runtime catalog；字段归属可由 `StableKey` 命名、DataOS authoring、DocsAI 和生成报告承载。
- `OwnerCapability` / `OwnerSkill` 对 AI 路由和 DataOS 裁剪有价值，但不应默认进入 `DataDefinition` 热路径对象；建议留在 authoring descriptor / generator manifest。
- `category`、`isPercentage`、`supportsModifiers`、`isComputed`、`options` 是旧 `DataMeta` mirror 残留，应分别被 `uiGroup/unit+format/modifierPolicy/dataKind/allowedValues` 替代，不能继续作为第二事实源进入 runtime snapshot。
- `defaultValue` 可以在知识库/SQLite 中以 text 保存，但进入 runtime snapshot / catalog 时必须按 `valueType` 转成 typed default；`DataSlot<T>` 创建后不应再每次从 object/text 转换。
- `DataValueType` 当前作为 schema 枚举可以保留，但它不应继续成为运行时到处 switch 的转换中心；更合理的是生成期映射到 `DataRuntimeType` / typed descriptor / generated handle。
- `throw` 不应消失。坏 catalog、坏 snapshot、typed API 误用仍要 fail-fast；但 fatal 前应先产出 `DataCatalogBuildReport` / `DataApplyReport` / structured Log，便于 AI 通过日志定位问题。
- `Dictionary<string, IDataSlot>` 短期可保留，但只能作为 stableKey 协议索引；中期应让 generated `DataKey<T>` 携带 `runtimeId`，DataRuntimeStorage 内部优先用数组或 sparse array 查 slot。
- DataOS policy 要分级：`write_policy` 这类权限约束降级为 DocsAI / owner skill / validator report / code review 规则；`data_kind/range/allowed/modifier/computed` 这类数据形态契约保留并尽量前移到生成期检查。runtime `DataDefinition` 不应全量承载 owner、display、legacy mirror 和长期为空的条件字段。
- 类型转换应统一到 `DataTypeContract` / `DataValueCodec` 边界，DB validator、snapshot generator、catalog build 共用同一套逻辑语义；runtime 热路径不再负责主要类型恢复。
- `SDD-0044` 的 `DataComputeRegistry` 单例方向仍成立，但不应孤立优先执行；应并入或依赖后续 Data Type Contract hard cutover。
- 表格驱动是未来上层能力，不是当前 Data runtime 第一目标；先完成底层 Runtime 解耦、Data 进入条件、Type Contract，再做 profile / capability manifest / DataOS 表格组合体验。

## 外部参考报告

- `/home/slime/Code/SlimeAI/Resources/Engine/Docs/FrameworkAnalysis/Reports/QFramework/07-QFramework数据结构与SlimeAIData对比深化.md`
  QFramework 专项分析：它的数据主要由 Model / BindableProperty<T> 强类型字段承担，字典用于注册表和事件表，不承担 SlimeAI 当前 Data 类型恢复职责。
- [`../6.架构学习/README.md`](../6.架构学习/README.md)
  本轮 DeepThink 学习包：确认 QFramework 应作为架构学习第一案例，传统 ECS / C# ECS 作为 runtime 机制对照；SlimeAI 不复制 API，只采纳机制和边界。
- [`../6.架构学习/06-运行时解耦第一原则与框架目标.md`](../6.架构学习/06-运行时解耦第一原则与框架目标.md)
  本轮补充裁决：SlimeAI 首要目标是多游戏功能可组合、可裁剪、可启停；AI-first 和表格驱动都是上层工程能力。

## 非目标

- 不恢复旧 `DataMeta` / `DataRegistry` 作为事实源。
- 不在本设计阶段直接修改 runtime 代码。
- 不追求全仓零 `object`；只要求业务热路径和 AI 可调用协议零 `object`。
- 不在第一阶段复制完整传统 ECS world/query/chunk；若 SDD-A/B/C 后仍证明 Data 是主要瓶颈，再单独评估传统 ECS 分支。
