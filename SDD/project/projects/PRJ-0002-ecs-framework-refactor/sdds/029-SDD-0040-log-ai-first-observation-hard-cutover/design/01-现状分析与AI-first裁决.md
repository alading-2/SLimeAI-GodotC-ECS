# 现状分析与 AI-first 裁决

> 更新：2026-06-11
> 状态：historical design note；记录层裁决仍可参考，整理层当前契约见项目级第二部分 `03-最终设计与完成清单.md`
> 提醒：本文是 SDD-0040 初始快照。旧 `by-owner` / `by-phase` raw 分桶和 pretty `flows.json` 不再是默认 analyzer 产物。

## 1. 当前事实

当前 `Src/ECS/Tools/Logger/Log.cs` 仍是传统文本日志：

- 6 个等级：`Trace / Debug / Info / Success / Warning / Error`。
- 支持全局等级和按 context 设置等级。
- `Trace` / `Debug` 依赖 `Conditional("DEBUG")`。
- `Warn` / `Error` 会额外推送到 Godot Debugger。
- 输出主体仍是 `GD.PrintRich` 的字符串拼接。

这套设计能工作，但不适合 AI-first 的高密度调试场景。

### 1.1 本轮本地扫描证据

本轮扫描范围覆盖 `Src/ECS/Tools/Logger/Log.cs`、`Src/ECS/**/*Test*.cs`、`.ai-config/skills/godot/godot-scene-test/scripts/godot-scene-runner.mjs` 和 `/home/slime/Code/SlimeAI/SlimeAI-AiFirst/GameOS/Observation/**`。

| 证据 | 当前形态 | 结论 |
| --- | --- | --- |
| `Log.cs` | `GlobalLevel` + `_contextFilters` + `ShowTimestamp` + `GD.PrintRich` | 只有文本显示和等级过滤，没有结构化 envelope、owner、operation、phase、entity、correlation、budget。 |
| `godot-scene-runner.mjs` | `FAILURE_PATTERNS` 包含 `[FAIL]`、`FAIL:`、`Exception`，摘要保留 `[PASS]` / `[OK]` | runner 仍以字符串 pattern 做主判断，说明测试事实源没有统一到 artifact。 |
| `DataSceneTestBase.cs` | `GD.Print("PASS ...")`、`GD.PushError("FAIL ...")` | PASS/FAIL 直接写 Godot 输出，无法携带 expected/actual/category/check id。 |
| `SystemCoreRuntimeTest.cs` 等 | `_log.Info("[PASS]")`、`_log.Error("[FAIL]")` | Log 和 Test 结果混在一起，AI 只能靠 message 文本判断。 |
| `GameOSObservationSession` 原型 | JSONL sink、memory sink、Validation artifact | 方向可复用，但仍缺 frame/phase/correlation/budget/profile/CLI。 |

这些证据证明：当前问题不是“日志级别设置不够细”，而是 Log、Validation、runner 和 AI 分析之间没有统一契约。

### 1.2 外部资料校准

本轮按用户要求补了 Context7 / Web 资料：

| 来源 | 采纳点 | 不采纳点 |
| --- | --- | --- |
| Context7 `/godotengine/godot-docs` + Godot docs `@GlobalScope` / `Engine` / `Time` | `GD.PushError` / `GD.PushWarning` 是 debugger + terminal 输出；`Engine.GetProcessFrames()`、`Engine.GetPhysicsFrames()`、`Time.GetTicksMsec()` / `GetTicksUsec()` 可支撑 run elapsed / frame 字段。 | 不把 Godot error 输出当测试断言主事实源。 |
| Context7 `/godotengine/godot-docs` + Godot logging docs | `print_rich` / `GD.PrintRich` 支持 BBCode rich text，会显示到 editor Output 和标准输出。 | AI-first 不需要 BBCode 颜色，不把 rich print 作为默认 sink。 |
| Context7 `/dotnet/docs` + Microsoft Learn `Console.WriteLine` / high-performance logging | `Console.WriteLine` 写标准输出，.NET logging 示例先 `IsEnabled` 再输出，高性能 logging 建议 source-generated / `LoggerMessage`。 | 不把每条高频日志都直接 `Console.WriteLine`；默认仍应 buffered JSONL 文件 + stdout summary。 |
| OpenTelemetry Logs Data Model | 采纳 severity、attributes、trace/span correlation 的结构化思想。 | 不复制 OpenTelemetry exporter / collector 作为当前依赖。 |
| Microsoft .NET Logging | 采纳 category/filter、结构化占位符、高性能日志需要避免关闭日志时构造消息的原则。 | 不直接迁到 `ILogger`，避免先引入依赖和 DI 复杂度。 |
| Google Cloud structured logging | 采纳 JSON structured fields / severity / labels 适合后处理。 | 不接云端日志平台。 |

官方链接记录在项目 `Core/notes.md`，本文件只保留采用/不采用裁决，避免设计正文变成资料堆叠。

## 2. 当前主要问题

### 2.1 信息密度不够

现在日志大多是自然语言句子。AI 要判断一条日志是否有用，必须靠上下文猜：

- 这是谁打的。
- 属于哪个阶段。
- 是否和某个 entity / system / operation 有关。
- 是否是重复噪声。

### 2.2 级别不是足够强的控制面

`Info` / `Debug` / `Warn` / `Error` 只能表达严重程度，不能表达：

- 该 context 是否值得继续看。
- 某条日志是否属于重复序列。
- 这条日志是否仅对 validation 有价值。
- 这条日志是否只应写进 artifact，不应刷 console。

等级只能回答“严重不严重”，不能回答“是否对当前 AI 调试目标有价值”。AI-first 控制面必须至少同时看：

- `channel`：runtime / validation / diagnostics / profile / analyzer。
- `owner`：Runtime/Data、Capability/Ability、Tools/Timer 等。
- `context`：类或具体组件。
- `operation`：一次业务过程，例如 `AbilityCast`、`DamageProcess`、`ObjectPoolRelease`。
- `phase`：Boot、DataLoad、SceneReady、Gameplay、Validation、Shutdown。
- `correlationId`：把同一过程的所有 step 串起来。
- `sink`：console / jsonl / memory / artifact。
- `budget`：每秒、每 owner、每 entity、每 operation 的展开预算。

### 2.3 测试事实源分裂

当前测试中同时存在：

- `GD.Print("PASS")`
- `GD.PushError("FAIL")`
- `_log.Success("[PASS]")`
- `_log.Error("[FAIL]")`
- `throw new Exception(...)`

这会让 scene runner 只能靠字符串 pattern 猜结果，不能把测试结果当成统一观测事实。

### 2.4 runner 仍在做字符串扫描

`godot-scene-runner.mjs` 和 `analyze-logs.sh` 仍依赖：

- `[FAIL]`
- `FAIL:`
- `Exception`
- `Failed to load`
- `scene not found`

这不是坏实现，但它说明当前日志还没有足够结构化，runner 只能做兜底识别。

### 2.5 过程日志分散

用户指出“施放技能从头到尾应该放在一起，到结束才一起打印”是成立的。当前散点日志会把一个过程拆成很多无关联行：

```text
target selected
cooldown ok
mana consumed
projectile spawned
damage applied
cooldown started
```

AI 要判断技能是否正确，必须自己把这些行按时间和实体拼回流程，容易漏掉关键分支。

AI-first 版本应该把这类过程建模为 `OperationTrace`：

```text
[FLOW:AbilityCast] correlation=cast_001 owner=Ability entity=player_001 ability=chain_lightning result=succeeded durationMs=18 steps=7 targets=3 suppressed=12
```

详细 step 写 JSONL / artifact，console 默认只显示结束摘要和失败摘要。

### 2.6 缺少游戏阶段

当前还没有完整游戏阶段系统，但 Log 必须提前留出 `phase` 字段。否则 Boot、DataOS 加载、场景初始化、Gameplay、Wave、Combat、Validation、Shutdown 的日志会混在一起。

第一版可以先接 Runtime System 的 `ProjectStateSnapshot` / scene runner mode，默认 phase：

```text
Boot / DataLoad / SceneReady / Validation / Gameplay / Wave / Combat / Paused / Shutdown / Unknown
```

没有阶段时填 `Unknown`，但 analyzer 必须把 Unknown 作为待补 owner 问题列出来。

### 2.7 默认 sink 用错了

当前 `LogInternal` 的最终输出是：

```csharp
GD.PrintRich($"[color={color}]{timestampStr}[{tag}]{contextInfoStr} {message}[/color]");
```

这条路径的设计目标是人类在 Godot editor 里看彩色日志。现在用户明确目标是 AI 分析，彩色 Output 面板不再是核心价值。

AI-first 角度看，默认走 Godot API 有几个问题：

- 结构化事实被先格式化成字符串，后续 analyzer 只能再解析文本。
- `GD.PrintRich` 的 BBCode / rich text 对 JSONL 没有价值。
- 每条日志都过 Godot C# binding / Godot String / editor output 路径，不适合高密度机器日志。
- headless runner 最终需要的还是 stdout、JSONL 和 artifact。

但也不能简单说“C# 打印一定更高性能”。`Console.WriteLine` 本身也是同步文本 IO，刷几百上千条一样会慢。正确裁决是：

```text
默认详细日志 -> C# buffered JSONL file
默认可见摘要 -> C# stdout summary
人工 editor 调试 -> optional GodotEditorSink
```

用户已确认本分析口径：**C# 输出链路更适合 AI-first 默认主链路，但这里的 C# 不是逐条 `Console.WriteLine`，而是 buffered JSONL + stdout summary 的 structured sink 组合。**

因此后续实现必须避免两个误读：

- 误读 A：继续保留 `GD.PrintRich` 作为默认实现，只在外面包一层 profile。
- 误读 B：把所有详细日志逐条改成 `Console.WriteLine`。

正确方向是先生成结构化 `LogEntry`，再按 sink 策略输出；`Console.WriteLine` 只负责摘要，JSONL / artifact 承载详细事实。

## 3. AI-first 裁决

### 3.1 日志必须先结构化，再显示

AI-first 要求：

```text
source -> structured log entry -> sink(s) -> console/jsonl/artifact
```

而不是：

```text
source -> formatted string -> console -> 事后猜语义
```

### 3.2 每条日志必须可被机器理解

至少要有：

- `runElapsedMs`
- `frame`
- `level`
- `channel`
- `owner`
- `context`
- `operation`
- `message`
- `fields`

必要时再加：

- `entityId`
- `correlationId`
- `physicsFrame`
- `gameElapsedMs`
- `wallClockUtc`
- `phase`
- `source`
- `tags`

其中 `runElapsedMs / frame / physicsFrame` 比墙钟时间更重要。墙钟时间只能回答“真实世界几点”，不能回答“游戏运行到第几秒、第几帧出现问题”。AI 调试更需要运行内因果顺序，所以 `wallClockUtc` 只能作为跨 artifact 对齐字段，不应作为 console 默认前缀。

### 3.3 日志不是越多越好

AI-first 不是把每一帧都刷满，而是保证：

- 关键事实保留。
- 重复噪声压缩。
- 低价值日志默认关闭。
- 开关可以实时调。
- 调整结果可以回写。

### 3.4 等级要重建，不是加更多等级

当前 `Success` 是结果，不是严重程度。建议 hard cutover 后使用：

```text
severity: Trace / Debug / Info / Warn / Error / Fatal
outcome: Started / Completed / Succeeded / Failed / Skipped / Suppressed / None
validationStatus: pass / fail / skip / expected-failure
```

这样可以避免 `_log.Success("[PASS]")` 这类混合表达，也能让负向测试表达“预期失败但测试通过”。

### 3.5 `GD.PushError` 只能是 sink，不是 Test API

Godot 官方语义是把错误推送到 debugger 和终端，并不暂停执行。它适合显示真实运行错误，不适合作为所有测试失败的唯一事实源。测试失败应该先写 Validation artifact；是否同步输出到 `GD.PushError` 由 profile 决定。

### 3.6 先脚本整理，再给 AI 分析

AI 不应直接消费完整 stdout。runner/analyzer 应固定生成：

```text
summary.md
ai-context.md
flows/flows.jsonl
noise/templates.jsonl
failures/
noise/
missing-fields/
raw/entries.jsonl
```

`ai-context.md` 只包含本次分析需要的 digest、top failures、owner links 和下一步建议，不包含全量原始日志。

### 3.7 默认输出必须从 Godot rich print 迁出

hard cutover 后：

- `GD.PrintRich` 不应出现在 AI 默认 Log sink。
- `GD.PushWarning` / `GD.PushError` 只由 `GodotEditorSink` 或 fatal bridge 按 profile 调用。
- `StdoutSummarySink` 使用 C# `Console.Out` 输出摘要。
- `JsonlFileSink` 使用 C# buffered writer 输出完整结构化日志。
- runner 读取 JSONL/artifact 为主，stdout summary 只辅助定位。

## 4. 裁决摘要

本次重构采用：

- **结构化事件优先**。
- **配置文件为默认事实源**。
- **CLI 为临时覆盖入口**。
- **AI 建议可以回写配置**。
- **测试 PASS / FAIL 统一进观测层**。
- **业务过程用 flow/span 聚合输出**。
- **每个 owner 的日志和分析规则写入 owner `Log.md` 或 README `## Log` 小节**。
- **runner 先整理 artifact，再给 AI 分析**。
- **默认 sink 迁到 C# stdout summary + buffered JSONL file，Godot editor sink 默认关闭**。

## 5. 不建议方向

- 不建议继续在现有 `Log.cs` 上只加颜色、等级或 timestamp；这会保留根问题。
- 不建议通过“提高全局 level”解决噪声；它会隐藏有价值的结构化事实。
- 不建议让 AI 每次临场判断哪些 log 有用；必须由 profile、budget 和 owner 文档固定默认策略。
- 不建议把测试 `Fail()` 封装成单纯 `GD.PushError`；这仍是文本输出，不是统一 Validation。
- 不建议继续把 `GD.PrintRich` 当成默认 Log 输出；颜色显示是人工调试需求，不是 AI-first 主链路。
