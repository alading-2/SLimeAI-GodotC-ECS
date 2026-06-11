# TDD 与测试系统优化设计

> 来源：SystemAgent 深度分析
> 日期：2026-06-11
> 优先级：P0

## 问题陈述

SystemAgent 的 TDD 规则写得完整（RED-GREEN-REFACTOR 微循环、铁律、RV-TEST-COVERAGE 门），但执行基础设施严重断裂：

1. **NUnit 测试项目不存在**：规则引用的 `SlimeAI/Tests/SlimeAI.GameOS.Tests/` 目录不存在
2. **没有共享测试基类**：每个测试文件重复 20-30 行断言方法
3. **ValidationSession 未被采用**：设计良好的 API 但几乎没有测试使用
4. **所有测试都是 Godot 场景测试**：每次 3-8 秒启动开销
5. **没有 CI 集成**：没有 `dotnet test` 可用
6. **BDD 到测试无追溯**：SDD 的 bdd.md 和实际测试代码之间没有自动连接

**结果**：TDD 是"纸面规则"——AI 可以声称"我遵循了 TDD"，但实际上 RED 步骤（快速运行测试看它失败）无法高效执行。

## 根因分析

### 为什么测试写不好

1. **没有标准模板**：AI 不知道"好的测试"长什么样，所以每次都从零发明
2. **断言方法不统一**：有的用字符串比较 `"PASS: ..."`, 有的用 Log 结构化, 有的用 ValidationSession
3. **测试粒度不明确**：单元测试 vs 功能测试 vs 集成测试的边界不清晰
4. **验证标准不明确**：什么算"测试通过"？exit code 0？stdout 有 PASS？artifact 有 Pass？
5. **Flow 信息不足**：测试打印的信息散乱，AI 无法快速判断功能是否正常

### 为什么需要 Log

Log 系统是连接"测试执行"和"AI 判断"的桥梁：

```
测试代码 → Log (Validation channel) → MemorySink (程序化断言)
                                    → ArtifactSink (JSON artifact)
                                    → JsonlBufferedFileSink (完整日志)
                                    → StdoutSummarySink (人类可读)
```

AI 不能直接"看"测试运行结果，但可以读 Log 产出的 artifact 和结构化日志。

## 设计方案

### 方案 1：创建 NUnit 测试项目（P0）

#### 目标

让纯逻辑测试（不涉及 Godot 引擎的测试）可以在毫秒级运行。

#### 实现

1. 创建 `SlimeAI/Tests/SlimeAI.GameOS.Tests.csproj`
2. 引用 NUnit + 框架项目
3. 创建共享测试基类
4. 迁移第一批纯逻辑测试

#### 共享测试基类设计

```csharp
namespace SlimeAI.GameOS.Tests;

/// <summary>
/// 所有 SlimeAI 测试的共享基类。
/// 提供统一的断言方法、日志集成和退出码管理。
/// </summary>
public abstract class TestBase
{
    private int _passedCount;
    private int _failedCount;
    private readonly string _owner;

    protected TestBase(string owner = "UnitTest")
    {
        _owner = owner;
    }

    /// <summary>
    /// 断言条件为真，记录到 Validation channel。
    /// </summary>
    protected void AssertTrue(string checkName, bool condition, string? message = null)
    {
        if (condition)
        {
            _passedCount++;
            // 使用 Log 结构化记录
            Log.Write(LogChannel.Validation, LogSeverity.Info, checkName,
                owner: _owner,
                outcome: LogOutcome.Succeeded,
                validationStatus: LogValidationStatus.Pass);
        }
        else
        {
            _failedCount++;
            Log.Write(LogChannel.Validation, LogSeverity.Error, checkName,
                owner: _owner,
                outcome: LogOutcome.Failed,
                validationStatus: LogValidationStatus.Fail,
                message: message ?? "Condition was false");
        }
    }

    /// <summary>
    /// 断言两个值相等。
    /// </summary>
    protected void AssertEqual<T>(string checkName, T expected, T actual)
    {
        bool pass = EqualityComparer<T>.Default.Equals(expected, actual);
        AssertTrue(checkName, pass, $"Expected: {expected}, Actual: {actual}");
    }

    /// <summary>
    /// 断言条件为假。
    /// </summary>
    protected void AssertFalse(string checkName, bool condition, string? message = null)
    {
        AssertTrue(checkName, !condition, message);
    }

    /// <summary>
    /// 断言抛出指定类型的异常。
    /// </summary>
    protected void AssertThrows<TException>(string checkName, Action action) where TException : Exception
    {
        try
        {
            action();
            AssertTrue(checkName, false, $"Expected {typeof(TException).Name} but no exception was thrown");
        }
        catch (TException)
        {
            AssertTrue(checkName, true);
        }
        catch (Exception ex)
        {
            AssertTrue(checkName, false, $"Expected {typeof(TException).Name} but got {ex.GetType().Name}");
        }
    }

    /// <summary>
    /// 通过计数。
    /// </summary>
    protected int PassedCount => _passedCount;

    /// <summary>
    /// 失败计数。
    /// </summary>
    protected int FailedCount => _failedCount;

    /// <summary>
    /// 退出码：0 = 全部通过，1 = 有失败。
    /// </summary>
    protected int ExitCode => _failedCount == 0 ? 0 : 1;
}
```

#### ValidationSession 集成版本

```csharp
/// <summary>
/// 使用 ValidationSession 的测试基类。
/// 产出 JSON artifact 作为主要证据源。
/// </summary>
public abstract class ValidationTestBase : TestBase, IDisposable
{
    private ValidationSession? _session;

    /// <summary>
    /// 开始验证会话。在 [SetUp] 中调用。
    /// </summary>
    protected void BeginValidation(ValidationSessionOptions options)
    {
        _session = ValidationSession.Start(options);
    }

    /// <summary>
    /// 执行一个 check，同时记录到基类和 ValidationSession。
    /// </summary>
    protected CheckResult Check(string name, bool condition,
        string? expected = null, string? actual = null, string? reasonCode = null, string? message = null)
    {
        AssertTrue(name, condition, message);
        return _session!.Check(name, condition, expected, actual, reasonCode, message);
    }

    public void Dispose()
    {
        _session?.Dispose();
    }
}
```

### 方案 2：测试分类规范（P0）

#### 三类测试

| 类型 | 运行方式 | 启动时间 | 适用范围 |
|------|----------|----------|----------|
| **单元测试** | `dotnet test` | < 100ms | 纯逻辑、无 Godot 依赖 |
| **场景测试** | `run-godot-scene.sh` | 3-8s | 需要 Godot 引擎的功能 |
| **集成测试** | `run-godot-scene.sh` | 5-15s | 多系统交互、游戏场景 |

#### 分类判定规则

```
需要 Godot 引擎？（Node, SceneTree, Physics, Input, Resource）
├─ 是 → 场景测试 / 集成测试
└─ 否 → 单元测试（NUnit）
```

### 方案 3：Flow 打印规范（P1）

#### 功能测试必须使用 Flow 模式

```csharp
// 开始
log.FlowStart("EntitySpawnPipeline", "spawning 3 entities");

// 每步
log.FlowStep("EntitySpawnPipeline", "probe1: spawn → register → lifecycle");

// 通过
log.FlowComplete("EntitySpawnPipeline", "3/3 passed, 12 entries, 0 errors");

// 失败
log.FlowFail("EntitySpawnPipeline", "probe2: spawn failed, registry returned null");
```

**优势**：
- AI 可以直接从 Flow 判断功能是否正常
- 不需要逐条分析每个 Log entry
- 失败时可以快速定位是哪一步出了问题

### 方案 4：TestDesigner 输出规范强化（P1）

TestDesigner 的 5 字段标准答案需要增加：

| 新增字段 | 含义 |
|----------|------|
| testType | 单元/场景/集成 |
| flowSteps | 预期 Flow 步骤（用于 Flow 打印） |
| logOwner | Log owner 名称（用于 logctl 过滤） |

### 方案 5：BDD 到测试追溯（P2）

在 bdd.md 中增加 `testRef` 字段：

```markdown
## Scenario: Entity 注册后生命周期回调触发

Given 一个新的 EntityRegistry
When 注册一个 probe entity
Then entity 收到 OnSpawned 回调
And 注册计数 +1

<!-- testRef: EntitySpawnPipelineRuntimeTest.cs::ProbeEntity_Registration_TriggersLifecycle -->
```

## 实施路径

| 阶段 | 内容 | 预计工作量 |
|------|------|-----------|
| Phase 1 | 创建 NUnit 项目 + 共享基类 | 1 SDD |
| Phase 2 | 迁移第一批纯逻辑测试 | 1 SDD |
| Phase 3 | Flow 打印规范 + TestDesigner 增强 | 1 SDD |
| Phase 4 | BDD 追溯 + CI 集成 | 1 SDD |

## 与 Code Review 的联动

TDD 产出的 artifact（ValidationSession JSON）应该成为 Code Review 的输入：

```
Code Review Phase 2（逐项审查）
  ├─ 读取 SDD 需求
  ├─ 读取实现代码
  ├─ 读取测试 artifact ← 新增
  │   ├─ ValidationSession JSON（check 通过率）
  │   ├─ Log JSONL（Flow 结构）
  │   └─ Exit code（0/1）
  └─ 判定 DONE/PARTIAL/MISSING/WRONG
```
