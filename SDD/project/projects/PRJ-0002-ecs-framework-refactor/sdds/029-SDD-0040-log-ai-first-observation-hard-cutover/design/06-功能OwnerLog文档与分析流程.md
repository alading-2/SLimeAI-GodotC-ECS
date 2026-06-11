# 功能 Owner Log 文档与分析流程

> 更新：2026-06-11
> 状态：historical design note；当前 analyzer 默认入口以项目级第二部分 `03-最终设计与完成清单.md` 为准
> 提醒：本文保留 owner Log.md 模板和分析流程思路；旧 `by-owner` / `by-phase` raw 分桶已经废弃，owner/phase 筛选改走 `logctl query --analysis-dir` 的语义索引。

## 1. 定位

Log 重构不能只改 `Log.cs`。每个 Runtime / Capability / Tools / UI owner 都必须说明自己“打什么日志、为什么打、怎么分析、哪些默认关闭”。否则后续 AI 仍会在代码里随手加日志，最后又变成全量 stdout 交给 AI 猜。

本文件定义 owner 级 `Log.md` 模板和 `logctl analyze/query` 输出后的固定 AI 分析流程。

## 2. Owner Log 文档位置

优先规则：

- 复杂 owner：新增 `DocsAI/ECS/<Runtime|Capabilities|Tools|UI>/<Owner>/Log.md`。
- 简单 owner：在现有 README 中新增 `## Log` 小节。
- 有执行型 SDD 时：SDD design 先写完整 Log 方案，执行完成后同步到 DocsAI owner 文档。

不允许只把日志规则写在源码注释或临时 notes 里。

## 3. Owner Log.md 模板

```markdown
# <Owner> Log

> status: current
> sourcePaths: <source paths>
> analyzerInputs: summary / ai-context / flows / success templates / failures / noise / missing-fields / rawRef
> lastReviewed: YYYY-MM-DD

## 1. Log 思路

- 该 owner 的关键业务事实是什么。
- 哪些事实必须结构化记录，哪些只适合人工 console。
- 哪些日志默认关闭，只有 debug profile 打开。

## 2. 关键 Flow

| Flow | 触发 | 必填字段 | 成功判断 | 失败判断 | 默认输出 |
| --- | --- | --- | --- | --- | --- |
| AbilityCast | 技能释放 | abilityId/entityId/targetId/cost/cooldown/outcome | outcome=Succeeded | reasonCode 非空或 validation fail | console summary + jsonl full |

## 3. 字段契约

- 必填字段：
- 可选字段：
- 禁止字段：
- 稳定 reasonCode：

## 4. 噪声预算

- console：
- jsonl：
- flow step：
- suppression summary：

## 5. Sink 策略

- stdout summary：
- jsonl buffered file：
- memory/artifact：
- godot editor sink：

## 6. 怎么分析 Log 判断是否有问题

1. 先读 `summary.md` 和 `ai-context.md` 的 owner 摘要。
2. 再用 `logctl query --analysis-dir <analysis> owner=<Owner>` 筛选 `flows/flows.jsonl` 和 `noise/templates.jsonl` 的语义结论。
3. 再读 `flows/index.md`、`failures/index.md` 和 `missing-fields/index.md` 判断失败、警告或日志缺口。
4. 只有结论对象的 `rawRef` 不足以解释问题时，才显式 `logctl query --file <analysis>/raw/entries.jsonl ...` 下钻原始 entry。

## 7. 测试与 artifact

- 场景：
- artifact：
- passCriteria：
- failCriteria：

## 8. 默认关闭项

- 高频每帧状态：
- 重复成功路径：
- 纯 UI 展示刷新：
```

## 4. Flow 设计规则

### 4.1 什么时候必须用 Flow

满足任一条件就应使用 `OperationTrace` / flow summary：

- 一个过程跨多个系统或组件。
- 过程需要按步骤判断是否正确。
- 单次过程会产生多条日志。
- AI 经常需要把多条日志拼起来理解。
- 过程失败原因可能来自多个分支。

典型 flow：

| Owner | Flow | 核心问题 |
| --- | --- | --- |
| Ability | `AbilityCast` / `AbilityAutoTrigger` | 技能为什么能/不能释放，目标、消耗、冷却、效果是否正确。 |
| Damage | `DamageProcess` | 每个 processor 如何改变伤害，是否被阻断。 |
| TargetSelector | `TargetQuery` | 候选来源、过滤、排序、截断、随机 seed 是否正确。 |
| Projectile | `ProjectileHitLifecycle` | 命中、穿透、归属、销毁/回池是否正确。 |
| ObjectPool | `PoolGetReleaseActivate` | 租借、回收、停放、首帧 embargo 是否正确。 |
| Timer | `TimerScheduleDispatchCancel` | owner/purpose、到期、取消、dispatch 是否正确。 |
| Runtime/System | `SystemPreflight` / `SystemExecutePhase` | 系统是否加载、阻断、执行、诊断是否完整。 |
| Validation | `ValidationSceneRun` | 输入、检查、结果、artifact 是否完整。 |

### 4.2 Flow 输出规则

console 默认只输出：

```text
[FLOW:<operation>] owner=<owner> phase=<phase> entity=<id> outcome=<outcome> durationMs=<n> steps=<n> reason=<reason>
```

JSONL / artifact 输出完整：

- inputs
- decisions
- checks
- outputs
- counters
- samples
- failureReasons
- suppressedCount

sink 固定规则：

- 默认详细事实只进 JSONL / artifact，不逐条刷 stdout。
- 默认 stdout 只显示 flow summary、validation verdict、关键 warn/error。
- Godot editor sink 默认关闭；owner 文档只能声明“人工 editor debug 时打开”，不能把它作为 AI 分析主路径。
- 如果某个 owner 需要把 warning/error 同步到 `GD.PushWarning` / `GD.PushError`，必须说明触发条件和为什么 artifact / stdout summary 不够。

### 4.3 过程失败优先字段

失败 flow 必须有：

- `reasonCode`
- `failureReason`
- `lastSuccessfulStep`
- `failedStep`
- `expected`
- `actual`
- `owner`
- `operation`
- `correlationId`

AI 分析失败时优先看这些字段，不优先读 message。

## 5. Analyzer 目录

2026-06-11 后，`logctl analyze` 默认输出目录固定为语义入口：

```text
<run>/analysis/
  summary.md
  ai-context.md
  gate-report.json
  raw/
    entries.jsonl
  flows/
    index.md
    flows.jsonl
  failures/
    index.md
  noise/
    templates.jsonl
    templates.md
    top-contexts.md
  missing-fields/
    index.md
```

AI 默认只读：

1. `summary.md`
2. `ai-context.md`
3. `flows/index.md` 与 `flows/flows.jsonl` 中失败或异常 outcome 的 flow conclusion
4. `noise/templates.md` 判断高频成功路径是否已聚合
5. `missing-fields/index.md` 和 `failures/index.md`
6. 必要时才通过 `rawRef` 显式下钻 `raw/entries.jsonl`

默认不生成 `by-owner` / `by-phase` raw 复制分桶，也不生成 pretty `flows/flows.json`。这能避免“把所有打印信息按目录复制一遍再丢给 AI”。

`logctl query` 必须能在该目录上做二次筛选：

```text
logctl query --analysis-dir <run>/analysis owner=Ability
logctl query --analysis-dir <run>/analysis operation=DamageProcess severity>=Warn
logctl query --file <run>/analysis/raw/entries.jsonl sourceFile=Src/ECS/Capabilities/Ability/System/AbilitySystem.cs
```

二次筛选输出不替代 `ai-context.md`，它用于在已有分析结果上缩小范围。AI 仍必须先读 summary / ai-context，再按 owner Log.md 判断。

## 6. AI 分析流程

固定分析顺序：

1. **确认 run 结果来源**：artifact / structured-log / stdout-pattern-fallback。
2. **确认 phase**：失败发生在 Boot、DataLoad、Validation、Gameplay、Combat 还是 Unknown。
3. **确认 owner**：优先从 structured owner 字段判断，不从自然语言 message 猜。
4. **确认 flow**：读失败 flow summary，再读失败 step。
5. **确认检查项**：读 Validation check expected/actual/reasonCode。
6. **确认噪声**：看 `noise/top-contexts.md` 和 `suppressed.md`，判断是否需要调整 profile。
7. **确认日志缺口**：看 `missing-fields/index.md`，区分“代码错了”和“日志不够判断”。
8. **输出结论**：必须分成 Code issue / Test issue / Log gap / Unknown。

## 7. 问题分类

AI 分析时必须使用固定分类：

| 分类 | 含义 | 下一步 |
| --- | --- | --- |
| Code issue | 结构化日志足以证明代码行为不符合预期 | 定位 owner 源码并修复。 |
| Test issue | 代码行为正确，但测试 expected/actual 或场景输入错误 | 修测试或测试数据。 |
| Log gap | 现有日志不足以判断 | 补 owner `Log.md` 和对应字段/flow，再复跑。 |
| Runner issue | Godot 场景运行、artifact 收集、exit code 或 run dir 保存错误 | 修 runner / godot-scene-test wrapper。 |
| Log CLI issue | `logctl analyze/query` 拆分、筛选或 gate report 生成错误 | 修 Log CLI / analyzer。 |
| Profile issue | 有价值日志被关闭或噪声压制策略错误 | 调整 profile/overrides。 |
| Unknown | 证据不足且无法安全推断 | 明确缺口，不猜。 |

## 8. 第一批 Owner 文档建议

优先写：

1. `DocsAI/ECS/Tools/Logger/README.md`：Log 工具 owner 当前入口。
2. `DocsAI/ECS/Runtime/Data/Log.md`：DataOS tests 和 snapshot/runtime apply。
3. `DocsAI/ECS/Runtime/System/Log.md`：System preflight、diagnostics、phase。
4. `DocsAI/ECS/Runtime/Entity/Log.md`：spawn/destroy/lifecycle/reference。
5. `DocsAI/ECS/Capabilities/Ability/Log.md`：AbilityCast flow。
6. `DocsAI/ECS/Capabilities/Damage/Log.md`：DamageProcess flow。
7. `DocsAI/ECS/Capabilities/Movement/Log.md`：MovementCollision flow。
8. `DocsAI/ECS/Tools/ObjectPool/Log.md`：PoolGetReleaseActivate flow。
9. `DocsAI/ECS/Tools/Timer/Log.md`：TimerScheduleDispatchCancel flow。
10. `DocsAI/ECS/Tools/TargetSelector/Log.md`：TargetQuery flow。

## 9. TODO

- TODO：把 TestSystem、scene-gate、runner 和 ValidationSession 的关系整理成单独测试/观察面设计。
- TODO：实现后补 analyzer 示例 artifact，并用一次失败 run 验证 AI 不读全量 stdout 也能定位问题。
- TODO：根据真实运行日志统计第一版 noise budget，当前文档只给结构和默认策略。
- TODO：明确未来是否需要 live socket/file watcher 实时控制；第一版默认环境变量 + override 文件即可。
