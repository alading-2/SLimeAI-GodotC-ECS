# PRJ-0002 ECS Framework Optimization

## Index Card

- **Status**: active
- **Created**: 2026-05-25
- **Updated**: 2026-06-15
- **Scope**: SlimeAI
- **Current SDD**: SDD-0044
- **Tags**: ecs, optimization, data, event, entity, component, relationship, directory-architecture, capability, docsai, tools, timer, objectpool, collision, system

## What This Project Is About

本项目用于重新梳理 `Src/ECS` 旧 ECS 框架的真实问题，并形成“保留旧 ECS 主线、按问题域优化完善”的设计事实源。当前框架仓和 SDD 均位于 `/home/slime/Code/SlimeAI/SlimeAI`；外层 `/home/slime/Code/SlimeAI` 只作为包含游戏仓、Resources 和框架仓的父目录。

当前方向已经纠偏：不再把旧 ECS 作为迁移输入，不再以整体替换或复制外部参考结构为目标。旧框架整体可保留；Data 子系统已按 SDD-0012 至 SDD-0022 完成 descriptor-first / snapshot-first / no-compat / residual contract hardening 收口，并按 SDD-0031 / SDD-0032 完成 runtime generic slot hard cutover 和 typed contract completion：typed `DataKey<T>` 主链路、modifier 和 computed cache 现在使用 `DataSlot<T> + IDataSlot`，业务 Capability 和 AI 可调用 Data 协议不再使用 string key / untyped write / object payload 作为主链路。2026-06-15 用户授权 Data 可重大重构、不保兼容，并确认 SlimeAI 的核心是功能解耦，不是数据形态统一；`design/Runtime/2.Data系统优化/5.Data类型系统重构/09-Data系统根本裁决与重构路线.md` 已冻结新方向：Data 只保留为跨功能共享的 typed runtime state protocol，DataOS 退回 authoring / validator / generator 边界，后续优先拆 `Data Runtime Simplification`、`Data Type Contract`、`Generated RuntimeId Storage` 三个 hard cutover。SDD-0044 的 `DataComputeRegistry` 单例方向仍成立，但不应孤立优先执行，应并入 Data Type Contract 或作为其前置子任务。Data 完成后的 GC/装箱优化已按 SDD-0033 完成非 Data 边界收口：Event dynamic object 主链路删除，Feature / Ability Execute 边界改为 typed payload/result helper，ObjectPoolManager 改 `IObjectPoolRuntime` 非泛型管理接口，TargetSelector 新增 `TargetQueryEngine + TargetQueryResult` ownership / diagnostics facade。2026-06-09 起按 SDD-0040 推进 Log AI-first Observation：T1 结构化记录层已落地，T2 离线语义整理层也已把 `logctl analyze` 默认入口改为 flow conclusion / success template / failure-first digest；但用户运行游戏看到的 live 打印仍然分离，说明 `Src/ECS` 源码调用点语义化未完成。2026-06-11 已新增 `design/Tool/10.Log/第三部分-源码调用点语义化/README.md` 和 SDD-0040 T3：先冻结 live stdout policy、owner flow contract、Debug UI/TestSystem 可见性，再按 owner 迁移 `_log.Info`、测试打印和高频成功路径。当前最终 Godot scene smoke 仍因缺少能验证本框架工作树的承载游戏 runner 阻塞，且未伪造通过。Entity / Relationship 已按 SDD-0024 完成 hard cutover。SDD-0025 已把 ECS 物理目录和 DocsAI 路由重构为 `Runtime + Capabilities + Tools + UI`，同时保留 ECS 语义；`design/Tool/其他Tool/` 已按 2026-06-04 至 2026-06-07 用户复核更新为功能优先 hard cutover：RuntimeMountRegistry、TargetQueryEngine、ResourceLoading、NodeLifecycleRegistry、Common Utilities、MathFormula 后续只保功能，不保旧 API 长期兼容；已确认 `/root/SlimeAIRuntime`、资源 strict fail-fast、Common Utilities 放 `Src/ECS/Tools/CommonUtilities/`、NodeLifecycle 迁 Runtime、TargetSelector 不做兼容桥。SDD-0035 至 SDD-0038 已完成剩余 Tools hard cutover：Runtime mount + NodeLifecycle、TargetQueryEngine、ResourceLoading + CommonUtilities、Math formula + deterministic RNG 均已完成并同步 DocsAI/skill。SDD-0026 已完成 Input Contract 业务语义 facade、调用点迁移和验证闭环。SDD-0027 Timer 重构已完成可执行代码/文档主链路但被当前 BrotatoLike runner/Godot CLI 缺失阻塞在场景验证。SDD-0028 已完成 ObjectPool / Collision ParkedInTree cutover。SDD-0029 已完成 Runtime System manifest / preflight / diagnostics / trace 收口。SDD-0030 已完成 Component 默认组合从 `.tscn` Preset 到 C# profile / composer 的切换，并补齐 Component manifest、DocsAI 和 owner skill 规则。

## Reading Order

1. `design/INDEX.md` — 项目共享设计索引
2. `design/Foundation/06-ECS完全重构执行原则.md` — Data 无兼容复盘后的 hard cutover 项目级原则
3. `design/Runtime/2.Data系统优化/README.md` — Data 完整重构设计包入口
4. `design/Runtime/2.Data系统优化/5.Data类型系统重构/00-README.md` — Data 类型系统重构设计入口
5. `design/Runtime/2.Data系统优化/5.Data类型系统重构/09-Data系统根本裁决与重构路线.md` — 当前 Data 后续 hard cutover 根本裁决
6. `design/Runtime/2.Data系统优化/4.Data验证与Registry简化/01-DataComputeRegistry单例与Catalog验证收敛.md` — SDD-0044 局部方向，后续并入 Data Type Contract
7. `sdds/030-SDD-0044-data-compute-registry-singleton-and-catalog-validation-convergence/README.md` — 已创建但不建议孤立优先执行的 Data registry / catalog 验证收敛 SDD
8. `design/Runtime/3.Entity系统优化/README.md` — Entity 完整重构设计包入口
9. `design/Runtime/6.ECS框架目录架构大重构/README.md` — 当前目录架构重构设计包入口
10. `sdds/021-SDD-0031-data-runtime-generic-slot-hard-cutover/README.md` — Data runtime generic slot hard cutover 已完成执行记录
11. `design/Runtime/ECS框架优化/1.拆箱装箱+GC优化/README.md` — 装箱/GC 设计包入口；Data 与非 Data 明显宽口已由 SDD-0031/0032/0033 完成，后续只从 Logger、TargetQuery pooled lease 或 profiler 证据驱动的 owner 小切片恢复
12. `sdds/023-SDD-0033-non-data-gc-boundary-completion/README.md` — 非 Data GC 边界收口执行记录
13. `sdds/022-SDD-0032-data-runtime-typed-contract-completion/README.md` — Data typed contract completion 执行记录
14. `sdds/015-SDD-0025-ecs-framework-directory-architecture-restructure/README.md` — 目录架构执行型 SDD 胶囊
15. `Core/directory-architecture-restructure-execution-prompt.md` — 目录架构重构总执行提示词
16. `design/Runtime/3.Entity系统优化/06-2026-05-31-DataEventDocsAI同步校准.md` — Data/Event/DocsAI 更新后的 Entity 执行前 override
17. `sdds/014-SDD-0024-entity-relationship-full-rewrite/README.md` — Entity / Relationship hard cutover 已完成执行记录
18. `Core/entity-rewrite-execution-prompt.md` — Entity / Relationship hard cutover 总执行提示词
19. `DocsAI/ECS/Runtime/Entity/README.md` — Entity current 文档入口
20. `design/Runtime/4.SystemAgent目录更改到SlimeAI里面/README.md` — SDD-0023 输入：AI 配置与 SystemAgent 根迁移后的规则同步设计
21. `sdds/013-SDD-0023-systemagent-root-migration-rule-sync/README.md` — SDD-0023 执行记录
22. `sdds/012-SDD-0022-data-projection-diagnostics-contract-hardening/progress.md` — Data residual contract hardening 已完成记录
23. `Core/roadmap.md` — 设计文档到 SDD 的映射、执行顺序、依赖和状态
24. `Core/progress.md` — 项目级关键结论和恢复点
25. `design/Tool/10.Log/README.md` — Log AI-first Observation 设计包入口
26. `design/Tool/10.Log/第二部分-语义提炼整理/03-最终设计与完成清单.md` — Log 离线语义整理层当前契约和完成边界
27. `design/Tool/10.Log/第三部分-源码调用点语义化/README.md` — live 打印仍分离的根因、T3 方向、Must Confirm 和 DoD 草案
28. `sdds/029-SDD-0040-log-ai-first-observation-hard-cutover/README.md` — Log hard cutover 执行型 SDD 胶囊
29. `sdds/029-SDD-0040-log-ai-first-observation-hard-cutover/execution-prompt.md` — SDD-0040 T3 新会话执行提示词
30. `design/Tool/Timer/README.md` — Timer 当前共享设计包入口
31. `sdds/017-SDD-0027-timer-scheduler-full-rewrite/README.md` — Timer 执行型 SDD 胶囊
32. `sdds/017-SDD-0027-timer-scheduler-full-rewrite/execution-prompt.md` — Timer 新会话执行提示词
33. `design/Tool/ObjectPool/README.md` — ObjectPool 当前共享设计包入口
34. `sdds/018-SDD-0028-objectpool-collision-parkedintree-cutover/README.md` — ObjectPool / Collision 执行型 SDD 胶囊
35. `sdds/018-SDD-0028-objectpool-collision-parkedintree-cutover/execution-prompt.md` — ObjectPool / Collision 新会话执行提示词
36. `design/Tool/其他Tool/README.md` — Input/ObjectPool/Timer 已改且 Log 跳过后的剩余 Tools AI-first 设计包入口
37. `design/Tool/其他Tool/01-现状证据与AI-first裁决.md` — 剩余 Tools 总体分析、已确认/未确认问题和默认假设
38. `design/Tool/其他Tool/02-CommonTool与ResourceManagement裁决.md` — Common Utilities、ResourceLoading / ResourceManagement、ResourceGenerator 和 project-filesystem workflow
39. `design/Tool/其他Tool/06-实施路线与验证门禁.md` — 剩余 Tools 后续执行 SDD 拆分、BDD、grep gate 和验证命令
40. `sdds/025-SDD-0035-runtime-mount-and-node-lifecycle-hard-cutover/README.md` — Runtime mount + NodeLifecycle 执行型 SDD 胶囊
41. `sdds/025-SDD-0035-runtime-mount-and-node-lifecycle-hard-cutover/execution-prompt.md` — SDD-0035 新会话执行提示词
42. `sdds/026-SDD-0036-target-query-engine-hard-cutover/README.md` — TargetQueryEngine 执行型 SDD 胶囊
43. `sdds/026-SDD-0036-target-query-engine-hard-cutover/execution-prompt.md` — SDD-0036 新会话执行提示词
44. `sdds/027-SDD-0037-resource-loading-and-common-utilities-hard-cutover/README.md` — ResourceLoading + CommonUtilities 执行型 SDD 胶囊
45. `sdds/027-SDD-0037-resource-loading-and-common-utilities-hard-cutover/execution-prompt.md` — SDD-0037 新会话执行提示词
46. `sdds/028-SDD-0038-math-formula-and-deterministic-random-cutover/README.md` — MathFormula + deterministic RNG 执行型 SDD 胶囊
47. `sdds/028-SDD-0038-math-formula-and-deterministic-random-cutover/execution-prompt.md` — SDD-0038 新会话执行提示词
48. `design/Runtime/7.Component/README.md` — Runtime Component AI-first 优化共享设计包入口
49. `design/Runtime/7.Component/04-Component代码化组合与参数注入裁决.md` — Component Preset 纯代码化和参数注入裁决
50. `sdds/020-SDD-0030-component-code-composition-and-contract-hardening/README.md` — Component code composition 执行型 SDD 胶囊
51. `DocsAI/ECS/Runtime/Component/ComponentManifest.md` — Component current manifest
52. `design/Runtime/8.System优化/README.md` — Runtime System AI-first 优化共享设计包入口
53. `sdds/019-SDD-0029-system-contract-manifest-and-diagnostics-hardening/README.md` — System contract 执行型 SDD 胶囊
54. `sdds/019-SDD-0029-system-contract-manifest-and-diagnostics-hardening/execution-prompt.md` — System contract 新会话执行提示词
55. `sdds/` — 项目内有序 SDD
56. `Core/notes.md` — 参考与开放问题
