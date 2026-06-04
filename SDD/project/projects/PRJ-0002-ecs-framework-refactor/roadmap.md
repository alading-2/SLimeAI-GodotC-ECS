# Roadmap

## Purpose

项目级执行路线图，追踪 `design/` 下每份问题分析文档的完成情况和后续 SDD 拆分建议。多份文档可以合并为一个 SDD；Data 核心 runtime 重构已按切片序列完成 descriptor-first / snapshot-first / no-compat / residual contract hardening 主链路。Entity / Relationship 已按 SDD-0024 完成 hard cutover。ECS 目录架构大重构已按 SDD-0025 收口为 `Runtime + Capabilities + Tools + UI`；Input Contract 已按 SDD-0026 完成业务语义 facade 和调用点迁移；Timer Scheduler Full Rewrite 已创建 SDD-0027 并因当前承载游戏 runner/Godot CLI 缺失阻塞在场景验证；ObjectPool / Collision ParkedInTree cutover 已创建 SDD-0028；Runtime System AI-first 优化已按 SDD-0029 完成 manifest / preflight / diagnostics / trace 和 DocsAI 同步。SDD-0030 已完成 Component Preset 代码化 composition profile / composer、typed options、ComponentManifest、DocsAI 和 skill sync。`design/Tool/其他Tool/` 已按 2026-06-04 用户裁决改为功能优先 hard cutover：RuntimeMountRegistry、TargetQueryEngine、ResourceLoading、MathFormula、NodeLifecycleRegistry 后续不为旧 API 长期兼容让路。

## Design Progress

| Design Document | Done | SDD | Notes |
| --- | --- | --- | --- |
| `design/main.md` | — | — | 项目主设计，共享上下文 |
| `design/00-旧ECS框架问题总览.md` | done | — | 已完成方向纠偏：保留旧 ECS，聚焦真实问题 |
| `design/01-Data系统问题分析.md` | done | SDD-0012~SDD-0021 | 兼容入口；完整 Data 设计已迁移到 `design/2.Data系统优化/`，SDD-0021 负责无兼容最终收口 |
| `design/2.Data系统优化/` | done | SDD-0012~SDD-0022 | Data 核心 runtime 已完成 descriptor-first、DataDefinitionCatalog、DataSlot/policy、modifier、compute、snapshot apply、字段迁移、旧路径删除、SDD-0020 snapshot-first usage cutover、SDD-0021 no-compat hard cutover 和 SDD-0022 residual contract hardening |
| `design/2.Data系统优化/04-Data系统现状复查与兼任问题.md` | done | SDD-0020 | SDD-0020 已完成 snapshot-first usage 主链路；其中部分 RuntimeTables / Data fallback 证据已被 06 更新 |
| `design/2.Data系统优化/05-Data重构运行报错根因分析.md` | done | SDD-0021 | `AbilityIcon` 和 `AvailableAnimations` 报错作为 SDD-0021 类型契约红灯输入 |
| `design/2.Data系统优化/06-无兼容完全重构总审计/README.md` | done | SDD-0021 | Data 无兼容总审计已执行：删除兼容 alias、隐式 string、record type 第二事实源、旧 authoring 和过期文档 |
| `design/2.Data系统优化/2.Data无兼容完全重构/03-对比AiFirst框架分析Data重构后剩余框架问题深度复查.md` | done | SDD-0022 | SDD-0022 已完成 residual contract hardening：projection 单一事实源、runtime projection typed key、write diagnostics、object_ref、spawn boundary、catalog freeze、display name query、docs gate |
| `design/2.Data系统优化/2.Data无兼容完全重构/04-BUG:Data无兼容重构后移动与施法失败根因说明.md` | done | SDD-0022 | `unit.player` / `unit.enemy` record completeness 和注册期 `DefaultMoveMode` 已前移到 final snapshot，不恢复 Entity/Pool fallback |
| `design/2.Data系统优化/2.Data无兼容完全重构/05-Data残余问题代码修复分解.md` | done | SDD-0022 | Movement 字段前移、validator、projection、diagnostics、类型契约、spawn、catalog、query 已落地并验证 |
| `design/2.Data系统优化/2.Data无兼容完全重构/06-Data文档更新与门禁清单.md` | done | SDD-0022 | current docs 已更新，旧 Data 路线 grep gate 清零 |
| `design/4.SystemAgent目录更改到SlimeAI里面/README.md` | done | SDD-0023 | `SDD/`、`Workspace/`、`.ai-config/` 迁入 `SlimeAI/` 后的 rules / skill / DocsAI / SDD template 语义收口已完成 |
| `design/3.Entity系统优化/` | done | SDD-0024 | Entity / Relationship hard cutover 已完成；typed EntityId、LifecycleTree、typed references、spawn/destroy pipeline、DamageAttribution 和旧 Relationship runtime 删除已收口 |
| `design/6.ECS框架目录架构大重构/` | done | SDD-0025 | 已完成目录架构收口；裁决 `Src/ECS/Runtime + Src/ECS/Capabilities`，DocsAI 当前入口为 `Runtime + Capabilities + Tools + UI`，不保留 `Foundation/Foundations` 当前路由层 |
| `design/7.Component/` | done | SDD-0030 | Component Code Composition And Contract Hardening 已完成：默认组合事实源迁到 C# profile / composer，Preset 仅 legacy 对照，DocsAI ComponentManifest 和 owner skill 已同步 |
| `design/Tool/Input/` | done | SDD-0026 | Input Contract Manifest And Facade Hardening 已完成：InputManager 业务语义 facade、Ability/Targeting/UI 调用点迁移、DocsAI/skill 同步和验证闭环已收口 |
| `design/Tool/其他Tool/` | proposed | TBD | 2026-06-04 override 已完成：功能优先、代码可丢弃；后续建议按 RuntimeMountRegistry、TargetQueryEngine、ResourceLoading、MathFormula、NodeLifecycleRegistry hard cutover 拆执行型 SDD |
| `design/Tool/Timer/` | pending | SDD-0027 | Timer Scheduler Full Rewrite 已完成可执行代码/文档主链路；当前 blocked 于 TimerStressValidation / scene-gate / BrotatoLike smoke 缺 runner 和 Godot CLI 证据 |
| `design/Tool/ObjectPool/` | pending | SDD-0028 | ObjectPool Collision ParkedInTree Cutover 已创建执行型 SDD；目标为 pool runtime state、parking grid、CollisionLogicGuard、ContactDamage 旧引用清理、ObjectPool contract、Godot collision validation 和 DocsAI/skill sync |
| `design/8.System优化/` | pending | SDD-0029 | Runtime System AI-first Contract Layer 设计包；保留现有 System Core，首切片只做 manifest / preflight / diagnostics / trace / validation artifact，不做 typed SystemId hard cutover |
| `design/13-旧ECS框架Event系统问题分析与优化方向.md` | done | TBD | EventBus 保留，重点优化事件主键、Context、Global 边界和订阅生命周期 |
| `design/03-字符串键名统一问题分析.md` | done | TBD | 跨 Data/Event/Relationship/Resource 的统一命名问题输入 |
| `design/04-优化优先级与SDD拆分建议.md` | done | SDD-0012~SDD-0019 | 已按 Data Full Rewrite 拆成 8 个新切片 |

## Next SDDs

| Priority | Design Docs | Goal |
| --- | --- | --- |
| P0 | `design/2.Data系统优化/` | **SDD-0012**：Catalog TDD 第一切片，建立 descriptor-first DataDefinitionCatalog 和一次性旧定义审计 |
| P0 | `design/2.Data系统优化/` | **SDD-0013**：补齐 DataOS descriptor authoring schema、generator、validator 和 snapshot descriptor 契约 |
| P0 | `design/2.Data系统优化/` | **SDD-0014**：重建 DataSlot、DataValueConverter、write/range/allowed values 和 typed handle 读写模型 |
| P0 | `design/2.Data系统优化/` | **SDD-0015**：已实现 modifier runtime，并让 Feature.Modifiers 作为 authoring_blob 接入 Data policy |
| P0 | `design/2.Data系统优化/` | **SDD-0016**：已实现 DataComputeRegistry、resolver、依赖图、cache 和 transitive dirty |
| P0 | `design/2.Data系统优化/` | **SDD-0017**：已实现 snapshot records ApplyRecord、DataApplyReport 和 Entity/Data bootstrap |
| P0 | `design/2.Data系统优化/` | **SDD-0018**：已按模块迁移 descriptors，生成薄 DataKey typed handle，并迁移调用点 |
| P0 | `design/2.Data系统优化/` | **SDD-0019**：已删除旧 Data/Data、DataNew 和旧 Data 测试场景，重建 Godot Data smoke，更新 Docs/Skill |
| P0 | `design/2.Data系统优化/04-Data系统现状复查与兼任问题.md` | **SDD-0020 已完成**：Data 取用主链路已切到 runtime snapshot / query / projection；但无兼容审计发现类型契约和兼容入口仍未硬收口，进入 SDD-0021 |
| P0 | `design/2.Data系统优化/06-无兼容完全重构总审计/README.md` | **SDD-0021 已完成**：按 no-compat hard cutover 删除 generator/validator/generated handle/Data API/旧 authoring/文档兼容残留，修复 `AbilityIcon` / `AvailableAnimations` 类型回归根因 |
| P0 | `design/2.Data系统优化/2.Data无兼容完全重构/03-*`、`04-*`、`05-*`、`06-*` | **SDD-0022 已完成**：Data Projection Diagnostics Contract Hardening，按 record completeness、projection 单一事实源、diagnostics、object_ref、spawn boundary、catalog freeze、display name query 和 docs gate 收口 |
| P0 | `design/4.SystemAgent目录更改到SlimeAI里面/README.md` | **SDD-0023**：SystemAgent / AI config 根迁移后的 rules、skill、SDD template、DocsAI 和验证门禁语义收口 |
| P0 | `design/3.Entity系统优化/` + `entity-rewrite-execution-prompt.md` | **SDD-0024 已完成**：Entity Relationship Full Rewrite，按 hard cutover 完成 EntityId、LifecycleTree、typed references、spawn/destroy pipeline、DamageAttribution 和旧 Relationship runtime 删除 |
| P0 | `design/6.ECS框架目录架构大重构/` + `directory-architecture-restructure-execution-prompt.md` | **SDD-0025 已完成**：ECS Framework Directory Architecture Restructure，按 `Runtime + Capabilities` 重构 `Src/ECS`，DocsAI current route 为 `Runtime + Capabilities + Tools + UI`，历史概念材料按 owner `Concepts/` 或 Archive/Thinking 收口 |
| P1 | `design/7.Component/` + `sdds/020-SDD-0030-component-code-composition-and-contract-hardening/README.md` | **SDD-0030 已完成**：Component Code Composition And Contract Hardening；已完成 ComponentCompositionProfile / ComponentComposer、Unit / Ability profile、Inspector 导出参数入口移除、manifest、DocsAI/skill sync 和 full validation |
| P1 | `design/Tool/Input/` + `sdds/016-SDD-0026-input-contract-manifest-and-facade-hardening/execution-prompt.md` | **SDD-0026 已完成**：Input Contract Manifest And Facade Hardening，业务语义 facade、调用点迁移、manifest gate 和验证闭环已收口 |
| P0 | `design/Tool/其他Tool/07-2026-06-04-AI-first完全重构校准.md` + `design/Tool/其他Tool/04-NodeLifecycle与ParentManager边界.md` | **建议新建 SDD**：Runtime Mount Registry Hard Cutover；用 manifest 化 mount registry 替代自由字符串 ParentManager，确认 mount scope 后迁移 Entity/ObjectPool/UI/System 调用点，并删除或 internal 化旧 `ParentManager` API |
| P0 | `design/Tool/其他Tool/07-2026-06-04-AI-first完全重构校准.md` + `design/Tool/其他Tool/05-TargetSelector查询契约.md` | **建议新建 SDD**：Target Query Engine Hard Cutover；补 query validation、resolved origin/forward、candidate source、diagnostics、deterministic RNG、safe sorting，迁移 Ability/AI/Feature 调用点 |
| P1 | `design/Tool/其他Tool/02-CommonTool与ResourceManagement裁决.md` | **建议新建 SDD**：Resource Loading Hard Cutover；删除 CommonTool，ResourceManagement strict lookup，LoadPath source policy，structured result 和 ResourceCatalogDiagnostics |
| P1 | `design/Tool/其他Tool/03-Math目标架构与验证.md` | **建议新建 SDD**：Math Formula And Deterministic Random Cutover；拆 MyMath owner，保留 Geometry2D 纯算法，公式归 Capability owner，随机可 seed/RNG 注入 |
| P1 | `design/Tool/Timer/` + `sdds/017-SDD-0027-timer-scheduler-full-rewrite/execution-prompt.md` | **SDD-0027 blocked**：Timer Scheduler Full Rewrite 已等待当前 BrotatoLike runner/Godot CLI，用于补 TimerStressValidation、scene-gate 和 smoke 证据 |
| P1 | `design/Tool/ObjectPool/` + `sdds/018-SDD-0028-objectpool-collision-parkedintree-cutover/execution-prompt.md` | **SDD-0028 pending**：ObjectPool Collision ParkedInTree Cutover，按 `ParkedInTree` 默认迁移、runtime state、CollisionLogicGuard、ContactDamage 清理、ObjectPool contract 和 Godot collision validation 一次性收口 |
| P1 | `design/8.System优化/` + `sdds/019-SDD-0029-system-contract-manifest-and-diagnostics-hardening/execution-prompt.md` | **SDD-0029 pending**：System Contract Manifest And Diagnostics Hardening；先补 SystemManifest、SystemPreflight、SystemDiagnosticsSnapshot、SystemLifecycleTrace、DocsAI Runtime/System 同步和 SystemCore artifact，再视证据决定是否进入 typed SystemId 或 schedule phase |
| P1 | `design/13-旧ECS框架Event系统问题分析与优化方向.md` | Entity hard cutover 或 Data 当前优先级收口后，再处理 Event 定义事实源与主键优化 |
