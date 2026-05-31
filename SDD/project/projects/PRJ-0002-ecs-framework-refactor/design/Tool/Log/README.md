# Log 工具设计包

> 更新：2026-05-31
> 状态：current design package
> 入口：`README.md`
> 裁决：Log 不是“输出文本”的工具，而是 AI-first 观测入口；必须同时服务运行调试、测试验证、scene runner 分析和人工排障。

## 0. 本设计包回答什么

当前旧 `Log` 的问题不是“等级不够多”，而是它仍然把日志当成字符串打印。

这带来四个直接问题：

- **信息不结构化**：AI 看到大量自然语言输出，仍要猜这条日志属于哪个模块、哪个实体、哪个阶段、哪个操作。
- **噪声太大**：运行几秒就几百条时，单靠 `Trace/Debug/Info/Warn/Error` 不足以控制可读性。
- **开关不灵活**：只靠全局等级或类级等级，难以在调试现场快速打开某个模块、关掉某类重复日志。
- **测试与日志分裂**：当前测试里同时存在 `GD.Print("PASS")`、`GD.PushError("FAIL")`、`_log.Error("[FAIL]")`、`throw Exception` 等做法，runner 又在用字符串 pattern 扫描失败信号，导致事实源不统一。

本设计包要解决的是：**把 Log 升级为 AI-first 观测层的入口，并把测试断言统一纳入同一套观测与 artifact 语义。**

## 1. 文件结构

| File | Role | 说明 |
| --- | --- | --- |
| `README.md` | design-index | 本文件。给出总裁决、阅读顺序、边界和完成定义。 |
| `01-现状分析与AI-first裁决.md` | research-decision | 当前 `Log`、测试场景、runner、Observation 原型与噪声问题分析。 |
| `02-目标架构与数据契约.md` | architecture | 定义结构化 Log envelope、profile、sink、CLI 控制和噪声预算。 |
| `03-控制面与CLI设计.md` | control-surface | 定义 `logctl`、运行时覆盖、AI 建议、日志预算和快照回写。 |
| `04-测试统一与Observation接入.md` | validation-observation | 定义测试如何统一通过 Log/Validation artifact 表达 PASS/FAIL。 |
| `05-调用点迁移与验证计划.md` | migration-test-plan | 旧日志调用点、测试 helper、runner 规则和验证门禁。 |

## 2. 总裁决

采用 **AI-first Observation Log**：

```text
业务日志 / 调试日志 / 测试断言 / 场景验证
  -> 统一进入结构化 Log envelope
  -> 输出到 console / jsonl / memory / artifact
  -> 由 CLI 和 profile 控制可见性
  -> 由 AI 分析建议降噪和规则调整
```

不采用：

- 不把 Log 继续限定为“打印字符串 + 颜色”。
- 不把 `GD.Print("PASS")`、`GD.PushError("FAIL")`、`throw`、`Log.Error("[FAIL]")` 当成分裂的测试事实源。
- 不把全局等级当成唯一控制面。
- 不让 AI 继续从自然语言日志里猜实体、阶段和原因。

## 3. AI-first 原则

| 旧问题 | AI-first 规则 |
| --- | --- |
| 一条日志只是一句话 | 一条日志必须是结构化事件，至少包含 `timestamp / level / channel / owner / context / operation / message / fields`。 |
| `Info` / `Debug` 只是打印级别 | 级别只是筛选维度，真正决定价值的是 `eventId`、`operation`、`entityId`、`correlationId` 和字段完整性。 |
| 测试靠 `PASS` 文本 | 测试结果必须进入统一 `Validation` 语义和 artifact。 |
| 运行时日志和 scene runner 分开 | runner 只负责收集、筛选、落盘和分析，不负责定义业务日志语义。 |
| 只靠手动开关 | 默认 profile + CLI 临时覆盖 + AI 建议回写三层控制。 |

## 4. 目标边界

| 模块 | 目标职责 | 禁止职责 |
| --- | --- | --- |
| `LogEntry` | 固定结构化 envelope，承载事实字段 | 不拼装业务流程，不猜默认语义。 |
| `LogProfile` | 默认等级、规则、预算、sink 组合 | 不执行业务逻辑。 |
| `LogManager` / `Log` | 生成事件、应用筛选、派发给 sink | 不直接承担测试断言。 |
| `LogSink` | console / jsonl / memory / artifact 输出 | 不决定哪些日志该产生。 |
| `logctl` | CLI 临时开关、查看热度、生成建议、应用建议 | 不把临时覆盖永久藏起来。 |
| `Validation` | PASS / FAIL / check / artifact 统一表达 | 不再单独用 `GD.Print` / `GD.PushError` 作为唯一结果。 |
| `Scene runner` | 注入 profile、收集输出、产出 gate report | 不分析业务规则本身。 |

## 5. 控制面裁决

Log 控制必须同时支持两层：

### 5.1 稳定事实源

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

### 5.2 CLI 临时控制

CLI 是现场调试入口，不是长期事实源。

CLI 适合做：

- 开某个 context。
- 关某个重复 channel。
- 切换某个 profile。
- 查看 top noisy contexts。
- 根据最近一次 run 生成建议。
- 把建议写回配置文件。

CLI 不适合单独承担永久配置，因为它不可复现，且下一次会话无法知道当时为何这么开。

### 5.3 结论

**配置文件负责默认策略，CLI 负责实时覆盖，AI 分析负责生成建议并回写配置。**

这三层缺一不可。

## 6. 噪声控制

Log 不是要“打更多信息”，而是要“打更少但更有用的信息”。

必须引入三种降噪机制：

1. **结构化字段约束**：没有 `operation`、`context`、`owner`、`entityId` 或 `reason` 的日志，默认不算高价值。
2. **重复合并**：短时间内相同 `eventId + context + entityId + operation + reason` 的日志必须聚合成摘要。
3. **预算控制**：每个 context / channel / owner / entity 都要有默认预算，超出后降级为摘要或只写 JSONL。

## 7. 测试统一

测试必须统一到同一套观测语义：

- `PASS` / `FAIL` 不再靠裸 `GD.Print`。
- `Check` / `Pass` / `Fail` 应写入结构化 artifact。
- runner 只解析 artifact + JSONL + exit code，不再靠松散字符串猜测。
- 负向测试允许产生错误级日志，但必须被标注为受控失败，不得污染 gate 结果。

## 8. 完成定义

Log 重构完成不是“还能打印”。

必须同时满足：

- 业务日志可通过结构化 envelope 输出到 console / jsonl / memory。
- CLI 能临时打开、关闭、查询和回写规则。
- 配置文件能作为默认事实源保存 profile。
- 测试断言与 scene runner 使用统一的 PASS / FAIL 观测语义。
- 同一类重复噪声能被合并或降级。
- AI 能从日志中直接读出模块、阶段、实体、操作和失败原因，而不是猜。

## 9. 阅读顺序

1. 先读 `01-现状分析与AI-first裁决.md`，确认为什么要重构。
2. 再读 `02-目标架构与数据契约.md`，确认结构化日志长什么样。
3. 再读 `03-控制面与CLI设计.md`，确认 CLI / profile / 回写怎么分工。
4. 再读 `04-测试统一与Observation接入.md`，确认测试怎么统一。
5. 最后读 `05-调用点迁移与验证计划.md`，看迁移和门禁。
