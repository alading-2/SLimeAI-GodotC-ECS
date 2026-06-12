# Tasks

## Progress

- **Status**: blocked
- **Completed**: 18/24
- **Current**: T3.0 源码调用点语义化方向冻结

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

- [x] T2.1 analyzer digest contract：`logctl analyze` 生成 `summary.md`、更强 `ai-context.md`、`noise/top-contexts.md`、`missing-fields/index.md`、`flows/index.md`、`failures/index.md`
  - **Validation**: 对 `.ai-temp/log-runs/20260610-013907` 复跑 analyze，AI 默认入口不需要读取 raw JSONL 就能看到 status、top noise、missing semantic fields、flow digest 和下一步 query。

- [x] T2.2 gate status semantics：区分 `passed`、`failed`、`no-failure-observed`、`stdout-pattern-fallback`、`invalid-input`
  - **Validation**: 当前样本 `validationEntries=0` 且 `artifacts=0` 时不得输出 `status=passed`；invalid JSONL line 必须出现在 summary/gate warning。

- [x] T2.3 flow boundary and aggregation：修正 `flows` 识别规则，不再把普通 `operation` 归为 flow；高频成功 flow 支持 sample / aggregate / suppressed summary
  - **Validation**: 当前样本的 `flows/index.md` 不包含全部 4914 条普通 operation；TargetSelector 高频成功路径不再默认逐条作为 AI 入口。
  - **Remaining**: analyzer flow 边界已完成；TargetSelector / ObjectPool 运行时 aggregate summary 仍归 T2.5。

- [x] T2.4 semantic missing-fields：按 owner 检测 `fields:{}`、`operation==context`、缺 `entityId/reasonCode/durationMs/sourceFile` 等 AI 判断缺口
  - **Validation**: `missing-fields/index.md` 至少列出 Runtime/HealthBarUI、TargetSelector、ObjectPool、Damage、System 的字段任务和分类。

- [x] T2.5 owner hot-spot cleanup：按样本 top noise 先处理 TargetSelector、ObjectPool、HealthBarUI、Damage、System
  - **Validation**: 同类成功文本合并或降级；失败/阻断保留 structured fields；`logctl suggest --dry-run` 不再为同 owner/context/operation 重复 message 生成多条等价建议。
  - **Done**: HealthBarUI `HealthBarBind` 字段化、Damage `DamageProcess` 字段补强、Logger `OperationTrace` source/duration 字段、`suggest` 聚合、TargetSelector/ObjectPool/System 默认 budget 规则和 analyzer success template 已完成；失败、warning、skipped 路径不被预算吞掉。

- [ ] T2.6 Validation artifact adoption：承载样本或后续场景的验证事实进入 `ValidationSession` / artifact；没有 artifact 的 run 只能是 no-failure-observed
  - **Validation**: runner gate report 优先 artifact/Validation；legacy `PASS/FAIL` 文本只作为 fallback。

- [x] T2.7 verification and SDD sync：用真实样本和可用非 Godot 门禁验证 T2，并同步 DocsAI / design / SDD 状态
  - **Validation**: `node --check Workspace/Tools/logctl/logctl.mjs`；`node --test Workspace/Tools/logctl/tests/logctl-analyze.test.mjs`；测试覆盖 stale `by-owner/by-phase/flows.json` 清理和 `query --analysis-dir` 不 raw fallback；`Workspace/Tools/logctl/logctl analyze --run-dir .ai-temp/log-runs/20260610-013907 --out .ai-temp/log-runs/20260610-013907/analysis-semantic`；`Workspace/Tools/logctl/logctl query --analysis-dir .ai-temp/log-runs/20260610-013907/analysis-semantic owner=TargetSelector operation=TargetQueryEntities --format json`；`python3 Workspace/SDD/sdd.py validate SDD-0040`；`git diff --check`。

## T3 源码调用点语义化 Follow-up

- [ ] T3.0 方向冻结：确认 live stdout 默认策略、第一批迁移链路、Debug UI / TestSystem 默认可见性和是否继续归入 SDD-0040
  - **Validation**: `design/Tool/10.Log/第三部分-源码调用点语义化/README.md` 写清 Must Confirm、默认假设、推荐方案和不进入实现的门禁。

- [ ] T3.1 调用点盘点：按 owner 分类当前 `_log.*`、`GD.Print`、`Console.WriteLine`、测试说明和 Debug UI 输出
  - **Validation**: 产出 owner 级迁移清单，按流程型、验证型、高频成功型、启动快照型、Debug UI 型、真异常型分类；不能只提交 grep 原始列表。

- [ ] T3.2 Owner flow contract：为第一批 owner 固定 flow / summary / Validation / debug profile 契约
  - **Validation**: Runtime/System、Ability、Spawn、TargetSelector、ObjectPool、Test/Validation、TestSystem/Debug UI 的 README `## Log` 或 `Log.md` 与第三部分契约一致。

- [ ] T3.3 第一批源码迁移：迁移 MainTest / ECSTest、SystemManager、Ability + Spawn、TargetSelector + ObjectPool、TestSystem / Debug UI 的 live 可见调用点
  - **Validation**: 成功路径默认 summary 或模板聚合；失败路径保留 structured fields；测试断言进入 `ValidationSession`；没有机械全仓替换 `_log.Info`。

- [ ] T3.4 新 run 验收：用用户实际运行或有效 Godot runner 产生的新 run 验证 live stdout 和 analyzer 结果
  - **Validation**: live stdout 默认只包含 warn/error、validation、flow summary、run summary；`logctl analyze` 默认可读入口小于 raw；关键业务 flow 可判断成功/失败/跳过；没有 artifact 时仍不得 `passed`。
