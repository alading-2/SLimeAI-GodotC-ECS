# 第二部分：语义提炼整理

> 更新：2026-06-11
> 状态：current contract（analyzer 默认入口已落地）
> 入口：本文件

## 这部分回答什么

第一部分把日志"记录"成了结构化 JSONL，但"整理"被做成了"按维度复制 + 全量展开"，结果 4914 行变 113864 行，还看不出一次操作成没成功。

第二部分只改一件事：**把整理的单位从「一条日志」换成「一次操作(flow 结论)」**，让产物以结论为主、行数显著变少、失败一眼可见。

## 阅读顺序

1. `03-最终设计与完成清单.md` —— 当前事实源：最终契约、已完成/未完成、验证命令和恢复点。
2. `00-为什么需要第二部分.md` —— 复盘：为什么 deepthink 做了、需求写了，功能还是没落地。根因是"把结果(提炼)翻译成了形状(目录)"。
3. `01-语义提炼整理设计.md` —— 方案来源：flow 树模型、结论对象、字段上提、failure-first 呈现、模板聚合、时间字段修正、用结果而非存在性做验收。
4. `04-当前实现审查报告.md` —— 当前实现审查：按 G1~G8 和 SDD 剩余项核对当前代码，说明哪些完成、哪些仍不能声称完成。
5. `02-代码审查与落地修正.md` —— 修复前代码审查快照：解释当时为什么判定 `analysis-next` 是 PARTIAL / WRONG，并沉淀 G1~G8 gate。

## 核心结论（一页纸）

| 维度 | 第一部分（保留） | 第二部分（新增） |
| --- | --- | --- |
| 整理单位 | 一条 LogEntry | 一次操作 = correlationId flow 树 |
| 产物主体 | by-owner/by-phase/flows 全量复制 | `flows/flows.jsonl` 结论对象 + `noise/templates.jsonl` 成功模板 |
| 体量 | 113864 行（放大 23×） | 当前样本默认可读 303 行，raw 4915 行，比例 0.062 |
| 看成败 | 要读 raw 拼流程 | summary 第一屏：成功 N / 失败 M + 失败清单 |
| 高频日志 | 3041 条逐条展开 | 模板 + 计数 + min/avg/max + rawRefs |
| 时间 | 每行 timestampUtc 墙钟打头 | 默认不写 wallClock；显式 profile 才写 `wallClockUtc` |
| 验收 | 文件存在即通过 | 行数下降 / 失败可定位到步 才通过 |

## 当前代码审查结论

`04-当前实现审查报告.md` 已确认：

- 记录层是 DONE：结构化 entry、sink、profile、gate 状态语义都已落地。
- 整理层默认入口已修正：`logctl analyze` 不再默认生成 `by-owner` / `by-phase` / pretty `flows.json`，复跑同一 output 时会清理这些 stale 旧入口，并改写 `flows/flows.jsonl`、`flows/index.md`、`noise/templates.jsonl`、`noise/templates.md`、`analysisQuality`。
- 时间字段默认已修正：`timestampUtc` 删除；`wallClockUtc` 默认不写，只能通过 profile/overrides 显式开启。
- 旧样本仍是 stale evidence：3730 个 correlationId 仍多为 single-entry flow，说明样本缺 step，不代表新 `OperationTrace` 契约已在该样本中复跑。
- SDD-0040 仍是 blocked：T2.6 Validation artifact adoption 和最终 Godot runner smoke 未完成，不能报告场景行为通过。

第二部分 analyzer 默认入口必须持续满足 `04` 中的 G1~G8 gate，才允许把“整理”标成完成；`02` 保留为修复前审查和防复发证据。

## 与第一部分的关系

- 不推翻第一部分。记录层（LogEntry、sink、profile、Validation）保留。
- 只替换"整理层"的数据单位和验收口径。
- 第一部分的实施清单（07 文档 T2.1~T2.7）里"补 markdown digest"部分仍有效；第二部分把它的**验收口径**从"产物存在"升级为"结果达标"，并补上它漏掉的"以 flow 为单位重建"。

## 需要确认

当前采用的默认假设：

1. **语义模型**：以 correlationId 重建 flow conclusion；成功折叠进模板，失败/警告保留结论对象。
2. **取消物理分桶**：默认不生成且清理 stale `by-owner/*.jsonl`、`by-phase/*.jsonl` 和 pretty `flows.json`；二次筛选走 `logctl query --analysis-dir` 的语义索引，raw 下钻必须显式 `--file`。
3. **flow 结论字段集**：先固定 `flowId/type/outcome/durationMs/frameRange/failedStep/reasonCode/keyFields/entryCount/rawRef/qualityFlags`，owner 细节后续扩展。
4. **墙钟时间**：raw 低层证据允许显式 profile 开启 `wallClockUtc`；默认不写，AI 默认产物不展示。
5. **SDD 状态**：analyzer 语义提炼和 T2.5 owner hot-spot cleanup 当前范围已完成；T2.6 Validation artifact adoption 和最终 Godot runner smoke 仍独立追踪。
