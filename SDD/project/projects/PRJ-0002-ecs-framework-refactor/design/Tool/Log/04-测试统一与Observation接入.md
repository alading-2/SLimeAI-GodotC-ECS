# 测试统一与 Observation 接入

> 更新：2026-05-31
> 状态：current design note

## 1. 当前问题

现在测试里常见几种结果表达混在一起：

- `GD.Print("PASS")`
- `GD.PushError("FAIL")`
- `_log.Success("[PASS]")`
- `_log.Error("[FAIL]")`
- `throw new Exception(...)`

这会让 runner 只能靠 pattern 猜结果。

## 2. 统一裁决

测试结果必须统一进入同一套观测语义：

- `Check`
- `Pass`
- `Fail`
- `Artifact`

console 只是显示层，不能作为唯一事实源。

## 3. 统一形态

建议每个 test scene / validation session 采用：

```text
Test start
  -> emit structured info
  -> run checks
  -> log pass/fail entries
  -> write artifact
  -> exit code reflects final verdict
```

## 4. Validation 形态

建议保留一个轻量 `ValidationSession`：

- 内部使用新的 Log 结构化 API。
- 记录 `checks`、`failureReasons`、`logs`。
- 最终写出 artifact JSON。
- artifact 中要有 `expectedInputs / expectedObservations / passCriteria / failCriteria`。

## 5. Runner 接入

scene runner 的职责应是：

- 注入环境变量。
- 收集 JSONL / artifact / stdout。
- 判断 exit code。
- 生成 gate report。
- 根据 artifact 和 structured logs 判断 pass/fail。

runner 不应该再依赖散乱的 `PASS` / `FAIL` 文本作为主判断。

## 6. 测试 helper 迁移

优先迁移这些地方：

- `Src/ECS/Test/SingleTest/Tools/Log/LogTest.cs`
- `Src/ECS/Test/SingleTest/ECS/DataOS/*`
- `Src/ECS/Test/SingleTest/ECS/System/SystemCore/*`
- `Src/ECS/Test/SingleTest/ECS/System/AbilitySystemTest/*`
- `Src/ECS/Test/SingleTest/ECS/System/DamageSystemTest/*`
- `Src/ECS/Test/SingleTest/ECS/System/Movement/*`
- `Src/ECS/Test/GlobalTest/MainTest/MainTest.cs`

这些地方最容易把日志和测试结果混成一团。

## 7. 结果约束

统一后应该达到：

- `PASS` / `FAIL` 是结构化事实。
- console 只显示摘要和关键错误。
- JSONL 可以回放。
- artifact 可以给 AI 做后续分析。
