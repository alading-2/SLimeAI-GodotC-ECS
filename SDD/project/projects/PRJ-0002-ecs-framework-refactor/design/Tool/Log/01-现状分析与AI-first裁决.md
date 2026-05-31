# 现状分析与 AI-first 裁决

> 更新：2026-05-31
> 状态：current design note

## 1. 当前事实

当前 `Src/ECS/Tools/Logger/Log.cs` 仍是传统文本日志：

- 6 个等级：`Trace / Debug / Info / Success / Warning / Error`。
- 支持全局等级和按 context 设置等级。
- `Trace` / `Debug` 依赖 `Conditional("DEBUG")`。
- `Warn` / `Error` 会额外推送到 Godot Debugger。
- 输出主体仍是 `GD.PrintRich` 的字符串拼接。

这套设计能工作，但不适合 AI-first 的高密度调试场景。

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

- `timestamp`
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
- `phase`
- `source`
- `tags`

### 3.3 日志不是越多越好

AI-first 不是把每一帧都刷满，而是保证：

- 关键事实保留。
- 重复噪声压缩。
- 低价值日志默认关闭。
- 开关可以实时调。
- 调整结果可以回写。

## 4. 裁决摘要

本次重构采用：

- **结构化事件优先**。
- **配置文件为默认事实源**。
- **CLI 为临时覆盖入口**。
- **AI 建议可以回写配置**。
- **测试 PASS / FAIL 统一进观测层**。
