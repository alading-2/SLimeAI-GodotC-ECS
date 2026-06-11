# TDD 与测试系统

## 当前 TDD 状态

### TDD 规则写得很好，但执行基础设施断裂

SystemAgent 的 TDD 规则（`Rules/TDD.md`）定义了完整的 RED-GREEN-REFACTOR 微循环，但存在严重的执行基础设施问题：

**规则写了什么**：
- 铁律：没有失败测试就没有产品代码
- Runtime 测试用 NUnit，位于 `SlimeAI/Tests/SlimeAI.GameOS.Tests/`
- Godot 测试用 `SceneValidationSession.Check()`
- RV-TEST-COVERAGE 门强制检查测试覆盖

**实际情况**：
- `SlimeAI/Tests/SlimeAI.GameOS.Tests/` **目录不存在**
- 整个项目 **零 NUnit 测试文件**
- 没有 `dotnet test` 可用
- 所有测试都是 Godot 场景测试（每个 3-8 秒启动开销）

**结论**：TDD 规则是"纸面 TDD"——写了规则但没有可执行的基础设施。

## 测试的三个世代

### 第一代：旧式 Godot 场景测试（已过时）

**文件示例**：`ECSTest.cs`, `DamageSystemTest.cs`, `TargetSelectorTest.cs`, `MathRuntimeTest.cs`

**特征**：
- 继承 `Node`，在 `_Ready()` 中运行
- 每个文件重复手写 `AssertTrue`/`AssertEqual`/`Pass`/`Fail`
- 使用字符串 `"PASS: ..."` / `"FAIL: ..."` 判断结果
- 使用全局单例（`DamageService.Instance`）
- `MathRuntimeTest` 不调用 `GetTree().Quit()` 退出

**问题**：
- 精确违反了 test-system skill 中"不使用全局单例"的规则
- 精确违反了"不用 GD.Print 作为断言源"的规则
- 没有退出码，CI 无法判断通过/失败

### 第二代：重构后的 Godot 场景测试（当前主力）

**文件示例**：`EntitySpawnPipelineRuntimeTest.cs`, `AbilityInventoryServiceRuntimeTest.cs`, `NodeLifecycleRegistryRuntimeTest.cs`

**改进**：
- 使用 `Log` + `LogChannel.Validation` + `LogOutcome.Succeeded/Failed`
- 跟踪 `_passedCount` / `_failedCount`
- 调用 `GetTree().Quit(_failedCount == 0 ? 0 : 1)` —— 正确的退出码
- 使用显式隔离：`new EntityRegistry()`, `new LifecycleTree()` 等
- 定义 probe/mock 实体作为内部类

**仍存在的问题**：
- 仍然继承 `Node`，需要 Godot 引擎运行
- 每个文件仍然重复 `AssertTrue`/`AssertEqual`/`Pass`/`Fail` 方法
- 没有共享的测试基类
- 没有使用 `ValidationSession`（用的是直接 Log 调用）

### 第三代：LogTest（Logger 自测）

**文件**：`Src/ECS/Tools/Logger/Tests/LogTest.cs`

**最先进的测试**：
- 使用 `Log.ResetForTests()` 和 `Log.Configure()` 做隔离
- 使用 `MemorySink` 在内存中捕获日志条目
- 程序化验证 JSON 输出
- 但仍然作为 Godot 场景运行

## 测试基础设施的关键缺失

### 缺失 1：NUnit 测试项目

这是最大的缺口。TDD 规则引用的 `SlimeAI/Tests/SlimeAI.GameOS.Tests/` 不存在。

**影响**：
- 没有 `dotnet test` 命令可用
- RED 步骤（运行测试看它失败）不能快速执行
- 每次测试需要 Godot 引擎启动（3-8 秒）
- 没有 CI 集成

**需要做的事**：
1. 创建 `SlimeAI/Tests/SlimeAI.GameOS.Tests.csproj`（引用 NUnit + 项目）
2. 创建共享测试基类 `TestBase.cs`
3. 迁移纯逻辑测试到 NUnit（不涉及 Godot 的测试）
4. 保留 Godot 场景测试用于引擎相关功能

### 缺失 2：共享断言库

每个测试文件重复 20-30 行断言方法。

**需要做的事**：
创建共享 `TestBase.cs`：

```csharp
public abstract class TestBase
{
    private int _passedCount;
    private int _failedCount;
    private Log _log;

    protected void AssertTrue(string name, bool condition, string? message = null)
    {
        if (condition) { _passedCount++; _log.Validation(name, LogValidationStatus.Pass); }
        else { _failedCount++; _log.Validation(name, LogValidationStatus.Fail, message); }
    }

    protected void AssertEqual<T>(string name, T expected, T actual)
    {
        bool pass = EqualityComparer<T>.Default.Equals(expected, actual);
        AssertTrue(name, pass, $"Expected: {expected}, Actual: {actual}");
    }

    protected int ExitCode => _failedCount == 0 ? 0 : 1;
}
```

### 缺失 3：ValidationSession 未被采用

`ValidationSession` 是设计良好的测试断言 API（5 字段标准答案 + artifact 输出），但几乎没有测试使用它。只有 `LogTest.cs` 在用。

**原因**：
- 测试文件写的时候 `ValidationSession` 还不存在或不够成熟
- 直接用 `Log` 调用更简单
- 没有强制要求（RV-TEST-COVERAGE 门没有被严格执行）

### 缺失 4：BDD 到测试的自动追溯

SDD 的 `bdd.md` 定义了 Given/When/Then 场景，但没有工具将 BDD 场景映射到测试方法。TestDesigner 设计的验证场景和实际测试代码之间的连接是手动的。

## Logger 在测试中的角色

### Log 系统架构

```
Log 系统
├── 核心类型
│   ├── LogSeverity: Trace/Debug/Info/Warn/Error/Fatal
│   ├── LogOutcome: Started/Completed/Succeeded/Failed/Skipped/Suppressed
│   ├── LogValidationStatus: Pass/Fail/Skip/ExpectedFailure
│   └── LogChannel: Runtime/Validation/Flow/Diagnostics
│
├── Sink 架构（5 个）
│   ├── StdoutSummarySink —— 人类可读摘要
│   ├── JsonlBufferedFileSink —— 结构化 JSONL
│   ├── MemorySink —— 内存捕获（测试用）
│   ├── ArtifactSink —— Validation 通道 → JSON artifact
│   └── GodotEditorSink —— 编辑器调试（默认关闭）
│
├── 预算系统 —— 速率限制重复日志
│
└── 测试支持
    ├── Log.ResetForTests() —— 隔离测试配置
    ├── MemorySink —— 程序化断言
    └── ValidationSession —— 结构化断言 + artifact
```

### 测试中如何使用 Log

**正确模式**（第二代测试）：
```csharp
var log = new Log("TestOwner");
// ... 运行测试 ...
if (result == expected)
    log.Write(LogChannel.Validation, LogSeverity.Info, "test-name",
        outcome: LogOutcome.Succeeded, validationStatus: LogValidationStatus.Pass);
else
    log.Write(LogChannel.Validation, LogSeverity.Error, "test-name",
        outcome: LogOutcome.Failed, validationStatus: LogValidationStatus.Fail);
```

**更好模式**（使用 ValidationSession）：
```csharp
using var session = ValidationSession.Start(new ValidationSessionOptions
{
    Name = "EntitySpawnTest",
    ExpectedInputs = "3 probe entities",
    ExpectedObservations = "spawn count = 3, log entries = 3",
    PassCriteria = "count == 3 && all logs Info",
    FailCriteria = "count != 3 || any log Error",
    ArtifactPath = ".ai-temp/scene-tests/artifacts/entity-spawn.json"
});

var result = registry.Spawn(probe1);
session.Check("probe1 spawned", result != null, expected: "non-null", actual: result?.ToString());
// ... 更多 check ...
// Dispose 时自动写 artifact
```

### Flow 打印 vs 逐条打印

**逐条打印**（当前主流）：
```
[INFO] probe1 spawned: Pass
[INFO] probe2 spawned: Pass
[INFO] probe3 spawned: Pass
[INFO] Total: 3 passed, 0 failed
```
问题：打印信息散乱，分析时需要自己串联。

**Flow 打印**（建议用于功能测试）：
```
[INFO] EntitySpawnPipeline started: 3 entities to spawn
[INFO]   ├─ probe1: spawned → registered → lifecycle-callback: Pass
[INFO]   ├─ probe2: spawned → registered → lifecycle-callback: Pass
[INFO]   └─ probe3: spawned → registered → lifecycle-callback: Pass
[INFO] EntitySpawnPipeline completed: 3/3 passed (flow: 12 entries, 0 errors)
```
优势：流程清晰，AI 可以直接从 flow 判断功能是否正常。

**建议**：
- 单元测试用逐条打印（每个断言独立）
- 功能/集成测试用 Flow 打印（通过 `LogChannel.Flow` + `LogOutcome.Started/Completed`）
- 结合 `logctl analyze` 工具分析 flow 结构

## TDD/Test 优化方向

详见优化设计文档：`SDD/project/projects/PRJ-0001-systemagent-optimization/design/优化/TDD-测试系统优化设计.md`
