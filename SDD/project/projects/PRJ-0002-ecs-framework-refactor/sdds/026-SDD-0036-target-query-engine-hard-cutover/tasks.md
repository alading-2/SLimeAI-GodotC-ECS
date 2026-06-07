# Tasks

## Progress

- **Status**: done
- **Completed**: 11/11
- **Current**: done

## Task List

- [x] T1.1 Readiness baseline
  - **Scope**: 记录当前 TargetSelector 源码、Ability/AI/Feature 调用点、NodeLifecycle 依赖、随机源、DocsAI current 示例和 SDD-0035 状态。
  - **Validation**: `git status --short`；TargetSelector grep gates；`python3 Workspace/SDD/sdd.py validate SDD-0036`。

- [x] T1.2 Query diagnostics tests first
  - **Scope**: 为 resolved origin/forward、candidate count、geometry hit、filter counts、sort/limit、warnings/errors 写 RED tests。
  - **Validation**: tests 先红后绿；空结果能解释原因。

- [x] T1.3 TargetQueryRequest / TargetQueryResult contract
  - **Scope**: 引入或修正 request/result/diagnostics DTO；明确 ownership，不返回可被调用方误改的内部 list。
  - **Validation**: public API 示例只使用 `TargetQueryEngine` / result diagnostics。

- [x] T1.4 Geometry validation and resolved origin/forward
  - **Scope**: Circle/Ring/Box/Line/Cone/Single 参数校验；几何过滤和排序统一使用 resolved origin/forward。
  - **Validation**: illegal query 输出 diagnostics error，不靠 null/empty list 猜原因。

- [x] T1.5 Candidate source abstraction
  - **Scope**: 增加 `ITargetCandidateSource` 或等价抽象；默认 source 接 EntityRegistry/显式 candidates；NodeLifecycle 只能作为内部/test source 或等待 SDD-0035 完成后移除。
  - **Validation**: Ability/AI/Feature 调用点不直接知道 NodeLifecycle。

- [x] T1.6 Deterministic RNG and random sorting
  - **Scope**: Random sorting、Position query、随机采样支持 seed/RNG 注入；移除 query 内部即时毫秒播种。
  - **Validation**: 固定 seed 下两次查询顺序一致。

- [x] T1.7 Team/type/lifecycle policy hardening
  - **Scope**: 阵营、类型、生命周期过滤收敛到共享策略或统一 fixture；缺 Data 字段排序时返回 warning。
  - **Validation**: TeamFilter / TypeFilter / Lifecycle 过滤计数正确。

- [x] T1.8 Ability / AI / Feature callsite migration
  - **Scope**: 迁移 `AbilityImpactTool`、AI find/wander action、Data/Feature ability handlers 和 tests。
  - **Validation**: `rg "EntityTargetSelector\\.Query|PositionTargetSelector\\.Query"` 当前业务路径清零或只剩历史说明。

- [x] T1.9 DocsAI and skill sync
  - **Scope**: 更新 `DocsAI/ECS/Tools/TargetSelector/` 和 `tools` skill；旧 list-only 示例降为历史迁移说明。
  - **Validation**: `bash Workspace/Tools/ai-config-sync/sync-ai-config.sh`；`bash Workspace/SystemAgent/Tools/skill-test/lint.sh static all --no-fail --summary-only`。

- [x] T1.10 Validation scenes and build
  - **Scope**: 跑 build；更新或新增 TargetSelector test scene / artifact；必要时补 README 五字段。
  - **Validation**: `dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly`；Godot runner 可用时运行 TargetSelectorTest。

- [x] T1.11 Closeout
  - **Scope**: 回填 SDD tasks/progress/bdd、项目 roadmap/progress；记录是否需要后续 spatial index / pooled lease SDD。
  - **Validation**: `python3 Workspace/SDD/sdd.py validate SDD-0036`；`git diff --check`。
