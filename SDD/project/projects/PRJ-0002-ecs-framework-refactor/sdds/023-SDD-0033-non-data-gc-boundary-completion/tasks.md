# Tasks

## Progress

- **Status**: done
- **Completed**: 8/8
- **Current**: done

## Task List

- [x] T1.1 建立 SDD 入口、设计、任务和验证记录
  - **Validation**: `python3 Workspace/SDD/sdd.py validate SDD-0033`
- [x] T1.2 写入 RED 测试与 grep baseline
  - **Validation**: `dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly` 预期失败，失败点来自新 typed API / runtime interface / query result API 尚未实现。
- [x] T1.3 收口 Event + Feature / Ability typed execution boundary
  - **Validation**: Ability pipeline 测试通过；grep 证明业务主链路不再写 `ActivationData =`、`ctx.ExecuteResult =` 或 `ExecuteResult is`。
- [x] T1.4 收口 ObjectPool manager runtime interface
  - **Validation**: ObjectPool contract runtime 测试通过；grep 证明 manager 不再通过 `GetMethod("Release"|"GetStats"|"Cleanup"|"Clear")` 反射调用。
- [x] T1.5 引入 TargetQueryResult / TargetQueryEngine ownership facade
  - **Validation**: TargetSelector 测试通过；新增 diagnostics 覆盖候选数、返回数和截断状态。
- [x] T1.6 同步 DocsAI 与 owner skill 源
  - **Validation**: `bash Workspace/Tools/ai-config-sync/sync-ai-config.sh`；`bash Workspace/SystemAgent/Tools/skill-test/lint.sh static all --no-fail --summary-only`。
- [x] T1.7 更新 PRJ-0002 roadmap / progress / README
  - **Validation**: `python3 Workspace/SDD/sdd.py validate SDD-0033`。
- [x] T1.8 完整验证与 SDD closeout
  - **Validation**: `dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly`；`bash Data/DataOS/Tools/validate-dataos.sh Data/DataOS/Authoring/slimeainew.authoring.db`；`python3 Workspace/SDD/sdd.py validate --all`；grep gates。
