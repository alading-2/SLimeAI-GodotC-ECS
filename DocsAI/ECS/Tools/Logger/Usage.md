<!-- migrated-from: Src/ECS/Tools/Logger/README.md -->

> 迁移来源：`Src/ECS/Tools/Logger/README.md`
> 迁移说明：本文主体从原 `Src/ECS` 文档迁入 `DocsAI` 统一管理；原 `Src/ECS` Markdown 文件已删除。

# Log 日志系统

> 当前文档描述 legacy API。AI-first Log / Observation 重构入口见 `README.md` 和 `SDD/project/projects/PRJ-0002-ecs-framework-refactor/design/Tool/10.Log/README.md`。

## 概述

**路径**: `Src/ECS/Tools/Logger/Log.cs`

**核心价值**:
- 统一的调试输出接口
- 分级过滤（Trace/Debug/Info/Success/Warning/Error）
- 条件编译优化（Release 版本零性能损耗）

## 快速开始

```csharp
// 推荐用法：每个类声明一个静态实例
private static readonly Log _log = new Log("ClassName");

public void MyMethod()
{
    _log.Trace("细粒度追踪");  // [Conditional("DEBUG")]
    _log.Debug("调试信息");    // [Conditional("DEBUG")]
    _log.Info("普通信息");
    _log.Success("成功提示");
    _log.Warn("警告");         // 自动推送到 Debugger 面板
    _log.Error("错误");        // 自动推送到 Debugger 面板
}
```

## 全局配置

```csharp
// 发布版本建议 Info 或更高
Log.GlobalLevel = LogLevel.Info;

// 针对特定类调试
Log.SetLevel("ClassName", LogLevel.Debug);
```

## 设计亮点

```csharp
// 实例化设计，每个类独立上下文
private static readonly Log Log = new Log("Player");

// 条件编译，DEBUG 模式外完全移除
[Conditional("DEBUG")]
public void Debug(object message) { }
```

## 使用场景

- **开发阶段**：追踪游戏逻辑流程
- **性能分析**：记录关键操作耗时
- **错误排查**：定位 Bug 来源

## 迁移提醒

- 不要在新测试中继续新增裸 `GD.Print("PASS")` / `GD.PushError("FAIL")` 作为主事实源。
- 不要把 `Success` 当作长期 severity 设计；后续目标是 `severity / outcome / validationStatus` 拆分。
- 高频 owner 新增日志前，应先补 owner `Log.md` 或 README `## Log`。
- AI-first 默认不使用 Godot API 打印详细日志；详细事实写 buffered JSONL，stdout 只写 summary。
- 不要把高密度详细日志简单改成每条 `Console.WriteLine`，否则只是把 Godot Output 噪声换成 stdout 噪声。
- 用户已确认：后续实现以 C# structured sink 为默认主链路，Godot editor sink 默认关闭。
