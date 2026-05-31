# Logger 概念

> status: current
> sourcePaths: Src/ECS/Tools/Logger/
> relatedDocs: DocsAI/ECS/Tools/Logger/Usage.md
> lastReviewed: 2026-05-30

## 1. 一句话定位

日志系统，封装 Godot 的 `GD.PrintRich` 支持 BBCode 颜色，6 个严重级别（Trace → Error），Release 条件编译排除。

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
