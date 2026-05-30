# Tasks

## Progress

- **Status**: done
- **Completed**: 6/6
- **Current**: done

## Task List

- [x] T1.1 建立 SDD 入口、设计、任务和验证记录
  - **Validation**: `python3 Workspace/SDD/sdd.py validate SDD-0002`
- [x] T1.2 清理 `Workspace/SystemAgent` 顶层入口、workflow、role、protocol、policy、gate 中的 OpenSpec 默认入口引用
  - **Validation**: `grep` 残留引用；允许 `systemagent-catalog.yaml` 中 legacy compatibility 条目
- [x] T1.3 删除 SystemAgent OpenSpec 专属协议文件并更新 manifest
  - **Validation**: `Workspace/SystemAgent/Protocols/OpenSpecChangeProtocol.md` 与 `OpenSpecExecutionMemoryProtocol.md` 已删除，`Catalog/manifest.yaml` 不再登记
- [x] T1.4 更新 `.ai-config` rule / skill 源为 SDD-first，保留 `openspec/*` 显式兼容 skill
  - **Validation**: `bash Workspace/Tools/ai-config-sync/sync-ai-config.sh`
- [x] T1.5 同步生成副本并运行 skill-test
  - **Validation**: `bash Workspace/SystemAgent/Tools/skill-test/lint.sh static all --no-fail --summary-only`
- [x] T1.6 更新执行跟踪、SDD 设计、任务、BDD 和 progress
  - **Validation**: `python3 Workspace/SDD/sdd.py validate SDD-0002`
