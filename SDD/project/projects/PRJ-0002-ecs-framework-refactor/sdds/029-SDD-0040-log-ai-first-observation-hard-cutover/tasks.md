# Tasks

## Progress

- **Status**: pending
- **Completed**: 0/10
- **Current**: T1.1

## Task List

- [ ] T1.1 Readiness baseline：确认 dirty worktree、读取 Logger/Validation/runner/DocsAI/skill 现状，记录 grep 命中和执行风险
  - **Validation**: `python3 Workspace/SDD/sdd.py validate SDD-0040`
- [ ] T1.2 Logger core TDD：新增 `LogEntry`、`Severity/Outcome/ValidationStatus`、run elapsed/frame/phase 字段、profile/rule/budget 基础测试
  - **Validation**: 目标 Logger tests 先 RED 后 GREEN；`dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly`
- [ ] T1.3 Sink hard cutover：实现 C# `StdoutSummarySink`、`JsonlBufferedFileSink`、`MemorySink`、`ArtifactSink`、optional `GodotEditorSink`，默认关闭 Godot editor sink
  - **Validation**: JSONL 每行 envelope 可解析；stdout 只包含 summary / validation verdict /关键 warn-error
- [ ] T1.4 ValidationSession：建立 `CheckResult` / artifact 主事实源，迁移 Logger/Data/System/Entity 第一批测试 PASS/FAIL
  - **Validation**: artifact 包含 expectedInputs / expectedObservations / passCriteria / failCriteria / checks / failures
- [ ] T1.5 Log CLI analyzer/query：实现或接入 `logctl analyze/query/ingest/suggest --dry-run` 最小可用版本
  - **Validation**: sample JSONL/run dir 生成 `analysis/raw/by-owner/by-phase/flows/failures/noise/missing-fields/ai-context.md`
- [ ] T1.6 godot-scene-test wrapper：runner 优先 artifact / structured-log / exit-code，stdout pattern 仅 fallback 并写 `resultSource`
  - **Validation**: `rg -n "FAILURE_PATTERNS|\\[PASS\\]|\\[FAIL\\]|FAIL:" .ai-config/skills/godot/godot-scene-test/scripts` 命中均标记为 fallback 或已删除
- [ ] T1.7 第一批 owner flow：Ability/Damage/TargetSelector/ObjectPool/Timer/System/Validation 关键过程接入 `OperationTrace` 或写明暂缓原因
  - **Validation**: flow summary 进 stdout，完整 step 进 JSONL/artifact；高频日志有 budget/suppressed summary
- [ ] T1.8 Owner Log 文档：补 `DocsAI/ECS/**/Log.md` 或 README `## Log`，至少覆盖 Logger、Data、System、Entity、Ability、Damage、Movement、ObjectPool、Timer、TargetSelector
  - **Validation**: owner docs grep gate 通过，文档说明 flow、字段、预算、sink 和分析流程
- [ ] T1.9 AI 配置同步：更新 `.ai-config/skills/godot/godot-scene-test` 和必要 owner skill 源，运行同步和 skill-test
  - **Validation**: `bash Workspace/Tools/ai-config-sync/sync-ai-config.sh`；`bash Workspace/SystemAgent/Tools/skill-test/lint.sh static all --no-fail --summary-only`
- [ ] T1.10 最终验证与收口：运行构建、DataOS、SDD validate、diff check 和可用的 Godot scene smoke；更新 PRJ-0002 状态
  - **Validation**: `dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly`；`bash Data/DataOS/Tools/validate-dataos.sh Data/DataOS/Authoring/slimeainew.authoring.db`；`python3 Workspace/SDD/sdd.py validate SDD-0040`；`git diff --check`
