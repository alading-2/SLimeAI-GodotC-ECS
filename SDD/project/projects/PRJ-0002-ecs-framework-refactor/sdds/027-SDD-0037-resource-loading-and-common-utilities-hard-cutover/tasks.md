# Tasks

## Progress

- **Status**: pending
- **Completed**: 0/11
- **Current**: T1.1

## Task List

- [ ] T1.1 Readiness baseline
  - **Scope**: 记录当前 `ResourceManagement`、`ResourceCatalog`、`ResourceGenerator`、`CommonTool.LoadPackedScene` 调用点、direct load grep、resource-path-migration skill 状态。
  - **Validation**: `git status --short`；resource grep gates；`python3 Workspace/SDD/sdd.py validate SDD-0037`。

- [ ] T1.2 Resource loading contract tests first
  - **Scope**: 为 strict lookup、missing key、wrong category、LoadPath source required、structured result 写 RED tests。
  - **Validation**: tests 先红后绿；失败原因可结构化读取。

- [ ] T1.3 ResourceLoading facade implementation
  - **Scope**: 新增或重命名 ResourceLoading facade、request/result/source DTO；保留 ResourceCatalog / ResourceGenerator 作为 catalog / diagnostics 工具。
  - **Validation**: public 示例使用 ResourceLoading；旧 ResourceManagement 管理器心智不作为 DocsAI current 入口。

- [ ] T1.4 Remove contains fallback
  - **Scope**: 删除 `Load<T>` contains fallback；精确 key/category 缺失走 diagnostics 或 structured fail-fast。
  - **Validation**: fallback grep gate 清零；重复/近似 key 不静默加载。

- [ ] T1.5 LoadPath source policy and PackedScene migration
  - **Scope**: `LoadPath` 增加 source/owner/usage；迁移 `CommonTool.LoadPackedScene` 调用点到 ResourceLoading；删除或 internal 化 CommonTool。
  - **Validation**: `rg "CommonTool\\."` 业务路径清零；DataOS resource ref 调用能说明 source。

- [ ] T1.6 ResourceCatalogDiagnostics
  - **Scope**: 新增 duplicate key、missing path、stale generated source、selected DataOS refs loadable diagnostics。
  - **Validation**: diagnostics artifact 或 CLI 输出可被测试读取；不进入 gameplay 热路径全量刷新。

- [ ] T1.7 ResourceGenerator gate
  - **Scope**: 补生成器 stale check 或至少在 docs/validation 中固定 generator 命令和旧路径残留检查。
  - **Validation**: `dotnet run --project Tools/ResourceGenerator/ResourceGenerator.csproj`。

- [ ] T1.8 Common Utilities owner setup
  - **Scope**: 建立 `Src/ECS/Tools/CommonUtilities/`、`DocsAI/ECS/Tools/CommonUtilities/`、manifest/README/禁止项；迁移或拒绝可归明确 owner 的 helper。
  - **Validation**: Common Utilities 中没有资源加载、Entity 查询、Timer、TargetSelector 等 owner 功能。

- [ ] T1.9 DocsAI and skill sync
  - **Scope**: 更新 ResourceManagement/ResourceLoading、CommonUtilities、ResourceGenerator、resource-path-migration 文档和 `tools` skill。
  - **Validation**: `bash Workspace/Tools/ai-config-sync/sync-ai-config.sh`；`bash Workspace/SystemAgent/Tools/skill-test/lint.sh static all --no-fail --summary-only`。

- [ ] T1.10 Build and migration smoke
  - **Scope**: 运行 build、ResourceGenerator、migration dry-run、SDD validate；如 DataOS refs 受影响，跑 DataOS validator。
  - **Validation**: `dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly`；`bash Data/DataOS/Tools/validate-dataos.sh Data/DataOS/Authoring/slimeainew.authoring.db`。

- [ ] T1.11 Closeout
  - **Scope**: 回填 SDD tasks/progress/bdd、项目 roadmap/progress；记录 UID / game-local catalog 是否需要后续 SDD。
  - **Validation**: `python3 Workspace/SDD/sdd.py validate SDD-0037`；`git diff --check`。
