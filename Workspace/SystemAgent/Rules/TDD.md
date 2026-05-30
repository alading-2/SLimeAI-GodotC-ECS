# TDD 协议

> 将 TDD 的 Red-Green-Refactor 微循环集成到 SystemAgent 角色链中。
> SystemAgent 宏循环（Plan → TDD → Implement → Review → Verify）是外层；
> TDD 微循环（RED → GREEN → REFACTOR）是 Implementer 在每项任务中的内层。
> 参考：`Resources/Skills/test-driven-development/SKILL.md`

## 铁律

```
NO PRODUCTION CODE WITHOUT A FAILING TEST FIRST
先有测试，再看它失败，再写实现。
已经写了代码但没有测试？删除代码，从测试开始。
```

## 在 SystemAgent 中的角色

TDD 微循环由 **Implementer** 驱动，由 **Verifier** 验证。

```
宏循环:  Planner → TestDesigner → [TDD 微循环] → Reviewer → Verifier → Retrospective
                                      ↑
微循环:                              RED → GREEN → REFACTOR
```

## 微循环步骤

### RED — 写失败测试

1. 针对一个具体行为（一个函数、一个方法、一个 BDD Scenario 中的检查项），先写测试代码。
2. 测试代码必须可独立运行。
3. Runtime 测试：NUnit `[Test]` + `Assert`。在 `SlimeAI/Tests/SlimeAI.GameOS.Tests/` 中。
4. Godot 验证场景：`SceneValidationSession.Check()`。在 `Games/BrotatoLike/Src/Validation/` 中。

### 确认 RED — 看它失败

**强制步骤，不可跳过。**

```bash
# Runtime 测试
dotnet test --filter "FullyQualifiedName~TestName"

# Godot 场景测试
cd Games/BrotatoLike
Tools/run-godot-scene.sh run res://Src/Validation/... --timeout 10
```

确认：
- 测试失败（不是编译错误）
- 失败原因是因为功能缺失（不是 typo、不是已存在的 bug）
- **测试意外通过？** 说明测试没有真正测试新行为。重写。

### 可接受 RED 证据

RED 证据必须证明新测试或新 check 能针对目标缺失行为失败。可接受形式：

| 类型 | 可接受 RED 证据 | 不可接受 |
| --- | --- | --- |
| Runtime NUnit | `dotnet test --filter ...` 输出显示目标 `[Test]` assertion failure，失败消息指向新行为 | build/restore 失败、测试过滤没命中、 unrelated test failure |
| DataOS validator | validator 输出包含目标 rule/check failure 和输入样例，且失败原因来自新规则未满足 | schema parse error、命令不存在、环境缺依赖 |
| Godot scene | `index.json`/`result.json` 指向目标 scene failed，scene artifact `status=fail` 且 `checks[]` 中目标 check 为 fail；或 artifact `failureReasons` 指向目标 check | 只有 stderr、只有 exit code 非 0、没有 artifact、失败来自脚本编译错误 |
| Scene README / catalog metadata | scene-gate 或 analyzer gate report 指出五字段、catalog、manifest 或 freshness 缺口 | 人工声明"看起来缺了"但无路径和字段 |

Godot RED 可以是一个 `SceneValidationSession.Check()` 的 check-level fail，不要求整个 Godot 进程崩溃。

### GREEN — 最小实现

写恰好让测试通过的代码。不添加测试没要求的逻辑。不"顺便"重构。

### 确认 GREEN — 看它通过

**强制步骤，不可跳过。**

```bash
# 同上验证命令，确认测试现在通过
# 同时确认旧测试没有回归
```

### 可接受 GREEN 证据

GREEN 证据必须和 RED 针对同一测试或 check：

| 类型 | 可接受 GREEN 证据 |
| --- | --- |
| Runtime NUnit | 同一 `dotnet test --filter ...` 通过，必要时追加相关 test project 全量通过 |
| DataOS validator | 同一 validator rule/check 通过，输出包含目标 rule/check 名 |
| Godot scene | 同一 scene 的 `index.json` entry `status=passed`、`result.json` `status=passed`、scene artifact `status=pass`、`failureReasons=[]`，五个标准答案字段非空，并且目标 `checks[]` 为 pass |
| Scene metadata/gate | analyzer 或 scene-gate gate report 对 README、catalog、manifest、artifact freshness 给出 `pass` 或明确 `warn` 原因 |

`无 error`、`exit code 0`、stdout PASS marker 或 clean terminal summary 只能辅助定位，不能单独作为 GREEN。

### REFACTOR — 清理

只在全绿后进行。消除重复，改进命名，提取 helper。

## 与 Godot 验证场景的配合

Godot 场景测试有特殊约束：
- 启动 Godot 进程有耗时（~3-8秒），不能像 NUnit 那样秒级循环
- 因此对 Godot 验证场景，RED-GREEN 循环可以适当放宽：
  - **RED 阶段**：先写好 `SceneValidationSession.Check()` 调用框架（含 failCriteria），运行一次确认 RED
  - **GREEN 阶段**：实现功能代码，再次运行场景确认 GREEN
  - **REFACTOR 阶段**：代码清理后运行全量场景确认无回归

对于纯 Runtime 代码（不涉及 GodotBridge），仍严格执行每次一个函数的 NUnit RED-GREEN-REFACTOR。

## 在 ReviewGates 中的位置

`RV-TEST-COVERAGE` gate 在 Implement 阶段之前触发。Reviewer 必须检查：
- 每个新增/修改的公共方法是否有对应测试
- 是否有测试曾经 RED（失败）的证据，且失败来自目标行为缺失，不是编译错误或环境错误
- 测试是否在实现后变为 GREEN，且 GREEN 证据引用同一 test/check 的输出或 artifact
- Godot 场景是否检查了 `index.json`、per-scene `result.json` 和 scene artifact 五字段

不允许 "测试一开始就通过" 的情况 — 这意味着测试没真正验证新行为。

## 禁止行为

- "先写实现，后补测试" — 违反 TDD（TDD skill 明确禁止）
- "测试一开始就通过了" — 测试未真正验证行为
- "太简单不需要测试" — TDD skill 的 rationalization 清单
- "手动测试过了就行" — 手动测试不可重复、不可自动化

## Godot 陷阱提醒

写 Godot 验证场景时，务必对照 `SlimeAI/DocsAI/GameOS/GodotPitfalls.md` 排除已知陷阱（坐标系、Camera2D、生命周期等）。

## 与 TestDesigner 的配合

- **TestDesigner** 设计验收标准（5 字段标准答案），输出到 spec/bdd 中
- **Implementer** 在 TDD 微循环中将 TestDesigner 的标准答案拆解为可运行的测试代码
- 每个 BDD Scenario 至少对应一个 `SceneValidationSession.Check()` 调用
