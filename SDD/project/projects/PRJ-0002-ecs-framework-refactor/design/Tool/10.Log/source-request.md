# Log 工具用户原始问题与去重提示词

> 更新：2026-06-10
> 状态：source request
> 作用：保留本轮 Log 设计更新的用户原始问题、去重后的关键意图和不可丢失约束。正文设计文档只引用本文件，不把长提示词全部塞进入口。

## 1. 原始问题摘录

用户要求对 Log 工具做 deepthink，并明确指出：

> 为什么没有对原本信息的整理，我感觉现在这样似乎更加麻烦了，信息太多了，你自己去看也能发现问题，设计文档要更新，要加上提示词，提示词可以总结应该有一些是重复的。

> 用 Logger 的设计文档的思想去分析现在的实际打印信息有哪些问题，怎么解决，我的本意是在前面加上 [] 这样，现在用 json 感觉也行，json 应该是更好的，json 可以被不同语言识别。

本轮重点样本：

```text
.ai-temp/log-runs/20260610-013907/raw/scene-log.jsonl
SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Tool/10.Log
```

## 2. 上一轮长提示词去重

上一轮提示词重复强调了这些点，重复本身说明优先级很高：

- Log 要参考 `SlimeAI-AiFirst/GameOS/Observation`、`DocsAI/ECS框架与AIFirst方向决策.md` 和现有 `Tool/10.Log`，按 AI-first ECS 思想重新设计。
- 允许完全重构；如果重构就不要为了兼容旧打印方式而折中。
- Log 等级需要重新思考，`Success` / `PASS` / `FAIL` 不能继续混在 severity 里。
- 开关不能只靠提高 level；需要 profile 文件、CLI 临时控制和 AI 建议回写。
- 打印信息必须对 AI 分析有帮助，没必要的信息应该默认关闭或聚合。
- Log、Debug、Test/TDD、Validation artifact 是一组系统；当前测试里散乱的 `PASS` / `FAIL`、`GD.Print`、`GD.PushError`、`throw` 需要统一。
- 日志几秒就几百条，不能把全量打印直接丢给 AI；必须先由脚本整理、拆分、摘要，再按固定流程分析。
- 日志需要分阶段，优先使用游戏运行时长、frame、physicsFrame，而不是默认墙钟时间。
- 过程执行完后输出完整 flow summary，比每个代码位置散点打印更适合 AI。
- 每个 Runtime / Capability / Tools / UI owner 都要写自己的 `Log.md` 或 README `## Log`，说明打什么、为什么打、怎么分析、哪些默认关闭。
- `godot-scene-test` 只负责运行场景、保存 run dir、调用 Log CLI；日志整理不应由测试 skill 维护。
- 用户主动运行游戏时也要保留 run dir / JSONL / artifact，再运行 `logctl analyze`，不要复制整段 console 给 AI。
- `logctl` 既要支持运行时控制，也要支持对已经整理好的日志二次查询，例如按 owner、sourceFile、operation、entityId、severity 筛选。
- 预算规则必须解释清楚：它限制日志输出、记录、展开和展示，不限制游戏代码执行次数。
- C# structured sink 更适合 AI-first；详细事实写 buffered JSONL，stdout 只写摘要，Godot editor sink 默认关闭。

## 3. 去重后的核心意图

1. **不要再让 AI 直接读全量日志**：raw log 只能是事实源，必须先由 `logctl analyze` 整理成 `summary / ai-context / by-owner / by-phase / flows / failures / noise / missing-fields`。
2. **JSONL 是正确方向，但不等于信息整理完成**：JSON 解决机器可读，不能自动解决字段语义、重复噪声、过程聚合和 owner 分析规则。
3. **过程优先于散点**：技能释放、伤害结算、目标查询、对象池租还等必须输出 flow summary 和结构化 step，AI 不应自己从几百行里拼流程。
4. **Log 和 Test 必须统一**：`PASS` / `FAIL`、`GD.Print`、`GD.PushError`、`throw`、`Log.Error("[FAIL]")` 不能继续作为分裂事实源；测试结果进入 Validation artifact。
5. **Log 控制必须可复现**：profile 文件是稳定事实源，CLI 是临时覆盖和离线查询，AI suggestion 只能生成可审查建议。
6. **预算是日志输出预算，不是游戏逻辑预算**：它限制 stdout/jsonl 展开和采样，不能影响代码执行次数。
7. **默认 sink 应是 C# structured sink**：详细事实写 buffered JSONL，stdout 只写摘要，Godot editor sink 默认关闭。
8. **每个 owner 都要写 Log 分析规则**：owner `Log.md` 要说明打什么、为什么打、怎么从 analyzer 目录判断是否有问题、哪些默认关闭。

## 4. 本轮必须回答的问题

- 当前 `.ai-temp/log-runs/20260610-013907/raw/scene-log.jsonl` 的真实问题是什么？
- 为什么“已经是 JSON”仍然让人感觉更麻烦？
- `logctl analyze` 现在整理得够不够？不够在哪里？
- 设计文档应该如何改，才能把“原始信息整理”和“AI 分析流程”写清楚？
- `godot-scene-test` 和 Log CLI 的职责边界如何写，避免测试 skill 再维护第二套 analyzer？

