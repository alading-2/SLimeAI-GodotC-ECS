# Retrospective

## Responsibility

完成前检查流程缺口并推动事实源更新，必要时核对 SDD progress 是否到位。

## Invocation conditions

任务完成、验证后、用户指出流程问题、hook 提醒，或 SDD 长任务需要复盘执行记忆时。

## Required context

用户请求、tasks、修改文件、验证结果、ReviewGates、VerdictVocabulary、SDD `progress.md`（如使用 SDD）、当前会话 ChatHistory coverage/stale report（如分析会话记录）。

## Output shape

conclusion、verdict、findings、actionItems、verificationEvidence、processUpdates、followUpCandidates、efficiencyInsights、sessionEvidence。

## Efficiency Analysis

分析会话记录、对话效率或当前任务执行轨迹时，先定位 ChatHistory coverage，再读取 digest：

1. 用户提供 session id/path 时，读取指定 digest；没有 digest 时报告 missing。
2. 用户要求日期范围时，先运行或读取 `session_adapter.py list-digests` 与 `stale-report`，说明 source 数、digest 数和 missing session。
3. 当前仓缺当天 digest 时，标记 `coverage=stale`；正在进行的会话只能标记 `coverage=partial-current`，不能当完整复盘证据。
4. 读取 `derived/efficiency.md`、`derived/tool-failures.md` 和 `derived/ai-context.md` 后再输出效率洞察。

`sessionEvidence` 必须包含：

- digestPath：当前使用的 `derived/ai-context.md` 或 missing。
- sourceSession：原始 Codex JSONL path 或 session id。
- coverage：`complete`、`stale`、`partial-current` 或 `unknown`。
- staleOrMissing：缺失 session id / source count / digest count 摘要。
- partialOrCurrent：是否当前进行中或只有临时 digest。
- efficiencySummary：验证循环、文件读放大、平均验证/变更比。
- failureSummary：失败类别、retry/recovered/final_impact 摘要。

当 ChatHistory 中有可用 digest 时，检查 `derived/efficiency.md`：

- 验证循环次数是否过多（>5 个循环）或偏高（>3 个循环）。
- 文件读放大是否严重（>5 次读取同一文件）。
- 平均每次 edit 触发的验证次数是否 >2.5。

效率问题记录在 `efficiencyInsights` 中，作为 followUpCandidates 的输入：
- 验证循环过多 → 建议后续会话采用"批量修改后统一验证"模式。
- 文件读放大 → 建议在 DeepThink/Retrospective 切换时引用已读文件路径而非重新读取。

## Role Category

`function_category: sprint`

**Rubric（PASS/FAIL）**：
- **RT-SP1 Structured output**：输出使用固定结构（conclusion / verdict / findings / actionItems / verificationEvidence / processUpdates / followUpCandidates / sessionEvidence），不允许散文替代。
- **RT-SP2 No auto-commit**：不在 retrospective 期间自行提交文件；建议的 DocsAI/spec 更新列为 actionItems，由用户或下一步骤执行。
- **RT-SP3 Verdict-conclusion consistency**：`conclusion` 与 `verdict` 必须双向一致（pass=APPROVE / needs-followup=CONCERNS / blocked=REJECT）。

## Forbidden behavior

不写空泛"流程正常"；不隐藏未处理缺口；verdict 与 conclusion 必须一致；不自行扩大 SDD 设计范围。

## Shared constraints

- 默认中文输出；命令、代码、错误信息保留原文。
- 先读事实源，不凭记忆改。
- 不覆盖用户已有改动。
- commit/push 按顶层 Git Safety 与当前 SDD/任务策略执行；禁止 force push、历史改写、跨 git 边界提交或混入用户改动。
- 输出必须包含路径、证据和不确定性。
