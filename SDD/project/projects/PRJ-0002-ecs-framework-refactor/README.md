# PRJ-0002 ECS Framework Optimization

## Index Card

- **Status**: active
- **Created**: 2026-05-25
- **Updated**: 2026-06-04
- **Scope**: SlimeAI
- **Current SDD**: none
- **Tags**: ecs, optimization, data, event, entity, component, relationship, directory-architecture, capability, docsai, tools, timer, objectpool, collision, system

## What This Project Is About

本项目用于重新梳理 `Src/ECS` 旧 ECS 框架的真实问题，并形成“保留旧 ECS 主线、按问题域优化完善”的设计事实源。当前框架仓和 SDD 均位于 `/home/slime/Code/SlimeAI/SlimeAI`；外层 `/home/slime/Code/SlimeAI` 只作为包含游戏仓、Resources 和框架仓的父目录。

当前方向已经纠偏：不再把旧 ECS 作为迁移输入，不再以整体替换或复制外部参考结构为目标。旧框架整体可保留；Data 子系统已按 SDD-0012 至 SDD-0022 完成 descriptor-first / snapshot-first / no-compat / residual contract hardening 收口。Entity / Relationship 已按 SDD-0024 完成 hard cutover。SDD-0025 已把 ECS 物理目录和 DocsAI 路由重构为 `Runtime + Capabilities + Tools + UI`，同时保留 ECS 语义；`design/Tool/其他Tool/` 已按 2026-06-04 用户复核更新为功能优先 hard cutover：RuntimeMountRegistry、TargetQueryEngine、ResourceLoading、NodeLifecycleRegistry、Common Utilities、MathFormula 后续只保功能，不保旧 API 长期兼容；已确认 `/root/SlimeAIRuntime` 和资源 strict fail-fast。SDD-0026 已完成 Input Contract 业务语义 facade、调用点迁移和验证闭环。SDD-0027 Timer 重构已完成可执行代码/文档主链路但被当前 BrotatoLike runner/Godot CLI 缺失阻塞在场景验证。SDD-0029 已完成 Runtime System manifest / preflight / diagnostics / trace 收口。SDD-0030 已完成 Component 默认组合从 `.tscn` Preset 到 C# profile / composer 的切换，并补齐 Component manifest、DocsAI 和 owner skill 规则。

## Reading Order

1. `design/INDEX.md` — 项目共享设计索引
2. `design/06-ECS完全重构执行原则.md` — Data 无兼容复盘后的 hard cutover 项目级原则
3. `design/2.Data系统优化/README.md` — Data 完整重构设计包入口
4. `design/3.Entity系统优化/README.md` — Entity 完整重构设计包入口
5. `design/6.ECS框架目录架构大重构/README.md` — 当前目录架构重构设计包入口
6. `sdds/015-SDD-0025-ecs-framework-directory-architecture-restructure/README.md` — 当前执行型 SDD 胶囊
7. `directory-architecture-restructure-execution-prompt.md` — 目录架构重构总执行提示词
8. `design/3.Entity系统优化/06-2026-05-31-DataEventDocsAI同步校准.md` — Data/Event/DocsAI 更新后的 Entity 执行前 override
9. `sdds/014-SDD-0024-entity-relationship-full-rewrite/README.md` — Entity / Relationship hard cutover 已完成执行记录
10. `entity-rewrite-execution-prompt.md` — Entity / Relationship hard cutover 总执行提示词
11. `DocsAI/ECS/Runtime/Entity/README.md` — Entity current 文档入口
12. `design/4.SystemAgent目录更改到SlimeAI里面/README.md` — SDD-0023 输入：AI 配置与 SystemAgent 根迁移后的规则同步设计
13. `sdds/013-SDD-0023-systemagent-root-migration-rule-sync/README.md` — SDD-0023 执行记录
14. `sdds/012-SDD-0022-data-projection-diagnostics-contract-hardening/progress.md` — Data residual contract hardening 已完成记录
15. `roadmap.md` — 设计文档到 SDD 的映射、执行顺序、依赖和状态
16. `progress.md` — 项目级关键结论和恢复点
17. `design/Tool/Timer/README.md` — Timer 当前共享设计包入口
18. `sdds/017-SDD-0027-timer-scheduler-full-rewrite/README.md` — Timer 执行型 SDD 胶囊
19. `sdds/017-SDD-0027-timer-scheduler-full-rewrite/execution-prompt.md` — Timer 新会话执行提示词
20. `design/Tool/ObjectPool/README.md` — ObjectPool 当前共享设计包入口
21. `sdds/018-SDD-0028-objectpool-collision-parkedintree-cutover/README.md` — ObjectPool / Collision 执行型 SDD 胶囊
22. `sdds/018-SDD-0028-objectpool-collision-parkedintree-cutover/execution-prompt.md` — ObjectPool / Collision 新会话执行提示词
23. `design/Tool/其他Tool/README.md` — Input/ObjectPool/Timer 已改且 Log 跳过后的剩余 Tools AI-first 设计包入口
24. `design/Tool/其他Tool/07-2026-06-04-AI-first完全重构校准.md` — 剩余 Tools 功能优先、可 hard cutover、不保旧 API 长期兼容的执行前 override
25. `design/Tool/其他Tool/08-2026-06-04-用户裁决后执行前复核.md` — 用户截图和最新答复后的通俗复核；进入剩余 Tools 实施前优先检查
26. `design/7.Component/README.md` — Runtime Component AI-first 优化共享设计包入口
27. `design/7.Component/04-Component代码化组合与参数注入裁决.md` — Component Preset 纯代码化和参数注入裁决
28. `sdds/020-SDD-0030-component-code-composition-and-contract-hardening/README.md` — Component code composition 执行型 SDD 胶囊
29. `DocsAI/ECS/Runtime/Component/ComponentManifest.md` — Component current manifest
30. `design/8.System优化/README.md` — Runtime System AI-first 优化共享设计包入口
31. `sdds/019-SDD-0029-system-contract-manifest-and-diagnostics-hardening/README.md` — System contract 执行型 SDD 胶囊
32. `sdds/019-SDD-0029-system-contract-manifest-and-diagnostics-hardening/execution-prompt.md` — System contract 新会话执行提示词
33. `sdds/` — 项目内有序 SDD
34. `notes.md` — 参考与开放问题
