# 控制面与 CLI 设计

> 更新：2026-06-09
> 状态：current design note

## 1. 原则

控制面分三层：

1. **文件**：默认事实源。
2. **CLI**：运行时临时覆盖。
3. **AI 建议**：从最近 run 里分析后回写文件。

这三层不是重复，而是各自负责不同稳定性。

## 2. 文件事实源

建议至少有三个文件：

- `Config/Log/log.profile.json`：默认策略。
- `Config/Log/log.rules.json`：可复用规则库。
- `Config/Log/log.overrides.json`：当前 run 或当前会话的临时覆盖快照。

文件里要能表达：

- default level。
- rule priority。
- budget。
- sink 开关。
- test 专用规则。
- validation 专用规则。
- flow 展开规则。
- phase 默认映射。
- analyzer 输出目录规则。

推荐形态：

```json
{
  "profile": "ai-default",
  "defaultSeverity": "Warn",
  "console": {
    "enabled": true,
    "showWallClockUtc": false,
    "showFlowSummary": true,
    "sink": "stdout-summary"
  },
  "jsonl": {
    "enabled": true,
    "sink": "buffered-file",
    "flush": "batch"
  },
  "godotEditor": {
    "enabled": false,
    "richText": false,
    "pushWarningsAndErrors": false
  },
  "rules": [
    {
      "owner": "Ability",
      "operation": "Cast",
      "minSeverity": "Info",
      "console": "summary",
      "jsonl": "full",
      "budgetPerSecond": 20
    }
  ]
}
```

`log.overrides.json` 必须带 `createdBy`、`createdAtRunElapsedMs`、`reason`、`expires`，否则临时覆盖会变成隐藏事实源。

## 3. CLI 控制

CLI 不应该只做“开关某个等级”，而应该直接面向 AI 调试任务。

建议命令：

- `logctl profile use <name>`
- `logctl set owner=Ability operation=Cast severity=Info console=summary jsonl=full`
- `logctl mute context=DamageSystem --console-only`
- `logctl enable channel=Validation`
- `logctl enable sink=godot-editor --profile editor-debug`
- `logctl flow expand operation=AbilityCast --max-steps 50`
- `logctl top --last 10s`
- `logctl analyze --run-dir <path> --out <path>`
- `logctl query --run-dir <path> owner=Ability operation=Cast severity>=Warn`
- `logctl query --analysis-dir <path> sourceFile=Src/ECS/Capabilities/Ability/System/AbilitySystem.cs --format md`
- `logctl query --file <path/to/scene-log.jsonl> context=DamageService --fields entityId,reasonCode,expected,actual`
- `logctl ingest --stdin --source legacy-stdout --out <run-dir>`
- `logctl suggest --run-dir <path>`
- `logctl apply-suggestions --dry-run`
- `logctl snapshot --write-overrides`

## 4. CLI 的真实作用

CLI 适合处理这些场景：

- 某个模块突然刷屏，先临时压掉。
- 某个模块需要打开更细的证据。
- 需要快速确认是哪个 context 最 noisy。
- 需要基于最近一次 run 自动生成下一轮建议。
- 需要把某个 flow 从 console summary 临时展开为 JSONL full。
- 需要把某个 Validation failure 的相关 owner 打开到 Debug。
- 需要在人工 editor debug 时临时打开 Godot editor sink。
- 需要对已经整理好的 run 做二次筛选，例如只看某个 owner、某个 source file、某个 entity、某个 operation 或某段时间窗口。
- 需要把用户手动运行游戏得到的 run 目录整理成 `analysis/`，再交给 AI，而不是复制整段 console 文本。

CLI 不适合单独承担永久配置，因为这会让下一次 AI 会话无法复现。

### 4.1 CLI 对已整理日志的操作是否必要

有必要。现实日志系统通常不只控制“运行时开关”，也支持对已经收集或已经整理的日志做查询、过滤和摘要。原因是调试不是一次完成的：第一次 run 产出 raw JSONL / artifact；第二次分析时经常会只关心某个文件、owner、entity、operation、reasonCode 或失败窗口。

参考外部系统的共同模式：

- Grafana Loki 的 `logcli` 可以查询 Loki 中已有日志，也支持 stdin / static log file 查询、label / field filter、limit 和统计。这说明“CLI 查询已有日志”是现实需求，不只是运行时控制。
- OpenTelemetry Collector processor 支持 transform / filter / enrich telemetry data，说明日志进入后处理管线后继续筛选、变换和降噪是常见架构。
- Datadog log indexes / exclusion filters 支持按 query 过滤和采样，说明日志量控制和按查询保留高价值日志是生产系统里的真实需求。

SlimeAI 的 `logctl` 因此应分两类命令：

| 类别 | 命令 | 输入 | 输出 | 职责 |
| --- | --- | --- | --- | --- |
| 运行控制 | `profile/use/set/mute/enable/snapshot` | profile / overrides | run metadata / overrides | 决定下一次 run 打什么、打到哪里。 |
| 离线分析 | `analyze/query/top/suggest/ingest` | run dir / JSONL / artifact / legacy stdout | `analysis/`、筛选结果、建议 | 操作已经产生的日志，不要求重新运行游戏。 |

`query` 必须支持 structured filter，不应该只做字符串 grep：

```text
logctl query --run-dir .ai-temp/scene-tests/runs/2026-06-09/12-30-00 owner=Ability operation=Cast
logctl query --analysis-dir <run>/analysis sourceFile=Src/ECS/Capabilities/Ability/System/AbilitySystem.cs
logctl query --file <run>/raw/scene-log.jsonl entityId=player_001 severity>=Warn
logctl query --run-dir <run> --contains "cooldown" --format json
```

`sourceFile` / `sourceMember` / `sourceLine` 是可选但建议保留的字段。它可以通过 C# caller info 或 Log API 显式传入；如果日志来自 legacy stdout，则只能降级为 `source=legacy-stdout`，不能假装知道文件来源。

结论：**`logctl` 必须支持对 raw JSONL、analysis dir 和 legacy stdout fallback 的二次查询；否则 AI 和用户仍会回到“复制一大段日志再人工筛”的旧流程。**

## 5. AI 建议回写

AI 不只是看日志，也要帮忙做策略优化。

建议流程：

```text
scene run
  -> runner 收集 stdout / JSONL / artifact
  -> logctl analyze 拆分 raw/by-owner/by-phase/flows/failures/noise
  -> 生成 ai-context.md
  -> AI 按 owner Log.md 读取热度 / 重复 / 缺字段 / 无价值日志
  -> 输出建议
  -> 人类确认或自动应用
  -> 回写 log.profile.json / log.rules.json
```

建议类型：

- 某 context 应降级到 `Warn`。
- 某 channel 应只进 JSONL，不刷 console。
- 某类重复日志应合并。
- 某个 test helper 应改成 Validation artifact。
- 某个 owner 缺 `phase` / `operation` / `reasonCode`。
- 某类 flow 应从逐条输出改为 summary。
- 某个 context 应只在失败 run 中展开。

## 6. 预算规则

必须支持：

- 每秒最大条数。
- 每 context 最大条数。
- 每 entity 最大条数。
- 每 operation 最多展开样本数。
- 每 phase 最大 console 条数。
- 每 flow 最大 step 展开数。
- 每 Validation check 最大 sample 数。

预算超出后，不能简单丢弃，应该：

- 输出摘要。
- 记录 suppressed count。
- 保持可追踪性。

预算不等于静默丢弃。预算超出后至少要写：

```text
[FLOW:SuppressedSummary] owner=Movement operation=CollisionCheck suppressed=382 windowMs=1000 reason=budget_exceeded
```

这样 AI 知道“有很多被压掉”，而不是误以为没有发生。

### 6.1 预算不是限制代码执行次数

预算规则限制的是 **日志输出 / 记录 / 展开 / 展示**，不是限制游戏代码执行次数。它不会让 `MovementSystem` 少执行，也不会让某个技能少触发；它只决定：

- 同一类日志是否继续写 stdout。
- 详细 step 是否继续写 JSONL。
- 是否从逐条记录切换为 sample / counter / summary。
- 是否记录 `suppressedCount`，告诉 AI 有多少信息被压缩。

示例：

```text
MovementCollisionCheck 每帧运行 60 次
  -> 代码仍然正常执行 60 次
  -> stdout 每秒最多显示 3 条 summary
  -> JSONL 每秒最多保留 20 条 sample
  -> 其余 37 条合并进 suppressedCount
```

### 6.2 为什么预算有必要

预算的目的不是少打日志，而是保护 AI-first 分析链路：

| 风险 | 没有预算会怎样 | 预算的作用 |
| --- | --- | --- |
| stdout 噪声 | 几秒几百条，runner summary 和人工观察都失效 | console 只保留摘要、失败和关键 warn/error。 |
| JSONL 文件膨胀 | 长时间运行后文件巨大，分析脚本慢，AI 输入过大 | 按 owner / context / flow 限制详细 sample，并写 suppressed summary。 |
| 性能和 IO 抖动 | 高频日志在热路径造成分配和 IO 压力 | 先 `IsEnabled`，再按 sink budget 决定是否构造详细字段。 |
| AI 判断偏差 | 重复成功日志淹没少量失败信号 | 失败、异常、reasonCode 和 validation fail 优先保留。 |
| 分析不可复现 | 每次人工临时关日志，下一次不知道少了什么 | budget 写入 profile / metadata / analysis/noise。 |

实际项目中，日志量控制是 observability 的基本问题。生产日志系统会做采样、过滤、索引限制、查询 limit 和 pipeline processor；SlimeAI 不需要复制这些平台，但必须有本地版预算，否则 AI-first 日志会变成“更结构化的刷屏”。

### 6.3 预算分层

预算应按 sink 分层，不是一个全局数字：

| Sink | 默认预算策略 |
| --- | --- |
| stdout summary | 最严格，只显示 flow summary、validation verdict、关键 warn/error、suppressed summary。 |
| jsonl buffered file | 比 stdout 宽，但高频 step 仍要 sample / aggregate。 |
| memory/artifact | Validation 相关检查必须完整保留；非检查类日志可摘要。 |
| godot editor sink | 默认关闭；打开时预算应比 stdout 更严格，避免 editor output 卡顿。 |

严重错误、Validation fail、Fatal、artifact 写入失败这类信号不应被普通预算静默压掉。它们可以绕过 console 条数预算，但仍要避免无限重复；重复 fatal/error 也应合并为 summary。

### 6.4 预算带来的风险和缓解

预算的主要风险是压掉调试需要的细节。缓解规则：

- 所有压制都必须写 `suppressedCount` / `budgetKey` / `sampleRate`。
- `logctl suggest` 只能建议预算调整，不能无审查永久隐藏 owner。
- 调试某个问题时可用 CLI 临时 `flow expand` 或 `jsonl=full`。
- 如果 analysis 发现 `missing-fields` 或证据不足，应分类为 `Log gap` / `Profile issue`，而不是猜代码错。

结论：**预算是日志观察面的限流和摘要策略，不是游戏逻辑限流；它的价值是保护性能、文件大小、AI 上下文和失败信号优先级。**

## 7. 回写边界

CLI 临时覆盖必须能落到：

- `log.overrides.json`
- scene run metadata
- gate report

否则下次还是要猜。

## 8. 开关策略裁决

用户关心“代码里的 Log 到底打开还是关闭，level 提高就是不打印了，能不能 CLI 控制”。裁决如下：

| 控制方式 | 适合 | 不适合 |
| --- | --- | --- |
| 代码 `if` / `IsEnabled` | 高频热路径避免构造昂贵字段；保护不该默认收集的数据 | 做长期策略和现场调试开关 |
| profile 文件 | 默认、可复现、可审查的策略 | 临场快速试错 |
| CLI override | 单次 run / 当前会话的快速调试 | 长期事实源 |
| AI suggestion | 根据 run digest 建议降噪、补字段、调预算 | 自动无审查大范围改策略 |

结论：**默认策略必须在文件，实时调整用 CLI，代码只负责廉价地判断是否需要构造日志。**

## 9. Sink 控制裁决

AI-first 默认 sink 不是 Godot Output 面板：

| Sink | 默认 | CLI/profile 控制 | 说明 |
| --- | --- | --- | --- |
| `stdout-summary` | 开 | `console=summary/off` | C# stdout，只输出摘要和关键错误。 |
| `jsonl-buffered-file` | 开 | `jsonl=full/summary/off` | AI 主输入，buffered file 写入。 |
| `memory` | 开 | Validation 内部控制 | 测试和 artifact 汇总使用。 |
| `artifact` | 开 | runner/Validation 控制 | gate report 和 analyzer 使用。 |
| `godot-editor` | 关 | `sink=godot-editor` | 仅人工 editor debug；可选择是否 `GD.PushWarning/Error`。 |

`GD.PrintRich` / `GD.PushError` 不再是默认打印实现。它们只能由 `GodotEditorSink` 调用，并且必须在 profile 和 run metadata 中留下痕迹。这样 AI 下次恢复时能知道当时是否启用了 Godot editor 输出。

## 10. 运行时接入方式

CLI 对 Godot headless run 的控制可以通过环境变量和 override 文件进入：

```text
SLIMEAI_LOG_PROFILE=ai-default
SLIMEAI_LOG_OVERRIDES=/path/to/log.overrides.json
SLIMEAI_LOG_RUN_DIR=/path/to/run
```

runner 负责把这些注入进 Godot 进程，Log runtime 负责读取并记录到 run metadata。这样 CLI 不需要直接连 Godot 运行时，也能保持可复现。

未来如果需要 live toggling，再考虑 Godot Debug panel / local socket / file watcher；第一版不把 live socket 作为必需项。

## 11. 用户主动运行游戏时怎么用

用户主动运行游戏也不应该复制整段 console 给 AI。推荐流程是：

```text
1. 选择 profile / run dir
   SLIMEAI_LOG_PROFILE=ai-default
   SLIMEAI_LOG_RUN_DIR=.ai-temp/log-runs/manual/<timestamp>

2. 启动游戏或 Godot editor play
   Log runtime 写 scene-log.jsonl、stdout summary、run metadata、artifact

3. 运行整理
   logctl analyze --run-dir .ai-temp/log-runs/manual/<timestamp> --out .ai-temp/log-runs/manual/<timestamp>/analysis

4. 需要进一步筛选时
   logctl query --analysis-dir .ai-temp/log-runs/manual/<timestamp>/analysis owner=Ability

5. AI 默认读取
   analysis/summary.md
   analysis/ai-context.md
   analysis/failures/
   analysis/flows/
```

如果用户只有复制出来的 legacy console 文本，则允许降级流程：

```text
cat copied-console.log | logctl ingest --stdin --source legacy-stdout --out .ai-temp/log-runs/manual/<timestamp>
logctl analyze --run-dir .ai-temp/log-runs/manual/<timestamp>
```

但这个结果必须标记为 `resultSource=legacy-stdout-fallback`。因为 legacy console 缺 `owner / operation / entityId / sourceFile / reasonCode` 等字段，AI 只能做低可信分析。长期目标是引导用户保留 run dir / JSONL / artifact，而不是复制粘贴。
