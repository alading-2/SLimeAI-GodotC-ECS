# Tasks

## Progress

- **Status**: pending
- **Completed**: 0/9
- **Current**: T1.1

## Task List

- [ ] T1.1 Readiness baseline
  - 记录 git boundary、dirty baseline、当前 System config/registry/execute 调用点、DocsAI 状态和现有验证基线。
  - **Validation**: `python3 Workspace/SDD/sdd.py validate SDD-0029`
- [ ] T1.2 DocsAI SystemManifest
  - 新增 `DocsAI/ECS/Runtime/System/SystemManifest.md`，覆盖当前 14 个 system 的 owner、源码、config、run condition、command handler、测试和风险。
  - **Validation**: `git diff --check -- DocsAI/ECS/Runtime/System`
- [ ] T1.3 DocsAI System README / Usage 同步
  - 更新 `README.md`、`Usage.md`，明确 System AI-first contract、manifest、preflight、diagnostics 和 SDD-0029 入口。
  - **Validation**: `rg -n "SystemManifest|SystemPreflight|SystemDiagnostics|SDD-0029" DocsAI/ECS/Runtime/System`
- [ ] T1.4 SystemPreflight contract
  - 实现 preflight report / issue，检查 config、registry、preset、dependencies、cycle 和 test-only descriptor allow-list。
  - **Validation**: SystemCoreRuntimeTest 覆盖 preflight 正向路径。
- [ ] T1.5 SystemDiagnosticsSnapshot
  - 实现 diagnostics DTO / builder，合并 config、registry、runtime 和 ProjectState；提供 stable blocked reason code。
  - **Validation**: diagnostics snapshot 可序列化为 JSON，并覆盖当前核心系统状态。
- [ ] T1.6 TestSystem integration
  - 让 TestSystem 系统信息模块复用 diagnostics contract，同时保留当前添加、移除、启禁用操作语义。
  - **Validation**: 现有 TestSystem system info 行为不回退。
- [ ] T1.7 Lifecycle trace and artifact dump
  - 增加生命周期 trace 或等价 ring buffer，并让 SystemCoreRuntimeTest 或 validation scene 输出 `.ai-temp/scene-tests/artifacts/system-core-diagnostics.json`。
  - **Validation**: JSON artifact 可解析，schemaVersion 和核心计数字段存在。
- [ ] T1.8 Full validation
  - 运行构建、DataOS validate、SDD validate、SystemCore Godot scene；缺 Godot runner 时记录 blocked。
  - **Validation**: `dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly` + DataOS validate + SDD validate + Godot scene 或 blocked 记录。
- [ ] T1.9 Docs / skill final sync
  - 根据实际实现同步 DocsAI；如新增 Runtime System owner skill，只改 `.ai-config/skills/` 并运行 ai-config sync 和 skill-test。
  - **Validation**: `bash Workspace/Tools/ai-config-sync/sync-ai-config.sh`（如改 `.ai-config`）+ `bash Workspace/SystemAgent/Tools/skill-test/lint.sh static all --no-fail --summary-only`
