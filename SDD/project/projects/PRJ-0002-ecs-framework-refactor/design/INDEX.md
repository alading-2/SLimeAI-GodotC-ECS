# Project Design Index

## Main Design

- `main.md`
- `00-旧ECS框架问题总览.md`

## Documents

| File | Role | Status | Updated | Notes |
| --- | --- | --- | --- | --- |
| `main.md` | main | current | 2026-05-28 | 项目共享设计：保留旧 ECS 主线；Data 子系统按完整重构例外处理 |
| `00-旧ECS框架问题总览.md` | overview | current | 2026-05-26 | 旧 ECS 的真实问题域、非目标和推荐拆分 |
| `06-ECS完全重构执行原则.md` | hard-cutover-principles | current | 2026-05-30 | Data 无兼容复盘后的项目级执行原则；后续 Entity / Relationship / Event hard cutover 前必须先读 |
| `ECS框架优化/0.ECS框架的思考/README.md` | ecs-concept-index | superseded | 2026-06-16 | 历史 ECS 框架概念层思考入口；已被 `Runtime/9.ECS框架优化/4.弃用ECS框架/` 覆盖 |
| `ECS框架优化/0.ECS框架的思考/01-Data作为ECS框架核心的概念复盘与方案批判.md` | ecs-data-core-concept-review | superseded | 2026-06-16 | 历史 Data 作为 ECS 核心复盘；弃用 ECS 后不再作为当前方向 |
| `Runtime/9.ECS框架优化/4.弃用ECS框架/README.md` | ecs-deprecation-direction | current | 2026-06-16 | 上游方向裁决：弃用 ECS 作为框架身份；已由 `Runtime/10.GodotOOP框架方向/` 校准为 SlimeAIFramework 当前方向 |
| `Runtime/9.ECS框架优化/4.弃用ECS框架/03-Data系统问题收敛与重写边界.md` | data-problem-reset | current | 2026-06-16 | 覆盖旧 Data 类型系统和架构学习路线：Data 名字保留，后续按受控共享状态和表格驱动重新设计 |
| `Runtime/10.GodotOOP框架方向/README.md` | slimeai-framework-godot-oop-direction | current | 2026-06-16 | SlimeAIFramework 当前方向入口：Object / Component / System / Feature / Event / Data 概念边界 |
| `Runtime/10.GodotOOP框架方向/Data/source-request.md` | slimeai-framework-data-source-request | current | 2026-06-19 | 本轮 Data 重构用户原始问题与追加确认；正文文档引用本文件避免入口过长 |
| `Runtime/10.GodotOOP框架方向/Data/README.md` | slimeai-framework-data-direction | current | 2026-06-19 | Data 名字保留后的方案入口：定义集中、运行时承载分区、authority/projection、DataBinding、Command 修改入口和 DataModifier |
| `Runtime/10.GodotOOP框架方向/Data/07-OOP中数据定义与运行时管理方案.md` | slimeai-framework-data-oop-runtime-management | current | 2026-06-19 | DataOS descriptor 不回退 DataMeta；RuntimeRecordBinder、Profile/Component/System 分区承载、Mutation/Observation 主方案 |
| `Runtime/10.GodotOOP框架方向/Data/08-Command与数据修改入口.md` | slimeai-framework-data-command-mutation-boundary | current | 2026-06-19 | Command / Event / Query / owner API / Service Pipeline 的语义边界；采用 typed intent，不照搬 QFramework AbstractCommand |
| `Runtime/10.GodotOOP框架方向/Data/09-Data底层执行草案.FeatureSpec.md` | slimeai-framework-data-runtime-core-feature-spec | draft | 2026-06-19 | 执行规格草案：先改框架 Data 底层 authority / owner write / projection / modifier guard，不做数据库到 Data 或 DataOS record 加载 |
| `Runtime/10.GodotOOP框架方向/Data/05-外部方案证据与采纳边界.md` | slimeai-framework-data-external-evidence | current | 2026-06-19 | Godot、Unity Entities、Unreal GAS、QFramework 对 Data / Component / Modifier / Command 方向的证据与采纳边界 |
| `Runtime/10.GodotOOP框架方向/QFramework二次评估/README.md` | qframework-second-pass-kernel-analysis | candidate | 2026-06-19 | 重新评估“愿意改 QFramework 底层时是否直接采用”：推荐 QFramework-inspired SlimeAI-native Kernel，不引入依赖或 `Architecture<T>` 根 |
| `Runtime/10.GodotOOP框架方向/QFramework二次评估/06-SlimeAIKernel执行草案.FeatureSpec.md` | slimeai-kernel-qframework-inspired-feature-spec | draft | 2026-06-19 | 执行草案：CommandDispatcher、Data 当前值订阅、FeatureManifest 和能力接口的最小试验切片 |
| `Runtime/10.GodotOOP框架方向/QFramework二次评估/07-框架根缺失与魔改QFramework可行性.md` | slimeai-framework-root-gap-qframework-prototype | candidate | 2026-06-19 | 回答 SlimeAI 缺框架根的问题：建议深学 QFramework，并用一次性魔改原型训练框架感，最终落成 SlimeAI-native Architecture Contract |
| `Runtime/10.GodotOOP框架方向/QFramework二次评估/08-QFramework优先增量实验路线.md` | qframework-first-incremental-pressure-route | candidate | 2026-06-19 | 回答用户最新路线：先保持 QFramework 清晰原味，再逐步加入 SlimeAI 概念压力源，通过冲突萃取 SlimeAI-native Architecture Contract |
| `Runtime/9.ECS框架优化/4.弃用ECS框架/04-QFramework采纳边界.md` | qframework-new-direction-boundary | current | 2026-06-16 | 重新判断 QFramework：学习少规则、强类型状态、Command/Query/Event，不直接接入依赖或替代 SlimeAI runtime |
| `ECS框架优化/1.拆箱装箱+GC优化/README.md` | gc-optimization-index | current | 2026-06-07 | 装箱拆箱与 GC 优化设计包入口；Data 与非 Data 明显宽口已由 SDD-0031/0032/0033 完成，后续只从 Logger、TargetQuery pooled lease 或 profiler 证据驱动 owner 小切片恢复 |
| `ECS框架优化/1.拆箱装箱+GC优化/设计/README.md` | gc-deepthink-entry | current | 2026-06-07 | DeepThink 确认包；记录 Data 完成后的非 Data 重新分析和 SDD-0033 执行结果，Logger 本轮不改 |
| `ECS框架优化/1.拆箱装箱+GC优化/设计/00-总览与AI-first裁决.md` | gc-overview-decision | current | 2026-06-07 | 裁决 Data 主链路与非 Data 明显宽口均已收口；Event/Feature、ObjectPool manager、TargetQueryResult 基础切片不再重复创建 |
| `ECS框架优化/1.拆箱装箱+GC优化/设计/01-Data运行时object去除设计.md` | gc-data-runtime-generic-slot | current | 2026-06-06 | Data runtime object 去除设计；用户已确认 `DataSlot<T> + IDataSlot` 为最终架构，废弃 `DataRuntimeValue` 多字段 union，改为 typed policy、typed computed resolver 和 untyped 边界 API |
| `ECS框架优化/1.拆箱装箱+GC优化/设计/02-EventBus动态object禁用设计.md` | gc-event-dynamic-object-removal | current | 2026-06-06 | 保留 typed EventBus；Event dynamic object 必须与 Feature / Ability typed boundary 同批收口，不建议只缓存反射 |
| `ECS框架优化/1.拆箱装箱+GC优化/设计/03-FeatureAbility上下文类型化设计.md` | gc-feature-ability-typed-context | current | 2026-06-06 | Feature 只类型化 Execute 输入/输出；Ability CastContext / AbilityExecutedResult 走 typed adapter，lifecycle context 不泛型化 |
| `ECS框架优化/1.拆箱装箱+GC优化/设计/04-ObjectPool反射管理接口设计.md` | gc-objectpool-runtime-interface | current | 2026-06-06 | ObjectPoolManager 反射调用改极小非泛型 runtime interface；P1 小切片，不重写对象池生命周期 |
| `ECS框架优化/1.拆箱装箱+GC优化/设计/05-TargetSelector集合分配与LINQ设计.md` | gc-target-query-allocation | current | 2026-06-06 | TargetQueryEngine / TargetQueryResult ownership 下处理 List、LINQ、Random；不在无 ownership 设计时池化返回 List |
| `ECS框架优化/1.拆箱装箱+GC优化/设计/06-Logger字符串与诊断分配设计.md` | gc-logger-lazy-message | current | 2026-06-06 | Logger 降为 P2/热路径局部处理；不禁字符串插值，用 IsEnabled / lazy / interpolated string handler 解决必要调用点 |
| `4.SystemAgent目录更改到SlimeAI里面/README.md` | systemagent-ai-config-root-migration | current | 2026-05-30 | `SDD/`、`Workspace/`、`.ai-config/` 已迁入 `SlimeAI/` 后的规则、路径和同步语义更新设计 |
| `01-Data系统问题分析.md` | data-analysis-legacy-entry | current | 2026-05-28 | 历史入口；完整 Data 设计已迁移到 `design/Runtime/2.Data系统优化/` |
| `2.Data系统优化/README.md` | data-design-index | current | 2026-05-30 | Data 完全重构设计包入口；descriptor-first、policy 分层、Feature/Compute 边界、旧路径删除和 SDD-0021 无兼容收口 |
| `2.Data系统优化/01-代码实现说明.md` | data-code-explanation | current | 2026-05-28 | DataDefinition 分层、DataDefinitionCatalog、Data.Get/Set、compute resolver、snapshot apply 的目标代码形状 |
| `2.Data系统优化/02-DataMeta属性审计与Feature计算边界.md` | data-meta-feature-boundary | current | 2026-05-28 | 逐项审计 DataMeta 属性，裁决 Feature 不替代 computed，旧 Data 输入路径不保留 |
| `2.Data系统优化/03-完全重构范围与TDD测试计划.md` | data-full-rewrite-tdd | current | 2026-05-28 | 明确删除 `SlimeAI/Data/Data`、`DataNew`，重建 Data 测试场景，并按 TDD 覆盖新 Data 能力 |
| `2.Data系统优化/04-Data系统现状复查与兼任问题.md` | data-compat-audit | historical-input | 2026-05-29 | SDD-0020 输入；部分 RuntimeTables / fallback 证据已被 06 总审计更新 |
| `2.Data系统优化/05-Data重构运行报错根因分析.md` | data-runtime-error-analysis | current | 2026-05-29 | `AbilityIcon` / `AvailableAnimations` 类型回归根因，作为 SDD-0021 输入 |
| `2.Data系统优化/2.Data无兼容完全重构/03-对比AiFirst框架分析Data重构后剩余框架问题深度复查.md` | data-residual-review | current | 2026-05-30 | 当前仍成立的 Data 残余中层契约问题总览；覆盖 generator、projection、object_ref、name lookup、Docs 漂移 |
| `2.Data系统优化/2.Data无兼容完全重构/04-BUG:Data无兼容重构后移动与施法失败根因说明.md` | data-behavior-bug-rootcause | current | 2026-05-30 | 移动与施法失败的端到端根因复盘；聚焦 `DefaultMoveMode`、时序和 completeness contract |
| `2.Data系统优化/2.Data无兼容完全重构/05-Data残余问题代码修复分解.md` | data-residual-fix-plan | current | 2026-05-30 | 当前残余问题的代码修改分解；逐文件说明具体怎么改 |
| `2.Data系统优化/2.Data无兼容完全重构/06-Data文档更新与门禁清单.md` | data-doc-gate-checklist | current | 2026-05-30 | 当前需要同步更新的文档清单和 Data / 文档门禁 |
| `Runtime/2.Data系统优化/4.Data验证与Registry简化/01-DataComputeRegistry单例与Catalog验证收敛.md` | data-registry-catalog-validation-convergence | superseded | 2026-06-16 | 用户已删除 SDD-0044；该页只保留 fatal 前 structured observation / catalog report 思想，不作为执行入口 |
| `Runtime/2.Data系统优化/5.Data类型系统重构/00-README.md` | data-type-system-restructure-index | superseded | 2026-06-16 | 历史 Data 问题证据；已被 `Runtime/9.ECS框架优化/4.弃用ECS框架/03-Data系统问题收敛与重写边界.md` 覆盖，不再作为执行路线入口 |
| `Runtime/2.Data系统优化/5.Data类型系统重构/01-需求归纳与真实问题.md` | data-type-system-problem-analysis | draft | 2026-06-14 | 归纳用户需求：DataDefinition 冗余、默认值恢复类型、DataValueType/Storage 转换复杂、throw/log 边界和统一 Data 容器是否可行 |
| `Runtime/2.Data系统优化/5.Data类型系统重构/02-DataDefinition瘦身与分层方案.md` | data-definition-runtime-slimming | draft | 2026-06-14 | 建议拆分 authoring descriptor、runtime definition 和 presentation descriptor；移出 owner/presentation/旧 mirror 字段，保留 runtime 必要策略 |
| `Runtime/2.Data系统优化/5.Data类型系统重构/03-类型系统与运行时存储重构方案.md` | data-runtime-type-storage-design | draft | 2026-06-14 | 设计 DataValueType 作用域收窄、typed default、slot 类型固定、typed policy、numeric modifier lane 和 generated contract 后续方向 |
| `Runtime/2.Data系统优化/5.Data类型系统重构/04-确认点与后续SDD建议.md` | data-type-system-confirmation-and-sdd | draft | 2026-06-14 | Must Confirm、默认假设与后续 SDD 拆分建议；当前不进入 runtime 实施 |
| `Runtime/2.Data系统优化/5.Data类型系统重构/05-Data存储结构与AI-first裁决.md` | data-storage-ai-first-decision | draft | 2026-06-15 | 裁决 stableKey -> typed slot 只作为短期协议层；中期升级 runtimeId -> slot array，长期按 profiler 做 numeric lane；AI-first 不绑定固定存储形式 |
| `Runtime/2.Data系统优化/5.Data类型系统重构/06-DataOS字段与Policy决策说明.md` | dataos-descriptor-policy-decision | draft | 2026-06-15 | 通俗解释 DataOS descriptor 字段、policy 用途、删留和推荐顺序；`write_policy` 权限约束降级，数据形态契约保留并前移验证 |
| `Runtime/2.Data系统优化/5.Data类型系统重构/07-类型转换与生成期检查深化.md` | data-type-conversion-generation-gate | draft | 2026-06-15 | 设计类型转换前移到 DB validator / snapshot generator / catalog build，并统一 DataTypeContract / DataValueCodec 边界 |
| `Runtime/2.Data系统优化/5.Data类型系统重构/08-传统ECS数据存储与SlimeAI对比.md` | data-traditional-ecs-storage-comparison | draft | 2026-06-15 | 单独对比传统 ECS、QFramework、字典、数组、chunk 与 SlimeAI Data；说明字典不是错但不是高频存储终局 |
| `Runtime/2.Data系统优化/5.Data类型系统重构/09-Data系统根本裁决与重构路线.md` | data-root-decision-restructure-roadmap | superseded | 2026-06-16 | 历史问题证据；旧 runtime simplification / type contract / runtimeId storage 路线已被 `Runtime/10.GodotOOP框架方向/Data/` 覆盖 |
| `Runtime/2.Data系统优化/5.Data类型系统重构/10-运行时解耦与表格驱动顺序校准.md` | data-runtime-decoupling-table-driven-order | draft | 2026-06-15 | 校准 DataOS 表格驱动顺序：填表组合功能是上层体验，必须排在底层 Runtime 解耦、Data 进入条件和 Type Contract 之后 |
| `Runtime/2.Data系统优化/6.架构学习/README.md` | data-architecture-learning-index | superseded | 2026-06-16 | 历史 QFramework / ECS / C# 框架研究证据；已被弃用 ECS 方向覆盖，不再支撑旧 Data hard cutover 路线 |
| `Runtime/2.Data系统优化/6.架构学习/01-问题判断与总体裁决.md` | data-architecture-learning-decision | current | 2026-06-15 | 回答“是否直接用成熟框架”：问题真实存在，建议学习后继续 SlimeAI 自研，不引入外部 ECS runtime 或 QFramework 依赖 |
| `Runtime/2.Data系统优化/6.架构学习/02-QFramework架构学习与采纳边界.md` | qframework-adoption-boundary | current | 2026-06-15 | 专项分析 QFramework / FrameworkDesign；采纳少规则、强类型边界、Command/Query/Event 思路，拒绝 Architecture 静态单例、IController、ICommand 对象层和 BindableProperty 替代 Data |
| `Runtime/2.Data系统优化/6.架构学习/03-成熟ECS与CSharp框架学习路线.md` | data-ecs-learning-roadmap | current | 2026-06-15 | 给出 QFramework、Unity Entities、Bevy、Friflo、Arch、DefaultEcs、Flecs、EnTT、GAS、ET、IFramework 的源码阅读顺序和学习目标 |
| `Runtime/2.Data系统优化/6.架构学习/04-Data系统学习落点与重构建议.md` | data-learning-to-restructure-plan | superseded | 2026-06-16 | 历史学习落点；旧 Data Runtime Simplification / Type Contract / Generated RuntimeId Storage 路线已被 SlimeAIFramework Data 方向覆盖 |
| `Runtime/2.Data系统优化/6.架构学习/05-证据与采纳决策.md` | data-architecture-learning-evidence | current | 2026-06-15 | ResearchAdoption 证据包；记录 Evidence / Inference / Unknown、Adopt Now / Later / Reject 和外部资料边界 |
| `Runtime/2.Data系统优化/6.架构学习/06-运行时解耦第一原则与框架目标.md` | runtime-decoupling-first-principle | current | 2026-06-15 | 回答用户补充裁决：SlimeAI 首要目标是多游戏功能可组合、可裁剪、可启停；Component/System 解耦必须保留，表格驱动和 AI-first 都是上层能力 |
| `3.Entity系统优化/README.md` | entity-design-index | current | 2026-05-31 | Entity 完整重构设计包入口；先读 `1.初级修改/06`，spawn 散点问题读 `2.重构/main.md` |
| `3.Entity系统优化/1.初级修改/00-研究证据与裁决.md` | entity-research-decision | current | 2026-05-31 | 当前代码事实、外部 ECS / 引擎对照、AiFirst 参考采纳、hard cutover 裁决；Data/Event/DocsAI 以 06 覆盖旧假设 |
| `3.Entity系统优化/1.初级修改/01-目标架构与模块拆分.md` | entity-architecture | current | 2026-05-31 | AI-first Entity runtime 目标架构、模块职责、Data projection、typed event 和 Observation 边界 |
| `3.Entity系统优化/1.初级修改/02-代码实现说明.md` | entity-code-shape | current | 2026-05-31 | 目标代码文件、类型、spawn pipeline、registry、component registrar、capability 调用点和 typed event 改法 |
| `3.Entity系统优化/1.初级修改/03-LifecycleTree与业务引用设计.md` | entity-lifecycle-reference | current | 2026-05-31 | 拆解旧 Relationship 语义，定义 LifecycleTree、typed runtime reference、generated Data projection、owner cleanup 和 DamageAttribution |
| `3.Entity系统优化/1.初级修改/04-完全重构范围与TDD测试计划.md` | entity-full-rewrite-tdd | current | 2026-05-31 | 删除清单、TDD 任务序、Godot validation scene、grep gate、typed event 和 BDD 验收 |
| `3.Entity系统优化/1.初级修改/05-源码调用点迁移清单.md` | entity-callsite-migration | current | 2026-05-31 | 基于旧源码 grep 统计并按 2026-05-31 路径/Data/Event/DocsAI 规则校准的迁移清单 |
| `3.Entity系统优化/1.初级修改/06-2026-05-31-DataEventDocsAI同步校准.md` | entity-current-override | current | 2026-05-31 | Data/Event/DocsAI 更新后的 Entity 执行前 override；明确 generated Data projection、typed Event payload、DocsAI 入口和新 grep gate |
| `3.Entity系统优化/2.重构/README.md` | entity-spawn-refactor-index | current | 2026-05-31 | Entity Spawn 统一与业务 facade 重构入口；回答 `EffectTool` 等散点生成是否统一到 EntityManager |
| `3.Entity系统优化/2.重构/main.md` | entity-spawn-refactor-design | current | 2026-05-31 | 裁决统一底层 `EntitySpawnPipeline`，保留薄业务 facade，`EntityManager.Spawn<T>` 只做通用转发 |
| `6.ECS框架目录架构大重构/README.md` | directory-architecture-index | current | 2026-06-01 | ECS 目录架构重构入口；裁决 `Runtime + Capabilities`，DocsAI 同步对齐，并保留 ECS 语义 |
| `6.ECS框架目录架构大重构/01-现状证据与AI-first裁决.md` | directory-architecture-decision | current | 2026-06-01 | 当前技术层分散问题、AiFirst Capability 参考采纳边界和不去 ECS 化裁决 |
| `6.ECS框架目录架构大重构/02-目标目录架构与归属规则.md` | directory-architecture-target-layout | current | 2026-06-01 | `Src/ECS/Runtime`、`Src/ECS/Capabilities`、`DocsAI/ECS/Runtime`、`DocsAI/ECS/Capabilities`、`DocsAI/ECS/Tools`、`DocsAI/ECS/UI` 的归属规则和初始映射 |
| `6.ECS框架目录架构大重构/03-迁移切片与验证门禁.md` | directory-architecture-migration-plan | current | 2026-06-01 | DocsAI 先行、Runtime、Capability、历史概念材料按 owner 收口和最终验证的分阶段执行门禁 |
| `7.Component/README.md` | component-design-index | current | 2026-06-04 | Runtime Component AI-first 优化设计包入口；裁决保留 `IComponent + ComponentRegistrar` 最小契约，补 manifest、lifecycle、subscription、dynamic policy 和验证门禁 |
| `7.Component/01-现状证据与AI-first裁决.md` | component-research-decision | current | 2026-06-04 | 基于 DocsAI、Runtime/Entity 源码、Capability Component、Context7/Web 和 Resources 报告复查 Component；推荐保留 Godot Node adapter 语义，不改纯数据 ECS storage |
| `7.Component/02-目标架构与优化路线.md` | component-architecture-roadmap | current | 2026-06-04 | ComponentManifest、ComponentLifecycleContract、SubscriptionContract、DynamicComponentPolicy、ComponentPreflight 和分阶段实施路线 |
| `7.Component/03-调用点迁移与验证计划.md` | component-migration-test-plan | current | 2026-06-04 | `GetComponent<T>` / `AddComponent` / 外部订阅 / Timer / Godot signal 调用点审计、BDD、验证命令和 grep gate |
| `7.Component/04-Component代码化组合与参数注入裁决.md` | component-code-composition-decision | current | 2026-06-04 | 用户确认 Component 组合完全代码化后的裁决；禁止 `[Export]` / Inspector 默认参数，参数在注册前 typed 注入，`EntityOrientationComponent.Sink` 不进 Data |
| `Tool/ObjectPool/README.md` | object-pool-design-index | current | 2026-06-03 | ObjectPool AI-first 生命周期工具设计包入口；裁决默认 `ParkedInTree` 场外常驻，不脱树、不关碰撞、不改 layer/mask/shape，通过 runtime state guard 与激活首帧 embargo 保证业务碰撞正确 |
| `Tool/ObjectPool/01-现状证据与AI-first裁决.md` | object-pool-research-decision | current | 2026-06-03 | 当前对象池代码仍是旧脱树/关碰撞实现；Godot 时序风险分析保留，但目标裁决改为场外常驻、parking grid、runtime state 和碰撞逻辑验证 |
| `Tool/ObjectPool/02-目标架构与重构路线.md` | object-pool-architecture-roadmap | current | 2026-06-03 | PoolNodeLifecycleStrategy、PoolParkingStrategy、ObjectPoolRuntimeStateStore、CollisionLogicGuard、激活首帧 embargo、fallback detach 对照验证和 scene artifact 门禁 |
| `Tool/ObjectPool/03-碰撞停放与逻辑验证结论草案.md` | object-pool-parked-in-tree-decision | current | 2026-06-03 | 记录 `ParkedInTree + CollisionLogicGuard + ActivationFrameEmbargo` 最终裁决；用户已接受“激活第一帧不处理碰撞”作为默认规则 |
| `8.System优化/README.md` | system-design-index | current | 2026-06-03 | Runtime System AI-first 优化设计包入口；裁决保留现有 System Core，补 manifest、preflight、diagnostics、trace 和验证 artifact |
| `8.System优化/01-现状证据与AI-first裁决.md` | system-research-decision | current | 2026-06-03 | 基于 DocsAI、源码、runtime snapshot、Context7/Web 和 Resources 报告复查 System Core；推荐保留生命周期模型并补 AI-first Contract Layer |
| `8.System优化/02-目标架构与优化路线.md` | system-architecture-roadmap | current | 2026-06-03 | SystemManifest、SystemPreflight、SystemDiagnosticsSnapshot、SystemLifecycleTrace、DocsAI/skill 同步和分阶段实施路线 |
| `8.System优化/03-调用点迁移与验证计划.md` | system-migration-test-plan | current | 2026-06-03 | SystemRegistry / SystemManager.Execute 调用点审计、BDD、构建/DataOS/Godot 场景验证和 grep gate |
| `Tool/Input/README.md` | input-design-index | current | 2026-06-01 | Input AI-first 契约设计包入口；裁决先显式化 action manifest、上下文、typed facade 和验证面，不重写 Godot InputMap |
| `Tool/Input/01-现状证据与AI-first裁决.md` | input-research-decision | current | 2026-06-01 | 当前 InputManager、project.godot、调用点分散、DocsAI 漂移和外部输入框架对照裁决 |
| `Tool/Input/02-目标架构与优化路线.md` | input-architecture-roadmap | current | 2026-06-01 | InputActionId、InputManifest、InputContext、typed facade 和分阶段优化路线 |
| `Tool/Input/03-调用点迁移与验证计划.md` | input-migration-test-plan | current | 2026-06-01 | Gameplay/UI/Debug/Test 输入调用点分层、grep gate、构建测试和 Godot 场景验证计划 |
| `Tool/10.Log/README.md` | log-design-entry | current | 2026-06-11 | Log 工具设计入口；分三部分（记录管道与现状 / 语义提炼整理 / 源码调用点语义化），先读本文确定该看哪部分 |
| `Tool/10.Log/第一部分-记录管道与现状/source-request.md` | log-source-request | current | 2026-06-10 | 本轮用户原始问题摘录、去重提示词和必须回答的问题；正文设计文档引用本文件，避免长提示词淹没入口 |
| `Tool/10.Log/第一部分-记录管道与现状/README.md` | log-part1-index | current | 2026-06-10 | 第一部分入口；记录层 AI-first Observation 设计包；复盘校准为“Logger core / 最小 analyzer 已落地，但整理层验收口径有缺陷” |
| `Tool/10.Log/第一部分-记录管道与现状/01-现状分析与AI-first裁决.md` | log-research-decision | current | 2026-06-10 | 基于当前 `Log.cs` 结构化雏形、样本 `scene-log.jsonl`、`logctl analyze` 产物、runner fallback、ValidationSession 和官方资料的现实检查 |
| `Tool/10.Log/第一部分-记录管道与现状/02-目标架构与数据契约.md` | log-architecture-contract | current | 2026-06-09 | 定义 `LogEntry`、severity/outcome/validationStatus、phase、`OperationTrace`、C# stdout/file sink、optional Godot editor sink、analyzer 输出和字段禁用规则 |
| `Tool/10.Log/第一部分-记录管道与现状/03-控制面与CLI设计.md` | log-control-cli | current | 2026-06-09 | 定义 `Config/Log` profile/rules/overrides、sink 控制、`logctl` 临时覆盖、AI 建议回写、预算和环境变量注入 |
| `Tool/10.Log/第一部分-记录管道与现状/04-测试统一与Observation接入.md` | log-validation-observation | current | 2026-06-08 | 定义 ValidationSession、CheckResult、artifact 主事实源、runner resultSource 和 `GD.PushError` sink 边界 |
| `Tool/10.Log/第一部分-记录管道与现状/05-调用点迁移与验证计划.md` | log-migration-test-plan | current | 2026-06-08 | 定义 Logger core、Validation helper、runner analyzer、业务 flow、owner Log 文档、BDD 和 grep gate |
| `Tool/10.Log/第一部分-记录管道与现状/06-功能OwnerLog文档与分析流程.md` | log-owner-analysis-template | current | 2026-06-10 | 定义每个 owner 的 `Log.md` 模板、flow 聚合规则、analyzer 目录、`ai-context.md` 最小内容和 AI 固定分析流程 |
| `Tool/10.Log/第一部分-记录管道与现状/07-当前样本日志问题与整理方案.md` | log-sample-analysis | current | 2026-06-10 | 基于 `.ai-temp/log-runs/20260610-013907/raw/scene-log.jsonl` 的复盘与实现路线；T2 实现 analyzer digest / flow 边界 / semantic missing-fields / owner 降噪 / Validation gate（验收口径在第二部分被升级） |
| `Tool/10.Log/第二部分-语义提炼整理/README.md` | log-part2-index | current | 2026-06-11 | 第二部分入口；把整理单位从“一条日志”改为“一次操作(flow 树)”，产物以结论为主、行数显著下降、失败一眼可见 |
| `Tool/10.Log/第二部分-语义提炼整理/00-为什么需要第二部分.md` | log-part2-retrospective | current | 2026-06-11 | 复盘：deepthink 做了、需求写了，为什么整理功能仍没落地；根因是把“结果(提炼)”翻译成了“形状(目录)”，验收只查存在性 |
| `Tool/10.Log/第二部分-语义提炼整理/01-语义提炼整理设计.md` | log-semantic-distill-design | current | 2026-06-11 | flow 树模型(correlationId=trace)、结论对象、字段上提、failure-first 呈现、Drain 式模板聚合、时间字段修正、以结果而非存在性做验收 |
| `Tool/10.Log/第二部分-语义提炼整理/02-代码审查与落地修正.md` | log-semantic-distill-code-review | current | 2026-06-11 | 修复前代码审查快照与 G1~G8 gate 清单；当前完成状态以 `03-最终设计与完成清单.md` 为准 |
| `Tool/10.Log/第二部分-语义提炼整理/03-最终设计与完成清单.md` | log-semantic-distill-current-contract | current | 2026-06-11 | 第二部分当前事实源；记录最终 analyzer/query 契约、已完成/未完成清单、样本验证数据和验收命令 |
| `Tool/10.Log/第二部分-语义提炼整理/04-当前实现审查报告.md` | log-semantic-distill-current-review | current | 2026-06-11 | 按 G1~G8 和 SDD-0040 T2.6 / final smoke 审查当前实现，区分 analyzer 已完成与 Validation/Godot 仍未完成 |
| `Tool/10.Log/第三部分-源码调用点语义化/README.md` | log-callsite-semantic-direction | draft | 2026-06-11 | 第三部分入口；确认 live 打印仍分离的根因是 `Src/ECS` 调用点未完成迁移，定义 T3 方向、Must Confirm、迁移路线和 DoD 草案 |
| `Tool/Timer/README.md` | timer-design-index | current | 2026-06-02 | Timer 最终架构裁决入口；保留 TimerManager facade，内部改纯 C# scheduler，补 debug diagnostics 和压力场景门禁 |
| `Tool/Timer/01-现状证据与AI-first裁决.md` | timer-research-decision | current | 2026-06-02 | 当前 TimerManager/GameTimer 热路径、Debug/压力验证缺口、外部计时器资料对照和采纳/拒绝裁决 |
| `Tool/Timer/02-目标架构与优化路线.md` | timer-architecture-roadmap | current | 2026-06-02 | 纯 C# TimerScheduler、min-heap、handle、owner/purpose、clock、主线程派发、debug diagnostics 和 timing wheel 后续触发条件 |
| `Tool/Timer/03-调用点迁移与验证计划.md` | timer-migration-test-plan | current | 2026-06-02 | Timer 调用点 owner/purpose 迁移、debug JSON、benchmark、TimerStressValidation 场景、scene-gate 和最终验证门禁 |
| `Tool/其他Tool/README.md` | other-tools-design-index | current | 2026-06-07 | Input/ObjectPool/Timer 已有设计且 Log 跳过后的剩余 Tools 设计包入口；current 文档收敛为总览、功能说明和实施验证，不再按确认时间追加文档。 |
| `Tool/其他Tool/01-现状证据与AI-first裁决.md` | other-tools-research-decision | current | 2026-06-07 | 剩余 Tools 总体分析；集中记录已确认决策、仍未确认问题、默认假设和 RuntimeMountRegistry、TargetQueryEngine、ResourceLoading、NodeLifecycleRegistry、Common Utilities、MathFormula hard cutover 总路线。 |
| `Tool/其他Tool/02-CommonTool与ResourceManagement裁决.md` | common-resource-loading-decision | current | 2026-06-07 | 功能说明：`CommonTool.LoadPackedScene` 迁入 ResourceLoading；Common Utilities 固定放 Tools；ResourceManagement 简化为 loading facade + generated catalog + diagnostics；project-filesystem workflow 负责路径移动后的替换和残留检查。 |
| `Tool/其他Tool/03-Math目标架构与验证.md` | math-tool-architecture-validation | current | 2026-06-04 | 裁决 Math 功能保留但不保旧 MyMath 聚合类；纯几何留 Geometry2D，业务公式拆 owner，概率/采样支持 deterministic RNG。 |
| `Tool/其他Tool/04-NodeLifecycle与ParentManager边界.md` | node-lifecycle-parent-manager-boundary | current | 2026-06-04 | 裁决 ParentManager 功能升级为 RuntimeMountRegistry / SceneMountRegistry；NodeLifecycle 只保底层 registry，业务查询 hard cutover 到 typed facade。 |
| `Tool/其他Tool/05-TargetSelector查询契约.md` | target-selector-query-contract | current | 2026-06-04 | 裁决 TargetSelector 升级为 TargetQueryEngine / TargetQueryResult；补 query validation、resolved origin/forward、candidate source、deterministic RNG、safe sorting 和 diagnostics。 |
| `Tool/其他Tool/06-实施路线与验证门禁.md` | other-tools-roadmap-validation | current | 2026-06-07 | 给出 RuntimeMountRegistry、TargetQueryEngine、ResourceLoading、NodeLifecycleRegistry、Common Utilities、MathFormula hard cutover SDD 拆分、BDD、grep gate、project-filesystem 和验证命令。 |
| `13-旧ECS框架Event系统问题分析与优化方向.md` | event-analysis | current | 2026-05-26 | Event 字符串主键、GameEventType、EventContext、GlobalEventBus 和订阅生命周期问题 |
| `03-字符串键名统一问题分析.md` | cross-cutting-analysis | current | 2026-05-26 | Data/Event/Relationship/Resource 中字符串变量名不统一的共性问题 |
| `04-优化优先级与SDD拆分建议.md` | roadmap-input | current | 2026-05-28 | 后续按问题域拆 SDD；Data 第一切片改为 Full Rewrite Catalog TDD |
| `05-DocsAI集中式ECS文档目录方案.md` | docs-layout-decision | current | 2026-05-30 | v2：DocsAI/ 统一文档目录方案；覆盖目录结构、迁移映射、AI 路由、分阶段执行计划 |
| `14-Event调用方式类型安全优化-执行提示词.md` | execution-prompt | current | 2026-05-26 | 删 const string、payload 做主键的执行步骤和代码变换示例 |
