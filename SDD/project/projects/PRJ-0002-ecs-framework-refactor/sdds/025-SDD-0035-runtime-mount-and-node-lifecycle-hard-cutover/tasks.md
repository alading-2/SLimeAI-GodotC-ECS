# Tasks

## Progress

- **Status**: pending
- **Completed**: 0/10
- **Current**: T1.1

## Task List

- [ ] T1.1 Readiness baseline
  - **Scope**: 记录 git boundary、dirty baseline、ParentManager / NodeLifecycle / EntityManager / UIManager / ObjectPool 调用点和当前 DocsAI 漂移。
  - **Validation**: `git status --short`；相关 `rg` gates；`python3 Workspace/SDD/sdd.py validate SDD-0035`。

- [ ] T1.2 Runtime mount tests first
  - **Scope**: 为 mount manifest、重复 mount id、deferred root pending、in-tree 状态和 invalid mount diagnostics 写 RED tests 或最小验证场景。
  - **Validation**: tests 先红后绿；不会依赖人工观察 SceneTree。

- [ ] T1.3 RuntimeMountRegistry implementation
  - **Scope**: 新增 Runtime mount 类型、manifest、registry、snapshot 和 diagnostics；默认 root `/root/SlimeAIRuntime`。
  - **Validation**: mount snapshot 覆盖 pending / in-tree / invalid；重复创建幂等。

- [ ] T1.4 ParentManager hard cutover
  - **Scope**: 迁移 SystemManager、EntityManager、ObjectPool parent 获取路径；删除或 internal 化旧 `ParentManager.GetOrRegister(name, path)` 和 `ParentNames` current API。
  - **Validation**: `rg` gate 只剩历史说明或 0 命中，具体例外必须写入 progress。

- [ ] T1.5 NodeLifecycleRegistry tests first
  - **Scope**: 为 owner/source metadata、duplicate register、unregister、invalid cleanup、snapshot by owner/type 写 RED tests。
  - **Validation**: tests 先红后绿；snapshot 字段稳定。

- [ ] T1.6 NodeLifecycle runtime migration
  - **Scope**: 将 NodeLifecycle 迁到 Runtime 语义和路径；Entity/UI/Component/Test 注册点传 owner/source。
  - **Validation**: Entity/UI/Component 注册来源可区分；旧 DocsAI Tools/NodeLifecycle 不再作为 current 入口。

- [ ] T1.7 Remove business global scan dependency
  - **Scope**: 业务路径删除 `GetAllNodes` / `GetNodesByInterface` 全局扫描依赖；TargetSelector 如需临时读取，必须写为后续 SDD-0036 candidate source TODO，不作为 public API。
  - **Validation**: `Src/ECS/Capabilities` 和 `Src/ECS/Tools/TargetSelector` 中全局扫描 grep gate 清零或只剩明确迁移说明。

- [ ] T1.8 DocsAI and skill sync
  - **Scope**: 更新 `DocsAI/ECS/Runtime/NodeLifecycle/`、Runtime/Mount 或 ParentManager current 文档；更新 `tools` / runtime owner skill 规则。
  - **Validation**: `bash Workspace/Tools/ai-config-sync/sync-ai-config.sh`；`bash Workspace/SystemAgent/Tools/skill-test/lint.sh static all --no-fail --summary-only`。

- [ ] T1.9 Runtime validation and optional Godot scene smoke
  - **Scope**: 运行 build；如新增/改 Godot validation scene，补 README 五字段和 PASS artifact。
  - **Validation**: `dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly`；必要时运行 game runner。

- [ ] T1.10 Closeout
  - **Scope**: 回填 SDD tasks/progress/bdd、项目 roadmap/progress、DocsAI 入口；记录 SDD-0036 前置状态。
  - **Validation**: `python3 Workspace/SDD/sdd.py validate SDD-0035`；`git diff --check`。
