# BDD

## Applicability

- **Required**: true
- **Reason**: 本 SDD 改 Log runtime、Validation artifact、Godot scene runner 分析和 AI 调试流程，必须用行为场景约束结果来源、结构化字段和降噪策略。

## Scenarios

### Scenario: Ability cast flow can be analyzed without full stdout

Given 一个 Ability cast 成功执行
When runner 调用 `logctl analyze`
Then `analysis/flows/AbilityCast/` 能看到目标选择、资源消耗、效果生成、冷却更新和最终 outcome
And stdout 只显示一条 `[FLOW:AbilityCast]` 摘要和关键 warn/error

### Scenario: Validation failure is structured

Given 一个 Validation scene 的断言失败
When runner 生成 gate report
Then result source 是 `artifact` 或 `structured-log`
And failure reason 包含 checkName、expected、actual、reasonCode
And 结果不依赖 `[FAIL]` 文本 pattern。

### Scenario: High frequency logs are budgeted

Given Movement collision 或 TargetQuery 高频重复日志超过预算
When 日志写入 stdout 和 JSONL
Then stdout 只输出 suppressed summary
And JSONL 保留 sample、suppressedCount、budgetKey 和 reason
And 游戏逻辑执行次数不受日志预算影响。

### Scenario: AI analysis starts from digest

Given 一次失败 run 已生成 `analysis/`
When AI 分析问题
Then AI 先读 `summary.md`、`ai-context.md`、owner `Log.md` 和目标 flow/failure
And 只有证据不足时才读取 `raw/scene-log.jsonl`。

### Scenario: Run without validation artifact is not reported as passed

Given 一个 run 只有 structured JSONL
And `validationEntries=0`
And `artifacts=0`
When `logctl analyze` 生成 gate report
Then status 不是 `passed`
And report 明确标记为 `no-failure-observed` 或包含 invalid-input warning
And `summary.md` 说明“没有发现失败”不等于“行为验证通过”。

### Scenario: Analyzer exposes semantic log gaps

Given raw JSONL 中存在大量 `fields:{}` 或 `operation == context`
When `logctl analyze` 生成 `missing-fields/index.md`
Then missing-fields 不只检查 envelope required fields
And 按 owner/context/operation 输出 `Log gap` 任务
And 至少指出缺 `entityId`、`reasonCode`、`durationMs` 或稳定 operation 的 owner。

### Scenario: Flow index excludes ordinary operations

Given raw JSONL 每条 entry 都有 `operation`
When `logctl analyze` 生成 `flows/index.md`
Then 普通 runtime operation 不自动进入 flow
And flow 只来自 `channel=Flow`、明确 `entryType` 或完整 OperationTrace 契约
And 高频成功 flow 以 summary / sample / aggregate 呈现，不让 AI 默认读全部 completion。

### Scenario: Query existing logs without rerun

Given 用户只想看某个 owner、sourceFile、operation 或 entityId 的日志
When 执行 `logctl query --analysis-dir <run>/analysis owner=Ability` 或 `sourceFile=...`
Then CLI 输出筛选后的摘要和原始 JSONL 引用
And 不要求重新运行 Godot 场景。
