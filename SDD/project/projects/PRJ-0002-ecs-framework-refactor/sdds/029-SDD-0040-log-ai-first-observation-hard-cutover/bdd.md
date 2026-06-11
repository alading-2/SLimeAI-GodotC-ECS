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
Then AI 先读 `summary.md`、`ai-context.md`、`flows/index.md`、`noise/templates.md`、`missing-fields/index.md`、owner `Log.md` 和目标 flow/failure
And 只有证据不足时才按 `rawRef` 或 `logctl query --file analysis/raw/entries.jsonl` 读取 raw。

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

### Scenario: Analyzer default output is smaller than raw

Given 一个 run 有几千行 raw JSONL
When `logctl analyze` 生成默认 analysis
Then `gate-report.json.analysisQuality.defaultReadableLines` 小于 `rawLines`
And 默认不生成或保留 stale `by-owner/`、`by-phase/` 或 `flows/flows.json`
And 高频成功路径进入 `noise/templates.jsonl`。

### Scenario: Query existing logs without rerun

Given 用户只想看某个 owner、sourceFile、operation 或 entityId 的日志
When 执行 `logctl query --analysis-dir <run>/analysis owner=Ability`
Then CLI 默认输出 flow conclusion 或 success template，而不是 raw entry 批量复制
And 如果语义索引为空，`query --analysis-dir` 返回空结果而不是回退 raw
And 需要 `sourceFile=...` 等原始字段下钻时，显式执行 `logctl query --file <run>/analysis/raw/entries.jsonl sourceFile=...`
And 不要求重新运行 Godot 场景。

### Scenario: Wall clock is opt-in

Given 默认 Log profile
When JSONL sink 写出一条 structured entry
Then entry 不包含旧 `timestampUtc`
And 默认也不包含 `wallClockUtc`
When profile 或 override 设置 `includeWallClockUtc=true`
Then entry 可包含 `wallClockUtc`
And AI 默认 analysis 不把 wall clock 当作主要时间基准。
