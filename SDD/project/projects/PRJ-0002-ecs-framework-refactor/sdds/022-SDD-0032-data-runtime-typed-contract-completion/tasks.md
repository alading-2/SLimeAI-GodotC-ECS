# Tasks

## Progress

- **Status**: done
- **Completed**: 10/10
- **Current**: done

## Task List

- [x] T1.1 建立 SDD 入口、设计、任务、BDD 和验证记录
  - **Validation**: `python3 Workspace/SDD/sdd.py validate SDD-0032`
- [x] T1.2 补 DataOS descriptor / generated handle：CurrentEnergy、CurrentAmmo
  - **Validation**: `bash Data/DataOS/Tools/validate-dataos.sh Data/DataOS/Authoring/slimeainew.authoring.db`
- [x] T1.3 实现 typed source-aware write API，并迁移 TargetNode system write
  - **Validation**: DataRuntimeTestScene typed system write 场景
- [x] T1.4 实现 typed default cache 与可变默认值保护
  - **Validation**: DataRuntimeTestScene default cache / array clone 场景
- [x] T1.5 实现 typed computed resolver registry 和内置 resolver 迁移
  - **Validation**: computed cache、type mismatch、dirty propagation 场景
- [x] T1.6 实现 typed Data changed event，并迁移 Recovery / Lifecycle 业务监听
  - **Validation**: typed `Changed<float>` 场景；diagnostic `PropertyChanged` 仍可用
- [x] T1.7 实现 diagnostic snapshot API 并迁移 GetAll 调用点
  - **Validation**: grep gate 只剩 obsolete wrapper / diagnostic 例外
- [x] T1.8 实现 DataModifierSource typed source 并迁移 Feature 调用点
  - **Validation**: source add/remove、不同 source 隔离、duplicate id 回归场景
- [x] T1.9 更新 DocsAI、owner skill、Ability/Feature/Event/Data 文档与 SDD progress
  - **Validation**: `bash Workspace/Tools/ai-config-sync/sync-ai-config.sh` + skill-test
- [x] T1.10 运行完整验证、grep gate、Godot 场景并收尾 SDD
  - **Validation**: build / DataOS / SDD validate / skill lint / grep gate / Godot scenes
