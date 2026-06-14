# Tasks

## Progress

- **Status**: pending
- **Completed**: 0/9
- **Current**: T1.1

## Task List

- [ ] T1.1 Readiness：确认 Data runtime、DocsAI、DataOS 场景和现有 dirty workspace 基线
  - **Validation**: `git status --short`、读取 `DataComputeRegistry.cs` / `DataDefinitionCatalog.cs` / `RuntimeDataSnapshotLoader.cs` / `DataRuntimeBootstrap.cs` / `DocsAI/ECS/Runtime/Data/Data系统说明.md`
- [ ] T2.1 实现 `DataComputeRegistry.Default` 默认单例和 `Freeze()` / `IsFrozen`
  - **Validation**: default registry 包含框架内置 resolver，frozen 后不能注册；custom registry 仍可 new + register + 注入 bootstrap
- [ ] T3.1 收窄 `DataComputeRegistry` 职责
  - **Validation**: 移除 registry 对 `DataDefinition` / descriptor value type 的依赖；typed resolver 获取仍能 fail-fast
- [ ] T4.1 引入 catalog build report / result，统一 computed 校验 owner
  - **Validation**: duplicate key、missing computeId、missing resolver、output mismatch、missing dependency、computed cycle 均产出稳定 code
- [ ] T5.1 删除 `RuntimeDataSnapshotLoader` 的重复 computed binding 校验
  - **Validation**: loader 只做 DTO 解析、默认值转换和 record apply；catalog build 负责跨 descriptor / resolver 校验
- [ ] T6.1 Bootstrap fatal report 先写 Log / observation 再 throw
  - **Validation**: catalog build fatal 有 `owner=Data operation=CatalogBuild` 记录；坏 catalog 不会继续运行
- [ ] T7.1 更新 DataOS / Godot Data 场景测试
  - **Validation**: 覆盖 default registry、自定义 registry、frozen duplicate、missing resolver、output mismatch、dependency missing、cycle、report/log 行为
- [ ] T8.1 同步 DocsAI 和 owner skill
  - **Validation**: `DocsAI/ECS/Runtime/Data/Data系统说明.md` 和 `.ai-config/skills/ecs/ecs-data/SKILL.md` 反映 registry / catalog / report 新边界；若改 skill，执行 ai-config sync + skill-test
- [ ] T9.1 最终验证与 SDD 收口
  - **Validation**: `dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly`、`bash Data/DataOS/Tools/validate-dataos.sh Data/DataOS/Authoring/slimeainew.authoring.db`、4 个 Data Godot scene、`python3 Workspace/SDD/sdd.py validate SDD-0044`
