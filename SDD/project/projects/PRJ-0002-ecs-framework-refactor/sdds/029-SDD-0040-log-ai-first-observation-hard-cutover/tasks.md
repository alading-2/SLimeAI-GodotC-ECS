# Tasks

## Progress

- **Status**: blocked
- **Completed**: 12/19
- **Current**: T2.1 analyzer digest contract

## Task List

- [x] T1.1 Readiness baseline：确认 dirty worktree、读取 Logger/Validation/runner/DocsAI/skill 现状，记录 grep 命中和执行风险
  - **Validation**: `python3 Workspace/SDD/sdd.py validate SDD-0040`
- [x] T1.2 Logger core TDD：新增 `LogEntry`、`Severity/Outcome/ValidationStatus`、run elapsed/frame/phase 字段、profile/rule/budget 基础测试
  - **Validation**: 目标 Logger tests 先 RED 后 GREEN；`dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly`
- [x] T1.3 Sink hard cutover：实现 C# `StdoutSummarySink`、`JsonlBufferedFileSink`、`MemorySink`、`ArtifactSink`、optional `GodotEditorSink`，默认关闭 Godot editor sink
  - **Validation**: JSONL 每行 envelope 可解析；stdout 只包含 summary / validation verdict /关键 warn-error
- [x] T1.4 ValidationSession：建立 `CheckResult` / artifact 主事实源，迁移 Logger/Data/System/Entity 第一批测试 PASS/FAIL
  - **Validation**: artifact 包含 expectedInputs / expectedObservations / passCriteria / failCriteria / checks / failures
- [x] T1.5 Log CLI analyzer/query：实现或接入 `logctl analyze/query/ingest/suggest --dry-run` 最小可用版本
  - **Validation**: sample JSONL/run dir 生成 `analysis/raw/by-owner/by-phase/flows/failures/noise/missing-fields/ai-context.md`
- [x] T1.6 godot-scene-test wrapper：runner 优先 artifact / structured-log / exit-code，stdout pattern 仅 fallback 并写 `resultSource`
  - **Validation**: `rg -n "FAILURE_PATTERNS|\\[PASS\\]|\\[FAIL\\]|FAIL:" .ai-config/skills/godot/godot-scene-test/scripts` 命中均标记为 fallback 或已删除
- [x] T1.7 第一批 owner flow：Ability/Damage/TargetSelector/ObjectPool/Timer/System/Validation 关键过程接入 `OperationTrace` 或写明暂缓原因
  - **Validation**: flow summary 进 stdout，完整 step 进 JSONL/artifact；高频日志有 budget/suppressed summary
- [x] T1.8 Owner Log 文档：补 `DocsAI/ECS/**/Log.md` 或 README `## Log`，至少覆盖 Logger、Data、System、Entity、Ability、Damage、Movement、ObjectPool、Timer、TargetSelector
  - **Validation**: owner docs grep gate 通过，文档说明 flow、字段、预算、sink 和分析流程
- [x] T1.9 AI 配置同步：更新 `.ai-config/skills/godot/godot-scene-test` 和必要 owner skill 源，运行同步和 skill-test
  - **Validation**: `bash Workspace/Tools/ai-config-sync/sync-ai-config.sh`；`bash Workspace/SystemAgent/Tools/skill-test/lint.sh static all --no-fail --summary-only`
- [x] T1.10 最终验证与收口：运行构建、DataOS、SDD validate、diff check 和可用的 Godot scene smoke；更新 PRJ-0002 状态
  - **Validation**: `dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly`；`bash Data/DataOS/Tools/validate-dataos.sh Data/DataOS/Authoring/slimeainew.authoring.db`；`python3 Workspace/SDD/sdd.py validate SDD-0040`；`git diff --check`

- [x] T1.11 补齐 Config/Log profile/rules/overrides、runtime metadata 和 budget suppressed summary 控制面
  - **Validation**: `dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly`；Logger tests 覆盖 profile metadata 和 budget suppression；`Workspace/Tools/logctl/logctl profile show --config-dir Config/Log`

- [x] T1.12 补齐 logctl profile show / suggest profilePatch、DocsAI/skill 同步和用户质疑状态校正
  - **Validation**: `node --check Workspace/Tools/logctl/logctl.mjs`；合成 run 验证 `analyze/query/suggest --dry-run`；`bash Workspace/Tools/ai-config-sync/sync-ai-config.sh`；`bash Workspace/SystemAgent/Tools/skill-test/lint.sh static all --no-fail --summary-only`

## T2 Log 整理闭环 Follow-up

- [ ] T2.1 analyzer digest contract：`logctl analyze` 生成 `summary.md`、更强 `ai-context.md`、`noise/top-contexts.md`、`missing-fields/index.md`、`flows/index.md`、`failures/index.md`
  - **Validation**: 对 `.ai-temp/log-runs/20260610-013907` 复跑 analyze，AI 默认入口不需要读取 raw JSONL 就能看到 status、top noise、missing semantic fields、flow digest 和下一步 query。

- [ ] T2.2 gate status semantics：区分 `passed`、`failed`、`no-failure-observed`、`stdout-pattern-fallback`、`invalid-input`
  - **Validation**: 当前样本 `validationEntries=0` 且 `artifacts=0` 时不得输出 `status=passed`；invalid JSONL line 必须出现在 summary/gate warning。

- [ ] T2.3 flow boundary and aggregation：修正 `flows` 识别规则，不再把普通 `operation` 归为 flow；高频成功 flow 支持 sample / aggregate / suppressed summary
  - **Validation**: 当前样本的 `flows/index.md` 不包含全部 4914 条普通 operation；TargetSelector 高频成功路径不再默认逐条作为 AI 入口。

- [ ] T2.4 semantic missing-fields：按 owner 检测 `fields:{}`、`operation==context`、缺 `entityId/reasonCode/durationMs/sourceFile` 等 AI 判断缺口
  - **Validation**: `missing-fields/index.md` 至少列出 Runtime/HealthBarUI、TargetSelector、ObjectPool、Damage、System 的字段任务和分类。

- [ ] T2.5 owner hot-spot cleanup：按样本 top noise 先处理 TargetSelector、ObjectPool、HealthBarUI、Damage、System
  - **Validation**: 同类成功文本合并或降级；失败/阻断保留 structured fields；`logctl suggest --dry-run` 不再为同 owner/context/operation 重复 message 生成多条等价建议。

- [ ] T2.6 Validation artifact adoption：承载样本或后续场景的验证事实进入 `ValidationSession` / artifact；没有 artifact 的 run 只能是 no-failure-observed
  - **Validation**: runner gate report 优先 artifact/Validation；legacy `PASS/FAIL` 文本只作为 fallback。

- [ ] T2.7 verification and SDD sync：用真实样本和可用非 Godot 门禁验证 T2，并同步 DocsAI / design / SDD 状态
  - **Validation**: `node --check Workspace/Tools/logctl/logctl.mjs`；`Workspace/Tools/logctl/logctl analyze --run-dir .ai-temp/log-runs/20260610-013907 --out .ai-temp/log-runs/20260610-013907/analysis-next`；`python3 Workspace/SDD/sdd.py validate SDD-0040`；`git diff --check`。
