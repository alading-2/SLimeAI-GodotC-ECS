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
| `ECS框架优化/0.ECS框架的思考/README.md` | ecs-concept-index | current | 2026-06-06 | ECS 框架概念层思考入口；不承接实现，聚焦 Data/Event 底层协议、AI-first 框架可行性和传统 ECS 采纳边界 |
| `ECS框架优化/0.ECS框架的思考/01-Data作为ECS框架核心的概念复盘与方案批判.md` | ecs-data-core-concept-review | current | 2026-06-06 | 深度复盘 Data 作为框架核心的概念定位；确认 `DataSlot<T> + IDataSlot` 方向正确，同时指出 typed policy、computed、change event、untyped 边界和高频索引仍需明确收口 |
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
| `01-Data系统问题分析.md` | data-analysis-legacy-entry | current | 2026-05-28 | 历史入口；完整 Data 设计已迁移到 `design/2.Data系统优化/` |
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
| `Tool/Timer/README.md` | timer-design-index | current | 2026-06-02 | Timer 最终架构裁决入口；保留 TimerManager facade，内部改纯 C# scheduler，补 debug diagnostics 和压力场景门禁 |
| `Tool/Timer/01-现状证据与AI-first裁决.md` | timer-research-decision | current | 2026-06-02 | 当前 TimerManager/GameTimer 热路径、Debug/压力验证缺口、外部计时器资料对照和采纳/拒绝裁决 |
| `Tool/Timer/02-目标架构与优化路线.md` | timer-architecture-roadmap | current | 2026-06-02 | 纯 C# TimerScheduler、min-heap、handle、owner/purpose、clock、主线程派发、debug diagnostics 和 timing wheel 后续触发条件 |
| `Tool/Timer/03-调用点迁移与验证计划.md` | timer-migration-test-plan | current | 2026-06-02 | Timer 调用点 owner/purpose 迁移、debug JSON、benchmark、TimerStressValidation 场景、scene-gate 和最终验证门禁 |
| `Tool/其他Tool/README.md` | other-tools-design-index | current | 2026-06-04 | Input/ObjectPool/Timer 已有设计且 Log 跳过后的剩余 Tools 设计包入口；2026-06-04 override：功能优先、可 hard cutover、不为旧 API 长期兼容。 |
| `Tool/其他Tool/01-现状证据与AI-first裁决.md` | other-tools-research-decision | current | 2026-06-04 | 基于 DocsAI、源码、调用点搜索、Godot 官方文档和 Resources 报告校准剩余 Tools；裁决按 RuntimeMountRegistry、TargetQueryEngine、ResourceLoading、NodeLifecycleRegistry、Common Utilities、MathFormula 功能切片 hard cutover。 |
| `Tool/其他Tool/02-CommonTool与ResourceManagement裁决.md` | common-resource-loading-decision | current | 2026-06-04 | 裁决 `CommonTool.LoadPackedScene` 迁入资源加载；Common Utilities 概念保留但需独立位置、manifest、禁止项和测试；ResourceManagement 走 strict loading、source policy、structured result 和 catalog diagnostics。 |
| `Tool/其他Tool/03-Math目标架构与验证.md` | math-tool-architecture-validation | current | 2026-06-04 | 裁决 Math 功能保留但不保旧 MyMath 聚合类；纯几何留 Geometry2D，业务公式拆 owner，概率/采样支持 deterministic RNG。 |
| `Tool/其他Tool/04-NodeLifecycle与ParentManager边界.md` | node-lifecycle-parent-manager-boundary | current | 2026-06-04 | 裁决 ParentManager 功能升级为 RuntimeMountRegistry / SceneMountRegistry；NodeLifecycle 只保底层 registry，业务查询 hard cutover 到 typed facade。 |
| `Tool/其他Tool/05-TargetSelector查询契约.md` | target-selector-query-contract | current | 2026-06-04 | 裁决 TargetSelector 升级为 TargetQueryEngine / TargetQueryResult；补 query validation、resolved origin/forward、candidate source、deterministic RNG、safe sorting 和 diagnostics。 |
| `Tool/其他Tool/06-实施路线与验证门禁.md` | other-tools-roadmap-validation | current | 2026-06-04 | 给出 RuntimeMountRegistry、TargetQueryEngine、ResourceLoading、NodeLifecycleRegistry、Common Utilities、MathFormula hard cutover SDD 拆分、BDD、grep gate 和验证命令。 |
| `Tool/其他Tool/07-2026-06-04-AI-first完全重构校准.md` | other-tools-hard-cutover-override | current | 2026-06-04 | 用户最新裁决落点：AI-first 功能优先、代码可丢弃、必要时完全重构；记录 Must Confirm、默认假设、Research Adoption 和执行前 override。 |
| `Tool/其他Tool/08-2026-06-04-用户裁决后执行前复核.md` | other-tools-user-review-execution-check | current | 2026-06-04 | 用户截图和最新答复后的通俗复核；确认 `/root/SlimeAIRuntime`、资源 strict fail-fast，补 TargetSelector 重构步骤、Common Utilities 保留规则和 NodeLifecycle Runtime 归属问题。 |
| `13-旧ECS框架Event系统问题分析与优化方向.md` | event-analysis | current | 2026-05-26 | Event 字符串主键、GameEventType、EventContext、GlobalEventBus 和订阅生命周期问题 |
| `03-字符串键名统一问题分析.md` | cross-cutting-analysis | current | 2026-05-26 | Data/Event/Relationship/Resource 中字符串变量名不统一的共性问题 |
| `04-优化优先级与SDD拆分建议.md` | roadmap-input | current | 2026-05-28 | 后续按问题域拆 SDD；Data 第一切片改为 Full Rewrite Catalog TDD |
| `05-DocsAI集中式ECS文档目录方案.md` | docs-layout-decision | current | 2026-05-30 | v2：DocsAI/ 统一文档目录方案；覆盖目录结构、迁移映射、AI 路由、分阶段执行计划 |
| `14-Event调用方式类型安全优化-执行提示词.md` | execution-prompt | current | 2026-05-26 | 删 const string、payload 做主键的执行步骤和代码变换示例 |
