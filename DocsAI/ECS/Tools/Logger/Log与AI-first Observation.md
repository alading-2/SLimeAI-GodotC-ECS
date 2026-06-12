# Log 与 AI-first Observation

> 状态：current
> 作用：说明 Log 在 AI-first 架构中的定位、三层结构、当前完成边界，以及它为什么是 AI debug 的关键 Observation substrate。

## 一句话定位

`Src/ECS/Tools/Logger/Log.cs` 不是普通打印工具，也不是 SystemAgent 本体；它是 **AI-first Observation substrate**，负责把运行时事实整理成 AI 可复查、可分析、可判定的证据。

## 为什么需要它

AI 调试最怕两类情况：

1. **信息分散**：一次业务动作被拆成十几条 `_log.Info`、测试说明、逐步成功文本，AI 需要自己把流程拼起来。
2. **证据不稳定**：只有 `GD.Print("PASS")`、stdout 文本、`exit code 0`，AI 只能猜“可能没问题”。

Log 的目标不是“多打日志”，而是：

```text
让一次业务动作
  -> 产生一个稳定 flow / summary / validation artifact
  -> AI 能先判断结果，再按需下钻 raw 证据
```

## 它不是什么

- **不是 SystemAgent control plane**：它不定义 workflow、actor、gate、skill。
- **不是单纯 Console/Godot 输出替代品**：把 `GD.PrintRich` 机械换成 `Console.WriteLine` 只是换一种噪声。
- **不是“analyzer 完成 = 整个 Log 完成”**：离线整理层完成，不等于源码调用点已经语义化。

## 它在 AI-first 工程层中的位置

```text
AI-first 工程层
├── Control Plane
│   └── SystemAgent / SDD / Skill / Review Gate / Retrospective
└── Evidence Plane
    └── Log / Validation / Test / artifact / logctl
```

SystemAgent 负责 **AI 怎么做事、怎么验证、怎么复盘**。

Log 负责 **拿什么去判断这件事是否成功、失败、跳过、证据不足**。

## 三层结构

Log 当前必须按三层理解：

| 层 | 解决的问题 | 当前状态 | 典型产物 |
| --- | --- | --- | --- |
| 记录层 | 一条日志怎样成为结构化事实 | 第一版已落地 | `LogEntry`、sink、profile、`ValidationSession` |
| 离线整理层 | raw 日志怎样提炼成 AI 默认入口 | 第一版已落地 | `logctl analyze`、`summary.md`、`ai-context.md`、`flows/` |
| 源码调用点语义化层 | 源码在什么时机、以什么粒度写什么事实 | **未完成** | owner flow contract、live stdout policy、debug profile |

设计入口：

- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Tool/10.Log/README.md`
- `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Tool/10.Log/第三部分-源码调用点语义化/README.md`

## 当前已完成什么

### 记录层

当前已经有：
- structured `LogEntry`
- `severity / outcome / validationStatus` 拆分
- `StdoutSummarySink`、`JsonlBufferedFileSink`、`MemorySink`、`ArtifactSink`
- `ValidationSession`
- `Config/Log` profile / rules / overrides

### 离线整理层

当前已经有：
- `logctl analyze`
- `summary.md`
- `ai-context.md`
- `flows/index.md`
- `flows/flows.jsonl`
- `noise/` / `missing-fields/` / `failures/`

这意味着：**AI 对“已有日志”的分析质量已经显著提升。**

## 当前还没完成什么

真正没完成的是第三层：**源码调用点语义化**。

也就是：
- `Src/ECS` 大量业务、测试、Debug UI 还在输出普通 `_log.Info` / `_log.Debug` / `_log.Success`
- 少量旧测试仍在用 `GD.Print` / `PASS` / `FAIL`
- live stdout 仍可能呈现“分散消息”，而不是“一个流程一个结论”

所以现在不能说：

- “Log 已全部改完”
- “analyzer done = Log done”
- “stdout 变少了 = AI-first Observation 完成了”

## 什么样的日志才算 AI-first

目标不是让日志更长，而是让 **流程结论稳定**：

```text
用户释放技能
  -> AbilityCast flow conclusion
     outcome=Succeeded/Failed/Skipped
     failedStep=...
     reasonCode=...
     keyFields={caster, abilityId, target, cost, cooldown, spawnedEffects}
     rawRef=...
```

AI 默认应该先看到：
- 这次动作成功了吗？
- 如果失败，失败在哪一步？
- 证据在哪个 flow / artifact / raw entry？

而不是先读十几条自然语言顺序日志。

## 与 Validation / Test 的关系

Log 不是只给 Debug 用，它同时是：

- **Test 的断言承载层**：`ValidationSession` + artifact
- **Review Gate 的证据输入**：artifact oracle / structured failure / no-failure-observed
- **Retrospective 的复盘材料**：flow、noise、missing-fields、tool failure

因此它不是“测试打印工具”，而是 **测试与调试共用的证据底座**。

## 与 SystemAgent 的关系

SystemAgent Debug workflow 关心的是：
- 怎么复现
- 怎么定位根因
- 什么算验证通过
- 什么证据可以被 Reviewer / Verifier 接受

Log 提供的是：
- 运行时事实
- flow 结论
- Validation artifact
- 离线分析入口

可以把它理解成：

```text
SystemAgent = Control Plane
Log = Observation substrate in Evidence Plane
```

对应 Debug workflow 说明见：

- `Workspace/SystemAgent/Docs/10-Debug工作流与证据链.md`

## 默认阅读顺序

1. 读 `DocsAI/ECS/Tools/Logger/README.md`，确认当前实现边界。
2. 读本文，确认 Log 在 AI-first 中的定位和三层结构。
3. 读 `Usage.md`，确认 structured API、`ValidationSession`、`logctl` 用法。
4. 读 PRJ-0002 Log 设计包，确认当前阶段是否在改记录层、整理层还是调用点语义化层。
5. 调试具体 run 时：先 `logctl analyze`，先看 `summary.md` / `ai-context.md` / `flows/`，最后才 raw 下钻。

## 禁止误判

- 不把 clean stdout 当通过证据。
- 不把 `no-failure-observed` 说成 `passed`。
- 不把 live 打印仍然分散的现象归因给 analyzer。
- 不在第三层未完成时声称“整个 Log 已经 AI-first 化”。
