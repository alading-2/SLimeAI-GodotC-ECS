# Tasks

## Progress

- **Status**: done
- **Completed**: 9/9
- **Current**: done

## Task List

- [x] T1.1 建立 SDD 入口、设计、任务和验证记录
  - 记录 selected workflow、must-read、git boundary、dirty baseline、Data-only 范围和默认假设。
  - **Validation**: `python3 Workspace/SDD/sdd.py validate SDD-0031`

- [x] T1.2 TDD RED: typed slot contract
  - 在 `DataRuntimeTestScene` 增加 typed hot path contract：typed `DataKey<T>` 写入后 storage 创建 typed slot，typed set 不走 untyped boundary counter。
  - **Validation**: 运行目标 Data runtime 测试或 `dotnet build`，预期先因缺少 typed slot contract/API 失败。

- [x] T1.3 Generic DataSlot and policy implementation
  - 用 `IDataSlot` + `DataSlot<T>` 替代 `DataSlot.Value object?`；`Data.Get/Set<T>` 直接走 typed slot。
  - **Validation**: T1.2 测试转绿，现有 get/set/default/range/allowed diagnostics 不回归。

- [x] T1.4 Modifier pipeline typed storage
  - 将数值 modifier 有效值写回 typed slot，不再保存为 `object?`；覆盖 int/float/double。
  - **Validation**: `Data_AddModifier_ShouldApplyModifierPipeline` 和 change/dirty tests 通过。

- [x] T1.5 Computed resolver typed path and cache
  - computed cache 进入 typed computed slot 或 typed cache helper，移除 `_computedCache Dictionary<string, object?>`。
  - **Validation**: computed dependency、cache、transitive dirty tests 通过。

- [x] T1.6 Boundary diagnostics and untyped API comments
  - 保留 loader/debug/TestSystem untyped 边界，补中文注释说明业务代码不要调用；diagnostic dump 不暴露为热路径。
  - **Validation**: snapshot apply / wrong type diagnostics 行为不变。

- [x] T1.7 DocsAI and owner skill sync
  - 更新 `DocsAI/ECS/Runtime/Data/Data系统说明.md` 和 `.ai-config/skills/ecs/ecs-data/SKILL.md` 源，说明 typed slot 当前状态和后续 Event/Feature 边界。
  - **Validation**: `bash Workspace/Tools/ai-config-sync/sync-ai-config.sh` + `bash Workspace/SystemAgent/Tools/skill-test/lint.sh static all --no-fail --summary-only`

- [x] T1.8 Validation gates
  - 运行 build、DataOS、SDD validate、grep gate；Godot 可用时运行 DataRuntimeTestScene。
  - **Validation**: 记录命令、结果和不可运行原因。

- [x] T1.9 Project SDD updates and closeout
  - 更新 PRJ-0002 roadmap/progress/README，完成 SDD progress/tasks/bdd，必要时 `done SDD-0031`。
  - **Validation**: `python3 Workspace/SDD/sdd.py validate SDD-0031`
