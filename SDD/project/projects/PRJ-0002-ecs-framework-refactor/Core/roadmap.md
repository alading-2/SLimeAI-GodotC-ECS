# Roadmap

## Purpose

项目级执行路线图，追踪 `design/` 下每份问题分析文档的完成情况和后续 SDD 拆分建议。多份文档可以合并为一个 SDD；Data 核心 runtime 重构已按切片序列完成 descriptor-first / snapshot-first / no-compat / residual contract hardening 主链路，并按 SDD-0031 / SDD-0032 完成 2026-06-06 GC/装箱复查后的 generic slot hard cutover 与 typed contract completion：typed `DataKey<T>` 主链路、modifier effective value 和 computed cache 已使用 `DataSlot<T> + IDataSlot`，业务 Capability 和 AI 可调用 Data 协议不再使用 string key / untyped write / object payload 作为主链路。Data 完成后的 GC/装箱优化已按 SDD-0033 完成非 Data 明显宽口收口：Event dynamic object 主链路删除，Feature / Ability Execute 边界改 typed payload/result helper，ObjectPoolManager 改 `IObjectPoolRuntime` 非泛型管理接口，TargetSelector 新增 `TargetQueryEngine + TargetQueryResult` ownership / diagnostics facade。2026-06-09 起按 SDD-0040 推进 Log AI-first Observation：T1 结构化记录层已落地，T2 离线语义整理层已把 `logctl analyze` 默认入口改为 flow conclusion、success template 和 failure-first digest；但用户运行游戏时 live 打印仍然分离，说明 `Src/ECS` 源码调用点语义化未完成。2026-06-11 已新增第三部分设计和 T3 follow-up，先冻结 live stdout policy、owner flow contract、Debug UI/TestSystem 可见性，再按 owner 迁移 `_log.Info`、测试打印和高频成功路径。Entity / Relationship 已按 SDD-0024 完成 hard cutover。ECS 目录架构大重构已按 SDD-0025 收口为 `Runtime + Capabilities + Tools + UI`；Input Contract 已按 SDD-0026 完成业务语义 facade 和调用点迁移；Timer Scheduler Full Rewrite 已创建 SDD-0027 并因当前承载游戏 runner/Godot CLI 缺失阻塞在场景验证；ObjectPool / Collision ParkedInTree cutover 已按 SDD-0028 完成；Runtime System AI-first 优化已按 SDD-0029 完成 manifest / preflight / diagnostics / trace 和 DocsAI 同步。SDD-0030 已完成 Component Preset 代码化 composition profile / composer、typed options、ComponentManifest、DocsAI 和 skill sync。`design/Tool/其他Tool/` 已按 2026-06-04 用户复核改为功能优先 hard cutover：RuntimeMountRegistry、TargetQueryEngine、ResourceLoading、NodeLifecycleRegistry、Common Utilities、MathFormula 后续不为旧 API 长期兼容让路；已确认 `/root/SlimeAIRuntime`、资源 strict fail-fast、Common Utilities 放 `Src/ECS/Tools/CommonUtilities/`、NodeLifecycle 迁 Runtime、TargetSelector 不做兼容桥。2026-06-07 用户再次校准 ResourceManagement：`res://` 本身不是问题；保留的是极薄 `ResourceLoading` 统一加载工具，不保留 ResourceManagement 作为大而泛的“资源管理器”概念；路径移动、目录增删改查和旧路径残留检查应交给 project directory / `project-filesystem` skill、ResourceGenerator 和 diagnostics，未来框架/游戏仓分离后游戏资源 catalog 由游戏仓拥有。

## Design Progress

| Design Document | Done | SDD | Notes |
| --- | --- | --- | --- |
| `design/main.md` | — | — | 项目主设计，共享上下文 |
| `design/Foundation/00-旧ECS框架问题总览.md` | done | — | 已完成方向纠偏：保留旧 ECS，聚焦真实问题 |
| `design/Runtime/ECS框架优化/1.拆箱装箱+GC优化/` | done | SDD-0031~SDD-0033 | Data generic slot、Data typed contract 和非 Data 明显宽口均已完成；Event dynamic object、Feature / Ability typed Execute、ObjectPool manager interface、TargetQueryResult ownership 已收口；Logger / pooled lease /局部 cleanup 保留为 profiler 或 owner 证据驱动后续 |
| `design/Foundation/01-Data系统问题分析.md` | done | SDD-0012~SDD-0021 | 兼容入口；完整 Data 设计已迁移到 `design/Runtime/2.Data系统优化/`，SDD-0021 负责无兼容最终收口 |
| `design/Runtime/2.Data系统优化/` | done | SDD-0012~SDD-0022 | Data 核心 runtime 已完成 descriptor-first、DataDefinitionCatalog、DataSlot/policy、modifier、compute、snapshot apply、字段迁移、旧路径删除、SDD-0020 snapshot-first usage cutover、SDD-0021 no-compat hard cutover 和 SDD-0022 residual contract hardening |
| `design/Runtime/2.Data系统优化/04-Data系统现状复查与兼任问题.md` | done | SDD-0020 | SDD-0020 已完成 snapshot-first usage 主链路；其中部分 RuntimeTables / Data fallback 证据已被 06 更新 |
| `design/Runtime/2.Data系统优化/05-Data重构运行报错根因分析.md` | done | SDD-0021 | `AbilityIcon` 和 `AvailableAnimations` 报错作为 SDD-0021 类型契约红灯输入 |
| `design/Runtime/2.Data系统优化/06-无兼容完全重构总审计/README.md` | done | SDD-0021 | Data 无兼容总审计已执行：删除兼容 alias、隐式 string、record type 第二事实源、旧 authoring 和过期文档 |
| `design/Runtime/2.Data系统优化/2.Data无兼容完全重构/03-对比AiFirst框架分析Data重构后剩余框架问题深度复查.md` | done | SDD-0022 | SDD-0022 已完成 residual contract hardening：projection 单一事实源、runtime projection typed key、write diagnostics、object_ref、spawn boundary、catalog freeze、display name query、docs gate |
| `design/Runtime/2.Data系统优化/2.Data无兼容完全重构/04-BUG:Data无兼容重构后移动与施法失败根因说明.md` | done | SDD-0022 | `unit.player` / `unit.enemy` record completeness 和注册期 `DefaultMoveMode` 已前移到 final snapshot，不恢复 Entity/Pool fallback |
| `design/Runtime/2.Data系统优化/2.Data无兼容完全重构/05-Data残余问题代码修复分解.md` | done | SDD-0022 | Movement 字段前移、validator、projection、diagnostics、类型契约、spawn、catalog、query 已落地并验证 |
| `design/Runtime/2.Data系统优化/2.Data无兼容完全重构/06-Data文档更新与门禁清单.md` | done | SDD-0022 | current docs 已更新，旧 Data 路线 grep gate 清零 |
| `design/Runtime/4.SystemAgent目录更改到SlimeAI里面/README.md` | done | SDD-0023 | `SDD/`、`Workspace/`、`.ai-config/` 迁入 `SlimeAI/` 后的 rules / skill / DocsAI / SDD template 语义收口已完成 |
| `design/Runtime/3.Entity系统优化/` | done | SDD-0024 | Entity / Relationship hard cutover 已完成；typed EntityId、LifecycleTree、typed references、spawn/destroy pipeline、DamageAttribution 和旧 Relationship runtime 删除已收口 |
| `design/Runtime/6.ECS框架目录架构大重构/` | done | SDD-0025 | 已完成目录架构收口；裁决 `Src/ECS/Runtime + Src/ECS/Capabilities`，DocsAI 当前入口为 `Runtime + Capabilities + Tools + UI`，不保留 `Foundation/Foundations` 当前路由层 |
| `design/Runtime/7.Component/` | done | SDD-0030 | Component Code Composition And Contract Hardening 已完成：默认组合事实源迁到 C# profile / composer，Preset 仅 legacy 对照，DocsAI ComponentManifest 和 owner skill 已同步 |
| `design/Tool/Input/` | done | SDD-0026 | Input Contract Manifest And Facade Hardening 已完成：InputManager 业务语义 facade、Ability/Targeting/UI 调用点迁移、DocsAI/skill 同步和验证闭环已收口 |
| `design/Tool/10.Log/` | blocked | SDD-0040 | T1 结构化 Logger 与 T2 离线语义 analyzer 默认入口已落地；T3 源码调用点语义化未完成，当前需先冻结 live stdout policy 与 owner flow contract；Godot scene smoke blocked 于当前无有效承载游戏 runner |
| `design/Tool/其他Tool/` | done | SDD-0035~SDD-0038 | 2026-06-07 consolidated：current 事实源收敛为 `README.md` + `01~06`；已创建 4 个执行型 SDD：Runtime mount + NodeLifecycle、TargetQueryEngine、ResourceLoading + CommonUtilities、MathFormula + deterministic RNG；全部为 hard cutover，不保旧 API 长期兼容 |
| `design/Tool/Timer/` | pending | SDD-0027 | Timer Scheduler Full Rewrite 已完成可执行代码/文档主链路；当前 blocked 于 TimerStressValidation / scene-gate / BrotatoLike smoke 缺 runner 和 Godot CLI 证据 |
| `design/Tool/ObjectPool/` | done | SDD-0028 | ObjectPool Collision ParkedInTree Cutover 已完成；SDD-0033 另行完成 ObjectPoolManager `IObjectPoolRuntime` 去反射小切片 |
| `design/Runtime/8.System优化/` | done | SDD-0029 | Runtime System manifest / preflight / diagnostics / trace 和 DocsAI Runtime/System 同步已完成 |
| `design/Runtime/13-旧ECS框架Event系统问题分析与优化方向.md` | done | TBD | EventBus 保留，重点优化事件主键、Context、Global 边界和订阅生命周期 |
| `design/Foundation/03-字符串键名统一问题分析.md` | done | TBD | 跨 Data/Event/Relationship/Resource 的统一命名问题输入 |
| `design/Foundation/04-优化优先级与SDD拆分建议.md` | done | SDD-0012~SDD-0019 | 已按 Data Full Rewrite 拆成 8 个新切片 |

## Next SDDs

| Priority | Design Docs | Goal |
| --- | --- | --- |
| Done | `design/Runtime/ECS框架优化/1.拆箱装箱+GC优化/设计/01-Data运行时object去除设计.md` | **SDD-0031 已完成**：Data Runtime Generic Slot Hard Cutover；该设计页保留为历史输入和执行记录来源，不再作为当前待办 |
| Done | `design/Runtime/ECS框架优化/1.拆箱装箱+GC优化/设计/01-Data运行时object去除设计.md` | **SDD-0032 已完成**：Data Runtime Typed Contract Completion；业务 Data 协议不再以 string/untyped/object 作为主链路，debug / loader / diagnostic 边界保留命名和 grep gate |
| Done | `design/Runtime/ECS框架优化/1.拆箱装箱+GC优化/设计/02-EventBus动态object禁用设计.md` + `03-FeatureAbility上下文类型化设计.md` | **SDD-0033 已完成**：Event + Feature/Ability Typed Execution Boundary；删除 `EmitDynamic` / `OnDynamic` / `Action<object>` 主链路，Feature event action 改 typed wrapper，Feature Execute 改 typed helper |
| Done | `design/Runtime/ECS框架优化/1.拆箱装箱+GC优化/设计/04-ObjectPool反射管理接口设计.md` | **SDD-0033 已完成**：`ObjectPoolManager` 改极小非泛型 runtime interface，删除 manager 反射调用；未重写对象池生命周期 |
| Done | `design/Runtime/ECS框架优化/1.拆箱装箱+GC优化/设计/05-TargetSelector集合分配与LINQ设计.md` | **SDD-0033 已完成基础切片**：已引入 `TargetQueryEngine + TargetQueryResult` ownership / diagnostics；pooled lease、deterministic RNG 和 allocation artifact 后续另起 TargetSelector owner SDD |
| P2 | `design/Runtime/ECS框架优化/1.拆箱装箱+GC优化/设计/06-Logger字符串与诊断分配设计.md` | 后续 Logger 热路径局部 SDD：不禁字符串插值，只对每帧热路径补 `IsEnabled` / lazy message / interpolated string handler |
| P0 | `design/Runtime/2.Data系统优化/` | **SDD-0012**：Catalog TDD 第一切片，建立 descriptor-first DataDefinitionCatalog 和一次性旧定义审计 |
| P0 | `design/Runtime/2.Data系统优化/` | **SDD-0013**：补齐 DataOS descriptor authoring schema、generator、validator 和 snapshot descriptor 契约 |
| P0 | `design/Runtime/2.Data系统优化/` | **SDD-0014**：重建 DataSlot、DataValueConverter、write/range/allowed values 和 typed handle 读写模型 |
| P0 | `design/Runtime/2.Data系统优化/` | **SDD-0015**：已实现 modifier runtime，并让 Feature.Modifiers 作为 authoring_blob 接入 Data policy |
| P0 | `design/Runtime/2.Data系统优化/` | **SDD-0016**：已实现 DataComputeRegistry、resolver、依赖图、cache 和 transitive dirty |
| P0 | `design/Runtime/2.Data系统优化/` | **SDD-0017**：已实现 snapshot records ApplyRecord、DataApplyReport 和 Entity/Data bootstrap |
| P0 | `design/Runtime/2.Data系统优化/` | **SDD-0018**：已按模块迁移 descriptors，生成薄 DataKey typed handle，并迁移调用点 |
| P0 | `design/Runtime/2.Data系统优化/` | **SDD-0019**：已删除旧 Data/Data、DataNew 和旧 Data 测试场景，重建 Godot Data smoke，更新 Docs/Skill |
| P0 | `design/Runtime/2.Data系统优化/04-Data系统现状复查与兼任问题.md` | **SDD-0020 已完成**：Data 取用主链路已切到 runtime snapshot / query / projection；但无兼容审计发现类型契约和兼容入口仍未硬收口，进入 SDD-0021 |
| P0 | `design/Runtime/2.Data系统优化/06-无兼容完全重构总审计/README.md` | **SDD-0021 已完成**：按 no-compat hard cutover 删除 generator/validator/generated handle/Data API/旧 authoring/文档兼容残留，修复 `AbilityIcon` / `AvailableAnimations` 类型回归根因 |
| P0 | `design/Runtime/2.Data系统优化/2.Data无兼容完全重构/03-*`、`04-*`、`05-*`、`06-*` | **SDD-0022 已完成**：Data Projection Diagnostics Contract Hardening，按 record completeness、projection 单一事实源、diagnostics、object_ref、spawn boundary、catalog freeze、display name query 和 docs gate 收口 |
| P0 | `design/Runtime/4.SystemAgent目录更改到SlimeAI里面/README.md` | **SDD-0023**：SystemAgent / AI config 根迁移后的 rules、skill、SDD template、DocsAI 和验证门禁语义收口 |
| P0 | `design/Runtime/3.Entity系统优化/` + `Core/entity-rewrite-execution-prompt.md` | **SDD-0024 已完成**：Entity Relationship Full Rewrite，按 hard cutover 完成 EntityId、LifecycleTree、typed references、spawn/destroy pipeline、DamageAttribution 和旧 Relationship runtime 删除 |
| P0 | `design/Runtime/6.ECS框架目录架构大重构/` + `Core/directory-architecture-restructure-execution-prompt.md` | **SDD-0025 已完成**：ECS Framework Directory Architecture Restructure，按 `Runtime + Capabilities` 重构 `Src/ECS`，DocsAI current route 为 `Runtime + Capabilities + Tools + UI`，历史概念材料按 owner `Concepts/` 或 Archive/Thinking 收口 |
| P1 | `design/Runtime/7.Component/` + `sdds/020-SDD-0030-component-code-composition-and-contract-hardening/README.md` | **SDD-0030 已完成**：Component Code Composition And Contract Hardening；已完成 ComponentCompositionProfile / ComponentComposer、Unit / Ability profile、Inspector 导出参数入口移除、manifest、DocsAI/skill sync 和 full validation |
| P1 | `design/Tool/Input/` + `sdds/016-SDD-0026-input-contract-manifest-and-facade-hardening/execution-prompt.md` | **SDD-0026 已完成**：Input Contract Manifest And Facade Hardening，业务语义 facade、调用点迁移、manifest gate 和验证闭环已收口 |
| P0 | `design/Tool/10.Log/README.md` + `design/Tool/10.Log/第三部分-源码调用点语义化/README.md` | **SDD-0040 blocked + T3 follow-up**：结构化 `LogEntry`、sink、`OperationTrace`、ValidationSession 和 analyzer 默认语义入口已完成；后续必须先冻结源码调用点语义化方向，再按 owner 迁移 live 可见 `_log.Info`、测试打印、Debug UI 输出和高频成功路径；Godot scene smoke blocked 于当前无有效承载游戏 runner |
| Done | `design/Tool/其他Tool/01-现状证据与AI-first裁决.md` + `design/Tool/其他Tool/04-NodeLifecycle与ParentManager边界.md` + `design/Tool/其他Tool/06-实施路线与验证门禁.md` + `sdds/025-SDD-0035-runtime-mount-and-node-lifecycle-hard-cutover/execution-prompt.md` | **SDD-0035 已完成**：Runtime Mount And NodeLifecycle hard cutover；`RuntimeMountService` / `RuntimeMountRegistry` 取代自由字符串 ParentManager，默认 `/root/SlimeAIRuntime`，Entity/ObjectPool/UI/System 调用点已迁移，NodeLifecycle 迁 Runtime registry 并提供 owner/source diagnostics |
| Done | `design/Tool/其他Tool/01-现状证据与AI-first裁决.md` + `design/Tool/其他Tool/05-TargetSelector查询契约.md` + `design/Tool/其他Tool/06-实施路线与验证门禁.md` + `sdds/026-SDD-0036-target-query-engine-hard-cutover/execution-prompt.md` | **SDD-0036 已完成**：Target Query Engine Hard Cutover；`TargetQueryEngine` / `TargetQueryResult` 成为 current API，query diagnostics、candidate source、resolved origin/forward、deterministic RNG 和 Ability/Data Feature 调用点已收口，旧 list-only facade 删除 |
| Done | `design/Tool/其他Tool/01-现状证据与AI-first裁决.md` + `design/Tool/其他Tool/02-CommonTool与ResourceManagement裁决.md` + `design/Tool/其他Tool/06-实施路线与验证门禁.md` + `sdds/027-SDD-0037-resource-loading-and-common-utilities-hard-cutover/execution-prompt.md` | **SDD-0037 已完成**：Resource Loading And Common Utilities Hard Cutover；`ResourceLoading` 成为 current public 心智，strict lookup、source/owner/usage、structured diagnostics、ResourceCatalogDiagnostics 和 CommonUtilities owner 边界已收口 |
| Done | `design/Tool/其他Tool/03-Math目标架构与验证.md` + `sdds/028-SDD-0038-math-formula-and-deterministic-random-cutover/execution-prompt.md` | **SDD-0038 已完成**：Math Formula And Deterministic Random Cutover；删除 `MyMath` / `GeometryCalculator` 旧入口，新增 `ProbabilityTool` / `DeterministicRandom`，Damage/Ability 公式归 owner，随机可 seed/RNG 注入 |
| P1 | `design/Tool/Timer/` + `sdds/017-SDD-0027-timer-scheduler-full-rewrite/execution-prompt.md` | **SDD-0027 blocked**：Timer Scheduler Full Rewrite 已等待当前 BrotatoLike runner/Godot CLI，用于补 TimerStressValidation、scene-gate 和 smoke 证据 |
| Done | `design/Tool/ObjectPool/` + `sdds/018-SDD-0028-objectpool-collision-parkedintree-cutover/execution-prompt.md` | **SDD-0028 done**：ObjectPool Collision ParkedInTree Cutover 已完成；后续对象池改动按 ObjectPool owner 新建小切片 |
| Done | `design/Runtime/8.System优化/` + `sdds/019-SDD-0029-system-contract-manifest-and-diagnostics-hardening/execution-prompt.md` | **SDD-0029 done**：System Contract Manifest And Diagnostics Hardening 已完成；typed SystemId 或 schedule phase 需新证据再开 SDD |
| P1 | `design/Runtime/13-旧ECS框架Event系统问题分析与优化方向.md` | Entity hard cutover 或 Data 当前优先级收口后，再处理 Event 定义事实源与主键优化 |
