# Logger 概念

> status: current-legacy
> sourcePaths: Src/ECS/Tools/Logger/
> relatedDocs: DocsAI/ECS/Tools/Logger/README.md, DocsAI/ECS/Tools/Logger/Usage.md, SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Tool/10.Log/README.md
> lastReviewed: 2026-06-09

## 1. 一句话定位

当前 legacy 日志系统，封装 Godot 的 `GD.PrintRich` 支持 BBCode 颜色，6 个严重级别（Trace → Error），Release 条件编译排除。

AI-first Log / Observation 目标设计不以本文为事实源，需读取 `README.md` 和 PRJ-0002 `design/Tool/10.Log/README.md`。

2026-06-09 sink 裁决：Godot rich/editor 输出只保留为人工调试 sink；AI-first 默认详细事实写 C# buffered JSONL file，runner 可见摘要写 C# stdout summary，Validation 事实写 memory/artifact。

用户已确认：C# 输出链路更适合 AI-first 默认主链路，但不是逐条 `Console.WriteLine` 替代 `GD.PrintRich`；正确方案是 C# structured sink，也就是 buffered JSONL + stdout summary + artifact。

## 2. 核心概念

### 严重级别

```
Trace → Debug → Info → Warn → Success → Error
```

### 推荐模式

每个类一个静态实例：

```csharp
private static readonly Log _log = new(nameof(MyClass));
_log.Info("消息");
_log.Error("错误");
```

### Release 编译

Trace/Debug 级别在 Release 编译时被条件编译排除。

## 3. 职责边界

| Logger 做 | Logger 不做 |
| ---- | ---- |
| 统一日志输出 | 性能分析 |
| BBCode 颜色支持 | 日志文件持久化 |
| 级别过滤 | 错误上报 |

## 4. 当前边界

- 本文描述当前实现，不代表后续推荐架构。
- 后续不建议继续只扩展颜色、墙钟时间或 `GlobalLevel`。
- 测试 PASS/FAIL、runner analyzer、owner `Log.md` 和结构化 Observation 规则以 PRJ-0002 Log 设计包为准。
- 不要把 legacy `GD.PrintRich` 方案迁移成“每条日志改用 `Console.WriteLine`”；高密度详细日志应 buffered 写 JSONL，stdout 只输出摘要。
