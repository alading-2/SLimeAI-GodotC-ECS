# Logger 字符串与诊断分配设计

## 当前结论

字符串插值不是 P0 架构问题，也不应被简单说成“插值错、拼接对/错”。真正问题是当前 Logger API 不能延迟消息构造。

用户认为“字符串差值不是问题，字符串拼接才是问题”，这在方向上提醒了不要误伤插值语法。更准确的裁决是：

- 插值是推荐可读写法。
- 在传给 `Log.Debug(object)` / `Log.Info(object)` 这种普通参数前，插值表达式会先求值。
- 如果日志级别关闭但消息已构造，就已经产生了字符串分配。
- 所以要改的是 Logger API 和热路径调用方式，不是全仓禁 `$"..."`。

## 当初为什么这么设计

`Log` 当前目标是 Godot 输出友好、上下文过滤和 DEBUG 条件编译：

- `Trace` / `Debug` 标记 `[Conditional("DEBUG")]`，非 DEBUG 编译会被编译器忽略。
- `Warn` / `Error` 内部先 `ShouldLog` 再格式化。
- `LogInternal` 统一格式化颜色、时间戳和上下文。

这对调试体验有价值。问题是 `Info/Debug` 的调用点在进入方法前已经把 `$"..."` 变成字符串，`ShouldLog` 无法阻止调用点求值。

## 源码证据

| 文件 | 证据 |
| --- | --- |
| `Log.cs` | `Trace(object message)`、`Debug(object message)`、`Info(object message)` |
| `Log.cs` | `LogInternal` 内部会 `ShouldLog`，但无法阻止调用点参数构造 |
| `Log.cs` | `Warn/Error` 内部做 `ShouldLog` 后再 `FormatRawMessage` 和 `LogInternal` |
| 全仓 grep | 多处 `_log.Debug($"...")`、`Log.Warn($"{ctx} ...")`、`GD.Print($"...")` |

## 目标设计

### 1. 显式日志级别查询

```csharp
public bool IsEnabled(LogLevel level) => ShouldLog(level);
public bool IsDebugEnabled => ShouldLog(LogLevel.Debug);
public bool IsInfoEnabled => ShouldLog(LogLevel.Info);
```

调用点可写：

```csharp
if (_log.IsDebugEnabled)
{
    _log.Debug($"Target count={count}, origin={origin}");
}
```

这适合少量复杂日志，但如果全仓都这样写会污染代码。

### 2. Lazy message API

```csharp
public void Debug(Func<string> messageFactory)
{
    if (!ShouldLog(LogLevel.Debug)) return;
    LogInternal(LogLevel.Debug, messageFactory(), "DEBUG", ColorDebug, checkFilter: false);
}
```

调用点：

```csharp
_log.Debug(() => $"Target count={count}, origin={origin}");
```

注意：lambda 本身可能分配闭包。适合低/中频复杂日志，不适合每帧高频最内层。

### 3. Interpolated string handler

长期更合适：

```csharp
public void Debug([InterpolatedStringHandlerArgument("")] LogInterpolatedStringHandler message)
```

handler 在日志关闭时不追加内容，避免构造完整字符串。这个方案最适合保留调用点插值可读性，但实现复杂度高于 `IsEnabled`。

## 推荐路线

第一阶段：

- 给 `Log` 加 `IsTraceEnabled/IsDebugEnabled/IsInfoEnabled`。
- 对明确热路径高成本日志加门禁，尤其是循环、每帧 `_Process`、TargetSelector、Data/Timer diagnostics。
- 文档明确：不要在热路径把复杂 `$"..."` 直接传给无法延迟的日志 API。

第二阶段：

- 设计 `LogInterpolatedStringHandler`，让 `_log.Debug($"...")` 在关闭时低成本。
- 保留 `Info(string)` 等简单 API，避免一次性大改。

第三阶段：

- diagnostics dump 用 `StringBuilder` 或 formatter，避免多段 `+` / `string.Join(Select(...))` 在热路径运行。

## 字符串插值使用规则

| 场景 | 规则 |
| --- | --- |
| 初始化 / 测试 / Debug scene | 可以直接用插值 |
| Error / Warn | 可直接用；当前 API 已先做级别判断，但调用点插值仍会先求值，复杂消息建议加门禁 |
| 每帧 / 高频循环 / Target query / Data hot path | 必须 `IsEnabled` 或 handler |
| UI 文本拼装 | 按 UI 刷新频率处理，不纳入 ECS P0 |
| `string.Join(Select(...))` diagnostics | 只在 dump/export 时运行，不放 scheduler hot path |

## 验证门禁

```bash
dotnet build Brotato_my.csproj --no-restore /clp:ErrorsOnly
rg -n "_log\\.Debug\\(\\$|Log\\.Debug\\(\\$|_log\\.Info\\(\\$|Log\\.Info\\(\\$" Src/ECS/Runtime/Data Src/ECS/Runtime/Event Src/ECS/Tools/TargetSelector Src/ECS/Tools/Timer Src/ECS/Capabilities
```

注意：grep 命中不等于失败。需要按 owner 判断是不是热路径。

## 不推荐

- 不推荐把所有字符串插值改成 `+`。这可能更差，也降低可读性。
- 不推荐在 Data/Event P0 前投入大量时间做日志风格重写。
- 不推荐为了零分配删除关键错误日志；错误日志属于可诊断性，优先保留。
