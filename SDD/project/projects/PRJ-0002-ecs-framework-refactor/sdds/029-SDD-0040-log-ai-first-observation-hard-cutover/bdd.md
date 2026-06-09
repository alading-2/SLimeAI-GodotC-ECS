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

### Scenario: Query existing logs without rerun

Given 用户只想看某个 owner、sourceFile、operation 或 entityId 的日志
When 执行 `logctl query --analysis-dir <run>/analysis owner=Ability` 或 `sourceFile=...`
Then CLI 输出筛选后的摘要和原始 JSONL 引用
And 不要求重新运行 Godot 场景。
