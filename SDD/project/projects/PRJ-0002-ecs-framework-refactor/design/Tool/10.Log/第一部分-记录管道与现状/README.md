# Log 工具设计包

> 更新：2026-06-11
> 状态：recording-layer design package；整理层当前契约见 `../第二部分-语义提炼整理/03-最终设计与完成清单.md`
> 入口：`README.md`
> 裁决：Log 不是“输出文本”的工具，而是 AI-first 观测入口；必须同时服务运行调试、测试验证、scene runner 分析和人工排障。

## 0. 用户原始问题

本轮长提示词和去重摘要保存在：

```text
SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Tool/10.Log/第一部分-记录管道与现状/source-request.md
```

最核心的问题是：**当前日志虽然已经开始写 JSONL，但原始信息没有被整理成 AI 可直接分析的入口，导致信息更多、分析更麻烦。**

用户本意从 `[]` 前缀标记开始，升级到 JSONL 是正确方向；但 JSONL 只解决“机器能读”，还没有解决“AI 该先看什么、如何判断问题、哪些信息应该合并、哪些 owner 需要补字段”。

## 1. 本设计包回答什么

当前 Log 的主要问题不是“等级不够多”，也不是“有没有 JSON”。真实问题是：

- **语义结构化不充分**：虽然已有 JSONL envelope，但大量日志仍是 `fields:{}` 或 `operation == context`，AI 仍要读自然语言 message。
- **噪声太大**：运行几秒就几百条时，单靠 `Trace/Debug/Info/Warn/Error` 不足以控制可读性。
- **开关不灵活**：只靠全局等级或类级等级，难以在调试现场快速打开某个模块、关掉某类重复日志。
- **测试与日志分裂**：当前测试里同时存在 `GD.Print("PASS")`、`GD.PushError("FAIL")`、`_log.Error("[FAIL]")`、`throw Exception` 等做法，runner 又在用字符串 pattern 扫描失败信号，导致事实源不统一。
- **整理链路不够**：`raw/scene-log.jsonl` 不是 AI 分析入口；必须先生成摘要、flow 结论、成功模板、失败、噪声和缺字段报告。旧的 owner/phase raw 分桶只作为修复前历史证据，不是当前默认产物。

本设计包要解决的是：**把 Log 升级为 AI-first 观测层的入口，并把测试断言统一纳入同一套观测与 artifact 语义。**

## 2. 文件结构

| File | Role | 说明 |
| --- | --- | --- |
| `source-request.md` | source-request | 本轮用户原始问题摘录、去重提示词和必须回答的问题。 |
| `README.md` | design-index | 本文件。先说明真实问题、阅读顺序、边界和完成定义。 |
| `01-现状分析与AI-first裁决.md` | research-decision | 当前 `Log`、测试场景、runner、Observation 原型与噪声问题分析。 |
| `02-目标架构与数据契约.md` | architecture | 定义结构化 Log envelope、profile、sink、CLI 控制和噪声预算。 |
| `03-控制面与CLI设计.md` | control-surface | 定义 `logctl`、运行时覆盖、AI 建议、日志预算和快照回写。 |
| `04-测试统一与Observation接入.md` | validation-observation | 定义测试如何统一通过 Log/Validation artifact 表达 PASS/FAIL。 |
| `05-调用点迁移与验证计划.md` | migration-test-plan | 旧日志调用点、测试 helper、runner 规则和验证门禁。 |
| `06-功能OwnerLog文档与分析流程.md` | owner-log-analysis | 定义每个 Runtime / Capability / Tools / UI owner 的 `Log.md` 模板、过程聚合日志和脚本拆分分析流程。 |
| `07-当前样本日志问题与整理方案.md` | sample-analysis | 基于 `.ai-temp/log-runs/20260610-013907/raw/scene-log.jsonl` 的真实问题、为什么 T1 没完成“打印信息整理”、T2 analyzer/owner 实现路线和验收门禁。 |

## 3. 总裁决

采用 **AI-first Observation Log**：

```text
业务日志 / 调试日志 / 测试断言 / 场景验证
  -> 统一进入结构化 Log envelope
  -> 默认输出到 C# stdout summary / buffered jsonl file / memory / artifact
  -> 可选输出到 Godot editor sink
  -> 由 CLI 和 profile 控制可见性
  -> 由 AI 分析建议降噪和规则调整
```

同时采用 **Analyzer-first 消费规则**：

```text
raw/scene-log.jsonl
  -> logctl analyze
  -> analysis/summary.md + ai-context.md + flows + noise + missing-fields + failures
  -> AI 按 owner Log.md 分析
  -> 只在证据不足时 query，最后才显式读取 raw
```

不采用：

- 不把 Log 继续限定为“打印字符串 + 颜色”。
- 不把 `GD.Print("PASS")`、`GD.PushError("FAIL")`、`throw`、`Log.Error("[FAIL]")` 当成分裂的测试事实源。
- 不把全局等级当成唯一控制面。
- 不让 AI 继续从自然语言日志里猜实体、阶段和原因。
- 不把所有日志原样丢给 AI；AI 只能消费经过脚本切分、聚合、摘要和 owner 分析说明约束后的 artifact。
- 不把 `GD.PrintRich` / Godot Output 面板作为 AI-first 默认输出路径；它只适合人工编辑器调试，不适合作为高密度机器日志主链路。

### 3.1 Sink 裁决：默认避开 Godot API

用户指出“现在给 AI 分析，不需要 Godot 颜色显示，应直接用 C# 终端/文件输出”是正确方向。新的默认 sink 顺序应是：

| Sink | 默认 | 作用 | 说明 |
| --- | --- | --- | --- |
| `JsonlFileSink` | 开 | AI 主事实源 | 使用 C# `StreamWriter` / `FileStream` buffered 写入，每行结构化 JSON。 |
| `StdoutSummarySink` | 开 | runner 摘要和人类快速看结果 | 使用 C# `Console.Out` / `Console.WriteLine`，只输出 flow summary、validation verdict、关键 warn/error。 |
| `MemorySink` | 开 | ValidationSession / artifact | 供测试和 artifact 汇总读取。 |
| `ArtifactSink` | 开 | scene gate / analyzer | 写 checks、failureReasons、flow summaries、profile snapshot。 |
| `GodotEditorSink` | 默认关 | 编辑器人工调试 | 只在 profile 显式启用时调用 `GD.PrintRich` / `GD.PushWarning` / `GD.PushError`。 |

理由：

- Godot rich print 的优势是编辑器 Output 面板和颜色，不是 AI 分析。
- `GD.PrintRich` 会做 Godot API 调用、Godot Variant/String 路径和 BBCode / rich text 处理；这些对 AI JSONL 没有价值。
- C# stdout/file sink 更贴近 headless runner：runner 本来就捕获进程 stdout/stderr 和 artifact 文件。
- 但不能把“C# 打印”误解成“每条日志都 `Console.WriteLine`”。高密度日志默认应 buffered 写 JSONL，stdout 只打摘要，否则 stdout IO 仍会成为瓶颈。

结论：**AI-first 默认链路是 C# buffered JSONL + stdout summary；Godot API sink 只是可选人工调试桥。**

### 3.1.1 用户确认裁决：C# structured sink 优于 Godot API 默认打印

2026-06-09 用户确认：本问题的重点不是“是否把所有日志改成 C# 打印”，而是分析 **C# 输出链路是否比 Godot API 默认打印更适合 AI-first Log**。裁决如下：

- `GD.PrintRich` 默认打印不适合 AI-first；它服务 Godot editor 颜色和人工 Output 面板。
- `Console.WriteLine` 可作为 runner 摘要 sink，但不应承载高密度详细日志。
- 高密度详细事实应使用 C# buffered JSONL file sink。
- 因此推荐方案不是“C# 打印替代 Godot 打印”，而是 **C# structured sink 替代 Godot rich print 作为默认主链路**。

这个裁决后续作为执行型 SDD 的硬前提：Logger core 第一阶段必须先做 sink abstraction，默认启用 `JsonlBufferedFileSink + StdoutSummarySink + MemorySink + ArtifactSink`，默认关闭 `GodotEditorSink`。

### 3.2 Log 和分析必须分离

本设计把 Log 拆成两层：

| 层 | 固定职责 | 不做什么 |
| --- | --- | --- |
| Log | 固定结构化事实、过程聚合、profile/CLI 控制、JSONL/artifact 输出 | 不让 AI 临场决定这条日志该怎么写，不把分析结论混进原始事实。 |
| Log analysis | 由脚本把 raw log 提炼成 `summary.md`、flow conclusion、success template、failure、noise digest 和 missing-fields，再由 AI 按 owner `Log.md` 分析 | 不把全量顺序日志直接塞给 AI，不把 raw 复制成多个默认入口，不让每次分析流程自由发挥。 |

结论：**Log 是可复现数据管道，AI 分析是受文档约束的二次处理流程。**

## 4. 当前样本的关键发现

本轮对 `.ai-temp/log-runs/20260610-013907/raw/scene-log.jsonl` 做了实际检查。结论不是“JSON 不好”，而是“JSONL 还没有变成可分析 artifact”：

| 观察 | 样本证据 | 设计含义 |
| --- | --- | --- |
| raw 日志量过大 | `scene-log.jsonl` 约 4914 行，`TargetSelector/TargetQueryEngine/TargetQueryEntities` 单项约 3041 条 | AI 不应直接消费 raw；必须先看 `analysis/summary.md`、`noise/top-contexts.md`、owner flow 摘要。 |
| 结构化不充分 | 约 1109 条 `fields:{}`，约 1109 条 `operation == context` | JSON envelope 有了，但 owner 语义还没完成；需要 owner `Log.md` 和字段门禁。 |
| analyzer 产物太薄 | 修复前 `ai-context.md` 只列 run metadata、owner 和 query 示例；没有 top noise、missing field、flow digest、owner 建议 | `logctl analyze` 必须生成面向 AI 的 digest，而不是只拆 raw JSONL。当前最终契约见第二部分。 |
| flow 边界错误 | 修复前 `flows/flows.json` 把几乎所有有 `operation` 的 entry 都纳入 flow | flow 必须由 `channel=Flow`、`entryType=flow_summary/flow_step` 或明确 OperationTrace 标记决定；当前默认输出改为 `flows/flows.jsonl` 结论对象。 |
| 测试事实缺失 | 样本 `validationEntries=0`、`artifacts=0` | 运行通过不等于行为被验证；Validation artifact 必须成为 gate 主事实源。 |

完整分析见 `07-当前样本日志问题与整理方案.md`。

### 4.1 2026-06-10 复盘裁决

用户追问“为什么需求写出来了，重构 Log 时仍没有完成打印信息整理”。裁决如下：

- SDD-0040 T1 完成的是 `LogEntry`、sink、profile、budget、`OperationTrace`、`ValidationSession` 和最小 `logctl` 管道。
- T1 没完成的是 analyzer digest、semantic missing-fields、正确 flow 边界、owner 字段契约、Validation gate 状态区分和真实样本验收。
- 2026-06-11 的 T2 修正已经把 analyzer 默认入口改成 `summary.md`、`ai-context.md`、`flows/flows.jsonl`、`noise/templates.jsonl`、`missing-fields` 和 `failures`，并清理 stale `by-owner` / `by-phase` / pretty `flows.json`。
- 2026-06-11 19:26 再校正：T2 analyzer 完成也不能代表 live 打印已经 AI-first。`Src/ECS` 源码调用点语义化仍未完成，后续进入第三部分 T3。
- 因此当前状态不能再描述为“只剩 Godot scene smoke blocker”，也不能描述为“整理仍是 raw 分桶”。更准确的状态是：记录层和 analyzer 默认语义入口已落地；源码调用点语义化、T2.6 Validation artifact adoption 与最终 Godot runner smoke 仍未完成。
- T2 的第一验收样本就是 `.ai-temp/log-runs/20260610-013907`；当前样本语义整理结果是 `rawLines=4915`、`defaultReadableLines=303`、`defaultReadableRatio=0.062`。这证明默认入口已从 raw 复制转为语义提炼；但旧样本仍缺 Validation artifact 和完整 flow step，不能据此声明行为通过。

## 5. AI-first 原则

| 旧问题 | AI-first 规则 |
| --- | --- |
| 一条日志只是一句话 | 一条日志必须是结构化事件，至少包含 `runElapsedMs / frame / level / channel / owner / context / operation / message / fields`。 |
| `Info` / `Debug` 只是打印级别 | 级别只是筛选维度，真正决定价值的是 `eventId`、`operation`、`entityId`、`correlationId` 和字段完整性。 |
| 测试靠 `PASS` 文本 | 测试结果必须进入统一 `Validation` 语义和 artifact。 |
| 运行时日志和 scene runner 分开 | runner 只负责收集、筛选、落盘和分析，不负责定义业务日志语义。 |
| 只靠手动开关 | 默认 profile + CLI 临时覆盖 + AI 建议回写三层控制。 |
| 分散打印导致 AI 看不出过程 | 对技能释放、伤害结算、目标选择、对象池租还、系统 preflight 等过程使用 `LogSpan` / `OperationTrace` 聚合，结束时输出 `[FLOW:<operation>]` 摘要。 |
| 每个功能自己随手写日志 | 每个 owner 必须有 `Log.md`，写清楚“打什么、为什么打、怎么分析、哪些默认关闭”。 |
| JSONL 仍然太大 | `logctl analyze` 必须输出 markdown digest、flow conclusion 和 success template，AI 默认不读 raw，也不读 owner/phase raw 复制分桶。 |

### 5.1 时间语义裁决

Log 默认不应打印墙钟时间。`[HH:mm:ss]` 对 AI 排查单次运行内的因果顺序价值很低，也无法回答“第几秒、第几帧、哪个物理 tick 出问题”。

默认显示和默认结构化字段应改为：

- `runElapsedMs`：从本次进程 / scene / log session 开始的单调运行时长，默认 console 显示为 `t=12.384s`。
- `frame`：Godot process frame，用来定位渲染帧顺序。
- `physicsFrame`：Godot physics frame，用来定位物理 tick 顺序。
- `gameElapsedMs`：游戏模拟时间；如果受 pause / timeScale 影响，必须明确。
- `wallClockUtc`：可选字段，只用于跨进程、跨 artifact、跨机器对齐，不作为 console 默认字段。

console 推荐前缀：

```text
t=12.384s f=742 pf=246 [WARN][Movement][EntityMovementComponent] operation=SwitchMode reason=ModeMissing entity=enemy_001
```

Godot 官方 API 支持这条方向：`Time.GetTicksMsec()` / `Time.GetTicksUsec()` 提供 engine 启动后的单调时长，`Engine.GetProcessFrames()` / `Engine.GetPhysicsFrames()` 提供 process / physics frame 序号。墙钟时间仅保留在 JSONL 的可选 `wallClockUtc`，用于跨进程或跨 artifact 对齐。

### 5.2 等级裁决

当前 `LogLevel.Success` 混淆了“严重程度”和“结果”。AI-first 版本应把两者拆开：

| 字段 | 推荐值 | 说明 |
| --- | --- | --- |
| `severity` | `Trace / Debug / Info / Warn / Error / Fatal` | 只表达对运行健康度的严重程度。 |
| `outcome` | `None / Started / Completed / Succeeded / Failed / Skipped / Suppressed` | 表达过程或检查结果。 |
| `validationStatus` | `pass / fail / skip / expected-failure` | 只用于 Validation/Test channel。 |

`PASS` / `FAIL` 不再作为通用日志等级；它们是 Validation 事实字段。负向测试的“预期失败”不得默认进入 `GD.PushError`，否则 runner 会把受控失败误判成场景失败。

## 6. 目标边界

| 模块 | 目标职责 | 禁止职责 |
| --- | --- | --- |
| `LogEntry` | 固定结构化 envelope，承载事实字段 | 不拼装业务流程，不猜默认语义。 |
| `LogProfile` | 默认等级、规则、预算、sink 组合 | 不执行业务逻辑。 |
| `LogManager` / `Log` | 生成事件、应用筛选、派发给 sink | 不直接承担测试断言。 |
| `LogSink` | console / jsonl / memory / artifact 输出 | 不决定哪些日志该产生。 |
| `logctl` | CLI 临时开关、查看热度、生成建议、应用建议 | 不把临时覆盖永久藏起来。 |
| `Validation` | PASS / FAIL / check / artifact 统一表达 | 不再单独用 `GD.Print` / `GD.PushError` 作为唯一结果。 |
| `Scene runner` | 注入 profile、收集输出、产出 gate report | 不分析业务规则本身。 |
| `LogAnalyzer` | 把 raw JSONL/stdout/artifact 拆成 AI 可消费目录、摘要、噪声、缺字段、flow digest | 不替代 owner 文档判断业务是否正确。 |
| owner `Log.md` | 说明该 owner 应该打什么、如何判错、默认噪声预算 | 不重复实现文档，不列全量源码调用点流水账。 |

## 7. 控制面裁决

Log 控制必须同时支持两层：

### 7.1 稳定事实源

稳定事实源是配置文件，不是 CLI。

原因：AI 需要可复现。每次会话里临时打开/关闭的 context、level、budget 和 sink，必须能被保存、复盘和回放。

建议位置：

- `Data/Log/` 或 `Config/Log/`
- 文件名使用 `log.profile.json`、`log.rules.json`、`log.overrides.json`

稳定配置至少应包含：

- 默认等级。
- channel / owner / context 规则。
- 每秒日志预算。
- sink 开关。
- 测试专用规则。
- `godotEditorSink` 默认关闭，只允许 debug profile 打开。

### 7.2 CLI 临时控制

CLI 是现场调试入口，不是长期事实源。

CLI 适合做：

- 开某个 context。
- 关某个重复 channel。
- 切换某个 profile。
- 查看 top noisy contexts。
- 根据最近一次 run 生成建议。
- 把建议写回配置文件。

CLI 不适合单独承担永久配置，因为它不可复现，且下一次会话无法知道当时为何这么开。

### 7.3 结论

**配置文件负责默认策略，CLI 负责实时覆盖，AI 分析负责生成建议并回写配置。**

这三层缺一不可。

## 8. 噪声控制

Log 不是要“打更多信息”，而是要“打更少但更有用的信息”。

必须引入三种降噪机制：

1. **结构化字段约束**：没有 `operation`、`context`、`owner`、`entityId` 或 `reason` 的日志，默认不算高价值。
2. **重复合并**：短时间内相同 `eventId + context + entityId + operation + reason` 的日志必须聚合成摘要。
3. **预算控制**：每个 context / channel / owner / entity 都要有默认预算，超出后降级为摘要或只写 JSONL。
4. **阶段隔离**：按 `phase` / `scenePhase` / `gamePhase` 切分日志，避免 Boot、DataLoad、Validation、Gameplay、Wave、Combat、Shutdown 混在一起。
5. **过程摘要优先**：长过程内部可以记录 step，但 console 默认只展示 `[FLOW:<operation>]` 结束摘要，详细 step 只写 JSONL / artifact。
6. **分析入口约束**：AI 默认从 `summary.md` / `ai-context.md` 开始，禁止把 raw JSONL 直接拼进提示词。

## 9. 测试统一

测试必须统一到同一套观测语义：

- `PASS` / `FAIL` 不再靠裸 `GD.Print`。
- `Check` / `Pass` / `Fail` 应写入结构化 artifact。
- runner 只解析 artifact + JSONL + exit code，不再靠松散字符串猜测。
- 负向测试允许产生错误级日志，但必须被标注为受控失败，不得污染 gate 结果。

Godot 的 `GD.PushError` / `GD.PushWarning` 会写入 debugger 和终端，是错误/警告输出 API，不是测试断言事实源。后续测试 helper 应把 `CheckResult`、失败原因、期望/实际值和 artifact 路径写入 Validation 结构，再由 runner 读取 artifact 判定。

## 10. 完成定义

Log 重构完成不是“还能打印”。

必须同时满足：

- 业务日志可通过结构化 envelope 输出到 console / jsonl / memory。
- CLI 能临时打开、关闭、查询和回写规则。
- 配置文件能作为默认事实源保存 profile。
- 测试断言与 scene runner 使用统一的 PASS / FAIL 观测语义。
- 同一类重复噪声能被合并或降级。
- AI 能从日志中直接读出模块、阶段、实体、操作和失败原因，而不是猜。
- 每个改动过的 Runtime / Capability / Tools / UI owner 至少有 `Log.md` 或 README 中的 `## Log` 小节，说明日志思路和分析流程。
- runner / logctl 默认产物中存在 `raw/`、`flows/`、`failures/`、`noise/`、`missing-fields/`、`summary.md`、`ai-context.md` 等分层目录；不默认生成 `by-owner` / `by-phase` raw 复制分桶。
- `ai-context.md` 必须包含 top noise、missing field、failure focus、flow digest、owner 文档链接和下一轮查询建议；不能只列 owner 名称。
- `summary.md` / `ai-context.md` 能让 AI 不读全量 stdout / raw JSONL 也能定位第一轮问题分类。

## 11. DeepThink 确认包

> 历史说明：本节保留 2026-06-09 设计形成时的确认包。2026-06-10 的原始长提示和去重结论已迁入 `source-request.md`，样本日志复查已迁入 `07-当前样本日志问题与整理方案.md`，避免 README 继续承载过多过程材料。

### Goal

解决当前 Log 对 AI 不友好的根问题：文本分散、噪声过大、开关不可复现、测试事实源分裂、分析流程不固定。非目标：本设计阶段不直接改源码、不引入第三方日志依赖、不把 SlimeAI 迁到外部 observability 平台。

### Context Read

历史记录：

- Git boundary：`/home/slime/Code/SlimeAI/SlimeAI`。
- 已读本地事实源：`DocsAI/README.md`、`DocsAI/ECS/README.md`、`DocsAI/ECS框架与AIFirst方向决策.md`、PRJ-0002 README / INDEX / progress / roadmap / notes、`design/Tool/10.Log/`、`DocsAI/ECS/Tools/Logger/*`、`Src/ECS/Tools/Logger/Log.cs`、`Src/ECS/**/Tests/**/*.cs`、`.ai-config/skills/godot/godot-scene-test/*`、`/home/slime/Code/SlimeAI/SlimeAI-AiFirst/GameOS/Observation/*`。

2026-06-10 校准：

- 当前 `Log.cs` 已经有 structured envelope、sink、budget、ValidationSession 和 `logctl` 雏形，不能再把它描述成纯 `GD.PrintRich` 文本 logger。
- 本轮已检查 `.ai-temp/log-runs/20260610-013907/raw/scene-log.jsonl`，真实缺口转为 analyzer digest、owner 字段契约、flow 边界和 Validation artifact 接入。

### Evidence / Search Coverage

本地证据：

- `Src/ECS/Tools/Logger/Log.cs` 当前已有 `LogEntry`、`OperationTrace`、structured sinks、profile 和 budget；问题是业务字段、owner flow 和 analyzer digest 尚不足。
- `.ai-temp/log-runs/20260610-013907/raw/scene-log.jsonl` 约 4914 行，`TargetSelector/TargetQueryEntities` 约 3041 条，约 1109 条 `fields:{}` 和约 1109 条 `operation == context`。
- `Workspace/Tools/logctl/logctl.mjs analyze` 已能生成 `summary.md`、`ai-context.md`、`flows/index.md`、`noise/top-contexts.md`、`missing-fields/index.md`；第二部分继续把默认入口从“raw 分桶”升级为 flow conclusion 和 success templates。
- `.ai-config/skills/godot/godot-scene-test/scripts/godot-scene-runner.mjs` 仍保留 `[FAIL]`、`FAIL:`、`Exception` 等 stdout fallback；长期主判断应来自 artifact / structured validation / structured log。
- `ValidationSession.cs` 已存在，但样本 run `validationEntries=0`、`artifacts=0`，说明测试/场景接入仍需补齐。

外部资料：

- Context7 `/websites/godotengine_en_stable` + Godot docs：`GD.PushError` / `GD.PushWarning` 输出到 debugger 和终端；`Engine.GetProcessFrames()`、`Engine.GetPhysicsFrames()`、`Time.GetTicksMsec()` / `GetTicksUsec()` 支持 frame 和运行时长字段。
- Microsoft Learn `Console.WriteLine` / .NET high-performance logging：stdout 可作为 runner summary sink；高频详细日志仍应先 `IsEnabled`，并使用 buffered JSONL / lazy fields 避免关闭日志时构造昂贵消息。
- OpenTelemetry Logs Data Model：日志记录应有 severity、body、attributes、trace/span correlation 等结构化字段。
- Microsoft .NET logging docs：日志过滤按 level 和 category 生效，`ILogger` 支持结构化占位符；高性能日志可用 `LoggerMessage` / source-generated logging 减少关闭日志时的开销。
- Google Cloud structured logging docs：JSON 对象里的 `severity`、labels、结构化字段比纯文本更适合后处理。

### Problem Reality Check

问题真实存在。2026-06-10 的更准确表述是：Logger core 已经部分结构化，但 raw JSONL 仍会把 AI 拉回“全量日志里找规律”；当前 analyzer 没有把 top noise、missing fields、flow digest 和 owner 文档链接整理成足够强的第一入口。推断是：如果不补 analyzer/owner 文档，JSONL 会比旧 stdout 更长、更难直接阅读。

### Idea Check

用户思路成立：Log、Debug、Test/TDD、Observation 必须一起设计；“过程完成后输出聚合数据”比散点日志更适合 AI；日志分析应先由脚本整理目录，再给 AI。需要修正的是：Log 不应替代 Test，Log 提供事实和 artifact，Test/Validation 仍负责判定规则；CLI 也不应替代文件事实源，否则不可复现。

### Options

| 方案 | 内容 | 取舍 |
| --- | --- | --- |
| A. 小修现有 Logger | 保留 `Log.cs`，补 `IsEnabled`、lazy message、少量 context 开关 | 成本低，但解决不了测试事实源、流程聚合和 AI 分析不固定。 |
| B. 推荐：AI-first Observation Log hard cutover | 重建结构化 envelope、profile/CLI、ValidationSession、runner analyzer、owner `Log.md` | 成本高，但符合用户“不考虑兼任、完全升级”的要求，能系统性解决问题。 |
| C. 接入外部日志框架 | 引入 Serilog / OpenTelemetry exporter 等 | 不推荐当前阶段。依赖和接入成本高，不能替代 SlimeAI owner 文档、Godot runner 和游戏过程语义。 |

### Recommendation

采用方案 B。原因：当前问题不是某个 `LogLevel` 不够，而是 Log 契约、测试事实源、runner artifact 和 AI 分析流程没有统一。完全升级能一次把“怎么打、怎么关、怎么聚合、怎么分析、怎么验证”固定下来。

### Must Confirm

思路问题：

- 暂无。用户已明确允许完全重构，并要求不考虑兼任。

信息缺口：

- 是否已有最典型的“几秒几百条”场景日志样本？为什么问：可以决定第一批预算和默认关闭 context。不回答时默认先用静态扫描 + 后续 run digest 调整。

决策未定：

- 第一版是否允许同时 hard cutover Logger、Validation helper 和 runner analyzer？为什么问：只改 Logger 不改 runner/test 会继续保留分裂事实源。不回答时默认创建一个执行型 SDD，把三者作为同一批主链路改造。

### Should Confirm

- profile 文件放 `Config/Log/` 还是 `Data/Log/`？默认 `Config/Log/`，因为它是运行观测策略，不是游戏业务 DataOS 字段。
- `Fatal` 是否作为独立 severity？默认保留，用于场景无法继续、runner 应立即 fail 的情况。
- 每个 owner 是新增独立 `Log.md`，还是内容少时写入 README 的 `## Log`？默认允许二者，复杂 owner 用独立 `Log.md`。

### Defaults I Will Use

- Log profile 默认文件事实源：`Config/Log/log.profile.json`、`Config/Log/log.rules.json`、`Config/Log/log.overrides.json`。
- 第一批迁移优先级：Logger core -> ValidationSession/Test helper -> runner analyzer -> Data/System/Entity/Ability/Movement/ObjectPool/Timer 测试 -> 高频业务 owner。
- console 默认只显示 error/warn、validation verdict、flow summary；详细 steps 写 JSONL。
- `Success` 从 severity 移除，改为 `outcome=Succeeded` 或 `validationStatus=pass`。

### Not Recommended

- 不建议只把 level 提高到 `Warn` 来降噪；这会把有价值的 Info/Debug 也藏掉。
- 不建议继续让 runner 以 `[PASS]` / `[FAIL]` 文本作为主事实源；它只能作为过渡 fallback。
- 不建议让 AI 直接读完整 stdout；应先脚本拆分、聚合、压缩和生成 owner 分析上下文。
- 不建议在没有 owner `Log.md` 的情况下到处新增日志；这会复发“这里一个 log 那里一个 log”的问题。

### Artifact Updates

本轮写入 `design/Tool/10.Log/README.md`、`01~06`、`DocsAI/ECS/Tools/Logger/README.md`、PRJ-0002 `README.md` / `design/INDEX.md` / `Core/roadmap.md` / `Core/progress.md` / `Core/notes.md`。不创建执行型 SDD；等用户确认或回复“按推荐执行”后再创建 Log hard cutover SDD。

## 10. 阅读顺序

1. 先读 `01-现状分析与AI-first裁决.md`，确认为什么要重构。
2. 再读 `02-目标架构与数据契约.md`，确认结构化日志长什么样。
3. 再读 `03-控制面与CLI设计.md`，确认 CLI / profile / 回写怎么分工。
4. 再读 `04-测试统一与Observation接入.md`，确认测试怎么统一。
5. 最后读 `05-调用点迁移与验证计划.md`，看迁移和门禁。
6. 若要给具体功能 owner 写日志规范，读 `06-功能OwnerLog文档与分析流程.md`。
