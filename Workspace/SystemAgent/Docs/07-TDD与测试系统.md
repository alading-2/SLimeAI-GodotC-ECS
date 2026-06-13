# TDD 与测试系统

## 当前结论

SystemAgent 的 TDD 方向已纠偏：当前不新增脱离 Godot 运行语义的测试框架，不把 Runtime/Data 当成脱离 Godot 的单测试点。

SlimeAI 的行为验证需要覆盖 Godot C# 运行环境、Node 生命周期、资源路径、DataOS、Log / Validation artifact 和 scene runner。测试系统的第一目标不是“跑得最快”，而是“能证明真实行为，并且证据可被 AI 复查”。

## 真实缺口

当前问题不是缺某个测试框架，而是缺稳定的测试设计流程：

1. **行为确认不足**：AI 容易直接写测试，没有先确认输入、观察、通过条件、失败条件和 artifact 路径。
2. **标准答案分散**：FeatureSpec、设计文档、SDD 摘录、测试 README、artifact 字段之间没有清晰引用关系。
3. **ValidationSession 使用不足**：结构化 artifact 没有成为默认断言载体。
4. **旧测试仍有 stdout 依赖**：部分测试仍依赖 `PASS/FAIL` 文本、exit code 或分散 Log。
5. **RED/GREEN 证据弱**：有时只能看到“命令过了”，不能看到同一个 check 从 fail 变 pass。

## TDD 在本项目中的定义

TDD 不是先选择某个测试框架，而是按以下顺序工作：

```text
确认行为标准答案
  -> 写一个能失败的 check / rule / scene
  -> 确认 RED 失败来自目标行为缺失
  -> 最小实现
  -> 同一个 check / rule / scene GREEN
  -> 清理并复跑
```

写测试前必须明确：

- expectedInputs
- expectedObservations
- passCriteria
- failCriteria
- artifactPath

这些字段优先来自 FeatureSpec 的 `TDD Handoff`，也可以由 TestDesigner 在用户确认后输出。SDD `bdd.md` 可以摘录任务需要执行的场景，但长期行为事实源应靠近设计文档，以 `.FeatureSpec.md` 为默认承载。

## 当前测试形态

### 旧式场景测试

特征：

- 继承 `Node`，在 `_Ready()` 中运行。
- 手写 `AssertTrue` / `AssertEqual`。
- 使用 `GD.Print("PASS")`、`GD.PushError("FAIL")` 或字符串 marker。

问题：

- stdout 不是稳定 oracle。
- CI / AI 需要猜测结果。
- 失败原因不可结构化下钻。

### 当前推荐形态

特征：

- 仍通过 Godot headless scene 运行。
- 断言进入 `ValidationSession` / `CheckResult`。
- 结果输出为 scene artifact、structured log、`index.json` 和 `result.json`。
- 必要时用 `logctl analyze/query` 下钻 flow、failure 和 missing fields。

主证据优先级：

1. Validation artifact。
2. scene runner `index.json` / `result.json`。
3. `logctl analyze` 产物。
4. stdout 只作辅助，不作主证据。

## Data 试点

用户指定先从 `Src/ECS/Runtime/Data` 做试点。试点目标是验证“行为标准答案先行 + Godot scene + Validation artifact”的流程，而不是引入新测试框架。

推荐方式：

1. 选 1 个 Data 行为，例如 range policy、typed `DataKey<T>` 写入读取、snapshot apply 或 modifier effective value。
2. 在设计旁写 `.FeatureSpec.md`，明确行为、实现指引和 TDD Handoff，必要时同步 SDD `bdd.md` 引用。
3. 改造现有 Data Godot 测试场景，让目标 check RED。
4. 最小实现后让同一个 check GREEN。
5. 用户复核试点思路后，再决定是否推广到其他 Runtime owner。

## Logger 在测试中的角色

Logger 不是普通打印工具，而是 evidence plane 的一部分。

在测试中它负责：

- 把 check 结果写成结构化 Validation entry。
- 生成 artifact 作为 AI 默认判断入口。
- 让 `logctl analyze` 能把运行结果压缩成 summary、flow 和 failure。

测试不能退回只看 live stdout。Log 第三层“源码调用点语义化”未完成时，live stdout 仍可能分散；这更说明 artifact 必须成为主证据。

## 与 Code Review 的边界

TDD/Test 证明“有没有可复查行为证据”。Code Review 仍然读生产代码，判断功能是否实现、架构红线是否被破坏、代码质量是否够用。

测试结果可以作为 `evidenceRef`，但不能替代 Code Review。

## 近期优化方向

1. Data 试点：Godot scene + ValidationSession，不新增脱离 Godot 的测试框架。
2. TestDesigner 输出标准答案前置，测试前必须确认行为。
3. FeatureSpec 靠近设计文档，SDD 只摘录当前任务场景。
4. 清理旧 stdout PASS/FAIL 依赖。
5. 完善 artifact / logctl 作为 RED/GREEN 主证据的读取规则。
