# Session Adapter 二次审查与会话分析流程设计

> 日期：2026-06-10
> 状态：current
> 来源：用户要求检查最新生成的 ChatHistory / session-adapter 模式是否有利于 AI 分析，并继续分析 6 月 8-10 日对话中的效率和流程问题。
> 设计裁决：`transcript.visible.md` 保真方向成立；digest 层和 Retrospective 消费协议仍不够稳定，下一步应优先修正分类误判、当前会话定位、失败恢复判断和 actor/skill 读取流程。

## 用户原始问题

> SDD/project/projects/PRJ-0001-systemagent-optimization/design/会话记录适配器参考设计  
> SDD/project/projects/PRJ-0001-systemagent-optimization/design/会话记录适配器参考设计/2026-06-10-会话效率与自动化流程问题分析.md  
> SDD/project/projects/PRJ-0001-systemagent-optimization/design/会话记录适配器参考设计/2026-06-10-Session-Adapter效率优化与自动化流程改进.md  
> Workspace/SystemAgent/Tools/session-adapter  
> Workspace/DocsAI/ChatHistory/2026/06  
> /home/slime/.codex/AGENTS.md，这里是全局规则，一部分是通用的规则，另外一部分是其他的，要改的只有通用的，ClaudeCode是一样的，不用改这个规则，你去看一下有无问题  
> - 现在按照最新的要求生成了，你检查一下这种模式是不是有利于AI分析问题，你认为现在session-adapter做得怎么样，还有没有问题  
> - transcript.visible.md就是根据原文转换后的格式是吗，感觉还不错  
> - 你分析一下8-10日一些对话的内容，然后找问题，我发现每次对话的git diff次数过多，有问题，应该还有很多问题  
> - 然后现在session-adapter改了，对应的分析对话记录的skill也要改，你看看这个skill在哪，systemagent的actor应该也要更新  
> - 生成相关设计文档  
> - 这次提示词是以前的，有些问题已经改了，你最重要的目标是继续发现问题，然后记录下来，深度思考  
> - 深度思考详细分析，必要时可以搜索web,ctx7相关内容

## 核心判断

当前模式对 AI 分析是有帮助的，但还不能算完成。

`transcript.visible.md` 的方向是对的：它是从 Codex 原始 JSONL 转出的可见内容 Markdown，不是摘要；message、tool call、tool output、事件 payload 和 turn context 会以可读形式保留，隐藏推理只保留不可解密占位和 hash。也就是说，它适合当“可见证据层”，但不是字节级原文副本；需要字节级证据时仍要回到 `Source Path` 指向的原始 JSONL。

真正的问题在 digest 层。`derived/ai-context.md`、`summary.md`、`efficiency.md`、`tool-failures.md` 能明显降低 AI 读取成本，但当前仍会把噪声当目标、把中间状态当结果、把只读 SDD 命令当 edit，并且没有稳定告诉 Retrospective“当前会话 digest 在哪里”。这部分不需要兼容旧 digest；要改就应按新契约完整重建 schema、index、folder 和 derived 文件，旧 digest 只作为一次性迁移输入。所以现阶段的结论是：

- **保留**：`visible-transcript -> digest -> index.json` 三层结构。
- **修正**：命令分类、标题/目标提取、最终结论识别、失败恢复判断、当前会话定位；允许破坏性升级 index / digest schema。
- **暂不做**：自动 hook 常驻抓取、复制完整 JSONL 进仓库、改 Claude/Codex/OpenCode 原始 session。

## 本地证据范围

本次没有使用 web 或 Context7。原因是判断点来自本仓工具、规则和本地 Codex session 行为，不依赖外部 API 或当前版本文档。

已检查的本地事实源：

- `Workspace/SystemAgent/Tools/session-adapter/session_adapter.py`
- `Workspace/SystemAgent/Tools/session-adapter/README.md`
- `Workspace/DocsAI/ChatHistory/2026/06`
- `/home/slime/.codex/sessions/2026/06/08`、`09`、`10`
- `Workspace/SystemAgent/Actors/Retrospective.md`
- `.ai-config/skills/systemagent-skill/systemagent-retrospective/SKILL.md`
- `Workspace/SystemAgent/Rules/Git.md`
- `/home/slime/.codex/AGENTS.md`
- PRJ-0001 当前会话记录适配器设计文档和项目进度文档

6 月 8-10 日样本统计：

| 日期 | 原始 Codex JSONL | 当前仓 ChatHistory digest | 临时 10 日 digest | 发现 |
| --- | ---: | ---: | ---: | --- |
| 2026-06-08 | 4 | 4 | - | 已覆盖；高循环集中在 08:01、09:29 |
| 2026-06-09 | 12 | 10 | - | 缺 22:30 后 2 个源会话，ChatHistory 已开始滞后 |
| 2026-06-10 | 4 | 0 | 4 | 当前仓未生成 10 日 digest，只能临时分析 |

合并 8-10 日 18 个 digest 后的信号：

| 指标 | 数值 |
| --- | ---: |
| high priority 会话 | 15 |
| interrupted 会话 | 12 |
| failed tool calls 合计 | 142 |
| validation signals 合计 | 627 |
| edit signals 合计 | 567 |
| `verification_loops >= 3` | 9 |
| 最高 `verification_loops` | 25 |
| 缺失或旧版 efficiency 字段 | 1 |

这些数字能证明问题真实存在，但也暴露出一个重要限制：当前 efficiency 指标本身有误判，不能直接把 loops 数字当绝对事实。

## 真实问题

### 1. git diff 过多是症状，验证/检查节奏才是根因

用户关于 `git diff` 过多的判断成立，但它只是表象。8/9 的高循环会话中，AI 经常在一个小改动后连续运行 `git diff --check`、`git status --short`、`sdd.py validate`、build、lint 或场景命令。典型样本：

- 2026-06-09 21:04：589 次 tool call，128 次 validation signal，25 个 verification loop，20 个 failed tool call。
- 2026-06-09 17:45：506 次 tool call，101 次 validation signal，20 个 verification loop，19 个 failed tool call。
- 2026-06-09 18:25：session-adapter 自身改动，16 次 edit，34 次 validation，9 个 loop。

10 日规则更新后，样本最高 loops 降到 2，说明“批量修改后统一验证”的规则有实际效果。但 10 日仍有 9 个 failed tool call 的会话，说明低效不只来自 `git diff`，还来自错误路径、失败命令重试、patch 上下文不匹配和失败后恢复不清楚。

### 2. efficiency 指标高估了 verification loop

`session_adapter.py` 当前有两个会误导复盘的正则：

```python
VALIDATION_RE = re.compile(... r"git diff|git status|git log|"...)
CODE_EDIT_RE = re.compile(... r"python3 .*sdd\.py\b"...)
```

问题是 `CODE_EDIT_RE` 把所有 `python3 ... sdd.py` 都当成 edit。结果是 `sdd.py validate`、`sdd.py show`、`sdd.py project-show` 这种只读或验证命令也会成为“edit 起点”，再接几个验证命令就被计为 loop。9 日高循环样本里可以直接看到 `sdd.py validate`、`sdd.py show` 被列为 Edit Target。

所以当前结论要拆成两层：

- **行为问题真实存在**：反复验证、反复读取、失败命令重试确实消耗大量时间。
- **指标需要修正**：`verification_loops` 数字只能作为预警，不能作为精确 KPI。

### 3. digest 默认入口仍会误导 AI

`transcript.visible.md` 保真较好，但 AI 默认不会先读完整 raw transcript，而是读 `derived/ai-context.md`。当前 `ai-context.md` 有几类问题：

- 标题容易被 resume boilerplate 污染，很多会话标题变成 `A previous agent produced the plan below...`。
- `User Goal` 取最新用户请求，遇到 `continue` 或重复消息时会丢失真实目标。
- `Outcome` 取最后一条 assistant message，当前会话或中断会话里可能只是“我准备继续做”的中间状态。
- `final_conclusion` 的正则过宽，包含“验证/结果/完成”等字样的中间消息也可能被识别成最终结论。
- `user-requests.md` 和 `summary.md` 里存在相邻重复 user/assistant 事件，AI 会误以为用户重复催促更多次。

这说明 digest 层还不是“给 AI 的稳定入口”，更像是“第一版索引”。

### 4. tool failure 有记录，但不能解释原因

`derived/tool-failures.md` 能列出失败工具和 raw ref，但 `Recovered` 全是 `unknown`，失败原因只是一段截断 output。它不能回答这些更有价值的问题：

- 失败是环境缺失、路径错误、命令写错、构建真实失败、patch 上下文漂移，还是搜索无结果？
- 失败后是否重试成功？
- 失败是否影响最终交付？
- 哪些失败是预期探测，哪些是 AI 误操作？

例如 9 日 Log 会话里有多次 `dotnet build` 失败、`apply_patch` 上下文失败、`rg` 输出过大、错误 git boundary、Godot runner 目录错误。当前 failure 文件能列出它们，但 AI 仍要回 raw transcript 才能判断根因。

### 5. ChatHistory 手动生成导致证据滞后

当前仓 `Workspace/DocsAI/ChatHistory/2026/06` 没有 6 月 10 日 digest，6 月 9 日也缺 22:30 后两个原始 session。也就是说，Retrospective 说“如果存在就读 efficiency.md”不够，实际流程需要先回答：

1. 当前会话是否已经生成 digest？
2. 如果没有，是因为会话仍在进行、没有导出、还是 index 滞后？
3. 这次复盘应该读当前未结束会话、上一个完成会话，还是某个指定 session？

没有这个定位协议，actor/skill 即使写了“读取 efficiency.md”，AI 也经常找不到或读到旧证据。

### 6. SystemAgent git/push 规则存在残留冲突

`/home/slime/.codex/AGENTS.md` 的通用规则本身不需要改；它现在已经包含“编辑完成后统一 `git status --short`”“AI 可自动 commit 和 push”“完成 SDD task 默认更新 tasks/progress + commit + push”等新规则。

但 SystemAgent 内部还有旧约束：

- `Workspace/SystemAgent/Rules/Git.md` 仍写“默认不 push”。
- 多个 `Workspace/SystemAgent/Actors/*.md` 仍写“不 push；commit 仅在用户或当前策略明确允许...”
- `.ai-config/skills/ai/ai-feature-development/references/workflow-governance.md` 仍写 “push 必须用户明确确认”。

这不是全局规则文件的问题，而是 SystemAgent actor/policy 的残留冲突。它会造成同一任务里顶层规则允许自动 push，actor 又阻止 push。

## session-adapter 当前评价

### 做得好的部分

- 可见 transcript 作为证据层是正确方向。
- `digest-codex*` 不从 Markdown 反解析，而是直接解析 JSONL，方向正确。
- index v3 把 source locator、digest locator、gate、tool status、恢复信号放在一个入口里，适合 AI 快速筛选。
- `tool-failures.md`、`validation.md`、`efficiency.md` 的拆分方向对，至少让问题可见。
- 10 日临时 digest 证明当前工具能快速分析新会话，不需要额外外部依赖。

### 还不够好的部分

- 命令分类太粗，edit / validation / git inspection / read / SDD state write 混在一起。
- title、goal、outcome 的提取还不够 AI-first。
- failure 文件只有列表，没有 root-cause 分类和 recovered 判断。
- file read amplification 只有次数，没有 ranges、命令来源、文件/目录/符号类型区分。
- Retrospective 没有 current digest locator 协议。
- ChatHistory 没有 stale report，用户和 AI 不容易发现 9 日缺 2 个、10 日缺 4 个。
- 测试只覆盖生成 folder 和 tool failure 基础行为，没有覆盖 `efficiency.md`、SDD CLI 误判、重复 user request 去重、resume boilerplate 标题清理。

## 设计方向

### 方案 A：只修规则，不动 session-adapter

优点是改动小，10 日样本已经显示 loops 降低。

缺点是指标会继续误导复盘，Retrospective 仍然找不到当前会话 digest，tool failure 也不能解释原因。

结论：不推荐作为主方案，只能作为临时缓解。

### 方案 B：修 session-adapter 分类和 digest 质量

核心改动：

- 增加 `command_category`：`read`、`search`、`edit`、`sdd_state_write`、`validation`、`git_inspection`、`build`、`scene_test`、`commit`、`push`、`unknown`。
- `sdd.py validate/show/list/project-show` 不算 edit；`sdd.py task/note/start/block/done/new/design-import` 才算 state write。
- `git diff/status/log` 从 validation 中拆出 `git_inspection`，仍可统计次数，但不直接等同 build/test 验证。
- `verification_loop` 只从 `edit` 或 `sdd_state_write` 后的 validation/build/test/lint/scene-test 计算。
- title/goal/outcome 增加去噪：跳过 resume boilerplate、跳过纯 `continue`、去重相邻重复消息，Outcome 选择最后一个 final-like assistant，找不到则标为 `incomplete`。
- `tool-failures.md` 增加 `failure_category`、`retry_count`、`recovered`、`final_impact`。

结论：推荐，能解决当前 digest 误导问题。

### 方案 C：补 Retrospective / DeepThink 会话证据协议

核心改动：

- `systemagent-retrospective` skill 的“当前会话 digest”不能只写“如果存在”，要写定位步骤：
  1. 用户给 session id/path 时读指定 digest。
  2. 用户要求分析日期范围时先跑 `list-digests` 或临时 digest。
  3. 当前仓 ChatHistory 缺当天 digest 时，明确报告 stale，不假装已覆盖。
  4. 复盘当前正在进行的会话时，只能作为 partial evidence，最终复盘应在会话完成后重跑。
- `Workspace/SystemAgent/Actors/Retrospective.md` 增加 `sessionEvidence` 输出：digest path、source session、coverage、stale/missing、partial/current。
- `systemagent-deepthink` 增加一条会话证据读取提示：用户要求分析对话记录时，优先用 `session-adapter list-digests`、`ai-context.md`、`efficiency.md`、`tool-failures.md`，并说明覆盖范围。

结论：推荐，与方案 B 配套。

### 方案 D：立刻接自动 hook

不推荐。当前问题不是“有没有 hook”，而是 digest 质量和消费协议还不稳定。现在接 hook 会把误判自动传播到更多流程里。

更小的替代方案是新增手动命令或 close 阶段建议：

```bash
python3 Workspace/SystemAgent/Tools/session-adapter/session_adapter.py digest-codex-month --source-root /home/slime/.codex/sessions/2026/06 --skip-interrupted
python3 Workspace/SystemAgent/Tools/session-adapter/session_adapter.py list-digests --efficiency-loop 3
```

后续如果要自动化，应先做 advisory hook，只报告 stale / high loop / failed tools，不自动修改 ChatHistory。

## 推荐下一步

创建一个后续执行型 SDD，建议标题：

`Session Adapter Digest Accuracy and Retrospective Handoff`

范围：

1. 按新契约重建 session-adapter 命令分类和 verification loop 计算，不保留旧 efficiency 语义兼容。
2. 重建 title / user goal / outcome 提取逻辑，跳过 resume boilerplate、`continue` 和重复消息。
3. 重建 tool failure 输出，增加 failure category、recovered、retry count 和 final impact。
4. 重建 stale report：源 session 数量、index 覆盖数量、缺失 session id。
5. 必要时升级 `index.json` schema version，旧 index / old digest 不作为默认读取入口。
6. 更新 `systemagent-retrospective` skill 和 `Workspace/SystemAgent/Actors/Retrospective.md` 的会话证据定位协议。
7. 小幅更新 `systemagent-deepthink`：用户要求分析会话记录时必须说明 ChatHistory 覆盖范围。
8. 同步修正 SystemAgent GitPolicy / actor 中与新 push 规则冲突的残留表达，避免和顶层规则打架。
9. 补测试：SDD CLI validate/show 不算 edit、resume boilerplate 不做标题、重复 user request 去重、failure recovered 判断、stale report、schema 升级后旧格式不被默认读取。

不纳入第一批：

- 常驻 hook/watch。
- Claude/OpenCode 高保真 digest。
- LLM 自动摘要。
- 删除原始 Codex JSONL 或依赖原始 session 存储之外的证据；旧 ChatHistory digest / index 可在后续 SDD 中破坏性重建。

## 默认假设

- 不改 `/home/slime/.codex/AGENTS.md` 和 ClaudeCode 全局规则；它们作为外部通用规则只做对照。
- 修改统一源时仍只改 `.ai-config/` 和 `Workspace/SystemAgent/`，不直接改同步副本。
- 当前 10 日 digest 只作为临时证据，正式进入仓库前应由后续 SDD 明确生成策略。
- `transcript.visible.md` 继续作为可见证据层保留，不追求替代原始 JSONL。
- Session-adapter 后续重构默认不兼容旧 digest / index schema；旧产物可迁移或重建，但不维护长期 fallback。

## 需要确认

| 问题 | 为什么问 | 默认值 |
| --- | --- | --- |
| 后续是否创建执行型 SDD？ | 这次只做设计分析；实现会涉及工具、skill、actor、policy 和测试 | 创建 `Session Adapter Digest Accuracy and Retrospective Handoff` |
| ChatHistory 10 日是否现在写入仓库？ | 当前只有临时 digest；写入仓库会增加约 4.6 MB，并改变 `index.json` | 后续 SDD 再正式生成 |
| SystemAgent actor 的“不 push”旧约束是否同步改掉？ | 顶层规则已允许自动 commit/push，actor/policy 残留会冲突 | 改为“按 GitPolicy 和顶层规则执行，禁止 force push/历史改写” |
