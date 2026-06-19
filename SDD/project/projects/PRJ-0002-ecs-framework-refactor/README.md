# PRJ-0002 ECS Framework Optimization

## Index Card

- **Status**: active
- **Created**: 2026-05-25
- **Updated**: 2026-06-19
- **Scope**: SlimeAI
- **Current SDD**: none
- **Tags**: ecs, optimization, data, event, entity, component, relationship, directory-architecture, capability, docsai, tools, timer, objectpool, collision, system

## What This Project Is About

本项目用于重新梳理 `Src/ECS` 旧 ECS 框架的真实问题，并形成“保留旧 ECS 主线、按问题域优化完善”的设计事实源。当前框架仓和 SDD 均位于 `/home/slime/Code/SlimeAI/SlimeAI`；外层 `/home/slime/Code/SlimeAI` 只作为包含游戏仓、Resources 和框架仓的父目录。

当前方向已在 2026-06-16 再次重大调整：用户明确裁决弃用 ECS 作为框架身份，正式框架名定为 `SlimeAIFramework`。`DocsAI/ECS` 和 `Src/ECS` 暂时仍是历史路径名，不代表继续实现 ECS runtime；后续设计使用 `Object / Component / System / Feature / Event / Data` 语义。新方向入口是 `design/Runtime/10.GodotOOP框架方向/README.md`，上游裁决和研究入口是 `design/Runtime/9.ECS框架优化/4.弃用ECS框架/README.md` 与 `DocsAI/思考/框架/ECS框架/`。2026-06-19 已进一步重构 Data 方向：不回退旧 `DataMeta`，继续使用 DataOS descriptor / runtime snapshot / generated `DataKey<T>`；字段定义集中但运行时值按 Data / Profile / Component / System 分区承载；descriptor 后续补 `authority`、`runtimeOwner`、`bindingPolicy`、`writeEntry`、`resetPolicy`；共享状态修改可使用 SlimeAI 风格 typed Command / Request + owner handler / Service Pipeline，不照搬 QFramework `AbstractCommand` 对象体系。此前 2026-06-15 的 Data Runtime Simplification / Type Contract / RuntimeId Storage 路线已被标记为历史问题证据，不再作为默认执行路线；用户已删除 SDD-0044，项目不再把它作为 current_sdd 或 pending 恢复入口；只保留其中 fatal 前 structured report 的思想。历史上 Data 子系统已按 SDD-0012 至 SDD-0022 完成 descriptor-first / snapshot-first / no-compat / residual contract hardening，并按 SDD-0031 / SDD-0032 完成 typed slot 主链路；这些实现记录保留为现状事实，不再证明框架必须继续走 ECS/Data 核心方向。Entity / Relationship、目录架构、Component、System、Tools、Input、ObjectPool、Timer、Log 等历史 SDD 状态仍按各自记录追溯；后续代码迁移需另开 SDD，不在本方向文档阶段直接改源码。

## Reading Order

1. `design/INDEX.md` — 项目共享设计索引
2. `design/Runtime/10.GodotOOP框架方向/README.md` — SlimeAIFramework 当前方向：Object / Component / System / Feature / Event / Data
3. `design/Runtime/10.GodotOOP框架方向/Data/README.md` — Data 名字保留后的字段定义集中、运行时承载分区、authority/projection、DataBinding、Command 修改入口和 DataModifier 方案入口
4. `design/Runtime/10.GodotOOP框架方向/Data/07-OOP中数据定义与运行时管理方案.md` — DataOS descriptor 不回退 DataMeta、RuntimeRecordBinder、Profile/Component/System 分区承载和 Mutation/Observation 主方案
5. `design/Runtime/10.GodotOOP框架方向/Data/08-Command与数据修改入口.md` — Command / Event / Query / owner API / Service Pipeline 的边界和 SlimeAI 采纳方式
6. `design/Runtime/10.GodotOOP框架方向/Data/05-外部方案证据与采纳边界.md` — Godot、Unity Entities、Unreal GAS、QFramework 对 Data 方案的证据和采纳边界
7. `design/Runtime/9.ECS框架优化/4.弃用ECS框架/README.md` — 2026-06-16 上游方向裁决：弃用 ECS
8. `design/Runtime/9.ECS框架优化/4.弃用ECS框架/03-Data系统问题收敛与重写边界.md` — Data 后续问题入口，已校准为不改名
9. `DocsAI/思考/框架/ECS框架/README.md` — 真正 ECS、Godot 适配性和 SlimeAI 概念取舍研究
10. `design/Runtime/2.Data系统优化/5.Data类型系统重构/00-README.md` — 历史 Data 问题证据，已 superseded
11. `design/Runtime/2.Data系统优化/6.架构学习/README.md` — 历史 QFramework / ECS 学习证据，已 superseded
12. `design/Runtime/3.Entity系统优化/README.md` — Entity 完整重构设计包入口
13. `design/Runtime/6.ECS框架目录架构大重构/README.md` — 当前目录架构重构设计包入口
14. `sdds/021-SDD-0031-data-runtime-generic-slot-hard-cutover/README.md` — Data runtime generic slot hard cutover 已完成执行记录
15. `design/Runtime/ECS框架优化/1.拆箱装箱+GC优化/README.md` — 装箱/GC 设计包入口；Data 与非 Data 明显宽口已由 SDD-0031/0032/0033 完成，后续只从 Logger、TargetQuery pooled lease 或 profiler 证据驱动的 owner 小切片恢复
16. `sdds/023-SDD-0033-non-data-gc-boundary-completion/README.md` — 非 Data GC 边界收口执行记录
17. `sdds/022-SDD-0032-data-runtime-typed-contract-completion/README.md` — Data typed contract completion 执行记录
18. `sdds/015-SDD-0025-ecs-framework-directory-architecture-restructure/README.md` — 目录架构执行型 SDD 胶囊
19. `Core/directory-architecture-restructure-execution-prompt.md` — 目录架构重构总执行提示词
20. `design/Runtime/3.Entity系统优化/06-2026-05-31-DataEventDocsAI同步校准.md` — Data/Event/DocsAI 更新后的 Entity 执行前 override
21. `sdds/014-SDD-0024-entity-relationship-full-rewrite/README.md` — Entity / Relationship hard cutover 已完成执行记录
22. `Core/entity-rewrite-execution-prompt.md` — Entity / Relationship hard cutover 总执行提示词
23. `DocsAI/ECS/Runtime/Entity/README.md` — Entity current 文档入口
24. `design/Runtime/4.SystemAgent目录更改到SlimeAI里面/README.md` — SDD-0023 输入：AI 配置与 SystemAgent 根迁移后的规则同步设计
25. `sdds/013-SDD-0023-systemagent-root-migration-rule-sync/README.md` — SDD-0023 执行记录
26. `sdds/012-SDD-0022-data-projection-diagnostics-contract-hardening/progress.md` — Data residual contract hardening 已完成记录
27. `Core/roadmap.md` — 设计文档到 SDD 的映射、执行顺序、依赖和状态
28. `Core/progress.md` — 项目级关键结论和恢复点
29. `design/Tool/10.Log/README.md` — Log AI-first Observation 设计包入口
30. `design/Tool/10.Log/第二部分-语义提炼整理/03-最终设计与完成清单.md` — Log 离线语义整理层当前契约和完成边界
31. `design/Tool/10.Log/第三部分-源码调用点语义化/README.md` — live 打印仍分离的根因、T3 方向、Must Confirm 和 DoD 草案
32. `sdds/029-SDD-0040-log-ai-first-observation-hard-cutover/README.md` — Log hard cutover 执行型 SDD 胶囊
33. `sdds/029-SDD-0040-log-ai-first-observation-hard-cutover/execution-prompt.md` — SDD-0040 T3 新会话执行提示词
34. `design/Tool/Timer/README.md` — Timer 当前共享设计包入口
35. `sdds/017-SDD-0027-timer-scheduler-full-rewrite/README.md` — Timer 执行型 SDD 胶囊
36. `sdds/017-SDD-0027-timer-scheduler-full-rewrite/execution-prompt.md` — Timer 新会话执行提示词
37. `design/Tool/ObjectPool/README.md` — ObjectPool 当前共享设计包入口
38. `sdds/018-SDD-0028-objectpool-collision-parkedintree-cutover/README.md` — ObjectPool / Collision 执行型 SDD 胶囊
39. `sdds/018-SDD-0028-objectpool-collision-parkedintree-cutover/execution-prompt.md` — ObjectPool / Collision 新会话执行提示词
40. `design/Tool/其他Tool/README.md` — Input/ObjectPool/Timer 已改且 Log 跳过后的剩余 Tools AI-first 设计包入口
41. `design/Tool/其他Tool/01-现状证据与AI-first裁决.md` — 剩余 Tools 总体分析、已确认/未确认问题和默认假设
42. `design/Tool/其他Tool/02-CommonTool与ResourceManagement裁决.md` — Common Utilities、ResourceLoading / ResourceManagement、ResourceGenerator 和 project-filesystem workflow
43. `design/Tool/其他Tool/06-实施路线与验证门禁.md` — 剩余 Tools 后续执行 SDD 拆分、BDD、grep gate 和验证命令
44. `sdds/025-SDD-0035-runtime-mount-and-node-lifecycle-hard-cutover/README.md` — Runtime mount + NodeLifecycle 执行型 SDD 胶囊
45. `sdds/025-SDD-0035-runtime-mount-and-node-lifecycle-hard-cutover/execution-prompt.md` — SDD-0035 新会话执行提示词
46. `sdds/026-SDD-0036-target-query-engine-hard-cutover/README.md` — TargetQueryEngine 执行型 SDD 胶囊
47. `sdds/026-SDD-0036-target-query-engine-hard-cutover/execution-prompt.md` — SDD-0036 新会话执行提示词
48. `sdds/027-SDD-0037-resource-loading-and-common-utilities-hard-cutover/README.md` — ResourceLoading + CommonUtilities 执行型 SDD 胶囊
49. `sdds/027-SDD-0037-resource-loading-and-common-utilities-hard-cutover/execution-prompt.md` — SDD-0037 新会话执行提示词
50. `sdds/028-SDD-0038-math-formula-and-deterministic-random-cutover/README.md` — MathFormula + deterministic RNG 执行型 SDD 胶囊
51. `sdds/028-SDD-0038-math-formula-and-deterministic-random-cutover/execution-prompt.md` — SDD-0038 新会话执行提示词
52. `design/Runtime/7.Component/README.md` — Runtime Component AI-first 优化共享设计包入口
53. `design/Runtime/7.Component/04-Component代码化组合与参数注入裁决.md` — Component Preset 纯代码化和参数注入裁决
54. `sdds/020-SDD-0030-component-code-composition-and-contract-hardening/README.md` — Component code composition 执行型 SDD 胶囊
55. `DocsAI/ECS/Runtime/Component/ComponentManifest.md` — Component current manifest
56. `design/Runtime/8.System优化/README.md` — Runtime System AI-first 优化共享设计包入口
57. `sdds/019-SDD-0029-system-contract-manifest-and-diagnostics-hardening/README.md` — System contract 执行型 SDD 胶囊
58. `sdds/019-SDD-0029-system-contract-manifest-and-diagnostics-hardening/execution-prompt.md` — System contract 新会话执行提示词
59. `sdds/` — 项目内有序 SDD
60. `Core/notes.md` — 参考与开放问题
