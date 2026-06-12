# TDD 协议

> 将 TDD 的 Red-Green-Refactor 微循环集成到 SystemAgent 角色链中。
> SystemAgent 宏循环（Plan → Test Design → Implement → Review → Verify）是外层；
> TDD 微循环（RED → GREEN → REFACTOR）是 Implementer 在每项行为变更中的内层。

## 铁律

```text
先确认行为标准答案，再写能失败的验证，再写实现。
没有看见目标 check 因目标行为缺失而失败，就不能声称完成了 TDD。
```

TDD 不是硬套某个测试框架。SlimeAI 当前默认验证环境是 Godot C# / DataOS / Log / Validation artifact。不要为了追求“纯代码快测”引入脱离 Godot 运行语义的测试入口。

## 角色分工

```text
Planner / DeepThink
  -> 明确目标和边界
TestDesigner
  -> 把目标变成 expectedInputs / expectedObservations / passCriteria / failCriteria / artifactPath
Implementer
  -> RED -> GREEN -> REFACTOR
Reviewer / Verifier
  -> 检查代码、证据和 artifact
```

## 写测试前必须确认

每个新增或修改行为先回答：

1. 行为是什么。
2. 输入是什么。
3. 应观察到什么。
4. 通过条件是什么。
5. 失败条件是什么。
6. 证据落在哪里：scene artifact、DataOS validator 输出、logctl analysis、build 输出或其他可复查路径。

用户未确认时，可以用默认假设推进小切片，但必须把默认行为和影响写清楚。不能直接硬写测试来替代需求确认。

## 可用验证入口

| 类型 | 使用场景 | 主证据 |
| --- | --- | --- |
| Godot headless scene | Runtime / Capability / Godot 生命周期 / 资源 / 场景 / 游戏胶水 | `index.json`、`result.json`、Validation artifact |
| DataOS validator | descriptor、authoring、snapshot schema 和数据规则 | validator rule/check 输出 |
| `dotnet build` | 编译和类型门禁 | build 结果 |
| `logctl analyze/query` | 运行后日志整理、flow / failure 下钻 | `summary.md`、`ai-context.md`、`flows/`、`failures/` |

当前不使用脱离 Godot 运行语义的新测试框架作为默认方向。除非用户单独批准并有明确工程理由，否则不新增测试框架。

## RED

RED 是写一个最小验证，让它因为目标行为缺失而失败。

可接受 RED：

| 类型 | 可接受证据 | 不可接受 |
| --- | --- | --- |
| Godot scene | 目标 scene failed，artifact 中目标 check 为 fail，failure reason 指向目标行为 | 只有 stderr、只有 exit code 非 0、没有 artifact、资源路径/编译错误 |
| DataOS validator | 目标 rule/check failure，输入样例和失败原因明确 | schema parse error、命令不存在、环境缺依赖 |
| Build gate | 对类型契约变更，目标编译错误能证明旧调用点未迁移 | unrelated compile error |
| Scene metadata/gate | scene-gate 指出五字段、catalog、manifest 或 freshness 缺口 | 人工声明“看起来缺了” |

Godot RED 可以是 check-level fail，不要求整个进程崩溃。

## GREEN

GREEN 是用最小实现让同一个验证通过。

可接受 GREEN：

| 类型 | 可接受证据 |
| --- | --- |
| Godot scene | 同一 scene 的 `index.json` / `result.json` passed，artifact `status=pass`、`failureReasons=[]`，目标 check 为 pass |
| DataOS validator | 同一 rule/check 通过，输出包含目标 rule/check 名 |
| Build gate | 目标编译错误消失，且没有新增 unrelated error |
| Scene metadata/gate | gate report 通过或明确 warning 不影响目标行为 |

`无 error`、`exit code 0`、stdout PASS marker 或 clean terminal summary 只能辅助定位，不能单独作为 GREEN。

## REFACTOR

只在 GREEN 后清理代码：

- 消除重复。
- 改进命名。
- 提取 helper。
- 收口日志 / artifact 字段。

Refactor 后必须重新跑相关 GREEN 证据。

## Godot 测试约束

- 新或改动的行为测试优先使用 `ValidationSession` / `CheckResult`。
- artifact 和 structured log 是主事实源，stdout pattern 只是过渡 fallback。
- 不新增裸 `GD.Print("PASS")`、`GD.PushError("FAIL")`、`[PASS]`、`[FAIL]` 作为断言事实。
- 测试日志写到 `.ai-temp/scene-tests/runs/...`，不要污染源码目录。
- 运行 Godot scene 前先确认承载游戏 runner 是否存在，不能伪造 scene smoke 通过。

## 与 BDD 的关系

BDD 是行为例子，不是归档装饰。

- 长期 BDD 优先靠近设计文档或写在设计文档的“行为验收”小节。
- SDD `bdd.md` 可以只摘录本任务执行的场景并链接到设计源。
- 每个需要验证的 Scenario 至少应能映射到一个 check、validator rule 或 gate。
- 非行为任务可以明确 `Required: false`。

## 在 ReviewGates 中的位置

`RV-TEST-COVERAGE` gate 检查：

- 是否先定义了行为标准答案。
- RED 是否证明目标行为缺失。
- GREEN 是否引用同一 check / rule / scene。
- artifact 是否足以复查。
- 是否把 stdout、exit code 或人工描述当成唯一证据。

Code Review 仍然读生产代码判断功能和质量；TDD artifact 只是 evidenceRef。

## 禁止行为

- 先写实现，后补一个永远通过的测试。
- 测试一开始就通过还声称 RED 完成。
- 未确认行为就硬写测试。
- 因为“很简单”跳过可复查证据。
- 用手动测试或 stdout PASS 替代 artifact。
- 未经确认引入新测试框架。
