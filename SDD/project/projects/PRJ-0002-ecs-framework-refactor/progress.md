# Project Progress

## Purpose

本文件是 `PRJ-0002` 的项目级进度事实源，用于记录项目状态、设计覆盖、阶段结论、验证证据和下一步。项目级设计资料放在 `design/`；子 SDD 执行细节放在各自 `sdds/<order>-SDD-xxxx/progress.md`。

## Latest Resume

- **Updated**: 2026-06-07
- **Current SDD**: none
- **Last Conclusion**: SDD-0033 Non-Data GC Boundary Completion 已完成：Event dynamic object 主链路删除，Feature / Ability Execute 边界改 typed payload/result helper，ObjectPoolManager 改 `IObjectPoolRuntime` 非泛型管理接口，TargetSelector 新增 `TargetQueryEngine + TargetQueryResult` ownership / diagnostics facade；Logger 本轮不改。
- **Next Action**: 若继续 GC/装箱优化，只从 Logger 热路径 lazy / interpolated string handler、TargetQuery pooled lease / deterministic RNG / allocation artifact、AbilityInventory / ComponentRegistrar 局部 cleanup 等 profiler 或 owner 证据驱动小切片恢复；不要重复创建 Event dynamic / Feature Execute / ObjectPool manager / TargetQueryResult 基础切片。
- **Open Blockers**: none

## Project Status Board

| SDD | Status | Design Docs | Current Result |
| --- | --- | --- | --- |
| SDD-0011 | done | `design/2.Data系统优化/` historical | 已完成 C#-first DataKey/SnapshotLoader 编译修复；已被 2026-05-28 descriptor-first 完整重构裁决取代为历史基线 |
| SDD-0012 | done | `design/2.Data系统优化/` | 已完成 Catalog TDD、DataDefinitionCatalog、BuildCatalog、DataComputeRegistry 和 LegacyDataAuditReport；旧 Data 运行时路径未接入新 catalog |
| SDD-0013 | done | `design/2.Data系统优化/` | 已完成 DataOS descriptor-first schema、generator/validator、snapshot descriptor 契约、capability trimming、record consistency 和最小 fixture |
| SDD-0014 | done | `design/2.Data系统优化/` | 已完成 DataSlot、DataValueConverter、descriptor default、write/range/allowed values、typed handle、catalog-bound Data runtime 和 changed event bridge |
| SDD-0015 | done | `design/2.Data系统优化/` | 已完成 modifier runtime、source rollback、Feature.Modifiers authoring_blob bridge 和 Feature modifier 授予/回滚边界 |
| SDD-0016 | done | `design/2.Data系统优化/` | 已完成 DataComputeRegistry / IDataComputeResolver、依赖图、cache、transitive dirty、computed readonly 和基础 resolver 示例 |
| SDD-0017 | done | `design/2.Data系统优化/` | 已完成 RuntimeDataSnapshot DTO、LoadFromJson、DataApplyReport、ApplyRecord、DataRuntimeBootstrap 和显式 Entity bootstrap 分支 |
| SDD-0018 | done | `design/2.Data系统优化/` | 已完成业务字段 descriptor 迁移、generated typed DataKey thin handle、运行时/业务调用点迁移 |
| SDD-0019 | done | `design/2.Data系统优化/` | 已完成旧 Data/Data、DataNew、旧 Data 测试场景删除，重建 DataOS Godot smoke，Docs/Skill sync |
| SDD-0020 | done | `design/2.Data系统优化/04-Data系统现状复查与兼任问题.md` | 已完成 Data snapshot-first usage 主链路：取用点切到 runtime snapshot / query / projection；但 06 审计发现类型契约和兼容入口仍未硬收口 |
| SDD-0021 | done | `design/2.Data系统优化/06-无兼容完全重构总审计/README.md` | 已完成 Data no-compat hard cutover：record type 来自 descriptor，validator 检查最终 snapshot，非标量 generated handle typed 化，DataKey 隐式 string 和 alias 删除，业务调用点、RuntimeModels、旧 Resource authoring 和文档事实源收口 |
| SDD-0022 | done | `design/2.Data系统优化/2.Data无兼容完全重构/03-*`、`04-*`、`05-*`、`06-*` | 已完成 Data residual contract hardening：record completeness、Movement/Ability 行为启动契约、projection 单一事实源、write diagnostics、object_ref 语义、spawn boundary、catalog freeze、display name query 和 docs gate |
| SDD-0023 | done | `design/4.SystemAgent目录更改到SlimeAI里面/README.md` | `SDD/`、`Workspace/`、`.ai-config` 迁入 `SlimeAI/` 后的 rules、skill、DocsAI、SDD template 和验证门禁语义收口已完成 |
| SDD-0024 | done | `design/3.Entity系统优化/` | Entity Relationship Full Rewrite 已完成：typed EntityId、LifecycleTree、typed references、spawn/destroy pipeline、DamageAttribution 和旧 Relationship runtime 删除已收口 |
| SDD-0025 | done | `design/6.ECS框架目录架构大重构/` | 已完成：`Src/ECS/Runtime + Src/ECS/Capabilities` 成为源码主入口；DocsAI 当前入口为 `Runtime + Capabilities + Tools + UI`；`Foundation/Foundations` 已从当前路由删除 |
| SDD-0026 | done | `design/Tool/Input/` | Input Contract Manifest And Facade Hardening 已完成；Input DocsAI 主入口改为 README，Concept/Usage/InputMap 降为可选辅助分层 |
| TBD | proposed | `design/Tool/其他Tool/` | 2026-06-04 user review 已完成：剩余 Tools 后续按 RuntimeMountRegistry、TargetQueryEngine、ResourceLoading、NodeLifecycleRegistry、Common Utilities、MathFormula 功能切片 hard cutover；已确认 `/root/SlimeAIRuntime` 和资源 strict fail-fast，剩余确认点见 `08-*` |
| SDD-0027 | blocked | `design/Tool/Timer/` | Timer scheduler core、TimerManager adapter、owner/purpose callsite migration、diagnostics、benchmark、TimerStressValidation 文件、DocsAI Timer 文档和 tools skill 同步已完成；当前 blocked 于缺 current BrotatoLike runner/Godot CLI，无法产出 scene artifact / scene-gate / smoke 证据 |
| SDD-0028 | pending | `design/Tool/ObjectPool/` | ObjectPool Collision ParkedInTree Cutover 已创建执行胶囊；等待按提示词执行 runtime state、parking grid、CollisionLogicGuard、ContactDamage stale attacker cleanup、contract tests、Godot collision validation 和 DocsAI/skill sync |
| SDD-0029 | done | `design/8.System优化/` | Runtime System manifest / preflight / diagnostics / trace 和 DocsAI Runtime/System 同步已完成 |
| SDD-0030 | done | `design/7.Component/` | Component Code Composition And Contract Hardening 已完成：默认组件组合迁到 C# profile / composer，Entity root scene 停止 instance Component Preset，ComponentManifest / DocsAI / ecs-component skill 已同步 |
| SDD-0031 | done | `design/ECS框架优化/1.拆箱装箱+GC优化/` | Data Runtime Generic Slot Hard Cutover 已完成；非 Data 部分已重新裁决为 Event + Feature/Ability typed execution boundary 同批收口，ObjectPool / TargetSelector / Logger 降为 P1/P2 独立切片 |
| SDD-0032 | done | `design/ECS框架优化/1.拆箱装箱+GC优化/` | Data Runtime Typed Contract Completion 已完成；业务 Data 协议不再以 string/untyped/object 作为主链路，debug / loader / diagnostic 边界保留命名和 grep gate |
| SDD-0033 | done | `design/ECS框架优化/1.拆箱装箱+GC优化/` | Non-Data GC Boundary Completion 已完成；Event dynamic object、Feature / Ability raw object Execute、ObjectPool manager 反射、TargetSelector list-only ownership 已收口；Logger 仍为 P2 / profiler 驱动 |
| TBD | proposed | `design/13-旧ECS框架Event系统问题分析与优化方向.md` | P1：保留 EventBus，优化事件主键、事件定义和请求-响应边界 |

## Timeline

### P001 — 2026-05-25 18:25 — project-created

- **Context**: 创建项目级 SDD 容器。
- **Conclusion**: 已建立项目级设计、路线图、进度和子 SDD 目录。
- **Evidence**: README、project.json、design、roadmap、progress、notes、sdds 已生成。
- **Impact**: 后续子 SDD 可共享项目级设计。
- **Resume**: 从 README 的 Reading Order 继续。

### P005 — 2026-05-26 18:06 — direction-reset

- **Context**: 用户明确说明 `SlimeAI/Src` ECS 框架已完全回退，当前目标是重写 `design/` 文档，只提炼旧 ECS 真实问题。
- **Conclusion**: 项目方向重置为“旧 ECS 优化完善”。新的设计包按 Data、Event、Entity/Relationship、字符串键名统一组织。
- **Evidence**: `design/INDEX.md`、`design/main.md`、`design/00-旧ECS框架问题总览.md`、`design/01-Data系统问题分析.md`、`design/13-旧ECS框架Event系统问题分析与优化方向.md`、`design/3.Entity系统优化/00-研究证据与裁决.md`、`design/03-字符串键名统一问题分析.md`、`design/04-优化优先级与SDD拆分建议.md`。
- **Impact**: 后续不再恢复旧执行路线；需要重新创建优化型 SDD。
- **Resume**: 从 `design/04-优化优先级与SDD拆分建议.md` 开始，优先创建 Data SDD。

### P006 — 2026-05-27 12:15 — data-design-refactor

- **Context**: 用户要求将 `design/01-Data系统问题分析.md` 重构到 `design/2.Data系统优化/`，并补充问题、解决方案和代码说明。
- **Conclusion**: Data 设计结论从“强化 C# DataKey”升级为“DataOS snapshot descriptor 单一字段定义事实源”。
- **Evidence**: `design/2.Data系统优化/README.md`、`01-代码实现说明.md`、`02-DataMeta属性审计与Feature计算边界.md`、兼容入口 `design/01-Data系统问题分析.md`。
- **Impact**: 后续 Data 执行型 SDD 应围绕 descriptor-first 与审计报告，而不是继续新增手写 DataKey。
- **Resume**: 从 `design/2.Data系统优化/` 创建执行型 SDD。

### P007 — 2026-05-28 19:28 — data-full-rewrite-sdd-split

- **Context**: 用户要求详读所有 Data 重构文档，并按文档要求拆分成一个或多个 SDD。
- **Conclusion**: 已将 Data 完整重构拆成 8 个顺序切片：Catalog TDD、DataOS schema、Data runtime policy、Modifier/Feature bridge、Compute runtime、Snapshot apply/bootstrap、Descriptor migration/generated handles、Legacy path removal/test scene/docs sync。
- **Evidence**: SDD-0012~SDD-0019 的 README、sdd.json、design/main.md、tasks.md、bdd.md、progress.md、notes.md；project.json、roadmap.md、README.md 同步到 SDD-0012 当前。
- **Impact**: Data 完整重构有可恢复、可验证、可逐步执行的路线，不再让旧 SDD-0011 的 C#-first 修复误导后续实现。
- **Resume**: 从 SDD-0012 T1.1 开始执行；完成每个切片后更新本项目状态板。

### P008 — 2026-05-28 20:41 — data-rewrite-execution-prompt

- **Context**: 用户要求生成 Data 系统重构总提示词，并澄清这些任务不应 8 个 SDD 并行改源码。
- **Conclusion**: 已新增 `data-rewrite-execution-prompt.md`，总提示词明确 Data 完整重构裁决、主会话顺序执行策略、subagent 使用边界、SDD-0012~SDD-0019 阶段目标、固定 TDD 执行循环和最终验收标准。
- **Evidence**: `data-rewrite-execution-prompt.md` 已写入项目根；`README.md` Reading Order 已登记该入口。
- **Impact**: 后续新会话可先读总提示词，再进入当前 SDD 的 `execution-prompt.md`，降低跨会话执行策略漂移风险。
- **Resume**: 从 `data-rewrite-execution-prompt.md` 的“下一步”进入 SDD-0012。

### P009 — 2026-05-28 21:18 — sdd-0012-done

- **Context**: 完成 Data Full Rewrite 第一个执行切片 SDD-0012。
- **Conclusion**: Catalog TDD Slice 已完成，建立 `Tools/DataCatalogTdd` 纯 C# 测试入口，并落地 descriptor-first catalog 最小实现与一次性旧定义审计报告。
- **Evidence**: `dotnet run --project Tools/DataCatalogTdd/DataCatalogTdd.csproj --no-restore` 通过 18/18；`dotnet build Brotato_my.csproj --no-restore` 通过；`bash Workspace/SystemAgent/Tools/skill-test/lint.sh static all --no-fail --summary-only` 输出 39 skills Critical 0 / Advisory 0。
- **Impact**: SDD-0012 不接入旧 `Data.cs` runtime，也不做 records apply、Entity bootstrap、Feature bridge 或旧路径删除；这些边界留给 SDD-0013~SDD-0019。
- **Resume**: 当前 SDD 切到 SDD-0013，下一步补齐 DataOS descriptor authoring schema、generator、validator 和 snapshot descriptor 契约。

### P010 — 2026-05-28 21:49 — sdd-0013-done

- **Context**: 完成 Data Full Rewrite 第二个执行切片 SDD-0013。
- **Conclusion**: DataOS authoring schema、validator、generator 和 snapshot fixture 已进入 descriptor-first 形态；仓库 `runtime_snapshot.json` 可被 `RuntimeDataSnapshotLoader.BuildCatalog` 消费。
- **Evidence**: `dotnet run --project Tools/DataCatalogTdd/DataCatalogTdd.csproj --no-restore` 通过 22/22；`bash Data/DataOS/Tools/validate-dataos.sh /tmp/sdd0013-final-generate.db` 通过；`bash Workspace/Tools/ai-config-sync/sync-ai-config.sh` 完成且 advisory skill-lint Critical 0 / Advisory 0。
- **Impact**: SDD-0014 可以开始实现 runtime slot 与 policy enforcement；SDD-0017 可依赖 record/descriptor consistency 和 descriptor DTO shape。
- **Resume**: 当前 SDD 切到 SDD-0014。

### P011 — 2026-05-28 22:12 — sdd-0014-done

- **Context**: 完成 Data Full Rewrite 第三个执行切片 SDD-0014。
- **Conclusion**: Data runtime slot 与 policy model 已接入旧 ECS Data：绑定 `DataDefinitionCatalog` 的 `Data` 会走 `DataRuntimeStorage`，执行 descriptor default、unknown key fail-fast、strict conversion、write policy、range policy、allowed values、remove/clear fallback 和 Data changed 事件桥接。
- **Evidence**: `dotnet run --project Tools/DataCatalogTdd/DataCatalogTdd.csproj --no-restore` 通过 31/31；`dotnet build Brotato_my.csproj --no-restore` 通过；`bash Workspace/SystemAgent/Tools/skill-test/lint.sh static all --no-fail --summary-only` 输出 39 skills Critical 0 / Advisory 0；`git diff --check` 通过。
- **Impact**: SDD-0015 可在新 runtime storage 上实现 modifier pipeline；computed resolver、snapshot records apply、Entity bootstrap、generated handles、旧路径删除仍留给 SDD-0016~SDD-0019。
- **Resume**: 当前 SDD 切到 SDD-0015。

### P012 — 2026-05-29 06:21 — sdd-0016-done

- **Context**: 完成 Data Full Rewrite 第五个执行切片 SDD-0016。
- **Conclusion**: Data computed runtime 已完成，Feature 仍只改输入/授予 modifier，computed 输出由 Data resolver 独立负责。
- **Evidence**: `dotnet run --project Tools/DataCatalogTdd/DataCatalogTdd.csproj --no-restore` 通过 42/42，新增覆盖 resolver dependencies、compute_params、cache、transitive dirty、computed readonly 和 AttributeBonus/Percent/AttackInterval 示例。
- **Impact**: SDD-0017 可基于 descriptor catalog + runtime storage + modifier + compute 实现 snapshot records apply 和 Entity/Data bootstrap。
- **Resume**: 当前 SDD 切到 SDD-0017。

### P013 — 2026-05-29 07:14 — sdd-0017-done

- **Context**: 完成 Data Full Rewrite 第六个执行切片 SDD-0017。
- **Conclusion**: Runtime snapshot records apply 和显式 Entity/Data bootstrap 路径已完成；新 snapshot apply 链路以 `DataDefinitionCatalog` 为事实源，不回退 `DataRegistry.GetMeta`。
- **Evidence**: `dotnet run --project Tools/DataCatalogTdd/DataCatalogTdd.csproj --no-restore` 通过 48/48；`dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly` 通过（保留既有 CA2255 warning）；`python3 Workspace/SDD/sdd.py validate --all` 0/0。
- **Impact**: SDD-0018 可开始迁移业务 descriptors 与 generated typed handles；未显式传入 bootstrap 的 Entity 仍保留旧 `LoadFromConfig` 迁移路径，留待 SDD-0019 删除。
- **Resume**: 当前 SDD 切到 SDD-0018。

### P014 — 2026-05-29 08:35 — sdd-0018-done

- **Context**: 完成 Data Full Rewrite 第七个执行切片 SDD-0018。
- **Conclusion**: 旧 DataKey/DataMeta 字段能力已迁移到 DataOS `data_key_descriptor` authoring 事实源；runtime snapshot descriptors 不再由 records 反推；`GeneratedDataKey` typed thin handle 由 snapshot 生成，且不携带默认值、范围、computed 或 modifier 策略。
- **Evidence**: DataOS build/validate/generate 通过，`runtime_snapshot.json` descriptorCount=212；`dotnet run --project Tools/DataCatalogTdd/DataCatalogTdd.csproj --no-restore` 50/50 通过；`dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly` 通过（保留既有 warnings）。
- **Impact**: Runtime/business Data 调用点已迁移到 `GeneratedDataKey.*`；旧 `DataKey.*` 迁移期残留仅限 TestSystem Attribute DataMeta UI 等旧元数据编辑入口，留给 SDD-0019 删除旧路径时收口。
- **Resume**: 当前 SDD 切到 SDD-0019。

### P015 — 2026-05-29 08:50 — entity-relationship-design-expanded

- **Context**: 用户要求基于当前 Entity core、DocsNew、AiFirst 参考实现、Resources/Engine 和外部 ECS 资料，深度分析 EntityManager 与 Relationship 是否应继续保留。
- **Conclusion**: `EntityManager` 应重构为兼容 facade + spawn/lifecycle pipeline；Relationship 概念保留但只作为 typed `LifecycleTree`，业务归属、统计归因、UI/Component 绑定迁出通用字符串关系图，改用 typed `EntityId` / `DataKey` / capability-owned index。
- **Evidence**: `design/3.Entity系统优化/00-研究证据与裁决.md` 已扩展为 12 节，覆盖当前代码事实、Bevy/Flecs/Unity/Friflo/EnTT 对照、AiFirst 采纳点、伤害归因建模、分阶段迁移和验证策略；`design/INDEX.md` 已更新摘要。
- **Impact**: 后续 Entity/Relationship 执行型 SDD 应优先做 ID 统一、`LifecycleTree` 并行引入、业务 reference 迁移和 `EntityManager` facade 化，不建议继续新增 `EntityRelationshipType` 字符串常量。
- **Resume**: 当前执行主线仍是 SDD-0019；Entity/Relationship 后续从 `design/3.Entity系统优化/README.md` 进入。

### P016 — 2026-05-29 08:35 — sdd-0019-done

- **Context**: 完成 Data Full Rewrite 第八个执行切片 SDD-0019。
- **Conclusion**: Data Legacy Path Removal and Test Scene Rebuild 已完成，Data 子系统的旧输入路径和旧 Data 单场景测试入口已收口到 descriptor-first DataOS 路径。
- **Evidence**: legacy Data grep gate 无命中；`dotnet run --project Tools/DataCatalogTdd/DataCatalogTdd.csproj --no-restore` 50/50 通过；`dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly` 通过（839 warnings / 0 errors）；四个 SlimeAI headless DataOS scenes exit=0 且 15 PASS；`bash Workspace/Tools/ai-config-sync/sync-ai-config.sh` 完成且 advisory skill-test Critical 0 / Advisory 0；`bash Workspace/SystemAgent/Tools/skill-test/lint.sh static all --no-fail --summary-only` 输出 39 skills Critical 0 / Advisory 0；`python3 Workspace/SDD/sdd.py validate SDD-0019` 0/0。
- **Impact**: SDD-0012~SDD-0019 的 Data Full Rewrite 序列完成；项目下一阶段可从 Event 或 Entity/Relationship 优化 SDD 开始。
- **Resume**: 若继续 PRJ-0002，优先从 `design/3.Entity系统优化/` 创建 Entity 完整重构执行型 SDD，或从 `design/13-旧ECS框架Event系统问题分析与优化方向.md` 创建 Event SDD。

### P017 — 2026-05-29 09:05 — entity-relationship-hard-cutover-decision

- **Context**: 用户明确要求 Entity/Relationship “不做任何兼任，完全重构”，否定兼容 facade、双写、legacy fallback 和旧入口长期保留。
- **Conclusion**: `design/3.Entity系统优化/00-研究证据与裁决.md` 已更新为 hard cutover 裁决：`EntityManager` 不继续兼任业务 manager，不保留 static 兼容 facade；旧 `EntityRelationshipManager / EntityRelationshipType / ParentRelationTypes / BindParentRelationships(params string[]) / EntityManager_Ability` 等作为删除目标；统计归因以 `DamageAttribution` 为唯一事实源。
- **Evidence**: `design/3.Entity系统优化/00-研究证据与裁决.md` 的结论、`EntityManager` 重构目标、执行路线、风险缓解、默认假设和后续 SDD 拆分均已改为完整重构；`design/INDEX.md` 已同步摘要。
- **Impact**: 后续 Entity/Relationship 执行型 SDD 不应拆成长期兼容迁移链，也不应保留 legacy adapter；应创建单个 `entity-relationship-full-rewrite` SDD，内部按 ID -> LifecycleTree -> typed references -> spawn pipeline 顺序完成 hard cutover。
- **Resume**: 从 `design/3.Entity系统优化/README.md` 创建完整重构 SDD。

### P018 — 2026-05-29 09:25 — entity-full-rewrite-design-package

- **Context**: 用户进一步指出 Entity 重构是大任务，需要对每个部分用 AI-first 思想深度思考，并允许拆成多文件、补充代码说明。
- **Conclusion**: 已新增 `design/3.Entity系统优化/` 设计包，承接 Entity 完整重构主事实源；原根目录 Entity 分析文档已迁入本目录作为 `00-研究证据与裁决.md`。
- **Evidence**: 新增 `3.Entity系统优化/README.md`、`01-目标架构与模块拆分.md`、`02-代码实现说明.md`、`03-LifecycleTree与业务引用设计.md`、`04-完全重构范围与TDD测试计划.md`；`design/INDEX.md` 已登记所有新文档。
- **Impact**: 后续执行型 SDD 应从 `3.Entity系统优化/README.md` 进入，按 hard cutover 完成 typed EntityId、EntityRegistry、SpawnPipeline、LifecycleTree、ComponentRegistrar、OwnedReferenceRegistry、DamageAttribution 和 Observation，且最终用 grep gate 证明旧 Relationship runtime 删除。
- **Resume**: 创建 `entity-relationship-full-rewrite` SDD 时，先读 `3.Entity系统优化/04-完全重构范围与TDD测试计划.md` 的 T1~T10 任务序和最终 grep gate。

### P019 — 2026-05-29 10:05 — entity-callsite-migration-and-execution-prompt

- **Context**: 用户强调 Entity 重构范围大，需要每个部分都详细说明，并可拆多文件、补充代码怎么改。
- **Conclusion**: 已把 Entity hard cutover 从架构设计补强到执行层：新增当前源码调用点迁移清单，按 Entity core、Component、Ability、Projectile、Effect、Damage、Movement、UI/Test 分桶说明旧入口、替代 owner、新 API、测试重写和最终 grep gate；新增 Entity 总执行提示词，固定新执行会话的读取顺序、TDD 任务序、禁止兼容项和验证命令。
- **Evidence**: `design/3.Entity系统优化/05-源码调用点迁移清单.md`、`entity-rewrite-execution-prompt.md`；`roadmap.md` 已把 Entity hard cutover 标为 P0 下一步；`design/04-优化优先级与SDD拆分建议.md` 已标注旧 `SDD-C/D` 被完整重构 SDD 替换。
- **Research Adoption**: externalResources enabled=`engine-framework`，scope=`Resources/Engine/Docs/FrameworkAnalysis/Reports/*` 中 Bevy/Flecs/DefaultEcs/Arch/Friflo/Unity/EnTT 相关 relationship/entity 片段 + 官方 Bevy/Flecs/Unity/Friflo/EnTT 文档；reason=校准 ECS relationship/hierarchy 是否应作为 SlimeAI runtime hot path；expires=current-task；copiedCodeOrAssets=none。
- **Impact**: 后续实现者不需要从旧问题分析里自行猜调用点，可直接从调用点清单和执行提示词创建 `Entity Relationship Full Rewrite` SDD。
- **Resume**: 下一步运行 `python3 Workspace/SDD/sdd.py new "Entity Relationship Full Rewrite" --project PRJ-0002 --type refactor --scope SlimeAI --area ecs/entity --tag entity --tag relationship --tag hard-cutover`，然后按 `entity-rewrite-execution-prompt.md` 执行。

### P020 — 2026-05-29 10:25 — entity-docs-consolidated

- **Context**: 用户要求 Entity 文档最终都改到 `design/3.Entity系统优化/`。
- **Conclusion**: 已将根目录旧 Entity 分析文档迁入 `design/3.Entity系统优化/00-研究证据与裁决.md`，Entity 设计事实源集中到 `3.Entity系统优化/`。
- **Evidence**: `design/3.Entity系统优化/00-研究证据与裁决.md`、`design/3.Entity系统优化/README.md`、`design/INDEX.md`、`roadmap.md`、`entity-rewrite-execution-prompt.md` 已同步引用新路径。
- **Impact**: 后续不再从根 `design/02-*` 进入 Entity；执行型 SDD 只读 `design/3.Entity系统优化/README.md` 及本目录子文档。
- **Resume**: 创建 Entity 执行 SDD 前，先确认根 `design/` 下没有独立 Entity 分析入口残留。

### P021 — 2026-05-29 10:40 — data-snapshot-first-usage-sdd-created

- **Context**: 用户指出 Data 要改的地方很多，所有取用都要改成最新 Data 系统形式，并要求生成 SDD 任务和深度分析。
- **Conclusion**: 已创建 SDD-0020 `Data Snapshot-First Usage Cutover`。本任务判定 SDD-0019 后仍存在 Data 兼任残留，后续先完成 Data 使用层 hard cutover，再继续 Entity/Event 大改。
- **Evidence**: `sdds/010-SDD-0020-data-snapshot-first-usage-cutover/README.md`、`design/main.md`、`tasks.md`、`bdd.md`、`progress.md`、`notes.md`；项目 `roadmap.md`、`project.json` 和本 `progress.md` 已登记 SDD-0020。
- **Impact**: PRJ-0002 当前 SDD 切到 SDD-0020；Entity hard cutover 不取消，但顺序后移，避免旧 Data 取用方式继续污染后续 Entity/Relationship 改造。
- **Resume**: 从 SDD-0020 T1.1 readiness gate 开始。

### P022 — 2026-05-29 19:25 — sdd-0020-done

- **Context**: 完成 Data snapshot-first usage hard cutover。
- **Conclusion**: Data 使用层已从旧 RuntimeTables / DataTable / config object 推断 / DataRegistry fallback 切到 `runtime_snapshot.json`、`DataRuntimeBootstrap`、`RuntimeDataRecordQuery`、typed projection 和 catalog-bound `Data`。旧 DataConfigEditor 与旧文档推荐路线已清理。
- **Evidence**: `dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly` 通过；`bash Data/DataOS/Tools/validate-dataos.sh Data/DataOS/Authoring/slimeainew.authoring.db` 通过；四个 DataOS Godot scenes 均 `status=passed` / `firstError=null`；最终 grep gates 对旧入口无命中。`Tools/DataCatalogTdd/DataCatalogTdd.csproj` 当前不存在，已作为提示词/仓库路径漂移记录在 SDD-0020。
- **Impact**: Data 不再阻塞 Entity / Relationship hard cutover；后续 Entity spawn 可要求显式 snapshot record handle，不需要兼容旧 config 推断。
- **Resume**: 下一步创建 `Entity Relationship Full Rewrite` SDD。

### P023 — 2026-05-30 — sdd-0021-created

- **Context**: SDD-0020 后运行报错和无兼容总审计指出 Data 主链路仍有类型契约和兼容入口残留。
- **Conclusion**: 已创建 SDD-0021 `Data No-Compatibility Hard Cutover`，把 `AbilityIcon` / `AvailableAnimations` 报错上升为 generator、validator、generated handle、Data API、非标量类型、旧 authoring 和文档事实源的系统性收口任务。
- **Evidence**: `sdds/011-SDD-0021-data-no-compatibility-hard-cutover/README.md`、`design/main.md`、`tasks.md`、`bdd.md`、`execution-prompt.md`；项目 `roadmap.md`、`project.json`、`README.md` 和本 `progress.md` 已切到 SDD-0021。
- **Impact**: Entity / Relationship hard cutover 继续后移；Data 先完成 no-compat hard cutover，避免旧兼容入口继续污染后续 Entity 改造。
- **Resume**: 从 SDD-0021 T1.1 readiness baseline 开始。

### P024 — 2026-05-30 — sdd-0021-done

- **Context**: 完成 Data No-Compatibility Hard Cutover。
- **Conclusion**: Data 主链路已完成无兼容收口：final snapshot record type 来自 descriptor，validator 校验最终 `runtime_snapshot.json`，`string_array/object_ref/modifier_list` 生成真实 CLR typed handle，`DataKey<T> -> string` 和 generated alias 删除，Data string-key API 内化，未绑定 Data 运行时窗口退出，DataMeta/DataRegistry/Legacy audit runtime 编译面删除，RuntimeTables 迁到 RuntimeModels，System/Feature Resource authoring 当前入口删除。
- **Evidence**: `bash Data/DataOS/Tools/validate-dataos.sh Data/DataOS/Authoring/slimeainew.authoring.db` 通过；snapshot jq mismatch 无输出；`dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly` 通过；最终 grep gates 无非历史命中；Godot DataOS scenes `DataCatalogTestScene`、`DataRuntimeTestScene`、`DataSnapshotApplyTestScene`、`DataFeatureBridgeTestScene` 在 `.ai-temp/scene-tests/runs/2026-05-30/08-15-52/index.json` 全部 passed。
- **Impact**: Data 不再作为 Entity / Relationship hard cutover 的兼容残留阻塞项；后续 Entity 设计可要求 typed ID、显式 record、descriptor-first Data 和无 legacy fallback。
- **Resume**: 下一步创建 `Entity Relationship Full Rewrite` SDD，并从 `design/3.Entity系统优化/README.md` 与 `entity-rewrite-execution-prompt.md` 执行。

### P025 — 2026-05-30 — sdd-0022-created

- **Context**: 用户新增 4 份 Data residual 文档，指出 SDD-0021 后仍有 projection、diagnostics、record completeness、spawn boundary 和 docs gate 残余问题。
- **Conclusion**: 已创建 SDD-0022 `Data Projection Diagnostics Contract Hardening`，并把 4 份来源文档导入子 SDD `design/`，生成执行级 tasks、BDD 和 `execution-prompt.md`。
- **Evidence**: `sdds/012-SDD-0022-data-projection-diagnostics-contract-hardening/README.md`、`design/main.md`、`design/03-*`、`design/04-*`、`design/05-*`、`design/06-*`、`tasks.md`、`bdd.md`、`progress.md`、`execution-prompt.md`；项目 `roadmap.md`、`project.json`、`README.md` 和本 `progress.md` 已切到 SDD-0022。
- **Impact**: Entity / Relationship hard cutover 继续后移；Data 先完成中层契约硬化，避免把 record completeness、初始化时序和 projection 软契约带入 Entity 重构。
- **Resume**: 从 SDD-0022 T1.1 readiness baseline 开始。

### P026 — 2026-05-30 — sdd-0022-done

- **Context**: 完成 Data Projection Diagnostics Contract Hardening。
- **Conclusion**: SDD-0022 已完成：DataOS final snapshot completeness、descriptor-first projection、runtime write diagnostics、typed runtime projection、`object_ref` / `string_array` / `modifier_list` 类型契约、spawn boundary、catalog freeze、display name query 和 current docs gate 已收口。验证中额外修正 `AbilityDamageBonus` 默认值语义，避免 ability 缺字段时默认伤害翻倍。
- **Evidence**: DataOS generate/validate 通过；snapshot descriptor/record mismatch `jq` gate 0 行；player/enemy `DefaultMoveMode` 为 `AIControlled` / `PlayerInput`；`AbilityDamageBonus` authoring DB 与 snapshot 默认值为 `0`；`dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly` 通过；runtime/docs grep gate 均 0 命中；Godot headless `DataRuntimeTestScene`、`DataCatalogTestScene`、`DataSnapshotApplyTestScene`、`MovementComponentTestScene -- --sdd-smoke`、`AbilitySystemPipelineTest` 全部 exit 0，Ability pipeline `PASS=16, FAIL=0`；`python3 Workspace/SDD/sdd.py validate SDD-0022` 0 error / 0 warning。
- **Impact**: PRJ-0002 当前没有 active 子 SDD；Data 不再阻塞 Entity / Relationship hard cutover。
- **Resume**: 下一步创建 `Entity Relationship Full Rewrite` SDD，从 `design/3.Entity系统优化/README.md` 与 `entity-rewrite-execution-prompt.md` 开始。

### P027 — 2026-05-31 — entity-data-event-docsai-sync

- **Context**: Data、Event 和 DocsAI 已更新为 current：Data 走 descriptor / generated handle / catalog-bound runtime，Event 走 typed payload key，DocsAI 成为框架统一文档入口。用户要求检查并更新 2026-05-29 的 Entity 重构设计包。
- **Conclusion**: Entity hard cutover 方向不变，但执行入口已校准：新增 `06-2026-05-31-DataEventDocsAI同步校准.md`，明确 `GeneratedDataKey.Id` 只作为 EntityId 字符串投影、业务引用默认使用 typed runtime API + generated Data projection、Entity lifecycle event 必须用 typed payload、DocsAI 是 current 文档同步目标。
- **Evidence**: `design/3.Entity系统优化/README.md`、`00~05`、`06-2026-05-31-DataEventDocsAI同步校准.md`、`entity-rewrite-execution-prompt.md`、`design/INDEX.md`、`roadmap.md` 已同步。
- **Impact**: 后续 Entity Relationship Full Rewrite SDD 不应再从 `DocsNew`、`SlimeAI/Src` 路径、旧 `DataKey.Id` 或旧 Event 字符串主键恢复上下文。
- **Resume**: 创建执行 SDD 时先跑 `cd /home/slime/Code/SlimeAI/SlimeAI && rg -n "EntityRelationshipManager|EntityRelationshipType|ParentRelationTypes|BindParentRelationships|EntityRelationshipTraversal|AutoAddParentRelation|ParentEntity|EntityManager\\.(AddAbility|GetAbilities|RemoveAbility)|GetAncestorChain|FindAncestorOfType<IUnit>|DataKey\\.Id|GetInstanceId\\(\\)\\.ToString\\(\\)" Src/ECS/Base Src/ECS/Test DocsAI/ECS`，再按 T1~T10 执行。

### P028 — 2026-05-31 — entity-docsai-current-entry

- **Context**: `DocsAI/` 已作为 SlimeAI 框架统一文档入口，Entity owner 文档仍有从 `Src/ECS` 迁入的旧示例，包含旧 `DataKey.*`、`XxxEventData`、`EntityRelationshipManager` 和 `GetInstanceId().ToString()` 写法。
- **Conclusion**: 已补齐 `DocsAI/ECS/Entity/README.md` current 入口，重写 `Entity使用说明.md` 和 `EntityManager.md` 为当前 Data/Event/Relationship 边界说明，并把 `Entity规范.md` 标为 legacy-migrated 且修正危险示例。
- **Evidence**: `DocsAI/ECS/Entity/README.md`、`Entity使用说明.md`、`EntityManager.md`、`Entity规范.md`；`python3 Workspace/SDD/sdd.py validate --all` 0 error / 0 warning；`find Src/ECS -type f -name '*.md' | sort` 无输出。
- **Impact**: current DocsAI 不再把旧 Entity/Data/Event/Relationship 示例作为可复制入口；旧长文档只保留迁移追溯语义。
- **Resume**: 后续创建 `Entity Relationship Full Rewrite` SDD 前，先读 `DocsAI/ECS/Entity/README.md`，再读 `design/3.Entity系统优化/06-2026-05-31-DataEventDocsAI同步校准.md`。

### P029 — 2026-05-31 — sdd-0024-created

- **Context**: 用户明确要求生成 Entity 重构执行型 SDD。
- **Conclusion**: 已创建并启动 `SDD-0024 Entity Relationship Full Rewrite`，将项目 `current_sdd` 切到 SDD-0024，并把任务拆成 T1.1~T1.11 的 TDD hard cutover 序列。
- **Evidence**: `sdds/014-SDD-0024-entity-relationship-full-rewrite/README.md`、`design/main.md`、`tasks.md`、`bdd.md`、`progress.md`；`project.json`、`README.md`、`roadmap.md`、本 `progress.md` 已同步 current SDD。
- **Impact**: PRJ-0002 默认恢复点现在进入 Entity / Relationship hard cutover，不再停留在“创建 SDD”的前置状态。
- **Resume**: 从 `sdds/014-SDD-0024-entity-relationship-full-rewrite/tasks.md` 的 T1.1 开始：先跑 baseline grep 和记录 dirty 范围，再写 EntityId / EntityRegistry RED tests。

### P030 — 2026-06-01 — directory-architecture-design-package

- **Context**: 用户确认 Runtime 以外的功能统一放入 `Capabilities/`，DocsAI 使用相同结构，并要求在 `design/6.ECS框架目录架构大重构` 下生成深度设计文档、SDD 和提示词。
- **Conclusion**: 已创建 SDD-0025 `ECS Framework Directory Architecture Restructure`，并生成项目级设计包和总执行提示词。当前裁决是保留 `Src/ECS` 主线，不迁到 `GameOS`；物理目录采用 `Runtime + Capabilities`，DocsAI 对齐 `Runtime + Capabilities + Foundations`，DocsOld 原文迁入目标为 `DocsAI/ECS/Foundations/`。
- **Evidence**: `design/6.ECS框架目录架构大重构/README.md`、`01-现状证据与AI-first裁决.md`、`02-目标目录架构与归属规则.md`、`03-迁移切片与验证门禁.md`、`directory-architecture-restructure-execution-prompt.md`、`sdds/015-SDD-0025-ecs-framework-directory-architecture-restructure/`。
- **Impact**: PRJ-0002 当前恢复入口切到 SDD-0025；后续真正迁目录必须先执行 readiness baseline，不能直接移动所有文件。
- **Resume**: 从 `sdds/015-SDD-0025-ecs-framework-directory-architecture-restructure/tasks.md` 的 T1.1 开始；先 `git status --short` 和 `rg` 旧路径引用，记录当前未提交 `.uid` / `__pycache__` 改动为既有工作区状态。

### P031 — 2026-06-01 — input-contract-research-adoption

- **Context**: 用户要求围绕 `Src/ECS/Tools/Input`、`ActiveSkillInputComponent`、`DocsAI/ECS/Tools/Input` 和 PRJ-0002 Input 设计包，深度研究键盘+手柄、手柄类型差异，并参考 Brotato / Slay the Spire 2。
- **Conclusion**: 已将 Input 收口为 AI-first Input Contract：`project.godot` 保持 Godot InputMap 物理绑定事实源，`DocsAI/ECS/Tools/Input/InputMap.md` 作为 AI 可读 manifest，`InputManager` 是业务 facade；手柄按 Xbox/XInput、PlayStation、Nintendo Switch、Steam Input、generic SDL gamepad 分层，UI glyph 后续单独做 `ControllerGlyphProfile`。
- **Evidence**: `project.godot` 为 `BtnX/BtnY/BtnLB/BtnRB` 增加键盘备用 `J/I/Q/E`；`DocsAI/ECS/Tools/Input/{Concept.md,Usage.md,InputMap.md}` 已重写；`design/Tool/Input/` 三个设计文档和 README 已补充官方资料、本地 Brotato/Slay the Spire 2 证据、InputMap 更新判定和验证 gate；`DocsAI/ECS框架与AIFirst方向决策.md` 已补 Input 契约小节；`tools` skill 源已同步 Input owner。
- **Research Adoption**: externalResources enabled=`official-docs,local-game-reference`，scope=Godot Input/InputMap docs、Unity Input System docs、Unreal Enhanced Input docs、`Resources/Games/Games/Brotato`、`Resources/Games/Games/Slay.the.Spire.2.v0.105.1`；copiedCodeOrAssets=none；adoption=分层原则与 manifest/glyph profile 路线，不复制实现代码。

### P032 — 2026-06-01 — directory-architecture-closeout-adjusted

- **Context**: 用户删除 `Foundation/Foundations` 当前层，并明确 `IEntity`、`TemplateEntity` 放入 `Runtime/Entity/`，`IWeapon` 已删除，需要同步当前事实源。
- **Conclusion**: SDD-0025 收口结论更新为：`Src/ECS/Base` 清空；`IEntity/TemplateEntity` 位于 `Src/ECS/Runtime/Entity/`；`IComponent/TemplateComponent` 位于 `Src/ECS/Runtime/Component/`；具体 Entity / Component / Preset 归功能 owner；DocsAI current route 为 Runtime / Capabilities / Tools / UI，不再包含 `Foundations`。
- **Evidence**: `DocsAI/ECS/README.md`、`DocsAI/ECS/Runtime/README.md`、`DocsAI/ECS/Capabilities/README.md`、`DocsAI/管理/目录架构迁移清单.md`、SDD-0025 README/tasks/progress/design 副本、`.ai-config` skill/rule 源已同步到该裁决；本轮验证见 SDD-0025 validation 记录。
- **Impact**: 后续 AI 路由不再把历史概念材料集中到 `DocsAI/ECS/Foundations/`，而是按 owner `Concepts/`、Archive 或 Thinking 查阅。
- **Resume**: 当前 active SDD 为 SDD-0026；目录架构问题从 `DocsAI/ECS/README.md` 和 `design/6.ECS框架目录架构大重构/README.md` 恢复。
- **Impact**: 后续 AI 改键不应从组件里猜 `BtnX` 含义；应从 `DocsAI/ECS/Tools/Input/InputMap.md` 查业务 action、context、consumer，再改 `project.godot` 和 `InputManager`。
- **Resume**: 若继续 Input runtime 优化，下一步先新增业务语义 facade（如 `IsUseActiveAbilityPressed`、`IsTargetConfirmPressed`），再分阶段替换 `ActiveSkillInputComponent` / `TargetingIndicatorControlComponent` 的按钮名 API，并保留 Debug/Test 例外。

### P033 — 2026-06-01 — sdd-0026-done

- **Context**: 完成 Input Contract Manifest And Facade Hardening。
- **Conclusion**: Input runtime 已补业务语义 facade：`IsUseActiveAbilityPressed`、`IsPreviousActiveAbilityPressed`、`IsNextActiveAbilityPressed`、`IsTargetConfirmPressed`、`IsTargetCancelPressed`、`IsPausePressed`；Ability、Targeting、PauseMenu 调用点已迁移，不再从按钮名 API 猜业务语义。
- **Evidence**: `dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly` 0 warning / 0 error；`python3 Workspace/SDD/sdd.py validate SDD-0026` 和 `validate --all` 均 0/0；`bash Workspace/SystemAgent/Tools/skill-test/lint.sh static all --no-fail --summary-only` 输出 39 skills Critical 0 / Advisory 0；业务层旧按钮 API grep gate 无输出。
- **Impact**: AI 改键入口现在从 `DocsAI/ECS/Tools/Input/InputMap.md` 的业务 action/context 出发，追到 `InputManager` 语义方法和 consumer；Debug/Test 裸输入仍作为例外保留。
- **Resume**: PRJ-0002 当前无 active 子 SDD；未来 Input 深化应新建 SDD 覆盖 `ControllerGlyphProfile`、运行时 `InputContext` 或 manifest 自动校验。

### P034 — 2026-06-02 — sdd-0027-created

- **Context**: 用户要求根据 `design/Tool/Timer` 文档生成 SDD 和执行 SDD 的提示词。
- **Conclusion**: 已创建并补齐 `SDD-0027 Timer Scheduler Full Rewrite`。该 SDD 固定 Timer 重构执行边界：纯 C# scheduler、TimerManager facade、min-heap、TimerHandle、owner/purpose/clock、主线程 dispatch、debug diagnostics、TimerStressValidation、DocsAI/skill sync 和最终验证。
- **Evidence**: `sdds/017-SDD-0027-timer-scheduler-full-rewrite/README.md`、`design/main.md`、`tasks.md`、`bdd.md`、`progress.md`、`notes.md`、`execution-prompt.md`；项目 `README.md`、`roadmap.md`、`project.json` 和本 `progress.md` 已切到 SDD-0027。
- **Impact**: 后续 Timer 实现不应从聊天记忆恢复，也不应做局部优化；新执行会话应直接按 `execution-prompt.md` 逐项推进。
- **Resume**: 从 SDD-0027 T1.1 readiness baseline 开始；当前框架仓 dirty worktree 中存在大量非本 SDD 改动，执行时必须保留并避免混入。

### P035 — 2026-06-03 — sdd-0028-created

- **Context**: 用户要求精简 `DocsAI/ECS/Capabilities/Collision/Concepts`，把旧脱树和碰撞长文放入 `History/`，并生成包含提示词的 ObjectPool / Collision SDD。
- **Conclusion**: 已创建并补齐 `SDD-0028 ObjectPool Collision ParkedInTree Cutover`。该 SDD 固定 ObjectPool / Collision 后续执行边界：`ParkedInTree` 默认迁移、pool runtime state、parking grid、CollisionLogicGuard、ContactDamage 旧 attacker timer 清理、ObjectPool contract、Godot collision validation、DocsAI/skill sync 和最终验证。
- **Evidence**: `DocsAI/ECS/Capabilities/Collision/Concepts/README.md`、`Godot物理时序与对象池碰撞.md`、`Node2D父链约束.md`、`History/`；`sdds/018-SDD-0028-objectpool-collision-parkedintree-cutover/README.md`、`design/main.md`、`tasks.md`、`bdd.md`、`progress.md`、`notes.md`、`execution-prompt.md`；项目 `README.md`、`roadmap.md`、`project.json` 和本 `progress.md` 已切到 SDD-0028。
- **Impact**: 后续 ObjectPool / Collision 实现不应从旧 `Concepts/对象池碰撞兼容说明.md` 顶层路径恢复，也不应做只改对象池半边的局部补丁；新执行会话应直接按 SDD-0028 提示词逐项推进。
- **Resume**: 从 SDD-0028 T1.1 readiness baseline 开始；当前框架仓 dirty worktree 中存在大量非本 SDD 改动，执行时必须保留并避免混入。

### P036 — 2026-06-04 — component-design-package

- **Context**: 用户要求按 AI-first 思想深度复查旧 ECS Component，并在 `design/7.Component/` 生成设计文档，参考本地 Resources、Context7 和 web 资料。
- **Conclusion**: 已补齐 Component 项目级共享设计包；裁决保留 `IComponent + ComponentRegistrar` 最小契约，不把 Component 改成纯数据 ECS storage，不恢复 `ENTITY_TO_COMPONENT` 关系图。后续首切片建议只做 ComponentManifest、生命周期契约、外部订阅清理审计、动态组件策略、preflight、DocsAI/skill sync 和验证 artifact。
- **Evidence**: `design/7.Component/README.md`、`01-现状证据与AI-first裁决.md`、`02-目标架构与优化路线.md`、`03-调用点迁移与验证计划.md`；`design/INDEX.md`、`README.md`、`roadmap.md` 和本 `progress.md` 已同步 Component 入口与恢复点。
- **Research Adoption**: externalResources enabled=`engine-framework, official-docs`，scope=`Resources/Engine/Docs/FrameworkAnalysis/Reports/*` 中 ECS component / relationship / GodotBridge 片段 + Context7 Bevy ECS component / bundle / query / ChildOf 文档片段；web/curl 部分官方细页在当前网络下超时，未作为强证据；copiedCodeOrAssets=none。
- **Impact**: 后续 AI 不应把 SlimeAI Component 当作 Bevy / Unity DOTS / Flecs / EnTT 的纯数据组件；新增或修改组件应先查 Component manifest、owner 文档、Data/Event/Service 边界和注销清理规则。
- **Resume**: 若进入 Component 实施，先创建 `Component Contract Manifest And Lifecycle Hardening` 执行型 SDD；若继续当前 System 主线，仍从 SDD-0029 execution prompt 的 T1.1 readiness baseline 开始。

### P037 — 2026-06-04 — other-tools-hard-cutover-override

- **Context**: 用户要求按 AI-first 重新分析 `design/Tool/其他Tool`，明确“需要重构就完全重构绝不兼容”，并指出 `ParentManager` 有用，应统一管理大量 Entity 节点在 tree 中的路径。
- **Conclusion**: 已将剩余 Tools 设计包从“增量 hardening / 兼容 facade”校准为“功能切片 hard cutover”：`ParentManager` 功能升级为 `RuntimeMountRegistry` / `SceneMountRegistry`，`TargetSelector` 升级为 `TargetQueryEngine` / `TargetQueryResult`，`ResourceManagement` 走 strict loading/source policy/structured result，`MyMath` 按公式 owner 拆分，`NodeLifecycle` 只保底层 registry/diagnostics。该条中 `CommonTool` 与 Must Confirm 口径已被后续 P039 用户复核校准。
- **Evidence**: `design/Tool/其他Tool/07-2026-06-04-AI-first完全重构校准.md` 新增；`README.md`、`01-现状证据与AI-first裁决.md`、`02-CommonTool与ResourceManagement裁决.md`、`03-Math目标架构与验证.md`、`04-NodeLifecycle与ParentManager边界.md`、`05-TargetSelector查询契约.md`、`06-实施路线与验证门禁.md`、`design/INDEX.md`、`README.md`、`roadmap.md` 和本 `progress.md` 已同步。
- **Research Adoption**: externalResources enabled=`engine-framework, official-docs`，scope=`Resources/Engine/Docs/FrameworkAnalysis/Reports` 中 query/resource/hierarchy/SceneTree 片段 + Context7 Godot 4.6 SceneTree/ResourceLoader/PackedScene/RandomNumberGenerator/groups 文档；copiedCodeOrAssets=none；adoption=采纳 mount lifecycle、resource facade、seeded random、capability-owned selector 和 diagnostics 思想，拒绝通用 world query DSL / group gameplay query / 第三方 ECS runtime。
- **Must Confirm**: 已被 P039 校准。用户已确认 Runtime mount 默认 `/root/SlimeAIRuntime` 和 ResourceManagement strict fail-fast；剩余确认点为 Common Utilities 目录、NodeLifecycle Runtime 归属、`EntityTargetSelector.Query` 是否只作临时桥。
- **Impact**: 后续执行者不应从旧 `ParentManager.GetOrRegister`、`EntityTargetSelector.Query` list-only、`CommonTool.LoadPackedScene`、`MyMath` 杂项公式或 `NodeLifecycleManager.GetAllNodes` 恢复 current API；应从 `07` override 和对应专题文档进入。
- **Resume**: 若切到剩余 Tools 实施，优先创建 `Runtime Mount Registry Hard Cutover` 或 `Target Query Engine Hard Cutover`，并在 SDD 中写明上述 Must Confirm 的用户裁决或采用默认假设。

### P038 — 2026-06-04 — sdd-0030-done

- **Context**: 用户要求围绕 Component design/source/docs 生成并执行 SDD。
- **Conclusion**: SDD-0030 已完成；Component 默认组合已从 `.tscn` Preset 迁到 C# profile / composer，`EntityOrientationComponent.Sink` 改为 typed options 注入，DocsAI ComponentManifest 和 `ecs-component` skill 源已同步。
- **Evidence**: `sdds/020-SDD-0030-component-code-composition-and-contract-hardening/`、`Src/ECS/Runtime/Component/ComponentComposition.cs`、`Src/ECS/Capabilities/Unit/Entity/UnitComponentCompositionProfiles.cs`、`Src/ECS/Capabilities/Ability/Entity/AbilityComponentCompositionProfiles.cs`、`DocsAI/ECS/Runtime/Component/ComponentManifest.md`。
- **Impact**: 后续 AI 不再从 Component Preset `.tscn` 推断默认组件集合；默认组合事实源是 owner C# profile，Preset 只作 legacy 对照输入。
- **Resume**: PRJ-0002 当前无 active 子 SDD；后续 Component 深化另起 SDD，先读 `DocsAI/ECS/Runtime/Component/ComponentManifest.md` 和 SDD-0030 progress。

### P039 — 2026-06-04 — other-tools-user-review-calibrated

- **Context**: 用户提供 SceneTree 截图并逐项裁决：`ParentManager` 当前规范路径作用成立，默认 `/root/SlimeAIRuntime` 可以；TargetSelector 重构可以但要说明怎么做；资源加载立刻报错；CommonTool 通用工具概念可保留但不应随便堆在 `Tools/`；NodeLifecycle 的统一注册/维护/id 管理本意成立，但要按 AI-first 重新规范。
- **Conclusion**: 已新增 `08-2026-06-04-用户裁决后执行前复核.md` 并同步其他 Tools 设计包：TargetSelector 改为“找目标报告”契约；CommonTool 裁决从“通用工具概念删除”校准为“迁出当前资源加载方法，保留受约束 Common Utilities”；NodeLifecycle 裁决从 Tools helper 校准为 Runtime registry；ParentManager 保留截图中的可读层级风格并加 `/root/SlimeAIRuntime`、manifest 和 diagnostics。
- **Evidence**: `design/Tool/其他Tool/08-2026-06-04-用户裁决后执行前复核.md`、`README.md`、`02-CommonTool与ResourceManagement裁决.md`、`04-NodeLifecycle与ParentManager边界.md`、`05-TargetSelector查询契约.md`、`06-实施路线与验证门禁.md`、`07-2026-06-04-AI-first完全重构校准.md`、`design/INDEX.md` 和本 `progress.md` 已同步。
- **Research Adoption**: externalResources enabled=`official-docs, engine-framework`，scope=Context7 `/godotengine/godot-docs` 的 PackedScene/root add child/groups 片段 + `Resources/Engine/Docs/FrameworkAnalysis/Reports` 中 Bevy/Flecs/EnTT/DefaultEcs/综合报告关于 capability-owned selector、relationship/hierarchy 和 query DSL 的裁决；copiedCodeOrAssets=none。
- **Impact**: 后续执行型 SDD 不应把 CommonTool 简单一删了事，也不应把 NodeLifecycle 删除后让各模块各写一套 registry；应按 `08-*` 的默认假设和确认点创建实现任务。
- **Resume**: 等待用户确认：1. Common Utilities 目录选 `Src/ECS/Common/Utilities/` 还是 `Src/ECS/Runtime/Common/`；2. NodeLifecycle 是否迁到 `Src/ECS/Runtime/NodeLifecycle/`；3. `EntityTargetSelector.Query(query)` 是否只作为执行期临时桥并在切片结束前删除或 internal 化。

### P040 — 2026-06-06 — gc-boxing-deepthink-design

- **Context**: 用户要求基于 AI-first 方向、DocsAI owner 文档、现有 `问题/` 初稿、源码和外部 .NET 资料，深度分析 SlimeAI 装箱拆箱与 GC 问题是否要改，并在 `design/ECS框架优化/1.拆箱装箱+GC优化` 下按功能生成设计文档。
- **Conclusion**: 已完成 GC/装箱设计包并按用户反馈修正 Data 设计。该条为 SDD-0031 前历史裁决：当时裁决 Data runtime object 是 P0，Data 方案从上一版 `DataRuntimeValue` 多字段 union 改为 `DataSlot<T>` 方向；Event dynamic object 是 P0；Feature/Ability 的 `ActivationData/ExecuteResult object?` 是 Event 禁 object 后必须同步收口的上下文宽口；ObjectPool、TargetSelector、Logger 分配问题真实存在但为 P1。Data 当前状态和非 Data 重新排序以后续 P042/P043 为准。
- **Evidence**: `design/ECS框架优化/1.拆箱装箱+GC优化/README.md`、`设计/README.md`、`设计/00-总览与AI-first裁决.md`、`01-Data运行时object去除设计.md`、`02-EventBus动态object禁用设计.md`、`03-FeatureAbility上下文类型化设计.md`、`04-ObjectPool反射管理接口设计.md`、`05-TargetSelector集合分配与LINQ设计.md`、`06-Logger字符串与诊断分配设计.md`；`design/INDEX.md`、`roadmap.md`、`notes.md` 已同步。
- **Research Adoption**: externalResources enabled=`official-docs, engine-framework`，scope=Microsoft Learn C# boxing/unboxing、.NET GC fundamentals、C# interpolated string handler；Unity Entities component docs；Bevy ECS component/storage docs；`Resources/Engine/Docs/FrameworkAnalysis/Reports` 中综合报告、DefaultEcs、Unreal GAS 对照；copiedCodeOrAssets=none；adoption=采纳 boxing/GC/handler 的语言运行时事实和 typed ECS 热路径共识，具体架构裁决仍以 SlimeAI DocsAI/SDD/源码为准。
- **Impact**: 后续不应只缓存反射或改字符串就宣称 GC 优化完成；该条的 Data 优先恢复点已被 SDD-0031 和 P043 覆盖。
- **Resume**: 历史恢复点。Data Runtime Generic Slot Hard Cutover 已完成；继续 GC/装箱优化时以 P043 的 `Event + Feature/Ability Typed Execution Boundary` 为准。

### P041 — 2026-06-06 — data-generic-slot-confirmed

- **Context**: 用户确认 `DataSlot<T> + IDataSlot` 应是 Data 去 object 的最佳方案，要求更新文档。
- **Conclusion**: Data GC hard cutover 方案已从“推荐”提升为“最终裁决”：`DataSlot<T>` 保存真实业务值，`IDataSlot` 只作为跨类型 slot 管理和 diagnostics 边界；不再把 `DataRuntimeValue` union、多字典拆分或 `object? Value` 作为同级候选。
- **Evidence**: `design/ECS框架优化/1.拆箱装箱+GC优化/设计/01-Data运行时object去除设计.md`、`00-总览与AI-first裁决.md`、`README.md`、`design/INDEX.md`、`roadmap.md`、本 `progress.md` 已同步。
- **Impact**: 后续 `Data Runtime Generic Slot Hard Cutover` SDD 不需要重新争论 Data 值容器形态；该条已由 SDD-0031 执行完成。
- **Resume**: 历史恢复点。Data Runtime Generic Slot Hard Cutover 已完成；继续 GC/装箱优化时以 P043 的 `Event + Feature/Ability Typed Execution Boundary` 为准。

### P042 — 2026-06-06 — sdd-0031-done

- **Context**: 用户要求按 GC/装箱设计包先执行 Data 方案，其他模块不动。
- **Conclusion**: SDD-0031 已完成 Data-only generic slot hard cutover：typed Data hot path 不再依赖 `DataSlot.Value object?`；computed cache 移入 typed slot；typed range policy 覆盖 float/int/double；DataCatalog descriptor count 测试改为对齐 runtime snapshot descriptors 和 manifest，避免硬编码数量漂移。
- **Evidence**: `dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly` 成功，960 warnings / 0 errors；DataOS validate 通过；`python3 Workspace/SDD/sdd.py validate SDD-0031` 和 `validate --all` 均 0/0；精确 grep gate 无旧 object slot / `_computedCache` / typed fallback 命中；`DataCatalogTestScene`、`DataRuntimeTestScene`、`DataSnapshotApplyTestScene`、`DataFeatureBridgeTestScene` 均 exit 0；`ecs-data` skill 已同步且 skill-test Critical 0 / Advisory 0。
- **Impact**: Data P0 装箱主链路已收口；`PropertyChanged(object?)`、Event dynamic object、Feature/Ability typed execution context 不在本轮范围，应后续单独 SDD。
- **Resume**: 该恢复点已被 P043 覆盖；如继续 GC/装箱优化，不再单独创建 Event SDD，应先读 P043 和 `design/ECS框架优化/1.拆箱装箱+GC优化/设计/00-总览与AI-first裁决.md`。

### P043 — 2026-06-06 — gc-non-data-reanalysis-after-data-complete

- **Context**: 用户指出 Data 已经改完，要求重新分析 `1.拆箱装箱+GC优化` 中除 Data 之外的 Event、Feature / Ability、ObjectPool、TargetSelector、Logger、Component / lifecycle 分配，并质疑此前是否真正重新分析。
- **Conclusion**: 已完成非 Data 重新裁决并落盘：下一步应合并为 `Event + Feature/Ability Typed Execution Boundary`，因为 Event dynamic object、Feature Execute object bridge 和 TriggerComponent object source 是同一条协议宽口；不建议只缓存 EventBus 反射，不建议把 Feature 完整 lifecycle 泛型化。ObjectPool 降为 P1 小切片，只补 `IObjectPoolRuntime` 去 manager 反射；TargetSelector 降为 P1，必须先定义 `TargetQueryResult/Lease` ownership；Logger、ComponentRegistrar、Entity lifecycle `ToArray()` 降为 P2 或 profiler 驱动。
- **Evidence**: `design/ECS框架优化/1.拆箱装箱+GC优化/README.md`、`设计/README.md`、`设计/00-总览与AI-first裁决.md`、`02-EventBus动态object禁用设计.md`、`03-FeatureAbility上下文类型化设计.md`、`04-ObjectPool反射管理接口设计.md`、`05-TargetSelector集合分配与LINQ设计.md`、`06-Logger字符串与诊断分配设计.md`、`问题/00-总览.md`、`design/INDEX.md`、`README.md`、`roadmap.md` 已同步。
- **Impact**: 后续恢复不应再从 Data 或单独 Event SDD 开始；应先创建联合协议收口 SDD，再按 owner 拆 ObjectPool / TargetSelector / Logger。
- **Resume**: 若继续 GC/装箱优化，创建 `Event + Feature/Ability Typed Execution Boundary` SDD；Must Confirm 为是否接受联合切片、Feature 只类型化 Execute、TriggerComponent 改 typed trigger binding id。

### P044 — 2026-06-07 — sdd-0033-done

- **Context**: 用户确认非 Data GC 边界收口方向，并要求更新设计文档、生成 SDD、执行任务一起完成。用户明确 Data 已改完、Logger 本轮不改，ObjectPool 只处理明显问题，原则不是全仓清零 GC。
- **Conclusion**: SDD-0033 已完成非 Data 明显宽口收口：`EventBus` 删除 dynamic object 主链路；`FeatureContext` 新增 typed activation/result helper，`IFeatureHandler.OnExecute` 改 `void`，Ability 主链路通过 typed `CastContext` / `AbilityExecutedResult` 传递；`ObjectPoolManager` 改 `Dictionary<string, IObjectPoolRuntime>` 并删除 manager 反射；`TargetQueryEngine + TargetQueryResult` 提供 read-only items 和 diagnostics，Ability / AI 调用点迁移到新 facade。
- **Evidence**: `sdds/023-SDD-0033-non-data-gc-boundary-completion/`；`Src/ECS/Runtime/Event/EventBus.cs`、`Src/ECS/Capabilities/Feature/System/*`、`Src/ECS/Capabilities/Ability/System/*`、`Src/ECS/Tools/ObjectPool/*`、`Src/ECS/Tools/TargetSelector/*`；`DocsAI/ECS/Runtime/Event/`、`DocsAI/ECS/Capabilities/{Feature,Ability}/`、`DocsAI/ECS/Tools/{ObjectPool,TargetSelector}/` 和 owner skill 源已同步。
- **Research Adoption**: externalResources enabled=`official-docs, engine-framework`，scope=Context7 Bevy `Message` docs、Unity managed memory / garbage collector docs、`Resources/Engine/Docs/FrameworkAnalysis/Reports/99-SlimeAI-引擎源码综合分析报告.md` 与 EnTT/DefaultEcs/Bevy 相关 typed event / Capability-owned selector 片段；copiedCodeOrAssets=none；adoption=采纳 typed payload、减少临时分配、显式 ownership 和少入口原则，不复制外部 ECS runtime。
- **Impact**: 后续 AI 不应再建议只缓存 `EmitDynamic` 反射，也不应恢复 `object? OnExecute` 或 `Dictionary<string, object> _pools`；TargetSelector 后续性能深化必须从 `TargetQueryEngine` owner 继续，Logger 仍等待 profiler 或明确热路径证据。
- **Resume**: PRJ-0002 当前无 active 子 SDD；若继续同一设计包，优先选择 Logger hot path lazy message API、TargetQuery pooled lease / deterministic RNG，或 profiler 证据指向的局部 cleanup。
